using GTA;
using GTA.Math;
using GTA.Native;
using GTA.NaturalMotion;
using Guarding.DispatchSystem;
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
    public string Respect { get; set; } 
    public List<string> Like { get; set; } = new List<string>();

    public RelationshipManager(List<string> hate, List<string> dislike, string respect, List<string> like)
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

    public Ped guardPed;
    public Ped guardPedOnVehicle;
    public Vehicle guardVehicle;
    public RelationshipGroup GuardGroup { get; set; }

    // New fields for combat tracking
    private Vector3 _originalPosition;
    private readonly float _originalHeading;
    private const float RETURN_THRESHOLD = 2f; // Distance threshold for considering ped "returned"
    private const int COMBAT_CHECK_DELAY = 1000; // Time to wait before checking if combat is truly over

    private string VehicleModelName;
    private string MVehicleModelName;
    private string PedModelName;
    private string WeaponName;
    private readonly GuardConfig GuardConfig;
    private string Scenario; //with value the original
    private string randomScenario;

   
    //and if xml returns any relationship group named as above means we have to check that current player is zero,one or two and then the player.hash of relationship will be used to setup

    //private RelationshipGroup guardGroup;

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
        //_relationshipsArea = new RelationshipManager(Area.Hate, Area.Dislike, Area.Respect, Area.Like);
        //_relationshipsGuard = new RelationshipManager(GuardConfig.Hate, GuardConfig.Dislike,
         //   GuardConfig.Respect, GuardConfig.Like);
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

    // Define relationship group hashes
    private static readonly int PrivateSecurityHash = (int)StringHash.AtStringHash("PRIVATE_SECURITY");
    private static readonly int SecurityGuardHash = (int)StringHash.AtStringHash("SECURITY_GUARD");
    private static readonly int ArmyHash = (int)StringHash.AtStringHash("ARMY");
    private static readonly int CopHash = (int)StringHash.AtStringHash("COP");
    private static readonly int GuardDogHash = (int)StringHash.AtStringHash("GUARD_DOG");
    private static readonly int MerryweatherHash = (int)StringHash.AtStringHash("MERRYWEATHER");


    private static readonly Random rand = new Random(); // Single Random instance
                                                        // Create a list of relationship group hashes
    private static readonly List<int> RelationshipGroupHashes = new List<int>
{
    PrivateSecurityHash,
    SecurityGuardHash,
    ArmyHash,
    CopHash,
    GuardDogHash
   // MerryweatherHash
};
    private static readonly List<int> LawEnforcementGroups = new List<int>
    {
        ArmyHash,
    CopHash,
    };

    private static readonly List<int> SecurityGroups = new List<int>
    {
        PrivateSecurityHash,
        SecurityGuardHash,
        GuardDogHash
    };
    private void RandomizeLoadout()
    {
        PedModelName = GetRandomElement(GuardConfig.PedModels);
        MVehicleModelName = GetRandomElement(GuardConfig.MVehicleModels);
        WeaponName = GetRandomElement(GuardConfig.Weapons);
        VehicleModelName = GetRandomElement(GuardConfig.VehicleModels);
        if (Scenario != null) randomScenario = GetRandomElement(GuardManager.scenarios);

    }

    private static T GetRandomElement<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("List cannot be null or empty");
        return list[rand.Next(list.Count)];
    }


    bool wanted = false;
    private bool lawGuardsShouldFight = true; // Flag to control combat behavior of law/security guards

    public void UpdateCombatState()
    {
        if (guardPed == null || !guardPed.Exists() || guardPed.IsDead)
            return;

        bool isCurrentlyInCombat = guardPed.IsInCombat;


        // Check if exiting combat naturally
        if (!isCurrentlyInCombat)
        {
            Script.Wait(COMBAT_CHECK_DELAY); // Ensure combat is truly over

            if (guardPed.IsIdle && guardPed.IsAlive && !guardPed.IsRagdoll && !guardPed.IsInAir &&
                !guardPed.IsClimbing && !guardPed.IsFalling)
            {
                ReturnGuardToPosition();
            }
        }
        else
        {
            if (RelationshipGroupHashes.Contains(guardPed.RelationshipGroup.Hash))
            {
                if (Game.Player.WantedLevel > 0 && !wanted)
                {
                    wanted = true;
                }

                else if (Game.Player.WantedLevel == 0 && wanted)
                {
                    Game.Player.IgnoredByEveryone = true;
                    guardPed.MarkAsNoLongerNeeded();
                    guardPed.MarkAsMissionEntity();
                    wanted = false;
                    Game.Player.IgnoredByEveryone = false;
                    return;
                }

            }
        }
    }


    private void ReturnGuardToPosition()
    {
        if (guardPed == null || !guardPed.Exists() || guardPed.IsDead)
            return;
        
        if (guardPed.Position.DistanceTo(_originalPosition) < RETURN_THRESHOLD && !guardPed.IsShooting && !guardPed.IsInCombat)
        {
            if (guardPed.Heading != _originalHeading)
                guardPed.Heading = _originalHeading;

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
                 !guardPed.IsInAir && !guardPed.IsClimbing && !guardPed.IsFalling && guardPed.IsOnFoot)
        {
            guardPed.Task.FollowNavMeshTo(_originalPosition, PedMoveBlendRatio.Walk);
            Logger.Log("Guard walking back to original position");
        }
    }


   
    private Ped SpawnGuard(Model mdl, Vector3 position)
    {
        if(!mdl.IsInCdImage)
        {
            Logger.Log($"Model {mdl} is not in CD Image. Area name: {AreaName} and Guard Model: {GuardConfig.Name}.");
            return null;
        }

        guardPed = World.CreatePed(mdl, Position);

        guardPed.Heading = Heading;

        guardPed.Weapons.Give(WeaponName, 400, true, true);
        guardPed.Armor = 200;
        guardPed.DiesOnLowHealth = false;
        guardPed.MaxHealth = 300;
        guardPed.Health = 300;
        guardPed.DrivingAggressiveness = 1f;
        guardPed.VehicleDrivingFlags = VehicleDrivingFlags.DrivingModeAvoidVehicles;
       // Function.Call(Hash.SET_PED_KEEP_TASK, guardPed.Handle, false);
        OutputArgument groundZArg = new OutputArgument();
        Function.Call(Hash.SET_PED_RANDOM_PROPS, guardPed);

        float groundZ = guardPed.Position.Z;

        Function.Call(Hash.GET_GROUND_Z_FOR_3D_COORD, Position.X, Position.Y, Position.Z + 5, groundZArg, false, false);

        if (!Interior)
        {

            groundZ = groundZArg.GetResult<float>();
            guardPed.Position = new Vector3(Position.X, Position.Y, groundZ);
        }

        else guardPed.Position = new Vector3(Position.X, Position.Y, Position.Z);

        guardPed.IsCollisionEnabled = true;
       
        guardPed.SetConfigFlag(PedConfigFlagToggles.WillNotHotwireLawEnforcementVehicle, false);
        guardPed.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
        guardPed.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, true);
        guardPed.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, true);
        guardPed.SetCombatAttribute(CombatAttributes.CanUseCover, true);
        guardPed.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
        guardPed.SetCombatAttribute(CombatAttributes.AlwaysFlee, false);
        guardPed.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, true);
        guardPed.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);
        guardPed.SetCombatAttribute(CombatAttributes.DisableBulletReactions, true);
        guardPed.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, true);
        guardPed.SetConfigFlag(PedConfigFlagToggles.CanDiveAwayFromApproachingVehicles, true);
        guardPed.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, true);
        guardPed.SetConfigFlag(PedConfigFlagToggles.AIDriverAllowFriendlyPassengerSeatEntry, true);


        return guardPed;

    }

    // This method will retrieve the correct relationship group for the given name
    public static uint GetHash(string characterName)
    {
        
           return StringHash.AtStringHash(characterName); // Convert custom group name to hash
        
    }

    void RelationshipCrapSetups()
    {
        // Convert all relationship group names to hashes
        var PrivateGuardHash = StringHash.AtStringHash("PRIVATE_SECURITY");
        var GuardHash = StringHash.AtStringHash("SECURITY_GUARD");
        var ArmyHash = StringHash.AtStringHash("ARMY");
        var CopHash = StringHash.AtStringHash("COP");
        var GuardDogHash = StringHash.AtStringHash("GUARD_DOG");
        var MerryW = StringHash.AtStringHash("MERRYWEATHER");

        var FiremanHash = StringHash.AtStringHash("FIREMAN");
        var MedicHash = StringHash.AtStringHash("MEDIC");
        var DealerHash = StringHash.AtStringHash("DEALER");

        // Gang relationship groups
        var GangLostHash = StringHash.AtStringHash("AMBIENT_GANG_LOST");
        var GangMexicanHash = StringHash.AtStringHash("AMBIENT_GANG_MEXICAN");
        var GangFamilyHash = StringHash.AtStringHash("AMBIENT_GANG_FAMILY");
        var GangBallasHash = StringHash.AtStringHash("AMBIENT_GANG_BALLAS");
        var GangMarabunteHash = StringHash.AtStringHash("AMBIENT_GANG_MARABUNTE");
        var GangCultHash = StringHash.AtStringHash("AMBIENT_GANG_CULT");
        var GangSalvaHash = StringHash.AtStringHash("AMBIENT_GANG_SALVA");
        var GangWeichengHash = StringHash.AtStringHash("AMBIENT_GANG_WEICHENG");
        var GangHillbillyHash = StringHash.AtStringHash("AMBIENT_GANG_HILLBILLY");

        // Law enforcement groups (for mutual respect)
        List<uint> lawGroups = new List<uint>
{
    ArmyHash, CopHash, GuardHash, PrivateGuardHash, GuardDogHash
};

        // Gang groups (they will hate each other)
        List<uint> gangGroups = new List<uint>
{
    GangLostHash, GangMexicanHash, GangFamilyHash, GangBallasHash,
    GangMarabunteHash, GangCultHash, GangSalvaHash, GangWeichengHash, GangHillbillyHash
};

        // === APPLY RELATIONSHIPS === //

        // Set mutual respect between law enforcement & fireman/medic
        foreach (uint law in lawGroups)
        {
            foreach (uint medicRescue in new List<uint> { FiremanHash, MedicHash })
            {
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, HelperClass.PedRelationship.Respect, law, medicRescue);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, HelperClass.PedRelationship.Respect, medicRescue, law);
            }
        }

        // Gangs hate each other
        foreach (uint gangA in gangGroups)
        {
            foreach (uint gangB in gangGroups)
            {
                if (gangA != gangB)
                {
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, HelperClass.PedRelationship.Hate, gangA, gangB);
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, HelperClass.PedRelationship.Hate, gangB, gangA);
                }
            }
        }



        // Cops & Dealers hate each other
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, HelperClass.PedRelationship.Hate, CopHash, DealerHash);
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, HelperClass.PedRelationship.Hate, DealerHash, CopHash);

        // Ensure law enforcement respects each other
        foreach (uint lawA in lawGroups)
        {
            foreach (uint lawB in lawGroups)
            {
                guardPed.SetConfigFlag(PedConfigFlagToggles.LawWillOnlyAttackIfPlayerIsWanted, true);

                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, HelperClass.PedRelationship.Respect, lawA, lawB);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, HelperClass.PedRelationship.Respect, lawB, lawA);
            }
        }


        // Ensure police, SWAT, and army units only attack players if they are wanted
        if (guardPed.PedType == PedType.Cop || guardPed.PedType == PedType.Swat || guardPed.PedType == PedType.Army)
        {
            guardPed.SetConfigFlag(PedConfigFlagToggles.LawWillOnlyAttackIfPlayerIsWanted, true);

            switch (guardPed.PedType)
            {
                case PedType.Army:
                    Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, guardPed.Handle, ArmyHash);
                    break;

                case PedType.Cop:

                case PedType.Swat:
                    Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, guardPed.Handle, CopHash);
                    break;
            }
        }

        guardPed.RelationshipGroup = World.AddRelationshipGroup(GuardConfig.RelationshipGroup);
        guardPed.RelationshipGroup.SetRelationshipBetweenGroups(guardPed.RelationshipGroup, Relationship.Companion);

        // Override relationships for Franklin/Michael's house guards
        if (Area.Name == "FranklinHouse" || Area.Name == "MichaelHouse")
        {

            bool isMichaelOrFranklin = (Game.Player.Character.Model == PedHash.Michael || Game.Player.Character.Model == PedHash.Franklin);

            if (isMichaelOrFranklin)
            {
                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(guardPed.RelationshipGroup, Relationship.Respect);
                guardPed.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Respect);
                Logger.Log($"{Area.Name}_GUARD respects {Game.Player.Character.Model.ToString()}");

            }
            else if (Game.Player.Character.Model == PedHash.Trevor)
            {
                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(guardPed.RelationshipGroup, Relationship.Neutral);
                guardPed.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Neutral);
            }

            if (Area.Name == "MichaelHouse" && Game.Player.Character.Model == PedHash.Trevor)
            {
                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(guardPed.RelationshipGroup, Relationship.Dislike);
                guardPed.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Dislike);
                Logger.Log("Trevor is disliked by Michael's guard.");
            }
        }

        if (Area.Respect == "YES") //applies to all character types.
        {
            // guardPed.RelationshipGroup = RelationshipGroup;
            Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(guardPed.RelationshipGroup, Relationship.Respect);
            guardPed.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Respect);

        }

        else if (Area.Respect == "TREVOR")
        {
            if (Game.Player.Character.Model == PedHash.Trevor)
            {
                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(guardPed.RelationshipGroup, Relationship.Respect);
                guardPed.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Respect);
                Logger.Log($"Trevor is respected by {Area.Name} guard.");
            }
            else
            {
                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(guardPed.RelationshipGroup, Relationship.Neutral);
                guardPed.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Neutral);
                Logger.Log($"Default relationship with {Game.Player.Character.Model.ToString()}");
            }
        }
        else if (Area.Respect == "MICHAEL")
        {
            if (Game.Player.Character.Model == PedHash.Michael)
            {
                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(guardPed.RelationshipGroup, Relationship.Respect);
                guardPed.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Respect);
                Logger.Log($"Michael is respected by {Area.Name} guard.");
            }
            else
            {
                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(guardPed.RelationshipGroup, Relationship.Neutral);
                guardPed.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Neutral);
                Logger.Log($"Default relationship with {Game.Player.Character.Model.ToString()}");
            }
        }
        else if (Area.Respect == "FRANKLIN")
        {
            if (Game.Player.Character.Model == PedHash.Franklin)
            {
                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(guardPed.RelationshipGroup, Relationship.Respect);
                guardPed.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Respect);
                Logger.Log($"Franklin is respected by {Area.Name} guard.");
            }
            else
            {
                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(guardPed.RelationshipGroup, Relationship.Neutral);
                guardPed.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Neutral);
                Logger.Log($"Default relationship with {Game.Player.Character.Model.ToString()}");
            }
        }

        else
        { // Default behavior for other guards
          // guardPed.RelationshipGroup = GuardConfig.RelationshipGroup;
            guardPed.RelationshipGroup.SetRelationshipBetweenGroups(guardPed.RelationshipGroup, Relationship.Companion);
            guardPed.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Neutral);
            Logger.Log($"Default relationship with {Game.Player.Character.Model.ToString()}");
        }

        if (lawGroups.Contains(StringHash.AtStringHash(GuardConfig.RelationshipGroup)))
        {
            guardPed.SetAsCop(true);
        }
    }


    public void Spawn()
    {
        Logger.Log($"Spawning guard at position {Position}, heading {Heading}, area {AreaName}, type {Type}");

        if (Type == "ped")
        {
            guardPed = SpawnGuard(PedModelName, Position);


            if (guardPed == null)
            {
                Logger.Log("Failed to create guard ped.");
                return;
            }

            Logger.Log($"{PedModelName} spawned at position {Position} with heading {Heading}.");


            Logger.Log($"Weapon {WeaponName} given to guard.");

            //guardPed.CombatAbility = CombatAbility.Professional;
            // guardPed.CombatMovement = CombatMovement.WillAdvance;
            // guardPed.CombatRange = CombatRange.Medium;
            // guardPed.FiringPattern = FiringPattern.FullAuto;
            // guardPed.Accuracy = 200;
            //  guardPed.ShootRate = 1000;

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

            // Set combat attributes

            Logger.Log("Relationships set: Gangs hate each other, Medic/Fireman respect law, Dealer vs Cop hostility.");

            ////if (!string.IsNullOrEmpty(GuardConfig.Respect))
            //{
            //    string respectedGroup = GuardConfig.Respect;  // Use the single string value for respect group
            //    guardPed.RelationshipGroup = GuardConfig.RelationshipGroup; // Set the guard's relationship group
            //    uint respectedHash = GetHash(respectedGroup); // Get the group hash for the respected group
            //    guardPed.RelationshipGroup.SetRelationshipBetweenGroups(GuardConfig.RelationshipGroup, Relationship.Respect); // Set the guard's respect for the respected group

            //    // Check if the player is Michael, Franklin, or Trevor based on PedType
            //    if (Game.Player.Character.PedType == PedType.Player0 && respectedGroup == "MICHAEL") // Michael
            //    {
            //        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, HelperClass.PedRelationship.Respect, respectedHash, Game.Player.Character.RelationshipGroup);
            //        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, HelperClass.PedRelationship.Respect, Game.Player.Character.RelationshipGroup, respectedHash);
            //    }
            //    else if (Game.Player.Character.PedType == PedType.Player1 && respectedGroup == "FRANKLIN") // Franklin
            //    {
            //        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, HelperClass.PedRelationship.Respect, respectedHash, Game.Player.Character.RelationshipGroup);
            //        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, HelperClass.PedRelationship.Respect, Game.Player.Character.RelationshipGroup, respectedHash);
            //    }
            //    else if (Game.Player.Character.PedType == PedType.Player2 && respectedGroup == "TREVOR") // Trevor
            //    {
            //        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, HelperClass.PedRelationship.Respect, respectedHash, Game.Player.Character.RelationshipGroup);
            //        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, HelperClass.PedRelationship.Respect, Game.Player.Character.RelationshipGroup, respectedHash);
            //    }
            //    else if(respectedGroup == "ANY")
            //    {
            //        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, HelperClass.PedRelationship.Respect, respectedHash, Game.Player.Character.RelationshipGroup);
            //        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, HelperClass.PedRelationship.Respect, Game.Player.Character.RelationshipGroup, respectedHash);

            //    }
            //}




            // One-time setup for Guards
            RelationshipCrapSetups();
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
        else if (Type == "mounted")
        {
            guardVehicle = World.CreateVehicle(MVehicleModelName, Position);
            guardPed = guardVehicle.CreatePedOnSeat(VehicleSeat.Driver, PedModelName);
            guardPed.KeepTaskWhenMarkedAsNoLongerNeeded = true;
            guardPed.Weapons.Give(WeaponName, 400, true, true);
            guardPed.IsCollisionEnabled = true;
            guardPed.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, false);
            guardPed.DiesOnLowHealth = false;
            guardPed.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true);
            guardPed.SetCombatAttribute(CombatAttributes.UseVehicleAttackIfVehicleHasMountedGuns, true);
            guardPed.SetConfigFlag(PedConfigFlagToggles.WillNotHotwireLawEnforcementVehicle, false);
            guardPed.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
            guardPed.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, true);
            guardPed.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, true);
            guardPed.SetCombatAttribute(CombatAttributes.CanUseCover, true);
            guardPed.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
            guardPed.SetCombatAttribute(CombatAttributes.AlwaysFlee, false);
            guardPed.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, true);
            guardPed.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);
            guardPed.SetCombatAttribute(CombatAttributes.DisableBulletReactions, true);
            guardPed.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, true);
            guardPed.SetConfigFlag(PedConfigFlagToggles.CanDiveAwayFromApproachingVehicles, true);
            guardPed.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, true);
            guardPed.SetConfigFlag(PedConfigFlagToggles.AIDriverAllowFriendlyPassengerSeatEntry, true);
            //guardPed.SetCombatAttribute(CombatAttributes.PerfectAccuracy, false);
            guardPed.FiringPattern = FiringPattern.FullAuto;
            guardPed.ShootRate = 999;


            guardPed.MarkAsNoLongerNeeded();
            guardVehicle.IsCollisionEnabled = true;
            guardPedOnVehicle = guardVehicle.CreatePedOnSeat((VehicleSeat)GuardConfig.SeatIndex, PedModelName);
            guardPedOnVehicle.Weapons.Give(WeaponName, 400, true, true);
            guardPedOnVehicle.IsCollisionEnabled = true;
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, false);
            guardPedOnVehicle.DiesOnLowHealth = false;


            // guardPed.SetCombatAttribute(CombatAttributes.ForceCheckAttackAngleForMountedGuns, true);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.UseVehicleAttackIfVehicleHasMountedGuns, true);
            guardPedOnVehicle.SetConfigFlag(PedConfigFlagToggles.WillNotHotwireLawEnforcementVehicle, false);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, true);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, true);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.CanUseCover, true);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.AlwaysFlee, false);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, true);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);
            guardPedOnVehicle.SetCombatAttribute(CombatAttributes.DisableBulletReactions, true);
            guardPedOnVehicle.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, true);
            guardPedOnVehicle.SetConfigFlag(PedConfigFlagToggles.CanDiveAwayFromApproachingVehicles, true);
            guardPedOnVehicle.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, true);
            guardPedOnVehicle.SetConfigFlag(PedConfigFlagToggles.AIDriverAllowFriendlyPassengerSeatEntry, true);
            //guardPed.SetCombatAttribute(CombatAttributes.PerfectAccuracy, false);
            guardPedOnVehicle.FiringPattern = FiringPattern.FullAuto;
            guardPedOnVehicle.ShootRate = 999;
            //guardPed.FireVehicleWeaponAt(Game.Player.Character);
            //guardPed.SetConfigFlag(PedConfigFlagToggles.CanAttackNonWantedPlayerAsLaw, true);
            guardVehicle.LockStatus = VehicleLockStatus.CanBeBrokenInto;
            guardVehicle.Heading = Heading;

            RelationshipCrapSetups();
        }
    }

    public void Despawn()
    {
        // Check if it's a pedestrian entity
        if (Type == "ped" && guardPed != null && guardPed.Exists())
        {
            guardPed.Delete();
            Logger.Log($"Guard ped despawned at position {Position}.");
        }
        // Check if it's a vehicle entity
        else if (guardVehicle != null && guardVehicle.Exists() && guardVehicle is Vehicle && guardVehicle.EntityType == EntityType.Vehicle)
        {
            guardVehicle.Delete();
            Logger.Log($"Guard vehicle despawned at position {Position}.");
        }
        // Handle any unclassified or invalid entities
        else
        {
            Logger.Log($"Failed to despawn guard at position {Position}. Type: {Type}");
        }
    }
}