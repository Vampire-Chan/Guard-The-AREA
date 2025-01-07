using GTA;
using GTA.Math;
using System;
using System.Collections.Generic;

public class Guard
{
    public Vector3 Position { get; set; } // Guard position
    public float Heading { get; set; } // Guard heading
    public string AreaName { get; set; } // Area name
    public string Model { get; set; } // Guard model
    public int LastSpawnTime { get; set; } // Last time the guard was spawned (using Game.GameTime)
    private const int CooldownDuration = 30000; // Cooldown duration in milliseconds (30 seconds)

    // Define multiple models for each guard type (e.g., Gruppe6, Police, etc.)
    private static readonly Dictionary<string, List<PedHash>> GuardModels = new Dictionary<string, List<PedHash>>()
    {
        { "gruppe6_guard", new List<PedHash> { PedHash.Armoured01SMM, PedHash.Armoured02SMM } },
        { "police_guard", new List<PedHash> { PedHash.Cop01SMY, PedHash.Swat01SMY, PedHash.Cop01SFY } },
        { "military_guard", new List<PedHash> { PedHash.Marine01SMY, PedHash.Marine02SMY } }
    };

    public Guard(GTA.Math.Vector3 position, float heading, string model, string areaName)
    {
        Position = position;
        Heading = heading;
        Model = model;
        AreaName = areaName;
        LastSpawnTime = 0; // Initialize last spawn time
    }


    private static RelationshipGroup guardGroup = StringHash.AtStringHash("SC_GUARD");

    // Check if the guard can respawn based on cooldown
    public bool CanRespawn()
    {
        Logger.Log($"Checking if guard can respawn in area {AreaName} and checking timer {Game.GameTime} vs {LastSpawnTime}...");
        return (Game.GameTime - LastSpawnTime) >= CooldownDuration; // Compare time difference using Game.GameTime
    }

    // Spawn the guard at the given position and heading
    public void Spawn()
    {
        if (CanRespawn()) // Check if cooldown has passed
        {
            // Check if the guard is already present at the spawn position
            if (!IsGuardPresent())
            {
                // Get a random guard model from the list for this guard type
                PedHash modelHash = GetRandomPedModelForGuard(Model);

                // Guard spawn logic (adjust based on your framework or game engine)
                Ped guardPed = World.CreatePed(modelHash, Position); // Create guard with model
                guardPed.Heading = Heading; // Set heading
                guardPed.Task.GuardCurrentPosition(); // Guard specific area

                guardPed.Weapons.Give(WeaponHash.CarbineRifle, 400, true, true); // Give weapon (e.g., pistol)
                guardPed.Armor = 100; // Set armor to 100
                guardPed.CanSufferCriticalHits = true; // Disable critical hits

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

                // Update the last spawn time after successfully spawning the guard
                LastSpawnTime = Game.GameTime; // Set last spawn time to current game time
            }
        }
    }

    // Check if a guard is already present at the spawn position
    public bool IsGuardPresent()
    {
        // Check if there is a guard within a small radius (e.g., 5 meters) of the spawn position
        Ped[] nearbyPeds = World.GetNearbyPeds(Position, 5f); // Get nearby peds within 5 meters

        foreach (var ped in nearbyPeds)
        {
            // Check if ped model is one of the models for this guard type
            if (GuardModels.ContainsKey(Model) && GuardModels[Model].Contains(ped.Model))
            {
                return true; // Guard is already present
            }
        }

        return false; // No guard present
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
