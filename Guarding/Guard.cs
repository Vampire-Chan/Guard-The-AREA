using GTA;
using GTA.Math;
using GTA.Native;
using GTA.NaturalMotion;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Windows.Forms;

// Helper class to manage relationships
public class RelationshipManager
{
    public List<string> Hate { get; set; } = new List<string>();
    public List<string> Dislike { get; set; } = new List<string>();
    public List<string> Respect { get; set; } = new List<string>();
    public List<string> Like { get; set; } = new List<string>();

    public RelationshipManager(List<string> hate, List<string> dislike, List<string> respect, List<string> like)
    {
        Hate = hate;
        Dislike = dislike;
        Respect = respect;
        Like = like;
    }
}



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
    private readonly RelationshipManager _relationshipsArea;
    private readonly RelationshipManager _relationshipsGuard;

    public Ped guardPed;
    public Vehicle guardVehicle;
    public RelationshipGroup GuardGroup { get; set; }

    // New fields for combat tracking
    private Vector3 _originalPosition;
    private readonly float _originalHeading;
    private const float RETURN_THRESHOLD = 2f; // Distance threshold for considering ped "returned"
    private const int COMBAT_CHECK_DELAY = 2000; // Time to wait before checking if combat is truly over

    private string VehicleModelName;
    private string PedModelName;
    private string WeaponName;
    private readonly GuardConfig GuardConfig;
    private string Scenario; //with value the original
    private string randomScenario;

    // PLAYER_ZERO IS MICHAEL
    // PLAYER_ONE IS FRANKLIN
    // PLAYER_TWO IS TREVOR

    private RelationshipGroup Michael
    {
        get
        {
            if (Game.Player.Character.Model == PedHash.Michael)
            {
                return Game.Player.Character.RelationshipGroup;
            }
            return null;
        }
    }

    private RelationshipGroup Franklin
    {
        get
        {
            if (Game.Player.Character.Model == PedHash.Franklin)
            {
                return Game.Player.Character.RelationshipGroup;
            }
            return null;
        }
    }

    private RelationshipGroup Trevor
    {
        get
        {
            if (Game.Player.Character.Model == PedHash.Trevor)
            {
                return Game.Player.Character.RelationshipGroup;
            }
            return null;
        }
    }


    //and if xml returns any relationship group named as above means we have to check that current player is zero,one or two and then the player.hash of relationship will be used to setup

    private RelationshipGroup guardGroup;

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

        // Initialize relationships
        _relationshipsArea = new RelationshipManager(Area.Hate, Area.Dislike, Area.Respect, Area.Like);
        _relationshipsGuard = new RelationshipManager(GuardConfig.Hate, GuardConfig.Dislike,
            GuardConfig.Respect, GuardConfig.Like);
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
            _activeScenario = GetRandomElement(MainScript.scenarios);
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
                GetRandomElement(MainScript.scenarios) :
                _configuredScenario;
            Logger.Log("Scenario disabled, using guard behavior");
        }
        // For all other cases, including "random"
        else
        {
            _isGuardScenario = false;
            _activeScenario = scenarioLower == "random" ?
                GetRandomElement(MainScript.scenarios) :
                _configuredScenario;
            Logger.Log($"Using scenario: {_activeScenario}");
        }
    }



    private static readonly Random rand = new Random(); // Single Random instance

    private void RandomizeLoadout()
    {
        PedModelName = GetRandomElement(GuardConfig.PedModels);
        WeaponName = GetRandomElement(GuardConfig.Weapons);
        VehicleModelName = GetRandomElement(GuardConfig.VehicleModels);
        if (Scenario != null) randomScenario = GetRandomElement(MainScript.scenarios);
        
    }

    private static T GetRandomElement<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("List cannot be null or empty");
        return list[rand.Next(list.Count)];
    }

    
    public void UpdateCombatState()
    {
        if (guardPed == null || !guardPed.Exists() || guardPed.IsDead)
            return;

        bool isCurrentlyInCombat = guardPed.IsInCombat;

        // Check if entering combat
        if (isCurrentlyInCombat)
        {
            EnterCombatMode();
        }

        // Check if exiting combat
        else if (!isCurrentlyInCombat)
        {
            Script.Wait(COMBAT_CHECK_DELAY); // Wait to ensure combat is truly over
            if (guardPed.IsIdle && guardPed.IsAlive && !guardPed.IsRagdoll && !guardPed.IsInAir && !guardPed.IsClimbing && !guardPed.IsFalling && !guardPed.IsShooting && !guardPed.IsInCombat)
            {
                ExitCombatMode();
            }
        }

    }

    private void EnterCombatMode()
    {
        if (guardPed == null || !guardPed.Exists())
            return;

        Logger.Log($"Guard entering combat mode at position {guardPed.Position}");

        // Set combat attributes
        guardPed.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
        guardPed.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, true);
        guardPed.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, true);
        guardPed.SetCombatAttribute(CombatAttributes.CanUseCover, true);
        guardPed.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
        guardPed.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);

        guardPed.Task.CombatHatedTargetsAroundPed(1000);
    }

    private void ExitCombatMode()
    {
        if (guardPed == null || !guardPed.Exists())
            return;

        Logger.Log($"Guard exiting combat mode, returning to position {_originalPosition}");

        if (guardPed.Exists() && guardPed.Position.DistanceTo(_originalPosition) < RETURN_THRESHOLD)
        {
            // Reset heading
            if (guardPed.Heading != _originalHeading)
                guardPed.Heading = _originalHeading;

            // Return to original behavior
            if (_isGuardScenario)
            {
                guardPed.Task.GuardCurrentPosition();
                Logger.Log("Guard resumed guard position");
            }
            else
            {
                guardPed.Task.StartScenarioInPlace(_activeScenario);
                Logger.Log($"Guard resumed scenario: {_activeScenario}");
            }
        }
        else if (guardPed.IsIdle && guardPed.IsAlive && !guardPed.IsRagdoll &&
                 !guardPed.IsInAir && !guardPed.IsClimbing && !guardPed.IsFalling &&
                 !guardPed.IsShooting && !guardPed.IsInCombat && guardPed.IsOnFoot)
        {
            guardPed.Task.FollowNavMeshTo(_originalPosition);
        }
    }

    public void Spawn()
    {
        Logger.Log($"Spawning guard at position {Position}, heading {Heading}, area {AreaName}, type {Type}");

        if (Type == "ped")
        {
            guardPed = World.CreatePed(PedModelName, Position);
            if (guardPed == null)
            {
                Logger.Log("Failed to create guard ped.");
                return;
            }
            Logger.Log($"{PedModelName} spawned at position {Position} with heading {Heading}.");
            guardPed.Heading = Heading;

            guardPed.Weapons.Give(WeaponName, 400, true, true);

            Logger.Log($"Weapon {WeaponName} given to guard.");

            guardPed.Armor = 200;
            guardPed.DiesOnLowHealth = false;
            guardPed.MaxHealth = 400;
            guardPed.Health = 400;

            //guardPed.CombatAbility = CombatAbility.Professional;
            // guardPed.CombatMovement = CombatMovement.WillAdvance;
            // guardPed.CombatRange = CombatRange.Medium;
            // guardPed.FiringPattern = FiringPattern.FullAuto;
            // guardPed.Accuracy = 200;
            //  guardPed.ShootRate = 1000;

            guardPed.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
            guardPed.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, true);
            guardPed.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, true);
            guardPed.SetCombatAttribute(CombatAttributes.CanUseCover, true);
            guardPed.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
            guardPed.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);

            guardPed.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, true);
            guardPed.SetConfigFlag(PedConfigFlagToggles.CanDiveAwayFromApproachingVehicles, true);
            guardPed.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, true);
            guardPed.SetConfigFlag(PedConfigFlagToggles.AIDriverAllowFriendlyPassengerSeatEntry, true);

            OutputArgument groundZArg = new OutputArgument();
            Function.Call(Hash.SET_PED_RANDOM_PROPS, guardPed);
            float groundZ = 0.0f;

            if (Interior)
            {
                Function.Call(Hash.GET_GROUND_Z_FOR_3D_COORD, Position.X, Position.Y, Position.Z + 30, groundZArg, false, false);
                guardPed.IsCollisionEnabled = true;

                groundZ = groundZArg.GetResult<float>();
                guardPed.Position = new Vector3(Position.X, Position.Y, groundZ);
            }

            else guardPed.Position = new Vector3(Position.X, Position.Y, Position.Z);

            // Determine the scenario to use
            if (_isGuardScenario)
            {
                Logger.Log($"Setting guard to use guard behavior at position {Position}");
                //guardPed.Weapons.Select(;
                guardPed.Task.GuardCurrentPosition();
            }
            else
            {
                Logger.Log($"Starting scenario {_activeScenario} for guard at position {Position}");
                guardPed.Task.StartScenarioInPlace(_activeScenario);
            }

            if (guardPed.PedType == PedType.Cop || guardPed.PedType == PedType.Swat || guardPed.PedType == PedType.Army)
            {
                // Do nothing for law enforcement
                guardPed.SetConfigFlag(PedConfigFlagToggles.LawWillOnlyAttackIfPlayerIsWanted, true);
                guardPed.SetConfigFlag(PedConfigFlagToggles.WillNotHotwireLawEnforcementVehicle, false);
            }

            if (guardPed.RelationshipGroup == RelationshipGroupHash.Army) //same way for cop
            {

            }
            else
            {
                guardPed.RelationshipGroup = guardGroup;
                guardGroup.SetRelationshipBetweenGroups(guardGroup, Relationship.Companion);
                guardGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Respect);

                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(guardGroup, Relationship.Respect);
            }
        }
        else if (Type == "vehicle")
        {
            guardVehicle = World.CreateVehicle(VehicleModelName, Position);
            if (guardVehicle == null)
            {
                Logger.Log("Failed to create guard vehicle.");
                return;
            }

            guardVehicle.Heading = Heading;
            //guardVehicle.IsPersistent = true;
            guardVehicle.LockStatus = VehicleLockStatus.CanBeBrokenInto;
            //guardVehicle.EngineHealth = 2000;

            Logger.Log($"Vehicle spawned at position {Position} with model {VehicleModelName}.");
        }
        else if (Type == "helicopter")
        {
            guardVehicle = World.CreateVehicle(VehicleModelName, Position);
            if (guardVehicle == null)
            {
                Logger.Log("Failed to create guard vehicle.");
                return;
            }
            
            guardVehicle.Heading = Heading;
            //guardVehicle.IsPersistent = true;
            guardVehicle.LockStatus = VehicleLockStatus.CanBeBrokenInto;
          //  guardVehicle.EngineHealth = 2000;

            Logger.Log($"Vehicle spawned at position {Position} with model {VehicleModelName}.");
        }
        else if (Type == "boat")
        {
            guardVehicle = World.CreateVehicle(VehicleModelName, Position);
            if (guardVehicle == null)
            {
                Logger.Log("Failed to create guard vehicle.");
                return;
            }

            guardVehicle.Heading = Heading;
            //guardVehicle.IsPersistent = true;
            guardVehicle.LockStatus = VehicleLockStatus.CanBeBrokenInto;
//guardVehicle.EngineHealth = 2000;

            Logger.Log($"Vehicle spawned at position {Position} with model {VehicleModelName}.");
        }
    }

    public void Despawn()
    {
        if (Type == "ped" && guardPed != null && guardPed.Exists())
        {
            guardPed.Delete();
            Logger.Log($"Guard ped despawned at position {Position}.");
        }
        else if (Type == "vehicle" && guardVehicle != null && guardVehicle.Exists())
        {
            guardVehicle.Delete();
            Logger.Log($"Guard vehicle despawned at position {Position}.");
        }
        else
        {
            Logger.Log($"Failed to despawn guard at position {Position}. Type: {Type}");
        }
    }
}