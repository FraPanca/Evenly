# Credenziali Gestore Sicurezza

## Account Amministratore di Sistema

| Campo    | Valore                  |
|----------|-------------------------|
| Username | `gestore`               |
| Password | `Sicurezza!2026`        |
| Email    | `gestore@evenly.local`  |
| Nome     | Gestore                 |
| Cognome  | Sicurezza               |
| Valuta   | EUR                     |
| Stato    | ATTIVO                  |

## Come accedere

1. Avviare il server (`dotnet run --project Evenly.Server`)
2. Aprire l'app client Evenly
3. Nella schermata di login inserire:
   - Username: `gestore`
   - Password: `Sicurezza!2026`
4. Dopo il login comparirà la **Dashboard Sicurezza** al posto della Home normale

## Funzionalità disponibili al gestore

- Visualizzazione di **tutti gli utenti registrati** nel sistema
- Lettura del **log di sistema** completo (`logs/evenly.log`)
- Aggiornamento manuale dei dati visualizzati (pulsante "Aggiorna")

## Dove vengono create le credenziali

Le credenziali sono inserite automaticamente all'avvio del server dal metodo
`SeedGestoreSicurezza()` in `Program.cs`. Se l'account esiste già, la password
viene aggiornata al valore sopra indicato ad ogni avvio.
