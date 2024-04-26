using Anecrypt.Avalonia.Services;
using Anecrypt.Avalonia.ViewModels;
using Anecrypt.Avalonia.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Anecrypt.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);

            MainWindow window = new MainWindow();

            ServiceCollection services = new ServiceCollection();

            services.AddScoped<MainWindowViewModel>();
            services.AddSingleton(x => new FileService(window.StorageProvider));

            Services = services.BuildServiceProvider();

            window.DataContext = Services.GetRequiredService<MainWindowViewModel>();

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public new static App? Current => Application.Current as App;

    public IServiceProvider? Services { get; private set; }
}