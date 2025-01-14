// XmlReader.cs
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
            return null;

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
            bool relationshipOverride = bool.Parse(areaElement.Attribute("override")?.Value ?? "false");

            Area area = new Area(areaName, model, relationshipOverride);

            // Parse relationships
            area.Hate = ParseRelationshipString(areaElement.Attribute("hates")?.Value);
            area.Dislike = ParseRelationshipString(areaElement.Attribute("dislikes")?.Value);
            area.Respect = ParseRelationshipString(areaElement.Attribute("respect")?.Value);
            area.Like = ParseRelationshipString(areaElement.Attribute("like")?.Value);

            foreach (var spawnPointElement in areaElement.Elements("SpawnPoint"))
            {
                var positionElement = spawnPointElement.Element("Position");
                float x = float.Parse(positionElement.Attribute("x")?.Value);
                float y = float.Parse(positionElement.Attribute("y")?.Value);
                float z = float.Parse(positionElement.Attribute("z")?.Value);

                float heading = float.Parse(spawnPointElement.Element("Heading")?.Value);
                string type = spawnPointElement.Attribute("type")?.Value?.ToLower() ?? "ped";

                string scenario = spawnPointElement.Attribute("scenario")?.Value;

                if (string.IsNullOrWhiteSpace(scenario))
                {
                    scenario = "none"; // Treat as no valid scenario
                }

                Vector3 position = new Vector3(x, y, z);
                area.AddSpawnPoint(position, heading, type, scenario);
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
                // Add relationship properties
                Hate = ParseRelationshipString(guardElement.Attribute("hates")?.Value),
                Dislike = ParseRelationshipString(guardElement.Attribute("dislikes")?.Value),
                Respect = ParseRelationshipString(guardElement.Attribute("respect")?.Value),
                Like = ParseRelationshipString(guardElement.Attribute("like")?.Value)
            };
            guardConfigs[guardName] = config;
        }

        return guardConfigs;
    }
}


public class GuardConfig
{
    public string Name { get; set; }
    public List<string> PedModels { get; set; }
    public List<string> Weapons { get; set; }
    public List<string> VehicleModels { get; set; }
    public string ScenarioOverride { get; set; }
    public List<string> Hate { get; set; } = new List<string>();
    public List<string> Dislike { get; set; } = new List<string>();
    public List<string> Respect { get; set; } = new List<string>();
    public List<string> Like { get; set; } = new List<string>();
}