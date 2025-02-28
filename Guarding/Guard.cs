using GTA;
using GTA.Math;
using GTA.Native;
using GTA.NaturalMotion;
using GTA.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Windows.Forms;


public class Guard
{
    private readonly string _configuredScenario; // Store the original scenario from config
    private string _activeScenario; // The currently active scenario
    private bool _isGuardScenario; // Flag to track if using guard-specific behavior

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

    // New fields for combat tracking
    private Vector3 _originalPosition;
    private readonly float _originalHeading;
    private const float RETURN_THRESHOLD = 2f; // Distance threshold for considering ped "returned"
    private const int COMBAT_CHECK_DELAY = 1000; // Time to wait before checking if combat is truly over
    private const float GUARD_RETURN_DISTANCE_THRESHOLD = 30f; // Minimum distance for guard scenario to return

    private string VehicleModelName;
    private string MVehicleModelName;
    private string PVehicleModelName;
    private string HVehicleModelName;
    private string LVehicleModelName;
    private string BVehicleModelName;
    private string PedModelName;
    private string WeaponName;
    private readonly GuardConfig GuardConfig;
    private string Scenario; // with value the original
    private string randomScenario;

    private bool guardInScene = false;


    public Guard(Vector3 position, float heading, string areaName, string type, GuardConfig guardConfig, string scenario, Area area, bool interior)
    {
        Position = position;
        Heading = heading;
        AreaName = areaName ?? throw new ArgumentNullException(nameof(areaName));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        GuardConfig = guardConfig;
        Area = area;
        _configuredScenario = scenario; // Store original scenario
        Interior = interior;

        // Initialize combat-related fields
        _originalPosition = position;
        _originalHeading = heading;

        GuardGroup = GuardConfig.RelationshipGroup;

        RandomizeLoadout();
        InitializeScenario();
    }

    private void InitializeScenario()
    {
        // If no scenario is defined in XML (null or empty), use random scenario
        if (string.IsNullOrEmpty(_configuredScenario))
        {
            _isGuardScenario = false;
            _activeScenario = GetRandomElement(GuardManager.scenarios);
            Logger.Log($"No scenario configured, using random scenario: {_activeScenario}");
            return;
        }

        // Convert to lowercase for comparison
        string scenarioLower = _configuredScenario.ToLower();

        // Handle explicit guard behaviors
        if (scenarioLower == "guard" ||
            scenarioLower == "guardpatrol" ||
            scenarioLower == "taskguard" ||
            scenarioLower == "task_guard")
        {
            _isGuardScenario = true;
            _activeScenario = null;
            Logger.Log("Using guard behavior as specified");
        }
        // Handle explicit none/false cases
        else if (scenarioLower == "none" || scenarioLower == "false")
        {
            _isGuardScenario = false;
            _activeScenario = scenarioLower == "random" ?
                GetRandomElement(GuardManager.scenarios) :
                _configuredScenario;
            Logger.Log("Scenario disabled, using guard behavior");
        }
        // For all other cases, including "random"
        else
        {
            _isGuardScenario = false;
            _activeScenario = scenarioLower == "random" ?
                GetRandomElement(GuardManager.scenarios) :
                _configuredScenario;
            Logger.Log($"Using scenario: {_activeScenario}");
        }
    }

    // Define relationship group hashes (unchanged)
    private static readonly int PrivateSecurityHash = (int)StringHash.AtStringHash("PRIVATE_SECURITY");
    private static readonly int SecurityGuardHash = (int)StringHash.AtStringHash("SECURITY_GUARD");
    private static readonly int ArmyHash = (int)StringHash.AtStringHash("ARMY");
    private static readonly int CopHash = (int)StringHash.AtStringHash("COP");
    private static readonly int GuardDogHash = (int)StringHash.AtStringHash("GUARD_DOG");
    private static readonly int MerryweatherHash = (int)StringHash.AtStringHash("MERRYWEATHER");

    private static readonly Random rand = new Random(); // Single Random instance (unchanged)


    private void RandomizeLoadout()
    {
        if (GuardConfig.PedModels != null && GuardConfig.PedModels.Any())
        {
            PedModelName = GetRandomElement(GuardConfig.PedModels);
        }
        else
        {
            Logger.Log($"Warning: No valid PedModels found for GuardConfig '{GuardConfig.Name}'. Random PedModel selection skipped.");
            PedModelName = null; // Or you can set a specific default here if you always want a fallback PedModel
        }

        if (GuardConfig.MVehicleModels != null && GuardConfig.MVehicleModels.Any())
        {
            MVehicleModelName = GetRandomElement(GuardConfig.MVehicleModels);
        }
        else
        {
            Logger.Log($"Warning: No valid Mounted Vehicle Models found for GuardConfig '{GuardConfig.Name}'. Random Mounted Vehicle Model selection skipped.");
            MVehicleModelName = null;
        }

        if (GuardConfig.PVehicleModels != null && GuardConfig.PVehicleModels.Any())
        {
            PVehicleModelName = GetRandomElement(GuardConfig.PVehicleModels);
        }
        else
        {
            Logger.Log($"Warning: No valid Plane Models found for GuardConfig '{GuardConfig.Name}'. Random Plane Model selection skipped.");
            PVehicleModelName = null;
        }

        if (GuardConfig.BVehicleModels != null && GuardConfig.BVehicleModels.Any())
        {
            BVehicleModelName = GetRandomElement(GuardConfig.BVehicleModels);
        }
        else
        {
            Logger.Log($"Warning: No valid Boat Models found for GuardConfig '{GuardConfig.Name}'. Random Boat Model selection skipped.");
            BVehicleModelName = null;
        }

        if (GuardConfig.LVehicleModels != null && GuardConfig.LVehicleModels.Any())
        {
            LVehicleModelName = GetRandomElement(GuardConfig.LVehicleModels);
        }
        else
        {
            Logger.Log($"Warning: No valid Large Vehicle Models found for GuardConfig '{GuardConfig.Name}'. Random Large Vehicle Model selection skipped.");
            LVehicleModelName = null;
        }

        if (GuardConfig.HVehicleModels != null && GuardConfig.HVehicleModels.Any())
        {
            HVehicleModelName = GetRandomElement(GuardConfig.HVehicleModels);
        }
        else
        {
            Logger.Log($"Warning: No valid Helicopter Models found for GuardConfig '{GuardConfig.Name}'. Random Helicopter Model selection skipped.");
            HVehicleModelName = null;
        }

        if (GuardConfig.Weapons != null && GuardConfig.Weapons.Any())
        {
            WeaponName = GetRandomElement(GuardConfig.Weapons);
        }
        else
        {
            Logger.Log($"Warning: No valid Weapons found for GuardConfig '{GuardConfig.Name}'. Random Weapon selection skipped.");
            WeaponName = null; // Or you can set a default weapon like "WEAPON_PISTOL" if you always want a weapon
        }

        if (GuardConfig.VehicleModels != null && GuardConfig.VehicleModels.Any())
        {
            VehicleModelName = GetRandomElement(GuardConfig.VehicleModels);
        }
        else
        {
            Logger.Log($"Warning: No valid Vehicle Models found for GuardConfig '{GuardConfig.Name}'. Random Vehicle Model selection skipped.");
            VehicleModelName = null;
        }

        if (GuardManager.scenarios != null && GuardManager.scenarios.Any() && Scenario != null) // Check GuardManager.scenarios and Scenario too
        {
            randomScenario = GetRandomElement(GuardManager.scenarios);
        }
        else
        {
            Logger.Log($"Warning: No valid Scenarios available or scenarios list is invalid. Random Scenario selection skipped.");
            randomScenario = null;
        }
    }

    private static T GetRandomElement<T>(List<T> list) // (unchanged)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("List cannot be null or empty");
        return list[rand.Next(list.Count)];
    }

    public void UpdateCombatState() // (unchanged)
    {
        if (guardPed == null || !guardPed.Exists() || guardPed.IsDead)
            return;

        bool isCurrentlyInCombat = guardPed.IsInCombat;
        bool isIdle = guardPed.IsIdle && !guardPed.IsRagdoll && !guardPed.IsInAir &&
                            !guardPed.IsClimbing && !guardPed.IsFalling;

        bool isMountedGunner = guardPedOnVehicle != null && guardPedOnVehicle.Exists(); // Check if mounted gunner exists


        // Handle post-combat behavior
        if (!isCurrentlyInCombat)
        {
            Script.Wait(COMBAT_CHECK_DELAY); // Ensure combat is truly over

            if (isIdle)
            {
                // Check distance for guard scenario types
                if (_isGuardScenario)
                {
                    if (guardPed.Position.DistanceTo(_originalPosition) >= GUARD_RETURN_DISTANCE_THRESHOLD)
                    {
                        guardInScene = false; // Reset guardInScene flag if far enough
                        ReturnGuardToPosition(); // Trigger return logic only if far away
                    }
                    // If it's a guard scenario but within the threshold, do nothing - guard stays in the area.
                }
                else
                {
                    // For non-guard scenarios, use the original return logic
                    if (guardPed.Position.DistanceTo(_originalPosition) >= RETURN_THRESHOLD)
                    {
                        guardInScene = false;
                    }
                    ReturnGuardToPosition();
                }


            }
        }
    }

    private void ReturnGuardToPosition()
    {
        if (guardPed == null || !guardPed.Exists() || guardPed.IsDead)
            return;

        if (!_isGuardScenario && guardPed.Position.DistanceTo(_originalPosition) < RETURN_THRESHOLD && // Original logic for non-guard scenarios
            !guardPed.IsShooting &&
            !guardPed.IsInCombat &&
            !guardInScene)
        {
            if (guardPed.Heading != _originalHeading)
                guardPed.Heading = _originalHeading;

            if (!_isGuardScenario) 
            { 
                guardPed.Task.StartScenarioInPlace(_activeScenario);
                Logger.Log($"Guard resumed scenario: {_activeScenario}");
            }
            guardInScene = true;
        }

        else if (_isGuardScenario && guardPed.Position.DistanceTo(_originalPosition) < GUARD_RETURN_DISTANCE_THRESHOLD && // New logic for guard scenarios, stay close
                 !guardPed.IsShooting &&
                 !guardPed.IsInCombat &&
                 !guardInScene)
        {
            // For guard scenarios, if close enough, just stand guard again
            if (guardPed.Heading != _originalHeading)
                guardPed.Heading = _originalHeading;

            //guardPed.Task.GuardCurrentPosition();
            HelperClass.StandGuard(guardPed, Position, Heading, "WORLD_HUMAN_GUARD_STAND");
            Logger.Log("Guard resumed guard position (within threshold)");
            guardInScene = true;
        }


        else if (guardPed.IsIdle && guardPed.IsAlive && !guardPed.IsRagdoll &&
                 !guardPed.IsInAir && !guardPed.IsClimbing && !guardPed.IsFalling && guardPed.IsOnFoot)
        {
            switch (new Random().Next(0, 2))
            {
                case 0:
                    guardPed.Task.FollowNavMeshTo(_originalPosition, PedMoveBlendRatio.Walk);
                    break;
                case 1:
                    guardPed.Task.FollowNavMeshTo(_originalPosition, PedMoveBlendRatio.Run);
                    break;
                case 2:
                    guardPed.Task.FollowNavMeshTo(_originalPosition, PedMoveBlendRatio.Sprint);
                    break;
                default:
                    guardPed.Task.FollowNavMeshTo(_originalPosition, PedMoveBlendRatio.Walk);
                    break;
            }
            Logger.Log("Guard walking back to original position");
        }
    }

    private Ped SpawnGuard(string modelName, Vector3 position) // (unchanged)
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
        mdl.MarkAsNoLongerNeeded(); // Mark model as no longer needed after spawning

        if (spawnedPed == null)
        {
            Logger.Log($"Failed to create guard ped with model {modelName}.");
            return null; // Return null if ped creation failed
        }


        spawnedPed.Heading = Heading;
        spawnedPed.Weapons.Give(WeaponName, 800, true, true);
        spawnedPed.Armor = 200;
        spawnedPed.MaxHealth = 300;
        spawnedPed.Health = 300;
        spawnedPed.DrivingAggressiveness = 1f;
        spawnedPed.VehicleDrivingFlags = VehicleDrivingFlags.DrivingModeAvoidVehicles;
        // Function.Call(Hash.SET_PED_KEEP_TASK, guardPed.Handle, false);
        OutputArgument groundZArg = new OutputArgument();
        Function.Call(Hash.SET_PED_RANDOM_PROPS, spawnedPed);

        float groundZ = spawnedPed.Position.Z;
        Function.Call(Hash.GET_GROUND_Z_FOR_3D_COORD, Position.X, Position.Y, Position.Z + 5, groundZArg, false, false);

        if (!Interior)
        {
            groundZ = groundZArg.GetResult<float>();
            spawnedPed.Position = new Vector3(Position.X, Position.Y, groundZ);
        }
        else
        {
            spawnedPed.Position = new Vector3(Position.X, Position.Y, Position.Z);
        }
        //spawnedPed.PopulationType = EntityPopulationType.RandomScenario; //this is kind of making peds to use default AI behavior defined by the pedpersonality? or whatever file name is


        spawnedPed.IsCollisionEnabled = true;
        spawnedPed.SetConfigFlag(PedConfigFlagToggles.WillNotHotwireLawEnforcementVehicle, false);
        spawnedPed.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
        spawnedPed.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, true);
        spawnedPed.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, true);
        //guardPed.SetCombatAttribute(CombatAttributes.AlwaysFight, true);
        spawnedPed.SetCombatAttribute(CombatAttributes.CanUseCover, true);
        spawnedPed.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
        spawnedPed.SetCombatAttribute(CombatAttributes.AlwaysFlee, false);
        spawnedPed.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, true);
        spawnedPed.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);
        spawnedPed.SetCombatAttribute(CombatAttributes.DisableBulletReactions, true);

        spawnedPed.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, true);
        spawnedPed.DiesOnLowHealth = false;
        spawnedPed.SetConfigFlag(PedConfigFlagToggles.CanDiveAwayFromApproachingVehicles, true);
        spawnedPed.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, true);
        spawnedPed.SetConfigFlag(PedConfigFlagToggles.AIDriverAllowFriendlyPassengerSeatEntry, true);
        spawnedPed.SetConfigFlag(PedConfigFlagToggles.KeepRelationshipGroupAfterCleanUp, true);
        spawnedPed.SetConfigFlag(PedConfigFlagToggles.AllowMedicsToReviveMe, true);
        if (spawnedPed.PedType == PedType.Cop || spawnedPed.PedType == PedType.Swat) spawnedPed.TargetLossResponse = TargetLossResponse.SearchForTarget;
        spawnedPed.VehicleDrivingFlags = VehicleDrivingFlags.SteerAroundObjects;
        if (spawnedPed.PedType == PedType.Cop || spawnedPed.PedType == PedType.Swat || spawnedPed.PedType == PedType.Army)
        {
            spawnedPed.SetConfigFlag(PedConfigFlagToggles.CanAttackNonWantedPlayerAsLaw, false);
            spawnedPed.SetConfigFlag(PedConfigFlagToggles.DontAttackPlayerWithoutWantedLevel, true);
        }
        return spawnedPed;
    }

    // This method will retrieve the correct relationship group for the given name (unchanged)
    public static uint GetHash(string characterName)
    {
        return StringHash.AtStringHash(characterName); // Convert custom group name to hash
    }

    void RelationshipCrapSetups(bool gunner = false) // (unchanged)
    {
        Ped pedToConfigureRelations; // Default to on-foot guard

        if (gunner)
        {
            pedToConfigureRelations = guardPedOnVehicle;
            if (pedToConfigureRelations == null)
            {
                Logger.Log("Warning: RelationshipCrapSetups called for gunner, but guardPedOnVehicle is null. Relationships will not be set for gunner.");
                return; // Exit if gunner is true but guardPedOnVehicle is null
            }
        }
        else
        {
            pedToConfigureRelations = guardPed; // Ensure it's guardPed if not gunner, even if redundant for clarity
            if (pedToConfigureRelations == null)
            {
                Logger.Log("Warning: RelationshipCrapSetups called for ped, but guardPed is null. Relationships will not be set for ped.");
                return; // Exit if not gunner but guardPed is null - although this should be handled in Spawn already.
            }
        }

        try
        {
            // Convert all relationship group names to hashes (unchanged)
            var PrivateGuardHash = StringHash.AtStringHash("PRIVATE_SECURITY");
            var GuardHash = StringHash.AtStringHash("SECURITY_GUARD");
            var ArmyHash = StringHash.AtStringHash("ARMY");
            var CopHash = StringHash.AtStringHash("COP");
            var GuardDogHash = StringHash.AtStringHash("GUARD_DOG");
            var MerryW = StringHash.AtStringHash("MERRYWEATHER");

            var FiremanHash = StringHash.AtStringHash("FIREMAN");
            var MedicHash = StringHash.AtStringHash("MEDIC");
            var DealerHash = StringHash.AtStringHash("DEALER");

            // Gang relationship groups (unchanged)
            var GangLostHash = StringHash.AtStringHash("AMBIENT_GANG_LOST");
            var GangMexicanHash = StringHash.AtStringHash("AMBIENT_GANG_MEXICAN");
            var GangFamilyHash = StringHash.AtStringHash("AMBIENT_GANG_FAMILY");
            var GangBallasHash = StringHash.AtStringHash("AMBIENT_GANG_BALLAS");
            var GangMarabunteHash = StringHash.AtStringHash("AMBIENT_GANG_MARABUNTE");
            var GangCultHash = StringHash.AtStringHash("AMBIENT_GANG_CULT");
            var GangSalvaHash = StringHash.AtStringHash("AMBIENT_GANG_SALVA");
            var GangWeichengHash = StringHash.AtStringHash("AMBIENT_GANG_WEICHENG");
            var GangHillbillyHash = StringHash.AtStringHash("AMBIENT_GANG_HILLBILLY");

            // Law enforcement groups (for mutual respect) (unchanged)
            List<uint> lawGroups = new List<uint>
            {
                ArmyHash, CopHash, GuardHash, PrivateGuardHash, GuardDogHash
            };

            // Gang groups (they will hate each other) (unchanged)
            List<uint> gangGroups = new List<uint>
            {
                GangLostHash, GangMexicanHash, GangFamilyHash, GangBallasHash,
                GangMarabunteHash, GangCultHash, GangSalvaHash, GangWeichengHash, GangHillbillyHash
            };

            // === APPLY RELATIONSHIPS === //

            // Set mutual respect between law enforcement & fireman/medic (unchanged)
            foreach (uint law in lawGroups)
            {
                foreach (uint medicRescue in new List<uint> { FiremanHash, MedicHash })
                {
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, PedRelationship.Respect, law, medicRescue);
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, PedRelationship.Respect, medicRescue, law);
                }
            }

            // Gangs hate each other (unchanged)
            foreach (uint gangA in gangGroups)
            {
                foreach (uint gangB in gangGroups)
                {
                    if (gangA != gangB)
                    {
                        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, PedRelationship.Hate, gangA, gangB);
                        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, PedRelationship.Hate, gangB, gangA);
                    }
                }
            }

            // Cops & Dealers hate each other (unchanged)
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, PedRelationship.Hate, CopHash, DealerHash);
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, PedRelationship.Hate, DealerHash, CopHash);

            // Ensure law enforcement respects each other (unchanged)
            foreach (uint lawA in lawGroups)
            {
                foreach (uint lawB in lawGroups)
                {
                    pedToConfigureRelations.SetConfigFlag(PedConfigFlagToggles.LawWillOnlyAttackIfPlayerIsWanted, true);
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, PedRelationship.Respect, lawA, lawB);
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, PedRelationship.Respect, lawB, lawA);
                }
            }

            pedToConfigureRelations.RelationshipGroup = World.AddRelationshipGroup(GuardConfig.RelationshipGroup);
            pedToConfigureRelations.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Companion);

            // Ensure police, SWAT, and army units only attack players if they are wanted (unchanged)
            if (pedToConfigureRelations.PedType == PedType.Cop || pedToConfigureRelations.PedType == PedType.Swat || pedToConfigureRelations.PedType == PedType.Army)
            {
                switch (pedToConfigureRelations.PedType)
                {
                    case PedType.Army:
                        Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, pedToConfigureRelations.Handle, ArmyHash);
                        break;
                    case PedType.Cop:
                    case PedType.Swat:
                        Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, pedToConfigureRelations.Handle, CopHash);
                        break;
                }
            }

            // Override relationships for Franklin/Michael's house guards. (unchanged)
            // If we are in FranklinHouse or MichaelHouse, apply these overrides and exit early so that later conditions do not reset them.
            //if (Area.Name == "FranklinHouse" || Area.Name == "MichaelHouse" )
            //{
            //    bool isMichaelOrFranklin = (Game.Player.Character.Model == PedHash.Michael || Game.Player.Character.Model == PedHash.Franklin);

            //    if (isMichaelOrFranklin)
            //    {
            //        Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Companion);
            //        pedToConfigureRelations.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Companion);
            //        Logger.Log($"{Area.Name}_GUARD respects {Game.Player.Character.Model.ToString()}");
            //    }
            //    else if (Game.Player.Character.Model == PedHash.Trevor)
            //    {
            //        Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Neutral);
            //        pedToConfigureRelations.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Neutral);
            //        Logger.Log($"{Area.Name}_GUARD is neutral towards Trevor.");
            //    }

            //    if (Area.Name == "MichaelHouse" && Game.Player.Character.Model == PedHash.Trevor)
            //    {
            //        Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Dislike);
            //        pedToConfigureRelations.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Dislike);
            //        Logger.Log("Trevor is disliked by Michael's guard.");
            //    }

            //    return;
            //}

            if (Area.Respect == "YES" || Area.Respect == "ANY" || Area.Respect == "ALL") // (unchanged)
            {
                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Companion);
                pedToConfigureRelations.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Companion);
            }
            else if (Area.Respect == "TREVOR") // (unchanged)
            {
                if (Game.Player.Character.Model == PedHash.Trevor)
                {
                    Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Companion);
                    pedToConfigureRelations.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Companion);
                    Logger.Log($"Trevor is respected by {Area.Name} guard.");
                }
                else
                {
                    Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Neutral);
                    pedToConfigureRelations.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Neutral);
                    Logger.Log($"Default relationship with {Game.Player.Character.Model.ToString()}");
                }
            }
            else if (Area.Respect == "MICHAEL") // (unchanged)
            {
                if (Game.Player.Character.Model == PedHash.Michael)
                {
                    Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Companion);
                    pedToConfigureRelations.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Companion);
                    Logger.Log($"Michael is respected by {Area.Name} guard.");
                }
                else
                {
                    Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Neutral);
                    pedToConfigureRelations.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Neutral);
                    Logger.Log($"Default relationship with {Game.Player.Character.Model.ToString()}");
                }
            }
            else if (Area.Respect == "FRANKLIN") // (unchanged)
            {
                if (Game.Player.Character.Model == PedHash.Franklin)
                {
                    Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Companion);
                    pedToConfigureRelations.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Companion);
                }
                else
                {
                    Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Neutral);
                    pedToConfigureRelations.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Neutral);
                }
            }
            else // Handle multiple entries or other Respect values
            {
                bool respectedCharacter = false;
                string[] respectedCharactersList = Area.Respect?.Split(',') ?? new string[0]; // Split by comma, handle null Area.Respect

                foreach (string characterName in respectedCharactersList)
                {
                    string trimmedCharacterName = characterName.Trim().ToUpperInvariant(); // Trim whitespace and make case-insensitive

                    if (trimmedCharacterName == "TREVOR")
                    {
                        if (Game.Player.Character.Model == PedHash.Trevor)
                        {
                            respectedCharacter = true;
                            Logger.Log($"Trevor is respected by {Area.Name} guard (multi-entry).");
                            break; // Exit loop once a match is found
                        }
                    }
                    else if (trimmedCharacterName == "MICHAEL")
                    {
                        if (Game.Player.Character.Model == PedHash.Michael)
                        {
                            respectedCharacter = true;
                            Logger.Log($"Michael is respected by {Area.Name} guard (multi-entry).");
                            break; // Exit loop once a match is found
                        }
                    }
                    else if (trimmedCharacterName == "FRANKLIN")
                    {
                        if (Game.Player.Character.Model == PedHash.Franklin)
                        {
                            respectedCharacter = true;
                            Logger.Log($"Franklin is respected by {Area.Name} guard (multi-entry).");
                            break; // Exit loop once a match is found
                        }
                    }
                    // Add more character name checks here if needed in the future (e.g., " любыйCustomCharacterName")
                }

                if (respectedCharacter)
                {
                    Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Companion);
                    pedToConfigureRelations.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Companion);
                }
                else
                {
                    Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Neutral);
                    pedToConfigureRelations.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Neutral);
                    Logger.Log($"Default relationship with {Game.Player.Character.Model.ToString()} (multi-entry or default).");
                }
            }
            //else // Default to neutral if none of the above conditions are met. (unchanged)
            //{
            //    Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(pedToConfigureRelations.RelationshipGroup, Relationship.Neutral);
            //    pedToConfigureRelations.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Neutral);
            //}
        }
        catch (Exception ex) // (unchanged)
        {
            Logger.Log($"Error in RelationshipCrapSetups: {ex.Message} StackTrace: {ex.StackTrace}");
            Notification.PostTicker($"Error setting up relationships. Check log.{ex.Message}        StackTrace: {ex.StackTrace}", false);
            throw; // Only this function re-throws exceptions as requested.
        }
    }

    public void Spawn() // (unchanged)
    {
        Logger.Log($"Spawning guard at position {Position}, heading {Heading}, area {AreaName}, type {Type}");

        if (Type == "ped")
        {
            guardPed = SpawnGuard(PedModelName, Position);

            if (guardPed == null)
            {
                Logger.Log("Failed to create guard ped in Spawn method.");
                return;
            }

            Logger.Log($"{PedModelName} spawned at position {Position} with heading {Heading}.");
            Logger.Log($"Weapon {WeaponName} given to guard.");

            // Determine the scenario to use (unchanged)
            if (_isGuardScenario)
            {
                Logger.Log($"Setting guard to use guard behavior at position {Position}");
                switch (new Random().Next(0, 3))
                {
                    case 0:
                        guardPed.GuardCurrentPosition(true);
                        break;
                    case 1:
                        guardPed.Task.GuardCurrentPosition();
                        break;
                    case 2:
                        guardPed.StandGuard(Position, Heading, "WORLD_HUMAN_GUARD_STAND");
                        break;
                    default:
                        guardPed.GuardCurrentPosition(false);
                        break;
                }
            }
            else
            {
                Logger.Log($"Starting scenario {_activeScenario} for guard at position {Position}");
                guardPed.Task.StartScenarioInPlace(_activeScenario);
            }

            // Set combat attributes (unchanged)
            Logger.Log("Relationships set: Gangs hate each other, Medic/Fireman respect law, Dealer vs Cop hostility.");

            // One-time setup for Guards (unchanged)
            RelationshipCrapSetups();
        }
        else if (Type == "vehicle") // (unchanged)
        {
            Vehicle vehicle = World.CreateVehicle(VehicleModelName, Position);
            if (vehicle == null)
            {
                Logger.Log("Failed to create guard vehicle in Spawn method.");
                return;
            }
            guardVehicle = vehicle; // Assign to the class member after null check

            guardVehicle.Heading = Heading;
            guardVehicle.LockStatus = VehicleLockStatus.CanBeBrokenInto;
            Logger.Log($"Vehicle spawned at position {Position} with model {VehicleModelName}.");
        }
        else if (Type == "largevehicle") // (unchanged)
        {
            Vehicle vehicle = World.CreateVehicle(LVehicleModelName, Position);
            if (vehicle == null)
            {
                Logger.Log("Failed to create guard vehicle in Spawn method.");
                return;
            }
            guardVehicle = vehicle; // Assign to the class member after null check

            guardVehicle.Heading = Heading;
            guardVehicle.LockStatus = VehicleLockStatus.CanBeBrokenInto;
            Logger.Log($"Vehicle spawned at position {Position} with model {VehicleModelName}.");
        }
        else if (Type == "helicopter") // (unchanged)
        {
            Vehicle vehicle = World.CreateVehicle(HVehicleModelName, Position);
            if (vehicle == null)
            {
                Logger.Log("Failed to create guard helicopter in Spawn method.");
                return;
            }
            guardVehicle = vehicle; // Assign to the class member after null check

            guardVehicle.Heading = Heading;
            guardVehicle.LockStatus = VehicleLockStatus.CanBeBrokenInto;
            Logger.Log($"Helicopter spawned at position {Position} with model {VehicleModelName}.");
        }
        else if (Type == "plane") // (unchanged)
        {
            Vehicle vehicle = World.CreateVehicle(PVehicleModelName, Position);
            if (vehicle == null)
            {
                Logger.Log("Failed to create guard helicopter in Spawn method.");
                return;
            }
            guardVehicle = vehicle; // Assign to the class member after null check

            guardVehicle.Heading = Heading;
            guardVehicle.LockStatus = VehicleLockStatus.CanBeBrokenInto;
            Logger.Log($"Plane spawned at position {Position} with model {VehicleModelName}.");
        }
        else if (Type == "boat") // (unchanged)
        {
            Vehicle vehicle = World.CreateVehicle(BVehicleModelName, Position);
            if (vehicle == null)
            {
                Logger.Log("Failed to create guard boat in Spawn method.");
                return;
            }
            guardVehicle = vehicle; // Assign to the class member after null check
            guardVehicle.Heading = Heading;
            guardVehicle.LockStatus = VehicleLockStatus.CanBeBrokenInto;
            Logger.Log($"Boat spawned at position {Position} with model {VehicleModelName}.");
        }
        else if (Type == "mounted") // (unchanged)
        {
            guardVehicle = World.CreateVehicle(MVehicleModelName, Position);
            guardVehicle.IsCollisionEnabled = true;

            for (int i = -1; i < guardVehicle.PassengerCapacity + 1; i++)
            {
                if (guardVehicle.IsSeatFree((VehicleSeat)i) && guardVehicle.IsTurretSeat((VehicleSeat)i))
                    guardPedOnVehicle = guardVehicle.CreatePedOnSeat((VehicleSeat)i, PedModelName);
            }
            guardPedOnVehicle.Weapons.Give(WeaponName, 200, true, true);
            guardPedOnVehicle.IsCollisionEnabled = true;
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, false);
            guardPedOnVehicle.DiesOnLowHealth = false;
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.UseVehicleAttackIfVehicleHasMountedGuns, true);
            guardPedOnVehicle.SetConfigFlag(PedConfigFlagToggles.WillNotHotwireLawEnforcementVehicle, true);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, false);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, false);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.CanUseCover, false);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.AlwaysFlee, false);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, true);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.DisableBulletReactions, true);
            guardPedOnVehicle.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, true);
            guardPedOnVehicle.SetConfigFlag(PedConfigFlagToggles.CanDiveAwayFromApproachingVehicles, true);
            guardPedOnVehicle.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, false);
            guardPedOnVehicle.SetConfigFlag(PedConfigFlagToggles.AIDriverAllowFriendlyPassengerSeatEntry, true);
            guardPedOnVehicle.FiringPattern = FiringPattern.FullAuto;
            guardPedOnVehicle.PopulationType = EntityPopulationType.RandomScenario;
            guardPedOnVehicle.SetConfigFlag(PedConfigFlagToggles.KeepRelationshipGroupAfterCleanUp, true);
            guardPedOnVehicle.ShootRate = 999;
            guardVehicle.LockStatus = VehicleLockStatus.CanBeBrokenInto;
            guardVehicle.Heading = Heading;
            if (guardPedOnVehicle.PedType == PedType.Cop || guardPedOnVehicle.PedType == PedType.Swat || guardPedOnVehicle.PedType == PedType.Army)
            {
                guardPedOnVehicle.SetConfigFlag(PedConfigFlagToggles.CanAttackNonWantedPlayerAsLaw, false);
                guardPedOnVehicle.SetConfigFlag(PedConfigFlagToggles.AllowMedicsToReviveMe, true);
                guardPedOnVehicle.SetConfigFlag(PedConfigFlagToggles.DontAttackPlayerWithoutWantedLevel, true);
            }
            RelationshipCrapSetups(true);
        }
    }

    public enum GuardType
    {
        Ped, //make this guard, ped, soldier, npc as return type for the mention strings
        Vehicle, // for vehicle, car, bike, cycle
        Helicopter, //heli, helicopter, chopper, copter is what ppl say
        Boat, //boat, ship, seabike, jetski ?
        Mounted, //gunner, mounted, turret
        LargeVehicle, //bus, truck, trailer? idk
        LargeHelicopter, //cargobob, bigheli, largehelicopter or whatever names
        CargoPlane, //cargoplane, globemaster?
        LargeBoat //warship, tugboat, largeboat, submarine, bigship?
    }

    public void Despawn()
    {
        Logger.Log($"Despawning guard at position {Position}, type {Type}");

        if (Type == "ped")
        {
            if (guardPed != null && guardPed.Exists()) // Null and existence check
            {
                guardPed.MarkAsNoLongerNeeded();
                Logger.Log($"Guard ped despawned at position {Position}.");
            }
            else
            {
                Logger.Log($"Guard ped was null or didn't exist, cannot despawn. Type: {Type}, Position: {Position}");
            }
        }
        else if (Type == "vehicle" || Type == "helicopter" || Type == "boat")
        {
            if (guardVehicle != null && guardVehicle.Exists()) // Null and existence check
            {
                guardVehicle.MarkAsNoLongerNeeded();
                Logger.Log($"Guard vehicle despawned at position {Position}.");
            }
            else
            {
                Logger.Log($"Guard vehicle was null or didn't exist, cannot despawn. Type: {Type}, Position: {Position}");
            }
        }
        else if (Type == "mounted")
        {
            if (guardPedOnVehicle != null && guardPedOnVehicle.Exists()) // Null and existence check for ped
            {
                guardPedOnVehicle.MarkAsNoLongerNeeded();
                Logger.Log($"Mounted guard ped despawned at position {Position}.");
            }
            else
            {
                Logger.Log($"Mounted guard ped was null or didn't exist, cannot despawn. Type: {Type}, Position: {Position}");
            }

            if (guardVehicle != null && guardVehicle.Exists()) // Null and existence check for vehicle
            {
                guardVehicle.MarkAsNoLongerNeeded();
                Logger.Log($"Mounted guard vehicle despawned at position {Position}.");
            }
            else
            {
                Logger.Log($"Mounted guard vehicle was null or didn't exist, cannot despawn. Type: {Type}, Position: {Position}");
            }
        }
        else
        {
            Logger.Log($"Failed to despawn guard at position {Position}. Unknown Type or Type not handled: {Type}");
        }
    }
}