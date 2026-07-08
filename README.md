# Evenly

Applicazione client-server per la gestione di spese condivise tra gruppi di utenti — progetto universitario (Ingegneria del Software, Alma Mater Studiorum – Università di Bologna).

![C#](https://img.shields.io/badge/-C%23-239120?style=flat-square&logo=c-sharp&logoColor=white)
![.NET MAUI](https://img.shields.io/badge/-.NET%20MAUI-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/-ASP.NET%20Core-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/-SQL%20Server-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white)

---

## 🇬🇧 English summary

Evenly is a client-server application for tracking and settling shared expenses within groups (e.g. trips among friends): it records who paid what, computes each member's balance in real time, and calculates the minimum number of transactions needed to settle debts at the end.

- **Client**: .NET MAUI — cross-platform (Windows, Android, iOS, macCatalyst) from a single codebase
- **Server**: ASP.NET Core Web API — REST, JWT authentication
- **Database**: SQL Server, with a separate file-based log server
- **Status**: first working prototype (June 2026 milestone) — core features implemented, some designed features (currency conversion, real push notifications) are stubbed/mocked. See [Known limitations](#limitazioni-note--roadmap) below.
- **Full design document** (3 phases: Requirements Analysis → Problem Analysis → Design, ~115 pages): [`docs/`](docs/)

The rest of this README is in Italian, matching the language of the design document and codebase comments.

---

## Abstract

Gestire le spese durante una vacanza tra amici può risultare complesso: chi paga cosa, chi anticipa una spesa, chi deve restituire cosa a chi. Evenly nasce per risolvere questo problema: tiene traccia in tempo reale del saldo di ciascun partecipante e, al termine dell'esperienza, calcola automaticamente il modo più efficiente per regolare i conti tra tutti, minimizzando il numero di transazioni necessarie.

## Team

**Gruppo 4 — A.A. 2025/2026**
- Francesco Brini
- Francesco Pancaldi
- Gaia Volta

## Documentazione di progetto

Il documento di progettazione completo (115 pagine, in 3 fasi — Analisi dei Requisiti, Analisi del Problema, Progettazione) è disponibile in [`docs/`](docs/).

## Architettura

Il sistema adotta un'architettura **Client/Server a 3 livelli**:

- **L1 — Client**: applicazione .NET MAUI, si occupa esclusivamente di presentazione e interazione utente. Nessuna logica di business.
- **L2 — Server**: server applicativo monolitico (ASP.NET Core Web API), espone la logica tramite endpoint REST, strutturato secondo il pattern **MVC/ECB** (Entity-Controller-Boundary).
- **L3 — Persistenza**: due server fisicamente separati — uno per il database applicativo (SQL Server), uno per il log delle operazioni — così che una compromissione dell'uno non intacchi l'altro.

Pattern applicati: **Strategy** (calcolo quote per metodo di divisione spesa), **Observer** (notifica dei partecipanti sugli eventi, tramite `IServizioNotifichePush` iniettata via DI).

## Stack tecnologico

| Livello | Tecnologia |
|---|---|
| Client | C# — .NET MAUI (Windows, Android, iOS, macCatalyst) |
| Server | ASP.NET Core Web API (REST, autenticazione JWT) |
| Database | SQL Server (LocalDB in sviluppo) |
| Log | File di log su server separato |
| Test | NUnit |

## Funzionalità implementate (prototipo — milestone Giugno 2026)

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

## Limitazioni note / Roadmap

Rispetto al documento di progettazione, alcune funzionalità sono ancora **stub o non implementate** nel prototipo attuale:

- **Conversione valuta**: non implementata — le spese vengono registrate nella valuta inserita, senza conversione tramite API esterna
- **Notifiche push**: il servizio (`IServizioNotifichePush`) è implementato come mock che scrive su log/console, non invia notifiche reali al dispositivo
- **Ricerca gruppo dedicata**: non presente come schermata separata; l'accesso avviene tramite GroupID + password o link
- **HTTPS in sviluppo**: il client si connette tramite HTTP in locale; l'endpoint HTTPS è configurato ma non ancora utilizzato dal client
- **Rate limiting globale**: presente solo sul login, non su tutte le API
- **Modifica spesa**: implementata lato server, non ancora esposta nell'interfaccia del client

**Prossime milestone** (dal piano di lavoro):

| Periodo | Fase |
|---|---|
| Settembre 2026 | Prima versione con tutte le funzionalità, test di integrazione e usabilità |
| Dicembre 2026 | Beta con accesso a numero limitato di utenti pre-registrati |
| Marzo 2027 | Rilascio pubblico |

## Struttura del repository

```
Evenly.Server/   → backend ASP.NET Core Web API
Evenly/          → client .NET MAUI (Windows, Android, iOS, macCatalyst)
Evenly.Tests/    → test NUnit
docs/            → documento di progettazione (PDF, 3 fasi)
```

## Come eseguire il progetto

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

## Licenza

*In attesa di accordo tra i membri del team* (progetto sviluppato collettivamente da Gruppo 4).
