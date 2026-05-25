using Evenly.Server.Enums;

namespace Evenly.Server.Domain;

public class LinkInvito
{
    private Guid _linkId;
    private Gruppo _gruppo;
    private string _token;
    private StatoLink _stato;
    private int _utilizziResidui;
    private DateTime _dataScadenza;

    public LinkInvito(Gruppo gruppo, int utilizziMassimi = 1, int validitaOre = 72)
    {
        _linkId = Guid.NewGuid();
        _gruppo = gruppo;
        _token = Guid.NewGuid().ToString("N");
        _stato = StatoLink.VALIDO;
        _utilizziResidui = utilizziMassimi;
        _dataScadenza = DateTime.UtcNow.AddHours(validitaOre);
    }

    public static LinkInvito FromDb(Guid linkId, Gruppo gruppo, string token,
        StatoLink stato, int utilizziResidui, DateTime dataScadenza)
    {
        var l = new LinkInvito(gruppo);
        l._linkId = linkId;
        l._token = token;
        l._stato = stato;
        l._utilizziResidui = utilizziResidui;
        l._dataScadenza = dataScadenza;
        return l;
    }

    public Guid GetLinkId() => _linkId;
    public Gruppo GetGruppo() => _gruppo;
    public string GetToken() => _token;
    public StatoLink GetStato() => _stato;
    public int GetUtilizziResidui() => _utilizziResidui;
    public DateTime GetDataScadenza() => _dataScadenza;

    public bool IsValido() => _stato == StatoLink.VALIDO
        && _utilizziResidui > 0
        && DateTime.UtcNow <= _dataScadenza;

    public bool IsUtilizzato() => _stato == StatoLink.SCADUTO || _utilizziResidui <= 0;

    public void Utilizza()
    {
        if (!IsValido()) throw new InvalidOperationException("Link non valido o scaduto.");
        _utilizziResidui--;
        if (_utilizziResidui <= 0) _stato = StatoLink.SCADUTO;
    }
}
