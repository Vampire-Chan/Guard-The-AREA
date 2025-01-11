using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;

public class Guard
{
    public Vector3 Position { get; set; } // Guard position
    public float Heading { get; set; } // Guard heading
    public string AreaName { get; set; } // Area name
    public string Model { get; set; } // Guard model

    public Ped guardPed;

    // Define multiple models for each guard type (e.g., Gruppe6, Police, etc.)
    private static readonly Dictionary<string, List<PedHash>> GuardModels = new Dictionary<string, List<PedHash>>()
    {
        { "gruppe6_guard", new List<PedHash> { PedHash.Armoured01SMM, PedHash.Armoured02SMM } },
        { "police_guard", new List<PedHash> { PedHash.Cop01SMY, PedHash.Swat01SMY, PedHash.Cop01SFY } },
        { "military_guard", new List<PedHash> { PedHash.Marine01SMY, PedHash.Marine02SMY, PedHash.Marine03SMY } }
    };

    public Guard(GTA.Math.Vector3 position, float heading, string model, string areaName)
    {
        Position = position;
        Heading = heading;
        Model = model;
        AreaName = areaName;
    }


    private static RelationshipGroup guardGroup = StringHash.AtStringHash("SC_GUARD");

    // Spawn the guard at the given position and heading
    public void Spawn()
    {
        {
            // Get a random guard model from the list for this guard type
            PedHash modelHash = GetRandomPedModelForGuard(Model);

            // Guard spawn logic (adjust based on your framework or game engine)
            guardPed = World.CreatePed(modelHash, Position); // Create guard with model
            guardPed.Heading = Heading; // Set heading
            guardPed.Task.GuardCurrentPosition(); // Guard specific area

            guardPed.Weapons.Give(WeaponHash.CarbineRifle, 400, true, true); // Give weapon (e.g., pistol)
            guardPed.Armor = 400; // Set armor to 400
            guardPed.Health = 400; // Set health to 400
            guardPed.Weapons.Select(WeaponHash.CarbineRifle); // Select the weapon

            guardPed.CanSufferCriticalHits = true; // Disable critical hits
            guardPed.CombatAbility = CombatAbility.Professional;
            guardPed.CombatMovement = CombatMovement.WillAdvance;
            guardPed.CombatRange = CombatRange.Medium;
            guardPed.FiringPattern = FiringPattern.FullAuto;
            guardPed.Accuracy = 100;
            guardPed.ShootRate = 500;

            Function.Call(Hash.SET_PED_RANDOM_PROPS, guardPed);

            // Create and set up the guard relationship group
            guardGroup = World.AddRelationshipGroup("SC_GUARD");

            // Set guard relationships
            guardGroup.SetRelationshipBetweenGroups(guardGroup, Relationship.Companion); // Guards are companions with their own kind
            guardGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Respect); // Guards respect the player

            // Assign the relationship group to the spawned guard
            guardPed.RelationshipGroup = guardGroup;

            // Make the player respect the guard group
            Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(guardGroup, Relationship.Respect); // Player respects the guards


            Logger.Log($"Guard spawned at position {Position} with model {modelHash}.");
            //guardPed.RelationshipGroup = RelationshipGroup.Hate; // Optional: set relationship group

        }
    }
    public void Despawn()
    {
        // Despawn logic (adjust based on your framework or game engine)
        if (guardPed != null || guardPed.Exists())
            guardPed.Delete();

        Logger.Log($"Guard despawned at position {Position}.");
    }

    // Get a random Ped model hash for the specified guard model
    private PedHash GetRandomPedModelForGuard(string modelName)
    {
        if (GuardModels.ContainsKey(modelName))
        {
            List<PedHash> availableModels = GuardModels[modelName]; // Get list of available models for the guard type
            Random rand = new Random();
            int index = rand.Next(availableModels.Count); // Select a random model from the list
            Logger.Log($"Selected model {availableModels[index]} for guard type {modelName}.");
            return availableModels[index]; // Return the randomly selected model
        }

        // Default model if not recognized
        return PedHash.Security01SMM;
    }
}