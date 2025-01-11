using GTA;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System;
using System.IO;
using System.Windows.Forms;

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

    public PlayerPositionLogger()
    {
        // Set up the log file path
        _logFilePath = "./scripts/PlayerPositions.log";
        Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath)); // Ensure the directory exists

        // Bind the key press event
        KeyDown += OnKeyDown;

        }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.L) // Replace 'L' with your preferred key
        {
            LogPlayerPosition();
        }
    }

    private void LogPlayerPosition()
    {
        try
        {
            // Get the player's position and heading
            var player = Game.Player.Character;
            var position = player.Position;
            var heading = player.Heading;

            // Prepare the XML log entry
            string logEntry = $"  <SpawnPoint>\n" +
                              $"    <Position x=\"{position.X:F2}\" y=\"{position.Y:F2}\" z=\"{position.Z:F2}\" />\n" +
                              $"    <Heading>{heading:F2}</Heading>\n" +
                              $"  </SpawnPoint>";

            // Write to the log file
            File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);

            // Notify the user
            // UI.Notify("Position logged in XML format!");
        }
        catch (Exception ex)
        {
            // UI.Notify($"Error logging position: {ex.Message}");
        }
    }

}

