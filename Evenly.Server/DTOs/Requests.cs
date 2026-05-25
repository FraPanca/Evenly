namespace Evenly.Server.DTOs;

public record RegistrazioneRequest(
    string Username,
    string Nome,
    string Cognome,
    string Email,
    string Password,
    string ValutaPredefinita = "EUR"
);

public record LoginRequest(string Username, string Password);

public record CreaGruppoRequest(
    string GroupName,
    string Password,
    string ValutaRiferimento = "EUR"
);

public record AccediGruppoRequest(string Password);

public record AccediConLinkRequest(string Token);

public record InserisciSpesaRequest(
    string Causale,
    decimal Importo,
    DateTime Data,
    List<string> PartecipantiUsernames,
    string MetodoDivisione = "EQUA",
    Dictionary<string, decimal>? Parametri = null,
    string? Valuta = null,
    string? Note = null
);

public record RegistraRimborsoRequest(
    string DebitoreUsername,
    string CreditoreUsername,
    decimal Importo
);

public record ModificaDatiRequest(
    string? Nome = null,
    string? Cognome = null,
    string? Email = null,
    string? ValutaPredefinita = null
);

public record AggiornaDipositivoRequest(string? DeviceToken);

public record CambiaPasswordRequest(string VecchiaPassword, string NuovaPassword);
