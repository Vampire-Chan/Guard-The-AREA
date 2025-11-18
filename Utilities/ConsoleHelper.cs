using System;
using System.IO;
using System.Runtime.InteropServices;

/// <summary>
/// Console helper for debugging GTA V scripts
/// Provides console window management and logging output
/// </summary>
public static class ConsoleHelper
{
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    private static bool consoleInitialized = false;
    private static PlayerPositionLogger.LoggingMode _loggingMode;

    /// <summary>
    /// Initialize the console window for debugging output
    /// </summary>
    public static void InitializeConsole()
    {
        if (!consoleInitialized)
        {
            AllocConsole();
            consoleInitialized = true;
            Console.Title = "GTA V Debug Console";
            Console.WriteLine("Console Initialized...");

            // Ensure proper console output binding
            StreamWriter writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(writer);
            Console.SetError(writer);

            // Sync logging mode
            _loggingMode = PlayerPositionLogger.GetLoggingMode();
        }
    }

    /// <summary>
    /// Log a message based on the current logging mode
    /// </summary>
    public static void Log(string message)
    {
        switch (_loggingMode)
        {
            case PlayerPositionLogger.LoggingMode.Console:
                EnsureConsoleInitialized();
                Console.WriteLine(message);
                break;

            case PlayerPositionLogger.LoggingMode.FileOnly:
                Logger.Log.Info(message);
                break;

            case PlayerPositionLogger.LoggingMode.Both:
                EnsureConsoleInitialized();
                Console.WriteLine(message);
                Logger.Log.Info(message);
                break;
        }
    }

    private static void EnsureConsoleInitialized()
    {
        if (!consoleInitialized)
        {
            InitializeConsole();
        }
    }

    /// <summary>
    /// Set the logging mode (console, file, or both)
    /// </summary>
    public static void SetLoggingMode(PlayerPositionLogger.LoggingMode mode)
    {
        _loggingMode = mode;
    }

    /// <summary>
    /// Close and cleanup the console window
    /// </summary>
    public static void CloseConsole()
    {
        if (consoleInitialized)
        {
            Console.SetOut(TextWriter.Null); // Avoid detached output issues
            FreeConsole();
            consoleInitialized = false;
        }
    }
}
