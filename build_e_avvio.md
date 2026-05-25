# Compilazione e Avvio di Evenly

## Prerequisiti

| Strumento            | Versione minima                              |
|----------------------|----------------------------------------------|
| .NET SDK             | 10.0                                         |
| SQL Server LocalDB   | incluso con VS 2022                          |
| Visual Studio 2022   | con workload **MAUI** e **Android SDK**      |
| Android SDK          | installato automaticamente dal workload MAUI |

---

## Ordine corretto di avvio

> **Importante:** avviare SEMPRE il server prima del client (qualsiasi piattaforma).

```
1. Avvia server  →  2. Avvia emulatore Android (se serve)  →  3. Avvia client
```

---

## 1. Avvio del Server (ASP.NET Core)

```cmd
dotnet run --project Evenly.Server
```

Il server:
- Si avvia su `http://localhost:5054`
- Crea automaticamente il database `EvenlyDB` su SQL Server LocalDB (prima esecuzione)
- Crea l'account gestore (`gestore` / `Sicurezza!2026`) se non esiste
- Inizia a scrivere log in `Evenly.Server\logs\evenly.log`

**Con Visual Studio:** click destro su `Evenly.Server` → **Set as Startup Project** → **F5**

---

## 2. Client Windows (Desktop)

```cmd
dotnet run --project Evenly -f net10.0-windows10.0.19041.0
```

**Con Visual Studio:** selezionare target **Windows Machine** → **F5**

---

## 3. Client Android (Emulatore AVD)

### Passo 1 — Creare un emulatore (solo la prima volta)

1. In Visual Studio → **Tools → Android → Android Device Manager**
2. Cliccare **New**
3. Scegliere un dispositivo base (es. **Pixel 6**) e **API 35**
4. Cliccare **Create** e attendere il download dell'immagine di sistema

### Passo 2 — Avviare l'emulatore

**Da Android Device Manager:**
- Cliccare **Start** accanto all'AVD creato
- Attendere che il telefono virtuale si avvii completamente (schermata home visibile)

**Da riga di comando** (dopo aver trovato il nome dell'AVD):
```cmd
# Lista AVD disponibili
%LOCALAPPDATA%\Android\Sdk\emulator\emulator.exe -list-avds

# Avvia un AVD per nome
%LOCALAPPDATA%\Android\Sdk\emulator\emulator.exe -avd Pixel_6_API_35
```

### Passo 3 — Verificare che il device sia rilevato

```cmd
%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe devices
```

Output atteso:
```
List of devices attached
emulator-5554   device
```

Se il device è `offline` attendere qualche secondo e riprovare.

### Passo 4 — Compilare e avviare il client Android

```cmd
dotnet build Evenly -f net10.0-android
dotnet run --project Evenly -f net10.0-android
```

**Con Visual Studio:**
1. Selezionare come framework target **net10.0-android**
2. Selezionare l'emulatore nel menu a tendina (es. `Pixel 6 API 35`)
3. Premere **F5**

> L'app si connette al server tramite `http://10.0.2.2:5054`
> (`10.0.2.2` è l'alias che l'emulatore Android usa per raggiungere `localhost` del PC).

---

## 4. Client iOS (solo su Mac)

Il toolchain iOS non è eseguibile su Windows da riga di comando. Le opzioni sono:

| Opzione | Come |
|---------|------|
| **Mac diretto** | Aprire `Evenly.sln` su macOS, selezionare simulatore iOS, F5 |
| **Pair to Mac** | Visual Studio → Tools → iOS → Pair to Mac (richiede Mac in rete) |

```cmd
# Compilazione iOS (solo su Mac o con Pair to Mac attivo)
dotnet build Evenly -f net10.0-ios
dotnet run --project Evenly -f net10.0-ios
```

Sul simulatore iOS `localhost` funziona direttamente (il simulatore condivide la rete del Mac).

---

## 5. Test (NUnit)

```cmd
dotnet test Evenly.Tests
```

Oppure da Visual Studio: **Test → Run All Tests**

---

## Compilazione standalone (solo build, senza avvio)

```cmd
# Server
dotnet build Evenly.Server

# Client Windows
dotnet build Evenly -f net10.0-windows10.0.19041.0

# Client Android
dotnet build Evenly -f net10.0-android

# Client iOS (solo Mac)
dotnet build Evenly -f net10.0-ios

# Tutto insieme
dotnet build Evenly.sln
```

---

## Configurazione

### Server — `Evenly.Server\appsettings.json`

```json
{
  "ConnectionStrings": {
    "EvenlyDB": "Server=(localdb)\\MSSQLLocalDB;Database=EvenlyDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Key": "EvenlySecretKey2025!ChangeInProduction!MinLength32Chars",
    "Issuer": "Evenly"
  },
  "LogPath": "logs/evenly.log"
}
```

### Client — URL server per piattaforma (`Evenly\Services\ApiService.cs`)

| Piattaforma | URL usato | Motivo |
|-------------|-----------|--------|
| Windows | `http://localhost:5054` | Comunicazione diretta |
| Android emulatore | `http://10.0.2.2:5054` | `10.0.2.2` è l'alias dell'host |
| iOS simulatore | `http://localhost:5054` | Il simulatore condivide la rete del Mac |
| Dispositivo reale (Android/iOS) | `http://<IP-del-PC>:5054` | Modifica manuale necessaria (vedi sotto) |

### Uso su dispositivo reale (telefono fisico)

1. Trovare l'IP del PC: `ipconfig` → scheda Wi-Fi → `Indirizzo IPv4` (es. `192.168.1.50`)
2. In `ApiService.cs` cambiare la costante per la piattaforma target:
   ```csharp
   private const string ServerUrl = "http://192.168.1.50:5054";
   ```
3. In `Platforms/Android/Resources/xml/network_security_config.xml` aggiungere il dominio:
   ```xml
   <domain includeSubdomains="false">192.168.1.50</domain>
   ```
4. PC e telefono devono essere sulla **stessa rete Wi-Fi**

---

## Riepilogo porte

| Componente   | URL                                             |
|--------------|-------------------------------------------------|
| Server HTTP  | `http://localhost:5054`                         |
| Server HTTPS | `https://localhost:7123`                        |
| DB LocalDB   | `(localdb)\MSSQLLocalDB` → database `EvenlyDB` |

---

## Risoluzione problemi comuni

| Problema | Causa | Soluzione |
|----------|-------|-----------|
| `Connection refused` | Server non avviato | Avviare prima `Evenly.Server` |
| `Database not found` | Prima esecuzione | Il DB viene creato automaticamente, attendere |
| `XA0010: Nessun dispositivo` | Nessun emulatore in esecuzione | Avviare un AVD da Android Device Manager |
| `not supported to launch on Windows` (iOS) | Limitazione Apple | Usare un Mac o Pair to Mac |
| App Android non raggiunge il server | URL errato o HTTP bloccato | Verificare `network_security_config.xml` e che l'URL sia `10.0.2.2` |
| `LocalDB not found` | SDK mancante | Installare SQL Server Express LocalDB (incluso in VS 2022) |
