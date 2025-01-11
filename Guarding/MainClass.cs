using GTA;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System;
using System.IO;
using System.Windows.Forms;
using GTA.UI;

public class MainScript : Script
{
    public static GuardSpawner _guardSpawner; // GuardSpawner instance
    
    public MainScript()
    {
        _guardSpawner = new GuardSpawner("./scripts/Areas.xml"); // Initialize guard spawner
        Tick += OnTick; // Bind the tick event
        Aborted += OnAbort; // Bind the abort event
    }

    private void OnTick(object sender, EventArgs e) // Called every tick/frame
    {
        Player player = Game.Player; // Get the player object
        // Check player proximity and spawn guards
        _guardSpawner.CheckPlayerProximityAndSpawn(player);
    }

    private void OnAbort(object sender, EventArgs e)
    {
        // Ensure guards are despawned
        _guardSpawner.UnInitialize();

        // Unbind events to prevent memory leaks
        Tick -= OnTick;

        // Log a message for debugging purposes
        // UI.Notify("MainScript cleanup completed.");
    }

}


public class PlayerPositionLogger : Script
{
    private readonly string _logFilePath;
    private readonly string _iniFilePath;
    private Keys _logKey;
    internal static bool _isLoggingEnabled;
    private bool _isPositionLoggingEnabled;

    public PlayerPositionLogger()
    {
        // Set up the log file path and ini file path
        _logFilePath = "./scripts/PlayerPositions.log";
        _iniFilePath = "./scripts/Guarding.ini";
        Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath)); // Ensure the directory exists

        // Load settings from the ini file
        LoadSettings();

        // Bind the key press event
        KeyDown += OnKeyDown;

        // Set the logging enabled state
        Logger.SetEnabled(_isLoggingEnabled);
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == _logKey && _isLoggingEnabled && _isPositionLoggingEnabled)
        {
            LogPlayerPosition();
        }
    }

    private void LogPlayerPosition()
    {
        try
        {
            // Get the player's character
            var player = Game.Player.Character;

            // Determine spawn type based on whether player is in a vehicle
            string spawnType = player.IsInVehicle() ? "vehicle" : "ped";

            // Get the player's position and heading
            var position = player.Position;
            var heading = player.Heading;

            // Prepare the XML log entry with automatic spawn type
            string logEntry = $"  <SpawnPoint type=\"{spawnType}\">\n" +
                             $"    <Position x=\"{position.X:F2}\" y=\"{position.Y:F2}\" z=\"{position.Z:F2}\" />\n" +
                             $"    <Heading>{heading:F2}</Heading>\n" +
                             $"  </SpawnPoint>";

            // Write to the log file
            File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);

            // Notify the user
            Notification.PostTicker($"{char.ToUpper(spawnType[0]) + spawnType.Substring(1)} position logged in XML format!", false);
        }
        catch (Exception ex)
        {
            Notification.PostTicker($"Error logging position: {ex.Message}", false);
        }
    }

    private void LoadSettings()
    {
        // Default settings
        _logKey = Keys.L;
        _isLoggingEnabled = true;
        _isPositionLoggingEnabled = true;

        try
        {
            if (File.Exists(_iniFilePath))
            {
                ScriptSettings settings = ScriptSettings.Load(_iniFilePath);

                // Read the LogKey setting
                string logKeyString = settings.GetValue("Settings", "Position Log Key", "L");
                if (Enum.TryParse(logKeyString, true, out Keys parsedKey))
                {
                    _logKey = parsedKey;
                }

                // Read the LoggingEnabled setting
                _isLoggingEnabled = settings.GetValue("Settings", "Logging", true);
                _isPositionLoggingEnabled = settings.GetValue("Settings", "Position Logging", true);
            }
            else
            {
                // Create a sample ini file with default settings
                CreateSampleIniFile();
            }
        }
        catch (Exception ex)
        {
            Notification.PostTicker($"Error loading settings: {ex.Message}", false);
        }
    }

    private void CreateSampleIniFile()
    {
        try
        {
            using (var file = File.Create(_iniFilePath))
            {
                file.Close();
            }

            // Now save the settings using ScriptSettings
            ScriptSettings settings = ScriptSettings.Load(_iniFilePath);

            settings.SetValue("Settings", "Position Log Key", _logKey.ToString());
            settings.SetValue("Settings", "Position Logging", _isPositionLoggingEnabled);
            settings.SetValue("Settings", "Logging", _isLoggingEnabled);

            settings.Save();

            Notification.PostTicker("Sample INI file created with default settings.", false);
        }
        catch (Exception ex)
        {
            Notification.PostTicker($"Error creating sample INI file: {ex.Message}", false);
        }
    }
}