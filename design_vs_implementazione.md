# Differenze tra Progettazione e Implementazione

Documento comparativo tra quanto specificato nel documento di progettazione
(Ingegneria_del_Software_1.pdf + Ingegneria_del_Software_2.pdf) e quanto
effettivamente realizzato nel prototipo (milestone Giugno 2026).

---

## Funzionalità NON implementate (previste dal documento)

### RF28 / RF29 — Conversione valuta tramite API esterna

**Progettazione:** Il documento prevede un `GestoreConversione` nel diagramma
delle classi (sezione 2.8.2.6 Gruppo) che si interfaccia con un servizio esterno
per ottenere i tassi di cambio aggiornati. Il caso d'uso `ConversioneValuta` è
definito come dipendenza di `GestioneSpese` e `VisualizzazioneSaldi`.

**Implementazione:** Il componente `GestoreConversione` non esiste nel codice.
Ogni spesa viene registrata nella valuta inserita dall'utente senza alcuna
conversione. I saldi vengono calcolati assumendo valuta omogenea all'interno del
gruppo. Rimandato alla versione completa.

---

### RF21 — Notifiche push reali

**Progettazione:** Il requisito RF21 specifica che il sistema deve inviare
notifiche push agli utenti per: nuove spese, modifiche spese, eliminazioni spese,
rimborsi. È prevista l'interfaccia `IServizioNotifichePush` e il meccanismo di
registrazione del device token.

**Implementazione:** L'interfaccia `IServizioNotifichePush` è implementata da
`ServizioNotifichePushMock` (stub), che scrive messaggi su console/logger senza
inviare effettivamente notifiche al dispositivo. La colonna `DeviceTokenPush`
esiste nel database e l'endpoint `PUT /api/utenti/me/dispositivo` è implementato,
ma il servizio di invio è solo simulato.

---

### RF13 — Ricerca gruppo tramite GroupID

**Progettazione:** Il caso d'uso prevede una funzionalità esplicita di ricerca
di un gruppo tramite identificativo (GroupID) come operazione autonoma.

**Implementazione:** Non esiste una schermata di ricerca separata. L'accesso
avviene direttamente tramite la schermata "Accedi a Gruppo" dove l'utente incolla
manualmente il GroupID + password oppure usa un link di invito.

---

### Sicurezza — HTTPS / TLS

**Progettazione:** La tabella minacce (sezione 1.5.2) identifica l'intercettazione
delle comunicazioni come rischio "Alta" probabilità e indica la cifratura HTTPS/TLS
come controllo obbligatorio.

**Implementazione:** Il server gira su HTTP (`http://localhost:5054`) in modalità
sviluppo. HTTPS è configurato (`https://localhost:7123`) ma il client si connette
al solo endpoint HTTP. Accettabile per prototipo locale, non per produzione.

---

### Sicurezza — Rate limiting server-wide

**Progettazione:** La tabella minacce prevede rate limiting e limitazione delle
richieste come controllo contro attacchi DoS (sezione 1.5.2).

**Implementazione:** Il rate limiting è applicato solo al login (massimo 3
tentativi falliti per account, poi blocco). Non esiste un rate limiter globale
sulle API REST.

---

### Notifica di sicurezza dopo brute force

**Progettazione:** Lo scenario `ControlloAccesso` (sezione 1.5.4.1) prevede che
dopo il blocco per brute force il sistema "invii una notifica di sicurezza".

**Implementazione:** Il blocco avviene correttamente (3 tentativi), ma nessuna
notifica viene inviata. L'evento viene solo scritto nel file di log.

---

## Funzionalità parzialmente diverse rispetto alla progettazione

### Modifica spesa (RF19)

**Progettazione:** RF19 specifica che la modifica di una spesa è consentita solo
all'utente che l'ha creata, con diagramma di sequenza dedicato (sezione 3.3.8).

**Implementazione:** Il server implementa `PUT /api/gruppi/{groupId}/spese/{spesaId}`
con controllo di ownership corretto. Tuttavia, il client MAUI non espone
questa funzionalità tramite interfaccia grafica (non c'è un pulsante "Modifica").
La logica server è completa, il frontend è incompleto.

---

### Pattern Observer — disaccoppiamento

**Progettazione:** L'Observer è progettato per notificare partecipanti tramite
push notification effettive (device token → servizio push esterno).

**Implementazione:** Il pattern Observer è correttamente strutturato
(`IServizioNotifichePush` iniettata via DI in `ControllerSpese` e
`ControllerRimborsi`), ma l'implementazione concreta è il mock. Sostituire
il mock con un'implementazione reale (es. Firebase FCM) richiederebbe solo
la registrazione di un binding diverso in `Program.cs`.

---

### ControllerLog separato

**Progettazione:** Il diagramma delle classi (sezione 2.8.2.3 Log) prevede
un `ControllerLog` distinto.

**Implementazione:** La funzionalità di lettura log è assorbita in
`ControllerSicurezza`, che gestisce sia i log che la lista utenti. Struttura
equivalente, solo accorpata.

---

## Aggiunte rispetto alla progettazione (non previste dal documento)

### Supporto multi-piattaforma Android/iOS

**Progettazione:** Il documento specifica solo il requisito non funzionale RNF1
(architettura client-server) senza dettagliare piattaforme target del client.

**Implementazione:** Il client MAUI è configurato per compilare su quattro
piattaforme — Windows, Android, iOS, macCatalyst — tramite i seguenti
adattamenti non presenti nel documento di progettazione:

- **`ApiService.cs`**: URL del server selezionato a compile-time con direttive
  del preprocessore (`#if ANDROID` / `#if IOS` / `#else`) per gestire le
  differenze di rete tra piattaforme (emulatore Android usa `10.0.2.2` come
  alias di `localhost` del PC host).
- **`Platforms/Android/Resources/xml/network_security_config.xml`**: file di
  configurazione sicurezza di rete Android che esplicita il permesso al
  traffico HTTP cleartext verso il server di sviluppo, necessario da Android
  API 28 in poi.
- **`Platforms/iOS/Info.plist`**: eccezione App Transport Security (ATS) per
  consentire connessioni HTTP verso `localhost` nel simulatore iOS, necessaria
  perché iOS blocca HTTP per default.

---

## Funzionalità implementate in accordo con la progettazione

| Requisito | Stato |
|-----------|-------|
| RF1 — Registrazione | Implementato |
| RF2 — Login con JWT | Implementato |
| RF3 — Eliminazione account | Implementato (con verifica saldo nullo) |
| RF4 — Modifica profilo | Implementato |
| RF5 — Creazione gruppo | Implementato |
| RF6 — Ruolo amministratore al creatore | Implementato |
| RF7 — Gestione membri da admin | Implementato |
| RF8 — Valuta di riferimento gruppo | Implementato |
| RF9 — Gruppi privati | Implementato |
| RF11 — Accesso con GroupID + password | Implementato |
| RF12 — Accesso con link di invito | Implementato |
| RF14 — Abbandono gruppo (saldo nullo) + nomina nuovo admin | Implementato |
| RF15 — Link con scadenza temporale e per utilizzi | Implementato |
| RF16 — Eliminazione automatica gruppo vuoto | Implementato (soft delete) |
| RF17 — Inserimento spesa (min 2 utenti) | Implementato |
| RF18 — Memorizzazione dati spesa | Implementato |
| RF20 — Eliminazione spesa solo al creatore | Implementato |
| RF22 — Divisione equa, importi esatti, percentuale | Implementato (Pattern Strategy) |
| RF24 — Calcolo automatico saldi | Implementato |
| RF25 — Indicazione credito/debito | Implementato |
| RF26/27 — Minimizzazione transazioni | Implementato (algoritmo greedy) |
| RF30 — Registrazione rimborso | Implementato |
| RF31 — Aggiornamento saldi dopo rimborso | Implementato |
| RF32 — Visualizzazione lista spese | Implementato |
| RF33 — Visualizzazione bilancio | Implementato |
| RF34/35 — Storico transazioni | Implementato |
| Brute force protection | Implementato (3 tentativi, blocco account) |
| Log di sistema | Implementato (file-based) |
| Dashboard gestore sicurezza | Implementato |
| Pattern ECB | Implementato (Entity/Controller/Boundary) |
| Pattern Strategy divisione spese | Implementato |
| Pattern Observer notifiche | Implementato (mock) |
| Architettura 3-tier Client/Server/DB | Implementato |
