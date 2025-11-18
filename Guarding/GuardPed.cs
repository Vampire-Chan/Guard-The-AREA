using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using Guarding.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

public class GuardPed
{
    public GuardShiftType Shift;
    private string _activeScenario;
    public Vector3 Position { get; set; }
    public float Heading { get; set; }
    public string AreaName { get; set; }
    public string Type { get; set; }
    public bool Interior { get; set; }
    private readonly Area Area;
    public Ped guardPed;
    public RelationshipGroup GuardGroup { get; set; }

    private Vector3 _originalPosition;
    private float _originalHeading;  // Removed readonly so position can be updated after vehicle arrival
    private const float RETURN_THRESHOLD = 2f;
    private const float GUARD_RETURN_DISTANCE_THRESHOLD = 30f;

    private string PedModelName;
    public string GetPedModelName()
    {
        return PedModelName;
    }
    private string WeaponName;
    private readonly GuardConfig GuardConfig;
    private readonly Scenarios ScenarioConfig;
    public static readonly Random _random = new Random();

    private ScenarioType States = ScenarioType.Random; 
    public GuardState CurrentState { get; set; } = GuardState.Idle;
    private GuardState _prevState = GuardState.Idle;

    // Greeting fields
    private bool _hasGreeted=false;
    private DateTime _greetingStartTime;
    private bool _isGreeting = false;
    public static float GREETING_TRIGGER_DISTANCE = 5;
    public  static float GREETING_RESET_DISTANCE = 25;
    private const int GREETING_ANIMATION_DURATION_MS = 4000; // 4 seconds

    // Duty task tracking (prevent repeated task assignment)
    private bool _dutyTaskAssigned = false;

    // Arrival driver warp tracking (prevent infinite loop)
    public bool DriverWarpAttempted { get; set; } = false;

    // Post-combat observation tracking
    private DateTime _postCombatStartTime;
    private const int POST_COMBAT_OBSERVE_MIN_SECONDS = 10;
    private const int POST_COMBAT_OBSERVE_MAX_SECONDS = 20;
    private int _postCombatObserveDuration; // Random duration for this guard


    // Vehicle coordination properties
    public GuardVehicle AssignedVehicle { get; set; }
    public VehicleSeat AssignedSeatIndex { get; set; } = VehicleSeat.Driver;

    // Blip management
    public Blip GuardBlip { get; private set; }


    public GuardPed(GuardSpawnPoint point, GuardConfig guardConfig, Area area, Scenarios scenarios)
    {
        Position = point.Position;
        Heading = point.Heading;
        AreaName = area.Name ?? throw new ArgumentNullException(nameof(area.Name));
        Type = point.Type ?? throw new ArgumentNullException(nameof(point.Type));
        GuardConfig = guardConfig;
        Area = area;
        ScenarioConfig = scenarios;
        GuardGroup = GuardConfig.RelationshipGroup;
        Interior = point.Interior;

        // Initialize original position and heading used for return-to-duty logic.
        _originalPosition = point.Position;
        _originalHeading = point.Heading;

        // If no scenario is provided at spawn, choose one randomly based on the area's scenario list.
        _activeScenario = string.IsNullOrEmpty(point.Scenario)
            ? GetRandomElement(ScenarioConfig.ScenarioList)
            : point.Scenario;

        States = GetScenarioTypeFromName(Area.Scenarios.Name);
        RandomizeLoadout();
        ChangeState(GuardState.OnDuty);
    }

    // Method to update the guard's assigned position after vehicle arrival
    public void UpdateAssignedPosition(Vector3 newPosition, float newHeading)
    {
        Position = newPosition;
        Heading = newHeading;
        _originalPosition = newPosition;  // CRITICAL: Update the original position used by PerformPositionAndDutyCheck
        _originalHeading = newHeading;    // CRITICAL: Update the original heading
        
        Logger.Log.Info($"Guard {AreaName} assigned position updated to {newPosition}, heading {newHeading}");
    }

    // Properly change state and update blip color
    public void ChangeState(GuardState newState)
    {
        if (CurrentState != newState)
        {
            Logger.Log.Info($"Guard {AreaName} changing state from {CurrentState} to {newState}");
            _prevState = CurrentState;
            CurrentState = newState;
            
            // Reset duty task flag when leaving OnDuty state
            // This allows task reassignment when returning to OnDuty
            if (CurrentState != GuardState.OnDuty)
            {
                _dutyTaskAssigned = false;
            }
            
            UpdateBlipColor();
        }
    }

    public void StartDeparture(GuardVehicle v, VehicleSeat seat)
    {
        AssignedVehicle = v;
        AssignedSeatIndex = seat;
        ChangeState(GuardState.Departing);
        Shift = GuardShiftType.ReachVehicle; // Set the first state in the FSM
        v.ChangeState(VehicleState.Departing);
        
        // CRITICAL: Clear any ambient tasks that might interfere with departure
        // Guards might be investigating dead bodies, searching for threats, etc.
        if (guardPed != null && guardPed.Exists())
        {
            Logger.Log.Info($"Guard {AreaName} starting departure - clearing all tasks to prevent ambient interference");
            guardPed.Task.ClearAllImmediately();
            
            // DON'T block permanent events during departure - guards need to react to threats
            // guardPed.BlockPermanentEvents = true; // REMOVED - prevents combat reactions
        }
    }

    public void StartArrival(GuardVehicle v, VehicleSeat seat)
    {
        AssignedVehicle = v;
        AssignedSeatIndex = seat;
        ChangeState(GuardState.Arriving);
        Shift = (seat == VehicleSeat.Driver) ? GuardShiftType.ArrivingAsDriver : GuardShiftType.ArrivingAsPassenger;
        v.ChangeState(VehicleState.Arriving);
    }

    private void RandomizeLoadout()
    {
        PedModelName = GetRandomElementOrDefault(GuardConfig.PedModels, "PedModels");
        WeaponName = GetRandomElementOrDefault(GuardConfig.Weapons, "Weapons");
    }

    private string GetRandomElementOrDefault(List<string> list, string logContext)
    {
        // Performance: Use Count > 0 instead of .Any()
        if (list != null && list.Count > 0)
        {
            return GetRandomElement(list);
        }
        Logger.Log.Fatal($"Warning: No valid {logContext} found for GuardConfig '{GuardConfig.Name}'. Random selection skipped.");
        return null;
    }

    private T GetRandomElement<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("List cannot be null or empty");
        return list[_random.Next(list.Count)];
    }

    private ScenarioType GetScenarioTypeFromName(string scenarioName)
    {
        return scenarioName.ToLower() switch
        {
            "guard" => ScenarioType.Guard,
            "patrol" => ScenarioType.Patrol,
            "ambient" => ScenarioType.Ambient,
            "random" => ScenarioType.Random,
            _ => ScenarioType.Vehicle,
        };
    }



    private void HandleGreetingLogic()
    {
        // Early exit guard clauses
        if (guardPed == null || !guardPed.Exists() ||
            Game.Player.Character == null || !Game.Player.Character.Exists())
            return;

        // Only allow greetings for guards on duty who are on foot
        if (CurrentState != GuardState.OnDuty || guardPed.IsInVehicle())
            return;

        // Calculate distance once and reuse
        float distance = guardPed.Position.DistanceTo(Game.Player.Character.Position);

        // Check if greeting animation has finished and resume scenario
        if (_isGreeting)
        {
            TimeSpan greetingDuration = DateTime.Now - _greetingStartTime;
            if (greetingDuration.TotalMilliseconds >= GREETING_ANIMATION_DURATION_MS)
            {
                // Greeting animation finished - resume scenario
                _isGreeting = false;
                ResumeScenarioAfterGreeting();
                Logger.Log.Info($"Guard {AreaName} finished greeting, resuming scenario '{_activeScenario}'");
            }
            return; // Don't trigger new greeting while one is active
        }

        // Handle greeting trigger
        if (distance <= GREETING_TRIGGER_DISTANCE &&
            !_hasGreeted &&
            IsGuardCompanion())
        {
            PlayPlayerResponse();
            PlayGuardAnimation();
            _hasGreeted = true; // Set flag to prevent immediate re-greeting
            _isGreeting = true; // Mark that greeting is in progress
            _greetingStartTime = DateTime.Now; // Track when greeting started
            Logger.Log.Info($"Guard {AreaName} starting greeting animation");
        }

        // Reset greeting flag when player moves away
        if (distance > GREETING_RESET_DISTANCE && _hasGreeted)
        {
            _hasGreeted = false;
        }
    }
    
    /// <summary>
    /// Resume the appropriate scenario animation after greeting the player
    /// </summary>
    private void ResumeScenarioAfterGreeting()
    {
        if (guardPed == null || !guardPed.Exists())
            return;

        try
        {
            // Resume appropriate behavior based on scenario type
            if (States == ScenarioType.Guard)
            {
                // For guard type, resume standing guard with scenario
                guardPed.Task.StartScenarioInPlace(_activeScenario);
                Logger.Log.Info($"Guard {AreaName} resuming StandGuard with scenario '{_activeScenario}'");
            }
            else if (States == ScenarioType.Patrol)
            {
                // For patrol, resume patrolling (no scenario needed)
                guardPed.GuardCurrentPosition(_random.Next(2) == 0);
                Logger.Log.Info($"Guard {AreaName} resuming patrol");
            }
            else // Ambient or Random scenarios
            {
                // For ambient/random, restart the scenario animation
                guardPed.Task.StartScenarioInPlace(_activeScenario);
                Logger.Log.Info($"Guard {AreaName} resuming scenario '{_activeScenario}' (Type: {States})");
            }
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Error resuming scenario after greeting for {AreaName}: {ex.Message}");
        }
    }


    private bool IsGuardCompanion()
    {
        int relationship = Function.Call<int>(Hash.GET_RELATIONSHIP_BETWEEN_GROUPS,
                                                guardPed.RelationshipGroup,
                                                Game.Player.Character.RelationshipGroup);
        return relationship == 0; // 0 indicates companion relationship.
    }

    private void PlayGuardAnimation()
    {
        string[] guardGreetings = { "GENERIC_HI", "GENERIC_BYE", "GENERIC_HOWS_IT_GOING", "GENERIC_THANKS" };
        string guardSpeech = guardGreetings[_random.Next(guardGreetings.Length)];

        Function.Call(Hash.TASK_PLAY_ANIM, guardPed,
                      "gestures@m@standing@casual", "gesture_hello",
                      1.0f, -1.0f, 4000, AnimationFlags.UpperBodyOnly,
                      0, false, 0, false);

        Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE, guardPed, guardSpeech, "SPEECH_PARAMS_FORCE");
    }

    private void PlayPlayerResponse()
    {
        Ped player = Game.Player.Character;
        if (player == null || !player.Exists())
            return;

        string[] responses;
        if (player.Model == PedHash.Michael ||
            player.Model == PedHash.Franklin ||
            player.Model == PedHash.Trevor)
        {
            responses = new string[] { "GENERIC_HI", "GENERIC_BYE", "GENERIC_THANKS" };
        }
        else
        {
            responses = new string[] { "GENERIC_HI", "GENERIC_HOWS_IT_GOING", "GENERIC_THANKS" };
        }
        string response = responses[_random.Next(responses.Length)];

        string[] anims = { "gesture_hello", "mp_player_int_salute", "mp_player_int_uppersalute" };
        string anim = anims[_random.Next(anims.Length)];
        string animDict = (anim == "gesture_hello")
                          ? "gestures@m@standing@casual"
                          : "mp_player_intsalute";

        Function.Call(Hash.TASK_PLAY_ANIM, player,
                      animDict, anim,
                      1.0f, -1.0f, 4000, AnimationFlags.UpperBodyOnly,
                      0, false, 0, false);

        Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE, player, response, "SPEECH_PARAMS_FORCE");
    }

    public Ped SpawnGuard(string modelName, Vector3 position)
    {
        Model mdl = new Model(modelName);
        if (!mdl.IsInCdImage)
        {
            Logger.Log.Fatal($"Model {mdl} is not in CD Image. Area: {AreaName} and Guard Model: {GuardConfig.Name}.");
            HelperClass.Subtitle($"Model: {mdl} not found.");
            return null;
        }
        mdl.Request(500);
        Ped spawnedPed = World.CreatePed(mdl, position);
        mdl.MarkAsNoLongerNeeded();

        if (spawnedPed == null)
        {
            Logger.Log.Fatal($"Failed to create guard ped with model {modelName}.");
            return null;
        }
        InitializePed(spawnedPed);
        return spawnedPed;
    }

    private void InitializePed(Ped ped, bool isGunner = false)
    {
        ped.Heading = Heading;
        ped.Weapons.Give(WeaponName, 1500, true, true);
        ped.Armor = 200;
        ped.MaxHealth = 300;
        ped.Health = 300;
        ped.DrivingAggressiveness = 1f;
        ped.IsCollisionEnabled = true;
        ped.DiesOnLowHealth = false; 
        ped.RelationshipGroup = GuardGroup;
        Function.Call(Hash.SET_PED_RANDOM_PROPS, ped);
        Function.Call(Hash.SET_PED_RANDOM_COMPONENT_VARIATION, ped);

        // Increase perception ranges so guards notice threats from farther away
        try
        {
            ped.SeeingRange = 100f;   // vision
            ped.HearingRange = 200f;  // hearing
        }
        catch { /* If properties are not available on some runtimes, ignore */ }

        // Combat attributes - ALL guards get these (including gunners)
        ped.SetCombatAttribute(CombatAttributes.AlwaysFight, true);
        ped.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
        ped.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, true);
        ped.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, true);
        ped.SetCombatAttribute(CombatAttributes.CanUseCover, true);
        ped.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
        ped.SetCombatAttribute(CombatAttributes.AlwaysFlee, false);
        ped.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, false);
        ped.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);
        ped.SetCombatAttribute(CombatAttributes.DisableBulletReactions, true);
        ped.SetCombatAttribute(CombatAttributes.CanChaseTargetOnFoot, true);
        ped.SetCombatAttribute(CombatAttributes.SwitchToDefensiveIfInCover, true);
        ped.SetCombatAttribute(CombatAttributes.BlindFireWhenInCover, true);
        ped.SetCombatAttribute(CombatAttributes.CanUsePeekingVariations, true);
        ped.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true);
        ped.SetCombatAttribute(CombatAttributes.CanTauntInVehicle, true);
        ped.SetCombatAttribute(CombatAttributes.AlwaysEquipBestWeapon, true);

        // Config flags - comprehensive set for ALL guards
        ped.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, true); // CRITICAL: No writhing - instant death
        ped.SetConfigFlag(PedConfigFlagToggles.CanDiveAwayFromApproachingVehicles, true);
        ped.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, true);
        ped.SetConfigFlag(PedConfigFlagToggles.AIDriverAllowFriendlyPassengerSeatEntry, true);
        ped.SetConfigFlag(PedConfigFlagToggles.KeepRelationshipGroupAfterCleanUp, true);
        ped.SetConfigFlag(PedConfigFlagToggles.AllowMedicsToReviveMe, false); // No medic revive if they die
        ped.SetConfigFlag(PedConfigFlagToggles.KeepTasksAfterCleanUp, true);
        ped.SetConfigFlag(PedConfigFlagToggles.TargetWhenInjuredAllowed, false);
        ped.SetConfigFlag(PedConfigFlagToggles.CanAttackFriendly, false);
        ped.SetConfigFlag(PedConfigFlagToggles.DisableBlindFiringInShotReactions, false);
        ped.SetConfigFlag(PedConfigFlagToggles.AvoidTearGas, true);

        // Additional attributes specific to gunners
        if (isGunner)
        {
            // Gunner-specific: Stay in vehicle
            ped.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, false);
            ped.SetCombatAttribute(CombatAttributes.UseVehicleAttackIfVehicleHasMountedGuns, true);
            Logger.Log.Info($"Guard {AreaName} initialized as GUNNER with full combat attributes");
        }
        
        // CRITICAL: Use Mission population type to prevent ambient behaviors
        ped.PopulationType = EntityPopulationType.Mission;
        
        // DON'T block permanent events - it prevents combat reactions!
        // ped.BlockPermanentEvents = true; // REMOVED

        if (!Interior)
        {
            OutputArgument groundZArg = new OutputArgument();
            Function.Call(Hash.GET_GROUND_Z_FOR_3D_COORD, Position.X, Position.Y, Position.Z + 5, groundZArg, false, false);
            ped.Position = new Vector3(Position.X, Position.Y, groundZArg.GetResult<float>());
        }

        if (ped.PedType == PedType.Cop || ped.PedType == PedType.Swat || ped.PedType == PedType.Army)
        {
            ped.SetConfigFlag(PedConfigFlagToggles.LawWillOnlyAttackIfPlayerIsWanted, true);
            ped.SetConfigFlag(PedConfigFlagToggles.KeepRelationshipGroupAfterCleanUp, true);
            ped.SetConfigFlag(PedConfigFlagToggles.KeepTasksAfterCleanUp, value: true);
            ped.SetConfigFlag(PedConfigFlagToggles.KeepTargetLossResponseOnCleanup, value: true);
            ped.SetConfigFlag(PedConfigFlagToggles.DontAttackPlayerWithoutWantedLevel, true);
            ped.TargetLossResponse = TargetLossResponse.SearchForTarget;
            if (ped.PedType != PedType.Cop) ped.SetCombatAttribute(CombatAttributes.CanThrowSmokeGrenade, true);
        }
    }

    // Setup relationships for the guard.
    public void SetupRelationships(bool gunner = false)
    {
        Ped pedToConfigure = guardPed;
        if (pedToConfigure == null)
        {
            Logger.Log.Fatal($"Warning: SetupRelationships called for {(gunner ? "gunner" : "ped")}, but it is null.");
            return;
        }
        try
        {
            var lawGroups = new List<uint>
            {
                GetHash("PRIVATE_SECURITY"),
                GetHash("SECURITY_GUARD"),
                GetHash("ARMY"),
                GetHash("COP"),
                GetHash("GUARD_DOG"),
                GetHash("INVESTIGATE")
            };

            foreach (uint lawA in lawGroups)
            {
                foreach (uint lawB in lawGroups)
                {
                    pedToConfigure.SetConfigFlag(PedConfigFlagToggles.CanAttackNonWantedPlayerAsLaw, false);
                    pedToConfigure.SetConfigFlag(PedConfigFlagToggles.LawWillOnlyAttackIfPlayerIsWanted, true);
                    pedToConfigure.TargetLossResponse = TargetLossResponse.SearchForTarget;
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, PedRelationship.Respect, lawA, lawB);
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, PedRelationship.Respect, lawB, lawA);
                }
            }

            // Setup the ped's relationship group.
            pedToConfigure.RelationshipGroup = World.AddRelationshipGroup(GuardConfig.RelationshipGroup);
            pedToConfigure.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigure.RelationshipGroup, Relationship.Companion, true);

            // Respect rules: if area demands respect based on settings.
            if (Area.Respect == "YES" || Area.Respect == "ANY" || Area.Respect == "ALL")
            {
                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigure.RelationshipGroup, Relationship.Companion);
                pedToConfigure.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Companion);
            }
            else if ((Area.Respect == "TREVOR" && Game.Player.Character.Model == PedHash.Trevor) ||
                     (Area.Respect == "MICHAEL" && Game.Player.Character.Model == PedHash.Michael) ||
                     (Area.Respect == "FRANKLIN" && Game.Player.Character.Model == PedHash.Franklin))
            {
                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigure.RelationshipGroup, Relationship.Companion);
                pedToConfigure.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Companion);
            }
            else
            {
                HandleMultipleRespectEntries(pedToConfigure);
            }
            
            // Note: Cross-area guard relationships are now setup centrally in GuardSpawner
            // during initialization, so we don't need to do it per-guard anymore
        }
        catch (Exception ex)
        {
            Logger.Log.Fatal($"Error in SetupRelationships: {ex.Message} StackTrace: {ex.StackTrace}");
            Notification.PostTicker($"Error setting up relationships. Check log. {ex.Message} StackTrace: {ex.StackTrace}", false);
            throw;
        }
    }
    
    private void HandleMultipleRespectEntries(Ped pedToConfigure)
    {
        bool respectedCharacter = false;
        string[] respectedCharactersList = Area.Respect?.Split(',') ?? new string[0];

        foreach (string characterName in respectedCharactersList)
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
            Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigure.RelationshipGroup, Relationship.Companion);
            pedToConfigure.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Companion);
        }
    }

    public static uint GetHash(string characterName)
    {
        return StringHash.AtStringHash(characterName);
    }
    public void UpdateCombatState()
    {
        if (guardPed == null || !guardPed.Exists() || guardPed.IsDead)
            return;

        HandleGreetingLogic();
        HandleWeaponSwitching(); // NEW: Smart weapon switching based on threat
        PerformPositionAndDutyCheck();
    }
    
    /// <summary>
    /// Handles intelligent weapon switching:
    /// - Use fists/melee when enemy uses fists/melee
    /// - Pull out firearm when enemy pulls out firearm
    /// - Holster weapon after combat
    /// </summary>
    private void HandleWeaponSwitching()
    {
        if (guardPed == null || !guardPed.Exists() || guardPed.IsDead)
            return;
            
        // Use game's combat detection directly
        bool isInCombat = guardPed.IsInCombat || guardPed.IsShooting || guardPed.IsInMeleeCombat;
        
        if (isInCombat)
        {
            // Get current enemy using native function
            Ped enemy = Function.Call<Ped>(Hash.GET_PED_TARGET_FROM_COMBAT_PED, guardPed, 0);
            
            if (enemy != null && enemy.Exists() && !enemy.IsDead)
            {
                // Check what weapon the enemy is using
                WeaponHash enemyWeapon = enemy.Weapons.Current.Hash;
                WeaponHash guardWeapon = guardPed.Weapons.Current.Hash;
                
                bool enemyUsingMelee = enemyWeapon == WeaponHash.Unarmed || 
                                       IsMeleeWeapon(enemyWeapon);
                bool guardUsingMelee = guardWeapon == WeaponHash.Unarmed || 
                                       IsMeleeWeapon(guardWeapon);
                
                // If enemy is unarmed/melee and guard has firearm out, switch to unarmed/melee
                if (enemyUsingMelee && !guardUsingMelee)
                {
                    Logger.Log.Info($"Guard {AreaName}: Enemy using melee, switching to fists");
                    guardPed.Weapons.Select(WeaponHash.Unarmed, true);
                }
                // If enemy pulls out firearm and guard is using melee, switch to firearm
                else if (!enemyUsingMelee && guardUsingMelee)
                {
                    Logger.Log.Info($"Guard {AreaName}: Enemy using firearm, switching to weapon");
                    // Get weapon hash from weapon name string using proper hash function
                    uint weaponHashValue = StringHash.AtStringHash(WeaponName);
                    WeaponHash weaponHash = (WeaponHash)weaponHashValue;
                    if (guardPed.Weapons.HasWeapon(weaponHash))
                    {
                        guardPed.Weapons.Select(weaponHash, true);
                    }
                }
            }
        }
        // Only holster weapon when truly safe - back to normal duty (not during PostCombat observation)
        else if (CurrentState == GuardState.OnDuty)
        {
            // Holster weapon only when returned to normal duty
            WeaponHash currentWeapon = guardPed.Weapons.Current.Hash;
            if (currentWeapon != WeaponHash.Unarmed)
            {
                Logger.Log.Info($"Guard {AreaName}: All clear, holstering weapon");
                guardPed.Weapons.Select(WeaponHash.Unarmed, true);
            }
        }
    }
    
    /// <summary>
    /// Check if a weapon is a melee weapon
    /// </summary>
    private bool IsMeleeWeapon(WeaponHash weapon)
    {
        return weapon == WeaponHash.Knife ||
               weapon == WeaponHash.Nightstick ||
               weapon == WeaponHash.Hammer ||
               weapon == WeaponHash.Bat ||
               weapon == WeaponHash.GolfClub ||
               weapon == WeaponHash.Crowbar ||
               weapon == WeaponHash.Bottle ||
               weapon == WeaponHash.Dagger ||
               weapon == WeaponHash.Hatchet ||
               weapon == WeaponHash.KnuckleDuster ||
               weapon == WeaponHash.Machete ||
               weapon == WeaponHash.Flashlight ||
               weapon == WeaponHash.SwitchBlade ||
               weapon == WeaponHash.BattleAxe ||
               weapon == WeaponHash.PoolCue ||
               weapon == WeaponHash.Wrench;
    }

    private float GetActiveReturnThreshold()
    {
        return (States == ScenarioType.Patrol) ? GUARD_RETURN_DISTANCE_THRESHOLD : RETURN_THRESHOLD;
    }

    /// <summary>
    /// Manages the guard's state and behavior using a Finite State Machine.
    /// This method should be called on every game tick.
    /// </summary>
    private void PerformPositionAndDutyCheck()
    {
        if (guardPed == null || !guardPed.Exists())
        {
            return;
        }

        //HelperClass.Subtitle($"Guard State: {CurrentState}");

        // Use game's IsInCombat directly to set combat state
        bool isInCombat = guardPed.IsInCombat || guardPed.IsShooting || guardPed.IsInMeleeCombat;

        // Handle combat state transitions (highest priority)
        // Exception: Don't re-enter combat if in PostCombat observation mode (unless actively being shot)
        if (isInCombat && CurrentState != GuardState.InCombat && CurrentState != GuardState.PostCombat)
        {
            Logger.Log.Info($"Guard {AreaName}: Game detected combat, entering InCombat state");
            ChangeState(GuardState.InCombat);
            return;
        }
        
        // If in PostCombat and being shot at again, re-enter combat immediately
        if (CurrentState == GuardState.PostCombat && isInCombat)
        {
            Logger.Log.Info($"Guard {AreaName} is under fire during observation, re-entering combat!");
            ChangeState(GuardState.InCombat);
            return;
        }

        // Handle post-combat state transition - enter observation mode
        if (CurrentState == GuardState.InCombat && !isInCombat)
        {
            Logger.Log.Info($"Guard {AreaName} has exited combat. Entering PostCombat observation mode.");
            
            // Set random observation duration (10-20 seconds)
            _postCombatObserveDuration = _random.Next(POST_COMBAT_OBSERVE_MIN_SECONDS, POST_COMBAT_OBSERVE_MAX_SECONDS + 1);
            _postCombatStartTime = DateTime.Now;
            
            ChangeState(GuardState.PostCombat);
            
            Logger.Log.Info($"Guard {AreaName} will observe for {_postCombatObserveDuration} seconds before returning to post.");
            return;
        }

        // CORE FINITE STATE MACHINE
        switch (CurrentState)
        {
            case GuardState.Idle:
                // Handle idle state - transition to appropriate state when ready
                if (guardPed.IsOnFoot && !guardPed.IsInVehicle() && !guardPed.IsEnteringVehicle && !guardPed.IsExitingVehicle)
                {
                    // Check if we need to return to our assigned position
                   float distanceTo = guardPed.Position.DistanceTo(guardPed.Position);
                    if (distanceTo > GetActiveReturnThreshold())
                    {
                        Logger.Log.Info($"Guard {AreaName} transitioning from Idle to Return state - distance to post: {distanceTo:F2}");
                        ChangeState(GuardState.Return);
                        Shift = GuardShiftType.OnDutyShift; 
                    }
                    else
                    {
                        Logger.Log.Info($"Guard {AreaName} transitioning from Idle to OnDuty - already at post");
                        ChangeState(GuardState.OnDuty);
                        ResumeNormalDuty();
                    }

                }
                break;

            case GuardState.OnDuty:
                // When on duty, check if guard has wandered too far
                float distanceToOriginal = guardPed.Position.DistanceTo(_originalPosition);
                
                if (ShouldReturnToPosition(distanceToOriginal))
                {
                    Logger.Log.Info($"Guard {AreaName} has wandered too far ({distanceToOriginal:F2} > {GetActiveReturnThreshold():F2}). Transitioning to Return.");
                    ChangeState(GuardState.Return);
                    break; // Exit case after state change
                }

                // Only call ResumeNormalDuty if not currently performing a task sequence
                // (e.g., the turn + scenario sequence from arrival)
                if (guardPed.TaskSequenceProgress == -1)
                {
                    ResumeNormalDuty();
                }
                // Otherwise, let the sequence complete naturally
                    
                break;

            case GuardState.Return:
                // Issue return command and immediately transition to Returning
                ReturnToPosition();
                ChangeState(GuardState.Returning);
                break;

            case GuardState.Returning:
                float currentDistance = guardPed.Position.DistanceTo(_originalPosition);
                
                // Step 1: Check if arrived at position
                if (currentDistance <= 2f)
                {
                    // Arrived! Clear any movement tasks first to avoid conflicts
                    guardPed.Task.ClearAll();
                    
                    Logger.Log.Info($"Guard {AreaName} arrived at post ({currentDistance:F2}m). Starting turn and duty sequence.");
                    
                    // Create sequence: Turn → Resume Duty
                    TaskSequence arrivedSequence = new TaskSequence();
                    
                    // Turn to correct heading
                    arrivedSequence.AddTask.TurnTo(GetTargetPositionFromHeading(_originalPosition, _originalHeading), 2000);
                    
                    // Resume duty based on scenario type
                    if (States == ScenarioType.Guard)
                    {
                        arrivedSequence.AddTask.StartScenarioInPlace(_activeScenario, -1);
                    }
                    else if (States == ScenarioType.Patrol)
                    {
                        arrivedSequence.AddTask.StandStill(100); // Brief stand, then patrol in OnDuty
                    }
                    else
                    {
                        arrivedSequence.AddTask.StartScenarioInPlace(_activeScenario, -1);
                    }
                    
                    arrivedSequence.Close();
                    guardPed.Task.PerformSequence(arrivedSequence);
                    arrivedSequence.Dispose();
                    
                    // Transition to OnDuty state
                    ChangeState(GuardState.OnDuty);
                    _dutyTaskAssigned = true; // Mark task as assigned since sequence handles it
                    
                    // For patrol, we still need to call GuardCurrentPosition after the sequence
                    if (States == ScenarioType.Patrol)
                    {
                        _dutyTaskAssigned = false; // Allow ResumeNormalDuty to assign patrol task
                    }
                    
                    return;
                }
                
                // Step 2: When very close (2-4m), stop re-issuing movement commands to avoid jitter
                if (currentDistance > 2f && currentDistance <= 4f)
                {
                    // Let the current movement task complete naturally
                    // Don't spam new commands in this close range
                    return;
                }
                
                // Step 3: Dynamically update movement speed based on current distance (only when >4m)
                PedMoveBlendRatio currentSpeed = PedMoveBlendRatio.Walk;
                PedMoveBlendRatio desiredSpeed;
                
                if (currentDistance < 10f)
                {
                    desiredSpeed = PedMoveBlendRatio.Walk; // Walk when close
                }
                else if (currentDistance < 30f)
                {
                    desiredSpeed = PedMoveBlendRatio.Run; // Run at medium distance
                }
                else
                {
                    desiredSpeed = PedMoveBlendRatio.Sprint; // Sprint when far
                }
                
                // Get current task status
                var goToStatus = guardPed.GetScriptTaskStatus(ScriptTaskNameHash.FollowNavMeshToCoord);
                
                // Check if guard is moving
                if (guardPed.IsWalking)
                    currentSpeed = PedMoveBlendRatio.Walk;
                else if (guardPed.IsRunning)
                    currentSpeed = PedMoveBlendRatio.Run;
                else if (guardPed.IsSprinting)
                    currentSpeed = PedMoveBlendRatio.Sprint;
                
                // Update speed if needed (speed changed based on distance) - but only if task isn't performing or speed mismatch is significant
                if (goToStatus != ScriptTaskStatus.Performing || (currentSpeed != desiredSpeed && currentDistance > 10f))
                {
                    // Re-issue movement with updated speed
                    guardPed.Task.FollowNavMeshTo(_originalPosition, desiredSpeed, -1, 1f);
                    Logger.Log.Info($"Guard {AreaName} updating movement: {currentDistance:F2}m away, speed: {desiredSpeed}");
                }
                
                // Step 4: Check if guard is stuck (stopped and still far)
                if (!guardPed.IsWalking && !guardPed.IsRunning && !guardPed.IsSprinting && currentDistance > 5f)
                {
                    Logger.Log.Info($"Guard {AreaName} appears stuck at {currentDistance:F2}m. Re-issuing return command.");
                    ReturnToPosition();
                }

                break;

            case GuardState.PostCombat:
                // Guard is observing the area after combat
                // Check if observation time has elapsed
                TimeSpan observationTime = DateTime.Now - _postCombatStartTime;
                
                if (observationTime.TotalSeconds >= _postCombatObserveDuration)
                {
                    // Observation complete - now assess if we need to return to post
                    float distanceFromPost = guardPed.Position.DistanceTo(_originalPosition);
                    float returnThreshold = GetActiveReturnThreshold();
                    
                    Logger.Log.Info($"Guard {AreaName} finished {_postCombatObserveDuration}s observation. Distance from post: {distanceFromPost:F2}m, Threshold: {returnThreshold:F2}m");
                    
                    if (distanceFromPost > returnThreshold)
                    {
                        Logger.Log.Info($"Guard {AreaName} is too far from post. Transitioning to Return state.");
                        ChangeState(GuardState.Return);
                    }
                    else
                    {
                        Logger.Log.Info($"Guard {AreaName} is close to post. Resuming OnDuty.");
                        ChangeState(GuardState.OnDuty);
                        ResumeNormalDuty();
                    }
                }
                else
                {
                    // Still observing - let the guard stand naturally
                    // Check if they have any active combat-related tasks
                    var combatTaskStatus = guardPed.GetScriptTaskStatus(ScriptTaskNameHash.Combat);
                    
                    // If no combat tasks are active, make them stand and look around
                    if (combatTaskStatus != ScriptTaskStatus.Performing)
                    {
                        // Check if they're idle (no tasks)
                        if (guardPed.TaskSequenceProgress == -1)
                        {
                            // Make them stand still and look around (natural behavior)
                            guardPed.Task.StandStill(1000); // Stand for 1 second, then re-evaluate
                        }
                    }
                    
                    // Log observation progress every few frames
                    int remainingSeconds = _postCombatObserveDuration - (int)observationTime.TotalSeconds;
                    if (remainingSeconds > 0 && Game.GameTime % 60 == 0) // Log every ~1 second
                    {
                        Logger.Log.Info($"Guard {AreaName} observing area... {remainingSeconds}s remaining");
                    }
                }
                break;

            case GuardState.Greeting:
                // Handle greeting logic (you may already have this in HandleGreetingLogic())
                // Transition back to previous state when greeting is done
                // This would require tracking the previous state
                break;

            case GuardState.Arriving:
                // Handle new guard arrival logic
                // Guards in this state are traveling in a vehicle to their destination
                // This state is managed by the GuardSpawner's Arrival method
                // Don't transition to ExitVehicle here - let the Arrival method handle the coordination
                if (AssignedVehicle == null || 
                    AssignedVehicle.guardVehicle == null || 
                    !AssignedVehicle.guardVehicle.Exists())
                {
                    // Vehicle is null or destroyed - emergency transition to OnDuty
                    Logger.Log.Warning($"Guard {AreaName} in Arriving state but no valid vehicle, emergency transition to OnDuty");
                    ChangeState(GuardState.OnDuty);
                }
                // Note: Arrival coordination is handled by GuardSpawner.Arrival() method
                // Don't check for destination arrival here to avoid premature state transitions
                break;

            case GuardState.ExitVehicle:
                // Handle guards exiting vehicle after arrival
                if (AssignedVehicle != null && 
                    guardPed.IsInVehicle(AssignedVehicle.guardVehicle))
                {
                    // Still in vehicle - issue exit command if not already exiting
                    if (!guardPed.IsExitingVehicle)
                    {
                        Logger.Log.Info($"Guard {AreaName} exiting vehicle");
                        guardPed.Task.LeaveVehicle(AssignedVehicle.guardVehicle, true);
                    }
                }
                else
                {
                    // Successfully exited vehicle - move to assigned position
                    Logger.Log.Info($"Guard {AreaName} successfully exited vehicle, _originalPosition already set to {_originalPosition}");
                    
                    // Clear vehicle association now that we're out
                    if (AssignedVehicle != null)
                    {
                        AssignedVehicle.UnassignPed(this);
                        AssignedVehicle = null;
                    }
                    
                    // Check if we're already close to our assigned position
                    // Note: _originalPosition was already set during CreateArrivingGuards via UpdateAssignedPosition
                    float distanceToAssignedPos = guardPed.Position.DistanceTo(_originalPosition);
                    if (distanceToAssignedPos > 3f)
                    {
                        Logger.Log.Info($"Guard {AreaName} needs to move to post {distanceToAssignedPos:F1}m away");
                        ChangeState(GuardState.Return);
                    }
                    else
                    {
                        Logger.Log.Info($"Guard {AreaName} already at post, resuming duty");
                        ChangeState(GuardState.OnDuty);
                        ResumeNormalDuty();
                    }
                }
                break;

            case GuardState.Departing:
                // Handle guard departure logic
                // This state might be terminal or transition to Idle
                break;

            default:
                Logger.Log.Warning($"Guard {AreaName} in unhandled state: {CurrentState}. Defaulting to OnDuty.");
                ChangeState(GuardState.OnDuty);
                break;
        }
    }

    /// <summary>
    /// Determines if the guard should return to position based on distance and safety conditions
    /// </summary>
    private bool ShouldReturnToPosition(float distanceToOriginal)
    {
        float threshold = GetActiveReturnThreshold();
        return distanceToOriginal > threshold &&
               !guardPed.IsInCombat &&
               !guardPed.IsRagdoll &&
               !guardPed.IsInAir &&
               !guardPed.IsClimbing &&
               !guardPed.IsFalling &&
               guardPed.IsOnFoot;
    }

    /// <summary>
    /// Determines if the guard has reached their position (within threshold)
    /// </summary>
    private bool HasReachedPosition(float distanceToOriginal)
    {
        float threshold = GetActiveReturnThreshold();
        return distanceToOriginal <= threshold;
    }

    
    private void ReturnToPosition()
    {
        if (guardPed == null || !guardPed.Exists())
            return;

        try
        {
            float distance = guardPed.Position.DistanceTo(_originalPosition);
            
            // Determine movement speed dynamically based on current distance
            PedMoveBlendRatio moveSpeed;
            if (distance < 10f)
            {
                moveSpeed = PedMoveBlendRatio.Walk; // Walk when close
            }
            else if (distance < 30f)
            {
                moveSpeed = PedMoveBlendRatio.Run; // Run at medium distance
            }
            else
            {
                moveSpeed = PedMoveBlendRatio.Sprint; // Sprint when far
            }
            
            // Just issue the GoTo command with appropriate speed
            // The Returning state will handle dynamic updates and transitions
            guardPed.Task.FollowNavMeshTo(_originalPosition, moveSpeed, -1,.25f, FollowNavMeshFlags.AccurateWalkRunStart | FollowNavMeshFlags.AdvancedSlideToCoordAndAchieveHeadingAtEnd | FollowNavMeshFlags.Default, _originalHeading);
            
            Logger.Log.Info($"Guard {AreaName} going to post ({distance:F2}m away, speed: {moveSpeed})");
        }
        catch (Exception ex)
        {
            Logger.Log.Fatal($"Error in ReturnToPosition: {ex.Message}");
        }
    }

    private Vector3 GetTargetPositionFromHeading(Vector3 guardPosition, float heading, float distance = 2.0f)
    {
        // Convert heading to radians
        float headingRadians = heading * (float)(Math.PI / 180.0f);

        // Calculate the target position in front of the guard
        float targetX = guardPosition.X + (float)(Math.Sin(headingRadians) * distance);
        float targetY = guardPosition.Y + (float)(Math.Cos(headingRadians) * distance);
        float targetZ = guardPosition.Z; // Keep same height

        return new Vector3(targetX, targetY, targetZ);
    }    
    
    public void ResumeNormalDuty()
    {
        if (guardPed == null || !guardPed.Exists())
            return;

        try
        {
            // Check if heading is aligned (within 10 degrees tolerance)
            float headingDifference = Math.Abs(guardPed.Heading - _originalHeading);
            // Handle wrap-around (e.g., 359° vs 1° should be 2° difference, not 358°)
            if (headingDifference > 180f)
                headingDifference = 360f - headingDifference;
            
            const float HEADING_TOLERANCE = 10f;
            
            // If heading is NOT aligned, adjust it first
            if (headingDifference > HEADING_TOLERANCE)
            { 
                // Reset duty task flag since we're re-aligning
                _dutyTaskAssigned = false;
                
                // Check if guard is already turning
                if (guardPed.GetScriptTaskStatus(ScriptTaskNameHash.TurnPedToFaceCoord) != ScriptTaskStatus.Performing)
                {
                    guardPed.Task.TurnTo(GetTargetPositionFromHeading(_originalPosition, _originalHeading), -1);
                    Logger.Log.Info($"Guard {AreaName} adjusting heading: current={guardPed.Heading:F1}°, target={_originalHeading:F1}°, diff={headingDifference:F1}°");
                }
                // Still turning, wait until aligned
                return;
            }
            
            // Heading is now aligned! But check if we've already assigned the duty task
            if (_dutyTaskAssigned)
            {
                // Task already assigned, don't reassign
                return;
            }
            
            // Heading is aligned and task not yet assigned - give the duty tasks
            Logger.Log.Info($"Guard {AreaName} heading aligned ({guardPed.Heading:F1}° ≈ {_originalHeading:F1}°), assigning duty tasks");
            
            // Now assign the appropriate duty task - the heading is aligned!
            if (States == ScenarioType.Guard)
            {
                // StandGuard should maintain the heading we just set
                guardPed.Task.StartScenarioInPlace(_activeScenario);
                Logger.Log.Info($"Guard {AreaName} standing guard at heading {_originalHeading:F1}");
            }
            else if (States == ScenarioType.Patrol)
            {
                guardPed.GuardCurrentPosition(_random.Next(2) == 0);
                Logger.Log.Info($"Guard {AreaName} patrolling current position");
            }
            else
            {
                guardPed.Task.StartScenarioInPlace(_activeScenario);
                Logger.Log.Info($"Guard {AreaName} starting scenario {_activeScenario}");
            }

            // Mark that duty task has been assigned
            _dutyTaskAssigned = true;
            
            Logger.Log.Info($"Guard {AreaName} resumed normal duty ({States}) facing heading {_originalHeading:F1}°");
        }
        catch (Exception ex)
        {
            Logger.Log.Fatal($"Error in ResumeNormalDuty: {ex.Message}");
        }
    }


    public void Spawn()
    {
        Logger.Log.Info($"Spawning guard ped at position {Position}, heading {Heading}, area {AreaName}");

        guardPed = SpawnGuard(PedModelName, Position);
        if (guardPed == null) return;

        if (States == ScenarioType.Guard) //stand always
            guardPed.Task.StartScenarioInPlace(_activeScenario);
        else if (States == ScenarioType.Patrol) //move here and there no animation played
            guardPed.GuardCurrentPosition(_random.Next(2) == 0);
        else
            guardPed.Task.StartScenarioInPlace(_activeScenario);

        // Mark that initial duty task has been assigned
        _dutyTaskAssigned = true;

        SetupRelationships();
        CreateBlip();
    }

    public void Despawn()
    {
        Logger.Log.Info($"Despawning guard ped at position {Position}");

        RemoveBlip();

        if (guardPed != null && guardPed.Exists())
        {
            guardPed.MarkAsNoLongerNeeded();
        }
    }

    // Create blip for guard
    public void CreateBlip()
    {
        // Check if blips are enabled in INI and if this area respects the player
        if (!PlayerPositionLogger.GetEnableBlips() || !DoesAreaRespectPlayer() || guardPed == null || !guardPed.Exists())
            return;

        try
        {
            GuardBlip = guardPed.AddBlip();
            if (GuardBlip != null)
            {
                GuardBlip.Sprite = BlipSprite.Enemy;
                GuardBlip.Scale = 0.7f;
                GuardBlip.Name = $"Guard - {AreaName}";

                // Color based on character respect: Franklin=Green, Michael=Blue, Trevor=Orange
                GuardBlip.Color = GetBlipColorForArea();
            }
        }
        catch (Exception ex)
        {
            Logger.Log.Warning($"Failed to create blip for guard: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Check if this area respects the current player character
    /// </summary>
    private bool DoesAreaRespectPlayer()
    {
        if (Area == null || string.IsNullOrEmpty(Area.Respect))
            return false;

        string respect = Area.Respect.ToUpperInvariant();
        
        // Check for universal respect
        if (respect == "YES" || respect == "ANY" || respect == "ALL")
            return true;

        // Get current player character
        GTA.Ped player = GTA.Game.Player.Character;
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
    /// Get blip color based on which character the area respects
    /// Franklin = Green, Michael = Blue, Trevor = Orange
    /// </summary>
    private BlipColor GetBlipColorForArea()
    {
        if (string.IsNullOrEmpty(Area.Respect))
            return BlipColor.White; // Default neutral color

        string respect = Area.Respect.ToUpperInvariant();

        // Check for Franklin (Green)
        if (respect.Contains("FRANKLIN") && Game.Player.Character.Model == PedHash.Franklin)
            return BlipColor.Green;

        // Check for Michael (Blue)  
        if (respect.Contains("MICHAEL") && Game.Player.Character.Model == PedHash.Michael)
            return BlipColor.Blue;

        // Check for Trevor (Orange)
        if (respect.Contains("TREVOR") && Game.Player.Character.Model == PedHash.Trevor)
            return BlipColor.Orange;

        // Default for other cases
        return BlipColor.White;
    }

    // Update blip color based on guard state
    private void UpdateBlipColor()
    {
        if (GuardBlip == null || !GuardBlip.Exists())
            return;

        // Keep character-based color (Franklin=Green, Michael=Blue, Trevor=Orange)
        // But change sprite based on state to show activity
        switch (CurrentState)
        {
            case GuardState.OnDuty:
                GuardBlip.Sprite = BlipSprite.Friend; // Standing guard
                GuardBlip.Scale = 0.7f;
                break;
            case GuardState.InCombat:
                GuardBlip.Sprite = BlipSprite.Enemy; // In combat
                GuardBlip.Scale = 0.9f; // Larger to highlight combat
                break;
            case GuardState.Departing:
                GuardBlip.Sprite = BlipSprite.GetawayCar; // Leaving
                GuardBlip.Scale = 0.7f;
                break;
            case GuardState.Arriving:
                GuardBlip.Sprite = BlipSprite.PickupSpawn; // Arriving
                GuardBlip.Scale = 0.7f;
                break;
            case GuardState.ExitVehicle:
                GuardBlip.Sprite = BlipSprite.PickupSpawn; // Deploying
                GuardBlip.Scale = 0.7f;
                break;
            case GuardState.Returning:
                GuardBlip.Sprite = BlipSprite.Waypoint; // Returning to post
                GuardBlip.Scale = 0.7f;
                break;
            default:
                GuardBlip.Sprite = BlipSprite.Standard; // Default
                GuardBlip.Scale = 0.7f;
                break;
        }
        
        // Color stays character-based (set during CreateBlip)
        // This allows players to identify which faction guards belong to
    }

    // Remove blip
    private void RemoveBlip()
    {
        if (GuardBlip != null && GuardBlip.Exists())
        {
            GuardBlip.Delete();
            GuardBlip = null;
        }
    }
}