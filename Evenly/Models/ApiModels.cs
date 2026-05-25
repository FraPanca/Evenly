namespace Evenly.Models;

// ─── Requests ───────────────────────────────────────────────────────────────

public record RegistrazioneRequest(
    string Username, string Nome, string Cognome,
    string Email, string Password, string ValutaPredefinita = "EUR");

public record LoginRequest(string Username, string Password);

public record CreaGruppoRequest(string GroupName, string Password, string ValutaRiferimento = "EUR");

public record AccediGruppoRequest(string Password);

public record ModificaProfiloRequest(
    string? Nome = null, string? Cognome = null,
    string? Email = null, string? ValutaPredefinita = null);

public record InserisciSpesaRequest(
    string Causale, decimal Importo, DateTime Data,
    List<string> PartecipantiUsernames,
    string MetodoDivisione = "EQUA",
    Dictionary<string, decimal>? Parametri = null,
    string? Valuta = null, string? Note = null);

public record RegistraRimborsoRequest(
    string DebitoreUsername, string CreditoreUsername, decimal Importo);

// ─── Responses ──────────────────────────────────────────────────────────────

public record UtenteResponse(string Username, string Nome, string Cognome,
    string Email, string ValutaPredefinita);

public record LoginResponse(string Username, string Nome, string Token);

public record GruppoResponse(Guid GroupId, string GroupName,
    string ValutaRiferimento, int NumeroPartecipanti);

public record QuotaResponse(string Username, decimal Importo);

public record SpesaResponse(Guid SpesaId, string Causale, decimal Importo,
    DateTime Data, string PaganteUsername, string MetodoDivisione,
    string Valuta, string? Note, List<QuotaResponse> Quote);

public record SaldoResponse(string Username, decimal Valore, string Stato, string Valuta);

public record VoceBilancioResponse(string CreditoreUsername, string DebitoreUsername, decimal Importo);

public record BilancioResponse(List<VoceBilancioResponse> VociBilancio, List<SaldoResponse> Saldi);

public record RimborsoResponse(Guid RimborsoId, string DebitoreUsername,
    string CreditoreUsername, decimal Importo, DateTime Data);

public record PartecipazioneResponse(string Username, string Nome, string Cognome, string Ruolo);

public record LinkInvitoResponse(Guid LinkId, string Token, DateTime DataScadenza, int UtilizziResidui);

public record CambiaPasswordRequest(string VecchiaPassword, string NuovaPassword);

public record VoceLogResponse(DateTime Timestamp, string Operazione, string Dettaglio);

public record VoceLogCompletaResponse(DateTime Timestamp, string Operazione, string Utente, string Dettaglio);

public record UtenteAdminResponse(string Username, string Nome, string Cognome, string Email, string Stato);

public record ErrorResponse(string Messaggio);
