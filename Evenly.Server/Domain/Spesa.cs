using Evenly.Server.Enums;

namespace Evenly.Server.Domain;

public class Spesa
{
    private Guid _spesaId;
    private string _causale;
    private decimal _importo;
    private DateTime _data;
    private Utente _pagante;
    private Gruppo _gruppo;
    private MetodoDivisione _metodoDivisione;
    private string _valuta;
    private string? _note;
    private List<QuotaSpesa> _quote = new();

    public Spesa(string causale, decimal importo, DateTime data, Utente pagante,
        Gruppo gruppo, MetodoDivisione metodoDivisione, string? valuta = null, string? note = null)
    {
        _spesaId = Guid.NewGuid();
        _causale = causale;
        _importo = importo;
        _data = data;
        _pagante = pagante;
        _gruppo = gruppo;
        _metodoDivisione = metodoDivisione;
        _valuta = valuta ?? gruppo.GetValutaRiferimento();
        _note = note;
    }

    public static Spesa FromDb(Guid spesaId, string causale, decimal importo, DateTime data,
        Utente pagante, Gruppo gruppo, MetodoDivisione metodoDivisione, string valuta, string? note)
    {
        var s = new Spesa(causale, importo, data, pagante, gruppo, metodoDivisione, valuta, note);
        s._spesaId = spesaId;
        return s;
    }

    public Guid GetSpesaId() => _spesaId;
    public string GetCausale() => _causale;
    public decimal GetImporto() => _importo;
    public DateTime GetData() => _data;
    public Utente GetPagante() => _pagante;
    public Gruppo GetGruppo() => _gruppo;
    public MetodoDivisione GetMetodoDivisione() => _metodoDivisione;
    public string GetValuta() => _valuta;
    public string? GetNote() => _note;
    public List<QuotaSpesa> GetQuote() => _quote;

    public void SetCausale(string causale) => _causale = causale;
    public void SetImporto(decimal importo) => _importo = importo;
    public void SetData(DateTime data) => _data = data;
    public void SetMetodoDivisione(MetodoDivisione metodo) => _metodoDivisione = metodo;
    public void SetNote(string? note) => _note = note;
    public void SetQuote(List<QuotaSpesa> quote) => _quote = quote;
    public void AddQuota(QuotaSpesa quota) => _quote.Add(quota);
}
