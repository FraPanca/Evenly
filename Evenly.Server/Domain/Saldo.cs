using Evenly.Server.Enums;

namespace Evenly.Server.Domain;

public class Saldo
{
    private Utente _utente;
    private Gruppo _gruppo;
    private decimal _valore;
    private string _valuta;

    public Saldo(Utente utente, Gruppo gruppo, decimal valore, string? valuta = null)
    {
        _utente = utente;
        _gruppo = gruppo;
        _valore = valore;
        _valuta = valuta ?? gruppo.GetValutaRiferimento();
    }

    public Utente GetUtente() => _utente;
    public Gruppo GetGruppo() => _gruppo;
    public decimal GetValore() => _valore;
    public string GetValuta() => _valuta;

    public StatoSaldo GetStato()
    {
        if (_valore > 0) return StatoSaldo.CREDITO;
        if (_valore < 0) return StatoSaldo.DEBITO;
        return StatoSaldo.PARI;
    }

    public void SetValore(decimal valore) => _valore = valore;

    public void CalcolaSaldo(List<Spesa> spese, List<Rimborso> rimborsi)
    {
        decimal totale = 0m;
        string username = _utente.GetUsername();

        foreach (var spesa in spese)
        {
            if (spesa.GetPagante().GetUsername() == username)
            {
                // Ha pagato: credito per le quote degli altri
                foreach (var quota in spesa.GetQuote())
                {
                    if (quota.GetUtente().GetUsername() != username)
                        totale += quota.GetImporto();
                }
            }
            else
            {
                // Non ha pagato: debito per la propria quota
                var quotaPersonale = spesa.GetQuote()
                    .FirstOrDefault(q => q.GetUtente().GetUsername() == username);
                if (quotaPersonale != null)
                    totale -= quotaPersonale.GetImporto();
            }
        }

        foreach (var rimborso in rimborsi)
        {
            if (rimborso.GetDebitore().GetUsername() == username)
                totale += rimborso.GetImporto(); // ho pagato un debito
            else if (rimborso.GetCreditore().GetUsername() == username)
                totale -= rimborso.GetImporto(); // ho ricevuto un rimborso
        }

        _valore = totale;
    }
}
