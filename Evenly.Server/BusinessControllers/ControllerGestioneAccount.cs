using Evenly.Server.Domain;
using Evenly.Server.Enums;
using Microsoft.Data.SqlClient;

namespace Evenly.Server.BusinessControllers;

public class ControllerGestioneAccount : ControllerPersistenza
{
    public ControllerGestioneAccount(string connectionString, string logPath)
        : base(connectionString, logPath) { }

    public Utente GetUtente(string username)
    {
        using var conn = GetConnection();
        using var cmd = new SqlCommand(
            "SELECT Username, Nome, Cognome, Email, PasswordHash, ValutaPredefinita, DeviceTokenPush, Stato FROM UTENTE WHERE Username = @u",
            conn);
        cmd.Parameters.AddWithValue("@u", username);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) throw new KeyNotFoundException($"Utente '{username}' non trovato.");
        return Utente.FromDb(
            reader.GetString(0), reader.GetString(1), reader.GetString(2),
            reader.GetString(3), reader.GetString(4), reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            Enum.Parse<StatoAccount>(reader.GetString(7)));
    }

    public void ModificaDati(string username, string? nuovoNome, string? nuovoCognome,
        string? nuovaEmail, string? nuovaValuta, string richiedente)
    {
        if (username != richiedente)
            throw new UnauthorizedAccessException("Non autorizzato.");

        using var conn = GetConnection();

        if (nuovaEmail != null)
        {
            using var checkCmd = new SqlCommand(
                "SELECT COUNT(*) FROM UTENTE WHERE Email = @e AND Username <> @u", conn);
            checkCmd.Parameters.AddWithValue("@e", nuovaEmail);
            checkCmd.Parameters.AddWithValue("@u", username);
            if ((int)checkCmd.ExecuteScalar()! > 0)
                throw new InvalidOperationException("Email già in uso.");
        }

        using var cmd = new SqlCommand(
            @"UPDATE UTENTE SET
                Nome = COALESCE(@n, Nome),
                Cognome = COALESCE(@c, Cognome),
                Email = COALESCE(@e, Email),
                ValutaPredefinita = COALESCE(@vp, ValutaPredefinita)
              WHERE Username = @u", conn);
        cmd.Parameters.AddWithValue("@n", (object?)nuovoNome ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@c", (object?)nuovoCognome ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@e", (object?)nuovaEmail ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@vp", (object?)nuovaValuta ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.ExecuteNonQuery();

        ScriviLog("MODIFICA_PROFILO", username, "Dati profilo aggiornati.");
    }

    public void EliminaAccount(string username, string richiedente)
    {
        if (username != richiedente)
            throw new UnauthorizedAccessException("Non autorizzato.");

        VerificaSaldoNullo(username);

        using var conn = GetConnection();

        // Rimuovi partecipazioni
        using (var cmd = new SqlCommand("DELETE FROM PARTECIPAZIONE WHERE Username = @u", conn))
        {
            cmd.Parameters.AddWithValue("@u", username);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new SqlCommand(
            "UPDATE UTENTE SET Stato = 'ELIMINATO' WHERE Username = @u", conn))
        {
            cmd.Parameters.AddWithValue("@u", username);
            cmd.ExecuteNonQuery();
        }

        ScriviLog("ELIMINA_ACCOUNT", username, "Account eliminato.");
    }

    public void AggiornaDipositivoToken(string username, string? token)
    {
        using var conn = GetConnection();
        using var cmd = new SqlCommand(
            "UPDATE UTENTE SET DeviceTokenPush = @t WHERE Username = @u", conn);
        cmd.Parameters.AddWithValue("@t", (object?)token ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.ExecuteNonQuery();
    }

    private void VerificaSaldoNullo(string username)
    {
        using var conn = GetConnection();
        using var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM SALDO WHERE Username = @u AND ABS(Valore) > 0.001", conn);
        cmd.Parameters.AddWithValue("@u", username);
        int count = (int)cmd.ExecuteScalar()!;
        if (count > 0)
            throw new InvalidOperationException("Impossibile eliminare l'account: saldi non nulli nei gruppi.");
    }
}
