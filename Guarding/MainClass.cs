using GTA;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System;
using System.IO;
using System.Windows.Forms;

public class MainScript
{
    public static GuardSpawner _guardSpawner; // GuardSpawner instance
 
    public void OnTick() // Called every tick/frame
    {
        Player player = Game.Player; // Get the player object

        // Check player proximity and spawn guards
        _guardSpawner.CheckPlayerProximityAndSpawn(player);
        Logger.Log("Checking player proximity and spawning guards...");
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

            // Prepare the log entry
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Position: X: {position.X:F2}, Y: {position.Y:F2}, Z: {position.Z:F2}, Heading: {heading:F2}";

            // Write to the log file
            File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);

            // Notify the user
          //  UI.Notify("Position logged!");
        }
        catch (Exception ex)
        {
         //   UI.Notify($"Error logging position: {ex.Message}");
        }
    }
}

