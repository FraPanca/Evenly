using Evenly.Server.Domain;
using Evenly.Server.Enums;
using Evenly.Server.Observer;
using Evenly.Server.Strategy;
using Microsoft.Data.SqlClient;

namespace Evenly.Server.BusinessControllers;

public class ControllerSpese : ControllerPersistenza
{
    private readonly IServizioNotifichePush _notifiche;

    public ControllerSpese(string connectionString, string logPath,
        IServizioNotifichePush notifiche)
        : base(connectionString, logPath)
    {
        _notifiche = notifiche;
    }

    public Spesa InserisciSpesa(string causale, decimal importo, DateTime data,
        Utente pagante, List<Utente> partecipanti, MetodoDivisione metodo,
        Dictionary<Utente, decimal>? parametri, Gruppo gruppo)
    {
        if (partecipanti.Count < 2)
            throw new InvalidOperationException("Il gruppo deve avere almeno due partecipanti.");
        if (importo <= 0)
            throw new ArgumentException("L'importo deve essere positivo.");
        if (string.IsNullOrWhiteSpace(causale))
            throw new ArgumentException("La causale è obbligatoria.");

        var spesa = new Spesa(causale, importo, data, pagante, gruppo, metodo);
        var strategia = ScegliStrategia(metodo);
        var quote = strategia.CalcolaQuote(spesa, partecipanti, parametri);
        spesa.SetQuote(quote);

        using var conn = GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = new SqlCommand(
                @"INSERT INTO SPESA (SpesaId, Causale, Importo, Data, PaganteUsername, GroupId, MetodoDivisione, Valuta, Note)
                  VALUES (@id, @c, @imp, @d, @pu, @g, @md, @v, @n)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", spesa.GetSpesaId());
                cmd.Parameters.AddWithValue("@c", spesa.GetCausale());
                cmd.Parameters.AddWithValue("@imp", spesa.GetImporto());
                cmd.Parameters.AddWithValue("@d", spesa.GetData());
                cmd.Parameters.AddWithValue("@pu", pagante.GetUsername());
                cmd.Parameters.AddWithValue("@g", gruppo.GetGroupId());
                cmd.Parameters.AddWithValue("@md", spesa.GetMetodoDivisione().ToString());
                cmd.Parameters.AddWithValue("@v", spesa.GetValuta());
                cmd.Parameters.AddWithValue("@n", (object?)spesa.GetNote() ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            foreach (var quota in quote)
            {
                using var qCmd = new SqlCommand(
                    "INSERT INTO QUOTA_SPESA (SpesaId, Username, Importo) VALUES (@s, @u, @imp)",
                    conn, tx);
                qCmd.Parameters.AddWithValue("@s", spesa.GetSpesaId());
                qCmd.Parameters.AddWithValue("@u", quota.GetUtente().GetUsername());
                qCmd.Parameters.AddWithValue("@imp", quota.GetImporto());
                qCmd.ExecuteNonQuery();
            }

            AggiornaSaldi(spesa, quote, conn, tx);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        var altriPartecipanti = partecipanti.Where(u => u.GetUsername() != pagante.GetUsername()).ToList();
        _notifiche.NotificaInserimentoSpesa(spesa, altriPartecipanti);
        ScriviLog("INSERISCI_SPESA", pagante.GetUsername(),
            $"Spesa '{causale}' di {importo} inserita nel gruppo {gruppo.GetGroupId()}.");
        return spesa;
    }

    public Spesa InserisciSpesaImportiEsatti(string causale, decimal importo, DateTime data,
        Utente pagante, List<Utente> partecipanti, Dictionary<Utente, decimal> importiEsatti, Gruppo gruppo)
        => InserisciSpesa(causale, importo, data, pagante, partecipanti,
            MetodoDivisione.IMPORTI_ESATTI, importiEsatti, gruppo);

    public void ModificaSpesa(Spesa spesa, string nuovaCausale, decimal nuovoImporto,
        DateTime nuovaData, Utente richiedente)
    {
        if (spesa.GetPagante().GetUsername() != richiedente.GetUsername())
            throw new UnauthorizedAccessException("Solo chi ha pagato può modificare la spesa.");

        spesa.SetCausale(nuovaCausale);
        spesa.SetImporto(nuovoImporto);
        spesa.SetData(nuovaData);

        using var conn = GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmd = new SqlCommand(
                "UPDATE SPESA SET Causale = @c, Importo = @imp, Data = @d WHERE SpesaId = @id",
                conn, tx);
            cmd.Parameters.AddWithValue("@c", nuovaCausale);
            cmd.Parameters.AddWithValue("@imp", nuovoImporto);
            cmd.Parameters.AddWithValue("@d", nuovaData);
            cmd.Parameters.AddWithValue("@id", spesa.GetSpesaId());
            cmd.ExecuteNonQuery();

            RicalcolaSaldi(spesa.GetGruppo().GetGroupId(), conn, tx);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        ScriviLog("MODIFICA_SPESA", richiedente.GetUsername(),
            $"Spesa {spesa.GetSpesaId()} modificata.");
    }

    public void EliminaSpesaById(Guid spesaId, Guid groupId, string richiedenteUsername)
    {
        string paganteUsername;
        using (var conn = GetConnection())
        using (var cmd = new SqlCommand(
            "SELECT PaganteUsername FROM SPESA WHERE SpesaId = @id AND GroupId = @g", conn))
        {
            cmd.Parameters.AddWithValue("@id", spesaId);
            cmd.Parameters.AddWithValue("@g", groupId);
            var result = cmd.ExecuteScalar();
            if (result == null) throw new KeyNotFoundException("Spesa non trovata.");
            paganteUsername = (string)result;
        }

        if (paganteUsername != richiedenteUsername)
            throw new UnauthorizedAccessException("Solo chi ha pagato può eliminare la spesa.");

        using var connDel = GetConnection();
        using var tx = connDel.BeginTransaction();
        try
        {
            using (var cmd = new SqlCommand("DELETE FROM QUOTA_SPESA WHERE SpesaId = @id", connDel, tx))
            {
                cmd.Parameters.AddWithValue("@id", spesaId);
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new SqlCommand("DELETE FROM SPESA WHERE SpesaId = @id", connDel, tx))
            {
                cmd.Parameters.AddWithValue("@id", spesaId);
                cmd.ExecuteNonQuery();
            }
            RicalcolaSaldi(groupId, connDel, tx);
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }

        ScriviLog("ELIMINA_SPESA", richiedenteUsername, $"Spesa {spesaId} eliminata.");
    }

    public void EliminaSpesa(Spesa spesa, Utente richiedente)
    {
        if (spesa.GetPagante().GetUsername() != richiedente.GetUsername())
            throw new UnauthorizedAccessException("Solo chi ha pagato può eliminare la spesa.");

        using var conn = GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = new SqlCommand(
                "DELETE FROM QUOTA_SPESA WHERE SpesaId = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", spesa.GetSpesaId());
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new SqlCommand(
                "DELETE FROM SPESA WHERE SpesaId = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", spesa.GetSpesaId());
                cmd.ExecuteNonQuery();
            }

            RicalcolaSaldi(spesa.GetGruppo().GetGroupId(), conn, tx);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        ScriviLog("ELIMINA_SPESA", richiedente.GetUsername(),
            $"Spesa {spesa.GetSpesaId()} eliminata.");
    }

    public List<Spesa> GetSpeseGruppo(Guid groupId, Dictionary<string, Utente> utentiCache,
        Gruppo gruppo)
    {
        using var conn = GetConnection();
        using var cmd = new SqlCommand(
            @"SELECT s.SpesaId, s.Causale, s.Importo, s.Data, s.PaganteUsername,
                     s.MetodoDivisione, s.Valuta, s.Note
              FROM SPESA s WHERE s.GroupId = @g ORDER BY s.Data ASC", conn);
        cmd.Parameters.AddWithValue("@g", groupId);

        var spese = new List<Spesa>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string paganteUser = reader.GetString(4);
            if (!utentiCache.TryGetValue(paganteUser, out var pagante))
                continue;

            var spesa = Spesa.FromDb(
                reader.GetGuid(0), reader.GetString(1), reader.GetDecimal(2),
                reader.GetDateTime(3), pagante, gruppo,
                Enum.Parse<MetodoDivisione>(reader.GetString(5)),
                reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7));
            spese.Add(spesa);
        }

        // Lazy load quote
        foreach (var spesa in spese)
            CaricaQuote(spesa, utentiCache, conn);

        return spese;
    }

    private void CaricaQuote(Spesa spesa, Dictionary<string, Utente> utentiCache,
        SqlConnection conn)
    {
        using var cmd = new SqlCommand(
            "SELECT Username, Importo FROM QUOTA_SPESA WHERE SpesaId = @id", conn);
        cmd.Parameters.AddWithValue("@id", spesa.GetSpesaId());
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string uname = reader.GetString(0);
            if (utentiCache.TryGetValue(uname, out var utente))
                spesa.AddQuota(new QuotaSpesa(spesa, utente, reader.GetDecimal(1)));
        }
    }

    private static void AggiornaSaldi(Spesa spesa, List<QuotaSpesa> quote,
        SqlConnection conn, SqlTransaction tx)
    {
        string pagante = spesa.GetPagante().GetUsername();
        Guid groupId = spesa.GetGruppo().GetGroupId();

        foreach (var quota in quote)
        {
            string debitore = quota.GetUtente().GetUsername();
            if (debitore == pagante) continue;

            // Debitore: saldo diminuisce
            using var dCmd = new SqlCommand(
                "UPDATE SALDO SET Valore = Valore - @imp WHERE Username = @u AND GroupId = @g",
                conn, tx);
            dCmd.Parameters.AddWithValue("@imp", quota.GetImporto());
            dCmd.Parameters.AddWithValue("@u", debitore);
            dCmd.Parameters.AddWithValue("@g", groupId);
            dCmd.ExecuteNonQuery();

            // Pagante: saldo aumenta
            using var cCmd = new SqlCommand(
                "UPDATE SALDO SET Valore = Valore + @imp WHERE Username = @u AND GroupId = @g",
                conn, tx);
            cCmd.Parameters.AddWithValue("@imp", quota.GetImporto());
            cCmd.Parameters.AddWithValue("@u", pagante);
            cCmd.Parameters.AddWithValue("@g", groupId);
            cCmd.ExecuteNonQuery();
        }
    }

    private static void RicalcolaSaldi(Guid groupId, SqlConnection conn, SqlTransaction tx)
    {
        using (var cmd = new SqlCommand(
            "UPDATE SALDO SET Valore = 0 WHERE GroupId = @g", conn, tx))
        {
            cmd.Parameters.AddWithValue("@g", groupId);
            cmd.ExecuteNonQuery();
        }

        // Leggi tutti i dati spese in memoria prima di aggiornare
        var righeSpese = new List<(string pagante, string debitore, decimal importo)>();
        using (var cmd = new SqlCommand(
            @"SELECT s.PaganteUsername, qs.Username, qs.Importo
              FROM QUOTA_SPESA qs
              INNER JOIN SPESA s ON qs.SpesaId = s.SpesaId
              WHERE s.GroupId = @g", conn, tx))
        {
            cmd.Parameters.AddWithValue("@g", groupId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                righeSpese.Add((reader.GetString(0), reader.GetString(1), reader.GetDecimal(2)));
        }
        foreach (var (pagante, debitore, importo) in righeSpese)
        {
            if (pagante == debitore) continue;
            UpdateSaldo(groupId, debitore, -importo, conn, tx);
            UpdateSaldo(groupId, pagante, importo, conn, tx);
        }

        // Leggi tutti i rimborsi in memoria prima di aggiornare
        var righeRimborsi = new List<(string debitore, string creditore, decimal importo)>();
        using (var cmd = new SqlCommand(
            "SELECT DebitoreUsername, CreditoreUsername, Importo FROM RIMBORSO WHERE GroupId = @g",
            conn, tx))
        {
            cmd.Parameters.AddWithValue("@g", groupId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                righeRimborsi.Add((reader.GetString(0), reader.GetString(1), reader.GetDecimal(2)));
        }
        foreach (var (debitore, creditore, importo) in righeRimborsi)
        {
            UpdateSaldo(groupId, debitore, importo, conn, tx);
            UpdateSaldo(groupId, creditore, -importo, conn, tx);
        }
    }

    private static void UpdateSaldo(Guid groupId, string username, decimal delta,
        SqlConnection conn, SqlTransaction tx)
    {
        using var cmd = new SqlCommand(
            "UPDATE SALDO SET Valore = Valore + @d WHERE Username = @u AND GroupId = @g",
            conn, tx);
        cmd.Parameters.AddWithValue("@d", delta);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@g", groupId);
        cmd.ExecuteNonQuery();
    }

    private static IStrategiaDivisione ScegliStrategia(MetodoDivisione metodo) => metodo switch
    {
        MetodoDivisione.EQUA => new DivisioneEqua(),
        MetodoDivisione.PERCENTUALE => new DivisionePercentuale(),
        MetodoDivisione.IMPORTI_ESATTI => new DivisioneImportiEsatti(),
        _ => throw new ArgumentOutOfRangeException()
    };
}
