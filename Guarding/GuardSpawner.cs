// GuardSpawner.cs
using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

public class GuardSpawner
{
    private List<Ped> AllPedsInWorld;
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

        // One-time setup for Guards
        var PrivateGuardHash = StringHash.AtStringHash("PRIVATE_SECURITY");
        var GuardHash = StringHash.AtStringHash("SECURITY_GUARD");
        var ArmyHash = StringHash.AtStringHash("ARMY");
        var CopHash = StringHash.AtStringHash("COP");
        var GuardDogHash = StringHash.AtStringHash("GUARD_DOG");

        // Set relationships within groups
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, PrivateGuardHash, PrivateGuardHash); // Private guards are companions to themselves
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, GuardHash, GuardHash);             // Guards are companions to themselves
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, ArmyHash, ArmyHash);               // Army is companion to itself
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, CopHash, CopHash);                 // Cops are companions to themselves
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, GuardDogHash, GuardDogHash);       // Guard dogs are companions to themselves

        // Set mutual respect relationships
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, PrivateGuardHash, GuardHash);      // Private guards respect guards
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, GuardHash, PrivateGuardHash);      // Guards respect private guards
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, PrivateGuardHash, ArmyHash);       // Private guards respect the army
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, ArmyHash, PrivateGuardHash);       // Army respects private guards
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, GuardHash, ArmyHash);              // Guards respect the army
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, ArmyHash, GuardHash);              // Army respects guards
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, CopHash, ArmyHash);                // Cops respect the army
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, ArmyHash, CopHash);                // Army respects cops
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, CopHash, PrivateGuardHash);        // Cops respect private guards
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, PrivateGuardHash, CopHash);        // Private guards respect cops

        // Set guard dogs to respect other groups (optional)
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, GuardDogHash, PrivateGuardHash);   // Guard dogs respect private guards
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, PrivateGuardHash, GuardDogHash);   // Private guards respect guard dogs
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, GuardDogHash, GuardHash);          // Guard dogs respect guards
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, GuardHash, GuardDogHash);          // Guards respect guard dogs

    }

    private Area temparea = null;

    private void SetupWorldStuffs()
    {
        // Get all current peds in the world
        AllPedsInWorld = World.GetAllPeds().ToList();

        // Remove globally found peds that no longer exist or are null
        AllPedsInWorld.RemoveAll(ped => ped == null || !_guards.Any(g => g.guardPed == ped) && !_removedones.Any(g => g.guardPed == ped));

        // Filter out already added peds (in guards or removedOnes)
        var newPeds = AllPedsInWorld
            .Where(ped => ped != null &&
                          !_guards.Any(g => g.guardPed == ped) &&
                          !_removedones.Any(g => g.guardPed == ped))
            .ToList();

        // Process only the remaining peds
        foreach (var ped in newPeds)
        {
            ProcessOtherPed(ped);
        }
    }

    // Define how to process other peds not in guards or removed lists
    private void ProcessOtherPed(Ped ped)
    {
        // Log the ped's ID or handle for debugging
        Logger.Log($"Processing ped: {ped.Handle}");

        ped.MaxHealth = 300;
        ped.Health = 300;
        ped.DiesOnLowHealth = false;
        if (ped.Armor > 1 && ped.Armor < 50) ped.Armor = 200; //for cops
        else if (ped.Armor > 50 && ped.Armor < 125) ped.Armor = 250; //army/swat
        // Set ped's configuration flags
      

        // Additional logic for law enforcement or military types
        if (ped.PedType == PedType.Cop || ped.PedType == PedType.Swat || ped.PedType == PedType.Army)
        {
            ped.SetConfigFlag(PedConfigFlagToggles.CanPerformArrest, true);
            //ped.SetConfigFlag(PedConfigFlagToggles.can)
        }

        // Additional processing logic can be added here as needed
        Logger.Log($"Ped {ped.Handle} configuration and attributes updated.");
    }

    public void CheckAllTime()
    {
        SetupWorldStuffs();
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
           area.Name, spawnPoint.Type, guardConfig, spawnPoint.Scenario, area, spawnPoint.Interior);

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
        // Only remove guards that belong to the specified area
        var guardsToRemove = _guards.Where(g => g.AreaName == area.Name).ToList();
        foreach (var guard in guardsToRemove)
        {
            if (guard != null)
            {
                guard.Despawn();
                _guards.Remove(guard); // Remove from active guards list
            }
        }

        // Ensure guards that have been removed once are also handled based on area
        var removedGuardsToRemove = _removedones.Where(g => g.AreaName == area.Name).ToList();
        foreach (var guard in removedGuardsToRemove)
        {
            if (guard != null)
            {
                _removedones.Remove(guard); // Remove from the removed guards list
            }
        }

        Logger.Log($"All guards in area {area.Name} have been despawned.");
    }

}