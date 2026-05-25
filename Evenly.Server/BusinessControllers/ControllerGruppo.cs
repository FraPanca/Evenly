using Evenly.Server.Domain;
using Evenly.Server.Enums;
using Microsoft.Data.SqlClient;

namespace Evenly.Server.BusinessControllers;

public class ControllerGruppo : ControllerPersistenza
{
    public ControllerGruppo(string connectionString, string logPath)
        : base(connectionString, logPath) { }

    public Gruppo CreaGruppo(string groupName, string password, string valutaRiferimento,
        Utente creatore)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("Nome gruppo obbligatorio.");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            throw new ArgumentException("Password gruppo deve essere almeno 6 caratteri.");

        var gruppo = new Gruppo(groupName, password, valutaRiferimento);

        using var conn = GetConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            using (var cmd = new SqlCommand(
                @"INSERT INTO GRUPPO (GroupId, GroupName, PasswordHash, ValutaRiferimento, DataCreazione, IsAttivo)
                  VALUES (@id, @name, @ph, @vr, @dc, 1)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", gruppo.GetGroupId());
                cmd.Parameters.AddWithValue("@name", gruppo.GetGroupName());
                cmd.Parameters.AddWithValue("@ph", gruppo.GetGroupPasswordHash());
                cmd.Parameters.AddWithValue("@vr", gruppo.GetValutaRiferimento());
                cmd.Parameters.AddWithValue("@dc", gruppo.GetDataCreazione());
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand(
                @"INSERT INTO PARTECIPAZIONE (Username, GroupId, Ruolo, DataIngresso)
                  VALUES (@u, @g, 'AMMINISTRATORE', @d)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@u", creatore.GetUsername());
                cmd.Parameters.AddWithValue("@g", gruppo.GetGroupId());
                cmd.Parameters.AddWithValue("@d", DateTime.UtcNow);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand(
                @"INSERT INTO SALDO (Username, GroupId, Valore, Valuta)
                  VALUES (@u, @g, 0, @v)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@u", creatore.GetUsername());
                cmd.Parameters.AddWithValue("@g", gruppo.GetGroupId());
                cmd.Parameters.AddWithValue("@v", gruppo.GetValutaRiferimento());
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        ScriviLog("CREA_GRUPPO", creatore.GetUsername(),
            $"Gruppo '{groupName}' creato (ID: {gruppo.GetGroupId()}).");
        return gruppo;
    }

    public Gruppo GetGruppo(Guid groupId)
    {
        using var conn = GetConnection();
        using var cmd = new SqlCommand(
            "SELECT GroupId, GroupName, PasswordHash, ValutaRiferimento, DataCreazione, IsAttivo FROM GRUPPO WHERE GroupId = @id",
            conn);
        cmd.Parameters.AddWithValue("@id", groupId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) throw new KeyNotFoundException($"Gruppo {groupId} non trovato.");
        return Gruppo.FromDb(
            reader.GetGuid(0), reader.GetString(1), reader.GetString(2),
            reader.GetString(3), reader.GetDateTime(4), reader.GetBoolean(5));
    }

    public List<(Gruppo Gruppo, int NumeroPartecipanti)> GetGruppiUtenteConPartecipanti(string username)
    {
        using var conn = GetConnection();
        using var cmd = new SqlCommand(
            @"SELECT g.GroupId, g.GroupName, g.PasswordHash, g.ValutaRiferimento, g.DataCreazione, g.IsAttivo,
                     (SELECT COUNT(*) FROM PARTECIPAZIONE p2 WHERE p2.GroupId = g.GroupId) AS NumPart
              FROM GRUPPO g INNER JOIN PARTECIPAZIONE p ON g.GroupId = p.GroupId
              WHERE p.Username = @u AND g.IsAttivo = 1", conn);
        cmd.Parameters.AddWithValue("@u", username);

        var result = new List<(Gruppo, int)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add((Gruppo.FromDb(
                reader.GetGuid(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetDateTime(4), reader.GetBoolean(5)),
                reader.GetInt32(6)));
        }
        return result;
    }

    public List<Gruppo> GetGruppiUtente(string username)
    {
        using var conn = GetConnection();
        using var cmd = new SqlCommand(
            @"SELECT g.GroupId, g.GroupName, g.PasswordHash, g.ValutaRiferimento, g.DataCreazione, g.IsAttivo
              FROM GRUPPO g INNER JOIN PARTECIPAZIONE p ON g.GroupId = p.GroupId
              WHERE p.Username = @u AND g.IsAttivo = 1", conn);
        cmd.Parameters.AddWithValue("@u", username);

        var gruppi = new List<Gruppo>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            gruppi.Add(Gruppo.FromDb(
                reader.GetGuid(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetDateTime(4), reader.GetBoolean(5)));
        }
        return gruppi;
    }

    internal void EliminaGruppo(Guid groupId, SqlConnection conn, SqlTransaction tx)
    {
        using var cmd = new SqlCommand(
            "UPDATE GRUPPO SET IsAttivo = 0 WHERE GroupId = @id", conn, tx);
        cmd.Parameters.AddWithValue("@id", groupId);
        cmd.ExecuteNonQuery();
    }
}
