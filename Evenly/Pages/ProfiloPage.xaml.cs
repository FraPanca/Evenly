using Evenly.Services;

namespace Evenly.Pages;

public partial class ProfiloPage : ContentPage
{
    private readonly ApiService _api;

    public ProfiloPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        UsernameLabel.Text = _api.CurrentUsername;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var profilo = await _api.GetProfiloAsync();
            if (profilo == null) return;
            NomeEntry.Text = profilo.Nome;
            CognomeEntry.Text = profilo.Cognome;
            EmailEntry.Text = profilo.Email;
            ValutaEntry.Text = profilo.ValutaPredefinita;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", ex.Message, "OK");
        }
    }

    private async void OnCambiaPasswordClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CambiaPasswordPage(_api));
    }

    private async void OnSalvaClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        try
        {
            await _api.ModificaProfiloAsync(
                NomeEntry.Text, CognomeEntry.Text,
                EmailEntry.Text, ValutaEntry.Text);
            await DisplayAlert("Successo", "Profilo aggiornato.", "OK");
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
    }

    private async void OnEliminaAccountClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Elimina account",
            "Sei sicuro? L'operazione è irreversibile. Non puoi eliminare l'account se hai saldi non nulli in un gruppo.",
            "Elimina", "Annulla");
        if (!confirm) return;
        try
        {
            await _api.EliminaAccountAsync();
            _api.Logout();
            Application.Current!.MainPage = new NavigationPage(new LoginPage(_api));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", ex.Message, "OK");
        }
    }
}
