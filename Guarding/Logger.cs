using System;
using System.IO;

public static class Logger
{
    private static readonly string LogFilePath = "./scripts/Logging.log";

    static Logger()
    {
        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));
    }

    public static void Log(string message)
    {
        try
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // If logging fails, you can optionally handle it here (e.g., ignore or show a notification)
        }
    }
}
