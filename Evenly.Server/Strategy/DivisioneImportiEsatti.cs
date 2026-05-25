using Evenly.Server.Domain;

namespace Evenly.Server.Strategy;

public class DivisioneImportiEsatti : IStrategiaDivisione
{
    public List<QuotaSpesa> CalcolaQuote(Spesa spesa, List<Utente> partecipanti,
        Dictionary<Utente, decimal>? parametri = null)
    {
        if (parametri == null || parametri.Count == 0)
            throw new ArgumentException("Gli importi esatti sono obbligatori.");

        decimal totale = parametri.Values.Sum();
        if (Math.Abs(totale - spesa.GetImporto()) > 0.01m)
            throw new ArgumentException(
                $"La somma degli importi ({totale}) non corrisponde all'importo della spesa ({spesa.GetImporto()}).");

        return partecipanti.Select(u =>
        {
            decimal importo = parametri.TryGetValue(u, out var imp) ? imp : 0m;
            return new QuotaSpesa(spesa, u, importo);
        }).ToList();
    }
}
