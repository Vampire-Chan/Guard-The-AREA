
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

public class XMLDataLoader
{
    public static Dictionary<string, WantedStarBase> LoadDispatchData(string xmlFilePath)
    {
        var doc = XDocument.Load(xmlFilePath);
        var dispatches = doc.Root.Element("DispatchVehicleInfo").Elements("Dispatch");

        var dispatchData = new Dictionary<string, List<VehicleInformation>>();

        foreach (var dispatchElement in dispatches)
        {
            string dispatchName = dispatchElement.Attribute("name").Value;
            var vehicleInfo = ParseVehicleInformation(dispatchElement);

            if (!dispatchData.ContainsKey(dispatchName))
            {
                dispatchData[dispatchName] = new List<VehicleInformation>();
            }

            dispatchData[dispatchName].Add(vehicleInfo);
        }

        var wantedStarData = new Dictionary<string, WantedStarBase>
        {
            { "One", new WantedStarOne() },
            { "Two", new WantedStarTwo() },
            { "Three", new WantedStarThree() },
            { "Four", new WantedStarFour() },
            { "Five", new WantedStarFive() },
        };

        var wantedLevels = doc.Root.Element("WantedLevels").Elements("WantedLevel");

        foreach (var level in wantedLevels)
        {
            string wantedLevel = level.Attribute("star").Value;
            var dispatchTypes = level.Elements("DispatchType");

            foreach (var dispatchType in dispatchTypes)
            {
                string type = dispatchType.Attribute("type").Value;
                var sets = dispatchType.Elements("DispatchSet").Select(ds => ds.Value).ToList();

                foreach (var set in sets)
                {
                    if (dispatchData.ContainsKey(set))
                    {
                        var vehicleInfos = dispatchData[set];

                        switch (wantedLevel)
                        {
                            case "One":
                                AddToWantedStar(wantedStarData["One"], type, vehicleInfos);
                                break;
                            case "Two":
                                AddToWantedStar(wantedStarData["Two"], type, vehicleInfos);
                                break;
                            case "Three":
                                AddToWantedStar(wantedStarData["Three"], type, vehicleInfos);
                                break;
                            case "Four":
                                AddToWantedStar(wantedStarData["Four"], type, vehicleInfos);
                                break;
                            case "Five":
                                AddToWantedStar(wantedStarData["Five"], type, vehicleInfos);
                                break;
                        }
                    }
                }
            }
        }

        return wantedStarData;
    }

    private static void AddToWantedStar(WantedStarBase wantedStar, string type, List<VehicleInformation> vehicleInfos)
    {
        switch (type)
        {
            case "Air":
                wantedStar.AirList.Add(new DispatchInfoAir { VehicleInfo = vehicleInfos });
                break;
            case "Ground":
                wantedStar.GroundList.Add(new DispatchInfoGround { VehicleInfo = vehicleInfos });
                break;
            case "Sea":
                wantedStar.SeaList.Add(new DispatchInfoSea { VehicleInfo = vehicleInfos });
                break;
        }
    }

    private static VehicleInformation ParseVehicleInformation(XElement dispatchElement)
    {
        var vehicles = dispatchElement.Elements("Vehicles")
            .Elements("Vehicle")
            .Select(v => v.Attribute("model")?.Value)
            .Where(v => v != null)
            .ToList() ?? new List<string>();

        var pilots = dispatchElement.Element("Pilots")?
            .Elements("Pilot")
            .Select(p => p.Value)
            .ToList() ?? new List<string>();

        var soldiers = dispatchElement.Element("Soldiers")?
            .Elements("Soldier")
            .Select(s => s.Value)
            .ToList() ?? new List<string>();

        var primaryWeapons = dispatchElement.Element("Weapons")?.Element("PrimaryWeapons")?
            .Elements("Weapon")
            .Select(w => w.Value)
            .ToList() ?? new List<string>();

        var secondaryWeapons = dispatchElement.Element("Weapons")?.Element("SecondaryWeapons")?
            .Elements("Weapon")
            .Select(w => w.Value)
            .ToList() ?? new List<string>();

        var vehicleWeapons = dispatchElement.Elements("Vehicles")
            .Elements("Vehicle")
            .Elements("VehicleWeapons")
            .Elements("Weapon")
            .Select(w => w.Value)
            .ToList() ?? new List<string>();

        var vehicleMods = dispatchElement.Elements("Vehicles")
            .Elements("Vehicle")
            .Elements("Mods")
            .Elements("Mod")
            .Select(m => new Mod
            {
                Type = m.Attribute("type").Value,
                Index = int.Parse(m.Attribute("index").Value)
            }).ToList() ?? new List<Mod>();

        return new VehicleInformation(
            vehicles,
            pilots,
            new SoldierInformation(soldiers, new List<WeaponInformation>
            {
                new WeaponInformation(primaryWeapons, secondaryWeapons)
            }),
            vehicleWeapons,
            vehicleMods
        );
    }
}