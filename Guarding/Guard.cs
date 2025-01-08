using GTA;
using GTA.Math;
using System;
using System.Collections.Generic;

public class Guard
{
    public Vector3 Position { get; set; } // Guard position
    public float Heading { get; set; } // Guard heading
    public GuardInfo GuardInfo { get; set; } // Guard info
    private Ped guardPed; // The actual guard ped

    // Define the relationship group for guards
    private static readonly RelationshipGroup guardGroup = World.AddRelationshipGroup("SC_GUARD");

    // Constructor to initialize guard properties
    public Guard(Vector3 position, float heading, GuardInfo guardInfo)
    {
        Position = position;
        Heading = heading;
        GuardInfo = guardInfo;
    }

    // Method to spawn the guard
    public void Spawn()
    {
        // Get a random guard model from the list for this guard type
        var randomModel = GuardInfo.PedModels[new Random().Next(GuardInfo.PedModels.Count)];
        var model = new Model(randomModel);

        if (!model.IsLoaded)
        {
            model.Request();
            while (!model.IsLoaded) Script.Wait(10);
        }

        // Create the guard ped
        guardPed = World.CreatePed(model, Position); // Create guard with model
        guardPed.Heading = Heading; // Set heading
        guardPed.Task.GuardCurrentPosition(); // Guard specific area

        // Equip guard with a random weapon from the list
        var randomWeapon = GuardInfo.Weapons[new Random().Next(0, GuardInfo.Weapons.Count)];
        guardPed.Weapons.Give(randomWeapon, 300, true, true);

        guardPed.Armor = 100; // Set armor to 100
        guardPed.CanSufferCriticalHits = true; // Disable critical hits

        // Set guard relationships
        guardPed.RelationshipGroup = guardGroup; // Assign the relationship group to the spawned guard
        guardGroup.SetRelationshipBetweenGroups(guardGroup, Relationship.Companion); // Guards are companions with their own kind
        guardGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Respect); // Guards respect the player
        Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(guardGroup, Relationship.Respect); // Player respects the guards

        Logger.Log($"Guard spawned at position {Position} with model {randomModel}.");
    }

    // Method to remove the guard
    public bool IsRemoved { get; private set; } = false;

    public void Remove()
    {
        try
        {
            guardPed?.Delete();
            guardPed = null;
            IsRemoved = true;
            Logger.Log("Guard successfully removed");
        }
        catch (Exception ex)
        {
            Logger.Log($"Error removing guard: {ex.Message}");
        }
    }
}