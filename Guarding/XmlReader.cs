using GTA.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

public class XmlReader
{
    private readonly string _xmlFilePath;
    private readonly string _guardsXmlPath;
    private readonly string _scenarioXmlPath;
    private readonly Dictionary<string, Scenarios> _scenarioData; // Store scenarios in a dictionary for quick lookup

    public XmlReader(string areasFilePath)
    {
        _xmlFilePath = areasFilePath;
        _guardsXmlPath = Path.Combine(Path.GetDirectoryName(areasFilePath), "Guards.xml");
        _scenarioXmlPath = Path.Combine(Path.GetDirectoryName(areasFilePath), "ScenarioLists.xml");

        _scenarioData = LoadScenarios(); // Load once and use dictionary for fast retrieval
    }

    private List<string> ParseRelationshipString(string relationshipString)
    {
        return string.IsNullOrWhiteSpace(relationshipString)
            ? new List<string>()
            : relationshipString.Split(',')
                                .Select(x => x.Trim())
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .ToList();
    }

    public Dictionary<string, Scenarios> LoadScenarios()
    {
        var scenarios = new Dictionary<string, Scenarios>();
        XElement xml = XElement.Load(_scenarioXmlPath);

        foreach (var scenarioElement in xml.Elements("Scenario"))
        {
            string scenarioName = scenarioElement.Attribute("name")?.Value;
            if (string.IsNullOrWhiteSpace(scenarioName)) continue;

            List<string> scenarioAnimations = scenarioElement.Elements("Name")
                                                             .Select(e => e.Value)
                                                             .Where(name => !string.IsNullOrWhiteSpace(name))
                                                             .ToList();

            scenarios[scenarioName] = new Scenarios(scenarioName, scenarioAnimations);
        }

        return scenarios;
    }

    public List<Area> LoadAreasFromXml()
    {
        var areas = new List<Area>();
        XElement xml = XElement.Load(_xmlFilePath);

        foreach (var areaElement in xml.Elements("Area"))
        {
            string areaName = areaElement.Attribute("name")?.Value;
            string model = areaElement.Attribute("model")?.Value;
            string defaultScenario = areaElement.Attribute("scenario")?.Value;

            bool.TryParse(areaElement.Attribute("override")?.Value, out bool relationshipOverride);

            var hate = ParseRelationshipString(areaElement.Attribute("hates")?.Value);
            var dislike = ParseRelationshipString(areaElement.Attribute("dislikes")?.Value);
            var respect = areaElement.Attribute("respects")?.Value;
            var like = ParseRelationshipString(areaElement.Attribute("likes")?.Value);

            // Assign the scenario based on the default scenario name
            _scenarioData.TryGetValue(defaultScenario, out Scenarios assignedScenario);

            // Create area
            Area area = new Area(areaName, model, defaultScenario, hate, dislike, respect, like, assignedScenario, relationshipOverride);

            foreach (var spawnPointElement in areaElement.Elements("SpawnPoint"))
            {
                var positionElement = spawnPointElement.Element("Position");
                if (positionElement == null) continue;

                float.TryParse(positionElement.Attribute("x")?.Value, out float x);
                float.TryParse(positionElement.Attribute("y")?.Value, out float y);
                float.TryParse(positionElement.Attribute("z")?.Value, out float z);
                float.TryParse(spawnPointElement.Element("Heading")?.Value, out float heading);

                string type = spawnPointElement.Attribute("type")?.Value?.ToLower() ?? "ped";
                string scenario = spawnPointElement.Attribute("scenario")?.Value;
                bool.TryParse(spawnPointElement.Attribute("interior")?.Value, out bool interior);

                // Determine final animation
                string finalAnimation = scenario; // Direct override if provided
                if (string.IsNullOrEmpty(finalAnimation) && assignedScenario != null && assignedScenario.ScenarioList.Count > 0)
                {
                    finalAnimation = assignedScenario.ScenarioList[new Random().Next(assignedScenario.ScenarioList.Count)];
                }

                Vector3 position = new(x, y, z);
                area.AddSpawnPoint(position, heading, type, scenario, interior, finalAnimation);
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

            var config = new GuardConfig
            {
                Name = guardName,
                PedModels = guardElement.Elements("PedModel").Select(x => x.Value).ToList(),
                Weapons = guardElement.Elements("Weapon").Select(x => x.Value).ToList(),
                VehicleModels = guardElement.Elements("VehicleModel").Select(x => x.Value).ToList(),
                MVehicleModels = guardElement.Elements("MountedVehicleModel").Select(x => x.Value).ToList(),
                BVehicleModels = guardElement.Elements("BoatModel").Select(x => x.Value).ToList(),
                PVehicleModels = guardElement.Elements("PlaneModel").Select(x => x.Value).ToList(),
                HVehicleModels = guardElement.Elements("HelicopterModel").Select(x => x.Value).ToList(),
                LVehicleModels = guardElement.Elements("LargeVehicleModel").Select(x => x.Value).ToList(),
                RelationshipGroup = guardGroup,
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
