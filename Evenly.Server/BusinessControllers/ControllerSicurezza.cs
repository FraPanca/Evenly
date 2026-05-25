using Evenly.Server.Domain;
using Microsoft.Data.SqlClient;

namespace Evenly.Server.BusinessControllers;

public class ControllerSicurezza : ControllerPersistenza
{
    private readonly string _logFilePath;

    public ControllerSicurezza(string connectionString, string logPath)
        : base(connectionString, logPath)
    {
        _logFilePath = logPath;
    }

    public void CambiaPassword(string username, string vecchiaPassword, string nuovaPassword)
    {
        if (string.IsNullOrWhiteSpace(nuovaPassword) || nuovaPassword.Length < 6)
            throw new ArgumentException("La nuova password deve avere almeno 6 caratteri.");

        string passwordHash;
        using (var conn = GetConnection())
        using (var cmd = new SqlCommand(
            "SELECT PasswordHash FROM UTENTE WHERE Username = @u AND Stato = 'ATTIVO'", conn))
        {
            cmd.Parameters.AddWithValue("@u", username);
            var result = cmd.ExecuteScalar();
            if (result == null) throw new KeyNotFoundException("Utente non trovato.");
            passwordHash = (string)result;
        }

        if (!Utente.VerifyPassword(vecchiaPassword, passwordHash))
            throw new UnauthorizedAccessException("Password corrente errata.");

        string nuovoHash = Utente.HashPassword(nuovaPassword);

        using (var conn = GetConnection())
        using (var cmd = new SqlCommand(
            "UPDATE UTENTE SET PasswordHash = @h WHERE Username = @u", conn))
        {
            cmd.Parameters.AddWithValue("@h", nuovoHash);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.ExecuteNonQuery();
        }

        ScriviLog("CAMBIA_PASSWORD", username, "Password cambiata con successo.");
    }

    public List<(DateTime Timestamp, string Operazione, string Utente, string Dettaglio)> GetTuttiLog(
        int maxRighe = 200)
    {
        var result = new List<(DateTime, string, string, string)>();
        if (!File.Exists(_logFilePath)) return result;

        try
        {
            var lines = File.ReadAllLines(_logFilePath);
            foreach (var line in lines.Reverse())
            {
                // Format: [TIMESTAMP] | [OPERAZIONE] | [UTENTE] | [DETTAGLIO]
                var parts = line.Split(new[] { " | " }, 4, StringSplitOptions.None);
                if (parts.Length < 4) continue;
                if (!DateTime.TryParse(parts[0].Trim('[', ']'), out var ts)) continue;
                string operazione = parts[1].Trim('[', ']');
                string utente = parts[2].Trim('[', ']');
                string dettaglio = parts[3].Trim('[', ']');
                result.Add((ts, operazione, utente, dettaglio));
                if (result.Count >= maxRighe) break;
            }
        }
        catch { }

        return result;
    }

    public List<(string Username, string Nome, string Cognome, string Email, string Stato)> GetTuttiUtenti()
    {
        var result = new List<(string, string, string, string, string)>();
        using var conn = GetConnection();
        using var cmd = new SqlCommand(
            "SELECT Username, Nome, Cognome, Email, Stato FROM UTENTE ORDER BY Username", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add((reader.GetString(0), reader.GetString(1),
                reader.GetString(2), reader.GetString(3), reader.GetString(4)));
        return result;
    }
}
