using Evenly.Services;

namespace Evenly.Pages;

public partial class CambiaPasswordPage : ContentPage
{
    private readonly ApiService _api;

    public CambiaPasswordPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void OnCambiaPasswordClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        SuccessLabel.IsVisible = false;

        var vecchia = VecchiaPasswordEntry.Text;
        var nuova = NuovaPasswordEntry.Text;
        var conferma = ConfermaPasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(vecchia) || string.IsNullOrWhiteSpace(nuova))
        {
            ErrorLabel.Text = "Compila tutti i campi.";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (nuova != conferma)
        {
            ErrorLabel.Text = "Le nuove password non corrispondono.";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (nuova.Length < 6)
        {
            ErrorLabel.Text = "La nuova password deve avere almeno 6 caratteri.";
            ErrorLabel.IsVisible = true;
            return;
        }

        CambiaBtn.IsEnabled = false;
        try
        {
            await _api.CambiaPasswordAsync(vecchia, nuova);
            VecchiaPasswordEntry.Text = "";
            NuovaPasswordEntry.Text = "";
            ConfermaPasswordEntry.Text = "";
            SuccessLabel.Text = "Password cambiata con successo.";
            SuccessLabel.IsVisible = true;
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            CambiaBtn.IsEnabled = true;
        }
    }
}
