namespace Evenly.Server.Domain;

public class Rimborso
{
    private Guid _rimborsoId;
    private Utente _debitore;
    private Utente _creditore;
    private decimal _importo;
    private Gruppo _gruppo;
    private DateTime _data;

    public Rimborso(Utente debitore, Utente creditore, decimal importo, Gruppo gruppo, DateTime data)
    {
        _rimborsoId = Guid.NewGuid();
        _debitore = debitore;
        _creditore = creditore;
        _importo = importo;
        _gruppo = gruppo;
        _data = data;
    }

    public static Rimborso FromDb(Guid rimborsoId, Utente debitore, Utente creditore,
        decimal importo, Gruppo gruppo, DateTime data)
    {
        var r = new Rimborso(debitore, creditore, importo, gruppo, data);
        r._rimborsoId = rimborsoId;
        return r;
    }

    public Guid GetRimborsoId() => _rimborsoId;
    public Utente GetDebitore() => _debitore;
    public Utente GetCreditore() => _creditore;
    public decimal GetImporto() => _importo;
    public Gruppo GetGruppo() => _gruppo;
    public DateTime GetData() => _data;
}
