using Evenly.Server.Domain;
using Microsoft.Data.SqlClient;

namespace Evenly.Server.BusinessControllers;

public class ControllerRegistrazione : ControllerPersistenza
{
    public ControllerRegistrazione(string connectionString, string logPath)
        : base(connectionString, logPath) { }

    public Utente RegistraUtente(string username, string nome, string cognome,
        string email, string password, string valutaPredefinita)
    {
        ValidaInput(username, nome, cognome, email, password, valutaPredefinita);

        using var conn = GetConnection();

        // Check username duplicato
        using (var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM UTENTE WHERE Username = @u", conn))
        {
            cmd.Parameters.AddWithValue("@u", username);
            int count = (int)cmd.ExecuteScalar()!;
            if (count > 0)
                throw new InvalidOperationException($"Username '{username}' già in uso.");
        }

        // Check email duplicata
        using (var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM UTENTE WHERE Email = @e", conn))
        {
            cmd.Parameters.AddWithValue("@e", email);
            int count = (int)cmd.ExecuteScalar()!;
            if (count > 0)
                throw new InvalidOperationException($"Email '{email}' già registrata.");
        }

        var utente = new Utente(username, nome, cognome, email, password, valutaPredefinita);

        using (var cmd = new SqlCommand(
            @"INSERT INTO UTENTE (Username, Nome, Cognome, Email, PasswordHash, ValutaPredefinita, Stato)
              VALUES (@u, @n, @c, @e, @ph, @vp, @s)", conn))
        {
            cmd.Parameters.AddWithValue("@u", utente.GetUsername());
            cmd.Parameters.AddWithValue("@n", utente.GetNome());
            cmd.Parameters.AddWithValue("@c", utente.GetCognome());
            cmd.Parameters.AddWithValue("@e", utente.GetEmail());
            cmd.Parameters.AddWithValue("@ph", utente.GetPasswordHash());
            cmd.Parameters.AddWithValue("@vp", utente.GetValutaPredefinita());
            cmd.Parameters.AddWithValue("@s", utente.GetStato().ToString());
            cmd.ExecuteNonQuery();
        }

        ScriviLog("REGISTRAZIONE", username, $"Nuovo utente registrato: {email}");
        return utente;
    }

    private static void ValidaInput(string username, string nome, string cognome,
        string email, string password, string valuta)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            throw new ArgumentException("Username deve essere almeno 3 caratteri.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("Email non valida.");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new ArgumentException("Password deve essere almeno 8 caratteri.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome obbligatorio.");
        if (string.IsNullOrWhiteSpace(cognome))
            throw new ArgumentException("Cognome obbligatorio.");
        if (string.IsNullOrWhiteSpace(valuta) || valuta.Length != 3)
            throw new ArgumentException("Valuta deve essere un codice di 3 caratteri (es. EUR).");
    }
}
