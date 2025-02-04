using GTA.Math;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

public class XmlReader
{
    private readonly string _xmlFilePath;
    private readonly string _guardsXmlPath;

    public XmlReader(string areasFilePath)
    {
        _xmlFilePath = areasFilePath;
        // Assuming Guards.xml is in the same directory
        _guardsXmlPath = Path.Combine(Path.GetDirectoryName(areasFilePath), "Guards.xml");
    }

    private List<string> ParseRelationshipString(string relationshipString)
    {
        if (string.IsNullOrWhiteSpace(relationshipString))
            return new List<string>();

        return relationshipString.Split(',')
                               .Select(x => x.Trim())
                               .Where(x => !string.IsNullOrWhiteSpace(x))
                               .ToList();
    }

    public List<Area> LoadAreasFromXml()
    {
        var areas = new List<Area>();
        XElement xml = XElement.Load(_xmlFilePath);

        foreach (var areaElement in xml.Elements("Area"))
        {
            string areaName = areaElement.Attribute("name")?.Value;
            string model = areaElement.Attribute("model")?.Value;

            bool relationshipOverride = false;
            bool.TryParse(areaElement.Attribute("override")?.Value, out relationshipOverride);

            // Parse relationships
            var hate = ParseRelationshipString(areaElement.Attribute("hates")?.Value);
            var dislike = ParseRelationshipString(areaElement.Attribute("dislikes")?.Value);
            var respect = areaElement.Attribute("respects")?.Value;
            var like = ParseRelationshipString(areaElement.Attribute("likes")?.Value);

            Area area = new Area(areaName, model, hate, dislike, respect, like, relationshipOverride);
            foreach (var spawnPointElement in areaElement.Elements("SpawnPoint"))
            {
                var positionElement = spawnPointElement.Element("Position");
                float x = float.Parse(positionElement.Attribute("x")?.Value);
                float y = float.Parse(positionElement.Attribute("y")?.Value);
                float z = float.Parse(positionElement.Attribute("z")?.Value);

                float heading = float.Parse(spawnPointElement.Element("Heading")?.Value);
                string type = spawnPointElement.Attribute("type")?.Value?.ToLower() ?? "ped";

                string scenario = spawnPointElement.Attribute("scenario")?.Value;

                bool interior = false;
                bool.TryParse(spawnPointElement.Attribute("interior")?.Value, out interior);

                if (string.IsNullOrWhiteSpace(scenario))
                {
                    scenario = null; // Treat as no valid scenario
                }

                Vector3 position = new Vector3(x, y, z);
                area.AddSpawnPoint(position, heading, type, scenario, interior);
            }

            areas.Add(area);
        }

        return areas;
    }

    public Dictionary<string, GuardConfig> LoadGuardConfigs()
    {
        var guardConfigs = new Dictionary<string, GuardConfig>();
        XElement xml = XElement.Load(_guardsXmlPath);

        foreach (var guardElement in xml.Elements("Guard"))
        {
            string guardName = guardElement.Attribute("name")?.Value;
            string guardGroup = guardElement.Attribute("group")?.Value;

            var mountedVehicle = guardElement.Element("MountedVehicleModel");
            int seatIndex = 0; // Default value if not found

            if (mountedVehicle != null && mountedVehicle.Attribute("seatindex") != null)
            {
                int.TryParse(mountedVehicle.Attribute("seatindex")?.Value, out seatIndex);
            }

            var config = new GuardConfig
            {
                Name = guardName,

                PedModels = guardElement.Elements("PedModel")
                                      .Select(x => x.Value)
                                      .ToList(),
                Weapons = guardElement.Elements("Weapon")
                                    .Select(x => x.Value)
                                    .ToList(),
                VehicleModels = guardElement.Elements("VehicleModel")
                                          .Select(x => x.Value)
                                          .ToList(),
                MVehicleModels = guardElement.Elements("MountedVehicleModel")
                                          .Select(x => x.Value)
                                          .ToList(),
                RelationshipGroup = guardGroup,
                SeatIndex = seatIndex, // Properly loaded seat index
                Hate = ParseRelationshipString(guardElement.Attribute("hates")?.Value),
                Dislike = ParseRelationshipString(guardElement.Attribute("dislikes")?.Value),
                Respect = guardElement.Attribute("respects")?.Value,
                Like = ParseRelationshipString(guardElement.Attribute("likes")?.Value)
            };

            guardConfigs[guardName] = config;
        }

        return guardConfigs;
    }

}

public class GuardConfig
{
    public string Name { get; set; }
    public List<string> PedModels { get; set; } = new List<string>();
    public List<string> Weapons { get; set; } = new List<string>();
    public List<string> VehicleModels { get; set; } = new List<string>();
    public List<string> MVehicleModels { get; set; } = new List<string>();
    public List<string> Hate { get; set; } = new List<string>();
    public List<string> Dislike { get; set; } = new List<string>();
    public string Respect { get; set; }
    public int SeatIndex { get; set; }
    public List<string> Like { get; set; } = new List<string>();
    public string RelationshipGroup { get; set; }
}