using Microsoft.Data.SqlClient;

namespace Evenly.Server.BusinessControllers;

public abstract class ControllerPersistenza
{
    private readonly string _connectionString;
    private readonly string _logPath;

    protected ControllerPersistenza(string connectionString, string logPath)
    {
        _connectionString = connectionString;
        _logPath = logPath;
    }

    protected SqlConnection GetConnection()
    {
        var conn = new SqlConnection(_connectionString);
        conn.Open();
        return conn;
    }

    protected void ScriviLog(string operazione, string utente, string dettaglio)
    {
        string voce = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] | [{operazione}] | [{utente}] | [{dettaglio}]";
        try
        {
            string dir = Path.GetDirectoryName(_logPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.AppendAllText(_logPath, voce + Environment.NewLine);
        }
        catch
        {
            // Log write failures are non-fatal
        }
    }
}
