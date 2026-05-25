using Evenly.Server.Enums;
using System.Security.Cryptography;

namespace Evenly.Server.Domain;

public class Utente
{
    private string _username;
    private string _nome;
    private string _cognome;
    private string _email;
    private string _passwordHash;
    private string _valutaPredefinita;
    private string? _deviceTokenPush;
    private StatoAccount _stato;

    public Utente(string username, string nome, string cognome, string email, string password, string valutaPredefinita)
    {
        _username = username;
        _nome = nome;
        _cognome = cognome;
        _email = email;
        _passwordHash = HashPassword(password);
        _valutaPredefinita = valutaPredefinita;
        _stato = StatoAccount.ATTIVO;
    }

    // Constructor for loading from DB (password already hashed)
    public static Utente FromDb(string username, string nome, string cognome, string email,
        string passwordHash, string valutaPredefinita, string? deviceTokenPush, StatoAccount stato)
    {
        var u = new Utente(username, nome, cognome, email, "placeholder", valutaPredefinita);
        u._passwordHash = passwordHash;
        u._deviceTokenPush = deviceTokenPush;
        u._stato = stato;
        return u;
    }

    public string GetUsername() => _username;
    public string GetNome() => _nome;
    public string GetCognome() => _cognome;
    public string GetEmail() => _email;
    public string GetPasswordHash() => _passwordHash;
    public string GetValutaPredefinita() => _valutaPredefinita;
    public string? GetDeviceTokenPush() => _deviceTokenPush;
    public StatoAccount GetStato() => _stato;

    public void SetUsername(string username) => _username = username;
    public void SetNome(string nome) => _nome = nome;
    public void SetCognome(string cognome) => _cognome = cognome;
    public void SetEmail(string email) => _email = email;
    public void SetValutaPredefinita(string valuta) => _valutaPredefinita = valuta;
    public void SetDeviceTokenPush(string? token) => _deviceTokenPush = token;
    public void SetStato(StatoAccount stato) => _stato = stato;

    public bool VerificaPassword(string password) => VerifyPassword(password, _passwordHash);

    public static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;
        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] expected = Convert.FromBase64String(parts[1]);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
