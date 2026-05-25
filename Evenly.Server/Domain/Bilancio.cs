namespace Evenly.Server.Domain;

// Not persisted — calculated dynamically
public class Bilancio
{
    private Gruppo _gruppo;
    private List<VoceBilancio> _vociBilancio = new();

    public Bilancio(Gruppo gruppo)
    {
        _gruppo = gruppo;
    }

    public Gruppo GetGruppo() => _gruppo;
    public List<VoceBilancio> GetVociBilancio() => _vociBilancio;

    public void AddVoce(VoceBilancio voce) => _vociBilancio.Add(voce);
    public void Clear() => _vociBilancio.Clear();
}
