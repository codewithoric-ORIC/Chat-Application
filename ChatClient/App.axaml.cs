using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ChatClient.Views;

namespace ChatClient;

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
            // ဒီနေရာမှာ MainWindow ကို ChatClient.Views.MainWindow အနေနဲ့ သုံးပါ
            desktop.MainWindow = new MainWindow
            {
                // ...
            };
        }

        base.OnFrameworkInitializationCompleted();

    }
}