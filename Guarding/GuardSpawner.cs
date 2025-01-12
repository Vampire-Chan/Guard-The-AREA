// GuardSpawner.cs
using GTA;
using System;
using System.Collections.Generic;
using System.Linq;

public class GuardSpawner
{
    private List<Area> _areas;
    private List<Guard> _guards;
    private List<Guard> _removedones;
    private Dictionary<string, GuardConfig> _guardConfigs;


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
            foreach (var guard in _guards.ToList()) // Use ToList() to avoid modifying the collection while iterating
            {
                if (guard?.guardPed != null)
                {
                    if (guard.guardPed.IsDead)
                    {
                        guard.guardPed.MarkAsNoLongerNeeded();
                        _guards.Remove(guard);
                        _removedones.Add(guard);
                    }
                    else if (!guard.guardPed.Exists())
                    {
                        _guards.Remove(guard);
                    }

                    if(guard.guardPed.IsInCombat)
                    {
                        guard.guardPed.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
                        guard.guardPed.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, true);
                        guard.guardPed.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, true);
                        guard.guardPed.SetCombatAttribute(CombatAttributes.CanUseCover, true);
                        guard.guardPed.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);
                        guard.guardPed.SetCombatAttribute(CombatAttributes.DisableReactToBuddyShot, true);
                        guard.guardPed.SetConfigFlag(PedConfigFlagToggles.CanPerformArrest, true);
                        //guard.guardPed.SetConfigFlag(PedConfigFlagToggles.can, true);
                        guard.guardPed.MarkAsNoLongerNeeded();
                        guard.guardPed.Task.CombatHatedTargetsAroundPed(1000);
                        guard.guardPed.HearingRange = 200;
                        guard.guardPed.SeeingRange = 75;
                        _guards.Remove(guard);
                        _removedones.Add(guard);
                    }
                }
                if (guard?.guardVehicle != null)
                {
                    if (guard.guardVehicle.IsDead)
                    {
                        guard.guardVehicle.MarkAsNoLongerNeeded();
                        _guards.Remove(guard);
                        _removedones.Add(guard);
                    }
                    else if (!guard.guardVehicle.Exists())
                    {
                        _guards.Remove(guard);
                    }
                    else if (guard.guardVehicle.Driver != null || guard.guardVehicle.PassengerCount != 0)
                    {
                        guard.guardVehicle.MarkAsNoLongerNeeded();
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
            Logger.Log($"Checking area {area.Name} for player proximity...");
            foreach (var spawnPoint in area.SpawnPoints)
            {
                if (spawnPoint?.Position == null) continue; // Ensure valid spawn point

                float distance = player.Character.Position.DistanceTo(spawnPoint.Position);
                Logger.Log($"Distance to spawn point: {distance}");
                if (distance < 150f)
                {
                    SpawnGuards(area);
                    Logger.Log($"Guards trying to spawn in area {area.Name}");
                    break;
                }
                else if (distance > 170f)
                {
                    DespawnGuards(area);
                    break;
                }
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
            var rand = new Random();

            var ped = guardConfig.PedModels[rand.Next(0, guardConfig.PedModels.Count)];
            var weapon = guardConfig.Weapons[rand.Next(0, guardConfig.Weapons.Count)];
            var vehicle = guardConfig.VehicleModels[rand.Next(0, guardConfig.VehicleModels.Count)];

            Guard guard = new Guard(spawnPoint.Position, spawnPoint.Heading, area.Name, spawnPoint.Type,vehicle, ped, weapon);
            
            Logger.Log("Initializing guard...");
            guard.scenario = MainScript.scenarios[rand.Next(MainScript.scenarios.Length)];
            // Check if the guard is already in the _removedones list
            if (!_removedones.Any(g => g.Position == guard.Position && g.AreaName == guard.AreaName))
            {
                if (!_guards.Any(g => g.Position == guard.Position && g.AreaName == guard.AreaName))
                {
                    _guards.Add(guard);
                    guard.Spawn();
                }
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