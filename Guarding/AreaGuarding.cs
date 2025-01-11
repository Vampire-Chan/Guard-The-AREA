// In AreaGuarding.cs
using GTA.Math;
using System.Collections.Generic;

public class Area
{
    public string Name { get; set; }
    public string Model { get; set; }
    public List<GuardSpawnPoint> SpawnPoints { get; set; }

    public Area(string name, string model)
    {
        Name = name;
        Model = model;
        SpawnPoints = new List<GuardSpawnPoint>();
    }

    public void AddSpawnPoint(Vector3 position, float heading, string type)
    {
        SpawnPoints.Add(new GuardSpawnPoint(position, heading, type));
    }
}

public class GuardSpawnPoint
{
    public Vector3 Position { get; set; }
    public float Heading { get; set; }
    public string Type { get; set; }

    public GuardSpawnPoint(Vector3 position, float heading, string type)
    {
        Position = position;
        Heading = heading;
        Type = type;
    }
}