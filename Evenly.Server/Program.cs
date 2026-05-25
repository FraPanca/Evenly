using Evenly.Server.Domain;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Evenly.Server.BusinessControllers;
using Evenly.Server.DTOs;
using Evenly.Server.Enums;
using Evenly.Server.Observer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connString = builder.Configuration.GetConnectionString("EvenlyDB")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=EvenlyDB;Trusted_Connection=True;MultipleActiveResultSets=true";

var logPath = builder.Configuration["LogPath"]
    ?? Path.Combine(AppContext.BaseDirectory, "logs", "evenly.log");

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? "EvenlySecretKey2025!ChangeInProduction!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Evenly";

// Services
builder.Services.AddSingleton<IServizioNotifichePush>(sp =>
    new ServizioNotifichePushMock(sp.GetService<ILogger<ServizioNotifichePushMock>>()));

builder.Services.AddSingleton(sp => new ControllerRegistrazione(connString, logPath));
builder.Services.AddSingleton(sp => new ControllerAutenticazione(connString, logPath));
builder.Services.AddSingleton(sp => new ControllerGestioneAccount(connString, logPath));
builder.Services.AddSingleton(sp => new ControllerGruppo(connString, logPath));
builder.Services.AddSingleton(sp => new ControllerAccessoGruppo(connString, logPath,
    sp.GetRequiredService<ControllerGruppo>()));
builder.Services.AddSingleton(sp => new ControllerBilancio(connString, logPath));
builder.Services.AddSingleton(sp => new ControllerSpese(connString, logPath,
    sp.GetRequiredService<IServizioNotifichePush>()));
builder.Services.AddSingleton(sp => new ControllerSicurezza(connString, logPath));
builder.Services.AddSingleton(sp => new ControllerRimborsi(connString, logPath,
    sp.GetRequiredService<IServizioNotifichePush>(),
    sp.GetRequiredService<ControllerBilancio>()));

// JWT Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddPolicy("AllowAll",
    p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddHostedService<DatabaseInitService>(sp =>
    new DatabaseInitService(connString,
        sp.GetRequiredService<ILogger<DatabaseInitService>>()));

var app = builder.Build();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// ─── HELPER ────────────────────────────────────────────────────────────────

static string GeneraToken(string username, string jwtKey, string jwtIssuer)
{
    var handler = new JwtSecurityTokenHandler();
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var token = handler.CreateToken(new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }),
        Expires = DateTime.UtcNow.AddHours(24),
        Issuer = jwtIssuer,
        SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
    });
    return handler.WriteToken(token);
}

static string GetUsername(ClaimsPrincipal user) =>
    user.FindFirst(ClaimTypes.Name)?.Value
    ?? throw new UnauthorizedAccessException("Token non valido.");

// ─── UTENTI ────────────────────────────────────────────────────────────────

app.MapPost("/api/utenti/registra", (RegistrazioneRequest req,
    ControllerRegistrazione ctrl) =>
{
    try
    {
        var utente = ctrl.RegistraUtente(req.Username, req.Nome, req.Cognome,
            req.Email, req.Password, req.ValutaPredefinita);
        return Results.Ok(new UtenteResponse(utente.GetUsername(), utente.GetNome(),
            utente.GetCognome(), utente.GetEmail(), utente.GetValutaPredefinita()));
    }
    catch (InvalidOperationException ex) { return Results.Conflict(new ErrorResponse(ex.Message)); }
    catch (ArgumentException ex) { return Results.BadRequest(new ErrorResponse(ex.Message)); }
});

app.MapPost("/api/utenti/login", (LoginRequest req,
    ControllerAutenticazione ctrl) =>
{
    try
    {
        var utente = ctrl.VerificaCredenziali(req.Username, req.Password);
        if (utente == null) return Results.Unauthorized();
        var token = GeneraToken(utente.GetUsername(), jwtKey, jwtIssuer);
        return Results.Ok(new LoginResponse(utente.GetUsername(), utente.GetNome(), token));
    }
    catch (UnauthorizedAccessException ex) { return Results.Json(new ErrorResponse(ex.Message), statusCode: 423); }
});

app.MapGet("/api/utenti/me", (ClaimsPrincipal user, ControllerGestioneAccount ctrl) =>
{
    try
    {
        var username = GetUsername(user);
        var u = ctrl.GetUtente(username);
        return Results.Ok(new UtenteResponse(u.GetUsername(), u.GetNome(),
            u.GetCognome(), u.GetEmail(), u.GetValutaPredefinita()));
    }
    catch (Exception ex) { return Results.BadRequest(new ErrorResponse(ex.Message)); }
}).RequireAuthorization();

app.MapPut("/api/utenti/me", (ModificaDatiRequest req,
    ClaimsPrincipal user, ControllerGestioneAccount ctrl) =>
{
    try
    {
        var username = GetUsername(user);
        ctrl.ModificaDati(username, req.Nome, req.Cognome, req.Email, req.ValutaPredefinita, username);
        return Results.NoContent();
    }
    catch (UnauthorizedAccessException) { return Results.Forbid(); }
    catch (Exception ex) { return Results.BadRequest(new ErrorResponse(ex.Message)); }
}).RequireAuthorization();

app.MapDelete("/api/utenti/me", (ClaimsPrincipal user, ControllerGestioneAccount ctrl) =>
{
    try
    {
        var username = GetUsername(user);
        ctrl.EliminaAccount(username, username);
        return Results.NoContent();
    }
    catch (InvalidOperationException ex) { return Results.Conflict(new ErrorResponse(ex.Message)); }
}).RequireAuthorization();

// ─── GRUPPI ────────────────────────────────────────────────────────────────

app.MapGet("/api/gruppi", (ClaimsPrincipal user, ControllerGruppo ctrl) =>
{
    var username = GetUsername(user);
    var gruppi = ctrl.GetGruppiUtenteConPartecipanti(username);
    return Results.Ok(gruppi.Select(g => new GruppoResponse(
        g.Gruppo.GetGroupId(), g.Gruppo.GetGroupName(),
        g.Gruppo.GetValutaRiferimento(), g.NumeroPartecipanti)));
}).RequireAuthorization();

app.MapPost("/api/gruppi", (CreaGruppoRequest req,
    ClaimsPrincipal user, ControllerGruppo ctrlGruppo,
    ControllerGestioneAccount ctrlAccount) =>
{
    try
    {
        var username = GetUsername(user);
        var utente = ctrlAccount.GetUtente(username);
        var gruppo = ctrlGruppo.CreaGruppo(req.GroupName, req.Password, req.ValutaRiferimento, utente);
        return Results.Ok(new GruppoResponse(gruppo.GetGroupId(), gruppo.GetGroupName(),
            gruppo.GetValutaRiferimento(), 1));
    }
    catch (ArgumentException ex) { return Results.BadRequest(new ErrorResponse(ex.Message)); }
}).RequireAuthorization();

app.MapGet("/api/gruppi/{groupId:guid}", (Guid groupId,
    ClaimsPrincipal user, ControllerGruppo ctrl, ControllerBilancio ctrlBilancio) =>
{
    try
    {
        var gruppo = ctrl.GetGruppo(groupId);
        var utentiGruppo = ctrlBilancio.GetUtentiGruppo(groupId);
        return Results.Ok(new GruppoResponse(gruppo.GetGroupId(), gruppo.GetGroupName(),
            gruppo.GetValutaRiferimento(), utentiGruppo.Count));
    }
    catch (KeyNotFoundException) { return Results.NotFound(); }
}).RequireAuthorization();

app.MapPost("/api/gruppi/{groupId:guid}/accedi", (Guid groupId,
    AccediGruppoRequest req, ClaimsPrincipal user,
    ControllerAccessoGruppo ctrl, ControllerGestioneAccount ctrlAccount) =>
{
    try
    {
        var username = GetUsername(user);
        var utente = ctrlAccount.GetUtente(username);
        ctrl.AccediAlGruppo(utente, groupId, req.Password);
        return Results.Ok();
    }
    catch (UnauthorizedAccessException) { return Results.Forbid(); }
    catch (InvalidOperationException ex) { return Results.Conflict(new ErrorResponse(ex.Message)); }
}).RequireAuthorization();

app.MapDelete("/api/gruppi/{groupId:guid}/partecipanti/me", (Guid groupId,
    ClaimsPrincipal user, ControllerAccessoGruppo ctrl,
    ControllerGruppo ctrlGruppo, ControllerGestioneAccount ctrlAccount) =>
{
    try
    {
        var username = GetUsername(user);
        var utente = ctrlAccount.GetUtente(username);
        var gruppo = ctrlGruppo.GetGruppo(groupId);
        ctrl.AbbandonaGruppo(utente, gruppo);
        return Results.NoContent();
    }
    catch (InvalidOperationException ex) { return Results.Conflict(new ErrorResponse(ex.Message)); }
}).RequireAuthorization();

app.MapPost("/api/gruppi/{groupId:guid}/link-invito", (Guid groupId,
    ClaimsPrincipal user, ControllerAccessoGruppo ctrl,
    ControllerGruppo ctrlGruppo, ControllerGestioneAccount ctrlAccount) =>
{
    try
    {
        var username = GetUsername(user);
        var utente = ctrlAccount.GetUtente(username);
        var gruppo = ctrlGruppo.GetGruppo(groupId);
        var link = ctrl.GeneraLinkInvito(gruppo, utente);
        return Results.Ok(new LinkInvitoResponse(link.GetLinkId(), link.GetToken(),
            link.GetDataScadenza(), link.GetUtilizziResidui()));
    }
    catch (UnauthorizedAccessException) { return Results.Forbid(); }
}).RequireAuthorization();

app.MapPost("/api/gruppi/accedi-link", (AccediConLinkRequest req,
    ClaimsPrincipal user, ControllerAccessoGruppo ctrl,
    ControllerGestioneAccount ctrlAccount) =>
{
    try
    {
        var username = GetUsername(user);
        var utente = ctrlAccount.GetUtente(username);
        ctrl.AccediConToken(utente, req.Token);
        return Results.Ok();
    }
    catch (InvalidOperationException ex) { return Results.Conflict(new ErrorResponse(ex.Message)); }
}).RequireAuthorization();

app.MapDelete("/api/gruppi/{groupId:guid}/partecipanti/{username}", (Guid groupId,
    string username, ClaimsPrincipal user, ControllerAccessoGruppo ctrl) =>
{
    try
    {
        var adminUsername = GetUsername(user);
        ctrl.RimuoviMembro(adminUsername, groupId, username);
        return Results.NoContent();
    }
    catch (UnauthorizedAccessException) { return Results.Forbid(); }
    catch (KeyNotFoundException) { return Results.NotFound(); }
    catch (InvalidOperationException ex) { return Results.Conflict(new ErrorResponse(ex.Message)); }
}).RequireAuthorization();

app.MapGet("/api/gruppi/{groupId:guid}/partecipanti", (Guid groupId,
    ClaimsPrincipal user, ControllerBilancio ctrl) =>
{
    var partecipanti = ctrl.GetPartecipantiConRuolo(groupId);
    return Results.Ok(partecipanti.Select(p =>
        new PartecipazioneResponse(p.Utente.GetUsername(), p.Utente.GetNome(),
            p.Utente.GetCognome(), p.Ruolo)));
}).RequireAuthorization();

// ─── SPESE ─────────────────────────────────────────────────────────────────

app.MapGet("/api/gruppi/{groupId:guid}/spese", (Guid groupId,
    ClaimsPrincipal user, ControllerSpese ctrl,
    ControllerGruppo ctrlGruppo, ControllerBilancio ctrlBilancio) =>
{
    try
    {
        var gruppo = ctrlGruppo.GetGruppo(groupId);
        var utenti = ctrlBilancio.GetUtentiGruppo(groupId);
        var spese = ctrl.GetSpeseGruppo(groupId, utenti, gruppo);
        return Results.Ok(spese.Select(s => new SpesaResponse(
            s.GetSpesaId(), s.GetCausale(), s.GetImporto(), s.GetData(),
            s.GetPagante().GetUsername(), s.GetMetodoDivisione().ToString(),
            s.GetValuta(), s.GetNote(),
            s.GetQuote().Select(q => new QuotaResponse(q.GetUtente().GetUsername(), q.GetImporto())).ToList()
        )));
    }
    catch (KeyNotFoundException) { return Results.NotFound(); }
}).RequireAuthorization();

app.MapPost("/api/gruppi/{groupId:guid}/spese", (Guid groupId,
    InserisciSpesaRequest req, ClaimsPrincipal user,
    ControllerSpese ctrl, ControllerGruppo ctrlGruppo,
    ControllerGestioneAccount ctrlAccount, ControllerBilancio ctrlBilancio) =>
{
    try
    {
        var username = GetUsername(user);
        var gruppo = ctrlGruppo.GetGruppo(groupId);
        var utentiGruppo = ctrlBilancio.GetUtentiGruppo(groupId);
        var pagante = ctrlAccount.GetUtente(username);

        var partecipanti = req.PartecipantiUsernames
            .Select(u => utentiGruppo.TryGetValue(u, out var ut) ? ut : null)
            .Where(u => u != null).Cast<Evenly.Server.Domain.Utente>().ToList();

        Dictionary<Evenly.Server.Domain.Utente, decimal>? parametri = null;
        if (req.Parametri != null)
            parametri = req.Parametri
                .Where(kv => utentiGruppo.ContainsKey(kv.Key))
                .ToDictionary(kv => utentiGruppo[kv.Key], kv => kv.Value);

        var metodo = Enum.Parse<MetodoDivisione>(req.MetodoDivisione, ignoreCase: true);
        var spesa = ctrl.InserisciSpesa(req.Causale, req.Importo, req.Data,
            pagante, partecipanti, metodo, parametri, gruppo);

        return Results.Ok(new SpesaResponse(
            spesa.GetSpesaId(), spesa.GetCausale(), spesa.GetImporto(), spesa.GetData(),
            spesa.GetPagante().GetUsername(), spesa.GetMetodoDivisione().ToString(),
            spesa.GetValuta(), spesa.GetNote(),
            spesa.GetQuote().Select(q => new QuotaResponse(q.GetUtente().GetUsername(), q.GetImporto())).ToList()
        ));
    }
    catch (ArgumentException ex) { return Results.BadRequest(new ErrorResponse(ex.Message)); }
    catch (InvalidOperationException ex) { return Results.Conflict(new ErrorResponse(ex.Message)); }
}).RequireAuthorization();

app.MapDelete("/api/gruppi/{groupId:guid}/spese/{spesaId:guid}", (Guid groupId, Guid spesaId,
    ClaimsPrincipal user, ControllerSpese ctrl) =>
{
    try
    {
        var username = GetUsername(user);
        ctrl.EliminaSpesaById(spesaId, groupId, username);
        return Results.NoContent();
    }
    catch (KeyNotFoundException) { return Results.NotFound(); }
    catch (UnauthorizedAccessException) { return Results.Forbid(); }
}).RequireAuthorization();

// ─── BILANCIO ──────────────────────────────────────────────────────────────

app.MapGet("/api/gruppi/{groupId:guid}/bilancio", (Guid groupId,
    ClaimsPrincipal user, ControllerBilancio ctrl, ControllerGruppo ctrlGruppo) =>
{
    try
    {
        var gruppo = ctrlGruppo.GetGruppo(groupId);
        var utenti = ctrl.GetUtentiGruppo(groupId);
        var bilancio = ctrl.CalcolaBilancio(gruppo, utenti);
        var saldi = ctrl.GetSaldiGruppo(groupId, utenti);

        return Results.Ok(new BilancioResponse(
            bilancio.GetVociBilancio().Select(v => new VoceBilancioResponse(
                v.GetCreditore().GetUsername(),
                v.GetDebitore().GetUsername(),
                v.GetImporto())).ToList(),
            saldi.Select(s => new SaldoResponse(
                s.GetUtente().GetUsername(), s.GetValore(),
                s.GetStato().ToString(), s.GetValuta())).ToList()
        ));
    }
    catch (KeyNotFoundException) { return Results.NotFound(); }
}).RequireAuthorization();

// ─── RIMBORSI ──────────────────────────────────────────────────────────────

app.MapGet("/api/gruppi/{groupId:guid}/rimborsi", (Guid groupId,
    ClaimsPrincipal user, ControllerRimborsi ctrl, ControllerBilancio ctrlBilancio) =>
{
    var utenti = ctrlBilancio.GetUtentiGruppo(groupId);
    var rimborsi = ctrl.GetRimborsiGruppo(groupId, utenti);
    return Results.Ok(rimborsi.Select(r => new RimborsoResponse(
        r.GetRimborsoId(), r.GetDebitore().GetUsername(),
        r.GetCreditore().GetUsername(), r.GetImporto(), r.GetData())));
}).RequireAuthorization();

app.MapPost("/api/gruppi/{groupId:guid}/rimborsi", (Guid groupId,
    RegistraRimborsoRequest req, ClaimsPrincipal user,
    ControllerRimborsi ctrl, ControllerGruppo ctrlGruppo,
    ControllerGestioneAccount ctrlAccount) =>
{
    try
    {
        var gruppo = ctrlGruppo.GetGruppo(groupId);
        var debitore = ctrlAccount.GetUtente(req.DebitoreUsername);
        var creditore = ctrlAccount.GetUtente(req.CreditoreUsername);
        var rimborso = ctrl.RegistraRimborso(debitore, creditore, req.Importo, gruppo);
        return Results.Ok(new RimborsoResponse(
            rimborso.GetRimborsoId(), rimborso.GetDebitore().GetUsername(),
            rimborso.GetCreditore().GetUsername(), rimborso.GetImporto(), rimborso.GetData()));
    }
    catch (ArgumentException ex) { return Results.BadRequest(new ErrorResponse(ex.Message)); }
}).RequireAuthorization();

// ─── SICUREZZA ─────────────────────────────────────────────────────────────

app.MapPut("/api/utenti/me/password", (CambiaPasswordRequest req,
    ClaimsPrincipal user, ControllerSicurezza ctrl) =>
{
    try
    {
        var username = GetUsername(user);
        ctrl.CambiaPassword(username, req.VecchiaPassword, req.NuovaPassword);
        return Results.NoContent();
    }
    catch (UnauthorizedAccessException ex) { return Results.Conflict(new ErrorResponse(ex.Message)); }
    catch (ArgumentException ex) { return Results.BadRequest(new ErrorResponse(ex.Message)); }
    catch (KeyNotFoundException) { return Results.NotFound(); }
}).RequireAuthorization();

app.MapGet("/api/sicurezza/log", (ClaimsPrincipal user, ControllerSicurezza ctrl) =>
{
    if (GetUsername(user) != "gestore") return Results.Forbid();
    var log = ctrl.GetTuttiLog();
    return Results.Ok(log.Select(v =>
        new VoceLogCompletaResponse(v.Timestamp, v.Operazione, v.Utente, v.Dettaglio)));
}).RequireAuthorization();

app.MapGet("/api/sicurezza/utenti", (ClaimsPrincipal user, ControllerSicurezza ctrl) =>
{
    if (GetUsername(user) != "gestore") return Results.Forbid();
    var utenti = ctrl.GetTuttiUtenti();
    return Results.Ok(utenti.Select(u =>
        new UtenteAdminResponse(u.Username, u.Nome, u.Cognome, u.Email, u.Stato)));
}).RequireAuthorization();

app.Run();

// ─── DATABASE INIT SERVICE ──────────────────────────────────────────────────

public class DatabaseInitService : IHostedService
{
    private readonly string _connString;
    private readonly ILogger<DatabaseInitService> _logger;

    public DatabaseInitService(string connString, ILogger<DatabaseInitService> logger)
    {
        _connString = connString;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken ct)
    {
        try
        {
            var sqlPath = Path.Combine(AppContext.BaseDirectory, "Database", "Init.sql");
            if (!File.Exists(sqlPath))
            {
                _logger.LogWarning("Init.sql non trovato in {Path}", sqlPath);
                return Task.CompletedTask;
            }

            string script = File.ReadAllText(sqlPath);
            var batches = script.Split(new[] { "\nGO\n", "\r\nGO\r\n", "\nGO\r\n", "\r\nGO\n" },
                StringSplitOptions.RemoveEmptyEntries);

            // Use master connection to create DB if needed
            var masterConnString = System.Text.RegularExpressions.Regex.Replace(
                _connString, @"(Database|Initial Catalog)=\w+", "$1=master",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            using var masterConn = new SqlConnection(masterConnString);
            masterConn.Open();

            foreach (var batch in batches)
            {
                var trimmed = batch.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                try
                {
                    using var cmd = new SqlCommand(trimmed, masterConn);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex) when (
                    ex.Message.Contains("already exists") ||
                    ex.Message.Contains("already an object"))
                {
                    // Ignore idempotent errors
                }
            }
            _logger.LogInformation("Database EvenlyDB inizializzato.");
            SeedGestoreSicurezza();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'inizializzazione del database.");
        }
        return Task.CompletedTask;
    }

    private void SeedGestoreSicurezza()
    {
        const string username = "gestore";
        const string password = "Sicurezza!2026";
        try
        {
            using var conn = new SqlConnection(_connString);
            conn.Open();
            string hash = Utente.HashPassword(password);
            using var cmd = new SqlCommand(
                @"IF EXISTS (SELECT 1 FROM UTENTE WHERE Username = @u)
                      UPDATE UTENTE SET PasswordHash = @h WHERE Username = @u
                  ELSE
                      INSERT INTO UTENTE (Username, Nome, Cognome, Email, PasswordHash, ValutaPredefinita, Stato)
                      VALUES (@u, 'Gestore', 'Sicurezza', 'gestore@evenly.local', @h, 'EUR', 'ATTIVO')", conn);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@h", hash);
            cmd.ExecuteNonQuery();
            _logger.LogInformation("Utente gestore sicurezza sincronizzato.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossibile sincronizzare il gestore sicurezza.");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
