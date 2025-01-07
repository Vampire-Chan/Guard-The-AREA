using GTA;
using GTA.Math;
using System.Collections.Generic;
using System.Xml.Linq;

public class GuardSpawner
{
    private List<Area> _areas; // List of areas
    private Dictionary<string, GuardInfo> _guards; // Dictionary of guard info
    private string _xmlFilePath;

    // Constructor to initialize the guard spawner
    public GuardSpawner(string areasFilePath, string guardsFilePath)
    {
        _xmlFilePath = areasFilePath;
        var xmlReader = new XmlReader(areasFilePath, guardsFilePath);
        _areas = xmlReader.LoadAreasFromXml(); // Load areas from XML
        _guards = xmlReader.LoadGuardsFromXml(); // Load guards from XML
        Logger.Log($"Loaded {_areas.Count} areas and {_guards.Count} guards from XML."); // Log the number of areas and guards loaded
    }

    // Method to check player proximity and spawn guards if needed
    public void CheckPlayerProximityAndSpawn(Player player)
    {
        foreach (var area in _areas) // Iterate through each area
        {
            Logger.Log($"Checking area {area.Name} for player proximity..."); // Log area check
            bool isPlayerNear = false;
            foreach (var spawnPoint in area.SpawnPoints) // Check each spawn point
            {
                Logger.Log($"Checking spawn point {spawnPoint.Position}..."); // Log spawn point check
                float distance = player.Character.Position.DistanceTo(spawnPoint.Position); // Calculate distance
                Logger.Log($"Distance to spawn point: {distance}"); // Log distance
                if (distance < 100f) // Player is within 100m
                {
                    isPlayerNear = true;
                    if (area.CanRespawn() && area.SpawnReady) // Cooldown passed and spawn ready
                    {
                        SpawnGuards(area); // Spawn guards for the area
                        area.UpdateLastSpawnTime(); // Update the last spawn time for the area
                        Logger.Log($"Guards spawning in area {area.Name}"); // Log guards spawned
                    }
                    area.SpawnReady = false; // Set spawn ready to false while player is in the area
                    break; // Stop checking after spawning guards
                }
            }
            if (!isPlayerNear)
            {
                area.SpawnReady = true; // Set spawn ready to true when player leaves the area
                area.RemoveGuards(); // Remove guards when player is not near
                Logger.Log($"Guards removed from area {area.Name}"); // Log guards removed
            }
        }
    }

    // Method to spawn guards in the area
    private void SpawnGuards(Area area)
    {
        Logger.Log($"Attempting to spawn guards for area {area.Name} with model {area.Model}.");

        if (_guards.TryGetValue(area.Model, out GuardInfo guardInfo))
        {
            foreach (var spawnPoint in area.SpawnPoints) // Iterate through spawn points
            {
                if (area.GuardAssignments[spawnPoint] == null) // Check if guard is not assigned
                {
                    var guard = new Guard(spawnPoint.Position, spawnPoint.Heading, guardInfo); // Create guard
                    Logger.Log("Initializing guard..."); // Log guard initialization
                    guard.Spawn(); // Spawn the guard
                    area.GuardAssignments[spawnPoint] = guard; // Assign guard to spawn point
                }
            }
        }
        else
        {
            Logger.Log($"Guard model {area.Model} not found in the dictionary.");
            throw new KeyNotFoundException($"Guard model {area.Model} not found in the dictionary.");
        }
    }

    // Method to add a spawn point to an area and save it to the XML
    public void AddSpawnPoint(string areaName, Vector3 position, float heading)
    {
        var area = _areas.Find(a => a.Name == areaName);
        if (area != null)
        {
            area.AddSpawnPoint(position, heading);
            SaveSpawnPointToXml(areaName, position, heading);
        }
    }

    // Method to save a new spawn point to the XML file
    private void SaveSpawnPointToXml(string areaName, Vector3 position, float heading)
    {
        XDocument xml = XDocument.Load(_xmlFilePath);
        var areaElement = xml.Root.Element("Area");

        if (areaElement != null)
        {
            XElement newSpawnPoint = new XElement("SpawnPoint",
                new XElement("Position",
                    new XAttribute("x", position.X),
                    new XAttribute("y", position.Y),
                    new XAttribute("z", position.Z)),
                new XElement("Heading", heading));

            areaElement.Add(newSpawnPoint);
            xml.Save(_xmlFilePath);
        }
    }

    // Method to get the list of areas
    public List<Area> GetAreas()
    {
        return _areas;
    }
}