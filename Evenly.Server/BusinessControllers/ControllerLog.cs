namespace Evenly.Server.BusinessControllers;

public class ControllerLog : ControllerPersistenza
{
    private readonly string _logPath;

    public ControllerLog(string connectionString, string logPath)
        : base(connectionString, logPath)
    {
        _logPath = logPath;
    }

    public List<string> GetVoci(DateTime da, DateTime a)
    {
        if (!File.Exists(_logPath)) return new List<string>();

        return File.ReadAllLines(_logPath)
            .Where(line => {
                if (line.Length < 21) return false;
                if (DateTime.TryParse(line[1..20], out var ts))
                    return ts >= da && ts <= a;
                return false;
            })
            .ToList();
    }

    public List<string> GetTutteLeVoci()
    {
        if (!File.Exists(_logPath)) return new List<string>();
        return File.ReadAllLines(_logPath).ToList();
    }
}
