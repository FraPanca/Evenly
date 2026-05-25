using Evenly.Models;
using Evenly.Services;

namespace Evenly.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _api;

    public LoginPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        var username = UsernameEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorLabel.Text = "Inserisci username e password.";
            ErrorLabel.IsVisible = true;
            return;
        }

        LoginBtn.IsEnabled = false;
        try
        {
            var resp = await _api.LoginAsync(new LoginRequest(username, password));
            if (resp != null)
            {
                _api.SetToken(resp.Token, resp.Username, resp.Nome);
                ContentPage home = resp.Username == "gestore"
                    ? new GestoreSicurezzaPage(_api)
                    : new HomePage(_api);
                Application.Current!.MainPage = new NavigationPage(home);
            }
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoginBtn.IsEnabled = true;
        }
    }

    private async void OnRegistratiClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegistrazionePage(_api));
    }
}
