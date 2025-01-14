// GuardSpawner.cs
using GTA;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.Linq;

public class GuardSpawner
{
    private List<Area> _areas;
    private List<Guard> _guards;
    private List<Guard> _removedones;
    private Dictionary<string, GuardConfig> _guardConfigs;

    private const float SPAWN_DISTANCE = 150f;
    private const float DESPAWN_DISTANCE = 170f;

    public GuardSpawner(string xmlFilePath)
    {
        XmlReader xml = new XmlReader(xmlFilePath);
        _areas = xml.LoadAreasFromXml();
        _guardConfigs = xml.LoadGuardConfigs();
        _guards = new List<Guard>();
        _removedones = new List<Guard>();
        Logger.Log($"Loaded {_areas.Count} areas from XML.");
    }

    private Area temparea = null;

    public void CheckAllTime()
    {
        if (temparea != null)
        {
            foreach (var guard in _guards.ToList())
            {
                if (guard?.guardPed != null)
                {
                    // Check if guard is dead or no longer exists
                    if (guard.guardPed.IsDead || !guard.guardPed.Exists())
                    {
                        if (guard.guardPed != null)
                        {
                            guard.guardPed.MarkAsNoLongerNeeded();
                        }
                        _guards.Remove(guard);
                        _removedones.Add(guard);
                        continue;
                    }

                    // Update combat state for each guard
                    guard.UpdateCombatState();
                }

                // Check vehicle status
                if (guard?.guardVehicle != null)
                {
                    if (guard.guardVehicle.IsDead || !guard.guardVehicle.Exists() ||
                        guard.guardVehicle.Driver != null || guard.guardVehicle.PassengerCount != 0)
                    {
                        if (guard.guardVehicle != null)
                        {
                            guard.guardVehicle.MarkAsNoLongerNeeded();
                        }
                        _guards.Remove(guard);
                        _removedones.Add(guard);
                    }
                }
            }
        }
    }

    public void CheckPlayerProximityAndSpawn(Player player)
    {
        if (player?.Character == null)
        {
            Logger.Log("Player or Player.Character is null. Skipping proximity check.");
            return;
        }

        CheckAllTime();

        foreach (var area in _areas)
        {
            temparea = area;
            Vector3 areaCentroid = area.GetCentroid();
            float areaRadius = area.GetRadius();

            float distanceToCentroid = player.Character.Position.DistanceTo(areaCentroid);
            Logger.Log($"Checking area {area.Name}, distance to centroid: {distanceToCentroid}");

            // Adjust spawn/despawn distances based on area radius
            float adjustedSpawnDistance = SPAWN_DISTANCE + areaRadius;
            float adjustedDespawnDistance = DESPAWN_DISTANCE + areaRadius;

            if (distanceToCentroid < adjustedSpawnDistance)
            {
                SpawnGuards(area);
                Logger.Log($"Guards spawning in area {area.Name}");
            }
            else if (distanceToCentroid > adjustedDespawnDistance)
            {
                DespawnGuards(area);
            }
        }
    }

    private void SpawnGuards(Area area)
    {
        if (!_guardConfigs.ContainsKey(area.Model))
        {
            Logger.Log($"Guard model {area.Model} not found in configurations.");
            return;
        }

        var guardConfig = _guardConfigs[area.Model];

        foreach (var spawnPoint in area.SpawnPoints)
        {
            // Create guard with the config
            Guard guard = new Guard(spawnPoint.Position, spawnPoint.Heading,
           area.Name, spawnPoint.Type, guardConfig, spawnPoint.Scenario, area);

            // Check if the guard should be spawned
            if (!_removedones.Any(g => g.Position == guard.Position && g.AreaName == guard.AreaName) &&
                !_guards.Any(g => g.Position == guard.Position && g.AreaName == guard.AreaName))
            {
                _guards.Add(guard);
                guard.Spawn();
            }
        }
    }

    public void UnInitialize()
    {
        foreach (var guard in _guards.ToList()) // Use ToList() to avoid modifying the collection while iterating
        {
            if (guard != null && guard.guardPed != null && guard.guardPed.Exists())
            {
                guard.guardPed.MarkAsNoLongerNeeded();
            }
            if (guard != null && guard.guardVehicle != null && guard.guardVehicle.Exists())
            {
                guard.guardVehicle.MarkAsNoLongerNeeded();
            }
        }
        foreach (var guard in _removedones.ToList()) // Use ToList() to avoid modifying the collection while iterating
        {
            if (guard != null && guard.guardPed != null && guard.guardPed.Exists())
            {
                guard.guardPed.MarkAsNoLongerNeeded();
            }
            if (guard != null && guard.guardVehicle != null && guard.guardVehicle.Exists())
            {
                guard.guardVehicle.MarkAsNoLongerNeeded();
            }
        }
        _guards.Clear();
        _removedones.Clear();
        Logger.Log("All guards have been uninitialized and despawned.");
    }

    public void DespawnGuards(Area area)
    {
        var guardsToRemove = _guards.Where(g => g.AreaName == area.Name).ToList();
        foreach (var guard in guardsToRemove)
        {
            if (guard != null)
            {
                guard.Despawn();
                _guards.Remove(guard);
            }
        }

        var removedGuardsToRemove = _removedones.Where(g => g.AreaName == area.Name).ToList();
        foreach(var guard in removedGuardsToRemove)
        {
            if (guard != null)
            {
                _removedones.Remove(guard);
            }
        }
        Logger.Log($"All guards in area {area.Name} have been despawned.");
    }
}