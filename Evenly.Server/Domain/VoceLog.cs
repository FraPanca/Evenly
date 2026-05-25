namespace Evenly.Server.Domain;

public class VoceLog
{
    private DateTime _timestamp;
    private string _operazione;
    private string _utente;
    private string _dettaglio;

    public VoceLog(string operazione, string utente, string dettaglio)
    {
        _timestamp = DateTime.UtcNow;
        _operazione = operazione;
        _utente = utente;
        _dettaglio = dettaglio;
    }

    public DateTime GetTimestamp() => _timestamp;
    public string GetOperazione() => _operazione;
    public string GetUtente() => _utente;
    public string GetDettaglio() => _dettaglio;

    // Format: [TIMESTAMP] | [OPERAZIONE] | [UTENTE] | [DETTAGLIO]
    public override string ToString() =>
        $"[{_timestamp:yyyy-MM-dd HH:mm:ss}] | [{_operazione}] | [{_utente}] | [{_dettaglio}]";
}
