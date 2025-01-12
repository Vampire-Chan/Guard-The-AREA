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
    public string Type { get; set; }
    public Ped guardPed;
    public Vehicle guardVehicle;
    private string VehicleModelName;
    private string PedModelName;
    private string WeaponName;

    Random rand = new Random();
    public Guard(Vector3 position, float heading, string areaName, string type, string vehicle, string ped, string weapon)
    {
        Position = position;
        Heading = heading;
        AreaName = areaName ?? throw new ArgumentNullException(nameof(areaName), "Area name cannot be null");
        Type = type ?? throw new ArgumentNullException(nameof(type), "Type cannot be null");
        VehicleModelName = vehicle;
        PedModelName = ped;
        WeaponName = weapon;
    }

    private static RelationshipGroup guardGroup = World.AddRelationshipGroup("SC_GUARD");

    public string scenario;

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

            guardPed.Heading = Heading;
            guardPed.Weapons.Give(WeaponName, 400, true, true);

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
            guardPed.SetConfigFlag(PedConfigFlagToggles.ActivateRagdollFromMinorPlayerContact, false);



            Function.Call(Hash.SET_PED_RANDOM_PROPS, guardPed);

            // Randomly select a scenario from the list
            string selectedScenario = scenario;

            // Start the in-place scenario (active by default)
            guardPed.Task.StartScenarioInPlace(selectedScenario);

            //guardPed.Task.PlayAnimation(, crClipName, 8f, 1f, -1, AnimationFlags.Loop, 0f);
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

            // Uncomment the block below to test the "at-position" scenario instead
            //guardPed.Task.StartScenarioAtPosition(selectedScenario, Position*2, Heading);

            Logger.Log($"Guard spawned with scenario {selectedScenario}.");
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
