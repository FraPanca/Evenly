using Evenly.Models;
using Evenly.Services;

namespace Evenly.Pages;

public partial class GruppoPage : ContentPage
{
    private readonly ApiService _api;
    private readonly GruppoResponse _gruppo;
    private List<PartecipazioneResponse> _partecipanti = new();
    private ToolbarItem? _membriItem;

    public GruppoPage(ApiService api, GruppoResponse gruppo)
    {
        InitializeComponent();
        _api = api;
        _gruppo = gruppo;
        Title = gruppo.GroupName;
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
            var speseTask    = _api.GetSpeseAsync(_gruppo.GroupId);
            var bilancioTask = _api.GetBilancioAsync(_gruppo.GroupId);
            var partTask     = _api.GetPartecipantiAsync(_gruppo.GroupId);
            await Task.WhenAll(speseTask, bilancioTask, partTask);

            SpeseCollection.ItemsSource = speseTask.Result;
            _partecipanti = partTask.Result;

            var bilancio = bilancioTask.Result;
            if (bilancio != null)
            {
                BilancioCollection.ItemsSource = bilancio.VociBilancio;
                SaldiCollection.ItemsSource = bilancio.Saldi.Select(s => new SaldoViewModel
                {
                    Username   = s.Username,
                    Valore     = s.Valore,
                    StatoColore = s.Stato switch
                    {
                        "CREDITO" => "#78D7C1",
                        "DEBITO"  => "#F87171",
                        _         => "#525B69"
                    }
                }).ToList();
            }

            // Mostra "Membri" nella toolbar solo all'amministratore
            bool isAdmin = _partecipanti.Any(p =>
                p.Username == _api.CurrentUsername && p.Ruolo == "AMMINISTRATORE");

            if (isAdmin && _membriItem == null)
            {
                _membriItem = new ToolbarItem { Text = "Membri" };
                _membriItem.Clicked += OnGestioneMembriClicked;
                ToolbarItems.Add(_membriItem);
            }
            else if (!isAdmin && _membriItem != null)
            {
                ToolbarItems.Remove(_membriItem);
                _membriItem = null;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", ex.Message, "OK");
        }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await CaricaDati();
        RefreshView.IsRefreshing = false;
    }

    private async void OnNuovaSpesaClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new InserisciSpesaPage(_api, _gruppo));
    }

    private async void OnRimborsoClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegistraRimborsoPage(_api, _gruppo));
    }

    private async void OnLasciaGruppoClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Abbandona gruppo",
            $"Vuoi abbandonare '{_gruppo.GroupName}'? Non puoi farlo se hai un saldo non nullo.",
            "Abbandona", "Annulla");
        if (!confirm) return;
        try
        {
            await _api.AbbandonaGruppoAsync(_gruppo.GroupId);
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", ex.Message, "OK");
        }
    }

    // Tap su una spesa → pagina di dettaglio
    private async void OnSpesaSelezionata(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not SpesaResponse spesa) return;
        SpeseCollection.SelectedItem = null;
        await Navigation.PushAsync(new DettaglioSpesaPage(_api, _gruppo, spesa));
    }

    private async void OnGestioneMembriClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new GestioneMembriPage(_api, _gruppo, _partecipanti));
    }

    private async void OnCondividiClicked(object sender, EventArgs e)
    {
        var scelta = await DisplayActionSheet(
            $"Condividi — {_gruppo.GroupName}",
            "Annulla", null,
            "Copia ID gruppo",
            "Genera link di invito");

        if (scelta == "Copia ID gruppo")
        {
            await Clipboard.Default.SetTextAsync(_gruppo.GroupId.ToString());
            await DisplayAlert("Copiato", $"ID gruppo copiato:\n{_gruppo.GroupId}", "OK");
        }
        else if (scelta == "Genera link di invito")
        {
            try
            {
                var link = await _api.GeneraLinkInvitoAsync(_gruppo.GroupId);
                if (link == null) return;
                string testo = link.Token;
                await Clipboard.Default.SetTextAsync(testo);
                await DisplayAlert("Link di invito generato",
                    $"Token copiato negli appunti:\n\n{testo}\n\n" +
                    $"Scade: {link.DataScadenza:dd/MM/yyyy HH:mm}\n" +
                    $"Utilizzi rimasti: {link.UtilizziResidui}",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", ex.Message, "OK");
            }
        }
    }
}

public class SaldoViewModel
{
    public string Username    { get; set; } = "";
    public decimal Valore     { get; set; }
    public string StatoColore { get; set; } = "#525B69";
}
