using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using Guarding.Core.Enums;
using iFruitAddon2;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

/// <summary>
/// Clean backup dispatch system with phone calls and NO billing
/// Guards stay until dead or dismissed
/// </summary>
    public class BackupDispatchSystem
    {
        // Dispatch Fees and Cooldowns are now loaded from Areas.xml dynamically
        // These are fallback defaults if no area configuration is found
        private const int DEFAULT_AIRSTRIKE_FEE = 50000;
        private const int DEFAULT_AERIAL_BACKUP_FEE = 5000;
        private const int DEFAULT_GROUND_BACKUP_FEE = 15000;
        
        private const int DEFAULT_COOLDOWN_AIRSTRIKE = 30;
        private const int DEFAULT_COOLDOWN_AERIAL = 30;
        private const int DEFAULT_COOLDOWN_GROUND = 30;
        
        // Dismiss key
        private const Keys DISMISS_KEY = Keys.P;
        private DateTime _lastDismissKeyPress = DateTime.MinValue;
        private float _dismissHoldTime = 0f;
        private const float DISMISS_HOLD_DURATION = 1f; // Hold for 2 seconds
        
        
        private CustomiFruit _phone;
        private iFruitContact _airstrikeContact;
        private iFruitContact _aerialBackupContact;
        private iFruitContact _groundBackupContact;
        
        public static List<BackupSquad> _activeSquads = new List<BackupSquad>();
        
        private DateTime _lastAirstrikeCall = DateTime.MinValue;
        private DateTime _lastAerialCall = DateTime.MinValue;
        private DateTime _lastGroundCall = DateTime.MinValue;
        
        private Random _random = new Random();
        
        // Area and Guard configuration
        private XmlReader _xmlReader;
        private List<Area> _areas = new List<Area>();
        private Dictionary<string, GuardConfig> _guardConfigs = new Dictionary<string, GuardConfig>();
        
        
        public BackupDispatchSystem()
        {
            InitializeXmlData();
            InitializePhone();
            Logger.Log.Info("BackupDispatchSystem initialized");
        }
        
        /// <summary>
        /// Called every tick - must be invoked from the main Script's Tick event
        /// </summary>
        public void Update()
        {
            try
            {
                _phone?.Update();
                UpdateSquads();
                CheckDismissKey();
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"BackupDispatchSystem error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if player is holding E to dismiss backup
        /// </summary>
        private void CheckDismissKey()
        {
            if (_activeSquads.Count == 0) return;
            
            if (Game.IsKeyPressed(DISMISS_KEY))
            {
                if (_lastDismissKeyPress == DateTime.MinValue)
                {
                    _lastDismissKeyPress = DateTime.Now;
                    _dismissHoldTime = 0f;
                }
                
                _dismissHoldTime += Game.LastFrameTime;
                
                // Show progress
                float progress = _dismissHoldTime / DISMISS_HOLD_DURATION;
                if (progress < 1f)
                {
                    int barWidth = 20;
                    int filled = (int)(barWidth * progress);
                    string bar = new string('|', filled) + new string('-', barWidth - filled);
                    HelperClass.Subtitle($"~y~Hold P to dismiss backup [{bar}] {(progress * 100):F0}%");
                }
                
                // Dismiss when held long enough
                if (_dismissHoldTime >= DISMISS_HOLD_DURATION)
                {
                    DismissAllBackup();
                    _lastDismissKeyPress = DateTime.MinValue;
                    _dismissHoldTime = 0f;
                }
            }
            else
            {
                // Reset on release
                _lastDismissKeyPress = DateTime.MinValue;
                _dismissHoldTime = 0f;
            }
        }
        
        /// <summary>
        /// Dismiss all active backup squads
        /// </summary>
        private void DismissAllBackup()
        {
            if (_activeSquads.Count == 0) return;
            
            //HelperClass.Notification($"~g~Dismissing ~b~{_activeSquads.Count} ~w~backup squad(s)...");
            Logger.Log.Info($"Player dismissed {_activeSquads.Count} backup squads");
            
            foreach (var squad in _activeSquads.ToList())
            {
                DismissSquad(squad);
            }
            
            _activeSquads.Clear();
            //HelperClass.Notification("~g~All backup dismissed");
        }
        
        /// <summary>
        /// Dismiss a specific squad - make them leave
        /// </summary>
        private void DismissSquad(BackupSquad squad)
        {
            try
            {
                if (squad.SquadType == BackupType.Airstrike || squad.SquadType == BackupType.AerialBackup)
                {
                    // Helicopters flee
                    if (squad.TacticalHelicopter is AttackHelicopter attackHeli)
                    {
                        attackHeli.CurrentState = AttackHelicopter.HelicopterState.ReadyToFlee;
                    }
                    else if (squad.TacticalHelicopter is TacticalHelicopter tacticalHeli)
                    {
                        tacticalHeli.CurrentTask = TacticalHelicopter.Task.Flee;
                    }
                }
                else if (squad.SquadType == BackupType.GroundVehicle)
                {
                    // Ground vehicles drive away
                    if (squad.TacticalHelicopter is GroundVehicle groundVehicle)
                    {
                    groundVehicle.Task = GroundVehicle.VehicleTask.Flee;
                    }
                }
                
                // Clean up blips
                squad.VehicleBlip?.Delete();
                foreach (var guard in squad.Guards)
                {
                    guard.Blip?.Delete();
                }
                
                Logger.Log.Info($"Dismissed {squad.SquadType} squad");
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"Error dismissing squad: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Cleanup method to be called when script is aborted
        /// </summary>
        public void Cleanup()
        {
            try
            {
                // Cleanup all active squads
                foreach (var squad in _activeSquads.ToList())
                {
                    CleanupSquad(squad);
                }
                _activeSquads.Clear();
                
                Logger.Log.Info("BackupDispatchSystem cleaned up");
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"Error during BackupDispatchSystem cleanup: {ex.Message}");
            }
        }
        
        private void InitializeXmlData()
        {
            try
            {
                // Use the same path as GuardSpawner - let XmlReader handle everything
                string areasPath = "./scripts/GTA/Areas.xml";
                _xmlReader = new XmlReader(areasPath);
                
                // Load guard configurations from Guards.xml (XmlReader finds it automatically)
                _guardConfigs = _xmlReader.LoadGuardConfigs();
                Logger.Log.Info($"✓ Loaded {_guardConfigs.Count} guard configurations from Guards.xml");

            // Load scenarios from ScenarioLists.xml (XmlReader finds it automatically)
            var scenarios = _xmlReader.LoadScenarios();
                Logger.Log.Info($"✓ Loaded {scenarios.Count} scenario configurations");
                
                // Load areas from Areas.xml with scenarios
                _areas = _xmlReader.LoadAreasFromXml(scenarios);
                Logger.Log.Info($"✓ Loaded {_areas.Count} areas from Areas.xml");
                
                // Debug: Show first few areas
                Logger.Log.Info("=== AREAS LOADED ===");
                foreach (var area in _areas.Take(3))
                {
                    Logger.Log.Info($"  Area: '{area.Name}', Model: '{area.Model}', Respects: '{area.Respect}'");
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"❌ Failed to load XML data: {ex.Message}");
                Logger.Log.Error($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void InitializePhone()
        {
            _phone = new CustomiFruit();

            // Check what backup types are available for the current player
            bool hasHelicopters = HasAvailableHelicopters();
            bool hasGroundVehicles = HasAvailableGroundVehicles();
            bool hasGuards = HasAvailableGuards();

            Logger.Log.Info($"Backup availability check - Helicopters: {hasHelicopters}, Ground: {hasGroundVehicles}, Guards: {hasGuards}");

            // Only add contacts for available backup types
            if (hasHelicopters)
            {
                // Airstrike Team Contact (requires helicopters)
                _airstrikeContact = new iFruitContact("Airstrike Team");
                _airstrikeContact.Answered += OnAirstrikeCall;
                _airstrikeContact.DialTimeout = 5000;
                _airstrikeContact.Active = true;
                _airstrikeContact.Icon = ContactIcon.Lester;
                _phone.Contacts.Add(_airstrikeContact);
                Logger.Log.Info("✓ Airstrike Team contact added");

                // Aerial Backup Contact (requires helicopters)
                _aerialBackupContact = new iFruitContact("Aerial Backup");
                _aerialBackupContact.Answered += OnAerialBackupCall;
                _aerialBackupContact.DialTimeout = 5000;
                _aerialBackupContact.Active = true;
                _aerialBackupContact.Icon = ContactIcon.Lester;
                _phone.Contacts.Add(_aerialBackupContact);
                Logger.Log.Info("✓ Aerial Backup contact added");
            }
            else
            {
                Logger.Log.Info("✗ Helicopter backups unavailable - no helicopters in Guards.xml for respected areas");
            }

            if (hasGroundVehicles && hasGuards)
            {
                // Ground Unit Contact (requires both vehicles and guards)
                _groundBackupContact = new iFruitContact("Guard Ground Unit");
                _groundBackupContact.Answered += OnGroundBackupCall;
                _groundBackupContact.DialTimeout = 5000;
                _groundBackupContact.Active = true;
                _groundBackupContact.Icon = ContactIcon.Lester;
                _phone.Contacts.Add(_groundBackupContact);
                Logger.Log.Info("✓ Guard Ground Unit contact added");
            }
            else
            {
                Logger.Log.Info($"✗ Ground backup unavailable - Vehicles: {hasGroundVehicles}, Guards: {hasGuards}");
            }

            int contactCount = _phone.Contacts.Count;
            if (contactCount == 0)
            {
                Logger.Log.Warning("No backup contacts available! Check Areas.xml and Guards.xml configuration.");
              //  HelperClass.Notification("~y~No backup available - check your guard configuration");
            }
            else
            {
                Logger.Log.Info($"Phone initialized with {contactCount} backup contact(s)");
            }
        }

        /// <summary>
        /// Check if player has access to helicopters from any respected area
        /// </summary>
        private bool HasAvailableHelicopters()
        {
            var suitableAreas = GetAreasRespectingPlayer();
            if (!suitableAreas.Any())
                return false;

            foreach (var area in suitableAreas)
            {
                // Check if area allows backup
                if (!area.AllowsBackup)
                    continue;

                // Get guard config for this area
                if (_guardConfigs.TryGetValue(area.Model, out GuardConfig config))
                {
                    // Check if this guard config has helicopters
                    if (config.HVehicleModels != null && config.HVehicleModels.Any(h => !string.IsNullOrEmpty(h)))
                    {
                        Logger.Log.Info($"✓ Helicopters available from area: {area.Name} ({config.HVehicleModels.Count} models)");
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if player has access to ground vehicles from any respected area
        /// </summary>
        private bool HasAvailableGroundVehicles()
        {
            var suitableAreas = GetAreasRespectingPlayer();
            if (!suitableAreas.Any())
                return false;

            foreach (var area in suitableAreas)
            {
                // Check if area allows backup
                if (!area.AllowsBackup)
                    continue;

                // Get guard config for this area
                if (_guardConfigs.TryGetValue(area.Model, out GuardConfig config))
                {
                    // Check if this guard config has ground vehicles
                    if (config.VehicleModels != null && config.VehicleModels.Any(v => !string.IsNullOrEmpty(v)))
                    {
                        Logger.Log.Info($"✓ Ground vehicles available from area: {area.Name} ({config.VehicleModels.Count} models)");
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if player has access to guards from any respected area
        /// </summary>
        private bool HasAvailableGuards()
        {
            var suitableAreas = GetAreasRespectingPlayer();
            if (!suitableAreas.Any())
                return false;

            foreach (var area in suitableAreas)
            {
                // Check if area allows backup
                if (!area.AllowsBackup)
                    continue;

                // Get guard config for this area
                if (_guardConfigs.TryGetValue(area.Model, out GuardConfig config))
                {
                    // Check if this guard config has ped models
                    if (config.PedModels != null && config.PedModels.Any(p => !string.IsNullOrEmpty(p)))
                    {
                        Logger.Log.Info($"✓ Guards available from area: {area.Name} ({config.PedModels.Count} models)");
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get all areas that respect the current player and allow backup
        /// </summary>
        private List<Area> GetAreasRespectingPlayer()
        {
            string playerName = GetPlayerCharacterName();
            if (string.IsNullOrEmpty(playerName))
                return new List<Area>();

            var suitableAreas = new List<Area>();

            // First, find areas that directly respect the player
            var directRespectAreas = _areas.Where(area =>
                !string.IsNullOrEmpty(area.Respect) &&
                area.Respect.ToUpper().Contains(playerName.ToUpper()) &&
                area.AllowsBackup
            ).ToList();

            // Add all directly respecting areas
            suitableAreas.AddRange(directRespectAreas);

            // Now check for CROSS-RESPECT: If the player has areas that respect them,
            // find OTHER areas that also respect those same characters
            // Example: Player is Franklin -> MichaelHouse respects Franklin
            // MichaelHouse also respects Michael -> FranklinHouse respects Michael
            // Therefore Franklin can call backup from FranklinHouse too!
            
            if (directRespectAreas.Any())
            {
                // Get all characters that ANY of the player's areas respect
                var allRespectedCharacters = directRespectAreas
                    .SelectMany(area => area.Respect.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(c => c.Trim().ToUpper())
                    .Distinct()
                    .ToList();

                Logger.Log.Info($"Player {playerName} has direct respect from {directRespectAreas.Count} areas. Characters in network: {string.Join(", ", allRespectedCharacters)}");

                // Find areas that respect ANY of these characters (cross-respect)
                var crossRespectAreas = _areas.Where(area =>
                    !string.IsNullOrEmpty(area.Respect) &&
                    area.AllowsBackup &&
                    !suitableAreas.Contains(area) && // Don't add duplicates
                    allRespectedCharacters.Any(character => area.Respect.ToUpper().Contains(character))
                ).ToList();

                if (crossRespectAreas.Any())
                {
                    Logger.Log.Info($"Cross-respect enabled: {crossRespectAreas.Count} additional areas available via mutual respect");
                    suitableAreas.AddRange(crossRespectAreas);
                }
            }

            return suitableAreas;
        }

        /// <summary>
        /// Get backup fees and cooldown for a specific backup type
        /// Uses the first available area's configuration, or defaults if none found
        /// </summary>
        private (int cost, int cooldown) GetBackupFees(string backupType)
        {
            var suitableAreas = GetAreasRespectingPlayer();
            
            if (suitableAreas.Any())
            {
                // Use the first suitable area's backup fees
                var area = suitableAreas.First();
                
                switch (backupType.ToLower())
                {
                    case "airstrike":
                        Logger.Log.Info($"Using airstrike fees from area '{area.Name}': ${area.BackupFees.AirstrikeCost}, cooldown {area.BackupFees.AirstrikeCooldown}s");
                        return (area.BackupFees.AirstrikeCost, area.BackupFees.AirstrikeCooldown);
                    
                    case "aerial":
                        Logger.Log.Info($"Using aerial fees from area '{area.Name}': ${area.BackupFees.AerialCost}, cooldown {area.BackupFees.AerialCooldown}s");
                        return (area.BackupFees.AerialCost, area.BackupFees.AerialCooldown);
                    
                    case "ground":
                        Logger.Log.Info($"Using ground fees from area '{area.Name}': ${area.BackupFees.GroundCost}, cooldown {area.BackupFees.GroundCooldown}s");
                        return (area.BackupFees.GroundCost, area.BackupFees.GroundCooldown);
                }
            }
            
            // Fallback to defaults
            Logger.Log.Warning($"No suitable area found for backup fees, using defaults for {backupType}");
            return backupType.ToLower() switch
            {
                "airstrike" => (DEFAULT_AIRSTRIKE_FEE, DEFAULT_COOLDOWN_AIRSTRIKE),
                "aerial" => (DEFAULT_AERIAL_BACKUP_FEE, DEFAULT_COOLDOWN_AERIAL),
                "ground" => (DEFAULT_GROUND_BACKUP_FEE, DEFAULT_COOLDOWN_GROUND),
                _ => (10000, 30)
            };
        }
    


    private void OnAirstrikeCall(iFruitContact contact)
    {
        // Get dynamic fees from Areas.xml
        var (cost, cooldown) = GetBackupFees("airstrike");
        
        // Check cooldown
        if ((DateTime.Now - _lastAirstrikeCall).TotalSeconds < cooldown)
        {
            int remaining = cooldown - (int)(DateTime.Now - _lastAirstrikeCall).TotalSeconds;
            HelperClass.Notification($"~r~Airstrike unavailable. Wait {remaining}s");
            return;
        }

        // Check money
        if (Game.Player.Money < cost)
        {
            HelperClass.Notification($"~r~Insufficient funds. Need ${cost:N0}");
            return;
        }

        // Charge dispatch fee
        Game.Player.Money -= cost;
        HelperClass.Notification($"~g~Airstrike Team dispatched! ${cost:N0} charged");

        _lastAirstrikeCall = DateTime.Now;

        SpawnAirstrikeBackup();
        //HelperClass.Subtitle($"~G~Strike Team ~W~in route.");
        _phone.Close(0);
    }
        
        private void OnAerialBackupCall(iFruitContact contact)
        {
            // Get dynamic fees from Areas.xml
            var (cost, cooldown) = GetBackupFees("aerial");
            
            // Check cooldown
            if ((DateTime.Now - _lastAerialCall).TotalSeconds < cooldown)
            {
                int remaining = cooldown - (int)(DateTime.Now - _lastAerialCall).TotalSeconds;
                HelperClass.Notification($"~r~Aerial Backup unavailable. Wait {remaining}s");
                return;
            }
            
            // Check money
            if (Game.Player.Money < cost)
            {
                HelperClass.Notification($"~r~Insufficient funds. Need ${cost:N0}");
                return;
            }
            
            // Charge dispatch fee
            Game.Player.Money -= cost;
            HelperClass.Notification($"~g~Aerial Backup dispatched! ${cost:N0} charged");
            
            _lastAerialCall = DateTime.Now;
            
            SpawnAerialBackup();
        //HelperClass.Subtitle($"~G~Aerial Team ~W~in route.");
        _phone.Close(0);
    }
        
        private void OnGroundBackupCall(iFruitContact contact)
        {
            // Get dynamic fees from Areas.xml
            var (cost, cooldown) = GetBackupFees("ground");
            
            // Check cooldown
            if ((DateTime.Now - _lastGroundCall).TotalSeconds < cooldown)
            {
                int remaining = cooldown - (int)(DateTime.Now - _lastGroundCall).TotalSeconds;
                HelperClass.Notification($"~r~Ground Unit unavailable. Wait {remaining}s");
                return;
            }
            
            // Check money
            if (Game.Player.Money < cost)
            {
                HelperClass.Notification($"~r~Insufficient funds. Need ${cost:N0}");
                return;
            }
            
            // Charge dispatch fee
            Game.Player.Money -= cost;
            HelperClass.Notification($"~g~Ground Unit dispatched! ${cost:N0} charged");
            
            _lastGroundCall = DateTime.Now;
            
            SpawnGroundBackup();
        //HelperClass.Subtitle($"~G~Backup Team ~W~in route.");
        _phone.Close(0);
    }
        
        
         
        /// <summary>
        /// Get a random guard configuration and matching area that respects the current player character
        /// </summary>
        /// <returns>Tuple of (GuardConfig, Area) or (null, null) if no suitable guards found</returns>
        private (GuardConfig config, Area area) GetRandomGuardAndAreaForPlayer()
        {
            // Get current player character name
            string playerName = GetPlayerCharacterName();
            if (string.IsNullOrEmpty(playerName))
            {
                Logger.Log.Error("Could not determine player character - check player model hash");
               // HelperClass.Notification("~r~ERROR: Could not identify player");
                return (null, null);
            }
            
            Logger.Log.Info($"=== GUARD SELECTION DEBUG ===");
            Logger.Log.Info($"Player character: {playerName}");
            Logger.Log.Info($"Total areas loaded: {_areas.Count}");
            Logger.Log.Info($"Total guard configs loaded: {_guardConfigs.Count}");
            
            // Find all areas that respect this player AND allow backup
            var suitableAreas = _areas.Where(area => 
                !string.IsNullOrEmpty(area.Respect) && 
                area.Respect.ToUpper().Contains(playerName.ToUpper()) &&
                area.AllowsBackup // CRITICAL: Check if backup is allowed for this area
            ).ToList();
            
            Logger.Log.Info($"Found {suitableAreas.Count} areas that respect {playerName} and allow backup");
            
            if (suitableAreas.Count == 0)
            {
                Logger.Log.Error($"No areas found that respect player: {playerName} with backup enabled");
                Logger.Log.Info("Listing first 5 areas for debugging:");
                foreach (var area in _areas.Take(5))
                {
                    Logger.Log.Info($"  Area: {area.Name}, Respect: '{area.Respect ?? "NULL"}', Model: '{area.Model}', AllowsBackup: {area.AllowsBackup}");
                }
                //HelperClass.Notification($"~r~No backup available for {playerName}");
                //HelperClass.Notification("~y~Check Areas.xml 'allowsBackup' attribute");
                return (null, null);
            }
            
            // Randomly select one area
            Area selectedArea = suitableAreas[_random.Next(suitableAreas.Count)];
            string guardModelName = selectedArea.Model;
            
            if (string.IsNullOrEmpty(guardModelName))
            {
                Logger.Log.Error($"Area '{selectedArea.Name}' has no guard model specified");
                //HelperClass.Notification($"~r~No guard model in area {selectedArea.Name}");
                return (null, null);
            }
            
            Logger.Log.Info($"Selected area: '{selectedArea.Name}', guard model: '{guardModelName}'");
            
            // Get the guard configuration
            if (_guardConfigs.TryGetValue(guardModelName, out GuardConfig guardConfig))
            {
                Logger.Log.Info($"✓ Selected guard: '{guardModelName}' from area: '{selectedArea.Name}'");
                Logger.Log.Info($"  Peds: {guardConfig.PedModels.Count}, Weapons: {guardConfig.Weapons.Count}");
                Logger.Log.Info($"  Vehicles: {guardConfig.VehicleModels.Count}, Helis: {guardConfig.HVehicleModels.Count}");
                Logger.Log.Info($"  Relationship Group: '{guardConfig.RelationshipGroup}'");
                return (guardConfig, selectedArea);
            }
            
            Logger.Log.Error($"Guard config not found in Guards.xml: {guardModelName}");
            Logger.Log.Info($"Available configs: {string.Join(", ", _guardConfigs.Keys)}");
            //HelperClass.Notification($"~r~Guard '{guardModelName}' not in Guards.xml");
            return (null, null);
        }
        
        /// <summary>
        /// Get a random guard configuration that respects the current player character
        /// Legacy method - prefer using GetRandomGuardAndAreaForPlayer() for relationship setup
        /// </summary>
        /// <returns>GuardConfig or null if no suitable guards found</returns>
        private GuardConfig GetRandomGuardForPlayer()
        {
            var (config, area) = GetRandomGuardAndAreaForPlayer();
            return config;
        }
        
        /// <summary>
        /// Get the current player character name (MICHAEL, FRANKLIN, or TREVOR)
        /// </summary>
        /// <returns>Player character name or empty string</returns>
        private string GetPlayerCharacterName()
        {
            Ped player = Game.Player.Character;
            if (player == null)
            {
                Logger.Log.Error("Player character is null!");
                return string.Empty;
            }
            
            // Use the same approach as GuardPed.cs - direct Model comparison with PedHash
            Model playerModel = player.Model;
            
            Logger.Log.Info($"=== PLAYER DETECTION DEBUG ===");
            Logger.Log.Info($"Player Model: {playerModel}");
            Logger.Log.Info($"Player Model.Hash: {playerModel.Hash}");
            Logger.Log.Info($"Michael Hash: {PedHash.Michael}");
            Logger.Log.Info($"Franklin Hash: {PedHash.Franklin}");
            Logger.Log.Info($"Trevor Hash: {PedHash.Trevor}");
            
            // Check using same method as GuardPed.cs
            if (playerModel == PedHash.Michael)
            {
                Logger.Log.Info("✓✓✓ DETECTED: MICHAEL");
                return "MICHAEL";
            }
            
            if (playerModel == PedHash.Franklin)
            {
                Logger.Log.Info("✓✓✓ DETECTED: FRANKLIN");
                return "FRANKLIN";
            }
            
            if (playerModel == PedHash.Trevor)
            {
                Logger.Log.Info("✓✓✓ DETECTED: TREVOR");
                return "TREVOR";
            }

            Logger.Log.Error($"❌ Unknown player model: {playerModel} (hash: {playerModel.Hash})");
            Logger.Log.Error($"Expected Michael={PedHash.Michael}, Franklin={PedHash.Franklin}, Trevor={PedHash.Trevor}");
            return string.Empty;
        }
        
        /// <summary>
        /// Setup guard relationships and combat attributes using Guards.xml configuration
        /// Uses same relationship logic as regular area guards for realistic shared behavior
        /// ENHANCED: Also sets up relationships with ALL allied area guard groups
        /// </summary>
        private void SetupGuardRelationships(Ped guard, GuardConfig config, Area area)
        {
            if (guard == null || !guard.Exists()) return;
            if (config == null)
            {
                Logger.Log.Error("GuardConfig is null in SetupGuardRelationships");
                return;
            }
            
            try
            {
                
                // Use relationship group from Guards.xml (NOT hardcoded)
                RelationshipGroup guardGroup = World.AddRelationshipGroup(config.RelationshipGroup);
                guard.RelationshipGroup = guardGroup;
                guardGroup.SetRelationshipBetweenGroups(guardGroup, Relationship.Companion, true);
                
                Logger.Log.Info($"Backup guard using relationship group: {config.RelationshipGroup}");
                
                // Apply respect rules from Areas.xml (same logic as GuardPed.cs)
                if (area != null)
                {
                    RelationshipGroup playerGroup = Game.Player.Character.RelationshipGroup;
                    
                    if (area.Respect == "YES" || area.Respect == "ANY" || area.Respect == "ALL")
                    {
                        // All players are respected
                        playerGroup.SetRelationshipBetweenGroups(guardGroup, Relationship.Companion);
                        guardGroup.SetRelationshipBetweenGroups(playerGroup, Relationship.Companion);
                        Logger.Log.Info($"Backup guard respects ALL players (area: {area.Name})");
                    }
                    else if ((area.Respect == "TREVOR" && Game.Player.Character.Model == PedHash.Trevor) ||
                             (area.Respect == "MICHAEL" && Game.Player.Character.Model == PedHash.Michael) ||
                             (area.Respect == "FRANKLIN" && Game.Player.Character.Model == PedHash.Franklin))
                    {
                        // Single character match
                        playerGroup.SetRelationshipBetweenGroups(guardGroup, Relationship.Companion);
                        guardGroup.SetRelationshipBetweenGroups(playerGroup, Relationship.Companion);
                        Logger.Log.Info($"Backup guard respects current player (area: {area.Name}, respect: {area.Respect})");
                    }
                    else if (!string.IsNullOrEmpty(area.Respect))
                    {
                        // Multiple characters check (comma-separated)
                        string[] respectedCharacters = area.Respect.Split(',');
                        bool respectedCharacter = false;
                        
                        foreach (string characterName in respectedCharacters)
                        {
                            string trimmedName = characterName.Trim().ToUpperInvariant();
                            if ((trimmedName == "TREVOR" && Game.Player.Character.Model == PedHash.Trevor) ||
                                (trimmedName == "MICHAEL" && Game.Player.Character.Model == PedHash.Michael) ||
                                (trimmedName == "FRANKLIN" && Game.Player.Character.Model == PedHash.Franklin))
                            {
                                respectedCharacter = true;
                                break;
                            }
                        }
                        
                        if (respectedCharacter)
                        {
                            playerGroup.SetRelationshipBetweenGroups(guardGroup, Relationship.Companion);
                            guardGroup.SetRelationshipBetweenGroups(playerGroup, Relationship.Companion);
                            Logger.Log.Info($"Backup guard respects current player from list (area: {area.Name}, respect: {area.Respect})");
                        }
                        else
                        {
                            Logger.Log.Warning($"Backup guard does NOT respect current player (area: {area.Name}, respect: {area.Respect})");
                        }
                    }
                    
                    // CRITICAL: Setup relationships with ALL allied area guards
                    // This ensures backup guards don't fight with area guards or other backup guards
                    SetupBackupToAreaGuardRelationships(guardGroup, config.RelationshipGroup, area);
                }

            // Set combat attributes
            // Maximum combat ability
            guard.CombatAbility = CombatAbility.Professional;
            guard.CombatMovement = CombatMovement.WillAdvance;  // Offensive movement
            guard.CombatRange = CombatRange.Far;     // Far range
            guard.Accuracy = 95;         // 75% accuracy
            guard.FiringPattern = FiringPattern.FullAuto; // Full auto
                
                // Combat flags
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, guard, 5, true);   // AlwaysFight
                //Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, guard, 46, true);  // UseVehicleAttack
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, guard, 52, true);  // UseVehicleWeapon
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, guard, 3, false);  // Don't flee
                
                // Make them mission entities (persistent - do NOT mark as no longer needed!)
               // guard.IsPersistent = true;
                
                Logger.Log.Info($"✓ Setup relationships for backup guard {guard.Handle} using config: {config.Name}");
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"Failed to setup guard relationships: {ex.Message}");
                Logger.Log.Error($"Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Setup relationships between backup guards and all allied area guard groups
        /// This ensures backup guards work together with area guards and other backup guards
        /// Uses the same cross-respect network logic as GuardSpawner
        /// </summary>
        private void SetupBackupToAreaGuardRelationships(RelationshipGroup backupGuardGroup, string backupRelationshipGroupName, Area backupArea)
        {
            try
            {
                if (string.IsNullOrEmpty(backupArea.Respect))
                {
                    Logger.Log.Info("Backup area has no respect attribute, skipping cross-guard relationships");
                    return;
                }
                
                // Get characters this backup area respects
                var backupRespectedCharacters = backupArea.Respect.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim().ToUpper())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();
                
                if (!backupRespectedCharacters.Any())
                {
                    Logger.Log.Info("No valid respected characters for backup area");
                    return;
                }
                
                Logger.Log.Info($"Backup area '{backupArea.Name}' respects: {string.Join(", ", backupRespectedCharacters)}");
                
                int alliedCount = 0;
                var backupGroupHash = StringHash.AtStringHash(backupRelationshipGroupName);
                
                // Find all areas that share at least one respected character with the backup area
                foreach (var area in _areas)
                {
                    // Skip self
                    if (area.Name == backupArea.Name)
                        continue;
                    
                    if (string.IsNullOrEmpty(area.Respect))
                        continue;
                    
                    // Get characters this area respects
                    var areaRespectedCharacters = area.Respect.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim().ToUpper())
                        .Where(c => !string.IsNullOrEmpty(c))
                        .ToList();
                    
                    // Check if there's any common character
                    var commonCharacters = backupRespectedCharacters.Intersect(areaRespectedCharacters).ToList();
                    
                    if (commonCharacters.Any())
                    {
                        // They share at least one respected character - make them allies
                        if (_guardConfigs.TryGetValue(area.Model, out GuardConfig areaGuardConfig))
                        {
                            var areaGroupHash = StringHash.AtStringHash(areaGuardConfig.RelationshipGroup);
                            
                            // Set relationship to 0 (Companion - highest respect level)
                            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, backupGroupHash, areaGroupHash);
                            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, areaGroupHash, backupGroupHash);
                            
                            alliedCount++;
                            Logger.Log.Info($"✓ Backup allied with: '{area.Name}' ({areaGuardConfig.RelationshipGroup}) via {string.Join(", ", commonCharacters)}");
                        }
                        else
                        {
                            Logger.Log.Warning($"No guard config found for area '{area.Name}' model '{area.Model}'");
                        }
                    }
                }
                
                if (alliedCount > 0)
                {
                    Logger.Log.Info($"✓ Backup guard group '{backupRelationshipGroupName}' allied with {alliedCount} area guard groups");
                }
                else
                {
                    Logger.Log.Info($"No allied area guard groups found for backup guard group '{backupRelationshipGroupName}'");
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"Error in SetupBackupToAreaGuardRelationships: {ex.Message}");
                Logger.Log.Error($"Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Check if a specific area respects the current player character
        /// </summary>
        private bool DoesAreaRespectPlayer(Area area)
        {
            if (area == null || string.IsNullOrEmpty(area.Respect))
                return false;

            string respect = area.Respect.ToUpperInvariant();
            
            // Check for universal respect
            if (respect == "YES" || respect == "ANY" || respect == "ALL")
                return true;

            // Get current player character
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return false;

            // Check which character the player is using
            if (player.Model == PedHash.Michael && respect.Contains("MICHAEL"))
                return true;
            if (player.Model == PedHash.Franklin && respect.Contains("FRANKLIN"))
                return true;
            if (player.Model == PedHash.Trevor && respect.Contains("TREVOR"))
                return true;

            return false;
        }
        
        /// <summary>
        /// Get a random ped model from guard config
        /// </summary>
        private Model GetRandomPedModel(GuardConfig config)
        {
            if (config.PedModels.Count == 0)
            {
                Logger.Log.Error("No ped models in guard config");
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
            
            // Try to parse weapon hash
            if (Enum.TryParse(weaponName, true, out WeaponHash weaponHash))
            {
                return weaponHash;
            }
            
            return WeaponHash.CarbineRifle;
        }
        
        /// <summary>
        /// Get a random helicopter model from guard config
        /// Filters for rappel-capable helicopters if specified
        /// </summary>
        private VehicleHash GetRandomHelicopter(GuardConfig config, bool rappelCapable = false)
        {
            List<string> heliModels = config.HVehicleModels;
            
            if (heliModels.Count == 0)
            {
                Logger.Log.Error("No helicopter models in guard config, using default");
                return VehicleHash.Buzzard;
            }
            
            // Filter to only actual helicopters
            List<string> validHelis = new List<string>();
            foreach (string modelName in heliModels)
            {
                if (Enum.TryParse(modelName, true, out VehicleHash hash))
                {
                    Model model = new Model(hash);
                    if (model.IsHelicopter)
                    {
                        validHelis.Add(modelName);
                    }
                }
            }
            
            if (validHelis.Count == 0)
            {
                Logger.Log.Warning("No valid helicopter models found, using default");
                return VehicleHash.Annihilator2;
            }
            
            // Rappel-capable helicopters (explicit list)
            List<string> rappelHelis = new List<string> 
            { 
                "ANNIHILATOR", "ANNIHILATOR2", "POLMAV", "MAVERICK" 
            };
            
            if (rappelCapable)
            {
                var filteredHelis = validHelis
                    .Where(h => rappelHelis.Contains(h.ToUpper()))
                    .ToList();
                
                if (filteredHelis.Count > 0)
                {
                    validHelis = filteredHelis;
                    Logger.Log.Info($"Using rappel-capable helicopter from {filteredHelis.Count} options");
                }
                else
                {
                    Logger.Log.Warning("No rappel-capable helicopters available, using any helicopter");
                }
            }
            
            string heliName = validHelis[_random.Next(validHelis.Count)];
            
            // Try to parse vehicle hash
            if (Enum.TryParse(heliName, true, out VehicleHash vehicleHash))
            {
                Logger.Log.Info($"Selected helicopter: {heliName}");
                return vehicleHash;
            }
            
            return VehicleHash.Maverick;
        }
        
        /// <summary>
        /// Get a random ground vehicle from guard config
        /// Filters for cars and bikes only (no boats, helicopters, etc)
        /// </summary>
        private VehicleHash GetRandomGroundVehicle(GuardConfig config)
        {
            if (config.VehicleModels.Count == 0)
            {
                Logger.Log.Error("No vehicle models in guard config, using default");
                return VehicleHash.Granger;
            }
            
            // Filter to only cars and bikes
            List<string> validVehicles = new List<string>();
            foreach (string modelName in config.VehicleModels)
            {
                if (Enum.TryParse(modelName, true, out VehicleHash hash))
                {
                    Model model = new Model(hash);
                    if (model.IsCar || model.IsBike)
                    {
                        validVehicles.Add(modelName);
                    }
                }
            }
            
            if (validVehicles.Count == 0)
            {
                Logger.Log.Warning("No valid ground vehicles (cars/bikes) found, using default");
                return VehicleHash.Granger;
            }
            
            string vehicleName = validVehicles[_random.Next(validVehicles.Count)];
            
            // Try to parse vehicle hash
            if (Enum.TryParse(vehicleName, true, out VehicleHash vehicleHash))
            {
                Logger.Log.Info($"Selected ground vehicle: {vehicleName}");
                return vehicleHash;
            }
            
            return VehicleHash.Granger;
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

    private void SpawnAirstrikeBackup()
        {
            Logger.Log.Info("=== AIRSTRIKE BACKUP SPAWNING - START ===");
            
            try
            {
                Logger.Log.Info("STEP 1: Getting guard configuration...");
                
                // Get appropriate guard configuration and area based on player
                var (guardConfig, selectedArea) = GetRandomGuardAndAreaForPlayer();
                if (guardConfig == null || selectedArea == null)
                {
                    Logger.Log.Error("FAILED: No guard config or area found");
                    HelperClass.Notification("~r~No backup available for your character");
                    return;
                }
                Logger.Log.Info($"STEP 1: SUCCESS - Guard: {guardConfig.Name}, Area: {selectedArea.Name}");
                
                Logger.Log.Info("STEP 2: Getting player reference...");
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    Logger.Log.Error("STEP 2: FAILED - Player is null or doesn't exist");
                    return;
                }
                Vector3 playerPos = player.Position;
                Logger.Log.Info($"STEP 2: SUCCESS - Player at {playerPos}");
                
                Logger.Log.Info("STEP 3: Getting helicopter model...");
                // Get helicopter model
                VehicleHash heliHash = GetRandomHelicopter(guardConfig, rappelCapable: false);
                Model heliModel = new Model(heliHash);
                
                Logger.Log.Info($"STEP 3: Requesting helicopter model: {heliHash}");
                heliModel.Request(2000);
                
                if (!heliModel.IsLoaded)
                {
                    Logger.Log.Error($"STEP 3: FAILED - Failed to load helicopter model: {heliHash}");
                    //HelperClass.Notification("~r~Failed to spawn helicopter");
                    return;
                }
                Logger.Log.Info("STEP 3: SUCCESS - Model loaded");
                
                // Use proper spawn point finder for aircraft
                Logger.Log.Info("STEP 4: Finding spawn point for attack helicopter...");
                if (!HelperClass.FindSpawnPointForAircraft(player, playerPos, 150f, 300f, 80f, out Vector3 spawnPos, out float spawnHeading))
                {
                    Logger.Log.Error("STEP 4: FAILED - Failed to find valid spawn point for attack helicopter");
                    HelperClass.Notification("~r~Failed to find spawn location");
                    heliModel.MarkAsNoLongerNeeded();
                    return;
                }
                Logger.Log.Info($"STEP 4: SUCCESS - Spawn point: {spawnPos}, heading {spawnHeading}");
                
                Logger.Log.Info("STEP 5: Creating helicopter vehicle...");
                Vehicle helicopter = World.CreateVehicle(heliModel, spawnPos, spawnHeading);
                
                if (helicopter == null || !helicopter.Exists())
                {
                    Logger.Log.Error("STEP 5: FAILED - Failed to create helicopter vehicle");
                    //HelperClass.Notification("~r~Failed to spawn helicopter");
                    heliModel.MarkAsNoLongerNeeded();
                    return;
                }
                
                Logger.Log.Info($"STEP 5: SUCCESS - Helicopter created: Handle={helicopter.Handle}");
                
                Logger.Log.Info("STEP 6: Configuring helicopter properties...");
                // Configure helicopter
                helicopter.PopulationType = EntityPopulationType.Mission;
                helicopter.IsEngineRunning = true;
               // helicopter.IsPersistent = true;
                Logger.Log.Info("STEP 6: SUCCESS");
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
            heliModel.MarkAsNoLongerNeeded();
                Logger.Log.Info("STEP 7: Model marked as no longer needed");
                
                Logger.Log.Info("STEP 8: Creating squad tracking object...");
                // Create squad tracking object
                var squad = new BackupSquad
                {
                    SquadType = BackupType.Airstrike,
                    Vehicle = helicopter,
                    SpawnTime = DateTime.Now,
                    IsActive = true,
                    Guards = new List<BackupGuard>(),
                    InitialVehicleHealth = helicopter.HeliEngineHealth
                };
                Logger.Log.Info("STEP 8: SUCCESS - Squad tracking object created");
                
                Logger.Log.Info("STEP 9: Adding vehicle blip...");
                // Add vehicle blip (only if blips enabled and area respects player)
                try
                {
                    if (PlayerPositionLogger.GetEnableBlips() && DoesAreaRespectPlayer(selectedArea))
                    {
                        Blip heliBlip = helicopter.AddBlip();
                        if (heliBlip != null && heliBlip.Exists())
                        {
                            heliBlip.Sprite = BlipSprite.HelicopterAnimated;
                            heliBlip.Color = GetBlipColorForArea(selectedArea);
                            heliBlip.Name = "Attack Helicopter";
                            heliBlip.IsShortRange = false;
                            Logger.Log.Info("STEP 9: SUCCESS - Blip created");
                        }
                        else
                        {
                            Logger.Log.Warning("STEP 9: WARNING - Blip is null");
                        }
                        squad.VehicleBlip = heliBlip;
                    }
                    else
                    {
                        Logger.Log.Info("STEP 9: Blips disabled or area doesn't respect player - no blip created");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"STEP 9: EXCEPTION - {ex.Message}");
                    Logger.Log.Error($"Stack trace: {ex.StackTrace}");
                }
                
                Logger.Log.Info("STEP 10: Getting guard model...");
                // Spawn guards in helicopter seats
                Model guardModel = GetRandomPedModel(guardConfig);
                Logger.Log.Info($"STEP 10: Guard model: {guardModel}, requesting...");
                guardModel.Request(2000);
                
                if (!guardModel.IsLoaded)
                {
                    Logger.Log.Error($"STEP 10: FAILED - Failed to load guard model: {guardModel}");
                    HelperClass.Notification("~r~Failed to spawn guards");
                    
                    // Clean up helicopter - mark occupants and vehicle as no longer needed instead of deleting
                    if (helicopter != null && helicopter.Exists())
                    {
                        try
                        {
                            foreach (var occ in helicopter.Occupants)
                            {
                                if (occ != null && occ.Exists()) occ.MarkAsNoLongerNeeded();
                            }
                            helicopter.MarkAsNoLongerNeeded();
                        }
                        catch (Exception) { try { helicopter.MarkAsNoLongerNeeded(); } catch { } }
                    }
                    guardModel.MarkAsNoLongerNeeded();
                    return;
                }
                Logger.Log.Info("STEP 10: SUCCESS - Guard model loaded");
                

                int guardNumber = 1;
                int guardsSpawned = 0;
                
                Logger.Log.Info("STEP 11: Starting guard spawn loop...");
                for(int seat = -1;seat< helicopter.PassengerCapacity;seat++)
                {
                    try
                    {
                       
                        Logger.Log.Info($"STEP 11.{seat}: Creating guard in seat {seat}...");
                        Ped guard = helicopter.CreatePedOnSeat((VehicleSeat)seat, guardModel);
                        
                        if (guard == null || !guard.Exists())
                        {
                            Logger.Log.Warning($"STEP 11.{seat}: FAILED - Guard not created");
                            continue;
                        }
                        
                        Logger.Log.Info($"STEP 11.{seat}: Guard created, Handle={guard.Handle}");
                        guardsSpawned++;
                        
                        Logger.Log.Info($"STEP 11.{seat}: Setting up guard relationships...");
                        // Setup relationships and combat attributes using Guards.xml config
                        SetupGuardRelationships(guard, guardConfig, selectedArea);
                        
                        Logger.Log.Info($"STEP 11.{seat}: Configuring guard stats...");
                        guard.PopulationType = EntityPopulationType.Mission;
                        guard.MaxHealth = 10000;
                        guard.Health = 10000;
                        guard.Armor = 5000;
                    guard.DiesOnLowHealth = false;
                    var ped = guard;
                    // Ensure guards spawned by the script are in a normal reactive state
                    try { ped.BlockPermanentEvents = false; ped.KeepTaskWhenMarkedAsNoLongerNeeded = false; } catch { }
                    ped.SetCombatAttribute(CombatAttributes.AlwaysFight, true);
                    ped.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
                    ped.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, true);
                    ped.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, true);
                    ped.SetCombatAttribute(CombatAttributes.CanUseCover, true);
                    ped.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
                    ped.SetCombatAttribute(CombatAttributes.AlwaysFlee, false);
                    ped.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, false);
                    ped.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);
                    //ped.SetCombatAttribute(CombatAttributes.DisableBulletReactions, true);
                    ped.SetCombatAttribute(CombatAttributes.CanChaseTargetOnFoot, true);
                    ped.SetCombatAttribute(CombatAttributes.SwitchToDefensiveIfInCover, true);
                    ped.SetCombatAttribute(CombatAttributes.BlindFireWhenInCover, true);
                    ped.SetCombatAttribute(CombatAttributes.CanUsePeekingVariations, true);
                    //ped.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true);
                    ped.SetCombatAttribute(CombatAttributes.CanTauntInVehicle, true);
                    ped.SetCombatAttribute(CombatAttributes.AlwaysEquipBestWeapon, true);

                    // Ensure backup guards will use vehicle-mounted weapons when present
                    try
                    {
                        ped.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true);
                        ped.SetCombatAttribute(CombatAttributes.UseVehicleAttackIfVehicleHasMountedGuns, true);
                        ped.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, false);
                    }
                    catch { }

                    // Config flags - comprehensive set
                    ped.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, true); // CRITICAL: No writhing - instant death
                    ped.SetConfigFlag(PedConfigFlagToggles.CanDiveAwayFromApproachingVehicles, true);
                    ped.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, true);
                    ped.SetConfigFlag(PedConfigFlagToggles.AIDriverAllowFriendlyPassengerSeatEntry, true);
                    ped.SetConfigFlag(PedConfigFlagToggles.KeepRelationshipGroupAfterCleanUp, true);
                    ped.SetConfigFlag(PedConfigFlagToggles.AllowMedicsToReviveMe, true); // No medic revive if they die
                    ped.SetConfigFlag(PedConfigFlagToggles.KeepTasksAfterCleanUp, true);
                    ped.SetConfigFlag(PedConfigFlagToggles.TargetWhenInjuredAllowed, false);
                    ped.SetConfigFlag(PedConfigFlagToggles.CanAttackFriendly, false);
                    ped.SetConfigFlag(PedConfigFlagToggles.DisableBlindFiringInShotReactions, false);
                    ped.SetConfigFlag(PedConfigFlagToggles.AvoidTearGas, true);
                    ped.SetConfigFlag(PedConfigFlagToggles.AllowMissionPedToUseInjuredMovement, true);
                    //ped.SetConfigFlag(fire)

                    Logger.Log.Info($"STEP 11.{seat}: Giving weapon...");
                        // Give weapon
                        WeaponHash weapon = GetRandomWeapon(guardConfig);
                        guard.Weapons.Give(weapon, 9999, true, true);
                        int currentAmmo = guard.Weapons.Current?.Ammo ?? 0;

                    guard.Weapons.Give(WeaponHash.MicroSMG, 9999, false, true);
                    guard.Weapons.Give(WeaponHash.APPistol, 9999, false, true);
                    guard.Weapons.Give(WeaponHash.Knife, 1, false, true);
                    guard.Weapons.Give(WeaponHash.Bat, 1, false, true);
                            guardNumber++;


                    if (PlayerPositionLogger.GetEnableBlips() && DoesAreaRespectPlayer(selectedArea))
                    {
                        Blip gblip = guard.AddBlip();
                        if (guard != null && guard.Exists())
                        {
                            gblip.Sprite = BlipSprite.BigCircleOutline;
                            gblip.Color = GetBlipColorForArea(selectedArea);
                            gblip.Name = "Attack Helicopter Guard";
                            gblip.IsShortRange = false;
                            Logger.Log.Info("STEP 9: SUCCESS - Blip created");
                        }
                        else
                        {
                            Logger.Log.Warning("STEP 9: WARNING - Blip is null");
                        }
                        
                    }
                    else
                    {
                        Logger.Log.Info("STEP 9: Blips disabled or area doesn't respect player - no blip created");
                    }
                    var backupGuard = new BackupGuard
                    {
                        Ped = guard,
                        InitialHealth = 10000,
                        InitialAmmo = currentAmmo,
                        HasWeapon = true
                    };
                        
                        squad.Guards.Add(backupGuard);
                        Logger.Log.Info($"STEP 11.{seat}: SUCCESS - Guard #{guardsSpawned} added to squad");
                    }
                    catch (Exception guardEx)
                    {
                        Logger.Log.Error($"STEP 11.{seat}: EXCEPTION - {guardEx.Message}");
                        Logger.Log.Error($"Stack trace: {guardEx.StackTrace}");
                    }
                }
                
                guardModel.MarkAsNoLongerNeeded();
                Logger.Log.Info("STEP 12: Guard model marked as no longer needed");
                
                squad.InitialGuardCount = squad.Guards.Count;
                Logger.Log.Info($"STEP 13: Total guards spawned: {guardsSpawned}");
                
                if (guardsSpawned == 0)
                {
                    Logger.Log.Error("STEP 13: FAILED - No guards were spawned! Cleaning up helicopter.");
                    if (helicopter != null && helicopter.Exists())
                    {
                        try
                        {
                            foreach (var occ in helicopter.Occupants)
                            {
                                if (occ != null && occ.Exists()) occ.MarkAsNoLongerNeeded();
                            }
                            helicopter.MarkAsNoLongerNeeded();
                        }
                        catch (Exception) { try { helicopter.MarkAsNoLongerNeeded(); } catch { } }
                    }
                    HelperClass.Notification("~r~Failed to spawn any guards");
                    return;
                }
                
                Logger.Log.Info("STEP 14: Creating AttackHelicopter AI controller...");
                
                // Small delay to ensure all guards are fully initialized
                Script.Yield();
                
                // Create AttackHelicopter AI controller
                try
                {
                    var attackHeli = new AttackHelicopter(helicopter, guardConfig, selectedArea);
                    squad.TacticalHelicopter = attackHeli; // Store for updates
                    
                    Logger.Log.Info($"STEP 14: SUCCESS - Attack helicopter controller created");
                    Logger.Log.Info($"✓✓✓ Attack helicopter spawned with {squad.Guards.Count} crew members");
                    HelperClass.Notification($"~g~Attack helicopter inbound with {squad.InitialGuardCount} crew!");
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"STEP 14: EXCEPTION - Failed to create AttackHelicopter controller: {ex.Message}");
                    Logger.Log.Error($"Stack trace: {ex.StackTrace}");
                    // Don't return - helicopter is still functional even without AI
                }
                
                Logger.Log.Info("STEP 15: Adding squad to active squads list...");
                _activeSquads.Add(squad);
                Logger.Log.Info("=== AIRSTRIKE BACKUP SPAWN COMPLETE ===");
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"❌ CRITICAL ERROR in SpawnAirstrikeBackup: {ex.Message}");
                Logger.Log.Error($"Stack trace: {ex.StackTrace}");
                HelperClass.Notification("~r~ERROR: Backup spawn failed");
                HelperClass.Notification("~y~Check log file for details");
            }
        }
        
        private void SpawnAerialBackup()
        {
            Logger.Log.Info("=== AERIAL BACKUP SPAWNING ===");
            
            // Get appropriate guard configuration and area based on player
            var (guardConfig, selectedArea) = GetRandomGuardAndAreaForPlayer();
            if (guardConfig == null || selectedArea == null)
            {
                HelperClass.Notification("~r~No backup available for your character");
                return;
            }
            
            Ped player = Game.Player.Character;
            Vector3 playerPos = player.Position;
            Vector3 deployZone = playerPos;
            
            // Determine deployment mode: rappel or landing
            bool canRappel = false;
            VehicleHash heliHash;
            
            // Try to get rappel-capable helicopter first
            if (guardConfig.HVehicleModels.Count > 0)
            {
                heliHash = GetRandomHelicopter(guardConfig, rappelCapable: true);
                Model testModel = new Model(heliHash);
                testModel.Request(500);
                canRappel = testModel.IsLoaded;
                testModel.MarkAsNoLongerNeeded();
            }
            else
            {
                heliHash = VehicleHash.Buzzard;
            }
            
            // Get helicopter model
            Model heliModel = new Model(heliHash);
            heliModel.Request(1000);
            
            if (!heliModel.IsLoaded)
            {
                Logger.Log.Error("Failed to load helicopter model");
               // HelperClass.Notification("~r~Failed to spawn helicopter");
                return;
            }
            
            // Use proper spawn point finder for aircraft
            if (!HelperClass.FindSpawnPointForAircraft(player, playerPos, 200f, 350f, 100f, out Vector3 spawnPos, out float spawnHeading))
            {
                Logger.Log.Error("Failed to find valid spawn point for tactical helicopter");
                HelperClass.Notification("~r~Failed to find spawn location");
                heliModel.MarkAsNoLongerNeeded();
                return;
            }
            
            Vehicle helicopter = World.CreateVehicle(heliModel, spawnPos, spawnHeading);
            
            if (helicopter == null)
            {
                Logger.Log.Error("Failed to create helicopter vehicle");
               // HelperClass.Notification("~r~Failed to spawn helicopter");
                heliModel.MarkAsNoLongerNeeded();
                return;
            }
            
            helicopter.PopulationType = EntityPopulationType.Mission;
            Function.Call(Hash.SET_VEHICLE_ENGINE_ON, helicopter, true, true, false);
            
            // Check if helicopter actually supports rappel
            canRappel = canRappel && helicopter.AllowRappel;
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
        // For helicopters with less than 4 seats, prefer rappel if possible
        bool shouldRappel = canRappel && (helicopter.PassengerCapacity < 4 || _random.Next(2) == 0);
            
            // Create squad tracking object
            var squad = new BackupSquad
            {
                SquadType = BackupType.AerialBackup,
                Vehicle = helicopter,
                SpawnTime = DateTime.Now,
                IsActive = true,
                Guards = new List<BackupGuard>(),
                InitialVehicleHealth = helicopter.HeliEngineHealth
            };
            
            // Add vehicle blip (only if blips enabled and area respects player) - BackupDebug can override
            if (PlayerPositionLogger.GetEnableBlips() && DoesAreaRespectPlayer(selectedArea))
            {
                Blip heliBlip = helicopter.AddBlip();
                if (heliBlip != null && heliBlip.Exists())
                {
                    heliBlip.Sprite = BlipSprite.HelicopterAnimated;
                    heliBlip.Color = GetBlipColorForArea(selectedArea);
                    heliBlip.Name = shouldRappel ? "Rappel Team" : "Tactical Backup";
                    heliBlip.IsShortRange = false;
                }
                squad.VehicleBlip = heliBlip;
            }
            
            // Spawn guards in helicopter seats
            Model guardModel = GetRandomPedModel(guardConfig);
            guardModel.Request(1000);
            
            // Fill seats (leave driver + co-pilot, fill rear seats)
            int maxSeats = helicopter.PassengerCapacity;
            int guardNumber = 1;
            
           
            
            // Create passengers (these will deploy)
            for (int i = -1; i < maxSeats; i++)
            {
                VehicleSeat seat = (VehicleSeat)i;
                
                Ped guard = helicopter.CreatePedOnSeat(seat, guardModel);
                if (guard == null) continue;
                
                // Setup relationships and combat attributes using Guards.xml config
                SetupGuardRelationships(guard, guardConfig, selectedArea);
                
                guard.PopulationType = EntityPopulationType.Mission;
                guard.MaxHealth = 10000;
                guard.Health = 10000;
                guard.Armor = 5000;
            guard.PopulationType = EntityPopulationType.Mission;
            guard.MaxHealth = 10000;
            guard.Health = 10000;
            guard.Armor = 5000;
            guard.DiesOnLowHealth = false;
            var ped = guard;
            // Ensure guards spawned by the script are in a normal reactive state
            try { ped.BlockPermanentEvents = false; ped.KeepTaskWhenMarkedAsNoLongerNeeded = false; } catch { }
            ped.SetCombatAttribute(CombatAttributes.AlwaysFight, true);
            ped.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
            ped.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, true);
            ped.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, true);
            ped.SetCombatAttribute(CombatAttributes.CanUseCover, true);
            ped.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
            ped.SetCombatAttribute(CombatAttributes.AlwaysFlee, false);
            ped.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, false);
            ped.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);
            //ped.SetCombatAttribute(CombatAttributes.DisableBulletReactions, true);
            ped.SetCombatAttribute(CombatAttributes.CanChaseTargetOnFoot, true);
            ped.SetCombatAttribute(CombatAttributes.SwitchToDefensiveIfInCover, true);
            ped.SetCombatAttribute(CombatAttributes.BlindFireWhenInCover, true);
            ped.SetCombatAttribute(CombatAttributes.CanUsePeekingVariations, true);
            //ped.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true);
            ped.SetCombatAttribute(CombatAttributes.CanTauntInVehicle, true);
            ped.SetCombatAttribute(CombatAttributes.AlwaysEquipBestWeapon, true);

            // Config flags - comprehensive set
            ped.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, true); // CRITICAL: No writhing - instant death
            ped.SetConfigFlag(PedConfigFlagToggles.CanDiveAwayFromApproachingVehicles, true);
            ped.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, true);
            ped.SetConfigFlag(PedConfigFlagToggles.AIDriverAllowFriendlyPassengerSeatEntry, true);
            ped.SetConfigFlag(PedConfigFlagToggles.KeepRelationshipGroupAfterCleanUp, true);
            ped.SetConfigFlag(PedConfigFlagToggles.AllowMedicsToReviveMe, true); // No medic revive if they die
            ped.SetConfigFlag(PedConfigFlagToggles.KeepTasksAfterCleanUp, true);
            ped.SetConfigFlag(PedConfigFlagToggles.TargetWhenInjuredAllowed, false);
            ped.SetConfigFlag(PedConfigFlagToggles.CanAttackFriendly, false);
            ped.SetConfigFlag(PedConfigFlagToggles.DisableBlindFiringInShotReactions, false);
            ped.SetConfigFlag(PedConfigFlagToggles.AvoidTearGas, true);
            ped.SetConfigFlag(PedConfigFlagToggles.AllowMissionPedToUseInjuredMovement, true);
            //ped.SetConfigFlag(fire)
            guard.Weapons.Give(WeaponHash.MicroSMG, 9999, false, true);
            guard.Weapons.Give(WeaponHash.APPistol, 9999, false, true);
            guard.Weapons.Give(WeaponHash.Knife, 1, false, true);
            guard.Weapons.Give(WeaponHash.Bat, 1, false, true);
            // Set combat attributes
            guard.SetCombatAttribute(CombatAttributes.AlwaysFight, true);
                guard.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, true);
               // guard.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, false);
                
                // Give weapon
                WeaponHash weapon = GetRandomWeapon(guardConfig);
                guard.Weapons.Give(weapon, 9999, true, true);
                int currentAmmo = guard.Weapons.Current?.Ammo ?? 30;
                
                // Create guard blip (not for pilot/copilot, only if blips enabled and area respects player)
                Blip guardBlip = null;
                if (seat != VehicleSeat.Driver && seat != (VehicleSeat)0 && PlayerPositionLogger.GetEnableBlips() && DoesAreaRespectPlayer(selectedArea))
                {
                    guardBlip = guard.AddBlip();
                    if (guardBlip != null && guardBlip.Exists())
                    {
                        guardBlip.Sprite = BlipSprite.SecurityContract;
                        guardBlip.Color = GetBlipColorForArea(selectedArea);
                        guardBlip.Name = $"Tactical Guard {guardNumber}";
                        guardBlip.IsShortRange = false;
                        guardBlip.Scale = 0.8f;
                    }
                }
                guardNumber++;
                
                var backupGuard = new BackupGuard
                {
                    Ped = guard,
                    InitialHealth = 10000,
                    InitialAmmo = currentAmmo,
                    HasWeapon = true,
                    Blip = guardBlip
                };
                
                squad.Guards.Add(backupGuard);
            }
            
            squad.InitialGuardCount = squad.Guards.Count;
            
            // Create TacticalHelicopter AI controller
            try
            {
            var tacticalHeli = new TacticalHelicopter(helicopter, deployZone, guardConfig, selectedArea)
            {
                Rappel = shouldRappel,
                Land = !shouldRappel
            };
                
                squad.TacticalHelicopter = tacticalHeli;
             squad.DeploymentMode = shouldRappel ? "Rappel" : "Landing";
            //squad.DeploymentMode = "Landing";
            
                Logger.Log.Info($"Tactical helicopter spawned with {squad.Guards.Count} crew, mode: {squad.DeploymentMode}");
                HelperClass.Notification($"~g~Tactical backup inbound ({squad.DeploymentMode})!");
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"Failed to create TacticalHelicopter controller: {ex.Message}");
            }
            
            _activeSquads.Add(squad);
            
            heliModel.MarkAsNoLongerNeeded();
            guardModel.MarkAsNoLongerNeeded();
        }
        
        private void SpawnGroundBackup()
        {
            Logger.Log.Info("=== GROUND BACKUP SPAWNING ===");
            
            // Get appropriate guard configuration and area based on player
            var (guardConfig, selectedArea) = GetRandomGuardAndAreaForPlayer();
            if (guardConfig == null || selectedArea == null)
            {
                HelperClass.Notification("~r~No backup available for your character");
                return;
            }
            
            Ped player = Game.Player.Character;
            Vector3 playerPos = player.Position;
            
            // Get vehicle model
            VehicleHash vehicleHash = GetRandomGroundVehicle(guardConfig);
            Model vehicleModel = new Model(vehicleHash);
            vehicleModel.Request(1000);
            
            if (!vehicleModel.IsLoaded)
            {
                Logger.Log.Error("Failed to load vehicle model");
                HelperClass.Notification("~r~Failed to spawn vehicle");
                return;
            }
            
            // Use proper spawn point finder for automobile
            if (!HelperClass.FindSpawnPointForAutomobile(player, playerPos, 150, 180f, out Vector3 spawnPos, out float spawnHeading))
            {
                Logger.Log.Error("Failed to find valid spawn point for ground vehicle");
                HelperClass.Notification("~r~Failed to find spawn location");
                vehicleModel.MarkAsNoLongerNeeded();
                return;
            }
            
            Vehicle vehicle = World.CreateVehicle(vehicleModel, spawnPos, spawnHeading);
            if (vehicle == null)
            {
                Logger.Log.Error("Failed to spawn ground backup vehicle");
                HelperClass.Notification("~r~Failed to spawn vehicle");
                vehicleModel.MarkAsNoLongerNeeded();
                return;
            }
            
            vehicle.PopulationType = EntityPopulationType.Mission;
            Function.Call(Hash.SET_VEHICLE_ENGINE_ON, vehicle, true, true, false);
            
            // Create vehicle blip (only if blips enabled and area respects player) - BackupDebug can override
            Blip vehicleBlip = null;
            if (PlayerPositionLogger.GetEnableBlips() && DoesAreaRespectPlayer(selectedArea))
            {
                vehicleBlip = vehicle.AddBlip();
                if (vehicleBlip != null && vehicleBlip.Exists())
                {
                    vehicleBlip.Sprite = BlipSprite.TestCar;
                    vehicleBlip.Color = GetBlipColorForArea(selectedArea);
                    vehicleBlip.Name = "Guard Vehicle";
                    vehicleBlip.IsShortRange = false;
                }
            }
            
            // Create squad
            var squad = new BackupSquad
            {
                SquadType = BackupType.GroundVehicle,
                Vehicle = vehicle,
                VehicleBlip = vehicleBlip,
                Guards = new List<BackupGuard>(),
                SpawnTime = DateTime.Now,
                IsActive = true,
                InitialVehicleHealth = vehicle.HealthFloat
            };
            
            // Spawn guards in vehicle
            Model guardModel = GetRandomPedModel(guardConfig);
            guardModel.Request(1000);
            
            // Fill all available seats
            
            int guardNumber = 1;
            
            for(int i=-1;i<vehicle.PassengerCapacity;i++)
            {
            var seat = (VehicleSeat)i;
                
                Ped guard = vehicle.CreatePedOnSeat(seat, guardModel);
                if (guard == null) continue;
                
                // Setup relationships and combat attributes using Guards.xml config
                SetupGuardRelationships(guard, guardConfig, selectedArea);
                
                //guard.PopulationType = EntityPopulationType.RandomAmbient;
                guard.MaxHealth = 10000;
                guard.Health = 10000;
                guard.Armor = 5000;
            guard.Weapons.Give(WeaponHash.MicroSMG, 9999, false, true);
            guard.Weapons.Give(WeaponHash.APPistol, 9999, false, true);
            guard.Weapons.Give(WeaponHash.Knife, 1, false, true);
            guard.Weapons.Give(WeaponHash.Bat, 1, false, true);
            
            guard.DiesOnLowHealth = false;
            var ped = guard;
            // Ensure guards spawned by the script are in a normal reactive state
            try { ped.BlockPermanentEvents = false; ped.KeepTaskWhenMarkedAsNoLongerNeeded = false; } catch { }
            ped.SetCombatAttribute(CombatAttributes.AlwaysFight, true);
            ped.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
            ped.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, true);
            ped.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, true);
            ped.SetCombatAttribute(CombatAttributes.CanUseCover, true);
            ped.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
            ped.SetCombatAttribute(CombatAttributes.AlwaysFlee, false);
            ped.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, false);
            ped.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);
            //ped.SetCombatAttribute(CombatAttributes.DisableBulletReactions, true);
            ped.SetCombatAttribute(CombatAttributes.CanChaseTargetOnFoot, true);
            ped.SetCombatAttribute(CombatAttributes.SwitchToDefensiveIfInCover, true);
            ped.SetCombatAttribute(CombatAttributes.BlindFireWhenInCover, true);
            ped.SetCombatAttribute(CombatAttributes.CanUsePeekingVariations, true);
            //ped.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true);
            ped.SetCombatAttribute(CombatAttributes.CanTauntInVehicle, true);
            ped.SetCombatAttribute(CombatAttributes.AlwaysEquipBestWeapon, true);

            // Config flags - comprehensive set
            ped.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, true); // CRITICAL: No writhing - instant death
            ped.SetConfigFlag(PedConfigFlagToggles.CanDiveAwayFromApproachingVehicles, true);
            ped.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, true);
            ped.SetConfigFlag(PedConfigFlagToggles.AIDriverAllowFriendlyPassengerSeatEntry, true);
            ped.SetConfigFlag(PedConfigFlagToggles.KeepRelationshipGroupAfterCleanUp, true);
            ped.SetConfigFlag(PedConfigFlagToggles.AllowMedicsToReviveMe, true); // No medic revive if they die
            ped.SetConfigFlag(PedConfigFlagToggles.KeepTasksAfterCleanUp, true);
            ped.SetConfigFlag(PedConfigFlagToggles.TargetWhenInjuredAllowed, false);
            ped.SetConfigFlag(PedConfigFlagToggles.CanAttackFriendly, false);
            ped.SetConfigFlag(PedConfigFlagToggles.DisableBlindFiringInShotReactions, false);
            ped.SetConfigFlag(PedConfigFlagToggles.AvoidTearGas, true);
            ped.SetConfigFlag(PedConfigFlagToggles.ThrowingGrenadeWhileAiming, true);
            ped.SetConfigFlag(PedConfigFlagToggles.AllowMissionPedToUseInjuredMovement, true);
            //ped.SetConfigFlag(fire)


            // Give weapon
            WeaponHash weapon = GetRandomWeapon(guardConfig);
                guard.Weapons.Give(weapon, 9999, true, true);
                int currentAmmo = guard.Weapons.Current?.Ammo ?? 0;
                
                // Create guard blip (not for driver, only if blips enabled and area respects player)
                Blip guardBlip = null;
                if (seat != VehicleSeat.Driver && PlayerPositionLogger.GetEnableBlips() && DoesAreaRespectPlayer(selectedArea))
                {
                    guardBlip = guard.AddBlip();
                    if (guardBlip != null && guardBlip.Exists())
                    {
                        guardBlip.Sprite = BlipSprite.SecurityContract;
                        guardBlip.Color = GetBlipColorForArea(selectedArea);
                        guardBlip.Name = $"Guard {guardNumber}";
                        guardBlip.IsShortRange = false;
                        guardBlip.Scale = 0.8f;
                    }
                    guardNumber++;
                }
                
                var backupGuard = new BackupGuard
                {
                    Ped = guard,
                    InitialHealth = 10000,
                    InitialAmmo = currentAmmo,
                    HasWeapon = true,
                    Blip = guardBlip
                };
                
                squad.Guards.Add(backupGuard);
            }
            
            squad.InitialGuardCount = squad.Guards.Count;
            
            Logger.Log.Info($"Ground backup: {squad.Guards.Count} guards spawned, waiting for initialization...");
            
            // Small delay to ensure all guards are fully initialized
            Script.Yield();
            
            // Create GroundVehicle AI controller
            try
            {
                Logger.Log.Info("Creating GroundVehicle AI controller...");
                var groundVehicle = new GroundVehicle(vehicle, playerPos);
                
                if (groundVehicle.Driver == null)
                {
                    Logger.Log.Error("GroundVehicle driver is null, cleanup and abort");
                    if (vehicle != null && vehicle.Exists())
                    {
                        try
                        {
                            foreach (var occ in vehicle.Occupants)
                            {
                                if (occ != null && occ.Exists()) occ.MarkAsNoLongerNeeded();
                            }
                            vehicle.MarkAsNoLongerNeeded();
                        }
                        catch (Exception) { try { vehicle.MarkAsNoLongerNeeded(); } catch { } }
                    }
                    return;
                }
                
                squad.TacticalHelicopter = groundVehicle; // Reuse field for any AI controller
                
                Logger.Log.Info($"Ground backup spawned with {squad.Guards.Count} crew members");
                HelperClass.Notification($"~g~Ground unit en route with {squad.InitialGuardCount} guards!");
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"Failed to create GroundVehicle controller: {ex.Message}");
                Logger.Log.Error($"Stack trace: {ex.StackTrace}");
                // Clean up on failure
                if (vehicle != null && vehicle.Exists())
                {
                    try
                    {
                        foreach (var occ in vehicle.Occupants)
                        {
                            if (occ != null && occ.Exists()) occ.MarkAsNoLongerNeeded();
                        }
                        vehicle.MarkAsNoLongerNeeded();
                    }
                    catch (Exception) { try { vehicle.MarkAsNoLongerNeeded(); } catch { } }
                }
                return;
            }
            
            _activeSquads.Add(squad);
            
            vehicleModel.MarkAsNoLongerNeeded();
            guardModel.MarkAsNoLongerNeeded();
        }

    private void UpdateSquads()
    {
        // Get player position once per frame for efficiency
        Vector3 playerPos = Game.Player.Character.Position;
        const float cleanupDistance = 400f;

        for (int i = _activeSquads.Count - 1; i >= 0; i--)
        {
            var squad = _activeSquads[i];
            bool shouldRemoveSquad = false;

            // Check individual guards for cleanup conditions
            for (int j = squad.Guards.Count - 1; j >= 0; j--)
            {
                var guard = squad.Guards[j];

                if (guard.Ped == null || !guard.Ped.Exists())
                {
                    // Remove dead/invalid guard
                    guard.Blip?.Delete();
                    squad.Guards.RemoveAt(j);
                    Logger.Log.Info("Removed invalid guard from squad");
                    continue;
                }

                if (!guard.Ped.IsAlive)
                {
                    // Remove dead guard
                    guard.Blip?.Delete();
                    guard.Ped.MarkAsNoLongerNeeded();
                    squad.Guards.RemoveAt(j);
                    Logger.Log.Info("Removed dead guard from squad");
                }
            }

            // Check vehicle for cleanup conditions (if no AI controller managing it)
            if (squad.Vehicle != null && squad.Vehicle.Exists() && squad.TacticalHelicopter == null)
            {
                float vehicleDistance = Vector3.Distance(playerPos, squad.Vehicle.Position);

                if (vehicleDistance > cleanupDistance)
                {
                    Logger.Log.Info($"Unmanaged vehicle is too far ({vehicleDistance:F1}m), removing squad");
                    CleanupSquad(squad);
                    _activeSquads.RemoveAt(i);
                    continue;
                }
            }
            else if (squad.Vehicle != null && !squad.Vehicle.Exists())
            {
                // Vehicle destroyed/deleted
                Logger.Log.Info("Squad vehicle destroyed, marking squad for removal");
                shouldRemoveSquad = true;
            }

            // Update AI controllers and check FSM flee state
            try
            {
                if (squad.TacticalHelicopter != null)
                {
                    // Call Update on the appropriate AI controller
                    if (squad.TacticalHelicopter is AttackHelicopter attackHeli)
                    {
                        if (attackHeli.IsHelicopterValid())
                        {
                            // Check if helicopter FSM is in Flee state and out of range
                            bool isHelicoFleeing = attackHeli.CurrentState == AttackHelicopter.HelicopterState.Flee;
                            float heliDistance = Vector3.Distance(playerPos, attackHeli.Helicopter.Position);

                            if (isHelicoFleeing && heliDistance > cleanupDistance)
                            {
                                Logger.Log.Info($"Attack helicopter is fleeing and too far ({heliDistance:F1}m), removing squad");
                                CleanupSquad(squad);
                                _activeSquads.RemoveAt(i);
                                continue;
                            }

                            attackHeli.Update();
                        }
                        else
                        {
                            Logger.Log.Info("Attack helicopter destroyed/invalid, removing squad");
                            CleanupSquad(squad);
                            _activeSquads.RemoveAt(i);
                            continue;
                        }
                    }
                    else if (squad.TacticalHelicopter is TacticalHelicopter tacticalHeli)
                    {
                        if (tacticalHeli.IsHelicopterValid())
                        {
                            // Mark deployment complete when tactical heli reports completion states
                            try
                            {
                                if (!squad.DeploymentComplete &&
                                    (tacticalHeli.CurrentTask == TacticalHelicopter.Task.RappelComplete ||
                                     tacticalHeli.CurrentTask == TacticalHelicopter.Task.LandingComplete ||
                                     tacticalHeli.CurrentTask == TacticalHelicopter.Task.ParatroopComplete))
                                {
                                    squad.DeploymentComplete = true;
                                    squad.DeploymentCompleteTime = DateTime.Now;
                                    Logger.Log.Info($"Tactical squad deployment completed at {squad.DeploymentCompleteTime}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Warning($"Error checking tactical heli deployment state: {ex.Message}");
                            }

                            // Check if helicopter FSM is in Flee state and out of range
                            bool isHelicoFleeing = tacticalHeli.CurrentTask == TacticalHelicopter.Task.Flee;
                            float heliDistance = Vector3.Distance(playerPos, tacticalHeli.Helicopter.Position);

                            if (isHelicoFleeing && heliDistance > cleanupDistance)
                            {
                                // Ensure we don't cleanup while guards are still rappelling/exiting the helicopter
                                bool guardsStillDeploying = false;
                                try
                                {
                                    if (squad.Vehicle != null && squad.Vehicle.Exists())
                                    {
                                        foreach (var bg in squad.Guards)
                                        {
                                            if (bg.Ped == null || !bg.Ped.Exists()) continue;

                                            if (bg.Ped.IsInVehicle(squad.Vehicle))
                                            {
                                                guardsStillDeploying = true;
                                                break;
                                            }

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
                                    Logger.Log.Info($"Tactical helicopter is fleeing but guards still deploying; deferring cleanup ({heliDistance:F1}m)");
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
                                            Logger.Log.Info($"Waiting grace period after deployment ({elapsed:F1}s)");
                                        }
                                        else
                                        {
                                            Logger.Log.Info($"Tactical helicopter is fleeing and too far ({heliDistance:F1}m), removing squad");
                                            CleanupSquad(squad);
                                            _activeSquads.RemoveAt(i);
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        Logger.Log.Info($"Tactical helicopter is fleeing and too far ({heliDistance:F1}m), removing squad");
                                        CleanupSquad(squad);
                                        _activeSquads.RemoveAt(i);
                                        continue;
                                    }
                                }
                            }

                            tacticalHeli.Update();
                        }
                        else
                        {
                            Logger.Log.Info("Tactical helicopter destroyed/invalid, removing squad");
                            CleanupSquad(squad);
                            _activeSquads.RemoveAt(i);
                            continue;
                        }
                    }
                    else if (squad.TacticalHelicopter is GroundVehicle groundVehicle)
                    {
                        if (groundVehicle.IsVehicleValid())
                        {
                            // Check if ground vehicle FSM is in Flee state and out of range
                            bool isVehicleFleeing = groundVehicle.Task == GroundVehicle.VehicleTask.Flee;
                            float vehicleDistance = Vector3.Distance(playerPos, groundVehicle.Vehicle.Position);

                            if (isVehicleFleeing && vehicleDistance > cleanupDistance)
                            {
                                Logger.Log.Info($"Ground vehicle is fleeing and too far ({vehicleDistance:F1}m), removing squad");
                                CleanupSquad(squad);
                                _activeSquads.RemoveAt(i);
                                continue;
                            }

                            //groundVehicle.Update();
                        }
                        else
                        {
                            Logger.Log.Info("Ground vehicle destroyed/invalid, removing squad");
                            CleanupSquad(squad);
                            _activeSquads.RemoveAt(i);
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"Error updating squad AI: {ex.Message}");
            }

            // Check if all guards are dead or removed - cleanup entire squad
            if (squad.Guards.Count == 0 || AllGuardsDead(squad))
            {
                Logger.Log.Info("All guards in squad are dead/removed, removing squad");
                HelperClass.Notification("~r~Backup squad eliminated");
                CleanupSquad(squad);
                _activeSquads.RemoveAt(i);
                continue;
            }

            // Remove squad if marked for removal
            if (shouldRemoveSquad)
            {
                CleanupSquad(squad);
                _activeSquads.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Check if all guards in a squad are dead
    /// </summary>
    private bool AllGuardsDead(BackupSquad squad)
    {
        if (squad.Guards.Count == 0) return false;

        foreach (var guard in squad.Guards)
        {
            if (guard.Ped != null && guard.Ped.Exists() && guard.Ped.IsAlive)
            {
                return false; // At least one guard is alive
            }
        }

        return true; // All guards are dead
    }

    /// <summary>
    /// Cleanup squad entities (just remove blips and references, don't delete peds/vehicles)
    /// </summary>
    private void CleanupSquad(BackupSquad squad)
    {
        try
        {
            Logger.Log.Info($"Cleaning up {squad.SquadType} squad...");

            // Delete blips
            squad.VehicleBlip?.Delete();

            // Cleanup all guards
            foreach (var guard in squad.Guards)
            {
                guard.Blip?.Delete();

                if (guard.Ped != null && guard.Ped.Exists())
                {
                    guard.Ped.MarkAsNoLongerNeeded();
                    Logger.Log.Info($"  Marked guard as no longer needed");
                }
            }

            // Clear guards list
            squad.Guards.Clear();

            // Cleanup vehicle
            if (squad.Vehicle != null && squad.Vehicle.Exists())
            {
                // Mark all occupants as no longer needed
                foreach (var occupant in squad.Vehicle.Occupants)
                {
                    if (occupant != null && occupant.Exists())
                    {
                        occupant.MarkAsNoLongerNeeded();
                    }
                }

                squad.Vehicle.MarkAsNoLongerNeeded();
                Logger.Log.Info($"  Marked vehicle as no longer needed");
            }

            // Cleanup AI controllers
            if (squad.TacticalHelicopter != null)
            {
                if (squad.TacticalHelicopter is AttackHelicopter attackHeli)
                {
                    // Cleanup attack helicopter
                    if (attackHeli.Helicopter != null && attackHeli.Helicopter.Exists())
                    {
                        attackHeli.Helicopter.MarkAsNoLongerNeeded();
                    }

                    if (attackHeli.Pilot != null && attackHeli.Pilot.Exists())
                    {
                        attackHeli.Pilot.MarkAsNoLongerNeeded();
                    }

                    // Cleanup crew
                    foreach (var crew in attackHeli.Crew)
                    {
                        if (crew != null && crew.Exists())
                        {
                            crew.MarkAsNoLongerNeeded();
                        }
                    }

                    Logger.Log.Info($"  Marked attack helicopter and crew as no longer needed");
                }
                else if (squad.TacticalHelicopter is TacticalHelicopter tacticalHeli)
                {
                    // Cleanup tactical helicopter
                    if (tacticalHeli.Helicopter != null && tacticalHeli.Helicopter.Exists())
                    {
                        tacticalHeli.Helicopter.MarkAsNoLongerNeeded();
                    }

                    if (tacticalHeli.Pilot != null && tacticalHeli.Pilot.Exists())
                    {
                        tacticalHeli.Pilot.MarkAsNoLongerNeeded();
                    }

                    // Cleanup crew
                    foreach (var crew in tacticalHeli.Crew)
                    {
                        if (crew != null && crew.Exists())
                        {
                            crew.MarkAsNoLongerNeeded();
                        }
                    }

                    Logger.Log.Info($"  Marked tactical helicopter and crew as no longer needed");
                }
                else if (squad.TacticalHelicopter is GroundVehicle groundVehicle)
                {
                    // Cleanup ground vehicle
                    if (groundVehicle.Vehicle != null && groundVehicle.Vehicle.Exists())
                    {
                        groundVehicle.Vehicle.MarkAsNoLongerNeeded();
                    }

                    if (groundVehicle.Driver != null && groundVehicle.Driver.Exists())
                    {
                        groundVehicle.Driver.MarkAsNoLongerNeeded();
                    }

                    Logger.Log.Info($"  Marked ground vehicle and driver as no longer needed");
                }
            }

            Logger.Log.Info($"{squad.SquadType} squad cleanup complete");
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Error cleaning up squad: {ex.Message}");
        }
    }


}


public class Test : Script
{
    public Test()
    {
        //KeyDown += OnTick;
    }

    public void OnTick(object sender, KeyEventArgs e)
    {
        var get = Game.Player.Character.CurrentVehicle;
        if (Game.Player.Character.IsInHeli && e.KeyCode == Keys.B)
        {
            Game.Player.Character.Task.WarpIntoVehicle(get, VehicleSeat.LeftRear);
            var chr2 = get.CreatePedOnSeat(VehicleSeat.RightRear, "S_m_y_swat_01");
            var chr3 = get.CreatePedOnSeat(VehicleSeat.ExtraSeat1, "S_m_y_swat_01");
            var chr4 = get.CreatePedOnSeat(VehicleSeat.ExtraSeat2, "S_m_y_swat_01");
            Ped chr = null;
            Wait(100);
            for (int i = 0; i < 100; i++)
            {
                if (get.IsSeatFree(VehicleSeat.Driver))
                {
                    chr = Game.Player.Character.CurrentVehicle.CreatePedOnSeat(VehicleSeat.Driver, "S_m_y_swat_01");
                    chr.Task.StartHeliMission(
                        get, get, VehicleMissionType.Stop, 0, 0, 0, 0, 0, 0); break;
                }
            }

            chr2.Task.RappelFromHelicopter();
            chr3.Task.RappelFromHelicopter();
            chr4.Task.RappelFromHelicopter();
            chr.KeepTaskWhenMarkedAsNoLongerNeeded = true;
            Game.Player.Character.Task.RappelFromHelicopter();

            //chr.MarkAsNoLongerNeeded();
            chr2.MarkAsNoLongerNeeded(); chr3.MarkAsNoLongerNeeded(); chr4.MarkAsNoLongerNeeded();

        }
    }
}