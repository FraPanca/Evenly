# Aspetti Importanti del Codice C#

---

## 1. Architettura Generale

Il sistema segue il pattern **ECB (Entity-Control-Boundary)** su architettura **3-tier**:

```
[Client MAUI]  ←→  [Server ASP.NET Core]  ←→  [SQL Server LocalDB]
   Boundary           Controller + Entity           Persistenza
```

- **Entity** (`Evenly.Server/Models/`): classi di dominio puro (Utente, Gruppo, Spesa, …)
- **Controller** (`Evenly.Server/Controllers/`): logica di business, validazione, accesso DB
- **Boundary** (`Evenly.Server/Program.cs` endpoints + `Evenly/Pages/`): API REST e UI MAUI

---

## 2. Pattern Strategy — Divisione delle Spese

**File:** `Evenly.Server/Strategies/`

Permette di selezionare a runtime l'algoritmo di divisione senza modificare il codice chiamante.

### Interfaccia

```csharp
public interface IStrategiaDivisione
{
    List<QuotaSpesa> CalcolaQuote(Spesa spesa, List<Utente> partecipanti,
        Dictionary<Utente, decimal>? parametri = null);
}
```

### Implementazioni concrete

```csharp
// Divide equamente: importo / n, arrotonda il primo per compensare il resto
public class DivisioneEqua : IStrategiaDivisione { ... }

// Somma percentuali deve essere 100 ± 0.01
public class DivisionePercentuale : IStrategiaDivisione { ... }

// Somma importi esatti deve essere == importo spesa ± 0.01
public class DivisioneImportiEsatti : IStrategiaDivisione { ... }
```

### Factory (in ControllerSpese)

```csharp
private static IStrategiaDivisione ScegliStrategia(MetodoDivisione metodo) => metodo switch
{
    MetodoDivisione.EQUA            => new DivisioneEqua(),
    MetodoDivisione.PERCENTUALE     => new DivisionePercentuale(),
    MetodoDivisione.IMPORTI_ESATTI  => new DivisioneImportiEsatti(),
    _                               => throw new ArgumentOutOfRangeException()
};
```

### Utilizzo

```csharp
var strategia = ScegliStrategia(spesa.GetMetodoDivisione());
var quote = strategia.CalcolaQuote(spesa, partecipanti, parametri);
```

---

## 3. Pattern Observer — Notifiche Push

**File:** `Evenly.Server/Services/IServizioNotifichePush.cs` + `ServizioNotifichePushMock.cs`

Disaccoppia chi genera eventi (Controller) da chi li gestisce (servizio notifiche).

### Interfaccia Observer

```csharp
public interface IServizioNotifichePush
{
    void NotificaInserimentoSpesa(Spesa spesa, List<Utente> destinatari);
    void NotificaModificaSpesa(Spesa spesa, List<Utente> destinatari);
    void NotificaEliminazioneSpesa(Guid spesaId, Gruppo gruppo, List<Utente> destinatari);
    void NotificaRimborso(Rimborso rimborso, List<Utente> destinatari);
}
```

### Implementazione Mock (Stub)

```csharp
public class ServizioNotifichePushMock : IServizioNotifichePush
{
    public void NotificaInserimentoSpesa(Spesa spesa, List<Utente> destinatari)
        => Console.WriteLine($"[NOTIFICA] Nuova spesa '{spesa.GetCausale()}' per {destinatari.Count} utenti");
    // ... analogamente per gli altri metodi
}
```

### Registrazione tramite Dependency Injection (Program.cs)

```csharp
builder.Services.AddSingleton<IServizioNotifichePush, ServizioNotifichePushMock>();
```

### Utilizzo nei Controller

```csharp
// In ControllerSpese.InserisciSpesa()
var altriPartecipanti = partecipanti.Where(u => u.GetUsername() != pagante.GetUsername()).ToList();
_notifiche.NotificaInserimentoSpesa(spesa, altriPartecipanti);

// In ControllerRimborsi.RegistraRimborso()
_notifiche.NotificaRimborso(rimborso, new List<Utente> { creditore });
```

Per passare a notifiche reali (es. Firebase FCM) basta creare una nuova classe
`ServizioNotifichePushFcm : IServizioNotifichePush` e cambiare la riga `AddSingleton`.

---

## 4. Accesso al Database — ADO.NET

Il progetto usa **ADO.NET raw** (non Entity Framework) con `SqlConnection` e `SqlCommand`.

### Classe base ControllerPersistenza

Tutti i controller del server ereditano da `ControllerPersistenza`, che fornisce:

```csharp
protected SqlConnection GetConnection()
{
    var conn = new SqlConnection(_connString);
    conn.Open();
    return conn;
}

protected void ScriviLog(string operazione, string utente, string dettaglio)
{
    var voce = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] | {operazione} | {utente} | {dettaglio}";
    File.AppendAllText(_logPath, voce + Environment.NewLine);
}
```

### Pattern di accesso tipico

```csharp
using var conn = GetConnection();
using var cmd = new SqlCommand(
    "SELECT Username, Nome FROM UTENTE WHERE Username = @u", conn);
cmd.Parameters.AddWithValue("@u", username);   // sempre parametrizzato (anti SQL injection)
using var reader = cmd.ExecuteReader();
if (reader.Read())
    return Utente.FromDb(reader);
```

### Transazioni per operazioni multi-step

```csharp
using var conn = GetConnection();
using var tx = conn.BeginTransaction();
try
{
    // es. cancella partecipazione + azzera saldo + soft-delete gruppo
    var cmd1 = new SqlCommand("DELETE FROM PARTECIPAZIONE WHERE ...", conn, tx);
    cmd1.ExecuteNonQuery();
    var cmd2 = new SqlCommand("DELETE FROM SALDO WHERE ...", conn, tx);
    cmd2.ExecuteNonQuery();
    tx.Commit();
}
catch
{
    tx.Rollback();
    throw;
}
```

Usate in: `AbbandonaGruppo`, `AggiungiPartecipante`, `InserisciSpesa`, `RicalcolaSaldi`.

---

## 5. Autenticazione JWT

**File:** `Evenly.Server/Program.cs`

### Generazione token

```csharp
static string GeneraToken(string username, string jwtKey, string jwtIssuer)
{
    var handler = new JwtSecurityTokenHandler();
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var token = handler.CreateToken(new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }),
        Expires = DateTime.UtcNow.AddHours(24),
        Issuer  = jwtIssuer,
        SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
    });
    return handler.WriteToken(token);
}
```

- Algoritmo: **HMAC-SHA256**
- Durata token: **24 ore**
- Claim: `ClaimTypes.Name = username`

### Protezione degli endpoint

```csharp
app.MapGet("/api/utenti/me", (HttpContext ctx, ...) => { ... })
   .RequireAuthorization();

// Estrazione username dal token dentro l'endpoint:
string username = ctx.User.Identity!.Name!;
```

### Client — invio token nelle richieste

```csharp
// In ApiService.cs (client MAUI)
_http.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", _token);
```

---

## 6. Sicurezza delle Password

**File:** `Evenly.Server/Models/Utente.cs`

### Hashing PBKDF2

```csharp
public static string HashPassword(string password)
{
    byte[] salt = RandomNumberGenerator.GetBytes(16);
    byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
        password, salt, iterations: 100_000,
        HashAlgorithmName.SHA256, outputLength: 32);
    return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
}
```

- Salt: **16 byte** casuali per ogni password
- Iterazioni: **100.000** (resistenza brute force)
- Algoritmo: **SHA-256**

### Verifica constant-time

```csharp
public static bool VerifyPassword(string password, string storedHash)
{
    var parts = storedHash.Split(':');
    byte[] salt = Convert.FromBase64String(parts[0]);
    byte[] expectedHash = Convert.FromBase64String(parts[1]);
    byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
        password, salt, 100_000, HashAlgorithmName.SHA256, 32);
    return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    // FixedTimeEquals evita timing attacks
}
```

---

## 7. Protezione Brute Force

**File:** `Evenly.Server/Controllers/ControllerAutenticazione.cs`

```csharp
private readonly Dictionary<string, int> _tentativiFalliti = new();

public Utente Autentica(string username, string password)
{
    _tentativiFalliti.TryGetValue(username, out int tentativi);
    if (tentativi >= 3)
        throw new UnauthorizedAccessException("Account temporaneamente bloccato");

    var utente = CaricaDaDb(username);
    if (utente == null || !utente.VerificaPassword(password))
    {
        _tentativiFalliti[username] = tentativi + 1;
        ScriviLog("LOGIN_FALLITO", username, $"Tentativo {tentativi + 1}/3");
        throw new UnauthorizedAccessException("Credenziali non valide");
    }

    _tentativiFalliti.Remove(username);   // reset su successo
    ScriviLog("LOGIN", username, "Accesso effettuato");
    return utente;
}
```

---

## 8. Algoritmo Minimizzazione Transazioni

**File:** `Evenly.Server/Controllers/ControllerBilancio.cs`

Algoritmo **greedy** che minimizza il numero di rimborsi necessari.

```csharp
public List<VoceBilancio> CalcolaBilancio(Dictionary<string, decimal> saldi)
{
    // Separa creditori (saldo > 0) e debitori (saldo < 0)
    var creditori = saldi.Where(s => s.Value > 0.005m)
                         .OrderByDescending(s => s.Value).ToList();
    var debitori  = saldi.Where(s => s.Value < -0.005m)
                         .OrderBy(s => s.Value).ToList();
    var voci = new List<VoceBilancio>();

    int i = 0, j = 0;
    while (i < creditori.Count && j < debitori.Count)
    {
        decimal trasferimento = Math.Min(creditori[i].Value, -debitori[j].Value);
        voci.Add(new VoceBilancio(debitori[j].Key, creditori[i].Key, trasferimento));

        creditori[i] = new(creditori[i].Key, creditori[i].Value - trasferimento);
        debitori[j]  = new(debitori[j].Key,  debitori[j].Value  + trasferimento);

        if (Math.Abs(creditori[i].Value) < 0.005m) i++;
        if (Math.Abs(debitori[j].Value)  < 0.005m) j++;
    }
    return voci;
}
```

---

## 9. Factory Method — Costruzione da DB

Tutti i modelli di dominio usano un factory method `FromDb()` che separa la
logica di idratazione dall'`SqlDataReader`.

```csharp
public static Utente FromDb(SqlDataReader r) => new Utente(
    username:            r["Username"].ToString()!,
    nome:                r["Nome"].ToString()!,
    cognome:             r["Cognome"].ToString()!,
    email:               r["Email"].ToString()!,
    passwordHash:        r["PasswordHash"].ToString()!,    // già hashata
    valutaPredefinita:   r["ValutaPredefinita"].ToString()!,
    deviceTokenPush:     r["DeviceTokenPush"] as string,
    stato:               Enum.Parse<StatoAccount>(r["Stato"].ToString()!)
);
```

Stesso pattern in: `Gruppo.FromDb()`, `Spesa.FromDb()`, `Rimborso.FromDb()`, `Saldo.FromDb()`.

---

## 10. Lazy Loading delle Quote

**File:** `Evenly.Server/Controllers/ControllerSpese.cs`

Le `QuotaSpesa` di una spesa non vengono caricate automaticamente con la spesa.
La lista viene popolata solo quando esplicitamente richiesta:

```csharp
private List<QuotaSpesa> CaricaQuote(Guid spesaId, Dictionary<string, Utente> utenti)
{
    using var conn = GetConnection();
    using var cmd  = new SqlCommand(
        "SELECT * FROM QUOTA_SPESA WHERE SpesaId = @id", conn);
    cmd.Parameters.AddWithValue("@id", spesaId);
    // ... legge e costruisce lista QuotaSpesa
}

// Chiamato solo in GetSpeseGruppo() dopo aver caricato la spesa principale
spesa.SetQuote(CaricaQuote(spesa.GetSpesaId(), utentiCache));
```

---

## 11. Ricalcolo Saldi (Idempotente)

**File:** `Evenly.Server/Controllers/ControllerSpese.cs`

Anziché aggiornare i saldi in modo incrementale (rischio inconsistenza),
il sistema può eseguire un **ricalcolo completo** da zero:

```csharp
public void RicalcolaSaldi(Guid groupId)
{
    // 1. Azzera tutti i saldi del gruppo
    using var conn = GetConnection();
    new SqlCommand("UPDATE SALDO SET Valore = 0 WHERE GruppoId = @g", conn)
        { ... }.ExecuteNonQuery();

    // 2. Riapplica tutte le spese
    foreach (var spesa in GetSpeseGruppo(groupId))
        AggiornaSaldi(spesa, conn, tx: null);

    // 3. Riapplica tutti i rimborsi
    foreach (var rimborso in GetRimborsiGruppo(groupId))
        AggiornaPerRimborso(rimborso, conn, tx: null);
}
```

Usato dopo `EliminaSpesa` per garantire la consistenza.

---

## 12. Mock nei Test

**File:** `Evenly.Tests/`

I test unitari usano le classi di dominio reali (no mock del DB).
Le strategie di divisione vengono testate direttamente:

```csharp
[Test]
public void DivisioneEqua_DuePersone_QuoteUguali()
{
    var strategia = new DivisioneEqua();
    var spesa = new Spesa(..., importo: 90m, ...);
    var partecipanti = new List<Utente> { utente1, utente2 };

    var quote = strategia.CalcolaQuote(spesa, partecipanti);

    Assert.AreEqual(2, quote.Count);
    Assert.AreEqual(90m, quote.Sum(q => q.GetImporto()), 0.01m);
}

[Test]
public void DivisionePercentuale_SommaErrataLanciaEccezione()
{
    var strategia = new DivisionePercentuale();
    var parametri = new Dictionary<Utente, decimal>
        { [utente1] = 60m, [utente2] = 30m };  // somma 90, non 100

    Assert.Throws<ArgumentException>(() =>
        strategia.CalcolaQuote(spesa, partecipanti, parametri));
}
```

Per `IServizioNotifichePush` i test usano il `ServizioNotifichePushMock`
già fornito, iniettato tramite costruttore (DI manuale).

---

## 13. Supporto Multi-Piattaforma — Direttive del Preprocessore

**File:** `Evenly/Services/ApiService.cs`

Il client MAUI compila per Windows, Android e iOS dallo stesso codice sorgente.
Le differenze di rete tra piattaforme vengono gestite con costanti selezionate
a **compile-time** tramite direttive `#if`, senza alcun overhead a runtime:

```csharp
#if ANDROID
    // L'emulatore Android usa 10.0.2.2 come alias del localhost del PC host.
    // Su dispositivo reale: sostituire con l'IP LAN del PC (es. 192.168.1.X).
    private const string ServerUrl = "http://10.0.2.2:5054";
#elif IOS
    // Il simulatore iOS condivide la rete del Mac, localhost funziona direttamente.
    // Su dispositivo reale: sostituire con l'IP LAN del Mac.
    private const string ServerUrl = "http://localhost:5054";
#else
    // Windows desktop: connessione diretta a localhost.
    private const string ServerUrl = "http://localhost:5054";
#endif

private static readonly HttpClient _http = new()
{
    BaseAddress = new Uri(ServerUrl)
};
```

### Configurazione sicurezza Android — `network_security_config.xml`

Android API 28+ blocca il traffico HTTP cleartext per default. Il file
`Platforms/Android/Resources/xml/network_security_config.xml` dichiara
un'eccezione esplicita per il server di sviluppo:

```xml
<network-security-config>
    <domain-config cleartextTrafficPermitted="true">
        <domain includeSubdomains="false">10.0.2.2</domain>
    </domain-config>
</network-security-config>
```

Referenziato dall'`AndroidManifest.xml` tramite:
```xml
android:networkSecurityConfig="@xml/network_security_config"
```

### Configurazione sicurezza iOS — `Info.plist`

iOS blocca HTTP tramite App Transport Security (ATS). L'eccezione per
`localhost` è dichiarata in `Platforms/iOS/Info.plist`:

```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSExceptionDomains</key>
    <dict>
        <key>localhost</key>
        <dict>
            <key>NSExceptionAllowsInsecureHTTPLoads</key>
            <true/>
        </dict>
    </dict>
</dict>
```

---

## 14. Log di Sistema

**Formato file** `logs/evenly.log`:

```
[2026-05-25 14:30:00] | REGISTRAZIONE | mario | Nuovo account creato
[2026-05-25 14:31:05] | LOGIN | mario | Accesso effettuato
[2026-05-25 14:32:10] | LOGIN_FALLITO | hacker | Tentativo 1/3
[2026-05-25 14:35:00] | INSERISCI_SPESA | mario | Cena 120.00 EUR in gruppo VacanzaRoma
[2026-05-25 14:40:00] | RIMBORSO | luigi | 30.00 EUR -> mario in gruppo VacanzaRoma
```

Scritto da `ScriviLog()` (in `ControllerPersistenza`) dopo ogni operazione rilevante.
Letto dal `ControllerSicurezza` e disponibile nella Dashboard Gestore.
