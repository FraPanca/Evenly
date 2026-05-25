using Evenly.Models;
using Evenly.Services;

namespace Evenly.Pages;

public partial class GestioneMembriPage : ContentPage
{
    private readonly ApiService _api;
    private readonly GruppoResponse _gruppo;
    private List<MembroVM> _membri;

    public GestioneMembriPage(ApiService api, GruppoResponse gruppo,
        List<PartecipazioneResponse> partecipanti)
    {
        InitializeComponent();
        _api = api;
        _gruppo = gruppo;
        _membri = partecipanti.Select(p => new MembroVM
        {
            Username = p.Username,
            Nome = p.Nome,
            Cognome = p.Cognome,
            Ruolo = p.Ruolo,
            PuoEssereRimosso = p.Ruolo != "AMMINISTRATORE"
                               && p.Username != api.CurrentUsername
        }).ToList();
        MembriCollection.ItemsSource = _membri;
    }

    private async void OnRimuoviClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string username) return;
        ErrorLabel.IsVisible = false;

        bool confirm = await DisplayAlert("Rimuovi membro",
            $"Rimuovere '{username}' dal gruppo?\nIl suo saldo deve essere nullo.",
            "Rimuovi", "Annulla");
        if (!confirm) return;

        try
        {
            await _api.RimuoviMembroAsync(_gruppo.GroupId, username);
            _membri = _membri.Where(m => m.Username != username).ToList();
            MembriCollection.ItemsSource = null;
            MembriCollection.ItemsSource = _membri;
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
    }
}

public class MembroVM
{
    public string Username { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Cognome { get; set; } = "";
    public string Ruolo { get; set; } = "";
    public bool PuoEssereRimosso { get; set; }
    public string RuoloBadge => Ruolo == "AMMINISTRATORE" ? "Admin" : "Membro";
    public string BadgeColor => Ruolo == "AMMINISTRATORE" ? "#6E88ED" : "#78D7C1";
}
