using GTA.Math;
using System;
using System.Collections.Generic;
using System.Linq;

public class Area
{
    public string Name { get; set; }
    public string Model { get; set; }
    public string DefaultScenario { get; set; }
    public Scenarios Scenarios { get; set; } // Now stores the scenario list from `Scenarios` class
    public List<string> Hate { get; set; }
    public List<string> Dislike { get; set; }
    public string Respect { get; set; }
    public List<string> Like { get; set; }
    public bool RelationshipOverride { get; set; }
    public List<GuardSpawnPoint> SpawnPoints { get; set; }
    
    // Cached centroid for performance (recalculated when spawn points change)
    private Vector3? _cachedCentroid = null;

    // Shift system properties
    public string ShiftDuration { get; set; }
    public List<int> ShiftStartHours { get; private set; } = new List<int>();
    public bool ShiftEnabled { get; set; } = false;
    
    // Backup system properties
    public bool AllowsBackup { get; set; } = true; // Default: backup enabled
    public int DailyCharges { get; set; } = 0; // Daily maintenance fee
    public BackupFeesConfig BackupFees { get; set; } = new BackupFeesConfig(); // Backup costs and cooldowns
    // Per-area override for wave spawn interval (seconds). If 0, use global default.
    public int BackupSpawnIntervalSeconds { get; set; } = 0;
    
    // Track the last shift change for this area (hour + minute for precision)
    private int _lastShiftHour = -1;
    private int _lastShiftMinute = -1;
    
    // Track if departure and arrival have been triggered for current shift to prevent multiple executions
    private int _lastDepartureShiftHour = -1;
    private int _lastArrivalShiftHour = -1;

    // Shift window constants - 15 minutes before and 15 minutes after shift time
    private const int SHIFT_PREPARATION_MINUTES = 15;
    private const int SHIFT_COMPLETION_MINUTES = 15;
    private const int OVERLAP_MINUTES = 5; // Time when both old and new guards are present

    public Area(string name, string model, string defaultScenario, List<string> hate, List<string> dislike, string respect, List<string> like, Scenarios scenarios, bool relationshipOverride = false)
    {
        Name = name;
        Model = model;
        DefaultScenario = defaultScenario;
        Scenarios = scenarios; // This will be filled from `Scenarios`
        SpawnPoints = new List<GuardSpawnPoint>();
        Hate = hate;

        Dislike = dislike;
        Respect = respect;
        Like = like;
        RelationshipOverride = relationshipOverride;
    }

    /// <summary>
    /// Parse the shift duration string and populate ShiftStartHours
    /// Format: "6-12,12-18,18-2,2-6"
    /// </summary>
    public void ParseShiftDuration()
    {
        if (string.IsNullOrEmpty(ShiftDuration))
        {
            ShiftEnabled = false;
            return;
        }

        ShiftStartHours.Clear();
        string[] shifts = ShiftDuration.Split(',');
        
        foreach (string shift in shifts)
        {
            string[] times = shift.Trim().Split('-');
            if (times.Length == 2 && int.TryParse(times[0], out int startHour))
            {
                if (startHour >= 0 && startHour <= 23)
                {
                    ShiftStartHours.Add(startHour);
                }
            }
        }

        ShiftEnabled = ShiftStartHours.Count > 0;
        Logger.Log.Info($"Area {Name}: Parsed {ShiftStartHours.Count} shift start hours: {string.Join(", ", ShiftStartHours)}");
    }

    /// <summary>
    /// Check if the current time falls within any defined shift period
    /// </summary>
    /// <param name="currentHour">Current game hour (0-23)</param>
    /// <returns>True if current time is within a defined shift period</returns>
    public bool IsWithinShiftPeriod(int currentHour)
    {
        if (!ShiftEnabled || ShiftStartHours.Count == 0)
            return false;

        string[] shifts = ShiftDuration.Split(',');
        
        foreach (string shift in shifts)
        {
            string[] times = shift.Trim().Split('-');
            if (times.Length == 2 && 
                int.TryParse(times[0], out int startHour) && 
                int.TryParse(times[1], out int endHour))
            {
                // Handle same-day shifts (e.g., 8-20)
                if (startHour <= endHour)
                {
                    if (currentHour >= startHour && currentHour < endHour)
                        return true;
                }
                // Handle overnight shifts (e.g., 20-8)
                else
                {
                    if (currentHour >= startHour || currentHour < endHour)
                        return true;
                }
            }
        }
        
        return false;
    }

    /// <summary>
    /// Check if it's time for a shift change (within 20-minute window)
    /// </summary>
    /// <param name="currentHour">Current game hour (0-23)</param>
    /// <param name="currentMinute">Current game minute (0-59)</param>
    /// <returns>True if a shift change should occur</returns>
    public bool IsShiftTime(int currentHour, int currentMinute)
    {
        if (!ShiftEnabled || ShiftStartHours.Count == 0)
            return false;

        // Check if we've already processed this exact hour and minute
        if (_lastShiftHour == currentHour && _lastShiftMinute == currentMinute)
            return false;

        // Check if current hour is a shift start hour
        bool isShiftStartHour = ShiftStartHours.Contains(currentHour);
        
        if (isShiftStartHour)
        {
            // Check if we are within the preparation or completion window
            if (currentMinute >= (60 - SHIFT_PREPARATION_MINUTES) || currentMinute <= SHIFT_COMPLETION_MINUTES)
            {
                _lastShiftHour = currentHour;
                _lastShiftMinute = currentMinute;
                Logger.Log.Info($"Area {Name}: Shift change triggered at hour {currentHour}, minute {currentMinute}");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if we're in the preparation phase (10 minutes before shift)
    /// </summary>
    /// <param name="currentHour">Current game hour (0-23)</param>
    /// <param name="currentMinute">Current game minute (0-59)</param>
    /// <returns>True if in preparation phase</returns>
    public bool IsShiftPreparationTime(int currentHour, int currentMinute)
    {
        if (!ShiftEnabled || ShiftStartHours.Count == 0)
            return false;

        bool isShiftStartHour = ShiftStartHours.Contains(currentHour);
        
        if (isShiftStartHour)
        {
            // 10 minutes before the hour (minutes 50-59)
            return currentMinute >= (60 - SHIFT_PREPARATION_MINUTES);
        }

        return false;
    }

    /// <summary>
    /// Check if we're in the completion phase (10 minutes after shift start)
    /// </summary>
    /// <param name="currentHour">Current game hour (0-23)</param>
    /// <param name="currentMinute">Current game minute (0-59)</param>
    /// <returns>True if in completion phase</returns>
    public bool IsShiftCompletionTime(int currentHour, int currentMinute)
    {
        if (!ShiftEnabled || ShiftStartHours.Count == 0)
            return false;

        bool isShiftStartHour = ShiftStartHours.Contains(currentHour);
        
        if (isShiftStartHour)
        {
            // 10 minutes after the hour (minutes 0-10)
            return currentMinute <= SHIFT_COMPLETION_MINUTES;
        }

        return false;
    }

    /// <summary>
    /// Check if we're in the overlap period (both old and new guards should be present)
    /// </summary>
    /// <param name="currentHour">Current game hour (0-23)</param>
    /// <param name="currentMinute">Current game minute (0-59)</param>
    /// <returns>True if in overlap period</returns>
    public bool IsShiftOverlapTime(int currentHour, int currentMinute)
    {
        if (!ShiftEnabled || ShiftStartHours.Count == 0)
            return false;

        bool isShiftStartHour = ShiftStartHours.Contains(currentHour);
        
        if (isShiftStartHour)
        {
            // Overlap occurs in the first few minutes after shift start
            return currentMinute <= OVERLAP_MINUTES;
        }

        return false;
    }

    /// <summary>
    /// Check if this is the departure start time (old guards should start leaving)
    /// Only returns true ONCE per shift to prevent multiple departure triggers
    /// </summary>
    /// <param name="currentHour">Current game hour (0-23)</param>
    /// <param name="currentMinute">Current game minute (0-59)</param>
    /// <returns>True if departure should start</returns>
    public bool ShouldStartDeparture(int currentHour, int currentMinute)
    {
        if (!ShiftEnabled || ShiftStartHours.Count == 0)
            return false;

        bool isShiftStartHour = ShiftStartHours.Contains(currentHour);
        
        if (isShiftStartHour)
        {
            // Check if we already triggered departure for this shift hour
            if (_lastDepartureShiftHour == currentHour)
                return false; // Already processed departure for this shift
            
            // Start departure 15 minutes before shift time (minutes 45-59)
            if (currentMinute >= (60 - SHIFT_PREPARATION_MINUTES))
            {
                // Mark this shift hour as processed for departure
                _lastDepartureShiftHour = currentHour;
                Logger.Log.Info($"Area {Name}: Departure triggered for shift hour {currentHour} (will not trigger again this hour)");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if this is the arrival start time (new guards should start arriving)
    /// Only returns true ONCE per shift to prevent multiple arrival triggers
    /// </summary>
    /// <param name="currentHour">Current game hour (0-23)</param>
    /// <param name="currentMinute">Current game minute (0-59)</param>
    /// <returns>True if arrival should start</returns>
    public bool ShouldStartArrival(int currentHour, int currentMinute)
    {
        if (!ShiftEnabled || ShiftStartHours.Count == 0)
            return false;

        bool isShiftStartHour = ShiftStartHours.Contains(currentHour);
        
        if (isShiftStartHour)
        {
            // Check if we already triggered arrival for this shift hour
            if (_lastArrivalShiftHour == currentHour)
                return false; // Already processed arrival for this shift
            
            // Start arrival 10 minutes before shift time, so they arrive during the overlap
            if (currentMinute >= (60 - (SHIFT_PREPARATION_MINUTES - OVERLAP_MINUTES)))
            {
                // Mark this shift hour as processed for arrival
                _lastArrivalShiftHour = currentHour;
                Logger.Log.Info($"Area {Name}: Arrival triggered for shift hour {currentHour} (will not trigger again this hour)");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Backward compatibility method - uses current game minute
    /// </summary>
    /// <param name="currentHour">Current game hour (0-23)</param>
    /// <returns>True if a shift change should occur</returns>
    public bool IsShiftTime(int currentHour)
    {
        // Get current minute from game
        int currentMinute = GTA.Native.Function.Call<int>(GTA.Native.Hash.GET_CLOCK_MINUTES);
        return IsShiftTime(currentHour, currentMinute);
    }

    /// <summary>
    /// Check if guards should be allowed to spawn at the current time
    /// </summary>
    /// <param name="currentHour">Current game hour (0-23)</param>
    /// <returns>True if guards can spawn, false if no guards should be present</returns>
    public bool ShouldSpawnGuards(int currentHour)
    {
        // If shifts are disabled, always allow spawning
        if (!ShiftEnabled)
            return true;

        // If shifts are enabled, only spawn if current time is within a shift period
        return IsWithinShiftPeriod(currentHour);
    }

    /// <summary>
    /// Determine if this is a departure (guards leaving) or arrival (new guards coming)
    /// This is a simplified logic - you can enhance it based on your needs
    /// </summary>
    /// <param name="currentHour">Current game hour</param>
    /// <returns>True if guards should depart, false if new guards should arrive</returns>
    public bool IsDeparture(int currentHour)
    {
        // Simple logic: if we have guards active, it's a departure
        // In a more complex system, you might track shift rotations
        return true; // For now, always treat shift changes as departures followed by arrivals
    }

    public void AddSpawnPoint(Vector3 position, float heading, string type, string scenario, bool interior, string finalAnimation)
    {
        SpawnPoints.Add(new GuardSpawnPoint(position, heading, type, scenario, interior, finalAnimation));
        _cachedCentroid = null; // Invalidate cache when spawn points change
    }

    /// <summary>
    /// Gets the centroid (center point) of all spawn points. 
    /// Result is cached for performance - only recalculated when spawn points change.
    /// </summary>
    public Vector3 GetCentroid()
    {
        // Return cached value if available
        if (_cachedCentroid.HasValue)
            return _cachedCentroid.Value;

        // Calculate and cache
        if (SpawnPoints == null || SpawnPoints.Count == 0)
        {
            _cachedCentroid = Vector3.Zero;
            return Vector3.Zero;
        }

        float sumX = 0, sumY = 0, sumZ = 0;
        foreach (var point in SpawnPoints)
        {
            sumX += point.Position.X;
            sumY += point.Position.Y;
            sumZ += point.Position.Z;
        }

        _cachedCentroid = new Vector3(
            sumX / SpawnPoints.Count,
            sumY / SpawnPoints.Count,
            sumZ / SpawnPoints.Count
        );
        
        return _cachedCentroid.Value;
    }
}

public class GuardSpawnPoint
{
    public Vector3 Position { get; set; }
    public float Heading { get; set; }
    public string Type { get; set; }
    public string Scenario { get; set; }  // Stores override scenario (if any)
    public string Animation { get; set; } // Final assigned animation
    public bool Interior { get; set; }

    public GuardSpawnPoint(Vector3 position, float heading, string type, string scenario, bool interior, string animation)
    {
        Position = position;
        Heading = heading;
        Type = type;
        Scenario = scenario;
        Animation = animation;
        Interior = interior;
    }
}

/// <summary>
/// Backup system configuration for costs and cooldowns
/// </summary>
public class BackupFeesConfig
{
    public int AerialCost { get; set; } = 5000;
    public int AerialCooldown { get; set; } = 30;
    
    public int AirstrikeCost { get; set; } = 50000;
    public int AirstrikeCooldown { get; set; } = 30;
    
    public int GroundCost { get; set; } = 15000;
    public int GroundCooldown { get; set; } = 30;
}
