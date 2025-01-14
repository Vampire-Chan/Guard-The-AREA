using GTA;
using GTA.Math;
using GTA.Native;
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
    public Vector3 Position { get; set; }
    public float Heading { get; set; }
    public string AreaName { get; set; }
    public string Type { get; set; }
    private readonly Area _area;
    private readonly GuardConfig _guardConfig;
    private readonly RelationshipManager _relationshipsArea;
    private readonly RelationshipManager _relationshipsGuard;

    public Ped guardPed;
    public Vehicle guardVehicle;

    // New fields for combat tracking
    private Vector3 _originalPosition;
    private readonly float _originalHeading;
    private const float RETURN_THRESHOLD = 0.1f; // Distance threshold for considering ped "returned"
    private const int COMBAT_CHECK_DELAY = 100; // Time to wait before checking if combat is truly over

    private string VehicleModelName;
    private string PedModelName;
    private string WeaponName;
    private readonly GuardConfig GuardConfig;
    private string Scenario; //with value the original

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

    public Guard(Vector3 position, float heading, string areaName, string type, GuardConfig guardConfig, string scenario, Area area)
    {
        Position = position;
        Heading = heading;
        AreaName = areaName ?? throw new ArgumentNullException(nameof(areaName));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        _guardConfig = guardConfig;
        _area = area;
        Scenario = scenario;

        // Initialize combat-related fields
        _originalPosition = position;
        _originalHeading = heading;

        // Initialize relationships
        _relationshipsArea = new RelationshipManager(_area.Hate, _area.Dislike, _area.Respect, _area.Like);
        _relationshipsGuard= new RelationshipManager(_guardConfig.Hate, _guardConfig.Dislike,
            _guardConfig.Respect, _guardConfig.Like);

        RandomizeLoadout();
    }

    public  Relationship GetRelationshipBetweenGroups(int group1, int group2)
    {
        return (Relationship)Function.Call<int>(Hash.GET_RELATIONSHIP_BETWEEN_GROUPS, group1, group2);
    }
    public void SetRelationshipBetweenGroups(Relationship relationship, int group1, int group2)
    {
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, (int)relationship, group1, group2);
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, (int)relationship, group2, group1);
    }
    //    private void ApplyRelationships()
    //    {
    //        if (guardPed == null || !guardPed.Exists())
    //            return;
    ////original guard respect each other
    //        guardGroup.SetRelationshipBetweenGroups(guardGroup, Relationship.Companion, true);

    //        //from here we grab the relationship lists and as per that 
    //        if (_area.RelationshipOverride)
    //        {
    //            if (_relationshipsArea.Hate != null || _relationshipsArea.Hate.Contains("null"))
    //            {
    //                foreach (var grp in _relationshipsArea.Hate)
    //                {
    //                    guardGroup.SetRelationshipBetweenGroups(grp, Relationship.Hate, true); //last param means bidirectonally
    //                }

    //            }
    //            //respect
    //            if (_relationshipsArea.Respect != null || _relationshipsArea.Respect.Contains("null"))
    //            {
    //                foreach (var grp in _relationshipsArea.Respect)
    //                {
    //                    guardGroup.SetRelationshipBetweenGroups(grp, Relationship.Respect, true); //last param means bidirectonally
    //                }

    //            }
    //            //like
    //            if (_relationshipsArea.Like != null || _relationshipsArea.Like.Contains("null"))
    //            {
    //                foreach (var grp in _relationshipsArea.Like)
    //                {
    //                    guardGroup.SetRelationshipBetweenGroups(grp, Relationship.Like, true); //last param means bidirectonally
    //                }

    //            }
    //            //dislike
    //            if (_relationshipsArea.Dislike != null || _relationshipsArea.Dislike.Contains("null"))
    //            {
    //                foreach (var grp in _relationshipsArea.Dislike)
    //                {
    //                    guardGroup.SetRelationshipBetweenGroups(grp, Relationship.Dislike, true); //last param means bidirectonally
    //                }

    //            }


    //            //

    //        }
    //        else

    //        {
    //            if (_relationshipsArea.Hate != null || _relationshipsArea.Hate.Contains("null"))
    //            {
    //                foreach (var grp in _relationshipsArea.Hate)
    //                {
    //                    guardGroup.SetRelationshipBetweenGroups(grp, Relationship.Hate, true); //last param means bidirectonally
    //                }

    //            }
    //            //respect
    //            if (_relationshipsArea.Respect != null || _relationshipsArea.Respect.Contains("null"))
    //            {
    //                foreach (var grp in _relationshipsArea.Respect)
    //                {
    //                    guardGroup.SetRelationshipBetweenGroups(grp, Relationship.Respect, true); //last param means bidirectonally
    //                }

    //            }
    //            //like
    //            if (_relationshipsArea.Like != null || _relationshipsArea.Like.Contains("null"))
    //            {
    //                foreach (var grp in _relationshipsArea.Like)
    //                {
    //                    guardGroup.SetRelationshipBetweenGroups(grp, Relationship.Like, true); //last param means bidirectonally
    //                }

    //            }
    //            //dislike
    //            if (_relationshipsArea.Dislike != null || _relationshipsArea.Dislike.Contains("null"))
    //            {
    //                foreach (var grp in _relationshipsArea.Dislike)
    //                {
    //                    guardGroup.SetRelationshipBetweenGroups(grp, Relationship.Dislike, true); //last param means bidirectonally
    //                }

    //            }
    //            if (_relationshipsGuard.Hate != null || _relationshipsGuard.Hate.Contains("null"))
    //            {
    //                foreach (var grp in _relationshipsGuard.Hate)
    //                {
    //                    guardGroup.SetRelationshipBetweenGroups(grp, Relationship.Hate, true); //last param means bidirectonally
    //                }

    //            }
    //            //respect
    //            if (_relationshipsGuard.Respect != null || _relationshipsGuard.Respect.Contains("null"))
    //            {
    //                foreach (var grp in _relationshipsGuard.Respect)
    //                {
    //                    guardGroup.SetRelationshipBetweenGroups(grp, Relationship.Respect, true); //last param means bidirectonally
    //                }

    //            }
    //            //like
    //            if (_relationshipsGuard.Like != null || _relationshipsGuard.Like.Contains("null"))
    //            {
    //                foreach (var grp in _relationshipsGuard.Like)
    //                {
    //                    guardGroup.SetRelationshipBetweenGroups(grp, Relationship.Like, true); //last param means bidirectonally
    //                }

    //            }
    //            //dislike
    //            if (_relationshipsGuard.Dislike != null || _relationshipsGuard.Dislike.Contains("null"))
    //            {
    //                foreach (var grp in _relationshipsGuard.Dislike)
    //                {
    //                    guardGroup.SetRelationshipBetweenGroups(grp, Relationship.Dislike, true); //last param means bidirectonally
    //                }

    //            }

    //        }




    //        Logger.Log($"Applied relationships for guard in area {AreaName}. Override: {_area.RelationshipOverride}");
    //    }

    private void ApplyRelationships()
    {
        if (guardPed == null || !guardPed.Exists())
            return;

        // Apply relationships for guard
        ApplyGroupRelationships(_relationshipsGuard);

        // Apply relationships for area, if the override is false or relationships are defined
        if (!_area.RelationshipOverride)
        {
            ApplyGroupRelationships(_relationshipsArea);
        }

        Logger.Log($"Applied relationships for guard in area {AreaName}. Override: {_area.RelationshipOverride}");
    }

    private void ApplyGroupRelationships(RelationshipManager relationships)
    {
        // Check and apply 'Hate' relationships
        if (relationships.Hate != null && !relationships.Hate.Contains("none"))
        {
            foreach (var grp in relationships.Hate)
            {
                // Apply relationship to players based on specific conditions
                if (grp == "PLAYER_ZERO")  // Michael
                {
                    guardGroup.SetRelationshipBetweenGroups(Michael, Relationship.Hate, true);
                }
                else if (grp == "PLAYER_ONE")  // Franklin
                {
                    guardGroup.SetRelationshipBetweenGroups(Franklin, Relationship.Hate, true);
                }
                else if (grp == "PLAYER_TWO")  // Trevor
                {
                    guardGroup.SetRelationshipBetweenGroups(Trevor, Relationship.Hate, true);
                }
                else
                {
                    guardGroup.SetRelationshipBetweenGroups(grp, Relationship.Hate, true);
                }
            }
        }

        // Check and apply 'Respect' relationships
        if (relationships.Respect != null && !relationships.Respect.Contains("none"))
        {
            foreach (var grp in relationships.Respect)
            {
                // Apply relationship to players based on specific conditions
                if (grp == "PLAYER_ZERO")  // Michael
                {
                    guardGroup.SetRelationshipBetweenGroups(Michael, Relationship.Respect, true);
                }
                else if (grp == "PLAYER_ONE")  // Franklin
                {
                    guardGroup.SetRelationshipBetweenGroups(Franklin, Relationship.Respect, true);
                }
                else if (grp == "PLAYER_TWO")  // Trevor
                {
                    guardGroup.SetRelationshipBetweenGroups(Trevor, Relationship.Respect, true);
                }
                else
                {
                    guardGroup.SetRelationshipBetweenGroups(grp, Relationship.Respect, true);
                }
            }
        }

        // Check and apply 'Like' relationships
        if (relationships.Like != null && !relationships.Like.Contains("none"))
        {
            foreach (var grp in relationships.Like)
            {
                // Apply relationship to players based on specific conditions
                if (grp == "PLAYER_ZERO")  // Michael
                {
                    guardGroup.SetRelationshipBetweenGroups(Michael, Relationship.Like, true);
                }
                else if (grp == "PLAYER_ONE")  // Franklin
                {
                    guardGroup.SetRelationshipBetweenGroups(Franklin, Relationship.Like, true);
                }
                else if (grp == "PLAYER_TWO")  // Trevor
                {
                    guardGroup.SetRelationshipBetweenGroups(Trevor, Relationship.Like, true);
                }
                else
                {
                    guardGroup.SetRelationshipBetweenGroups(grp, Relationship.Like, true);
                }
            }
        }

        // Check and apply 'Dislike' relationships
        if (relationships.Dislike != null && !relationships.Dislike.Contains("none"))
        {
            foreach (var grp in relationships.Dislike)
            {
                // Apply relationship to players based on specific conditions
                if (grp == "PLAYER_ZERO")  // Michael
                {
                    guardGroup.SetRelationshipBetweenGroups(Michael, Relationship.Dislike, true);
                }
                else if (grp == "PLAYER_ONE")  // Franklin
                {
                    guardGroup.SetRelationshipBetweenGroups(Franklin, Relationship.Dislike, true);
                }
                else if (grp == "PLAYER_TWO")  // Trevor
                {
                    guardGroup.SetRelationshipBetweenGroups(Trevor, Relationship.Dislike, true);
                }
                else
                {
                    guardGroup.SetRelationshipBetweenGroups(grp, Relationship.Dislike, true);
                }
            }
        }
    }

    private static readonly Random rand = new Random(); // Single Random instance

    private void RandomizeLoadout()
    {
        PedModelName = GetRandomElement(GuardConfig.PedModels);
        WeaponName = GetRandomElement(GuardConfig.Weapons);
        VehicleModelName = GetRandomElement(GuardConfig.VehicleModels);
        Scenario = GetRandomElement(MainScript.scenarios);
    }

    private static T GetRandomElement<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("List cannot be null or empty");
        return list[rand.Next(list.Count)];
    }

    private static RelationshipGroup guardGroup = World.AddRelationshipGroup("SC_GUARD");

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
            if (!guardPed.IsInCombat && !guardPed.IsShooting)
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

        // Clear tasks and return to original position
        guardPed.Task.ClearAllImmediately();

        // First move back to original position

        Script.Wait(1000); // Give time for movement to start

        // Check periodically if guard has returned to position
        if (guardPed.Exists() && guardPed.Position.DistanceTo(_originalPosition) < RETURN_THRESHOLD)
        {

            // Reset heading
            if (guardPed.Heading != _originalHeading)
                guardPed.Heading = _originalHeading;

            // guardPed.Task.GuardCurrentPosition();
            guardPed.Task.StartScenarioInPlace(Scenario);
            Logger.Log("Guard resumed default guard position");

        }

        else if (guardPed.IsIdle && guardPed.IsAlive && !guardPed.IsRagdoll && !guardPed.IsInAir && !guardPed.IsClimbing && !guardPed.IsFalling && !guardPed.IsShooting && !guardPed.IsInCombat)
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

            guardPed.CombatAbility = CombatAbility.Professional;
            guardPed.CombatMovement = CombatMovement.WillAdvance;
            guardPed.CombatRange = CombatRange.Medium;
            guardPed.FiringPattern = FiringPattern.FullAuto;
            guardPed.Accuracy = 200;
            guardPed.ShootRate = 1000;

            guardPed.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, true);
            guardPed.SetConfigFlag(PedConfigFlagToggles.CanDiveAwayFromApproachingVehicles, true);
            guardPed.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, true);
            guardPed.SetConfigFlag(PedConfigFlagToggles.AIDriverAllowFriendlyPassengerSeatEntry, true);

            OutputArgument groundZArg = new OutputArgument();
            Function.Call(Hash.SET_PED_RANDOM_PROPS, guardPed);
            float groundZ = 0.0f;

            Function.Call(Hash.GET_GROUND_Z_FOR_3D_COORD, Position.X, Position.Y, Position.Z + 30, groundZArg, false, false);
            guardPed.IsCollisionEnabled = true;

            groundZ = groundZArg.GetResult<float>();

            // Determine the scenario to use
          
            if (Scenario.ToLower() == "none" && Scenario.ToLower() == "false")
            {
                guardPed.Task.StartScenarioInPlace(Scenario);
            }
            else if (Scenario.ToLower() == "guard" || Scenario.ToLower() == "guardpatrol" || Scenario.ToLower() == "taskguard" || Scenario.ToLower() == "task_guard") 
            {
                guardPed.Task.GuardCurrentPosition();
            }
            else
            {
                guardPed.Task.StartScenarioInPlace(Scenario);
            }

            ApplyRelationships();

            if (guardPed.PedType == PedType.Cop || guardPed.PedType == PedType.Swat || guardPed.PedType == PedType.Army)
            {
                // Do nothing for law enforcement
                guardPed.SetConfigFlag(PedConfigFlagToggles.LawWillOnlyAttackIfPlayerIsWanted, true);
                guardPed.SetConfigFlag(PedConfigFlagToggles.WillNotHotwireLawEnforcementVehicle, false);
            }
            else
            {
            //    guardPed.RelationshipGroup = guardGroup;

                //guardGroup.SetRelationshipBetweenGroups(guardGroup, Relationship.Companion);
                //guardGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Respect);

                //Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(guardGroup, Relationship.Respect);
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
            guardVehicle.IsPersistent = true;
            guardVehicle.LockStatus = VehicleLockStatus.CanBeBrokenInto;
            guardVehicle.EngineHealth = 2000;

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
            guardVehicle.IsPersistent = true;
            guardVehicle.LockStatus = VehicleLockStatus.CanBeBrokenInto;
            guardVehicle.EngineHealth = 2000;

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
            guardVehicle.IsPersistent = true;
            guardVehicle.LockStatus = VehicleLockStatus.CanBeBrokenInto;
            guardVehicle.EngineHealth = 2000;

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