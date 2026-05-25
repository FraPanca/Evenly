using Evenly.Models;
using Evenly.Services;

namespace Evenly.Pages;

public partial class DettaglioSpesaPage : ContentPage
{
    private readonly ApiService _api;
    private readonly GruppoResponse _gruppo;
    private readonly SpesaResponse _spesa;

    public DettaglioSpesaPage(ApiService api, GruppoResponse gruppo, SpesaResponse spesa)
    {
        InitializeComponent();
        _api = api;
        _gruppo = gruppo;
        _spesa = spesa;

        Title = spesa.Causale;

        ImportoLabel.Text = $"{spesa.Importo:F2} {spesa.Valuta}";
        PaganteLabel.Text = $"Pagato da {spesa.PaganteUsername}";
        DataLabel.Text = spesa.Data.ToString("dd/MM/yyyy");
        MetodoLabel.Text = spesa.MetodoDivisione switch
        {
            "EQUA"           => "Divisione equa",
            "IMPORTI_ESATTI" => "Importi esatti",
            "PERCENTUALE"    => "Percentuale",
            _                => spesa.MetodoDivisione
        };

        if (!string.IsNullOrWhiteSpace(spesa.Note))
        {
            NoteContainer.IsVisible = true;
            NoteLabel.Text = spesa.Note;
        }

        QuoteCollection.ItemsSource = spesa.Quote;

        if (spesa.PaganteUsername == _api.CurrentUsername)
            EliminaBtn.IsVisible = true;
    }

    private async void OnEliminaClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Elimina spesa",
            $"Eliminare '{_spesa.Causale}'?", "Elimina", "Annulla");
        if (!confirm) return;
        try
        {
            await _api.EliminaSpesaAsync(_gruppo.GroupId, _spesa.SpesaId);
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", ex.Message, "OK");
        }
    }
}
