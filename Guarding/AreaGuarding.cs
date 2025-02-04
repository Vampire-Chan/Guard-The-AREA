﻿// In AreaGuarding.cs
using GTA.Math;
using System.Collections.Generic;

public class Area
{
    public string Name { get; set; }
    public string Model { get; set; }
    public List<string> Hate { get; set; }
    public List<string> Dislike { get; set; }
    public string Respect { get; set; }
    public List<string> Like { get; set; }
    public bool RelationshipOverride { get; set; }
    public List<GuardSpawnPoint> SpawnPoints { get; set; }

    public Area(string name, string model, List<string> hate, List<string> dislike, string respect, List<string>like,  bool relationshipOverride = false)
    {
        Name = name;
        Model = model;
        SpawnPoints = new List<GuardSpawnPoint>();
        Hate = hate;
        Dislike = dislike;
        Respect = respect;
        Like = like;
        RelationshipOverride = relationshipOverride;
    }

    public Vector3 GetCentroid()
    {
        if (SpawnPoints == null || SpawnPoints.Count == 0)
            return Vector3.Zero;

        float sumX = 0, sumY = 0, sumZ = 0;
        foreach (var point in SpawnPoints)
        {
            sumX += point.Position.X;
            sumY += point.Position.Y;
            sumZ += point.Position.Z;
        }

        return new Vector3(
            sumX / SpawnPoints.Count,
            sumY / SpawnPoints.Count,
            sumZ / SpawnPoints.Count
        );
    }


    public void AddSpawnPoint(Vector3 position, float heading, string type, string scenario, bool interior)
    {
        SpawnPoints.Add(new GuardSpawnPoint(position, heading, type, scenario, interior));
    }
}

public class GuardSpawnPoint
{
    public Vector3 Position { get; set; }
    public float Heading { get; set; }
    public string Type { get; set; }
    public string Scenario { get; set; }

    public bool Interior { get; set; }

    public GuardSpawnPoint(Vector3 position, float heading, string type, string scenario, bool interior)
    {
        Position = position;
        Heading = heading;
        Type = type;
        Scenario = scenario;
        Interior = interior;
    }
}