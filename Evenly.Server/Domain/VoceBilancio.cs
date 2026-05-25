namespace Evenly.Server.Domain;

public class VoceBilancio
{
    private Utente _creditore;
    private Utente _debitore;
    private decimal _importo;

    public VoceBilancio(Utente creditore, Utente debitore, decimal importo)
    {
        _creditore = creditore;
        _debitore = debitore;
        _importo = importo;
    }

    public Utente GetCreditore() => _creditore;
    public Utente GetDebitore() => _debitore;
    public decimal GetImporto() => _importo;

    // Returns the counterpart from the perspective of a given user
    public Utente GetControparte(Utente utente)
    {
        return utente.GetUsername() == _debitore.GetUsername() ? _creditore : _debitore;
    }
}
