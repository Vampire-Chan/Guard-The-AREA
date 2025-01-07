using GTA;
using System.Collections.Generic;

public class GuardSpawner
{
    private List<Area> _areas; // List of areas

    public GuardSpawner(string xmlFilePath)
    {
        _areas = new XmlReader(xmlFilePath).LoadAreasFromXml(); // Load areas from XML
        Logger.Log($"Loaded {_areas.Count} areas from XML."); // Log the number of areas loaded
    }

    public void CheckPlayerProximityAndSpawn(Player player) // Check player's proximity
    {
        foreach (var area in _areas) // Iterate through each area
        {   
            Logger.Log($"Checking area {area.Name} for player proximity..."); // Log area check
            foreach (var spawnPoint in area.SpawnPoints) // Check each spawn point
            {
                Logger.Log($"Checking spawn point {spawnPoint.Position}..."); // Log spawn point check
                float distance = player.Character.Position.DistanceTo(spawnPoint.Position); // Calculate distance
                Logger.Log($"Distance to spawn point: {distance}"); // Log distance
                if (distance < 100f) // Player is within 100m
                {
                    SpawnGuards(area); // Spawn guards for the area
                    Logger.Log($"Guards trying to spawn in area {area.Name}"); // Log guards spawned
                    break; // Stop checking after spawning guards
                }
            }
        }
    }

    private void SpawnGuards(Area area) // Spawn all guards in area
    {
        foreach (var spawnPoint in area.SpawnPoints) // Iterate through spawn points
        {
            Guard guard = new Guard(spawnPoint.Position, spawnPoint.Heading, area.Model, area.Name); // Create guard
            Logger.Log("Initializing guard..."); // Log guard initialization
            if (guard.CanRespawn() && !guard.IsGuardPresent()) // Guard can respawn and is not present
                guard.Spawn(); // Spawn the guard
        }
    }
}
