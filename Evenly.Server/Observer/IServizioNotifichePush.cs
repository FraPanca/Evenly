using Evenly.Server.Domain;

namespace Evenly.Server.Observer;

public interface IServizioNotifichePush
{
    void NotificaInserimentoSpesa(Spesa spesa, List<Utente> destinatari);
    void NotificaModificaSpesa(Spesa spesa, List<Utente> destinatari);
    void NotificaEliminazioneSpesa(Guid spesaId, Gruppo gruppo, List<Utente> destinatari);
    void NotificaRimborso(Rimborso rimborso, List<Utente> destinatari);
}
