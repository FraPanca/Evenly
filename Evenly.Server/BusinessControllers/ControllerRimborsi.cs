using Evenly.Server.Domain;
using Evenly.Server.Observer;
using Microsoft.Data.SqlClient;

namespace Evenly.Server.BusinessControllers;

public class ControllerRimborsi : ControllerPersistenza
{
    private readonly IServizioNotifichePush _notifiche;
    private readonly ControllerBilancio _controllerBilancio;

    public ControllerRimborsi(string connectionString, string logPath,
        IServizioNotifichePush notifiche, ControllerBilancio controllerBilancio)
        : base(connectionString, logPath)
    {
        _notifiche = notifiche;
        _controllerBilancio = controllerBilancio;
    }

    public Rimborso RegistraRimborso(Utente debitore, Utente creditore,
        decimal importo, Gruppo gruppo)
    {
        if (importo <= 0)
            throw new ArgumentException("L'importo del rimborso deve essere positivo.");
        if (debitore.GetUsername() == creditore.GetUsername())
            throw new ArgumentException("Debitore e creditore devono essere utenti diversi.");

        var rimborso = new Rimborso(debitore, creditore, importo, gruppo, DateTime.UtcNow);

        using var conn = GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = new SqlCommand(
                @"INSERT INTO RIMBORSO (RimborsoId, DebitoreUsername, CreditoreUsername, GroupId, Importo, Data)
                  VALUES (@id, @d, @c, @g, @imp, @data)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", rimborso.GetRimborsoId());
                cmd.Parameters.AddWithValue("@d", debitore.GetUsername());
                cmd.Parameters.AddWithValue("@c", creditore.GetUsername());
                cmd.Parameters.AddWithValue("@g", gruppo.GetGroupId());
                cmd.Parameters.AddWithValue("@imp", importo);
                cmd.Parameters.AddWithValue("@data", rimborso.GetData());
                cmd.ExecuteNonQuery();
            }

            // Aggiorna saldi: debitore migliora, creditore diminuisce
            using (var cmd = new SqlCommand(
                "UPDATE SALDO SET Valore = Valore + @imp WHERE Username = @u AND GroupId = @g",
                conn, tx))
            {
                cmd.Parameters.AddWithValue("@imp", importo);
                cmd.Parameters.AddWithValue("@u", debitore.GetUsername());
                cmd.Parameters.AddWithValue("@g", gruppo.GetGroupId());
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new SqlCommand(
                "UPDATE SALDO SET Valore = Valore - @imp WHERE Username = @u AND GroupId = @g",
                conn, tx))
            {
                cmd.Parameters.AddWithValue("@imp", importo);
                cmd.Parameters.AddWithValue("@u", creditore.GetUsername());
                cmd.Parameters.AddWithValue("@g", gruppo.GetGroupId());
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        _notifiche.NotificaRimborso(rimborso, new List<Utente> { creditore });
        ScriviLog("RIMBORSO", debitore.GetUsername(),
            $"Rimborso di {importo} a {creditore.GetUsername()} nel gruppo {gruppo.GetGroupId()}.");
        return rimborso;
    }

    public List<Rimborso> GetRimborsiGruppo(Guid groupId, Dictionary<string, Utente> utentiCache)
    {
        using var conn = GetConnection();
        using var cmd = new SqlCommand(
            @"SELECT RimborsoId, DebitoreUsername, CreditoreUsername, Importo, Data
              FROM RIMBORSO WHERE GroupId = @g ORDER BY Data DESC", conn);
        cmd.Parameters.AddWithValue("@g", groupId);

        var rimborsi = new List<Rimborso>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string debU = reader.GetString(1);
            string credU = reader.GetString(2);
            if (!utentiCache.TryGetValue(debU, out var deb) ||
                !utentiCache.TryGetValue(credU, out var cred)) continue;

            var gruppoTemp = Gruppo.FromDb(groupId, "", "", "EUR", DateTime.UtcNow, true);
            rimborsi.Add(Rimborso.FromDb(
                reader.GetGuid(0), deb, cred, reader.GetDecimal(3),
                gruppoTemp, reader.GetDateTime(4)));
        }
        return rimborsi;
    }
}
