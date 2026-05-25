using Evenly.Models;
using Evenly.Services;

namespace Evenly.Pages;

public partial class RegistrazionePage : ContentPage
{
    private readonly ApiService _api;

    public RegistrazionePage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        ValutaPicker.SelectedIndex = 0; // EUR default
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        var username = UsernameEntry.Text?.Trim();
        var nome = NomeEntry.Text?.Trim();
        var cognome = CognomeEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        var valuta = ValutaPicker.SelectedItem?.ToString() ?? "EUR";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(nome) ||
            string.IsNullOrWhiteSpace(cognome) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            ErrorLabel.Text = "Compila tutti i campi.";
            ErrorLabel.IsVisible = true;
            return;
        }

        RegisterBtn.IsEnabled = false;
        try
        {
            await _api.RegistraAsync(new RegistrazioneRequest(
                username, nome, cognome, email, password, valuta));
            await DisplayAlert("Successo", "Account creato! Effettua il login.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            RegisterBtn.IsEnabled = true;
        }
    }
}
