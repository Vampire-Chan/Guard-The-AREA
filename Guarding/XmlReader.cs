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

    public XmlReader(string areasFilePath)
    {
        _xmlFilePath = areasFilePath;
        _guardsXmlPath = Path.Combine(Path.GetDirectoryName(areasFilePath), "Guards.xml");
        _scenarioXmlPath = Path.Combine(Path.GetDirectoryName(areasFilePath), "ScenarioLists.xml");

        Logger.Log.Info($"XmlReader initialized with Areas: {_xmlFilePath}, Guards: {_guardsXmlPath}, Scenarios: {_scenarioXmlPath}");
    }

    private List<string> ParseRelationshipString(string relationshipString)
    {
        return string.IsNullOrWhiteSpace(relationshipString)
            ? new List<string>()
            : relationshipString.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }

    public Dictionary<string, Scenarios> LoadScenarios()
    {
        Logger.Log.Info("Loading scenarios...");
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
            Logger.Log.Info($"Loaded Scenario: {scenarioName} with {scenarioAnimations.Count} animations");
        }

        return scenarios;
    }

    public List<Area> LoadAreasFromXml(Dictionary<string, Scenarios> scenarios = null)
    {
        Logger.Log.Info("Loading areas...");
        var areas = new List<Area>();
        XElement xml = XElement.Load(_xmlFilePath);

        foreach (var areaElement in xml.Elements("Area"))
        {
            string areaName = areaElement.Attribute("name")?.Value;
            string model = areaElement.Attribute("model")?.Value;
            string defaultScenario = areaElement.Attribute("scenario")?.Value;
            bool.TryParse(areaElement.Attribute("override")?.Value, out bool relationshipOverride);

            // Parse shift attributes
            bool.TryParse(areaElement.Attribute("shiftEnabled")?.Value, out bool shiftEnabled);
            string shiftDuration = areaElement.Attribute("shiftDuration")?.Value;

            // Parse backup attributes
            bool.TryParse(areaElement.Attribute("allowsBackup")?.Value ?? "true", out bool allowsBackup);
            int.TryParse(areaElement.Attribute("charges")?.Value ?? "0", out int dailyCharges);

            // Parse backup fees from BackupFees element
            var backupFeesElement = areaElement.Element("BackupFees");
            var backupFees = new BackupFeesConfig();
            if (backupFeesElement != null)
            {
                var aerialElement = backupFeesElement.Element("Aerial");
                if (aerialElement != null)
                {
                    int.TryParse(aerialElement.Attribute("cost")?.Value ?? "5000", out int aerialCost);
                    int.TryParse(aerialElement.Attribute("cooldown")?.Value ?? "30", out int aerialCooldown);
                    backupFees.AerialCost = aerialCost;
                    backupFees.AerialCooldown = aerialCooldown;
                }

                var airstrikeElement = backupFeesElement.Element("Airstrike");
                if (airstrikeElement != null)
                {
                    int.TryParse(airstrikeElement.Attribute("cost")?.Value ?? "50000", out int airstrikeCost);
                    int.TryParse(airstrikeElement.Attribute("cooldown")?.Value ?? "30", out int airstrikeCooldown);
                    backupFees.AirstrikeCost = airstrikeCost;
                    backupFees.AirstrikeCooldown = airstrikeCooldown;
                }

                var groundElement = backupFeesElement.Element("Ground");
                if (groundElement != null)
                {
                    int.TryParse(groundElement.Attribute("cost")?.Value ?? "15000", out int groundCost);
                    int.TryParse(groundElement.Attribute("cooldown")?.Value ?? "30", out int groundCooldown);
                    backupFees.GroundCost = groundCost;
                    backupFees.GroundCooldown = groundCooldown;
                }
            }

            var hate = ParseRelationshipString(areaElement.Attribute("hates")?.Value);
            var dislike = ParseRelationshipString(areaElement.Attribute("dislikes")?.Value);
            var respect = areaElement.Attribute("respects")?.Value;
            var like = ParseRelationshipString(areaElement.Attribute("likes")?.Value);

            // Use the passed scenarios parameter instead of static field
            Scenarios assignedScenario = null;
            if (scenarios != null)
            {
                scenarios.TryGetValue(defaultScenario, out assignedScenario);
            }

            Area area = new Area(areaName, model, defaultScenario, hate, dislike, respect, like, assignedScenario, relationshipOverride)
            {
                ShiftEnabled = shiftEnabled,
                ShiftDuration = shiftDuration,
                AllowsBackup = allowsBackup,
                DailyCharges = dailyCharges,
                BackupFees = backupFees
            };

            // Parse optional backup spawn interval (seconds) per area
            int.TryParse(areaElement.Attribute("backupSpawnInterval")?.Value ?? "0", out int spawnInterval);
            area.BackupSpawnIntervalSeconds = spawnInterval;

            if (spawnInterval > 0)
            {
                Logger.Log.Info($"Area {areaName}: backupSpawnInterval set to {spawnInterval} seconds");
            }

            Logger.Log.Info($"Created Area: {areaName}, Scenario: {defaultScenario}, Model: {model}, ShiftEnabled: {shiftEnabled}, ShiftDuration: {shiftDuration}, AllowsBackup: {allowsBackup}, DailyCharges: {dailyCharges}");

            foreach (var spawnPointElement in areaElement.Elements("SpawnPoint"))
            {
                var positionElement = spawnPointElement.Element("Position");
                if (positionElement == null)
                {
                    Logger.Log.Info("Skipped spawn point due to missing Position");
                    continue;
                }

                float.TryParse(positionElement.Attribute("x")?.Value, out float x);
                float.TryParse(positionElement.Attribute("y")?.Value, out float y);
                float.TryParse(positionElement.Attribute("z")?.Value, out float z);
                float.TryParse(spawnPointElement.Element("Heading")?.Value, out float heading);

                string type = spawnPointElement.Attribute("type")?.Value?.ToLower() ?? "ped";
                string scenario = spawnPointElement.Attribute("scenario")?.Value;
                bool.TryParse(spawnPointElement.Attribute("interior")?.Value, out bool interior);

                string finalAnimation = scenario;
                if (string.IsNullOrEmpty(finalAnimation) && assignedScenario != null && assignedScenario.ScenarioList.Count > 0)
                {
                    finalAnimation = assignedScenario.ScenarioList[new Random().Next(assignedScenario.ScenarioList.Count)];
                }

                Vector3 position = new(x, y, z);
                area.AddSpawnPoint(position, heading, type, scenario, interior, finalAnimation);
                Logger.Log.Info($"  SpawnPoint at ({x},{y},{z}) Type: {type}, Scenario: {finalAnimation}");
            }

            areas.Add(area);
        }

        Logger.Log.Info($"Finished loading {areas.Count} areas.");
        return areas;
    }


        public Dictionary<string, GuardConfig> LoadGuardConfigs()
    {
        Logger.Log.Info("Loading guard configs...");
        var guardConfigs = new Dictionary<string, GuardConfig>();
        XElement xml = null;
        try
        {
            xml = XElement.Load(_guardsXmlPath);
        }
        catch (Exception ex)
        {
            var cleanedPath = Path.Combine(Path.GetDirectoryName(_guardsXmlPath), "Guards.cleaned.xml");
            Logger.Log.Warn($"Failed to load {_guardsXmlPath}: {ex.Message}. Trying fallback: {cleanedPath}");
            if (File.Exists(cleanedPath))
            {
                xml = XElement.Load(cleanedPath);
            }
            else
            {
                throw;
            }
        }

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
            Logger.Log.Info($"Loaded GuardConfig: {guardName}, Peds: {config.PedModels.Count}, Weapons: {config.Weapons.Count}");
        }

        Logger.Log.Info($"Finished loading {guardConfigs.Count} guards.");
        return guardConfigs;
    }
}
