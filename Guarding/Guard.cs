using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public class Guard
{
    private string _activeScenario;
    public Vector3 Position { get; set; }
    public float Heading { get; set; }
    public string AreaName { get; set; }
    public string Type { get; set; }
    public bool Interior { get; set; }
    private readonly Area Area;
    public Ped guardPed;
    public Ped guardPedOnVehicle;
    public Vehicle guardVehicle;
    public RelationshipGroup GuardGroup { get; set; }
    private Vector3 _originalPosition;
    private readonly float _originalHeading;
    private const float RETURN_THRESHOLD = 2f;
    private const int COMBAT_CHECK_DELAY = 1000;
    private const float GUARD_RETURN_DISTANCE_THRESHOLD = 30f;
    private string VehicleModelName;
    private string MVehicleModelName;
    private string PVehicleModelName;
    private string HVehicleModelName;
    private string LVehicleModelName;
    private string BVehicleModelName;
    private string PedModelName;
    private string WeaponName;
    private readonly GuardConfig GuardConfig;
    private static readonly Random _random = new Random(); // Make it static


    public Guard(GuardSpawnPoint point, GuardConfig guardConfig, Area area)
    {
        Position = point.Position;
        Heading = point.Heading;
        AreaName = area.Name ?? throw new ArgumentNullException(nameof(area.Name));
        Type = point.Type ?? throw new ArgumentNullException(nameof(point.Type));
        GuardConfig = guardConfig;
        Area = area;
        _originalPosition = point.Position;
        _originalHeading = point.Heading;
        GuardGroup = GuardConfig.RelationshipGroup;
        Interior = point.Interior;

        // If no scenario was provided, default it using the Area's default scenario.
        if (string.IsNullOrEmpty(point.Scenario))
        {
            InitializeScenario(area.Scenarios.Name);
        }
        else
            _activeScenario = point.Scenario;

        RandomizeLoadout();
    }

    private ScenarioType States = ScenarioType.Vehicle; // Initial value

    private ScenarioType GetScenarioTypeFromName(string scenarioName)
    {
        return scenarioName.ToLower() switch
        {
            "guards" => ScenarioType.Guard,
            "patrol" => ScenarioType.Patrol,
            "ambient" => ScenarioType.Ambient,
            "random" => ScenarioType.Random,
            _ => ScenarioType.Vehicle,
        };
    }

    private ScenarioType DetermineScenarioType(string scenario)
    {
        if (string.IsNullOrEmpty(scenario))
            return ScenarioType.Random;

        string scenarioLower = scenario.ToLower();

        // First check exact matches in Area.Scenarios
        if (Area?.Scenarios?.Name != null &&
            Area.Scenarios.Name.Equals(scenario, StringComparison.OrdinalIgnoreCase))
        {
            return GetScenarioTypeFromName(Area.Scenarios.Name);
        }

        // Then check contains
        return scenarioLower switch
        {
            var s when s.Contains("guard") => ScenarioType.Guard,
            var s when s.Contains("patrol") => ScenarioType.Patrol,
            var s when s is "random" or "anything" => ScenarioType.Random,
            var s when s is "none" or "false" => ScenarioType.Vehicle,
            _ => ScenarioType.Ambient
        };
    }

    private string GetRandomScenario(ScenarioType scenarioType)
    {
        string scenarioKey = scenarioType switch
        {
            ScenarioType.Guard => "guard",
            ScenarioType.Patrol => "patrol",
            ScenarioType.Ambient => "ambient",
            ScenarioType.Random => "random",
            ScenarioType.Vehicle => "vehicle",
            _ => "random"
        };

        if (GuardSpawner.scans.TryGetValue(scenarioKey, out Scenarios scenarioData))
        {
            if (scenarioData != null && scenarioData.ScenarioList != null && scenarioData.ScenarioList.Any())
            {
                return GetRandomElement(scenarioData.ScenarioList);
            }
        }

        Logger.Log($"No valid scenario found for type {scenarioType}. Using default scenario.");
        return "WORLD_HUMAN_GUARD_STAND"; // Default fallback scenario
    }

    private void InitializeScenario(string scenario)
    {
        try
        {
            // Determine state based on scenario name from ScenarioLists.xml
            if (Area?.Scenarios != null)
            {
                // Set state based on scenario name
                States = DetermineScenarioType(scenario);

                // Use override animation or random scenario from list
                _activeScenario = GetRandomScenario(States);
            }
            else
            {
                // Fallback if no scenarios available
                States = ScenarioType.Ambient;
                _activeScenario = GetRandomScenario(States);
            }

            if (string.IsNullOrEmpty(_activeScenario))
            {
                _activeScenario = "WORLD_HUMAN_GUARD_STAND";
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error in InitializeScenario: {ex.Message}");
            States = ScenarioType.Guard;
            _activeScenario = "WORLD_HUMAN_GUARD_STAND";
        }
    }

    private void RandomizeLoadout()
    {
        PedModelName = GetRandomElementOrDefault(GuardConfig.PedModels, "PedModels");
        MVehicleModelName = GetRandomElementOrDefault(GuardConfig.MVehicleModels, "Mounted Vehicle Models");
        PVehicleModelName = GetRandomElementOrDefault(GuardConfig.PVehicleModels, "Plane Models");
        BVehicleModelName = GetRandomElementOrDefault(GuardConfig.BVehicleModels, "Boat Models");
        LVehicleModelName = GetRandomElementOrDefault(GuardConfig.LVehicleModels, "Large Vehicle Models");
        HVehicleModelName = GetRandomElementOrDefault(GuardConfig.HVehicleModels, "Helicopter Models");
        WeaponName = GetRandomElementOrDefault(GuardConfig.Weapons, "Weapons");
        VehicleModelName = GetRandomElementOrDefault(GuardConfig.VehicleModels, "Vehicle Models");
    }

    private string GetRandomElementOrDefault(List<string> list, string logContext)
    {
        if (list != null && list.Any())
        {
            return GetRandomElement(list);
        }
        Logger.Log($"Warning: No valid {logContext} found for GuardConfig '{GuardConfig.Name}'. Random selection skipped.");
        return null;
    }

    private T GetRandomElement<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("List cannot be null or empty");
        return list[_random.Next(list.Count)];
    }

    public void UpdateCombatState()
    {
        if (guardPed == null || !guardPed.Exists() || guardPed.IsDead)
            return;

        HandleGreetingLogic();
        if (!guardPed.IsInCombat)
        {
            Script.Wait(COMBAT_CHECK_DELAY);

            if (guardPed.IsIdle && !guardPed.IsRagdoll && !guardPed.IsInAir &&
                !guardPed.IsClimbing && !guardPed.IsFalling && !guardPed.IsShooting)
            {
                ReturnGuardToPosition();
            }
        }
    }

    private void ReturnGuardToPosition()
    {
        if (guardPed == null || !guardPed.Exists() || guardPed.IsDead)
            return;

        float distanceToOriginal = guardPed.Position.DistanceTo(_originalPosition);

        if (States == ScenarioType.Patrol && distanceToOriginal >= GUARD_RETURN_DISTANCE_THRESHOLD)
        {
            MoveGuardToPosition();
        }
        else if (States == ScenarioType.Guard && distanceToOriginal >= RETURN_THRESHOLD)
        {
            MoveGuardToPosition();
        }

        if (distanceToOriginal < (States == ScenarioType.Guard ? GUARD_RETURN_DISTANCE_THRESHOLD : RETURN_THRESHOLD) &&
            !guardPed.IsShooting && !guardPed.IsInCombat)
        {
            guardPed.Heading = _originalHeading;

            // Decide what behavior to use
            if (States == ScenarioType.Guard)
            {
                if (guardPed.GetScriptTaskStatus(ScriptTaskNameHash.StandGuard) != ScriptTaskStatus.Performing)
                {
                    guardPed.StandGuard(Position, Heading, _activeScenario);
                }
            }
            else if (States == ScenarioType.Patrol)
            {
                if (guardPed.GetScriptTaskStatus(ScriptTaskNameHash.GuardCurrentPosition) != ScriptTaskStatus.Performing)
                {
                    guardPed.GuardCurrentPosition(_random.Next(2) == 0); // Random defensive status
                }
            }
            else
            {
                if (guardPed.GetScriptTaskStatus(ScriptTaskNameHash.StartScenarioInPlace) != ScriptTaskStatus.Performing)
                {
                    guardPed.Task.StartScenarioInPlace(_activeScenario);
                }
            }
        }
    }

    private void MoveGuardToPosition()
    {
        if (guardPed.IsIdle && guardPed.IsAlive && !guardPed.IsRagdoll &&
            !guardPed.IsInAir && !guardPed.IsClimbing && !guardPed.IsFalling && guardPed.IsOnFoot && !guardPed.IsWalking)
        {
            guardPed.Task.FollowNavMeshTo(_originalPosition, (PedMoveBlendRatio)_random.Next(0, 3));
        }
    }

    private bool hasPlayedAnimation = false;
    private readonly float triggerDistance = 10f;
    private readonly float resetDistance = 25f;

    private void HandleGreetingLogic()
    {
        if (guardPed == null || !guardPed.Exists()) return;

        // Check if guard is a companion
        if (!IsGuardCompanion()) return;

        float distance = guardPed.Position.DistanceTo(Game.Player.Character.Position);

        if (distance <= triggerDistance && !hasPlayedAnimation)
        {
            PlayGuardAnimation();
            Script.Wait(500); // Small delay before player's response
            PlayPlayerResponse();
            hasPlayedAnimation = true;
        }
        else if (distance > resetDistance)
        {
            hasPlayedAnimation = false;
        }
    }

    private bool IsGuardCompanion()
    {
        int guardRel = Function.Call<int>(Hash.GET_RELATIONSHIP_BETWEEN_GROUPS,
                                           guardPed.RelationshipGroup,
                                           Game.Player.Character.RelationshipGroup);
        return guardRel == 0; // 0 = Companion
    }

    private void PlayGuardAnimation()
    {
        string[] guardSpeeches = { "GENERIC_HI", "GENERIC_BYE", "GENERIC_HOWS_IT_GOING", "GENERIC_THANKS" };
        string guardSpeech = guardSpeeches[_random.Next(guardSpeeches.Length)];

        Function.Call(Hash.TASK_PLAY_ANIM, guardPed,
                      "gestures@m@standing@casual", "gesture_hello",
                      1.0f, -1.0f, 4000, AnimationFlags.UpperBodyOnly,
                      0, false, 0, false);

        Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE, guardPed, guardSpeech, "SPEECH_PARAMS_FORCE");
    }

    private void PlayPlayerResponse()
    {
        Ped player = Game.Player.Character;
        if (player == null || !player.Exists()) return;

        string[] playerSpeeches;
        if (player.Model == PedHash.Michael || player.Model == PedHash.Franklin || player.Model == PedHash.Trevor)
        {
            playerSpeeches = new string[] { "GENERIC_HI", "GENERIC_BYE", "GENERIC_THANKS" };
        }
        else
        {
            playerSpeeches = new string[] { "GENERIC_HI", "GENERIC_BYE", "GENERIC_HOWS_IT_GOING", "GENERIC_THANKS" };
        }

        string playerSpeech = playerSpeeches[_random.Next(playerSpeeches.Length)];

        // Random animation selection
        string[] playerAnims = { "gesture_hello", "mp_player_int_salute", "mp_player_int_uppersalute" };
        string playerAnim = playerAnims[_random.Next(playerAnims.Length)];
        string playerAnimDict = (playerAnim == "gesture_hello")
            ? "gestures@m@standing@casual"
            : "mp_player_intsalute";

        Function.Call(Hash.TASK_PLAY_ANIM, player,
                      playerAnimDict, playerAnim,
                      1.0f, -1.0f, 4000, AnimationFlags.UpperBodyOnly,
                      0, false, 0, false);

        Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE, player, playerSpeech, "SPEECH_PARAMS_FORCE");
    }

    private Ped SpawnGuard(string modelName, Vector3 position)
    {
        Model mdl = new Model(modelName);
        if (!mdl.IsInCdImage)
        {
            Logger.Log($"Model {mdl} is not in CD Image. Area name: {AreaName} and Guard Model: {GuardConfig.Name}.");
            HelperClass.Subtitle($"Model: {mdl} not found.");
            return null;
        }
        mdl.Request(500);
        Ped spawnedPed = World.CreatePed(mdl, Position);
        mdl.MarkAsNoLongerNeeded();

        if (spawnedPed == null)
        {
            Logger.Log($"Failed to create guard ped with model {modelName}.");
            return null;
        }

        InitializePed(spawnedPed);
        return spawnedPed;
    }


    private void InitializePed(Ped ped, bool isGunner = false)
    {
        ped.Heading = Heading;
        ped.Weapons.Give(WeaponName, 800, true, true);
        ped.Armor = 200;
        ped.MaxHealth = 300;
        ped.Health = 300;
        ped.DrivingAggressiveness = 1f;
        ped.IsCollisionEnabled = true;
        ped.DiesOnLowHealth = false;
        ped.RelationshipGroup = GuardGroup;
        Function.Call(Hash.SET_PED_RANDOM_PROPS, ped);

        if (!isGunner)
        {
            ped.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
            ped.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, true);
            ped.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, true);
            ped.SetCombatAttribute(CombatAttributes.CanUseCover, true);
            ped.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
            ped.SetCombatAttribute(CombatAttributes.AlwaysFlee, false);
            ped.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, true);
            ped.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);
            ped.SetCombatAttribute(CombatAttributes.DisableBulletReactions, true);

            ped.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, true);
            ped.SetConfigFlag(PedConfigFlagToggles.CanDiveAwayFromApproachingVehicles, true);
            ped.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, true);
            ped.SetConfigFlag(PedConfigFlagToggles.AIDriverAllowFriendlyPassengerSeatEntry, true);
            ped.SetConfigFlag(PedConfigFlagToggles.KeepRelationshipGroupAfterCleanUp, true);
            ped.SetConfigFlag(PedConfigFlagToggles.AllowMedicsToReviveMe, true);
        }
        else
        {
            ped.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, false);
            ped.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true);
            ped.SetCombatAttribute(CombatAttributes.UseVehicleAttackIfVehicleHasMountedGuns, true);
            ped.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
            ped.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
            ped.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);
            ped.SetConfigFlag(PedConfigFlagToggles.KeepRelationshipGroupAfterCleanUp, true);
        }
        ped.PopulationType = EntityPopulationType.RandomPatrol;

        if (!Interior)
        {
            OutputArgument groundZArg = new OutputArgument();
            Function.Call(Hash.GET_GROUND_Z_FOR_3D_COORD, Position.X, Position.Y, Position.Z + 5, groundZArg, false, false);
            ped.Position = new Vector3(Position.X, Position.Y, groundZArg.GetResult<float>());
        }

        if (ped.PedType == PedType.Cop || ped.PedType == PedType.Swat || ped.PedType == PedType.Army)
        {
            ped.SetConfigFlag(PedConfigFlagToggles.CanAttackNonWantedPlayerAsLaw, false);
            ped.SetConfigFlag(PedConfigFlagToggles.DontAttackPlayerWithoutWantedLevel, true);
            ped.TargetLossResponse = TargetLossResponse.SearchForTarget;
        }
    }

    public void Spawn()
    {
        Logger.Log($"Spawning guard at position {Position}, heading {Heading}, area {AreaName}, type {Type}");

        switch (Type.ToLower())
        {
            case "ped":
                guardPed = SpawnGuard(PedModelName, Position);
                if (guardPed == null) return;

                if (States == ScenarioType.Guard)
                {
                    guardPed.StandGuard(Position, Heading, _activeScenario);
                }
                else if (States == ScenarioType.Patrol)
                {
                    guardPed.GuardCurrentPosition(_random.Next(2) == 0);
                }
                else
                {
                    guardPed.Task.StartScenarioInPlace(_activeScenario);
                }
                SetupRelationships();
                break;

            case "vehicle":
            case "largevehicle":
            case "helicopter":
            case "plane":
            case "boat":
                VehicleModelName = GetVehicleModelName(Type);
                guardVehicle = CreateVehicle(VehicleModelName);
                if (guardVehicle == null) return;
                break;

            case "mounted":
                guardVehicle = CreateVehicle(MVehicleModelName);
                if (guardVehicle == null) return;
                AssignPedToVehicle();
                SetupRelationships(true);
                break;

            default:
                Logger.Log($"Unknown guard type: {Type}");
                break;
        }
    }

    private string GetVehicleModelName(string type)
    {
        return type switch
        {
            "vehicle" => VehicleModelName,
            "largevehicle" => LVehicleModelName,
            "helicopter" => HVehicleModelName,
            "plane" => PVehicleModelName,
            "boat" => BVehicleModelName,
            _ => throw new ArgumentException($"Unknown vehicle type: {type}")
        };
    }

    private Vehicle CreateVehicle(string modelName)
    {
        Vehicle vehicle = World.CreateVehicle(modelName, Position);
        if (vehicle == null)
        {
            Logger.Log($"Failed to create guard vehicle with model {modelName}.");
            return null;
        }
        vehicle.Heading = Heading;
        vehicle.LockStatus = VehicleLockStatus.CanBeBrokenInto;
        return vehicle;
    }

    private void AssignPedToVehicle()
    {
        for (int i = -1; i < guardVehicle.PassengerCapacity + 1; i++)
        {
            if (guardVehicle.IsSeatFree((VehicleSeat)i) && guardVehicle.IsTurretSeat((VehicleSeat)i))
            {
                guardPedOnVehicle = guardVehicle.CreatePedOnSeat((VehicleSeat)i, PedModelName);
                InitializePed(guardPedOnVehicle, isGunner: true);
                guardPedOnVehicle.FiringPattern = FiringPattern.FullAuto;
                guardPedOnVehicle.SetConfigFlag(PedConfigFlagToggles.CreatedByFactory, true);
                break;
            }
        }
    }

    public void Despawn()
    {
        Logger.Log($"Despawning guard at position {Position}, type {Type}");

        if (Type == "ped" && guardPed != null && guardPed.Exists())
        {
            guardPed.MarkAsNoLongerNeeded();
        }
        else if ((Type == "vehicle" || Type == "helicopter" || Type == "boat") && guardVehicle != null && guardVehicle.Exists())
        {
            guardVehicle.MarkAsNoLongerNeeded();
        }
        else if (Type == "mounted")
        {
            if (guardPedOnVehicle != null && guardPedOnVehicle.Exists())
            {
                guardPedOnVehicle.MarkAsNoLongerNeeded();
            }
            if (guardVehicle != null && guardVehicle.Exists())
            {
                guardVehicle.MarkAsNoLongerNeeded();
            }
        }
        else
        {
            Logger.Log($"Failed to despawn guard at position {Position}. Unknown Type or Type not handled: {Type}");
        }
    }

    private void SetupRelationships(bool gunner = false)
    {
        Ped pedToConfigure = gunner ? guardPedOnVehicle : guardPed;
        if (pedToConfigure == null)
        {
            Logger.Log($"Warning: SetupRelationships called for {(gunner ? "gunner" : "ped")}, but it is null.");
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
                GetHash("GUARD_DOG")
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

            pedToConfigure.RelationshipGroup = World.AddRelationshipGroup(GuardConfig.RelationshipGroup);
            pedToConfigure.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigure.RelationshipGroup, Relationship.Companion, true);

            if (pedToConfigure.PedType == PedType.Cop || pedToConfigure.PedType == PedType.Swat || pedToConfigure.PedType == PedType.Army)
            {
                switch (pedToConfigure.PedType)
                {
                    case PedType.Army:
                        Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, pedToConfigure.Handle, GetHash("ARMY"));
                        break;
                    case PedType.Cop:
                    case PedType.Swat:
                        Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, pedToConfigure.Handle, GetHash("COP"));
                        break;
                }
            }

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
        }
        catch (Exception ex)
        {
            Logger.Log($"Error in SetupRelationships: {ex.Message} StackTrace: {ex.StackTrace}");
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
}
