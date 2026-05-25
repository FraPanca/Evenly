using Evenly.Server.Domain;
using Evenly.Server.Enums;
using Microsoft.Data.SqlClient;

namespace Evenly.Server.BusinessControllers;

public class ControllerBilancio : ControllerPersistenza
{
    public ControllerBilancio(string connectionString, string logPath)
        : base(connectionString, logPath) { }

    // Calcola il bilancio del gruppo dinamicamente (non persistito)
    public Bilancio CalcolaBilancio(Gruppo gruppo, Dictionary<string, Utente> utentiGruppo)
    {
        var saldi = GetSaldiGruppo(gruppo.GetGroupId(), utentiGruppo);
        var bilancio = new Bilancio(gruppo);

        // Algoritmo di minimizzazione dei trasferimenti
        var creditori = saldi.Where(s => s.GetValore() > 0)
            .OrderByDescending(s => s.GetValore()).ToList();
        var debitori = saldi.Where(s => s.GetValore() < 0)
            .OrderBy(s => s.GetValore()).ToList();

        int ci = 0, di = 0;
        var creditoResiduo = creditori.ToDictionary(s => s.GetUtente().GetUsername(), s => s.GetValore());
        var debitoResiduo = debitori.ToDictionary(s => s.GetUtente().GetUsername(), s => Math.Abs(s.GetValore()));

        var creditoriList = creditori.Select(s => s.GetUtente().GetUsername()).ToList();
        var debitoriList = debitori.Select(s => s.GetUtente().GetUsername()).ToList();

        while (ci < creditoriList.Count && di < debitoriList.Count)
        {
            string credUsername = creditoriList[ci];
            string debUsername = debitoriList[di];
            decimal trasferimento = Math.Min(creditoResiduo[credUsername], debitoResiduo[debUsername]);

            bilancio.AddVoce(new VoceBilancio(
                utentiGruppo[credUsername],
                utentiGruppo[debUsername],
                Math.Round(trasferimento, 2)));

            creditoResiduo[credUsername] -= trasferimento;
            debitoResiduo[debUsername] -= trasferimento;

            if (creditoResiduo[credUsername] < 0.001m) ci++;
            if (debitoResiduo[debUsername] < 0.001m) di++;
        }

        return bilancio;
    }

    // Per i test: ritorna le voci di bilancio dalla prospettiva di un utente
    public List<VoceBilancio> GetVociBilancio(Gruppo gruppo, Utente utente)
    {
        var utentiGruppo = GetUtentiGruppo(gruppo.GetGroupId());
        var bilancio = CalcolaBilancio(gruppo, utentiGruppo);
        return bilancio.GetVociBilancio()
            .Where(v => v.GetDebitore().GetUsername() == utente.GetUsername()
                     || v.GetCreditore().GetUsername() == utente.GetUsername())
            .ToList();
    }

    public List<Saldo> GetSaldiGruppo(Guid groupId, Dictionary<string, Utente> utentiGruppo)
    {
        using var conn = GetConnection();
        using var cmd = new SqlCommand(
            "SELECT Username, Valore, Valuta FROM SALDO WHERE GroupId = @g", conn);
        cmd.Parameters.AddWithValue("@g", groupId);

        var saldi = new List<Saldo>();
        // Lazy load utenti dal gruppo se non passati
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string username = reader.GetString(0);
            if (!utentiGruppo.TryGetValue(username, out var utente)) continue;
            // Creiamo un gruppo temporaneo per il saldo
            var gruppoTemp = Gruppo.FromDb(groupId, "", "", reader.GetString(2), DateTime.UtcNow, true);
            saldi.Add(new Saldo(utente, gruppoTemp, reader.GetDecimal(1), reader.GetString(2)));
        }
        return saldi;
    }

    public List<(Utente Utente, string Ruolo)> GetPartecipantiConRuolo(Guid groupId)
    {
        using var conn = GetConnection();
        using var cmd = new SqlCommand(
            @"SELECT u.Username, u.Nome, u.Cognome, u.Email, u.PasswordHash,
                     u.ValutaPredefinita, u.DeviceTokenPush, u.Stato, p.Ruolo
              FROM UTENTE u
              INNER JOIN PARTECIPAZIONE p ON u.Username = p.Username
              WHERE p.GroupId = @g", conn);
        cmd.Parameters.AddWithValue("@g", groupId);

        var risultato = new List<(Utente, string)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var u = Utente.FromDb(
                reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                Enum.Parse<StatoAccount>(reader.GetString(7)));
            risultato.Add((u, reader.GetString(8)));
        }
        return risultato;
    }

    public Dictionary<string, Utente> GetUtentiGruppo(Guid groupId)
    {
        using var conn = GetConnection();
        using var cmd = new SqlCommand(
            @"SELECT u.Username, u.Nome, u.Cognome, u.Email, u.PasswordHash,
                     u.ValutaPredefinita, u.DeviceTokenPush, u.Stato
              FROM UTENTE u
              INNER JOIN PARTECIPAZIONE p ON u.Username = p.Username
              WHERE p.GroupId = @g", conn);
        cmd.Parameters.AddWithValue("@g", groupId);

        var risultato = new Dictionary<string, Utente>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var u = Utente.FromDb(
                reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                Enum.Parse<StatoAccount>(reader.GetString(7)));
            risultato[u.GetUsername()] = u;
        }
        return risultato;
    }
}
