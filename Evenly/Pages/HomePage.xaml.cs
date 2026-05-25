using Evenly.Models;
using Evenly.Services;

namespace Evenly.Pages;

public partial class HomePage : ContentPage
{
    private readonly ApiService _api;

    public HomePage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        BenvenutoLabel.Text = $"Ciao, {_api.CurrentNome}!";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CaricaGruppi();
    }

    private async Task CaricaGruppi()
    {
        try
        {
            var gruppi = await _api.GetGruppiAsync();
            GruppiCollection.ItemsSource = gruppi;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", ex.Message, "OK");
        }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await CaricaGruppi();
        RefreshView.IsRefreshing = false;
    }

    private async void OnGruppoSelezionato(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not GruppoResponse gruppo) return;
        GruppiCollection.SelectedItem = null;
        await Navigation.PushAsync(new GruppoPage(_api, gruppo));
    }

    private async void OnNuovoGruppoClicked(object sender, EventArgs e)
    {
        var action = await DisplayActionSheet("Gestione Gruppi", "Annulla", null,
            "Crea nuovo gruppo", "Accedi a gruppo esistente");

        if (action == "Crea nuovo gruppo")
            await Navigation.PushAsync(new CreaGruppoPage(_api));
        else if (action == "Accedi a gruppo esistente")
            await Navigation.PushAsync(new AccediGruppoPage(_api));
    }

    private async void OnProfiloClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProfiloPage(_api));
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        _api.Logout();
        Application.Current!.MainPage = new NavigationPage(new LoginPage(_api));
    }
}
