using Evenly.Services;

namespace Evenly.Pages;

public partial class AccediGruppoPage : ContentPage
{
    private readonly ApiService _api;

    public AccediGruppoPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void OnIncollaIdClicked(object sender, EventArgs e)
    {
        var testo = await Clipboard.Default.GetTextAsync();
        if (!string.IsNullOrEmpty(testo)) GroupIdEntry.Text = testo;
    }

    private async void OnAccediClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        var groupIdStr = GroupIdEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (!Guid.TryParse(groupIdStr, out var groupId) || string.IsNullOrWhiteSpace(password))
        {
            ErrorLabel.Text = "Inserisci un GroupID valido e la password.";
            ErrorLabel.IsVisible = true;
            return;
        }

        AccediBtn.IsEnabled = false;
        try
        {
            await _api.AccediGruppoAsync(groupId, password);
            await DisplayAlert("Successo", "Accesso al gruppo effettuato!", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            AccediBtn.IsEnabled = true;
        }
    }

    private async void OnIncollaTokenClicked(object sender, EventArgs e)
    {
        var testo = await Clipboard.Default.GetTextAsync();
        if (!string.IsNullOrEmpty(testo)) TokenEntry.Text = testo;
    }

    private async void OnAccediConLinkClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        var token = TokenEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            ErrorLabel.Text = "Inserisci il token di invito.";
            ErrorLabel.IsVisible = true;
            return;
        }

        AccediLinkBtn.IsEnabled = false;
        try
        {
            await _api.AccediConLinkAsync(token);
            await DisplayAlert("Successo", "Accesso al gruppo effettuato!", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            AccediLinkBtn.IsEnabled = true;
        }
    }
}
