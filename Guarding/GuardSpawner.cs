// GuardSpawner.cs
using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

public class GuardSpawner
{
    private List<Area> areas;
    private List<Guard> guards;
    private List<Guard> removedGuards;
    
        public static Dictionary<string, Scenarios> scans = new Dictionary<string, Scenarios>();
    
    private List<Ped> processedPeds; // Renamed from Processed to processedPeds (camelCase) and to reflect actively managed peds
    private List<Ped> writheProcessedPeds; // New list for peds in writhe state (camelCase, PascalCase consistency)
    private Dictionary<string, GuardConfig> guardConfigs;

    private const float SpawnDistance = 220; // PascalCase for const
    private const float DespawnDistance = 250f; // PascalCase for const

    public GuardSpawner(string xmlFilePath) // PascalCase for constructor
    {
        XmlReader xml = new XmlReader(xmlFilePath); // PascalCase for class name
        areas = xml.LoadAreasFromXml(); // camelCase
        guardConfigs = xml.LoadGuardConfigs(); // camelCase
        scans = xml.LoadScenarios();
        guards = new List<Guard>(); // camelCase
        removedGuards = new List<Guard>(); // Renamed to removedGuards (PascalCase for clarity)
        processedPeds = new List<Ped>(); // Initialize processedPeds list
        writheProcessedPeds = new List<Ped>(); // Initialize writheProcessedPeds list
        Logger.Log($"Loaded {areas.Count} areas from XML."); // camelCase
    }

    private void SetupWorldStuffs() // PascalCase for method name
    {
        List<Ped> allPedsInWorld = World.GetAllPeds().ToList(); // camelCase for local variable

        // Convert all relationship group names to hashes (unchanged)
        var privateGuardHash = StringHash.AtStringHash("PRIVATE_SECURITY"); // camelCase for local variable
        var guardHash = StringHash.AtStringHash("SECURITY_GUARD"); // camelCase
        var armyHash = StringHash.AtStringHash("ARMY"); // camelCase
        var copHash = StringHash.AtStringHash("COP"); // camelCase
        var guardDogHash = StringHash.AtStringHash("GUARD_DOG"); // camelCase
        var merryWHash = StringHash.AtStringHash("MERRYWEATHER"); // Corrected variable name and camelCase
        var playerGroupHash = Game.Player.Character.RelationshipGroup; // camelCase

        //if (Game.Player.WantedLevel > 0) // (unchanged)


            // Remove globally found peds that no longer exist or are null (unchanged)
            allPedsInWorld.RemoveAll(ped => ped == null ||
                                             !guards.Any(g => g.guardPed == ped) &&
                                             !removedGuards.Any(g => g.guardPed == ped));

        // Filter out already added peds - now checking against BOTH lists (modified for processedPeds list)
        var newPeds = allPedsInWorld // camelCase for local variable
            .Where(ped => ped != null &&
                           !guards.Any(g => g.guardPed == ped) &&
                           !removedGuards.Any(g => g.guardPed == ped) &&
                           !processedPeds.Contains(ped) && // Exclude already processed peds (renamed list)
                           !writheProcessedPeds.Contains(ped)) // Exclude already in writhe processed peds (new list)
            .ToList();

        // Process only the remaining peds
        foreach (var ped in newPeds) // camelCase for local variable
        {
            if (!processedPeds.Contains(ped)) // Redundant check - can be removed as already filtered above in Where clause.
                ProcessOtherPed(ped, true);

            processedPeds.Add(ped); // Add processed ped to the list for tracking and writhe management. (Moved here)

            // Removed health check and second ProcessOtherPed call here. Health based writhe logic is now in UpdatePedWritheStates
        }
        //we are likely to process the ped again if we find less health? 120 and less. new list or existing list? idk - Comment is now outdated.
    }


    // Define how to process other peds not in guards or removed lists (unchanged mostly, offwrithe renamed to disableWrithe)
    private void ProcessOtherPed(Ped ped, bool disableWrithe) // PascalCase for method, renamed parameter for clarity
    {
        // Log the ped's ID or handle for debugging (unchanged)
        Logger.Log($"Processing ped: {ped.Handle}");

        ped.MaxHealth = 300; // (unchanged)
        ped.Health = 300;   // (unchanged)
        ped.DiesOnLowHealth = !disableWrithe; // Use disableWrithe parameter
        ped.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, disableWrithe); // Use disableWrithe parameter
        Function.Call(Hash.SET_PED_IS_IGNORED_BY_AUTO_OPEN_DOORS, ped, true);
        if (ped.Armor > 1 && ped.Armor < 50) ped.Armor = 200; // (unchanged)
        else if (ped.Armor > 50 && ped.Armor < 125) ped.Armor = 250; // (unchanged)

        if (disableWrithe) // Use disableWrithe parameter
            Logger.Log($"Ped {ped.Handle} configuration and attributes updated. Writhe disabled: {disableWrithe}"); // Updated log to show writhe status
    }

    // NEW FUNCTION: Update ped writhe states based on health and list management (PascalCase for method name)
    private void UpdatePedWritheStates()
    {
        // 1. Process processedPeds (Health Monitoring and Transition to writheProcessedPeds)
        foreach (Ped ped in processedPeds.ToList()) // ToList() for safe removal during iteration
        {
            if (ped == null || !ped.Exists() || ped.IsDead)
            {
                processedPeds.Remove(ped); // Cleanup invalid peds from processedPeds list
                continue;
            }

            if (ped.Health < 120)
            {
                // Re-enable "go to writhe" and move to writheProcessedPeds
                if (ped.GetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured))
                {
                    ped.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, false); // Re-enable writhe
                    Logger.Log($"Ped {ped.Handle} health dropped below 120, enabling 'go to writhe' and moving to writhe-processed list. Health: {ped.Health}");
                    processedPeds.Remove(ped); // Remove from active monitoring list
                    writheProcessedPeds.Add(ped); // Add to writhe-processed list
                }
            }
            // No 'else' to re-disable writhe in this logic.
        }

        // 2. Process writheProcessedPeds (Cleanup dead/gone peds)
        foreach (Ped ped in writheProcessedPeds.ToList()) // ToList() for safe removal during iteration
        {
            if (ped == null || !ped.Exists() || ped.IsDead)
            {
                writheProcessedPeds.Remove(ped); // Cleanup dead/gone peds from writheProcessedPeds list
                Logger.Log($"Ped {ped.Handle} is dead or gone, removed from writhe-processed list.");
            }
        }
    }


    public void CheckAllTime() // PascalCase for method name
    {
        SetupWorldStuffs(); // PascalCase for method call
        UpdatePedWritheStates(); // Call the new writhe update function every frame

        foreach (var guard in guards.ToList()) // camelCase for local variable
        {
            if (guard == null) continue;

            bool isPedDeadOrMissing = guard.guardPed == null || !guard.guardPed.Exists() || guard.guardPed.IsDead; // camelCase for local variable
            bool isGunnerDeadOrMissing = guard.guardPedOnVehicle == null || !guard.guardPedOnVehicle.Exists() || guard.guardPedOnVehicle.IsDead; // camelCase

            // Remove guards if they are dead or missing (unchanged)
            if (isPedDeadOrMissing && isGunnerDeadOrMissing)
            {
                if (guard.guardPed != null)
                {
                    guard.guardPed.MarkAsNoLongerNeeded(); // camelCase
                }
                if (guard.guardPedOnVehicle != null)
                {
                    guard.guardPedOnVehicle.MarkAsNoLongerNeeded(); // camelCase
                }

                guards.Remove(guard); // camelCase
                removedGuards.Add(guard); // camelCase
                continue;
            }

            // Update combat state for each guard (unchanged)
            guard.UpdateCombatState(); // camelCase

            // Handle vehicle status correctly (unchanged)
            if (guard.guardVehicle != null)
            {
                bool isVehicleInvalid = !guard.guardVehicle.Exists() || guard.guardVehicle.IsDead; // camelCase
                bool isVehicleOccupied = guard.guardVehicle.Driver != null || guard.guardVehicle.PassengerCount > 0; // camelCase

                if (isVehicleInvalid || isVehicleOccupied)
                {
                    guard.guardVehicle.MarkAsNoLongerNeeded(); // camelCase
                    guards.Remove(guard); // camelCase
                    removedGuards.Add(guard); // camelCase
                }
            }
        }


    }

    public void CheckPlayerProximityAndSpawn(Player player) // PascalCase for method name
    {
        if (player?.Character == null) // (unchanged)
        {
            Logger.Log("Player or Player.Character is null. Skipping proximity check."); // camelCase
            return;
        }

        CheckAllTime(); // PascalCase for method call

        foreach (var area in areas) // camelCase for local variable
        {
            Vector3 areaCentroid = area.GetCentroid(); // camelCase

            float distanceToCentroid = player.Character.Position.DistanceTo(areaCentroid); // camelCase
            Logger.Log($"Checking area {area.Name}, distance to centroid: {distanceToCentroid}"); // camelCase

            // Adjust spawn/despawn distances based on area radius (unchanged)
            float adjustedSpawnDistance = SpawnDistance; // camelCase
            float adjustedDespawnDistance = DespawnDistance; // camelCase

            if (distanceToCentroid < adjustedSpawnDistance) // camelCase
            {
                SpawnGuards(area); // PascalCase for method call
                Logger.Log($"Guards spawning in area {area.Name}"); // camelCase
            }
            else if (distanceToCentroid > adjustedDespawnDistance) // camelCase
            {
                DespawnGuards(area); // PascalCase for method call
            }
        }
    }

    private void SpawnGuards(Area area) // PascalCase for method name
    {
        if (!guardConfigs.ContainsKey(area.Model)) // camelCase
        {
            Logger.Log($"Guard model {area.Model} not found in configurations."); // camelCase
            return;
        }

        var guardConfig = guardConfigs[area.Model]; // camelCase

        foreach (var spawnPoint in area.SpawnPoints) // camelCase
        {
            // Create guard with the config (unchanged)
            Guard guard = new Guard(spawnPoint, guardConfig, area); // camelCase

            // Check if the guard should be spawned (unchanged)
            if (!removedGuards.Any(g => g.Position == guard.Position && g.AreaName == guard.AreaName) &&
                !guards.Any(g => g.Position == guard.Position && g.AreaName == guard.AreaName))
            {
                guards.Add(guard); // camelCase
                guard.Spawn(); // PascalCase for method call
            }
        }
    }

    public void UnInitialize() // PascalCase for method name
    {
        foreach (var guard in guards.ToList()) // camelCase
        {
            if (guard != null) guard.Despawn(); // PascalCase for method call
        }
        guards.Clear(); // camelCase
        removedGuards.Clear(); // camelCase
        processedPeds.Clear(); // Clear processedPeds list on uninitialize
        writheProcessedPeds.Clear(); // Clear writheProcessedPeds list on uninitialize
        Logger.Log("All guards have been uninitialized and despawned."); // camelCase
    }

    public void DespawnGuards(Area area) // PascalCase for method name
    {
        // Only remove guards that belong to the specified area (unchanged)
        var guardsToRemove = guards.Where(g => g.AreaName == area.Name).ToList(); // camelCase
        foreach (var guard in guardsToRemove) // camelCase
        {
            if (guard != null)
            {
                guard.Despawn(); // PascalCase for method call
                guards.Remove(guard); // camelCase // Remove from active guards list
            }
        }

        // Ensure guards that have been removed once are also handled based on area (unchanged)
        var removedGuardsToRemove = removedGuards.Where(g => g.AreaName == area.Name).ToList(); // camelCase
        foreach (var guard in removedGuardsToRemove) // camelCase
        {
            if (guard != null)
            {
                removedGuards.Remove(guard); // camelCase // Remove from the removed guards list
            }
        }

        Logger.Log($"All guards in area {area.Name} have been despawned."); // camelCase
    }

}