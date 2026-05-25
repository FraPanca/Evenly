using System.Globalization;
using Evenly.Models;
using Evenly.Services;

namespace Evenly.Pages;

public partial class InserisciSpesaPage : ContentPage
{
    private readonly ApiService _api;
    private readonly GruppoResponse _gruppo;
    private List<PartecipanteVM> _partecipanti = new();
    private readonly Dictionary<string, Entry> _parametriEntries = new();

    public InserisciSpesaPage(ApiService api, GruppoResponse gruppo)
    {
        InitializeComponent();
        _api = api;
        _gruppo = gruppo;
        DataPicker.Date = DateTime.Today;
        MetodoPicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var partecipanti = await _api.GetPartecipantiAsync(_gruppo.GroupId);
            _partecipanti = partecipanti.Select(p => new PartecipanteVM
            {
                Username = p.Username,
                IsSelected = true
            }).ToList();
            PartecipantiCollection.ItemsSource = _partecipanti;

            if (ParametriPanel.IsVisible)
                AggiornaParametriPanel();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", ex.Message, "OK");
        }
    }

    private void OnMetodoChanged(object sender, EventArgs e)
    {
        bool mostraParametri = MetodoPicker.SelectedIndex > 0;
        ParametriLabel.IsVisible = mostraParametri;
        ParametriPanel.IsVisible = mostraParametri;
        if (mostraParametri)
            AggiornaParametriPanel();
    }

    private void AggiornaParametriPanel()
    {
        ParametriPanel.Children.Clear();
        _parametriEntries.Clear();

        bool isPercentuale = MetodoPicker.SelectedIndex == 2;
        ParametriLabel.Text = isPercentuale
            ? "Percentuale (%) per partecipante:"
            : "Importo per partecipante:";

        foreach (var p in _partecipanti)
        {
            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection(
                    new ColumnDefinition(new GridLength(130)),
                    new ColumnDefinition(GridLength.Star)),
                Margin = new Thickness(0, 2)
            };
            row.Add(new Label { Text = p.Username, VerticalOptions = LayoutOptions.Center, FontSize = 14 }, 0, 0);
            var entry = new Entry { Placeholder = isPercentuale ? "0" : "0.00", Keyboard = Keyboard.Numeric };
            row.Add(entry, 1, 0);
            ParametriPanel.Children.Add(row);
            _parametriEntries[p.Username] = entry;
        }
    }

    private async void OnSalvaClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        var causale = CausaleEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(causale))
        {
            ErrorLabel.Text = "La causale è obbligatoria.";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (!decimal.TryParse(ImportoEntry.Text?.Replace(",", "."),
            NumberStyles.Any, CultureInfo.InvariantCulture, out var importo) || importo <= 0)
        {
            ErrorLabel.Text = "Inserisci un importo valido.";
            ErrorLabel.IsVisible = true;
            return;
        }

        var selezionati = _partecipanti.Where(p => p.IsSelected)
            .Select(p => p.Username).ToList();
        if (selezionati.Count < 2)
        {
            ErrorLabel.Text = "Seleziona almeno 2 partecipanti.";
            ErrorLabel.IsVisible = true;
            return;
        }

        var metodo = MetodoPicker.SelectedIndex switch
        {
            1 => "IMPORTI_ESATTI",
            2 => "PERCENTUALE",
            _ => "EQUA"
        };

        Dictionary<string, decimal>? parametri = null;
        if (MetodoPicker.SelectedIndex > 0)
        {
            parametri = new Dictionary<string, decimal>();
            foreach (var u in selezionati)
            {
                if (!_parametriEntries.TryGetValue(u, out var entry) ||
                    !decimal.TryParse(entry.Text?.Replace(",", "."),
                        NumberStyles.Any, CultureInfo.InvariantCulture, out var val) || val < 0)
                {
                    ErrorLabel.Text = $"Inserisci un valore valido per {u}.";
                    ErrorLabel.IsVisible = true;
                    return;
                }
                parametri[u] = val;
            }

            if (MetodoPicker.SelectedIndex == 2)
            {
                var somma = parametri.Values.Sum();
                if (Math.Abs(somma - 100m) > 0.01m)
                {
                    ErrorLabel.Text = $"Le percentuali devono sommare a 100 (attuale: {somma:F2}).";
                    ErrorLabel.IsVisible = true;
                    return;
                }
            }
        }

        SalvaBtn.IsEnabled = false;
        try
        {
            var note = NoteEditor.Text?.Trim();
            await _api.InserisciSpesaAsync(_gruppo.GroupId, new InserisciSpesaRequest(
                causale, importo, DataPicker.Date ?? DateTime.Today,
                selezionati, metodo, parametri, null,
                string.IsNullOrEmpty(note) ? null : note));
            await DisplayAlert("Successo", "Spesa registrata!", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            SalvaBtn.IsEnabled = true;
        }
    }
}

public class PartecipanteVM
{
    public string Username { get; set; } = "";
    public bool IsSelected { get; set; } = true;
}
