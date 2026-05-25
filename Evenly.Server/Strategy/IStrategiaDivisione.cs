using Evenly.Server.Domain;

namespace Evenly.Server.Strategy;

public interface IStrategiaDivisione
{
    List<QuotaSpesa> CalcolaQuote(Spesa spesa, List<Utente> partecipanti,
        Dictionary<Utente, decimal>? parametri = null);
}
