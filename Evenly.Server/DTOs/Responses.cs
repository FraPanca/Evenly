namespace Evenly.Server.DTOs;

public record UtenteResponse(string Username, string Nome, string Cognome, string Email, string ValutaPredefinita);

public record LoginResponse(string Username, string Nome, string Token);

public record GruppoResponse(Guid GroupId, string GroupName, string ValutaRiferimento, int NumeroPartecipanti);

public record PartecipazioneResponse(string Username, string Nome, string Cognome, string Ruolo);

public record QuotaResponse(string Username, decimal Importo);

public record SpesaResponse(
    Guid SpesaId,
    string Causale,
    decimal Importo,
    DateTime Data,
    string PaganteUsername,
    string MetodoDivisione,
    string Valuta,
    string? Note,
    List<QuotaResponse> Quote
);

public record SaldoResponse(string Username, decimal Valore, string Stato, string Valuta);

public record VoceBilancioResponse(string CreditoreUsername, string DebitoreUsername, decimal Importo);

public record BilancioResponse(List<VoceBilancioResponse> VociBilancio, List<SaldoResponse> Saldi);

public record RimborsoResponse(Guid RimborsoId, string DebitoreUsername, string CreditoreUsername, decimal Importo, DateTime Data);

public record LinkInvitoResponse(Guid LinkId, string Token, DateTime DataScadenza, int UtilizziResidui);

public record VoceLogResponse(DateTime Timestamp, string Operazione, string Dettaglio);

public record VoceLogCompletaResponse(DateTime Timestamp, string Operazione, string Utente, string Dettaglio);

public record UtenteAdminResponse(string Username, string Nome, string Cognome, string Email, string Stato);

public record ErrorResponse(string Messaggio);
