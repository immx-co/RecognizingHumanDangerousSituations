using Telegram.Bot;

using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using System.Threading;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using DangerousSituationsUI.ViewModels;
using System.IO;
using Avalonia.Media;
using Avalonia.Threading;
using OpenCvSharp.Dnn;
using SkiaSharp;
using ClassLibrary.Database.Models;
using ClassLibrary.Database;
using Microsoft.Extensions.DependencyInjection;
using ClassLibrary.Repository;


namespace DangerousSituationsUI;

public class TelegramBotAPI : IDisposable
{
    #region Private Fields
    private static ITelegramBotClient _botClient;

    private long? _chatId = null;

    private static ReceiverOptions _receiverOptions;

    private CancellationTokenSource _cts;

    private bool _isRunning;

    private NavigationViewModel _navigationViewModel;

    private LogJournalViewModel _logJournalViewModel;

    private IServiceProvider _serviceProvider;
    #endregion

    #region Properties
    public bool IsRunning => _isRunning;

    public long? ChatId { get => _chatId; set => _chatId = value; }
    #endregion

    #region Constructor
    public TelegramBotAPI(string token, long? chatId, IServiceProvider serviceProvider, LogJournalViewModel logJournalViewModel, NavigationViewModel navigationViewModel)
    {
        _serviceProvider = serviceProvider;
        _navigationViewModel = navigationViewModel;
        _logJournalViewModel = logJournalViewModel;
        _chatId = chatId;

        if (string.IsNullOrEmpty(token))
        {
            Log.Warning("Telegram bot token cannot be empty.");
            _logJournalViewModel.LogString += "Telegram bot token cannot be empty\n";
            throw new ArgumentException("Telegram bot token cannot be empty", nameof(token));
        }

        Initialize(token);

        UpdateChatId(chatId);
    }
    #endregion

    #region Public Methods
    public async Task StartBotAsync()
    {
        if (IsRunning) return;

        try
        {
            _cts = new CancellationTokenSource();
            _botClient.StartReceiving(
                UpdateHandler,
                ErrorHandler,
                _receiverOptions,
                _cts.Token
            );

            _isRunning = true;
            var me = await _botClient.GetMe();
            Log.Information($"{me.FirstName} запущен!");
            _logJournalViewModel.LogString += $"{me.FirstName} запущен!\n";
        }
        catch (Exception ex)
        {
            Log.Error($"Ошибка при запуске бота: {ex}");
            _logJournalViewModel.LogString += $"Ошибка при запуске бота: {ex}\n";
            _isRunning = false;
            throw;
        }
    }

    public void StopBot()
    {
        if (_isRunning) return;

        try
        {
            _cts.Cancel();
            _isRunning = false;
            Log.Information("Бот остановлен");
            _logJournalViewModel.LogString += "Бот остановлен\n";
        }
        catch (Exception ex)
        {
            Log.Error($"Ошибка при остановке бота: {ex}");
            _logJournalViewModel.LogString += $"Ошибка при остановке бота: {ex}\n";
            throw;
        }
    }

    public async Task RestartBotAsync(string newToken = null)
    {
        StopBot();

        if (!string.IsNullOrEmpty(newToken))
        {
            Initialize(newToken);
        }

        await StartBotAsync();
    }

    public async Task SendWelcomeMessage(string welcomeText, string imagePath)
    {
        try
        {
            await using var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            await _botClient.SendPhoto(
                chatId: _chatId,
                photo: InputFile.FromStream(fileStream, "welcome.jpg"),
                caption: welcomeText);
        }
        catch (Exception ex)
        {
            Log.Error($"Ошибка при отправке приветствия: {ex.Message}");
            _logJournalViewModel.LogString += $"Ошибка при отправке приветствия: {ex.Message}\n";
        }
    }

    public async Task SendFarewellMessage(string farewellText, string imagePath)
    {
        try
        {
            await using var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            await _botClient.SendPhoto(
                chatId: _chatId,
                photo: InputFile.FromStream(fileStream, "farewell.jpg"),
                caption: farewellText);
        }
        catch (Exception ex)
        {
            Log.Error($"Ошибка при отправке прощания: {ex.Message}");
            _logJournalViewModel.LogString += $"Ошибка при отправке прощания: {ex.Message}\n";
        }
    }

    public void UpdateChatId(long? chatId)
    {
        _chatId = chatId;

        if (_chatId.HasValue)
        {
            try
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);

                    var chat = await _botClient.GetChat(_chatId.Value);
                    string userName = chat.Username ?? chat.FirstName ?? "Пользователь";

                    string welcomeImagePath = Path.Combine(AppContext.BaseDirectory, "..//..//..//Assets/welcome.jpg");
                    await SendWelcomeMessage($"Добро пожаловать в Recognition Dangerous Situations, {userName}!", welcomeImagePath);
                    _logJournalViewModel.LogString += $"Обновлен ChatId у телеграм бота, chatId == {ChatId}";
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _navigationViewModel.TgBotConnectionStatus = Brushes.Green;
                    });
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при отправке приветственного сообщения.");
                _logJournalViewModel.LogString += $"Ошибка при отправке приветственного сообщения.: {ex.Message}";
                _navigationViewModel.TgBotConnectionStatus = Brushes.Red;
            }
        }
        else
        {
            _navigationViewModel.TgBotConnectionStatus = Brushes.Red;
        }
    }

    public void Dispose()
    {
        StopBot();
        _cts?.Dispose();
        _botClient = null;
    }

    public async Task SendEventDataWrapperAsync(BitmapModel frameBitmapModel, RecognitionResult det)
    {
        using var ms = new MemoryStream();
        frameBitmapModel.frame.Save(ms);
        ms.Seek(0, SeekOrigin.Begin);
        using var skBitmap = SKBitmap.Decode(ms.ToArray());

        using var surface = SKSurface.Create(new SKImageInfo(skBitmap.Width, skBitmap.Height));
        var canvas = surface.Canvas;

        canvas.DrawBitmap(skBitmap, 0, 0);

        using var detPaint = new SKPaint
        {
            Color = SKColors.Red,
            StrokeWidth = 3,
            IsStroke = true,
            Style = SKPaintStyle.Stroke
        };

        float topLeftCornerX = det.X - det.Width / 2;
        float topLeftCornerY = det.Y - det.Height / 2;
        canvas.DrawRect(topLeftCornerX, topLeftCornerY, det.Width, det.Height, detPaint);

        using var classNamePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 16,
            IsAntialias = true
        };
        canvas.DrawText("Lying", topLeftCornerX, topLeftCornerY - 5, classNamePaint);

        using var skeletonPaint = new SKPaint
        {
            Color = SKColors.Gray,
            StrokeWidth = 2,
            IsStroke = true,
            Style = SKPaintStyle.Stroke
        };

        using var skeletonPointPaint = new SKPaint
        {
            Color = SKColors.Red,
            StrokeWidth = 4,
            IsStroke = true,
            Style = SKPaintStyle.Fill
        };

        canvas.DrawCircle(det.Nose[0], det.Nose[1], 3, skeletonPointPaint);
        canvas.DrawCircle(det.LeftEye[0], det.LeftEye[1], 3, skeletonPointPaint);
        canvas.DrawCircle(det.RightEye[0], det.RightEye[1], 3, skeletonPointPaint);
        canvas.DrawCircle(det.LeftEar[0], det.LeftEar[1], 3, skeletonPointPaint);
        canvas.DrawCircle(det.RightEar[0], det.RightEar[1], 3, skeletonPointPaint);
        canvas.DrawCircle(det.LeftShoulder[0], det.LeftShoulder[1], 3, skeletonPointPaint);
        canvas.DrawCircle(det.RightShoulder[0], det.RightShoulder[1], 3, skeletonPointPaint);
        canvas.DrawCircle(det.LeftElbow[0], det.LeftElbow[1], 3, skeletonPointPaint);
        canvas.DrawCircle(det.RightElbow[0], det.RightElbow[1], 3, skeletonPointPaint);
        canvas.DrawCircle(det.LeftWrist[0], det.LeftWrist[1], 3, skeletonPointPaint);
        canvas.DrawCircle(det.RightWrist[0], det.RightWrist[1], 3, skeletonPointPaint);
        canvas.DrawCircle(det.LeftHip[0], det.LeftHip[1], 3, skeletonPointPaint);
        canvas.DrawCircle(det.RightHip[0], det.RightHip[1], 3, skeletonPointPaint);
        canvas.DrawCircle(det.LeftKnee[0], det.LeftKnee[1], 3, skeletonPointPaint);
        canvas.DrawCircle(det.RightKnee[0], det.RightKnee[1], 3, skeletonPointPaint);
        canvas.DrawCircle(det.LeftAnkle[0], det.LeftAnkle[1], 3, skeletonPointPaint);
        canvas.DrawCircle(det.RightAnkle[0], det.RightAnkle[1], 3, skeletonPointPaint);

        canvas.DrawLine(det.LeftShoulder[0], det.LeftShoulder[1], det.RightShoulder[0], det.RightShoulder[1], skeletonPaint);
        canvas.DrawLine(det.LeftShoulder[0], det.LeftShoulder[1], det.LeftElbow[0], det.LeftElbow[1], skeletonPaint);
        canvas.DrawLine(det.RightShoulder[0], det.RightShoulder[1], det.RightElbow[0], det.RightElbow[1], skeletonPaint);
        canvas.DrawLine(det.LeftElbow[0], det.LeftElbow[1], det.LeftWrist[0], det.LeftWrist[1], skeletonPaint);
        canvas.DrawCircle(det.RightElbow[0], det.RightElbow[1], 3, skeletonPointPaint);
        canvas.DrawLine(det.RightElbow[0], det.RightElbow[1], det.RightWrist[0], det.RightWrist[1], skeletonPaint);
        canvas.DrawLine(det.LeftHip[0], det.LeftHip[1], det.RightHip[0], det.RightHip[1], skeletonPaint);
        canvas.DrawLine(det.LeftHip[0], det.LeftHip[1], det.LeftKnee[0], det.LeftKnee[1], skeletonPaint);
        canvas.DrawLine(det.RightHip[0], det.RightHip[1], det.RightKnee[0], det.RightKnee[1], skeletonPaint);
        canvas.DrawLine(det.LeftKnee[0], det.LeftKnee[1], det.LeftAnkle[0], det.LeftAnkle[1], skeletonPaint);
        canvas.DrawLine(det.RightKnee[0], det.RightKnee[1], det.RightAnkle[0], det.RightAnkle[1], skeletonPaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        using var resultMs = new MemoryStream();
        data.SaveTo(resultMs);
        var frameBytes = resultMs.ToArray();

        var detectionInfo = $"Человек упал {frameBitmapModel.timeSpan}! Координаты: X={topLeftCornerX}, Y={topLeftCornerY}, " +
                           $"Ширина={det.Width}, Высота={det.Height}";
        await SendEventDataAsync(frameBytes, detectionInfo);
    }
    #endregion

    #region Private Methods
    private void Initialize(string token)
    {
        _botClient = new TelegramBotClient(token);
        _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[]
           {
                UpdateType.Message,
            },
            DropPendingUpdates = true,
        };
        _cts = new CancellationTokenSource();
        _isRunning = false;
    }

    private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    {
                        string? userMessage = update.Message.Text;
                        using var scope = _serviceProvider.CreateScope();
                        var repository = scope.ServiceProvider.GetRequiredService<IRepository>();
                        if (userMessage == "/start")
                        {
                            _chatId = update.Message.Chat.Id;
                            ClassLibrary.Database.Models.User? dbUser = repository.GetUserByNickname(_navigationViewModel.GetParsedUserName());
                            if (dbUser != null)
                            {
                                repository.UpdateChatIdOnUser(_chatId, dbUser);
                            }
                            string welcomeImagePath = Path.Combine(AppContext.BaseDirectory, "..//..//..//Assets/welcome.jpg");
                            await SendWelcomeMessage($"Добро пожаловать в Recognition Dangerous Situations, {update.Message.From}!", welcomeImagePath);
                            _navigationViewModel.TgBotConnectionStatus = Brushes.Green;
                            return;
                        }
                        else if (userMessage == "/stop")
                        {
                            ClassLibrary.Database.Models.User? dbUser = repository.GetUserByNickname(_navigationViewModel.GetParsedUserName());
                            if (dbUser != null)
                            {
                                repository.UpdateChatIdOnUser(null, dbUser);
                            }
                            string farewellImagePath = Path.Combine(AppContext.BaseDirectory, "..//..//..//Assets/farewell.jpg");
                            await SendFarewellMessage($"Досвидания, {update.Message.From}!", farewellImagePath);
                            _chatId = null;
                            _navigationViewModel.TgBotConnectionStatus = Brushes.Red;
                            return;
                        }
                        else
                        {
                            var message = update.Message;
                            _chatId = message.Chat.Id;
                            var user = message.From;
                            Debug.Write($"{user.FirstName} ({user.Id}) написал сообщение: {message.Text}");
                            Log.Information($"{user.FirstName} ({user.Id}) написал сообщение: {message.Text}");
                            _logJournalViewModel.LogString += $"{user.FirstName} ({user.Id}) написал сообщение: {message.Text}\n";

                            var chat = message.Chat;
                            await botClient.SendMessage(
                                chat.Id,
                                message.Text,
                                replyParameters: message.MessageId
                                );

                            return;
                        }
                    }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
            _logJournalViewModel.LogString += $"{ex.ToString()}\n";
            _navigationViewModel.TgBotConnectionStatus = Brushes.Red;
        }
    }

    private Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        var ErrorMessage = error switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => error.ToString()
        };

        Debug.Write(ErrorMessage);
        Log.Error(ErrorMessage);
        _logJournalViewModel.LogString += $"{ErrorMessage}\n";
        return Task.CompletedTask;
    }

    private async Task SendEventDataAsync(byte[] imageBytes, string detectionInfo)
    {
        try
        {
            using var memoryStream = new MemoryStream(imageBytes);
            await _botClient.SendPhoto(
                chatId: _chatId,
                photo: InputFile.FromStream(memoryStream, "image.jpg"),
                caption: detectionInfo
            );
        }
        catch (Exception ex)
        {
            Log.Error($"Ошибка при отправке изображения с детекцией боту: {ex.Message}");
            _logJournalViewModel.LogString += $"Ошибка при отправке изображения с детекцией боту: {ex.Message}\n";
        }
    }
    #endregion
}
