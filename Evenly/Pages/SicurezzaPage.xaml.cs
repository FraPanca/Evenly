using Evenly.Services;

namespace Evenly.Pages;

public partial class GestoreSicurezzaPage : ContentPage
{
    private readonly ApiService _api;

    public GestoreSicurezzaPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CaricaDati();
    }

    private async Task CaricaDati()
    {
        try
        {
            var utentiTask = _api.GetTuttiUtentiAsync();
            var logTask = _api.GetTuttiLogAsync();
            await Task.WhenAll(utentiTask, logTask);

            UtentiCollection.ItemsSource = utentiTask.Result;
            LogCollection.ItemsSource = logTask.Result;
            LogCountLabel.Text = $"{logTask.Result.Count} voci (ultime 200)";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", ex.Message, "OK");
        }
    }

    private async void OnAggiornaDatiClicked(object sender, EventArgs e)
    {
        await CaricaDati();
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        _api.Logout();
        Application.Current!.MainPage = new NavigationPage(new LoginPage(_api));
    }
}
