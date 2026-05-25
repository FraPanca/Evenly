using Evenly.Server.Domain;
using Evenly.Server.Enums;
using Microsoft.Data.SqlClient;

namespace Evenly.Server.BusinessControllers;

public class ControllerAutenticazione : ControllerPersistenza
{
    private readonly Dictionary<string, int> _tentativiFalliti = new();
    private const int MaxTentativi = 3;

    public ControllerAutenticazione(string connectionString, string logPath)
        : base(connectionString, logPath) { }

    public Utente VerificaCredenziali(string username, string password)
    {
        if (_tentativiFalliti.TryGetValue(username, out int tentativi) && tentativi >= MaxTentativi)
            throw new UnauthorizedAccessException("Account temporaneamente bloccato per troppi tentativi falliti.");

        using var conn = GetConnection();
        Utente? utente = null;

        using (var cmd = new SqlCommand(
            "SELECT Username, Nome, Cognome, Email, PasswordHash, ValutaPredefinita, DeviceTokenPush, Stato FROM UTENTE WHERE Username = @u",
            conn))
        {
            cmd.Parameters.AddWithValue("@u", username);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                utente = Utente.FromDb(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    Enum.Parse<StatoAccount>(reader.GetString(7))
                );
            }
        }

        if (utente == null || !utente.VerificaPassword(password))
        {
            _tentativiFalliti[username] = (_tentativiFalliti.GetValueOrDefault(username)) + 1;
            ScriviLog("LOGIN_FALLITO", username, "Credenziali errate.");
            return null!;
        }

        _tentativiFalliti.Remove(username);
        ScriviLog("LOGIN", username, "Accesso effettuato.");
        return utente;
    }

    public void ResetTentativi(string username) => _tentativiFalliti.Remove(username);
}
