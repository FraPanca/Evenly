namespace Evenly.Server.Domain;

public class QuotaSpesa
{
    private Spesa _spesa;
    private Utente _utente;
    private decimal _importo;

    public QuotaSpesa(Spesa spesa, Utente utente, decimal importo)
    {
        _spesa = spesa;
        _utente = utente;
        _importo = importo;
    }

    public Spesa GetSpesa() => _spesa;
    public Utente GetUtente() => _utente;
    public decimal GetImporto() => _importo;

    public void SetImporto(decimal importo) => _importo = importo;
}
