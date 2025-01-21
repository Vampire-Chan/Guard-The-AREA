// GuardSpawner.cs
using GTA;
using GTA.Math;
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
        if (ped.Armor > 10 && ped.Armor < 35) ped.Armor = 150; //for cops
        else if (ped.Armor > 35 && ped.Armor <100) ped.Armor = 200; //army/swat
        // Set ped's configuration flags
        ped.SetConfigFlag(PedConfigFlagToggles.WillNotHotwireLawEnforcementVehicle, false);
        ped.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, true);
        ped.SetConfigFlag(PedConfigFlagToggles.CanDiveAwayFromApproachingVehicles, true);
        ped.SetCombatAttribute(CombatAttributes.CanUseCover, true);
        ped.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, true);
        ped.SetConfigFlag(PedConfigFlagToggles.AIDriverAllowFriendlyPassengerSeatEntry, true);
        ped.SetConfigFlag(PedConfigFlagToggles.IgnoreInteriorCheckForSprinting, true);
       // ped.SetConfigFlag(PedConfigFlagToggles.HasBulletProofVest)
        // Apply combat attributes if the ped is a guard or has combat capabilities
        ped.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
        ped.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, true);
        ped.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, true);
        ped.SetCombatAttribute(CombatAttributes.CanUseCover, true);
        ped.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
        ped.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);
        //ped.SetCombatAttribute(inj)

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