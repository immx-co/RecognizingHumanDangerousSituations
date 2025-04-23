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


namespace DangerousSituationsUI;

public class TelegramBotAPI : IDisposable
{
    #region Private Fields
    private static ITelegramBotClient _botClient;

    private long _chatId;

    private static ReceiverOptions _receiverOptions;

    private CancellationTokenSource _cts;

    private bool _isRunning;

    private LogJournalViewModel _logJournalViewModel;
    #endregion

    #region Properties
    public bool IsRunning => _isRunning;
    #endregion

    #region Constructor
    public TelegramBotAPI(string token, LogJournalViewModel logJournalViewModel)
    {
        _logJournalViewModel = logJournalViewModel;

        if (string.IsNullOrEmpty(token))
        {
            Log.Warning("Telegram bot token cannot be empty");
            _logJournalViewModel.LogString += "Telegram bot token cannot be empty\n";
            throw new ArgumentException("Telegram bot token cannot be empty", nameof(token));
        }

        Initialize(token);
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

    public void Dispose()
    {
        StopBot();
        _cts?.Dispose();
        _botClient = null;
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
                        if (update.Message.Text == "/start")
                        {
                            _chatId = update.Message.Chat.Id;
                            string welcomeImagePath = Path.Combine(AppContext.BaseDirectory, "..//..//..//Assets/welcome.jpg");
                            await SendWelcomeMessage($"Добро пожаловать в Recognition Dangerous Situations, {update.Message.From}!", welcomeImagePath);
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
            _logJournalViewModel.LogString += $"{ex.ToString()}\n" ;
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
    #endregion
}
