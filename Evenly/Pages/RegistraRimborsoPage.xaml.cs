using Evenly.Models;
using Evenly.Services;

namespace Evenly.Pages;

public partial class RegistraRimborsoPage : ContentPage
{
    private readonly ApiService _api;
    private readonly GruppoResponse _gruppo;

    public RegistraRimborsoPage(ApiService api, GruppoResponse gruppo)
    {
        InitializeComponent();
        _api = api;
        _gruppo = gruppo;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CaricaBilancio();
    }

    private async Task CaricaBilancio()
    {
        try
        {
            var bilancio = await _api.GetBilancioAsync(_gruppo.GroupId);
            var mieiDebiti = bilancio?.VociBilancio
                .Where(v => v.DebitoreUsername == _api.CurrentUsername)
                .ToList() ?? new();
            TransazioniCollection.ItemsSource = mieiDebiti;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", ex.Message, "OK");
        }
    }

    private async void OnTransazioneSelezionata(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not VoceBilancioResponse voce) return;
        TransazioniCollection.SelectedItem = null;

        bool confirm = await DisplayAlert(
            "Conferma rimborso",
            $"{voce.DebitoreUsername} rimborsa {voce.Importo:F2} € a {voce.CreditoreUsername}.\n\nProcedere?",
            "Rimborsa", "Annulla");
        if (!confirm) return;

        ErrorLabel.IsVisible = false;
        try
        {
            await _api.RegistraRimborsoAsync(_gruppo.GroupId,
                new RegistraRimborsoRequest(voce.DebitoreUsername, voce.CreditoreUsername, voce.Importo));
            await CaricaBilancio();
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
    }
}
