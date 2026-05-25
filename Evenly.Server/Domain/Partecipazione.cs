using Evenly.Server.Enums;

namespace Evenly.Server.Domain;

public class Partecipazione
{
    private Utente _utente;
    private Gruppo _gruppo;
    private RuoloGruppo _ruolo;
    private DateTime _dataIngresso;

    public Partecipazione(Utente utente, Gruppo gruppo, RuoloGruppo ruolo, DateTime dataIngresso)
    {
        _utente = utente;
        _gruppo = gruppo;
        _ruolo = ruolo;
        _dataIngresso = dataIngresso;
    }

    public Utente GetUtente() => _utente;
    public Gruppo GetGruppo() => _gruppo;
    public RuoloGruppo GetRuolo() => _ruolo;
    public DateTime GetDataIngresso() => _dataIngresso;

    public bool IsAmministratore() => _ruolo == RuoloGruppo.AMMINISTRATORE;

    public void SetRuolo(RuoloGruppo ruolo) => _ruolo = ruolo;
}
