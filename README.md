# Evenly

Applicazione client-server per la gestione di spese condivise tra gruppi di utenti — progetto universitario (Ingegneria del Software, Alma Mater Studiorum – Università di Bologna).

![C#](https://img.shields.io/badge/-C%23-239120?style=flat-square&logo=c-sharp&logoColor=white)
![.NET MAUI](https://img.shields.io/badge/-.NET%20MAUI-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/-ASP.NET%20Core-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/-SQL%20Server-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white)

---

## Italiano

### Abstract

Gestire le spese durante una vacanza tra amici può risultare complesso: chi paga cosa, chi anticipa una spesa, chi deve restituire cosa a chi. Evenly nasce per risolvere questo problema: tiene traccia in tempo reale del saldo di ciascun partecipante e, al termine dell'esperienza, calcola automaticamente il modo più efficiente per regolare i conti tra tutti, minimizzando il numero di transazioni necessarie.

**Stato**: primo prototipo funzionante — le funzionalità principali sono implementate, alcune funzionalità previste in fase di progettazione (conversione valuta, notifiche push reali) sono al momento stub/mock. Vedi [Limitazioni note](#limitazioni-note) più sotto.

### Team

**Gruppo 4 — A.A. 2025/2026**
- Francesco Brini
- Francesco Pancaldi
- Gaia Volta

### Documentazione di progetto

Il documento di progettazione completo (115 pagine, in 3 fasi — Analisi dei Requisiti, Analisi del Problema, Progettazione) è disponibile in [`docs/`](docs/).

### Architettura

Il sistema adotta un'architettura **Client/Server a 3 livelli**:

- **L1 — Client**: applicazione .NET MAUI, si occupa esclusivamente di presentazione e interazione utente. Nessuna logica di business.
- **L2 — Server**: server applicativo monolitico (ASP.NET Core Web API), espone la logica tramite endpoint REST, strutturato secondo il pattern **MVC/ECB** (Entity-Controller-Boundary).
- **L3 — Persistenza**: due server fisicamente separati — uno per il database applicativo (SQL Server), uno per il log delle operazioni — così che una compromissione dell'uno non intacchi l'altro.

Pattern applicati: **Strategy** (calcolo quote per metodo di divisione spesa), **Observer** (notifica dei partecipanti sugli eventi, tramite `IServizioNotifichePush` iniettata via DI).

### Stack tecnologico

| Livello | Tecnologia |
|---|---|
| Client | C# — .NET MAUI (Windows, Android, iOS, macCatalyst) |
| Server | ASP.NET Core Web API (REST, autenticazione JWT) |
| Database | SQL Server (LocalDB in sviluppo) |
| Log | File di log su server separato |
| Test | NUnit |

### Funzionalità implementate

**Account e autenticazione**
- Registrazione, login (JWT), modifica profilo, eliminazione account (con verifica saldo nullo)
- Protezione brute-force sul login (blocco dopo 3 tentativi falliti)

**Gruppi**
- Creazione, accesso tramite GroupID + password o link di invito (con scadenza per tempo/utilizzi)
- Ruolo amministratore automatico al creatore, gestione membri
- Abbandono gruppo con nomina automatica di un nuovo amministratore; eliminazione automatica del gruppo se resta vuoto

**Spese e saldi**
- Inserimento ed eliminazione spesa (min. 2 utenti nel gruppo)
- Divisione della spesa: equa, per importo esatto o per percentuale (Pattern Strategy)
- Calcolo automatico del saldo di ciascun utente e algoritmo greedy di minimizzazione delle transazioni per il pareggio conti
- Registrazione rimborsi con aggiornamento automatico dei saldi
- Storico transazioni e visualizzazione bilancio

**Sicurezza e amministrazione**
- Dashboard gestore per la sicurezza (log, utenti)
- Log di sistema su file, su server separato dal DB

### Limitazioni note

Rispetto al documento di progettazione, alcune funzionalità sono ancora **stub o non implementate** nel prototipo attuale:

- **Conversione valuta**: non implementata — le spese vengono registrate nella valuta inserita, senza conversione tramite API esterna
- **Notifiche push**: il servizio (`IServizioNotifichePush`) è implementato come mock che scrive su log/console, non invia notifiche reali al dispositivo
- **Ricerca gruppo dedicata**: non presente come schermata separata; l'accesso avviene tramite GroupID + password o link
- **HTTPS in sviluppo**: il client si connette tramite HTTP in locale; l'endpoint HTTPS è configurato ma non ancora utilizzato dal client
- **Rate limiting globale**: presente solo sul login, non su tutte le API
- **Modifica spesa**: implementata lato server, non ancora esposta nell'interfaccia del client

### Struttura del repository

```
Evenly.Server/   → backend ASP.NET Core Web API
Evenly/          → client .NET MAUI (Windows, Android, iOS, macCatalyst)
Evenly.Tests/    → test NUnit
docs/            → documento di progettazione (PDF, 3 fasi)
```

### Come eseguire il progetto

**Prerequisiti**: .NET SDK 10.0, SQL Server LocalDB, Visual Studio 2022 con workload MAUI + Android SDK.

> Avviare sempre il **server** prima del client.

```bash
# 1. Server (crea automaticamente il DB alla prima esecuzione)
dotnet run --project Evenly.Server
# → http://localhost:5054

# 2. Client Windows
dotnet run --project Evenly -f net10.0-windows10.0.19041.0

# 3. Client Android (richiede un emulatore avviato)
dotnet run --project Evenly -f net10.0-android

# Test
dotnet test Evenly.Tests
```

Per client iOS, uso su dispositivo fisico, configurazione di rete per piattaforma e risoluzione problemi comuni, vedi la guida completa: [`build_e_avvio.md`](build_e_avvio.md).

> ⚠️ **Nota**: le credenziali dell'account gestore usate in fase di sviluppo/test sono documentate in [`credenziali_gestore.md`](credenziali_gestore.md) — valide solo per l'ambiente locale, da sostituire prima di qualsiasi utilizzo pubblico o in produzione.

### Licenza

MIT — concordata tra tutti i membri del team (Gruppo 4).

---

## English

### Abstract

Managing shared expenses during a trip with friends can get complicated: who paid for what, who fronted an expense, who owes what to whom. Evenly was built to solve this: it tracks each participant's balance in real time and, at the end, automatically works out the most efficient way to settle up between everyone, minimizing the number of transactions required.

**Status**: first working prototype — core features are implemented; some features from the design phase (currency conversion, real push notifications) are currently stubbed/mocked. See [Known limitations](#known-limitations) below.

### Team

**Group 4 — Academic Year 2025/2026**
- Francesco Brini
- Francesco Pancaldi
- Gaia Volta

### Project documentation

The full design document (115 pages, across 3 phases — Requirements Analysis, Problem Analysis, Design) is available in [`docs/`](docs/).

### Architecture

The system follows a **3-tier Client/Server architecture**:

- **L1 — Client**: .NET MAUI application, responsible solely for presentation and user interaction. No business logic.
- **L2 — Server**: a monolithic application server (ASP.NET Core Web API), exposing the business logic through REST endpoints, structured according to the **MVC/ECB** pattern (Entity-Controller-Boundary).
- **L3 — Persistence**: two physically separate servers — one for the application database (SQL Server), one for the operations log — so that a compromise of one does not affect the other.

Design patterns used: **Strategy** (computing shares based on the expense-splitting method), **Observer** (notifying participants about events, via an `IServizioNotifichePush` interface injected through DI).

### Technology stack

| Layer | Technology |
|---|---|
| Client | C# — .NET MAUI (Windows, Android, iOS, macCatalyst) |
| Server | ASP.NET Core Web API (REST, JWT authentication) |
| Database | SQL Server (LocalDB in development) |
| Logging | File-based log on a separate server |
| Testing | NUnit |

### Implemented features

**Accounts and authentication**
- Registration, login (JWT), profile editing, account deletion (with a zero-balance check)
- Brute-force protection on login (lockout after 3 failed attempts)

**Groups**
- Creation, access via GroupID + password or an invite link (with time/usage-based expiry)
- Automatic administrator role for the creator, member management
- Leaving a group with automatic appointment of a new administrator; automatic group deletion if it becomes empty

**Expenses and balances**
- Adding and deleting expenses (min. 2 users in the group)
- Expense splitting: even, by exact amount, or by percentage (Strategy pattern)
- Automatic balance calculation for each user and a greedy algorithm minimizing the number of transactions needed to settle up
- Refund recording with automatic balance updates
- Transaction history and balance overview

**Security and administration**
- Manager dashboard for security (logs, users)
- System logging to file, on a server separate from the database

### Known limitations

Compared to the design document, some features are still **stubbed or not implemented** in the current prototype:

- **Currency conversion**: not implemented — expenses are recorded in the currency entered, with no conversion via an external API
- **Push notifications**: the service (`IServizioNotifichePush`) is implemented as a mock that writes to log/console, rather than sending real notifications to the device
- **Dedicated group search**: not available as a separate screen; access happens via GroupID + password or an invite link
- **HTTPS in development**: the client connects over HTTP locally; the HTTPS endpoint is configured but not yet used by the client
- **Global rate limiting**: only present on login, not across all APIs
- **Editing an expense**: implemented server-side, not yet exposed in the client interface

### Repository structure

```
Evenly.Server/   → ASP.NET Core Web API backend
Evenly/          → .NET MAUI client (Windows, Android, iOS, macCatalyst)
Evenly.Tests/    → NUnit tests
docs/            → design document (PDF, 3 phases)
```

### How to run the project

**Prerequisites**: .NET SDK 10.0, SQL Server LocalDB, Visual Studio 2022 with the MAUI workload + Android SDK.

> Always start the **server** before the client.

```bash
# 1. Server (creates the DB automatically on first run)
dotnet run --project Evenly.Server
# → http://localhost:5054

# 2. Windows client
dotnet run --project Evenly -f net10.0-windows10.0.19041.0

# 3. Android client (requires a running emulator)
dotnet run --project Evenly -f net10.0-android

# Tests
dotnet test Evenly.Tests
```

For the iOS client, running on a physical device, per-platform network configuration, and troubleshooting common issues, see the full guide: [`build_e_avvio.md`](build_e_avvio.md).

> ⚠️ **Note**: the manager account credentials used during development/testing are documented in [`credenziali_gestore.md`](credenziali_gestore.md) — valid only for the local environment, to be replaced before any public or production use.

### License

MIT — agreed upon by all team members (Group 4).
