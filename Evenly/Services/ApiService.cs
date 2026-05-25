using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Evenly.Models;

namespace Evenly.Services;

public class ApiService
{
#if ANDROID
    // Emulatore Android: 10.0.2.2 è l'alias dell'host (localhost del PC).
    // Su dispositivo reale nella stessa rete, sostituisci con l'IP del PC (es. http://192.168.1.X:5054).
    private const string ServerUrl = "http://10.0.2.2:5054";
#elif IOS
    // Simulatore iOS: condivide la rete dell'host, localhost funziona.
    // Su dispositivo reale, sostituisci con l'IP del PC (es. http://192.168.1.X:5054).
    private const string ServerUrl = "http://localhost:5054";
#else
    private const string ServerUrl = "http://localhost:5054";
#endif

    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri(ServerUrl)
    };

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private string? _token;

    public bool IsLoggedIn => _token != null;
    public string? CurrentUsername { get; private set; }
    public string? CurrentNome { get; private set; }

    public void SetToken(string token, string username, string nome)
    {
        _token = token;
        CurrentUsername = username;
        CurrentNome = nome;
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public void Logout()
    {
        _token = null;
        CurrentUsername = null;
        CurrentNome = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    // ─── Utenti ─────────────────────────────────────────────────────────────

    public async Task<UtenteResponse?> RegistraAsync(RegistrazioneRequest req)
    {
        var resp = await _http.PostAsJsonAsync("/api/utenti/registra", req);
        if (!resp.IsSuccessStatusCode) throw await ParseError(resp);
        return await resp.Content.ReadFromJsonAsync<UtenteResponse>(_json);
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest req)
    {
        var resp = await _http.PostAsJsonAsync("/api/utenti/login", req);
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new Exception("Credenziali errate.");
        if ((int)resp.StatusCode == 423)
            throw new Exception("Account bloccato per troppi tentativi falliti.");
        if (!resp.IsSuccessStatusCode) throw await ParseError(resp);
        return await resp.Content.ReadFromJsonAsync<LoginResponse>(_json);
    }

    // ─── Gruppi ─────────────────────────────────────────────────────────────

    public async Task<List<GruppoResponse>> GetGruppiAsync()
    {
        var resp = await _http.GetAsync("/api/gruppi");
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<GruppoResponse>>(_json)
               ?? new List<GruppoResponse>();
    }

    public async Task<GruppoResponse?> CreaGruppoAsync(CreaGruppoRequest req)
    {
        var resp = await _http.PostAsJsonAsync("/api/gruppi", req);
        if (!resp.IsSuccessStatusCode) throw await ParseError(resp);
        return await resp.Content.ReadFromJsonAsync<GruppoResponse>(_json);
    }

    public async Task AccediGruppoAsync(Guid groupId, string password)
    {
        var resp = await _http.PostAsJsonAsync($"/api/gruppi/{groupId}/accedi",
            new AccediGruppoRequest(password));
        if (!resp.IsSuccessStatusCode) throw await ParseError(resp);
    }

    public async Task RimuoviMembroAsync(Guid groupId, string username)
    {
        var resp = await _http.DeleteAsync($"/api/gruppi/{groupId}/partecipanti/{username}");
        if (!resp.IsSuccessStatusCode) throw await ParseError(resp);
    }

    public async Task AbbandonaGruppoAsync(Guid groupId)
    {
        var resp = await _http.DeleteAsync($"/api/gruppi/{groupId}/partecipanti/me");
        if (!resp.IsSuccessStatusCode) throw await ParseError(resp);
    }

    public async Task AccediConLinkAsync(string token)
    {
        var resp = await _http.PostAsJsonAsync("/api/gruppi/accedi-link",
            new { Token = token });
        if (!resp.IsSuccessStatusCode) throw await ParseError(resp);
    }

    public async Task<LinkInvitoResponse?> GeneraLinkInvitoAsync(Guid groupId)
    {
        var resp = await _http.PostAsync($"/api/gruppi/{groupId}/link-invito", null);
        if (!resp.IsSuccessStatusCode) throw await ParseError(resp);
        return await resp.Content.ReadFromJsonAsync<LinkInvitoResponse>(_json);
    }

    public async Task<List<PartecipazioneResponse>> GetPartecipantiAsync(Guid groupId)
    {
        var resp = await _http.GetAsync($"/api/gruppi/{groupId}/partecipanti");
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<PartecipazioneResponse>>(_json)
               ?? new List<PartecipazioneResponse>();
    }

    // ─── Profilo ────────────────────────────────────────────────────────────

    public async Task<UtenteResponse?> GetProfiloAsync()
    {
        var resp = await _http.GetAsync("/api/utenti/me");
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<UtenteResponse>(_json);
    }

    public async Task CambiaPasswordAsync(string vecchiaPassword, string nuovaPassword)
    {
        var resp = await _http.PutAsJsonAsync("/api/utenti/me/password",
            new CambiaPasswordRequest(vecchiaPassword, nuovaPassword));
        if (!resp.IsSuccessStatusCode) throw await ParseError(resp);
    }

    public async Task<List<VoceLogCompletaResponse>> GetTuttiLogAsync()
    {
        var resp = await _http.GetAsync("/api/sicurezza/log");
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<VoceLogCompletaResponse>>(_json)
               ?? new List<VoceLogCompletaResponse>();
    }

    public async Task<List<UtenteAdminResponse>> GetTuttiUtentiAsync()
    {
        var resp = await _http.GetAsync("/api/sicurezza/utenti");
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<UtenteAdminResponse>>(_json)
               ?? new List<UtenteAdminResponse>();
    }

    public async Task ModificaProfiloAsync(string? nome, string? cognome, string? email, string? valuta)
    {
        var resp = await _http.PutAsJsonAsync("/api/utenti/me",
            new ModificaProfiloRequest(nome, cognome, email, valuta));
        if (!resp.IsSuccessStatusCode) throw await ParseError(resp);
    }

    public async Task EliminaAccountAsync()
    {
        var resp = await _http.DeleteAsync("/api/utenti/me");
        if (!resp.IsSuccessStatusCode) throw await ParseError(resp);
    }

    // ─── Spese ──────────────────────────────────────────────────────────────

    public async Task<List<SpesaResponse>> GetSpeseAsync(Guid groupId)
    {
        var resp = await _http.GetAsync($"/api/gruppi/{groupId}/spese");
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<SpesaResponse>>(_json)
               ?? new List<SpesaResponse>();
    }

    public async Task<SpesaResponse?> InserisciSpesaAsync(Guid groupId, InserisciSpesaRequest req)
    {
        var resp = await _http.PostAsJsonAsync($"/api/gruppi/{groupId}/spese", req);
        if (!resp.IsSuccessStatusCode) throw await ParseError(resp);
        return await resp.Content.ReadFromJsonAsync<SpesaResponse>(_json);
    }

    public async Task EliminaSpesaAsync(Guid groupId, Guid spesaId)
    {
        var resp = await _http.DeleteAsync($"/api/gruppi/{groupId}/spese/{spesaId}");
        if (!resp.IsSuccessStatusCode) throw await ParseError(resp);
    }

    // ─── Bilancio ───────────────────────────────────────────────────────────

    public async Task<BilancioResponse?> GetBilancioAsync(Guid groupId)
    {
        var resp = await _http.GetAsync($"/api/gruppi/{groupId}/bilancio");
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<BilancioResponse>(_json);
    }

    // ─── Rimborsi ───────────────────────────────────────────────────────────

    public async Task<RimborsoResponse?> RegistraRimborsoAsync(Guid groupId,
        RegistraRimborsoRequest req)
    {
        var resp = await _http.PostAsJsonAsync($"/api/gruppi/{groupId}/rimborsi", req);
        if (!resp.IsSuccessStatusCode) throw await ParseError(resp);
        return await resp.Content.ReadFromJsonAsync<RimborsoResponse>(_json);
    }

    // ─── Helper ─────────────────────────────────────────────────────────────

    private static async Task<Exception> ParseError(HttpResponseMessage resp)
    {
        try
        {
            var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>(_json);
            return new Exception(err?.Messaggio ?? resp.ReasonPhrase);
        }
        catch
        {
            return new Exception($"Errore {(int)resp.StatusCode}: {resp.ReasonPhrase}");
        }
    }
}
