using GTA;
using System.Collections.Generic;
using System.Linq;

public class GuardSpawner
{
    private List<Area> _areas; // List of areas
    private List<Guard> _guards; // List of guards

    public GuardSpawner(string xmlFilePath)
    {
        XmlReader xml = new XmlReader(xmlFilePath);
        _areas = xml.LoadAreasFromXml(); // Load areas from XML
        _guards = new List<Guard>(); // Initialize guards list
        Logger.Log($"Loaded {_areas.Count} areas from XML."); // Log the number of areas loaded
    }
    private Area temparea = null;
    public void UnInitialize()
    {
        foreach(var guard in _guards)
        {
            if (guard.guardPed.Exists())
            {
                guard.Despawn();
            }
        }
        _guards.Clear();
    }

    public void CheckAllTime()
    {
        if (temparea != null)
        {
            foreach (var guard in _guards)
            {
                if (guard.guardPed.IsDead)
                {
                    guard.guardPed.MarkAsNoLongerNeeded(); //we keep in list so taht they dont spawn again
                }
                else if (!guard.guardPed.Exists())
                {
                    _guards.Remove(guard);
                }
            }

        }
    }
    public void CheckPlayerProximityAndSpawn(Player player) // Check player's proximity
    {
        CheckAllTime();
        foreach (var area in _areas) // Iterate through each area
        {
            temparea = area;
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
                else if (distance > 130f)
                {
                    DespawnGuards(area);
                    break;
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
            if (!_guards.Any(g => g.Position == guard.Position && g.AreaName == guard.AreaName)) // Check if guard is already present
            {
                _guards.Add(guard); // Add guard to list
                guard.Spawn(); // Spawn the guard
            }

        }
    }

    public void DespawnGuards(Area area)
    {
        var guardsToRemove = _guards.Where(g => g.AreaName == area.Name).ToList();
        foreach (var guard in guardsToRemove)
        {
            guard.Despawn();
            _guards.Remove(guard);
        }
    }
}