using GTA;
using System;
using System.IO;
using System.Windows.Forms;

/// <summary>
/// Player position logger for creating spawn points
/// Logs player position in XML format for Areas.xml configuration
/// </summary>
public class PlayerPositionLogger : Script
{
    private readonly string _logFilePath;
    private readonly string _iniFilePath;
    private Keys _logKey;
    private Keys _shiftDebugKey;
    internal static bool _isLoggingEnabled;
    private bool _isPositionLoggingEnabled;
    private bool _isShiftDebugEnabled;
    private static LoggingMode _loggingMode;
    private static bool _enableBlips = true;
    // Backup debug removed - no longer used

    public enum LoggingMode
    {
        FileOnly,
        Console,
        Both
    }

    public PlayerPositionLogger()
    {
        _logFilePath = "./scripts/GTA/PlyPos.log";
        _iniFilePath = "./scripts/GTA/GTA.ini";
        Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath));

        LoadSettings();
        KeyDown += OnKeyDown;
        Logging.IsEnabled = _isLoggingEnabled;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == _logKey && _isPositionLoggingEnabled)
        {
            LogPlayerPosition();
        }
    }

    public static LoggingMode GetLoggingMode()
    {
        return _loggingMode;
    }

    public static bool GetEnableBlips()
    {
        return _enableBlips;
    }

    private string GetVehicleType(Vehicle vehicle)
    {
        if (vehicle.Model.IsCar || vehicle.Model.IsBike || vehicle.IsRegularAutomobile || vehicle.IsAutomobile)
            return "vehicle";
        if (vehicle.Model.IsHelicopter)
            return "helicopter";
        if (vehicle.Model.IsPlane)
            return "plane";
        if (vehicle.Model.IsBoat || vehicle.Model.IsSubmarine || vehicle.Model.IsAmphibiousCar || 
            vehicle.Model.IsAmphibiousQuadBike || vehicle.Model.IsAmphibiousVehicle)
            return "boat";
        if (vehicle.Model.IsBigVehicle)
            return "largevehicle";
        return "vehicle";
    }

    private void LogPlayerPosition()
    {
        try
        {
            var player = Game.Player.Character;
            string type = player.IsInVehicle() ? GetVehicleType(player.CurrentVehicle) : "ped";

            var pos = player.Position;
            var heading = player.Heading;

            string log = $"  <SpawnPoint type=\"{type}\">\n" +
                         $"    <Position x=\"{pos.X:F2}\" y=\"{pos.Y:F2}\" z=\"{pos.Z:F2}\" />\n" +
                         $"    <Heading>{heading:F2}</Heading>\n" +
                         $"  </SpawnPoint>";

            if (_loggingMode == LoggingMode.Console || _loggingMode == LoggingMode.Both)
                ConsoleHelper.Log(log);
            if (_loggingMode == LoggingMode.FileOnly || _loggingMode == LoggingMode.Both)
                File.AppendAllText(_logFilePath, log + Environment.NewLine);

            Logger.Log.Info($"[PositionLogger] Position logged: {pos}");
            HelperClass.Notification($"{char.ToUpper(type[0]) + type.Substring(1)} position logged.");
        }
        catch (Exception ex)
        {
            Logger.Log.Fatal($"[PositionLogger] Error: {ex.Message}");
            HelperClass.Notification("Failed to log position.");
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(_iniFilePath))
            {
                CreateSampleIniFile();
                Logger.Log.Info("Sample INI created.");
            }

            // Ensure required entries exist in the INI. This checks for keys instead of relying
            // solely on the file existing so missing keys get defaulted without replacing user edits.
            EnsureIniEntries();

            ScriptSettings settings = ScriptSettings.Load(_iniFilePath);

            // Read settings
            string logKeyString = settings.GetValue("Settings", "Position Log Key", "L");
            if (Enum.TryParse(logKeyString, true, out Keys parsedKey))
                _logKey = parsedKey;
            else
                _logKey = Keys.L;

            // Read shift debug key
            string shiftDebugKeyString = settings.GetValue("Settings", "Shift Debug Key", "F9");
            if (Enum.TryParse(shiftDebugKeyString, true, out Keys parsedShiftKey))
                _shiftDebugKey = parsedShiftKey;
            else
                _shiftDebugKey = Keys.F9;

            _isLoggingEnabled = settings.GetValue("Settings", "Logging", true);
            _isPositionLoggingEnabled = settings.GetValue("Settings", "Position Logging", true);
            _isShiftDebugEnabled = settings.GetValue("Settings", "Shift Debug", true);
            _enableBlips = settings.GetValue("Settings", "Enable Blips", true);
            GuardPed.GREETING_TRIGGER_DISTANCE = settings.GetValue("Settings", "Default Greet Distance", 15);
            GuardPed.GREETING_RESET_DISTANCE = settings.GetValue("Settings", "Default Greet Reset Distance", 35);
            // Backup debug removed - legacy keys ignored
            string mode = settings.GetValue("Settings", "Logging Mode", "FileOnly");
            if (!Enum.TryParse(mode, true, out _loggingMode))
                _loggingMode = LoggingMode.FileOnly;

            Logger.Log.Info($"Settings Loaded: LogKey = {_logKey}, ShiftDebugKey = {_shiftDebugKey}, " +
                          $"Logging = {_isLoggingEnabled}, Position Logging = {_isPositionLoggingEnabled}, " +
                          $"Shift Debug = {_isShiftDebugEnabled}, Enable Blips = {_enableBlips}, Mode = {_loggingMode}");
        }
        catch (Exception ex)
        {
            Logger.Log.Fatal($"[PositionLogger] INI load error: {ex.Message}");
            HelperClass.Notification("Failed to load GTA.ini");
        }
    }

    /// <summary>
    /// Ensure the INI file has required entries in the [Settings] section.
    /// If keys are missing, append them with defaults without overwriting the file wholesale.
    /// </summary>
    private void EnsureIniEntries()
    {
        try
        {
            var lines = File.ReadAllLines(_iniFilePath);
            var list = new System.Collections.Generic.List<string>(lines);

            int settingsIndex = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Trim().StartsWith("[Settings]", StringComparison.OrdinalIgnoreCase))
                {
                    settingsIndex = i;
                    break;
                }
            }

            if (settingsIndex == -1)
            {
                // No [Settings] section - append it and defaults
                list.Add("[Settings]");
                settingsIndex = list.Count - 1;
            }

            // Collect keys present in the Settings section
            var presentKeys = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = settingsIndex + 1; i < list.Count; i++)
            {
                var l = list[i].Trim();
                if (l.StartsWith("[") && l.EndsWith("]")) break; // next section
                if (string.IsNullOrWhiteSpace(l) || l.StartsWith(";")) continue;
                var parts = l.Split(new char[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    presentKeys.Add(parts[0].Trim());
                }
            }

            // Required keys and defaults
            var required = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Position Log Key", "L" },
                { "Shift Debug Key", "F9" },
                { "Logging", "true" },
                { "Position Logging", "true" },
                { "Shift Debug", "true" },
                { "Enable Blips", "true" },
                { "Default Greet Distance", "10" },
                { "Default Greet Reset Distance", "35" },
                { "Logging Mode", "FileOnly" },
                // Backup debug keys removed
            };

            bool changed = false;
            int insertPos = settingsIndex + 1;
            foreach (var kv in required)
            {
                if (!presentKeys.Contains(kv.Key))
                {
                    list.Insert(insertPos, $"{kv.Key} = {kv.Value}");
                    insertPos++;
                    changed = true;
                }
            }

            if (changed)
            {
                File.WriteAllLines(_iniFilePath, list);
                Logger.Log.Info("Updated GTA.ini with missing settings defaults.");
            }
        }
        catch (Exception ex)
        {
            Logger.Log.Warning($"EnsureIniEntries failed: {ex.Message}");
        }
    }

    // Backup debug accessors removed

    private void CreateSampleIniFile()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_iniFilePath));
            File.WriteAllText(_iniFilePath,
@"[Settings]
Position Log Key = L
Logging = true
Position Logging = true
Logging Mode = FileOnly
Enable Blips = true
Default Greet Distance = 10
Default Greet Reset Distance = 35
");
            Logger.Log.Info("Created default GTA.ini");
            HelperClass.Notification("Sample INI created.");
        }
        catch (Exception ex)
        {
            Logger.Log.Fatal($"[PositionLogger] Failed to create INI: {ex.Message}");
            HelperClass.Notification("Failed to create GTA.ini");
        }
    }
}
