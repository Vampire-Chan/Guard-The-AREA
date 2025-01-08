using GTA;
using GTA.Math;
using System;
using System.Collections.Generic;

public class Area
{
    public string Name { get; set; } // Area Name
    public string Model { get; set; } // Guard model for the area
    public List<GuardSpawnPoint> SpawnPoints { get; set; } // Spawn Points
    public Dictionary<GuardSpawnPoint, Guard> GuardAssignments { get; set; } // Dictionary of spawn points and guards
    public int LastSpawnTime { get; set; } // Last time guards were spawned in this area
    public bool SpawnReady { get; set; } // Flag to check if spawning is allowed
    private const int CooldownDuration = 30000; // Cooldown duration in milliseconds (30 seconds)

    // Constructor to initialize area properties
    public Area(string name, string model)
    {
        Name = name; // Set area name
        Model = model; // Set guard model
        SpawnPoints = new List<GuardSpawnPoint>(); // Initialize spawn points list
        GuardAssignments = new Dictionary<GuardSpawnPoint, Guard>(); // Initialize guard assignments dictionary
        LastSpawnTime = 0; // Initialize last spawn time
        SpawnReady = true; // Initialize spawn ready flag
    }

    // Method to add a spawn point to the area
    public void AddSpawnPoint(Vector3 position, float heading)
    {
        var spawnPoint = new GuardSpawnPoint(position, heading);
        SpawnPoints.Add(spawnPoint); // Add spawn point to list
        GuardAssignments[spawnPoint] = null; // Initialize guard assignment for spawn point
    }

    // Method to check if the area can respawn guards
    public bool CanRespawn()
    {
        return (Game.GameTime - LastSpawnTime) >= CooldownDuration; // Compare time difference using Game.GameTime
    }

    // Method to update the last spawn time
    public void UpdateLastSpawnTime()
    {
        LastSpawnTime = Game.GameTime; // Update last spawn time to current game time
    }

    // Method to remove all guards from the area
    public void RemoveGuards()
    {
        try
        {
            foreach (var guard in GuardAssignments.Values)
            {
                guard?.Remove();
            }
            GuardAssignments.Clear(); // Clear all guard assignments
            Logger.Log($"All guards removed from area '{Name}'.");
        }
        catch (Exception ex)
        {
            Logger.Log($"Error removing guards from area '{Name}': {ex.Message}");
        }
    }
}

public class GuardSpawnPoint
{
    public Vector3 Position { get; set; } // Position
    public float Heading { get; set; } // Heading

    public GuardSpawnPoint(Vector3 position, float heading)
    {
        Position = position; // Set position
        Heading = heading; // Set heading
    }
}
