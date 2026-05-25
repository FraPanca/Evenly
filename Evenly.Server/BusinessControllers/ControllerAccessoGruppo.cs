using Evenly.Server.Domain;
using Evenly.Server.Enums;
using Microsoft.Data.SqlClient;

namespace Evenly.Server.BusinessControllers;

public class ControllerAccessoGruppo : ControllerPersistenza
{
    private readonly ControllerGruppo _controllerGruppo;

    public ControllerAccessoGruppo(string connectionString, string logPath,
        ControllerGruppo controllerGruppo)
        : base(connectionString, logPath)
    {
        _controllerGruppo = controllerGruppo;
    }

    public void AccediAlGruppo(Utente utente, Guid groupId, string password)
    {
        var gruppo = _controllerGruppo.GetGruppo(groupId);

        if (!gruppo.VerificaPassword(password))
            throw new UnauthorizedAccessException("Password del gruppo errata.");

        if (!gruppo.IsAttivo())
            throw new InvalidOperationException("Gruppo non attivo.");

        AggiungiPartecipante(utente, gruppo);
        ScriviLog("ACCESSO_GRUPPO", utente.GetUsername(),
            $"Accesso al gruppo '{gruppo.GetGroupName()}' tramite credenziali.");
    }

    public void AccediConLink(Utente utente, LinkInvito link)
    {
        if (!link.IsValido())
            throw new InvalidOperationException("Link di invito non valido o scaduto.");

        link.Utilizza();

        using var conn = GetConnection();
        using (var cmd = new SqlCommand(
            "UPDATE LINK_INVITO SET UtilizziResidui = @ur, Stato = @s WHERE LinkId = @id", conn))
        {
            cmd.Parameters.AddWithValue("@ur", link.GetUtilizziResidui());
            cmd.Parameters.AddWithValue("@s", link.GetStato().ToString());
            cmd.Parameters.AddWithValue("@id", link.GetLinkId());
            cmd.ExecuteNonQuery();
        }

        AggiungiPartecipante(utente, link.GetGruppo());
        ScriviLog("ACCESSO_LINK", utente.GetUsername(),
            $"Accesso al gruppo '{link.GetGruppo().GetGroupName()}' tramite link.");
    }

    public void AbbandonaGruppo(Utente utente, Gruppo gruppo)
    {
        VerificaSaldoNullo(utente.GetUsername(), gruppo.GetGroupId());

        using var conn = GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = new SqlCommand(
                "DELETE FROM PARTECIPAZIONE WHERE Username = @u AND GroupId = @g", conn, tx))
            {
                cmd.Parameters.AddWithValue("@u", utente.GetUsername());
                cmd.Parameters.AddWithValue("@g", gruppo.GetGroupId());
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand(
                "DELETE FROM SALDO WHERE Username = @u AND GroupId = @g", conn, tx))
            {
                cmd.Parameters.AddWithValue("@u", utente.GetUsername());
                cmd.Parameters.AddWithValue("@g", gruppo.GetGroupId());
                cmd.ExecuteNonQuery();
            }

            // Se il gruppo è rimasto vuoto, lo elimina
            int rimasti = ContaPartecipanti(gruppo.GetGroupId(), conn, tx);
            if (rimasti == 0)
            {
                _controllerGruppo.EliminaGruppo(gruppo.GetGroupId(), conn, tx);
                gruppo.SetIsAttivo(false);
            }
            else
            {
                // Se era admin, assegna nuovo admin
                AssegnaAmministratoreSeNecessario(utente.GetUsername(), gruppo.GetGroupId(), conn, tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        ScriviLog("ABBANDONA_GRUPPO", utente.GetUsername(),
            $"Utente ha abbandonato il gruppo '{gruppo.GetGroupName()}'.");
    }

    public void RimuoviMembro(string adminUsername, Guid groupId, string targetUsername)
    {
        VerificaAmministratore(adminUsername, groupId);

        if (adminUsername == targetUsername)
            throw new InvalidOperationException("Non puoi rimuovere te stesso. Usa 'Abbandona gruppo'.");

        string? ruoloTarget;
        using (var conn = GetConnection())
        using (var cmd = new SqlCommand(
            "SELECT Ruolo FROM PARTECIPAZIONE WHERE Username = @u AND GroupId = @g", conn))
        {
            cmd.Parameters.AddWithValue("@u", targetUsername);
            cmd.Parameters.AddWithValue("@g", groupId);
            ruoloTarget = cmd.ExecuteScalar()?.ToString();
        }
        if (ruoloTarget == null) throw new KeyNotFoundException("Utente non trovato nel gruppo.");
        if (ruoloTarget == "AMMINISTRATORE")
            throw new InvalidOperationException("Non puoi rimuovere un altro amministratore.");

        VerificaSaldoNullo(targetUsername, groupId);

        using var connDel = GetConnection();
        using var tx = connDel.BeginTransaction();
        try
        {
            using (var cmd = new SqlCommand(
                "DELETE FROM PARTECIPAZIONE WHERE Username = @u AND GroupId = @g", connDel, tx))
            {
                cmd.Parameters.AddWithValue("@u", targetUsername);
                cmd.Parameters.AddWithValue("@g", groupId);
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new SqlCommand(
                "DELETE FROM SALDO WHERE Username = @u AND GroupId = @g", connDel, tx))
            {
                cmd.Parameters.AddWithValue("@u", targetUsername);
                cmd.Parameters.AddWithValue("@g", groupId);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }

        ScriviLog("RIMUOVI_MEMBRO", adminUsername,
            $"Rimosso '{targetUsername}' dal gruppo {groupId}.");
    }

    public void AccediConToken(Utente utente, string token)
    {
        Guid linkId, groupId;
        string tok, statoStr;
        int utilizziResidui;
        DateTime dataScadenza;

        using (var conn = GetConnection())
        using (var cmd = new SqlCommand(
            "SELECT LinkId, GroupId, Token, Stato, UtilizziResidui, DataScadenza FROM LINK_INVITO WHERE Token = @t",
            conn))
        {
            cmd.Parameters.AddWithValue("@t", token);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                throw new InvalidOperationException("Token non trovato.");
            linkId = reader.GetGuid(0);
            groupId = reader.GetGuid(1);
            tok = reader.GetString(2);
            statoStr = reader.GetString(3);
            utilizziResidui = reader.GetInt32(4);
            dataScadenza = reader.GetDateTime(5);
        }

        var stato = Enum.Parse<StatoLink>(statoStr);
        var gruppo = _controllerGruppo.GetGruppo(groupId);
        var link = LinkInvito.FromDb(linkId, gruppo, tok, stato, utilizziResidui, dataScadenza);
        AccediConLink(utente, link);
    }

    public LinkInvito GeneraLinkInvito(Gruppo gruppo, Utente richiedente)
    {
        VerificaAmministratore(richiedente.GetUsername(), gruppo.GetGroupId());
        var link = gruppo.GeneraLinkInvito();

        using var conn = GetConnection();
        using var cmd = new SqlCommand(
            @"INSERT INTO LINK_INVITO (LinkId, GroupId, Token, Stato, UtilizziResidui, DataScadenza)
              VALUES (@id, @gid, @tok, @stato, @ur, @ds)", conn);
        cmd.Parameters.AddWithValue("@id", link.GetLinkId());
        cmd.Parameters.AddWithValue("@gid", gruppo.GetGroupId());
        cmd.Parameters.AddWithValue("@tok", link.GetToken());
        cmd.Parameters.AddWithValue("@stato", link.GetStato().ToString());
        cmd.Parameters.AddWithValue("@ur", link.GetUtilizziResidui());
        cmd.Parameters.AddWithValue("@ds", link.GetDataScadenza());
        cmd.ExecuteNonQuery();

        ScriviLog("GENERA_LINK", richiedente.GetUsername(),
            $"Link invito generato per gruppo '{gruppo.GetGroupName()}'.");
        return link;
    }

    private void AggiungiPartecipante(Utente utente, Gruppo gruppo)
    {
        using var conn = GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var checkCmd = new SqlCommand(
                "SELECT COUNT(*) FROM PARTECIPAZIONE WHERE Username = @u AND GroupId = @g", conn, tx))
            {
                checkCmd.Parameters.AddWithValue("@u", utente.GetUsername());
                checkCmd.Parameters.AddWithValue("@g", gruppo.GetGroupId());
                if ((int)checkCmd.ExecuteScalar()! > 0)
                    throw new InvalidOperationException("Utente già membro del gruppo.");
            }

            using (var cmd = new SqlCommand(
                "INSERT INTO PARTECIPAZIONE (Username, GroupId, Ruolo, DataIngresso) VALUES (@u, @g, 'MEMBRO', @d)",
                conn, tx))
            {
                cmd.Parameters.AddWithValue("@u", utente.GetUsername());
                cmd.Parameters.AddWithValue("@g", gruppo.GetGroupId());
                cmd.Parameters.AddWithValue("@d", DateTime.UtcNow);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand(
                "INSERT INTO SALDO (Username, GroupId, Valore, Valuta) VALUES (@u, @g, 0, @v)",
                conn, tx))
            {
                cmd.Parameters.AddWithValue("@u", utente.GetUsername());
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
    }

    private void VerificaSaldoNullo(string username, Guid groupId)
    {
        using var conn = GetConnection();
        using var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM SALDO WHERE Username = @u AND GroupId = @g AND ABS(Valore) > 0.001",
            conn);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@g", groupId);
        if ((int)cmd.ExecuteScalar()! > 0)
            throw new InvalidOperationException("Impossibile abbandonare il gruppo: saldo non nullo.");
    }

    private void VerificaAmministratore(string username, Guid groupId)
    {
        using var conn = GetConnection();
        using var cmd = new SqlCommand(
            "SELECT Ruolo FROM PARTECIPAZIONE WHERE Username = @u AND GroupId = @g", conn);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@g", groupId);
        var ruolo = cmd.ExecuteScalar()?.ToString();
        if (ruolo != "AMMINISTRATORE")
            throw new UnauthorizedAccessException("Solo l'amministratore può eseguire questa operazione.");
    }

    private static int ContaPartecipanti(Guid groupId, SqlConnection conn, SqlTransaction tx)
    {
        using var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM PARTECIPAZIONE WHERE GroupId = @g", conn, tx);
        cmd.Parameters.AddWithValue("@g", groupId);
        return (int)cmd.ExecuteScalar()!;
    }

    private static void AssegnaAmministratoreSeNecessario(string utenteAbbandonato,
        Guid groupId, SqlConnection conn, SqlTransaction tx)
    {
        using var checkCmd = new SqlCommand(
            "SELECT Ruolo FROM PARTECIPAZIONE WHERE Username = @u AND GroupId = @g",
            conn, tx);
        checkCmd.Parameters.AddWithValue("@u", utenteAbbandonato);
        checkCmd.Parameters.AddWithValue("@g", groupId);
        // Row already deleted, check if there's still an admin
        using var adminCmd = new SqlCommand(
            "SELECT COUNT(*) FROM PARTECIPAZIONE WHERE GroupId = @g AND Ruolo = 'AMMINISTRATORE'",
            conn, tx);
        adminCmd.Parameters.AddWithValue("@g", groupId);
        if ((int)adminCmd.ExecuteScalar()! == 0)
        {
            using var promCmd = new SqlCommand(
                @"UPDATE PARTECIPAZIONE SET Ruolo = 'AMMINISTRATORE'
                  WHERE GroupId = @g AND Username = (
                    SELECT TOP 1 Username FROM PARTECIPAZIONE WHERE GroupId = @g ORDER BY DataIngresso)",
                conn, tx);
            promCmd.Parameters.AddWithValue("@g", groupId);
            promCmd.ExecuteNonQuery();
        }
    }
}
