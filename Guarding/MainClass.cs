using GTA;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System;
using System.IO;
using System.Windows.Forms;
using GTA.UI;
using GTA.Native;
using GTA.Math;
using System.Collections.Generic;
using System.Linq;
using Guarding.DispatchSystem;

public class GuardManager : Script
{
    public static GuardSpawner _guardSpawner; // GuardSpawner instance

    public static List<string> scenarios = new()
    {
        "WORLD_HUMAN_AA_COFFEE",
        "WORLD_HUMAN_AA_SMOKE",
        "WORLD_HUMAN_BINOCULARS",
        "WORLD_HUMAN_CLIPBOARD",
        "WORLD_HUMAN_COP_IDLES",
        "WORLD_HUMAN_DRINKING",
        "WORLD_HUMAN_GUARD_PATROL",
        "WORLD_HUMAN_GUARD_STAND",
        "WORLD_HUMAN_GUARD_STAND_ARMY",
        //"WORLD_HUMAN_LEANING",
        //"WORLD_HUMAN_SEAT_STEPS",
        //"WORLD_HUMAN_SEAT_WALL",
        //"WORLD_HUMAN_SEAT_WALL_EATING",
        //"WORLD_HUMAN_SEAT_WALL_TABLET",
        "WORLD_HUMAN_SECURITY_SHINE_TORCH",
        "WORLD_HUMAN_SMOKING",
        "WORLD_HUMAN_STAND_MOBILE"
    };

    public GuardManager()    
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
public class GateManager : Script
{
    public GateManager()
    {
        Tick += OnTick;
    }

    // Door heading states: 0 (closed), 1 (opened), -1 (opened but weird)
    // States: Locked doors have 0 as heading (closed), other states are 1 (unlocked)

    private void OnTick(object sender, EventArgs e)
    {
        if (TryGetDoorInFront(out var doorProp, out var heading, out var isLocked))
        {
            if (isLocked)
            {
                // Unlock the door and ensure a valid heading is set
                float newHeading = heading == 0 ? 0 : heading; // Default to 0 if locked and closed
               // GTA.UI.Screen.ShowSubtitle($"Unlocking Door: {doorProp.Model.Hash}, Heading: {newHeading}, Locked: {isLocked}");
                Function.Call(Hash.SET_STATE_OF_CLOSEST_DOOR_OF_TYPE, doorProp.Model.Hash, doorProp.Position.X, doorProp.Position.Y, doorProp.Position.Z, false, newHeading, false);
            }
            else
            {
               // GTA.UI.Screen.ShowSubtitle($"Door already unlocked: {doorProp.Model.Hash}, Heading: {heading}, Locked: {isLocked}");
            }
        }
     //   HelperClass.Subtitle("Gate Manager Active");
        string copHash = "Not Found";
        string armouredHash = "Not Found";

        foreach (var mwops in World.GetAllPeds().ToList())
        {
            if (mwops.Model == "G_F_Y_Families_01")
            {
                copHash = $"{mwops.RelationshipGroup.Hash}";
            }
            else if (mwops.Model == PedHash.Michael)
            {
                armouredHash = $"{mwops.RelationshipGroup.Hash}";
            }
        }

        // Display both in a single subtitle
       // HelperClass.Subtitle($"Fam Hash: {copHash} | Ply Hash: {armouredHash} | Type: {new RelationshipGroup(StringHash.AtStringHash(copHash)).GetRelationshipBetweenGroups(armouredHash)}. {new RelationshipGroup(StringHash.AtStringHash(armouredHash)).GetRelationshipBetweenGroups(copHash)}");

    }

    private bool TryGetDoorInFront(out Prop doorProp, out float heading, out bool isLocked)
    {
        Vector3 position = Game.Player.Character.Position;
        Vector3 position2 = position + Game.Player.Character.ForwardVector * 1f;
        float radius = 100f;

        Prop[] nearbyProps = World.GetNearbyProps(position2, radius);
        foreach (Prop prop in nearbyProps)
        {
            if (IsDoor(prop, out heading, out isLocked))
            {
                doorProp = prop;
                return true;
            }
        }

        // If no door is found, initialize out parameters and return false
        doorProp = null;
        heading = 0f;
        isLocked = false;
        return false;
    }

    private bool IsDoor(Prop prop, out float heading, out bool isLocked)
    {
        // Initialize output parameters
        heading = 0f;
        isLocked = false;

        if (prop == null)
        {
            return false;
        }

        int hash = prop.Model.Hash;

        // Use OutputArgument to capture the results of the native function
        OutputArgument lockedStatus = new OutputArgument();
        OutputArgument doorHeading = new OutputArgument();

        // Call the native function to get the door state
        Function.Call(Hash.GET_STATE_OF_CLOSEST_DOOR_OF_TYPE, hash, prop.Position.X, prop.Position.Y, prop.Position.Z, lockedStatus, doorHeading);

        // Extract the results from the OutputArgument objects
        isLocked = lockedStatus.GetResult<bool>();
        heading = doorHeading.GetResult<float>();

        // Additional checks can be added to determine if the prop qualifies as a door
        return true;
    }
}

public class PlayerPositionLogger : Script
{
    private readonly string _logFilePath;
    private readonly string _iniFilePath;
    private Keys _logKey;
    internal static bool _isLoggingEnabled;
    private bool _isPositionLoggingEnabled;
    internal static bool _isNewHeliDispatchEnabled;

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
        if (e.KeyCode == _logKey  && _isPositionLoggingEnabled)
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

            // Determine spawn type based on player's state
            string spawnType;

            if (player.IsInVehicle())
            {
                var vehicle = player.CurrentVehicle;

                // Determine the type of the vehicle
                if (vehicle.Model.IsCar || vehicle.Model.IsBike ||vehicle.IsRegularAutomobile || vehicle.IsAutomobile)
                {
                    spawnType = "vehicle";
                }
                else if (vehicle.Model.IsHelicopter || vehicle.Model.IsPlane)
                {
                    spawnType = "helicopter";
                }
                else if (vehicle.Model.IsBoat || vehicle.Model.IsSubmarine || vehicle.Model.IsAmphibiousCar || vehicle.Model.IsAmphibiousQuadBike || vehicle.Model.IsAmphibiousVehicle)
                {
                    spawnType = "boat";
                }
                else
                {
                    spawnType = "vehicle"; // Fallback for unclassified vehicles
                }
            }
            else
            {
                spawnType = "ped";
            }

            // Check if the player is in an interior
            bool isInInterior = false;
            //Function.Call(Hash.)

            // Get the player's position and heading
            var position = player.Position;
            var heading = player.Heading;
            var seat = (int)player.SeatIndex;

            // Prepare the XML log entry with spawn type and interior status
            string logEntry = $"  <SpawnPoint type=\"{spawnType}\">\n" +
                              $"    <Position x=\"{position.X:F2}\" y=\"{position.Y:F2}\" z=\"{position.Z:F2}\" />\n" +
                              $"    <Heading>{heading:F2}</Heading>\n" +
                              $"  </SpawnPoint>";

            // Append seat index comment only if player is in a vehicle
            if (player.IsInVehicle())
            {
                logEntry += $" <!-- MountedVehicleModel seat={seat} (Place this in Guards.xml) -->";
            }

            // Output log entry
            Console.WriteLine(logEntry);

            // Write to the log file
            File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);

            // Notify the user
            Notification.PostTicker($"{char.ToUpper(spawnType[0]) + spawnType.Substring(1)} position logged in XML format (Interior: {isInInterior})!", false);
        }
        catch (Exception ex)
        {
            // Handle exceptions and notify the user
            Notification.PostTicker($"Error logging position: {ex.Message}", true);
        }
    }

    private void LoadSettings()
    {
        // Default settings
        _logKey = Keys.L;
        _isLoggingEnabled = true;
        _isPositionLoggingEnabled = true;
        _isNewHeliDispatchEnabled = true;
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
                _isNewHeliDispatchEnabled = settings.GetValue("Settings", "New Dispatch Heli System (BETA)", false);
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
            settings.SetValue("Settings", "New Dispatch Heli System (BETA)", _isNewHeliDispatchEnabled);

            settings.Save();

            Notification.PostTicker("Sample INI file created with default settings.", false);
        }
        catch (Exception ex)
        {
            Notification.PostTicker($"Error creating sample INI file: {ex.Message}", false);
        }
    }
}