using Evenly.Pages;
using Evenly.Services;

namespace Evenly;

public partial class App : Application
{
    private readonly ApiService _api;

    public App(ApiService api)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
        _api = api;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var nav = new NavigationPage(new LoginPage(_api))
        {
            BarBackgroundColor = Color.FromArgb("#10192A"),
            BarTextColor     = Color.FromArgb("#E8F0FE")
        };
        return new Window(nav);
    }
}
