using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;

public class Guard
{
    public Vector3 Position { get; set; }
    public float Heading { get; set; }
    public string AreaName { get; set; }
    public GuardConfig Config { get; set; }
    public string Type { get; set; }

    public Ped guardPed;
    public Vehicle guardVehicle;

    public Guard(Vector3 position, float heading, GuardConfig config, string areaName, string type)
    {
        Position = position;
        Heading = heading;
        Config = config ?? throw new ArgumentNullException(nameof(config), "Config cannot be null");
        AreaName = areaName ?? throw new ArgumentNullException(nameof(areaName), "Area name cannot be null");
        Type = type ?? throw new ArgumentNullException(nameof(type), "Type cannot be null");
    }

    private static RelationshipGroup guardGroup = World.AddRelationshipGroup("SC_GUARD");

    public void Spawn()
    {
        // Log the initial state
        Logger.Log($"Spawning guard at position {Position}, heading {Heading}, area {AreaName}, type {Type}");

        if (Type == "ped")
        {
            if (Config.PedModels == null || Config.PedModels.Count == 0)
            {
                throw new InvalidOperationException("PedModels must be defined in the configuration");
            }

            var rand = new Random();
            string modelName = Config.PedModels[rand.Next(Config.PedModels.Count)];
            Logger.Log($"Selected ped model: {modelName}");

            guardPed = World.CreatePed(modelName, Position);
            if (guardPed == null)
            {
                throw new InvalidOperationException($"Failed to create ped with model {modelName}");
            }

            guardPed.Heading = Heading;
            guardPed.Task.GuardCurrentPosition();

            foreach (var weapon in Config.Weapons)
            {
                guardPed.Weapons.Give(weapon, 400, true, true);
            }

            guardPed.Armor = 400;
            guardPed.DiesOnLowHealth = false;
            guardPed.MaxHealth = 400;
            guardPed.Health = 400;
            


            guardPed.CanSufferCriticalHits = true;
            guardPed.CombatAbility = CombatAbility.Professional;
            guardPed.CombatMovement = CombatMovement.WillAdvance;
            guardPed.CombatRange = CombatRange.Medium;
            guardPed.FiringPattern = FiringPattern.FullAuto;
            guardPed.Accuracy = 200;
            guardPed.ShootRate = 1000;
            guardPed.SetConfigFlag(PedConfigFlagToggles.NoCriticalHits, true);
            guardPed.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, true);
            //guardPed.SetConfigFlag(PedConfigFlagToggles.DisableHelmetArmor, false);
            guardPed.SetConfigFlag(PedConfigFlagToggles.CanDiveAwayFromApproachingVehicles, true);
            guardPed.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, true);
            //guardPed.SetConfigFlag(PedConfigFlagToggles.HasBulletProofVest, true);
            guardPed.SetConfigFlag(PedConfigFlagToggles.ActivateRagdollFromMinorPlayerContact, false);
            //guardPed.SetConfigFlag(PedConfigFlagToggles.DontActivateRagdollFromBulletImpact, true);

            Function.Call(Hash.SET_PED_RANDOM_PROPS, guardPed);

            if (guardPed.PedType == PedType.Cop || guardPed.PedType == PedType.Swat || guardPed.PedType == PedType.Army)
            {
                //donothing
            }
            else
            {
                guardPed.RelationshipGroup = guardGroup;

                guardGroup.SetRelationshipBetweenGroups(guardGroup, Relationship.Companion);
                guardGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Respect);

                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(guardGroup, Relationship.Respect);
            }
            Logger.Log($"Guard spawned at position {Position} with model {modelName}.");
        }
        else if (Type == "vehicle")
        {
            if (Config.VehicleModels == null || Config.VehicleModels.Count == 0)
            {
                throw new InvalidOperationException("VehicleModels must be defined in the configuration");
            }

            var rand = new Random();
            string vehicleModelName = Config.VehicleModels[rand.Next(Config.VehicleModels.Count)];
            Logger.Log($"Selected vehicle model: {vehicleModelName}");

            guardVehicle = World.CreateVehicle(vehicleModelName, Position);
            if (guardVehicle == null)
            {
                throw new InvalidOperationException($"Failed to create vehicle with model {vehicleModelName}");
            }

            guardVehicle.Heading = Heading;
            guardVehicle.IsPersistent = true;
            guardVehicle.EngineHealth = 2000;

            Logger.Log($"Vehicle spawned at position {Position} with model {vehicleModelName}.");
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
            //if (!guardVehicle.Driver.IsAlive)
            guardVehicle.Delete();

            Logger.Log($"Guard vehicle despawned at position {Position}.");
        }
        else
        {
            Logger.Log($"Failed to despawn guard at position {Position}. Type: {Type}");
        }
    }
}