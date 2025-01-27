using System;
using System.IO;

public static class Logger
{
    private static readonly string LogFilePath = "./scripts/Logging.log";
    private static bool _enabled;

    static Logger()
    {
        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));
    }

    public static void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }

    public static void Log(string message)
    {
        if (!_enabled)
        {
            return;
        }

        try
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            // If logging fails, you can optionally handle it here (e.g., ignore or show a notification)
        }
    }
}