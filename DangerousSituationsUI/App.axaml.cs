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
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();

                IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"))
                    .AddJsonFile("appsettings.json")
                    .Build();

                Log.Logger = LoggerSetup.CreateLogger();

                IServiceCollection servicesCollection = new ServiceCollection();

                servicesCollection.AddSingleton<IScreen, IScreenRealization>();

                servicesCollection.AddSingleton(configuration);
                servicesCollection.AddSingleton<InputApplicationViewModel>();
                servicesCollection.AddSingleton<NavigationViewModel>();
                servicesCollection.AddSingleton<MainViewModel>();
                servicesCollection.AddSingleton<VideoEventJournalViewModel>();
                servicesCollection.AddSingleton<ConfigurationViewModel>();

                servicesCollection.AddSingleton<ConfigurationService>();
                servicesCollection.AddTransient<FilesService>();
                servicesCollection.AddTransient<VideoService>();
                servicesCollection.AddTransient<RectItemService>();

                servicesCollection.AddDbContext<ApplicationContext>(options => 
                    options.UseNpgsql(
                        configuration.GetConnectionString("dbStringConnection")), 
                        ServiceLifetime.Transient
                    );

                servicesCollection.AddSingleton<HubConnectionWrapper>();

                servicesCollection.AddScoped<IRepository, Repository>();

                ServiceProvider servicesProvider = servicesCollection.BuildServiceProvider();

                servicesProvider.GetRequiredService<HubConnectionWrapper>().Start();
                Log.Logger.Information("Оформлено подключение к хабу.");

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