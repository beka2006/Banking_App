namespace BankingApp.Services;

public static class Logger
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "Logs", "app.log");

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Error(string message, Exception ex)
    {
        Write("ERROR", $"{message} | {ex.Message}");
    }

    private static void Write(string level, string message)
    {
        try
        {
            var folder = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
        catch
        {
            // ლოგირების ჩავარდნა არასოდეს შეწყვეტს აპს
        }
    }
}
