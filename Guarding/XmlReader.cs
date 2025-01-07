using GTA.Math;
using System.Collections.Generic;
using System.Xml.Linq;

public class XmlReader
{
    private readonly string _areasFilePath; // XML File Path for Areas
    private readonly string _guardsFilePath; // XML File Path for Guards

    // Constructor to initialize file paths for areas and guards
    public XmlReader(string areasFilePath, string guardsFilePath)
    {
        _areasFilePath = areasFilePath; // Set file path for areas
        _guardsFilePath = guardsFilePath; // Set file path for guards
    }

    // Method to load areas from the XML file
    public List<Area> LoadAreasFromXml()
    {
        var areas = new List<Area>(); // Store areas

        XElement xml = XElement.Load(_areasFilePath); // Load XML

        foreach (var areaElement in xml.Elements("Area")) // Iterate through areas
        {
            string areaName = areaElement.Attribute("name")?.Value; // Get area name
            string model = areaElement.Attribute("model")?.Value; // Get area model name
            Area area = new Area(areaName, model); // Create new area with model

            foreach (var spawnPointElement in areaElement.Elements("SpawnPoint")) // Iterate spawn points
            {
                var positionElement = spawnPointElement.Element("Position"); // Get position
                float x = float.Parse(positionElement.Attribute("x")?.Value); // X position
                float y = float.Parse(positionElement.Attribute("y")?.Value); // Y position
                float z = float.Parse(positionElement.Attribute("z")?.Value); // Z position

                float heading = float.Parse(spawnPointElement.Element("Heading")?.Value); // Heading

                Vector3 position = new Vector3(x, y, z); // Create position vector
                area.AddSpawnPoint(position, heading); // Add spawn point to area
            }

            areas.Add(area); // Add area to list
        }

        return areas; // Return list of areas
    }

    // Method to load guards from the XML file
    public Dictionary<string, GuardInfo> LoadGuardsFromXml()
    {
        var guards = new Dictionary<string, GuardInfo>(); // Store guards

        XElement xml = XElement.Load(_guardsFilePath); // Load XML

        foreach (var guardElement in xml.Elements("Guard")) // Iterate through guards
        {
            string guardName = guardElement.Attribute("name")?.Value; // Get guard name
            GuardInfo guardInfo = new GuardInfo(guardName); // Create new guard info

            foreach (var pedModelElement in guardElement.Elements("PedModel")) // Iterate ped models
            {
                guardInfo.PedModels.Add(pedModelElement.Value); // Add ped model to guard info
            }

            foreach (var weaponElement in guardElement.Elements("Weapon")) // Iterate weapons
            {
                guardInfo.Weapons.Add(weaponElement.Value); // Add weapon to guard info
            }

            guards.Add(guardName, guardInfo); // Add guard info to dictionary
        }

        return guards; // Return dictionary of guards
    }
}

// Class to store guard information including models and weapons
public class GuardInfo
{
    public string Name { get; set; } // Guard name
    public List<string> PedModels { get; set; } // List of ped models
    public List<string> Weapons { get; set; } // List of weapons

    // Constructor to initialize guard name and lists
    public GuardInfo(string name)
    {
        Name = name; // Set guard name
        PedModels = new List<string>(); // Initialize ped models list
        Weapons = new List<string>(); // Initialize weapons list
    }
}