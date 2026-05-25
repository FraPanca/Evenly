using Evenly.Server.Domain;

namespace Evenly.Server.Strategy;

public class DivisioneEqua : IStrategiaDivisione
{
    public List<QuotaSpesa> CalcolaQuote(Spesa spesa, List<Utente> partecipanti,
        Dictionary<Utente, decimal>? parametri = null)
    {
        if (partecipanti.Count == 0)
            throw new ArgumentException("Nessun partecipante.");

        decimal quotaPerTesta = Math.Round(spesa.GetImporto() / partecipanti.Count, 2);
        decimal arrotondamento = spesa.GetImporto() - quotaPerTesta * partecipanti.Count;

        var quote = new List<QuotaSpesa>();
        for (int i = 0; i < partecipanti.Count; i++)
        {
            decimal importo = i == 0 ? quotaPerTesta + arrotondamento : quotaPerTesta;
            quote.Add(new QuotaSpesa(spesa, partecipanti[i], importo));
        }
        return quote;
    }
}
