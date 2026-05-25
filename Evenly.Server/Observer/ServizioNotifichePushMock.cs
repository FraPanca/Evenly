using Evenly.Server.Domain;

namespace Evenly.Server.Observer;

public class ServizioNotifichePushMock : IServizioNotifichePush
{
    private readonly ILogger<ServizioNotifichePushMock>? _logger;

    public ServizioNotifichePushMock(ILogger<ServizioNotifichePushMock>? logger = null)
    {
        _logger = logger;
    }

    public void NotificaInserimentoSpesa(Spesa spesa, List<Utente> destinatari) =>
        Log($"[NOTIFICA] Nuova spesa '{spesa.GetCausale()}' per {destinatari.Count} utenti.");

    public void NotificaModificaSpesa(Spesa spesa, List<Utente> destinatari) =>
        Log($"[NOTIFICA] Spesa '{spesa.GetCausale()}' modificata per {destinatari.Count} utenti.");

    public void NotificaEliminazioneSpesa(Guid spesaId, Gruppo gruppo, List<Utente> destinatari) =>
        Log($"[NOTIFICA] Spesa {spesaId} eliminata in '{gruppo.GetGroupName()}' per {destinatari.Count} utenti.");

    public void NotificaRimborso(Rimborso rimborso, List<Utente> destinatari) =>
        Log($"[NOTIFICA] Rimborso di {rimborso.GetImporto()} da {rimborso.GetDebitore().GetUsername()} a {rimborso.GetCreditore().GetUsername()}.");

    private void Log(string msg)
    {
        if (_logger != null)
            _logger.LogInformation(msg);
        else
            Console.WriteLine(msg);
    }
}
