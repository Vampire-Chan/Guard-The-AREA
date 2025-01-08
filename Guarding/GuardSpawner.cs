using GTA;
using System.Collections.Generic;
using System;

public class GuardSpawner
{
    private readonly Dictionary<string, GuardInfo> _guards;
    private readonly List<Area> _areas;

    public GuardSpawner(string areasFilePath, string guardsFilePath)
    {
        // Loading the XML files and initializing dictionaries
        var xmlReader = new XmlReader(areasFilePath, guardsFilePath);
        _areas = xmlReader.LoadAreasFromXml();
        _guards = xmlReader.LoadGuardsFromXml();
        Logger.Log($"Loaded {_areas.Count} areas and {_guards.Count} guards from XML.");
    }

    public void CheckPlayerProximityAndSpawn(Player player)
    {
        foreach (var area in _areas)
        {
            Logger.Log($"Checking area {area.Name} for player proximity...");
            bool isPlayerNear = false;
            foreach (var spawnPoint in area.SpawnPoints)
            {
                float distance = player.Character.Position.DistanceTo(spawnPoint.Position);
                Logger.Log($"Distance to spawn point: {distance}");
                if (distance < 100f)
                {
                    isPlayerNear = true;
                    if (area.CanRespawn() && area.SpawnReady)
                    {
                        SpawnGuards(area);
                        area.UpdateLastSpawnTime();
                        Logger.Log($"Guards spawning in area {area.Name}");
                    }
                    area.SpawnReady = false;
                    break;
                }
            }
            if (!isPlayerNear)
            {
                area.SpawnReady = true;
                area.RemoveGuards();
                Logger.Log($"Guards removed from area {area.Name}");
            }
        }
    }

    private void SpawnGuards(Area area)
    {
        try
        {
            Logger.Log($"Attempting to spawn guards for area {area.Name} with model {area.Model}.");

            if (_guards.TryGetValue(area.Model, out GuardInfo guardInfo))
            {
                foreach (var spawnPoint in area.SpawnPoints)
                {
                    if (area.GuardAssignments.ContainsKey(spawnPoint) && area.GuardAssignments[spawnPoint] != null)
                    {
                        Logger.Log($"Guard already assigned to spawn point at {spawnPoint.Position}, skipping.");
                        continue; // Skip if guard is already assigned to this spawn point
                    }

                    var guard = new Guard(spawnPoint.Position, spawnPoint.Heading, guardInfo);
                    Logger.Log("Initializing guard...");
                    guard.Spawn();
                    area.GuardAssignments[spawnPoint] = guard;
                    Logger.Log($"Guard spawned at {spawnPoint.Position} in area {area.Name}.");
                }
            }
            else
            {
                Logger.Log($"ERROR: Guard model {area.Model} not found in the dictionary.");
                Logger.Log($"Available models: {string.Join(", ", _guards.Keys)}");
                throw new KeyNotFoundException($"Guard model {area.Model} not found in the dictionary.");
            }
        }
        catch (KeyNotFoundException ex)
        {
            Logger.Log($"KeyNotFoundException: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"Exception: {ex.Message}");
            throw;
        }
    }
        
    public void DiagnoseGuardModels()
    {
        Logger.Log("=== Guard Models Diagnostic ===");
        foreach (var guard in _guards)
        {
            Logger.Log($"Guard Type: '{guard.Key}'");
            Logger.Log($"- Ped Models: {string.Join(", ", guard.Value.PedModels)}");
            Logger.Log($"- Weapons: {string.Join(", ", guard.Value.Weapons)}");
        }

        Logger.Log("=== Areas Diagnostic ===");
        foreach (var area in _areas)
        {
            Logger.Log($"Area: '{area.Name}'");
            Logger.Log($"- Model: '{area.Model}'");
            Logger.Log($"- Spawn Points: {area.SpawnPoints.Count}");
        }
    }
}