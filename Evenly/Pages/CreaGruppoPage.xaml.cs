using Evenly.Models;
using Evenly.Services;

namespace Evenly.Pages;

public partial class CreaGruppoPage : ContentPage
{
    private readonly ApiService _api;

    public CreaGruppoPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        ValutaPicker.SelectedIndex = 0;
    }

    private async void OnCreaClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        var nome = NomeEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        var valuta = ValutaPicker.SelectedItem?.ToString() ?? "EUR";

        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(password))
        {
            ErrorLabel.Text = "Compila tutti i campi.";
            ErrorLabel.IsVisible = true;
            return;
        }

        CreaBtn.IsEnabled = false;
        try
        {
            await _api.CreaGruppoAsync(new CreaGruppoRequest(nome, password, valuta));
            await DisplayAlert("Successo", $"Gruppo '{nome}' creato!", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            CreaBtn.IsEnabled = true;
        }
    }
}
