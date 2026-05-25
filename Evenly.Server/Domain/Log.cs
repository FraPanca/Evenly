namespace Evenly.Server.Domain;

public class Log
{
    private List<VoceLog> _voci = new();

    public List<VoceLog> GetVoci() => _voci;

    public List<VoceLog> GetVoci(DateTime da, DateTime a) =>
        _voci.Where(v => v.GetTimestamp() >= da && v.GetTimestamp() <= a).ToList();

    public void AddVoce(VoceLog voce) => _voci.Add(voce);
}
