using GTA;
using GTA.Math;
using GTA.Native;
using Guarding.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

// Type alias to resolve ambiguity between GTA.VehicleType and Guarding.Core.Enums.VehicleType
using GuardVehicleType = Guarding.Core.Enums.VehicleType;

public class GuardSpawner
{
    public static List<Area> areas;
    public static List<GuardPed> guardPeds;
    public static List<GuardVehicle> guardVehicles;
    public static List<GuardPed> removedGuards; // DEPRECATED: Will be replaced by deadGuards
    
    // NEW: Separate tracking for dead/destroyed entities (don't respawn these)
    public static List<GuardPed> deadGuards;
    public static List<GuardVehicle> destroyedVehicles;
    
    // NEW: Track which areas have active spawns
    private static HashSet<string> _areasWithActiveSpawns = new HashSet<string>();
    
    // NEW: Track if player is currently in each area
    private static Dictionary<string, bool> _playerInArea = new Dictionary<string, bool>();

    // Shift preparation tracking - track which areas have prepared for which shift periods
    private static Dictionary<string, int> _lastPreparedShiftHour = new Dictionary<string, int>();
    
    // Shift assignment tracking - stores departure info to reuse for arrivals
    private static Dictionary<string, ShiftAssignment> _activeShiftAssignments = new Dictionary<string, ShiftAssignment>();
    
    // Area backup system tracking
    private static Dictionary<string, bool> _areaInCombat = new Dictionary<string, bool>();
    private static Dictionary<string, DateTime> _areaCombatEndTime = new Dictionary<string, DateTime>();
    private static Dictionary<string, List<BackupSquad>> _areaBackupSquads = new Dictionary<string, List<BackupSquad>>();
    
    // Wave-based backup dispatch system
    private static Dictionary<string, DateTime> _areaLastBackupSpawn = new Dictionary<string, DateTime>();
    private static Dictionary<string, int> _areaBackupWaveCount = new Dictionary<string, int>();
    private static Dictionary<string, DateTime> _areaCombatStartTime = new Dictionary<string, DateTime>();
    
    private static Random _random = new Random();
    
    public static Dictionary<string, Scenarios> scenarioConfigs;
    public static List<Ped> processedPeds;
    public static List<Ped> writheProcessedPeds;
    public static Dictionary<string, GuardConfig> guardConfigs; // Changed to static for cross-area relationship access
    public DateTime currentGameTime;
    public const float SpawnDistance = 220f;
    public const float DespawnDistance = 250f;
    private static string _exceptionLogPath = "scripts\\Guarding_Exceptions.log";

    private static void LogDriverException(string message)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}\n";
            System.IO.File.AppendAllText(_exceptionLogPath, logEntry);
        }
        catch
        {
            // Fail silently if we can't write to exception log
        }
    }

    /// <summary>
    /// Setup relationships between all guard areas to ensure they work together
    /// </summary>
    private void SetupAllCrossAreaRelationships()
    {
        try
        {
            // Convert relationship group names to hashes
            var privateGuardHash = StringHash.AtStringHash("PRIVATE_SECURITY");
            var guardHash = StringHash.AtStringHash("SECURITY_GUARD");
            var armyHash = StringHash.AtStringHash("ARMY");
            var copHash = StringHash.AtStringHash("COP");
            var guardDogHash = StringHash.AtStringHash("GUARD_DOG");
            var merryWHash = StringHash.AtStringHash("MERRYWEATHER");
            var playerGroupHash = Game.Player.Character.RelationshipGroup;

            // Setup relationships between guard groups (they should help each other)
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, privateGuardHash, guardHash); // Like
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, guardHash, privateGuardHash); // Like

            // Guards hate criminals and enemies
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, privateGuardHash, StringHash.AtStringHash("CRIMINAL")); // Hate
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, guardHash, StringHash.AtStringHash("CRIMINAL")); // Hate
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, privateGuardHash, StringHash.AtStringHash("GANG_1")); // Hate
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, guardHash, StringHash.AtStringHash("GANG_1")); // Hate

            // Guards respect police and army
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, privateGuardHash, copHash); // Respect
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, guardHash, copHash); // Respect
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, privateGuardHash, armyHash); // Respect
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, guardHash, armyHash); // Respect

            Logger.Log.Info("Cross-area guard relationships established");
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Failed to setup cross-area relationships: {ex.Message}");
        }
    }

    /// <summary>
    /// Setup relationships for a specific guard
    /// </summary>
    private void SetupGuardRelationships(Ped guard, GuardConfig config, Area area)
    {
        try
        {
            // Set guard's relationship group
            if (config.RelationshipGroup != null && config.RelationshipGroup.Length > 0)
            {
                var groupHash = StringHash.AtStringHash(config.RelationshipGroup);
                guard.RelationshipGroup = groupHash;
            }
            else
            {
                // Default to private security
                guard.RelationshipGroup = StringHash.AtStringHash("PRIVATE_SECURITY");
            }

            Logger.Log.Info($"Setup relationships for guard in area {area.Name}");
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Failed to setup guard relationships: {ex.Message}");
        }
    }

    /// <summary>
    /// Manage guard shifting (departure/arrival) for an area
    /// </summary>
    private void ManageGuardShifting(Area area, bool isDeparture)
    {
        try
        {
            if (isDeparture)
            {
                // Handle guard departure - this requires vehicle assignment logic
                // For now, just log that departure was requested
                Logger.Log.Info($"Departure requested for area {area.Name} - vehicle assignment needed");
            }
            else
            {
                // Handle guard arrival
                SpawnGuardsForArea(area);
                Logger.Log.Info($"Started arrival process for area {area.Name}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Failed to manage guard shifting: {ex.Message}");
        }
    }

    public GuardSpawner(string xmlFilePath)
    {
        XmlReader xml = new XmlReader(xmlFilePath);
        guardConfigs = xml.LoadGuardConfigs();
        scenarioConfigs = xml.LoadScenarios();
        areas = xml.LoadAreasFromXml(scenarioConfigs); // Pass scenarios as parameter

        // Parse shift durations for all areas
        foreach (var area in areas)
        {
            area.ParseShiftDuration();
            _playerInArea[area.Name] = false; // Initialize all areas as "player not in area"
            _areaInCombat[area.Name] = false; // Initialize all areas as not in combat
            _areaBackupSquads[area.Name] = new List<BackupSquad>(); // Initialize empty backup squad lists
        }

        guardPeds = new List<GuardPed>();
        guardVehicles = new List<GuardVehicle>();
        removedGuards = new List<GuardPed>(); // Legacy, will phase out
        deadGuards = new List<GuardPed>(); // NEW: Track dead guards
        destroyedVehicles = new List<GuardVehicle>(); // NEW: Track destroyed vehicles
        processedPeds = new List<Ped>();
        writheProcessedPeds = new List<Ped>();

        Logger.Log.Info($"Loaded {areas.Count} areas from XML.");
        
        // Setup cross-area guard relationships ONCE during initialization
        SetupAllCrossAreaRelationships();
    }



    // Allows external management (e.g., via a shift manager)
    public void AddGuard(GuardPed guard)
    {
        if (!guardPeds.Contains(guard))
        {
            guardPeds.Add(guard);
            Logger.Log.Info($"Guard added to normal management in area {guard.AreaName}");
        }
    }

    public void AddVehicle(GuardVehicle vehicle)
    {
        if (!guardVehicles.Contains(vehicle))
        {
            guardVehicles.Add(vehicle);
            Logger.Log.Info($"Vehicle added to management in area {vehicle.AreaName}");
        }
    }

    public List<GuardPed> GetActiveGuards() => guardPeds;
    public List<GuardVehicle> GetActiveVehicles() => guardVehicles;

    /// <summary>
    /// Fast per-frame cleanup: mark dead or abandoned guards/vehicles as no longer needed
    /// This helps the game collect and remove entities faster.
    /// Conservative rules:
    /// - Dead guards -> MarkAsNoLongerNeeded + add to deadGuards
    /// - Idle guards (OnFoot, not in combat, no active tasks) that are far from player -> MarkAsNoLongerNeeded
    /// - Destroyed or non-existent vehicles -> Despawn and move to destroyedVehicles
    /// - Idle vehicles with no assigned peds and far from player -> MarkAsNoLongerNeeded
    /// </summary>
    public void FastCleanupTick(Player player)
    {
        if (player == null || player.Character == null) return;

        try
        {
            // Guards
            foreach (var guard in guardPeds.ToList())
            {
                try
                {
                    if (guard == null) continue;
                    if (guard.guardPed == null) continue;
                    if (!guard.guardPed.Exists()) continue;

                    // Dead guards -> mark and record
                    if (guard.guardPed.IsDead)
                    {
                        Logger.Log.Info($"FastCleanup: Guard dead in {guard.AreaName} - marking as no longer needed");
                        // Remove blip if present
                        try { if (guard.GuardBlip != null && guard.GuardBlip.Exists()) guard.GuardBlip.Delete(); } catch { }
                        guard.guardPed.MarkAsNoLongerNeeded();
                        if (!deadGuards.Contains(guard)) deadGuards.Add(guard);
                        continue;
                    }

                    // Idle guards: on foot, not in combat, no active task sequence, OnDuty/Idle state
                    bool isIdleGuard = guard.guardPed.IsOnFoot && !guard.guardPed.IsInCombat && guard.guardPed.TaskSequenceProgress == -1 && (guard.CurrentState == GuardState.Idle || guard.CurrentState == GuardState.OnDuty);
                    float distToPlayer = guard.guardPed.Position.DistanceTo(player.Character.Position);
                    if (isIdleGuard && distToPlayer > DespawnDistance)
                    {
                        Logger.Log.Info($"FastCleanup: Idle guard far from player in {guard.AreaName} - marking as no longer needed");
                        try { if (guard.GuardBlip != null && guard.GuardBlip.Exists()) guard.GuardBlip.Delete(); } catch { }
                        guard.guardPed.MarkAsNoLongerNeeded();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Warning($"FastCleanup: guard cleanup error: {ex.Message}");
                }
            }

            // Vehicles
            foreach (var veh in guardVehicles.ToList())
            {
                try
                {
                    if (veh == null) continue;
                    if (veh.guardVehicle == null)
                    {
                        // Already null - treat as destroyed
                        if (!destroyedVehicles.Contains(veh)) destroyedVehicles.Add(veh);
                        continue;
                    }

                    if (!veh.guardVehicle.Exists())
                    {
                        Logger.Log.Info($"FastCleanup: Vehicle in {veh.AreaName} no longer exists - despawning");
                        veh.Despawn();
                        if (!destroyedVehicles.Contains(veh)) destroyedVehicles.Add(veh);
                        continue;
                    }

                    // Idle vehicle: no boarded peds and no assigned peds and far from player
                    int boarded = 0;
                    try { boarded = veh.GetBoardedPedCount(); } catch { boarded = 0; }
                    bool hasAssigned = veh.AssignedPeds != null && veh.AssignedPeds.Count > 0;
                    float distToPlayerV = veh.guardVehicle.Position.DistanceTo(player.Character.Position);

                    if (boarded == 0 && !hasAssigned && distToPlayerV > DespawnDistance)
                    {
                        Logger.Log.Info($"FastCleanup: Idle vehicle far from player in {veh.AreaName} - marking as no longer needed");
                        try { if (veh.VehicleBlip != null && veh.VehicleBlip.Exists()) veh.VehicleBlip.Delete(); } catch { }
                        veh.guardVehicle.MarkAsNoLongerNeeded();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Warning($"FastCleanup: vehicle cleanup error: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log.Warning($"FastCleanup: unexpected error: {ex.Message}");
        }
    }

    // Check player proximity to areas and spawn or despawn guards accordingly.
    // NOTE: Player can be in multiple areas simultaneously (e.g., Michael's doors + snipers)
    // Each area is processed independently with its own tracking
    public void CheckPlayerProximityAndSpawn(Player player)
    {
        if (player?.Character == null)
        {
            Logger.Log.Fatal("Player or Player.Character is null; skipping proximity check.");
            return;
        }

        // Process each area independently - supports multiple overlapping areas
        foreach (var area in areas)
        {
            Vector3 areaCentroid = area.GetCentroid();
            float distanceToCentroid = player.Character.Position.DistanceTo(areaCentroid);
            bool wasInArea = _playerInArea.ContainsKey(area.Name) && _playerInArea[area.Name];
            bool isNowInArea = distanceToCentroid < SpawnDistance;
            bool hasLeftArea = distanceToCentroid > DespawnDistance;

            // ENTER AREA: Player entered area
            if (!wasInArea && isNowInArea)
            {
                Logger.Log.Info($"Player ENTERED area {area.Name} (distance: {distanceToCentroid:F1}m)");
                _playerInArea[area.Name] = true;
                
                // Check if it's shift time and spawn appropriate guards
                int currentHour = Function.Call<int>(Hash.GET_CLOCK_HOURS);
                int currentMinute = Function.Call<int>(Hash.GET_CLOCK_MINUTES);
                bool shouldSpawn = area.ShouldSpawnGuards(currentHour);

                if (shouldSpawn)
                {
                    SpawnGuardsForArea(area);
                }
                else
                {
                    Logger.Log.Info($"Area {area.Name} is outside shift hours (time: {currentHour:D2}:{currentMinute:D2}) - no guards will spawn");
                }
            }
            // EXIT AREA: Player left area
            else if (wasInArea && hasLeftArea)
            {
                Logger.Log.Info($"Player LEFT area {area.Name} (distance: {distanceToCentroid:F1}m) - CLEANING ALL");
                _playerInArea[area.Name] = false;
                CleanupAreaCompletely(area);
            }
            // INSIDE AREA: Player is currently in area
            else if (isNowInArea && wasInArea)
            {
                // Check for shift changes while player is in area
                CheckShiftChangesInArea(area);
            }
        }

        // Update all active guards and vehicles (regardless of which area)
        CheckAllTime();
    }

    /// <summary>
    /// Check if any area needs a shift change and trigger it
    /// ONLY called when player is inside the area
    /// </summary>
    private void CheckShiftChangesInArea(Area area)
    {
        if (!area.ShiftEnabled)
            return;

        int currentHour = Function.Call<int>(Hash.GET_CLOCK_HOURS);
        int currentMinute = Function.Call<int>(Hash.GET_CLOCK_MINUTES);

        // Check if we should start departure (old guards leaving)
        if (area.ShouldStartDeparture(currentHour, currentMinute))
        {
            var guardsToDepart = guardPeds.Where(g => 
                g.AreaName == area.Name && 
                g.CurrentState == GuardState.OnDuty).ToList();

            if (guardsToDepart.Count > 0)
            {
                Logger.Log.Info($"SHIFT CHANGE: Starting departure for area {area.Name} - {guardsToDepart.Count} guards leaving");
                ManageGuardShifting(area, true); // Start departure
            }
        }

        // Check if we should start arrival (new guards coming)
        if (area.ShouldStartArrival(currentHour, currentMinute))
        {
            bool hasActiveArrival = guardPeds.Any(g => 
                g.AreaName == area.Name && 
                (g.CurrentState == GuardState.Arriving || g.CurrentState == GuardState.ExitVehicle));

            if (!hasActiveArrival)
            {
                Logger.Log.Info($"SHIFT CHANGE: Starting arrival for area {area.Name}");
                ManageGuardShifting(area, false); // Start arrival
            }
        }
    }

    /// <summary>
    /// Spawn guards for an area (called when player enters or during shift change)
    /// Does NOT respawn dead guards or destroyed vehicles
    /// NOTE: Multiple areas can trigger this simultaneously (e.g., overlapping zones)
    /// </summary>
    private void SpawnGuardsForArea(Area area)
    {
        if (!guardConfigs.ContainsKey(area.Model))
        {
            Logger.Log.Info($"Guard model {area.Model} not found in configurations.");
            return;
        }

        Logger.Log.Info($"Spawning guards for area {area.Name} (player may be in multiple areas)");
        
        var guardConfig = guardConfigs[area.Model];
        var scenarioConfig = scenarioConfigs[area.DefaultScenario];

        foreach (var spawnPoint in area.SpawnPoints)
        {
            // Check if this spawn point already has an active guard/vehicle
            bool alreadyActive = guardPeds.Any(g => g.Position == spawnPoint.Position && g.AreaName == area.Name) ||
                                guardVehicles.Any(v => v.Position == spawnPoint.Position && v.AreaName == area.Name);

            // Check if this spawn point had a dead/destroyed entity (DON'T RESPAWN)
            bool isDead = deadGuards.Any(g => g.Position == spawnPoint.Position && g.AreaName == area.Name);
            bool isDestroyed = destroyedVehicles.Any(v => v.Position == spawnPoint.Position && v.AreaName == area.Name);

            if (!alreadyActive && !isDead && !isDestroyed)
            {
                // Create appropriate entity based on spawn point type
                if (spawnPoint.Type.ToLower() == "ped")
                {
                    GuardPed guard = new GuardPed(spawnPoint, guardConfig, area, scenarioConfig);
                    guardPeds.Add(guard);
                    guard.Spawn();
                    Logger.Log.Info($"Spawned guard at {spawnPoint.Position} in area {area.Name}");
                    
                    if (guard.guardPed != null && guard.guardPed.Exists() && !processedPeds.Contains(guard.guardPed))
                    {
                        processedPeds.Add(guard.guardPed);
                    }
                }
                else if (spawnPoint.Type.ToLower() == "vehicle" ||
                         spawnPoint.Type.ToLower() == "largevehicle" ||
                         spawnPoint.Type.ToLower() == "helicopter" ||
                         spawnPoint.Type.ToLower() == "plane" ||
                         spawnPoint.Type.ToLower() == "boat" ||
                         spawnPoint.Type.ToLower() == "mounted")
                {
                    GuardVehicle vehicle = new GuardVehicle(spawnPoint, guardConfig, area);
                    guardVehicles.Add(vehicle);
                    vehicle.Spawn();
                    Logger.Log.Info($"Spawned vehicle at {spawnPoint.Position} in area {area.Name}");
                }
            }
            else if (isDead || isDestroyed)
            {
                Logger.Log.Info($"Skipping spawn at {spawnPoint.Position} in area {area.Name} - entity died/destroyed (won't respawn)");
            }
        }

        _areasWithActiveSpawns.Add(area.Name);
    }

    /// <summary>
    /// Completely cleanup an area when player leaves
    /// Moves dead/destroyed to separate lists, removes everything else
    /// NOTE: Only cleans guards for THIS specific area - other overlapping areas unaffected
    /// </summary>
    private void CleanupAreaCompletely(Area area)
    {
        Logger.Log.Info($"COMPLETE CLEANUP of area {area.Name} (other areas remain active if player still inside them)");

        // Remove all guards from this area ONLY (filtered by AreaName)
        var guardsToRemove = guardPeds.Where(g => g.AreaName == area.Name).ToList();
        foreach (var guard in guardsToRemove)
        {
            if (guard != null)
            {
                // If dead, move to dead list; otherwise just despawn
                if (guard.guardPed == null || !guard.guardPed.Exists() || guard.guardPed.IsDead)
                {
                    if (!deadGuards.Contains(guard))
                    {
                        deadGuards.Add(guard);
                        Logger.Log.Info($"Guard at {guard.Position} in {area.Name} died - moved to dead list");
                    }
                }
                
                guard.Despawn();
                guardPeds.Remove(guard);
            }
        }

        // Remove all vehicles from this area
        var vehiclesToRemove = guardVehicles.Where(v => v.AreaName == area.Name).ToList();
        foreach (var vehicle in vehiclesToRemove)
        {
            if (vehicle != null)
            {
                // If destroyed, move to destroyed list; otherwise just despawn
                if (vehicle.guardVehicle == null || !vehicle.guardVehicle.Exists() || vehicle.CurrentState == VehicleState.Destroyed)
                {
                    if (!destroyedVehicles.Contains(vehicle))
                    {
                        destroyedVehicles.Add(vehicle);
                        Logger.Log.Info($"Vehicle at {vehicle.Position} in {area.Name} destroyed - moved to destroyed list");
                    }
                }
                
                vehicle.Despawn();
                guardVehicles.Remove(vehicle);
            }
        }

        // Clear legacy removed guards list for this area
        var legacyRemovedToRemove = removedGuards.Where(g => g.AreaName == area.Name).ToList();
        foreach (var guard in legacyRemovedToRemove)
        {
            removedGuards.Remove(guard);
        }

        _areasWithActiveSpawns.Remove(area.Name);
        _activeShiftAssignments.Remove(area.Name);
        _lastPreparedShiftHour.Remove(area.Name);
        
        // Clean up backup system tracking for this area
        _areaInCombat.Remove(area.Name);
        _areaCombatEndTime.Remove(area.Name);
        _areaBackupSquads.Remove(area.Name);
        _areaLastBackupSpawn.Remove(area.Name);
        _areaBackupWaveCount.Remove(area.Name);
        _areaCombatStartTime.Remove(area.Name);

        Logger.Log.Info($"Area {area.Name} cleanup complete - ready for fresh spawn on re-entry");
    }

    /// <summary>
    /// LEGACY: Check if any area needs a shift change (still used by CheckAllTime loop)
    /// Eventually this will be replaced by CheckShiftChangesInArea
    /// </summary>
    private void CheckShiftChanges(Area area)
    {
        if (!area.ShiftEnabled)
            return;

        int currentHour = Function.Call<int>(Hash.GET_CLOCK_HOURS);
        int currentMinute = Function.Call<int>(Hash.GET_CLOCK_MINUTES);

        // Check if we should start departure (old guards leaving)
        if (area.ShouldStartDeparture(currentHour, currentMinute))
        {
            // Only start departure if we have guards that are:
            // 1. OnDuty (standing at posts)
            // 2. NOT in Arriving state (prevents clash with incoming guards)
            // 3. NOT in Departing state (already leaving)
            // 4. NOT in ExitVehicle state (still exiting from arrival)
            var guardsToDepart = guardPeds.Where(g => 
                g.AreaName == area.Name && 
                g.CurrentState == GuardState.OnDuty &&
                g.CurrentState != GuardState.Arriving &&
                g.CurrentState != GuardState.Departing &&
                g.CurrentState != GuardState.ExitVehicle).ToList();

            // Performance: Use Count > 0 instead of .Any()
            if (guardsToDepart.Count > 0)
            {
                Logger.Log.Info($"Starting guard departure for area {area.Name} at {currentHour:D2}:{currentMinute:D2} - {guardsToDepart.Count} guards eligible");
                ManageGuardShifting(area, true); // Start departure
            }
            else
            {
                Logger.Log.Info($"Departure window for area {area.Name} but no eligible OnDuty guards found (preventing clash with arriving/departing guards)");
            }
        }

        // Check if we should start arrival (new guards coming) - happens a bit later
        if (area.ShouldStartArrival(currentHour, currentMinute))
        {
            // Only start arrival if we don't already have:
            // 1. Guards in Arriving state (already arriving)
            // 2. Guards in ExitVehicle state (currently exiting from arrival)
            // Performance: Use Count > 0 instead of .Any()
            bool hasActiveArrival = guardPeds.Count(g => 
                g.AreaName == area.Name && 
                (g.CurrentState == GuardState.Arriving || g.CurrentState == GuardState.ExitVehicle)) > 0;

            if (!hasActiveArrival)
            {
                Logger.Log.Info($"Starting guard arrival for area {area.Name} at {currentHour:D2}:{currentMinute:D2}");
                ManageGuardShifting(area, false); // Start arrival
            }
            else
            {
                Logger.Log.Info($"Arrival window for area {area.Name} but arrival already in progress");
            }
        }
        //    }
        //}
    }

    // Uninitialize by despawning all guards and clearing tracking lists.
    public void UnInitialize()
    {
        foreach (var guard in guardPeds.ToList())
        {
            guard?.Despawn();
        }
        foreach (var vehicle in guardVehicles.ToList())
        {
            vehicle?.Despawn();
        }
        guardPeds.Clear();
        guardVehicles.Clear();
        removedGuards.Clear();
        deadGuards.Clear();
        destroyedVehicles.Clear();
        processedPeds.Clear();
        writheProcessedPeds.Clear();
        _areasWithActiveSpawns.Clear();
        _playerInArea.Clear();
        _activeShiftAssignments.Clear();
        _lastPreparedShiftHour.Clear();
        Logger.Log.Info("All guards and vehicles have been uninitialized and despawned.");
    }

    // Sets up the world state by tracking all relevant pedestrians.
    private void SetupWorldStuffs()
    {
        List<Ped> allPedsInWorld = World.GetAllPeds().ToList();

        // Convert relationship group names to hashes.
        var privateGuardHash = StringHash.AtStringHash("PRIVATE_SECURITY");
        var guardHash = StringHash.AtStringHash("SECURITY_GUARD");
        var armyHash = StringHash.AtStringHash("ARMY");
        var copHash = StringHash.AtStringHash("COP");
        var guardDogHash = StringHash.AtStringHash("GUARD_DOG");
        var merryWHash = StringHash.AtStringHash("MERRYWEATHER");
        var playerGroupHash = Game.Player.Character.RelationshipGroup;

    }

    

    void HandleAreasBackups(Area area)
    {
        if (area == null) return;
        
        // Check if area is in combat
        bool currentlyInCombat = IsAreaInCombat(area);
        bool wasInCombat = _areaInCombat.ContainsKey(area.Name) && _areaInCombat[area.Name];
        
        // Update combat status
        _areaInCombat[area.Name] = currentlyInCombat;
        
        // Combat started - initialize backup tracking
        if (currentlyInCombat && !wasInCombat)
        {
            Logger.Log.Info($"Area {area.Name}: Combat detected - initializing backup dispatch system");
            InitializeAreaBackupDispatch(area);
        }
        // Combat ended - start dismissal timer for all squads
        else if (!currentlyInCombat && wasInCombat)
        {
            Logger.Log.Info($"Area {area.Name}: Combat ended - starting dismissal timers for all backup squads");
            StartAreaBackupDismissal(area);
        }
        
        // Handle ongoing combat - spawn waves at intervals
        if (currentlyInCombat)
        {
            HandleAreaBackupWaves(area);
        }
        
        // Update existing backup squads for this area
        if (_areaBackupSquads.ContainsKey(area.Name))
        {
            UpdateAreaBackupSquads(area, _areaBackupSquads[area.Name]);
        }
    }

    /// <summary>
    /// Check if any guard in the specified area is currently in combat
    /// </summary>
    private bool IsAreaInCombat(Area area)
    {
        if (area == null) return false;

        // Check all active guards in this area
        var areaGuards = guardPeds.Where(g => g.AreaName == area.Name && g.guardPed != null && g.guardPed.Exists()).ToList();
        
        foreach (var guard in areaGuards)
        {
            // Check if guard is in combat
            if (guard.guardPed.IsInCombat)
            {
                Logger.Log.Info($"Area {area.Name}: Guard {guard.guardPed.Handle} is in combat");
                return true;
            }

            // Check if guard is being shot at (has recent damage)
            if (guard.guardPed.HasBeenDamagedByAnyWeapon())
            {
                Logger.Log.Info($"Area {area.Name}: Guard {guard.guardPed.Handle} has been damaged recently");
                return true;
            }

            // Check if guard is shooting
            if (guard.guardPed.IsShooting)
            {
                Logger.Log.Info($"Area {area.Name}: Guard {guard.guardPed.Handle} is shooting");
                return true;
            }

            // Check if guard has a combat target
            if (guard.guardPed.CombatTarget != null && guard.guardPed.CombatTarget.Exists())
            {
                Logger.Log.Info($"Area {area.Name}: Guard {guard.guardPed.Handle} has combat target {guard.guardPed.CombatTarget.Handle}");
                return true;
            }
        }

        // Also check backup squads assigned to this area
        if (_areaBackupSquads.ContainsKey(area.Name))
        {
            foreach (var squad in _areaBackupSquads[area.Name])
            {
                foreach (var backupGuard in squad.Guards)
                {
                    if (backupGuard.Ped != null && backupGuard.Ped.Exists())
                    {
                        // Same combat checks for backup guards
                        if (backupGuard.Ped.IsInCombat || 
                            backupGuard.Ped.HasBeenDamagedByAnyWeapon() || 
                            backupGuard.Ped.IsShooting ||
                            (backupGuard.Ped.CombatTarget != null && backupGuard.Ped.CombatTarget.Exists()))
                        {
                            Logger.Log.Info($"Area {area.Name}: Backup guard {backupGuard.Ped.Handle} is in combat");
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Initialize backup dispatch tracking when combat starts in an area
    /// </summary>
    private void InitializeAreaBackupDispatch(Area area)
    {
        _areaLastBackupSpawn[area.Name] = DateTime.Now;
        _areaBackupWaveCount[area.Name] = 0;
        _areaCombatStartTime[area.Name] = DateTime.Now;
        
        // Log available backup types for this area
        if (guardConfigs.ContainsKey(area.Model))
        {
            LogAvailableBackupTypes(area, guardConfigs[area.Model]);
        }
        
        Logger.Log.Info($"Area {area.Name}: Backup dispatch initialized - wave system ready");
    }

    /// <summary>
    /// Start dismissal timers for all backup squads when combat ends
    /// </summary>
    private void StartAreaBackupDismissal(Area area)
    {
        if (_areaBackupSquads.ContainsKey(area.Name))
        {
            foreach (var squad in _areaBackupSquads[area.Name])
            {
                if (squad.IsActive && !squad.CombatEndCheck)
                {
                    squad.CombatEndCheck = true;
                    squad.CombatEndTime = DateTime.Now;
                    Logger.Log.Info($"Area {area.Name}: Starting dismissal timer for {squad.SquadType} squad");
                }
            }
        }
    }

    /// <summary>
    /// Check what backup types are available for an area based on guard config
    /// </summary>
    private void LogAvailableBackupTypes(Area area, GuardConfig config)
    {
        bool hasHelicopters = config.HVehicleModels != null && config.HVehicleModels.Any();
        bool hasVehicles = config.VehicleModels != null && config.VehicleModels.Any();

        if (!hasHelicopters && !hasVehicles)
        {
            Logger.Log.Info($"Area {area.Name}: No backup types configured - aerial and ground dispatch disabled");
        }
        else if (hasHelicopters && !hasVehicles)
        {
            Logger.Log.Info($"Area {area.Name}: Aerial dispatch enabled, ground vehicles disabled");
        }
        else if (!hasHelicopters && hasVehicles)
        {
            Logger.Log.Info($"Area {area.Name}: Ground vehicles enabled, aerial dispatch disabled");
        }
        else
        {
            Logger.Log.Info($"Area {area.Name}: Both aerial and ground backup types available");
        }
    }

    /// <summary>
    /// Handle wave-based backup spawning during ongoing combat
    /// Spawns reinforcements at intervals based on combat duration and intensity
    /// </summary>
    private void HandleAreaBackupWaves(Area area)
    {
        if (!guardConfigs.ContainsKey(area.Model)) return;

        var guardConfig = guardConfigs[area.Model];
        
        // Check if any backup types are available at all
        bool hasHelicopters = guardConfig.HVehicleModels != null && guardConfig.HVehicleModels.Any();
        bool hasVehicles = guardConfig.VehicleModels != null && guardConfig.VehicleModels.Any();
        
        if (!hasHelicopters && !hasVehicles)
        {
            // No backup types configured - silently skip without logging every frame
            return;
        }

        // Get combat duration
        DateTime combatStart = _areaCombatStartTime.ContainsKey(area.Name) ? 
            _areaCombatStartTime[area.Name] : DateTime.Now;
        TimeSpan combatDuration = DateTime.Now - combatStart;

        // Get current wave count
        int currentWave = _areaBackupWaveCount.ContainsKey(area.Name) ? 
            _areaBackupWaveCount[area.Name] : 0;

        // Get last spawn time
        DateTime lastSpawn = _areaLastBackupSpawn.ContainsKey(area.Name) ? 
            _areaLastBackupSpawn[area.Name] : DateTime.MinValue;
        TimeSpan timeSinceLastSpawn = DateTime.Now - lastSpawn;

        // Calculate spawn interval based on combat intensity and wave count
    // Base interval. Can be overridden per-area by Area.BackupSpawnIntervalSeconds
    double baseIntervalSeconds = (area.BackupSpawnIntervalSeconds > 0) ? area.BackupSpawnIntervalSeconds : 45.0;
        
        // Reduce interval as combat continues (more urgent reinforcements)
        double intensityMultiplier = Math.Max(0.3, 1.0 - (combatDuration.TotalMinutes * 0.05)); // Min 30% of base
        
        // Increase frequency for higher waves (escalation)
        double waveMultiplier = Math.Max(0.5, 1.0 - (currentWave * 0.1)); // Min 50% of base
        
        double spawnIntervalSeconds = baseIntervalSeconds * intensityMultiplier * waveMultiplier;
        
        // Check if it's time to spawn next wave
        if (timeSinceLastSpawn.TotalSeconds >= spawnIntervalSeconds)
        {
            // Check if we should spawn based on current squad count and combat intensity
            int maxConcurrentSquads = CalculateMaxConcurrentSquads(area, combatDuration, currentWave);
            int currentActiveSquads = CountActiveBackupSquads(area);
            
            if (currentActiveSquads < maxConcurrentSquads)
            {
                Logger.Log.Info($"Area {area.Name}: Spawning wave {currentWave + 1} - {currentActiveSquads}/{maxConcurrentSquads} active squads, interval: {spawnIntervalSeconds:F1}s");
                SpawnBackupWaveForArea(area, currentWave);
                
                // Update tracking
                _areaLastBackupSpawn[area.Name] = DateTime.Now;
                _areaBackupWaveCount[area.Name] = currentWave + 1;
            }
            else
            {
                Logger.Log.Info($"Area {area.Name}: Wave spawn skipped - at max capacity ({currentActiveSquads}/{maxConcurrentSquads})");
            }
        }
    }

    /// <summary>
    /// Calculate maximum concurrent squads allowed based on combat duration and wave count
    /// </summary>
    private int CalculateMaxConcurrentSquads(Area area, TimeSpan combatDuration, int currentWave)
    {
        // Base maximum of 2 squads
        int baseMax = 2;
        
        // Allow more squads as combat continues
        int durationBonus = (int)(combatDuration.TotalMinutes / 2); // +1 every 2 minutes
        
        // Allow more squads with higher waves
        int waveBonus = currentWave / 3; // +1 every 3 waves
        
        return Math.Min(6, baseMax + durationBonus + waveBonus); // Cap at 6 concurrent squads
    }

    /// <summary>
    /// Count currently active backup squads for an area
    /// </summary>
    private int CountActiveBackupSquads(Area area)
    {
        if (!_areaBackupSquads.ContainsKey(area.Name))
            return 0;

        return _areaBackupSquads[area.Name].Count(s => s.IsActive && !s.CombatEndCheck);
    }

    /// <summary>
    /// Spawn a backup wave for an area with varied unit types
    /// </summary>
    private void SpawnBackupWaveForArea(Area area, int waveNumber)
    {
        if (area == null || !guardConfigs.ContainsKey(area.Model)) return;

        var guardConfig = guardConfigs[area.Model];
        Vector3 spawnCenter = area.GetCentroid();

        Logger.Log.Info($"Area {area.Name}: Spawning backup wave {waveNumber + 1} at {spawnCenter}");

        try
        {
            // Determine wave composition based on wave number and available types
            List<BackupType> waveComposition = GenerateWaveComposition(guardConfig, waveNumber);
            
            if (!waveComposition.Any())
            {
                Logger.Log.Warning($"Area {area.Name}: No backup types available for wave {waveNumber + 1}");
                return;
            }

            // Spawn each unit in the wave
            foreach (BackupType backupType in waveComposition)
            {
                Logger.Log.Info($"Area {area.Name}: Wave {waveNumber + 1} - spawning {backupType}");
                
                BackupSquad squad = null;

                switch (backupType)
                {
                    case BackupType.Airstrike:
                        squad = SpawnAttackHelicopterBackup(area, guardConfig, spawnCenter);
                        break;

                    case BackupType.AerialBackup:
                        squad = SpawnTacticalHelicopterBackup(area, guardConfig, spawnCenter);
                        break;

                    case BackupType.GroundVehicle:
                        squad = SpawnGroundVehicleBackup(area, guardConfig, spawnCenter);
                        break;
                }

                if (squad != null)
                {
                    // Add to area's backup squad list
                    if (!_areaBackupSquads.ContainsKey(area.Name))
                    {
                        _areaBackupSquads[area.Name] = new List<BackupSquad>();
                    }
                    _areaBackupSquads[area.Name].Add(squad);

                    Logger.Log.Info($"Area {area.Name}: Wave {waveNumber + 1} - successfully spawned {backupType} squad");
                }
                else
                {
                    Logger.Log.Error($"Area {area.Name}: Wave {waveNumber + 1} - failed to spawn {backupType} squad");
                }
            }

            Logger.Log.Info($"Area {area.Name}: Wave {waveNumber + 1} complete - spawned {waveComposition.Count} units");
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Area {area.Name}: Failed to spawn backup wave {waveNumber + 1}: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate wave composition based on available backup types and wave number
    /// Priority: Helicopters first (if available), then ground vehicles
    /// If no helicopters configured, aerial dispatch is disabled
    /// If no vehicles configured, ground dispatch is disabled
    /// If neither configured, returns empty list (no backup spawning)
    /// </summary>
    private List<BackupType> GenerateWaveComposition(GuardConfig config, int waveNumber)
    {
        List<BackupType> availableTypes = new List<BackupType>();

        // Check for helicopter availability - if none, aerial dispatch disabled
        if (config.HVehicleModels != null && config.HVehicleModels.Any())
        {
            availableTypes.Add(BackupType.AerialBackup); // Tactical helicopter
            availableTypes.Add(BackupType.Airstrike);    // Attack helicopter
        }
        // Note: If no helicopters in config, aerial dispatch is completely disabled

        // Check for vehicle availability - if none, ground dispatch disabled  
        if (config.VehicleModels != null && config.VehicleModels.Any())
        {
            availableTypes.Add(BackupType.GroundVehicle);
        }
        // Note: If no vehicles in config, ground dispatch is completely disabled

        // If neither helicopters nor vehicles are configured, no backup spawning
        if (!availableTypes.Any())
            return new List<BackupType>();

        // Determine number of units in this wave
        int unitsInWave = 1; // Base 1 unit
        
        // Higher waves can have multiple units
        if (waveNumber >= 2) unitsInWave = 2; // Waves 3+ can have 2 units
        if (waveNumber >= 5) unitsInWave = 3; // Waves 6+ can have 3 units
        
        // But don't exceed available types
        unitsInWave = Math.Min(unitsInWave, availableTypes.Count);

        // Select random units for this wave from available types
        List<BackupType> waveComposition = new List<BackupType>();
        List<BackupType> availableForWave = new List<BackupType>(availableTypes);
        
        for (int i = 0; i < unitsInWave && availableForWave.Any(); i++)
        {
            int randomIndex = _random.Next(availableForWave.Count);
            waveComposition.Add(availableForWave[randomIndex]);
            availableForWave.RemoveAt(randomIndex);
        }

        return waveComposition;
    }

    /// <summary>
    /// Spawn automatic backup for an area that's in combat
    /// Uses existing backup classes but manages them under area control
    /// </summary>
    private void SpawnAutomaticBackupForArea(Area area)
    {
        if (area == null || !guardConfigs.ContainsKey(area.Model)) return;

        // Don't spawn if we already have active backups for this area
        if (_areaBackupSquads.ContainsKey(area.Name) && _areaBackupSquads[area.Name].Any(s => s.IsActive))
        {
            Logger.Log.Info($"Area {area.Name}: Already has active backup squads, skipping spawn");
            return;
        }

        var guardConfig = guardConfigs[area.Model];
        Vector3 spawnCenter = area.GetCentroid();

        Logger.Log.Info($"Area {area.Name}: Spawning automatic backup at {spawnCenter}");

        try
        {
            // Randomly choose backup type based on available configurations
            List<BackupType> availableTypes = new List<BackupType>();

            if (guardConfig.HVehicleModels != null && guardConfig.HVehicleModels.Any())
            {
                availableTypes.Add(BackupType.AerialBackup); // Tactical helicopter
                availableTypes.Add(BackupType.Airstrike);    // Attack helicopter
            }

            if (guardConfig.VehicleModels != null && guardConfig.VehicleModels.Any())
            {
                availableTypes.Add(BackupType.GroundVehicle);
            }

            if (!availableTypes.Any())
            {
                Logger.Log.Warning($"Area {area.Name}: No backup types available in guard config");
                return;
            }

            // Select random backup type
            BackupType selectedType = availableTypes[_random.Next(availableTypes.Count)];
            Logger.Log.Info($"Area {area.Name}: Selected backup type: {selectedType}");

            BackupSquad squad = null;

            switch (selectedType)
            {
                case BackupType.Airstrike:
                    squad = SpawnAttackHelicopterBackup(area, guardConfig, spawnCenter);
                    break;

                case BackupType.AerialBackup:
                    squad = SpawnTacticalHelicopterBackup(area, guardConfig, spawnCenter);
                    break;

                case BackupType.GroundVehicle:
                    squad = SpawnGroundVehicleBackup(area, guardConfig, spawnCenter);
                    break;
            }

            if (squad != null)
            {
                // Add to area's backup squad list
                if (!_areaBackupSquads.ContainsKey(area.Name))
                {
                    _areaBackupSquads[area.Name] = new List<BackupSquad>();
                }
                _areaBackupSquads[area.Name].Add(squad);

                Logger.Log.Info($"Area {area.Name}: Successfully spawned {selectedType} backup squad");
            }
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Area {area.Name}: Failed to spawn automatic backup: {ex.Message}");
        }
    }

    /// <summary>
    /// Spawn an attack helicopter backup squad for the area
    /// </summary>
    private BackupSquad SpawnAttackHelicopterBackup(Area area, GuardConfig config, Vector3 spawnCenter)
    {
        try
        {
            // Get random helicopter model
            if (config.HVehicleModels == null || !config.HVehicleModels.Any())
                return null;

            string heliModelName = config.HVehicleModels[_random.Next(config.HVehicleModels.Count)];
            VehicleHash heliHash;
            if (!Enum.TryParse(heliModelName, true, out heliHash))
                return null;

            Model heliModel = new Model(heliHash);
            heliModel.Request(2000);
            if (!heliModel.IsLoaded)
                return null;

            // Find spawn point for aircraft
            if (!HelperClass.FindSpawnPointForAircraft(Game.Player.Character, spawnCenter, 200f, 400f, 100f, out Vector3 spawnPos, out float spawnHeading))
            {
                Logger.Log.Error($"Failed to find spawn point for attack helicopter in area {area.Name}");
                heliModel.MarkAsNoLongerNeeded();
                return null;
            }

            // Create helicopter
            Vehicle helicopter = World.CreateVehicle(heliModel, spawnPos, spawnHeading);
            if (helicopter == null || !helicopter.Exists())
            {
                heliModel.MarkAsNoLongerNeeded();
                return null;
            }

            // Configure helicopter
            helicopter.PopulationType = EntityPopulationType.Mission;
            helicopter.IsEngineRunning = true;
            helicopter.Health = 10000;
            helicopter.BodyHealth = 10000;
            helicopter.EngineHealth = 10000;
            helicopter.HeliEngineHealth = 10000;
            helicopter.HeliMainRotorHealth = 10000;
            helicopter.HeliTailRotorHealth = 10000;
            helicopter.MaxHealth = 10000;
            helicopter.PetrolTankHealth = 10000;
            helicopter.MaxHealthFloat = 10000;
            helicopter.HealthFloat = 10000;

            // Create crew
            List<BackupGuard> crew = new List<BackupGuard>();
            Model guardModel = GetRandomPedModel(config);
            guardModel.Request(1000);

            if (guardModel.IsLoaded)
            {
                for (int seat = -1; seat < helicopter.PassengerCapacity; seat++)
                {
                    Ped guard = helicopter.CreatePedOnSeat((VehicleSeat)seat, guardModel);
                    if (guard != null && guard.Exists())
                    {
                        SetupBackupGuard(guard, config, area);
                        crew.Add(new BackupGuard
                        {
                            Ped = guard,
                            InitialHealth = 10000,
                            InitialAmmo = 9999,
                            HasWeapon = true
                        });
                    }
                }
                guardModel.MarkAsNoLongerNeeded();
            }

            // Create attack helicopter controller
            AttackHelicopter attackHeli = new AttackHelicopter(helicopter, config, area);

            // Create squad
            var squad = new BackupSquad
            {
                SquadType = BackupType.Airstrike,
                Vehicle = helicopter,
                Guards = crew,
                SpawnTime = DateTime.Now,
                IsActive = true,
                InitialGuardCount = crew.Count,
                InitialVehicleHealth = helicopter.HeliEngineHealth,
                TacticalHelicopter = attackHeli
            };

            heliModel.MarkAsNoLongerNeeded();
            Logger.Log.Info($"Spawned attack helicopter backup for area {area.Name} with {crew.Count} crew");
            return squad;
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Failed to spawn attack helicopter backup: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Spawn a tactical helicopter backup squad for the area
    /// </summary>
    private BackupSquad SpawnTacticalHelicopterBackup(Area area, GuardConfig config, Vector3 spawnCenter)
    {
        try
        {
            // Get random helicopter model
            if (config.HVehicleModels == null || !config.HVehicleModels.Any())
                return null;

            string heliModelName = config.HVehicleModels[_random.Next(config.HVehicleModels.Count)];
            VehicleHash heliHash;
            if (!Enum.TryParse(heliModelName, true, out heliHash))
                return null;

            Model heliModel = new Model(heliHash);
            heliModel.Request(2000);
            if (!heliModel.IsLoaded)
                return null;

            // Find spawn point for aircraft
            if (!HelperClass.FindSpawnPointForAircraft(Game.Player.Character, spawnCenter, 200f, 400f, 100f, out Vector3 spawnPos, out float spawnHeading))
            {
                Logger.Log.Error($"Failed to find spawn point for tactical helicopter in area {area.Name}");
                heliModel.MarkAsNoLongerNeeded();
                return null;
            }

            // Create helicopter
            Vehicle helicopter = World.CreateVehicle(heliModel, spawnPos, spawnHeading);
            if (helicopter == null || !helicopter.Exists())
            {
                heliModel.MarkAsNoLongerNeeded();
                return null;
            }

            // Configure helicopter
            helicopter.PopulationType = EntityPopulationType.Mission;
            Function.Call(Hash.SET_VEHICLE_ENGINE_ON, helicopter, true, true, false);
            helicopter.Health = 10000;
            helicopter.BodyHealth = 10000;
            helicopter.EngineHealth = 10000;
            helicopter.HeliEngineHealth = 10000;
            helicopter.HeliMainRotorHealth = 10000;
            helicopter.HeliTailRotorHealth = 10000;
            helicopter.MaxHealth = 10000;
            helicopter.PetrolTankHealth = 10000;
            helicopter.MaxHealthFloat = 10000;
            helicopter.HealthFloat = 10000;

            // Create crew
            List<BackupGuard> crew = new List<BackupGuard>();
            Model guardModel = GetRandomPedModel(config);
            guardModel.Request(1000);

            if (guardModel.IsLoaded)
            {
                for (int seat = -1; seat < helicopter.PassengerCapacity; seat++)
                {
                    Ped guard = helicopter.CreatePedOnSeat((VehicleSeat)seat, guardModel);
                    if (guard != null && guard.Exists())
                    {
                        SetupBackupGuard(guard, config, area);
                        crew.Add(new BackupGuard
                        {
                            Ped = guard,
                            InitialHealth = 10000,
                            InitialAmmo = 9999,
                            HasWeapon = true
                        });
                    }
                }
                guardModel.MarkAsNoLongerNeeded();
            }

            // Create tactical helicopter controller
            TacticalHelicopter tacticalHeli = new TacticalHelicopter(helicopter, spawnCenter, config, area)
            {
                Rappel = helicopter.PassengerCapacity <= 4, // Small helicopters rappel
                Land = helicopter.PassengerCapacity > 4     // Large helicopters land
            };

            // Create squad
            var squad = new BackupSquad
            {
                SquadType = BackupType.AerialBackup,
                Vehicle = helicopter,
                Guards = crew,
                SpawnTime = DateTime.Now,
                IsActive = true,
                InitialGuardCount = crew.Count,
                InitialVehicleHealth = helicopter.HeliEngineHealth,
                TacticalHelicopter = tacticalHeli,
                DeploymentMode = tacticalHeli.Rappel ? "Rappel" : "Landing"
            };

            heliModel.MarkAsNoLongerNeeded();
            Logger.Log.Info($"Spawned tactical helicopter backup for area {area.Name} with {crew.Count} crew, mode: {squad.DeploymentMode}");
            return squad;
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Failed to spawn tactical helicopter backup: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Spawn a ground vehicle backup squad for the area
    /// </summary>
    private BackupSquad SpawnGroundVehicleBackup(Area area, GuardConfig config, Vector3 spawnCenter)
    {
        try
        {
            // Get random vehicle model
            if (config.VehicleModels == null || !config.VehicleModels.Any())
                return null;

            string vehicleModelName = config.VehicleModels[_random.Next(config.VehicleModels.Count)];
            VehicleHash vehicleHash;
            if (!Enum.TryParse(vehicleModelName, true, out vehicleHash))
                return null;

            Model vehicleModel = new Model(vehicleHash);
            vehicleModel.Request(2000);
            if (!vehicleModel.IsLoaded)
                return null;

            // Find spawn point for automobile
            if (!HelperClass.FindSpawnPointForAutomobile(Game.Player.Character, spawnCenter, 150f, 200f, out Vector3 spawnPos, out float spawnHeading))
            {
                Logger.Log.Error($"Failed to find spawn point for ground vehicle in area {area.Name}");
                vehicleModel.MarkAsNoLongerNeeded();
                return null;
            }

            // Create vehicle
            Vehicle vehicle = World.CreateVehicle(vehicleModel, spawnPos, spawnHeading);
            if (vehicle == null || !vehicle.Exists())
            {
                vehicleModel.MarkAsNoLongerNeeded();
                return null;
            }

            // Configure vehicle
            vehicle.PopulationType = EntityPopulationType.Mission;
            Function.Call(Hash.SET_VEHICLE_ENGINE_ON, vehicle, true, true, false);

            // Create crew
            List<BackupGuard> crew = new List<BackupGuard>();
            Model guardModel = GetRandomPedModel(config);
            guardModel.Request(1000);

            if (guardModel.IsLoaded)
            {
                for (int seat = -1; seat < vehicle.PassengerCapacity; seat++)
                {
                    Ped guard = vehicle.CreatePedOnSeat((VehicleSeat)seat, guardModel);
                    if (guard != null && guard.Exists())
                    {
                        SetupBackupGuard(guard, config, area);
                        crew.Add(new BackupGuard
                        {
                            Ped = guard,
                            InitialHealth = 10000,
                            InitialAmmo = 9999,
                            HasWeapon = true
                        });
                    }
                }
                guardModel.MarkAsNoLongerNeeded();
            }

            // Create ground vehicle controller
            GroundVehicle groundVehicle = new GroundVehicle(vehicle, spawnCenter);

            // Create squad
            var squad = new BackupSquad
            {
                SquadType = BackupType.GroundVehicle,
                Vehicle = vehicle,
                Guards = crew,
                SpawnTime = DateTime.Now,
                IsActive = true,
                InitialGuardCount = crew.Count,
                InitialVehicleHealth = vehicle.HealthFloat,
                TacticalHelicopter = groundVehicle
            };

            vehicleModel.MarkAsNoLongerNeeded();
            Logger.Log.Info($"Spawned ground vehicle backup for area {area.Name} with {crew.Count} crew");
            return squad;
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Failed to spawn ground vehicle backup: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Setup a backup guard with proper configuration
    /// </summary>
    private void SetupBackupGuard(Ped guard, GuardConfig config, Area area)
    {
        if (guard == null || !guard.Exists()) return;

        // Basic setup
        guard.PopulationType = EntityPopulationType.Mission;
        guard.MaxHealth = 10000;
        guard.Health = 10000;
        guard.Armor = 5000;
        guard.DiesOnLowHealth = false;
    // Ensure guard is not left in an unusual non-reactive state
    try { guard.BlockPermanentEvents = false; guard.KeepTaskWhenMarkedAsNoLongerNeeded = false; } catch { }

        // Combat attributes
        guard.SetCombatAttribute(CombatAttributes.AlwaysFight, true);
        guard.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
        guard.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, true);
        guard.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, true);
        guard.SetCombatAttribute(CombatAttributes.CanUseCover, true);
        guard.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
        guard.SetCombatAttribute(CombatAttributes.AlwaysFlee, false);
        guard.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, false);
        guard.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);
        guard.SetCombatAttribute(CombatAttributes.CanChaseTargetOnFoot, true);
        guard.SetCombatAttribute(CombatAttributes.SwitchToDefensiveIfInCover, true);
        guard.SetCombatAttribute(CombatAttributes.BlindFireWhenInCover, true);
        guard.SetCombatAttribute(CombatAttributes.CanUsePeekingVariations, true);
        guard.SetCombatAttribute(CombatAttributes.CanTauntInVehicle, true);
        guard.SetCombatAttribute(CombatAttributes.AlwaysEquipBestWeapon, true);

        // Ensure backup guards will use vehicle-mounted weapons when present
        try
        {
            guard.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true);
            guard.SetCombatAttribute(CombatAttributes.UseVehicleAttackIfVehicleHasMountedGuns, true);
            guard.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, false);
        }
        catch { }

        // Config flags
        guard.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, true);
        guard.SetConfigFlag(PedConfigFlagToggles.CanDiveAwayFromApproachingVehicles, true);
        guard.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, true);
        guard.SetConfigFlag(PedConfigFlagToggles.AIDriverAllowFriendlyPassengerSeatEntry, true);
        guard.SetConfigFlag(PedConfigFlagToggles.KeepRelationshipGroupAfterCleanUp, true);
        guard.SetConfigFlag(PedConfigFlagToggles.AllowMedicsToReviveMe, true);
        guard.SetConfigFlag(PedConfigFlagToggles.KeepTasksAfterCleanUp, true);
        guard.SetConfigFlag(PedConfigFlagToggles.TargetWhenInjuredAllowed, false);
        guard.SetConfigFlag(PedConfigFlagToggles.CanAttackFriendly, false);
        guard.SetConfigFlag(PedConfigFlagToggles.DisableBlindFiringInShotReactions, false);
    guard.SetConfigFlag(PedConfigFlagToggles.AvoidTearGas, true);
        guard.SetConfigFlag(PedConfigFlagToggles.AllowMissionPedToUseInjuredMovement, true);

        // Give weapons
        WeaponHash weapon = GetRandomWeapon(config);
        guard.Weapons.Give(weapon, 9999, true, true);
        guard.Weapons.Give(WeaponHash.MicroSMG, 9999, false, true);
        guard.Weapons.Give(WeaponHash.APPistol, 9999, false, true);
    guard.Weapons.Give(WeaponHash.Knife, 1, false, true);
    guard.Weapons.Give(WeaponHash.Bat, 1, false, true);

        // Setup relationships using same logic as regular guards
        SetupGuardRelationships(guard, config, area);
    }

    /// <summary>
    /// Get a random ped model from guard config
    /// </summary>
    private Model GetRandomPedModel(GuardConfig config)
    {
        if (config.PedModels.Count == 0)
        {
            return new Model(PedHash.Blackops01SMY);
        }

        string pedName = config.PedModels[_random.Next(config.PedModels.Count)];
        return new Model(pedName);
    }

    /// <summary>
    /// Get a random weapon from guard config
    /// </summary>
    private WeaponHash GetRandomWeapon(GuardConfig config)
    {
        if (config.Weapons.Count == 0)
        {
            return WeaponHash.CarbineRifle;
        }

        string weaponName = config.Weapons[_random.Next(config.Weapons.Count)];

        if (Enum.TryParse(weaponName, true, out WeaponHash weaponHash))
        {
            return weaponHash;
        }

        return WeaponHash.CarbineRifle;
    }

    /// <summary>
    /// Update all backup squads for all areas
    /// Called from CheckAllTime to manage backup lifecycle
    /// </summary>
    private void UpdateAreaBackupSquads()
    {
        foreach (var area in areas)
        {
            if (_areaBackupSquads.ContainsKey(area.Name))
            {
                UpdateAreaBackupSquads(area, _areaBackupSquads[area.Name]);
            }
        }
    }

    /// <summary>
    /// Update backup squads for a specific area
    /// Manages the lifecycle of backup units based on combat status
    /// </summary>
    private void UpdateAreaBackupSquads(Area area, List<BackupSquad> squads)
    {
        bool areaInCombat = IsAreaInCombat(area);

        // Debugging removed - production cleanup only

        for (int i = squads.Count - 1; i >= 0; i--)
        {
            var squad = squads[i];
            bool shouldRemoveSquad = false;

            try
            {
                // Update AI controllers if they exist
                if (squad.TacticalHelicopter != null)
                {
                    if (squad.TacticalHelicopter is AttackHelicopter attackHeli)
                    {
                        if (attackHeli.IsHelicopterValid())
                        {
                            // Check if helicopter is fleeing (mission complete)
                            if (attackHeli.CurrentState == AttackHelicopter.HelicopterState.Flee)
                            {
                                Logger.Log.Info($"Area {area.Name}: Attack helicopter is fleeing, marking for removal");
                                shouldRemoveSquad = true;
                            }
                            else
                            {
                                attackHeli.Update();
                            }
                        }
                        else
                        {
                            Logger.Log.Info($"Area {area.Name}: Attack helicopter destroyed/invalid, removing squad");
                            shouldRemoveSquad = true;
                        }
                    }
                    else if (squad.TacticalHelicopter is TacticalHelicopter tacticalHeli)
                    {
                        if (tacticalHeli.IsHelicopterValid())
                        {
                            // If the tactical heli reports deployment completion states, mark squad as deployed
                            try
                            {
                                if (!squad.DeploymentComplete &&
                                    (tacticalHeli.CurrentTask == TacticalHelicopter.Task.RappelComplete ||
                                     tacticalHeli.CurrentTask == TacticalHelicopter.Task.LandingComplete ||
                                     tacticalHeli.CurrentTask == TacticalHelicopter.Task.ParatroopComplete))
                                {
                                    squad.DeploymentComplete = true;
                                    squad.DeploymentCompleteTime = DateTime.Now;
                                    Logger.Log.Info($"Area {area.Name}: Squad deployment completed at {squad.DeploymentCompleteTime}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Warning($"Error checking tactical heli deployment state: {ex.Message}");
                            }

                            // Check if helicopter is fleeing (mission complete)
                            if (tacticalHeli.CurrentTask == TacticalHelicopter.Task.Flee)
                            {
                                // Don't remove the squad while troops are still deploying (rappelling/landing).
                                bool guardsStillDeploying = false;
                                try
                                {
                                    if (squad.Vehicle != null && squad.Vehicle.Exists())
                                    {
                                        foreach (var bg in squad.Guards)
                                        {
                                            if (bg.Ped == null || !bg.Ped.Exists()) continue;

                                            // If any guard is still inside the vehicle, they may be mid-rappel/exit
                                            if (bg.Ped.IsInVehicle(squad.Vehicle))
                                            {
                                                guardsStillDeploying = true;
                                                break;
                                            }

                                            // Also check the script task status for rappel to catch in-progress tasks
                                            var status = bg.Ped.GetScriptTaskStatus(ScriptTaskNameHash.RappelFromHeli);
                                            if (status == ScriptTaskStatus.Performing || status == ScriptTaskStatus.WaitingToStart)
                                            {
                                                guardsStillDeploying = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log.Warning($"Error checking guard deploy status: {ex.Message}");
                                }

                                if (guardsStillDeploying)
                                {
                                    Logger.Log.Info($"Area {area.Name}: Tactical helicopter is fleeing but guards still deploying; deferring removal");
                                }
                                else
                                {
                                    // If we have a deployment-complete timestamp, apply a short grace delay
                                    const double DEPLOYMENT_CLEANUP_GRACE_SECONDS = 3.0;
                                    if (squad.DeploymentComplete)
                                    {
                                        var elapsed = (DateTime.Now - squad.DeploymentCompleteTime).TotalSeconds;
                                        if (elapsed < DEPLOYMENT_CLEANUP_GRACE_SECONDS)
                                        {
                                            Logger.Log.Info($"Area {area.Name}: Waiting grace period after deployment ({elapsed:F1}s)");
                                        }
                                        else
                                        {
                                            Logger.Log.Info($"Area {area.Name}: Tactical helicopter is fleeing, marking for removal");
                                            shouldRemoveSquad = true;
                                        }
                                    }
                                    else
                                    {
                                        // No explicit deployment flag and no guards deploying -> safe to remove
                                        Logger.Log.Info($"Area {area.Name}: Tactical helicopter is fleeing, marking for removal");
                                        shouldRemoveSquad = true;
                                    }
                                }
                            }
                            else
                            {
                                tacticalHeli.Update();
                            }
                        }
                        else
                        {
                            Logger.Log.Info($"Area {area.Name}: Tactical helicopter destroyed/invalid, removing squad");
                            shouldRemoveSquad = true;
                        }
                    }
                    else if (squad.TacticalHelicopter is GroundVehicle groundVehicle)
                    {
                        if (groundVehicle.IsVehicleValid())
                        {
                            // Ground vehicles don't have a built-in flee state, check distance
                            float distance = Game.Player.Character.Position.DistanceTo(groundVehicle.Vehicle.Position);
                            if (distance > 500f) // Far enough to consider mission complete
                            {
                                Logger.Log.Info($"Area {area.Name}: Ground vehicle is far away ({distance:F1}m), marking for removal");
                                shouldRemoveSquad = true;
                            }
                        }
                        else
                        {
                            Logger.Log.Info($"Area {area.Name}: Ground vehicle destroyed/invalid, removing squad");
                            shouldRemoveSquad = true;
                        }
                    }
                }

                // Check vehicle destruction
                if (squad.Vehicle != null && !squad.Vehicle.Exists())
                {
                    Logger.Log.Info($"Area {area.Name}: Squad vehicle destroyed, removing squad");
                    shouldRemoveSquad = true;
                }

                // Check if all guards are dead
                bool allGuardsDead = true;
                foreach (var backupGuard in squad.Guards)
                {
                    if (backupGuard.Ped != null && backupGuard.Ped.Exists() && !backupGuard.Ped.IsDead)
                    {
                        allGuardsDead = false;
                        break;
                    }
                }

                if (allGuardsDead)
                {
                    Logger.Log.Info($"Area {area.Name}: All squad guards dead, removing squad");
                    shouldRemoveSquad = true;
                }

                // Handle combat end detection and dismissal
                if (!areaInCombat && squad.IsActive)
                {
                    // Area no longer in combat
                    if (!squad.CombatEndCheck)
                    {
                        // Start combat end timer
                        squad.CombatEndCheck = true;
                        squad.CombatEndTime = DateTime.Now;
                        Logger.Log.Info($"Area {area.Name}: Combat ended, starting 30-second dismissal timer for {squad.SquadType}");
                    }
                    else
                    {
                        // Check if 30 seconds have passed
                        TimeSpan timeSinceCombatEnd = DateTime.Now - squad.CombatEndTime;
                        if (timeSinceCombatEnd.TotalSeconds >= 30)
                        {
                            Logger.Log.Info($"Area {area.Name}: 30 seconds passed since combat end, dismissing {squad.SquadType}");
                            DismissBackupSquad(squad);
                            shouldRemoveSquad = true;
                        }
                    }
                }
                else if (areaInCombat && squad.CombatEndCheck)
                {
                    // Combat resumed, cancel dismissal
                    squad.CombatEndCheck = false;
                    Logger.Log.Info($"Area {area.Name}: Combat resumed, canceling dismissal timer for {squad.SquadType}");
                }

                // Remove squad if marked for removal
                if (shouldRemoveSquad)
                {
                    CleanupBackupSquad(squad);
                    squads.RemoveAt(i);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"Error updating squad in area {area.Name}: {ex.Message}");
                // Remove problematic squad
                CleanupBackupSquad(squad);
                squads.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Dismiss a backup squad (make them leave the area)
    /// </summary>
    private void DismissBackupSquad(BackupSquad squad)
    {
        try
        {
            if (squad.TacticalHelicopter != null)
            {
                if (squad.TacticalHelicopter is AttackHelicopter attackHeli)
                {
                    attackHeli.DismissTeam();
                    Logger.Log.Info($"Dismissed attack helicopter squad");
                }
                else if (squad.TacticalHelicopter is TacticalHelicopter tacticalHeli)
                {
                    // Use public method instead of private StartFleeTask
                    tacticalHeli.CurrentTask = TacticalHelicopter.Task.Flee;
                    Logger.Log.Info($"Dismissed tactical helicopter squad");
                }
                else if (squad.TacticalHelicopter is GroundVehicle groundVehicle)
                {
                    groundVehicle.Task = GroundVehicle.VehicleTask.Flee;
                    Logger.Log.Info($"Dismissed ground vehicle squad");
                }
            }

            // Clean up blips
            if (squad.VehicleBlip != null && squad.VehicleBlip.Exists())
            {
                squad.VehicleBlip.Delete();
            }

            foreach (var guard in squad.Guards)
            {
                if (guard.Blip != null && guard.Blip.Exists())
                {
                    guard.Blip.Delete();
                }
            }

            squad.IsActive = false;
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Error dismissing backup squad: {ex.Message}");
        }
    }

    /// <summary>
    /// Clean up a backup squad (delete entities)
    /// </summary>
    private void CleanupBackupSquad(BackupSquad squad)
    {
        try
        {
            // Instead of forcing Delete() which can leave race conditions with scripts,
            // mark entities as no longer needed so the game can GC them gracefully.
            // Vehicle
            if (squad.Vehicle != null && squad.Vehicle.Exists())
            {
                try
                {
                    // Mark occupants and vehicle as no longer needed
                    foreach (var occ in squad.Vehicle.Occupants)
                    {
                        if (occ != null && occ.Exists())
                        {
                            occ.MarkAsNoLongerNeeded();
                        }
                    }
                    squad.Vehicle.MarkAsNoLongerNeeded();
                }
                catch (Exception ex)
                {
                    Logger.Log.Warning($"Error marking vehicle/occupants as no longer needed: {ex.Message}");
                }
            }

            // Clean up vehicle blip
            if (squad.VehicleBlip != null && squad.VehicleBlip.Exists())
            {
                try { squad.VehicleBlip.Delete(); } catch { }
            }

            // Clean up guards: mark as no longer needed and remove blips
            foreach (var guard in squad.Guards)
            {
                try
                {
                    if (guard.Ped != null && guard.Ped.Exists())
                    {
                        guard.Ped.MarkAsNoLongerNeeded();
                    }

                    if (guard.Blip != null && guard.Blip.Exists())
                    {
                        guard.Blip.Delete();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Warning($"Error cleaning up backup guard in squad cleanup: {ex.Message}");
                }
            }

            Logger.Log.Info($"Marked backup squad entities as no longer needed: {squad.SquadType}");
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Error cleaning up backup squad: {ex.Message}");
        }
    }

    /// <summary>
    /// Main update loop for all time-based guard operations
    /// Called every frame to handle shift changes, backup management, and area combat detection
    /// </summary>
    private void CheckAllTime()
    {
        // Handle legacy shift changes for areas (still used by some areas)
        foreach (var area in areas)
        {
            CheckShiftChanges(area);
        }

        // Update all backup squads across all areas
        UpdateAreaBackupSquads();

        // Check each area for combat and handle automatic backups
        foreach (var area in areas)
        {
            HandleAreasBackups(area);
        }
    }

    /// <summary>
    /// Get blip color based on which character the area respects
    /// Franklin = Green, Michael = Blue, Trevor = Orange
    /// </summary>
    private BlipColor GetBlipColorForArea(Area area)
    {
        if (area == null || string.IsNullOrEmpty(area.Respect))
            return BlipColor.White;

        string respect = area.Respect.ToUpperInvariant();

        if (respect.Contains("FRANKLIN") && Game.Player.Character.Model == PedHash.Franklin)
            return BlipColor.Green;

        if (respect.Contains("MICHAEL") && Game.Player.Character.Model == PedHash.Michael)
            return BlipColor.Blue;

        if (respect.Contains("TREVOR") && Game.Player.Character.Model == PedHash.Trevor)
            return BlipColor.Orange;

        return BlipColor.White;
    }
}

