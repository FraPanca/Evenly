using Evenly.Server.Domain;

namespace Evenly.Server.Domain;

public class Gruppo
{
    private Guid _groupId;
    private string _groupName;
    private string _groupPasswordHash;
    private string _valutaRiferimento;
    private DateTime _dataCreazione;
    private bool _isAttivo;
    private List<Partecipazione> _partecipazioni = new();

    public Gruppo(string groupName, string groupPassword, string valutaRiferimento)
    {
        _groupId = Guid.NewGuid();
        _groupName = groupName;
        _groupPasswordHash = Utente.HashPassword(groupPassword);
        _valutaRiferimento = valutaRiferimento;
        _dataCreazione = DateTime.UtcNow;
        _isAttivo = true;
    }

    public static Gruppo FromDb(Guid groupId, string groupName, string passwordHash,
        string valutaRiferimento, DateTime dataCreazione, bool isAttivo)
    {
        var g = new Gruppo(groupName, "placeholder", valutaRiferimento);
        g._groupId = groupId;
        g._groupPasswordHash = passwordHash;
        g._dataCreazione = dataCreazione;
        g._isAttivo = isAttivo;
        return g;
    }

    public Guid GetGroupId() => _groupId;
    public string GetGroupName() => _groupName;
    public string GetGroupPasswordHash() => _groupPasswordHash;
    public string GetValutaRiferimento() => _valutaRiferimento;
    public DateTime GetDataCreazione() => _dataCreazione;
    public bool IsAttivo() => _isAttivo;

    public void SetGroupName(string name) => _groupName = name;
    public void SetValutaRiferimento(string valuta) => _valutaRiferimento = valuta;
    public void SetIsAttivo(bool attivo) => _isAttivo = attivo;

    public bool VerificaPassword(string password) => Utente.VerifyPassword(password, _groupPasswordHash);

    public LinkInvito GeneraLinkInvito(int utilizziMassimi = 1, int validitaOre = 72)
    {
        return new LinkInvito(this, utilizziMassimi, validitaOre);
    }

    public bool ContainsUtente(Utente utente)
    {
        return _partecipazioni.Any(p => p.GetUtente().GetUsername() == utente.GetUsername());
    }

    internal void AddPartecipazione(Partecipazione p) => _partecipazioni.Add(p);
    internal void RemovePartecipazione(string username) =>
        _partecipazioni.RemoveAll(p => p.GetUtente().GetUsername() == username);
    public List<Partecipazione> GetPartecipazioni() => _partecipazioni;
    public int GetNumeroPartecipanti() => _partecipazioni.Count;
}
