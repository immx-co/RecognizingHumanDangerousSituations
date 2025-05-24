using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using ClassLibrary;
using ClassLibrary.Database;
using ClassLibrary.Repository;
using ClassLibrary.Services;
using DangerousSituationsUI.Services;
using DangerousSituationsUI.ViewModels;
using DangerousSituationsUI.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Linq;

namespace DangerousSituationsUI
{
    public partial class App : Application
    {
        public new static App? Current => Application.Current as App;

        public Window? CurrentWindow
        {
            get
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    return desktop.MainWindow;
                }
                else return null;
            }
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var envPath = Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\.env");
            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
            }
            else
            {
                Log.Error("Не удалось подгрузить env переменные. Требуется token телеграм бота.");
                throw new InvalidOperationException("Не удалось подгрузить env переменные. Требуется token телеграм бота.");
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();

                IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"))
                    .AddJsonFile("appsettings.json")
                    .Build();

                var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Debug()
                  .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Information).WriteTo.File(@"Logs\Info.log"))
                  .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Debug).WriteTo.File(@"Logs\Debug.log"))
                  .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Warning).WriteTo.File(@"Logs\Warning.log"))
                  .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error).WriteTo.File(@"Logs\Error.log"))
                  .CreateLogger();

                IServiceCollection servicesCollection = new ServiceCollection();

                servicesCollection.AddSingleton<IScreen, IScreenRealization>();

                servicesCollection.AddSingleton(configuration);
                servicesCollection.AddSingleton<Serilog.ILogger>(loggerConfiguration);
                servicesCollection.AddSingleton<InputApplicationViewModel>();
                servicesCollection.AddSingleton<NavigationViewModel>();
                servicesCollection.AddSingleton<MainViewModel>();
                servicesCollection.AddSingleton<VideoEventJournalViewModel>();
                servicesCollection.AddSingleton<ConfigurationViewModel>();
                servicesCollection.AddSingleton<RegistrationViewModel>();
                servicesCollection.AddSingleton<AuthorizationViewModel>();
                servicesCollection.AddSingleton<LogJournalViewModel>();
                servicesCollection.AddSingleton<VideoPlayerViewModel>();
                servicesCollection.AddTransient<ExportService>();

                servicesCollection.AddSingleton<ConfigurationService>();
                servicesCollection.AddTransient<FilesService>();
                servicesCollection.AddTransient<VideoService>();
                servicesCollection.AddTransient<RectItemService>();
                servicesCollection.AddTransient<FigItemService>();

                servicesCollection.AddSingleton<UserManagementViewModel>();
                servicesCollection.AddSingleton<AddUserViewModel>();
                servicesCollection.AddSingleton<DialogService>();
                servicesCollection.AddTransient<UserService>();

                servicesCollection.AddDbContext<ApplicationContext>(options =>
                    options.UseNpgsql(
                        configuration.GetConnectionString("dbStringConnection")),
                        ServiceLifetime.Transient
                    );

                servicesCollection.AddSingleton<HubConnectionWrapper>();

                servicesCollection.AddSingleton<PasswordHasher>();

                servicesCollection.AddScoped<IRepository, Repository>();
                //servicesCollection.AddSingleton

                string telegramBotToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");

                long? chatId = null;
                string? chatIdStr = Environment.GetEnvironmentVariable("CHAT_ID");
                if (!string.IsNullOrEmpty(chatIdStr) && long.TryParse(chatIdStr, out long tempChatId))
                {
                    chatId = tempChatId;
                }
                servicesCollection.AddSingleton<TelegramBotAPI>(provider =>
                    new TelegramBotAPI(
                        telegramBotToken,
                        chatId,
                        provider.GetRequiredService<IServiceProvider>(),
                        provider.GetRequiredService<LogJournalViewModel>(),
                        provider.GetRequiredService<NavigationViewModel>()
                    )
                );

                ServiceProvider servicesProvider = servicesCollection.BuildServiceProvider();

                using (var scope = servicesProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                }

                Log.Logger = servicesProvider.GetRequiredService<Serilog.ILogger>();

                servicesProvider.GetRequiredService<HubConnectionWrapper>().Start();
                Log.Logger.Information("Оформлено подключение к хабу.");
                LogJournalViewModel.logString += "Оформлено подключение к хабу.\n";
                desktop.MainWindow = new NavigationWindow
                {
                    DataContext = servicesProvider.GetRequiredService<NavigationViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}
