using GTA.Math;
using System.Collections.Generic;

public class Area
{
    public string Name { get; set; } // Area Name
    public string Model { get; set; } // Guard model for the area
    public List<GuardSpawnPoint> SpawnPoints { get; set; } // Spawn Points

    public Area(string name, string model)
    {
        Name = name; // Set area name
        Model = model; // Set guard model
        SpawnPoints = new List<GuardSpawnPoint>(); // Initialize spawn points list
    }

    public void AddSpawnPoint(Vector3 position, float heading) // Add spawn point
    {
        SpawnPoints.Add(new GuardSpawnPoint(position, heading)); // Add spawn point to list
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
