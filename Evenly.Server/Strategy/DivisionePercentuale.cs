using Evenly.Server.Domain;

namespace Evenly.Server.Strategy;

public class DivisionePercentuale : IStrategiaDivisione
{
    public List<QuotaSpesa> CalcolaQuote(Spesa spesa, List<Utente> partecipanti,
        Dictionary<Utente, decimal>? parametri = null)
    {
        if (parametri == null || parametri.Count == 0)
            throw new ArgumentException("Le percentuali sono obbligatorie per la divisione percentuale.");

        decimal totalePercentuale = parametri.Values.Sum();
        if (Math.Abs(totalePercentuale - 100m) > 0.01m)
            throw new ArgumentException($"Le percentuali devono sommare a 100 (attuale: {totalePercentuale}).");

        return partecipanti.Select(u =>
        {
            decimal perc = parametri.TryGetValue(u, out var p) ? p : 0m;
            decimal importo = Math.Round(spesa.GetImporto() * perc / 100m, 2);
            return new QuotaSpesa(spesa, u, importo);
        }).ToList();
    }
}
