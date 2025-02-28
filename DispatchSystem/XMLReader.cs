
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

public class XMLDataLoader
{
    /// <summary>
    /// Loads dispatch data from XML, implementing Option 2: Dispatch Sets as collections of VehicleInformation objects.
    /// </summary>
    /// <param name="xmlFilePath">Path to XML dispatch file.</param>
    /// <returns>Dictionary of WantedStarBase objects.</returns>
    public static Dictionary<string, WantedStarBase> LoadDispatchData(string xmlFilePath)
    {
        var doc = XDocument.Load(xmlFilePath);
        var dispatches = doc.Root?.Element("DispatchVehicleInfo")?.Elements("Dispatch");
        if (dispatches == null) return new Dictionary<string, WantedStarBase>();

        var dispatchData = new Dictionary<string, List<VehicleInformation>>();

        foreach (var dispatchElement in dispatches)
        {
            string dispatchName = dispatchElement.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(dispatchName)) continue;

            var vehicleInfos = ParseVehicleInformationList(dispatchElement); // Parse to get List<VehicleInformation> for each Dispatch
            dispatchData[dispatchName] = vehicleInfos; // Store the list of VehicleInformation objects
        }

        var wantedStarData = new Dictionary<string, WantedStarBase>
        {
            { "One", new WantedStarOne() },
            { "Two", new WantedStarTwo() },
            { "Three", new WantedStarThree() },
            { "Four", new WantedStarFour() },
            { "Five", new WantedStarFive() },
        };

        var wantedLevels = doc.Root?.Element("WantedLevels")?.Elements("WantedLevel");
        if (wantedLevels == null) return wantedStarData;


        foreach (var level in wantedLevels)
        {
            string wantedLevel = level.Attribute("star")?.Value;
            if (string.IsNullOrEmpty(wantedLevel)) continue;

            var dispatchTypes = level.Elements("DispatchType");
            foreach (var dispatchType in dispatchTypes)
            {
                string type = dispatchType.Attribute("type")?.Value;
                if (string.IsNullOrEmpty(type)) continue;

                var sets = dispatchType.Elements("DispatchSet").Select(ds => ds.Value).ToList();
                foreach (var set in sets)
                {
                    if (dispatchData.ContainsKey(set))
                    {
                        var vehicleInfos = dispatchData[set]; // Get the LIST of VehicleInformation objects for this DispatchSet
                        switch (wantedLevel)
                        {
                            case "One": AddToWantedStar(wantedStarData["One"], type, vehicleInfos); break;
                            case "Two": AddToWantedStar(wantedStarData["Two"], type, vehicleInfos); break;
                            case "Three": AddToWantedStar(wantedStarData["Three"], type, vehicleInfos); break;
                            case "Four": AddToWantedStar(wantedStarData["Four"], type, vehicleInfos); break;
                            case "Five": AddToWantedStar(wantedStarData["Five"], type, vehicleInfos); break;
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
                wantedStar.AirList.AddRange(vehicleInfos.Select(vi => new DispatchInfoAir(new List<VehicleInformation> { vi }))); // Still wrap, but now adding lists of VehicleInformation
                break;
            case "Ground":
                wantedStar.GroundList.AddRange(vehicleInfos.Select(vi => new DispatchInfoGround(new List<VehicleInformation> { vi })));
                break;
            case "Sea":
                wantedStar.SeaList.AddRange(vehicleInfos.Select(vi => new DispatchInfoSea(new List<VehicleInformation> { vi })));
                break;
        }
    }


    private static List<VehicleInformation> ParseVehicleInformationList(XElement dispatchElement)
    {
        var vehicleInfoList = new List<VehicleInformation>(); // This will now hold MULTIPLE VehicleInformation objects PER DispatchSet
        var vehicleElements = dispatchElement.Element("Vehicles")?.Elements("Vehicle");
        if (vehicleElements == null) return vehicleInfoList;

        foreach (var vehicleElement in vehicleElements)
        {
            // For each Vehicle in the Dispatch, parse and ADD a VehicleInformation object to the list
            vehicleInfoList.Add(ParseVehicleInformation(vehicleElement, dispatchElement)); // Parse EACH Vehicle element
        }
        return vehicleInfoList; // Return the LIST of VehicleInformation objects for this DispatchSet
    }


    private static VehicleInformation ParseVehicleInformation(XElement vehicleElement, XElement dispatchElement)
    {
        string vehicleModel = vehicleElement.Attribute("model")?.Value;
        string taskAttributeValue = vehicleElement.Attribute("task")?.Value;
        List<string> vehicleTasks = string.IsNullOrEmpty(taskAttributeValue)
            ? null
            : taskAttributeValue.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        // Get Pilots and Soldiers LISTS from the PARENT dispatchElement (for Option 2)
        List<string> pilots = dispatchElement.Element("Pilots")?.Elements("Pilot").Select(p => p.Value).ToList();
        List<string> soldiers = dispatchElement.Element("Soldiers")?.Elements("Soldier").Select(s => s.Value).ToList();

        // Parse Soldier Weapons (as before - these are still at the Dispatch level in XML)
        List<WeaponDetails> primaryWeapons = ParseSoldierWeapons(dispatchElement, "PrimaryWeapons");
        List<WeaponDetails> secondaryWeapons = ParseSoldierWeapons(dispatchElement, "SecondaryWeapons");

        // Parse Vehicle Specific Details (as before - these are at the Vehicle level in XML)
        List<WeaponDetails> vehicleWeaponDetailsList = ParseVehicleWeapons(vehicleElement);
        List<Mod> vehicleMods = ParseVehicleMods(vehicleElement);
        VehicleHealth vehicleHealth = ParseVehicleHealth(vehicleElement);
        List<VehicleLivery> vehicleLiveries = ParseVehicleLiveries(vehicleElement);


        // Create VehicleDetails object (as before) - This is SPECIFIC to each <Vehicle> element
        var vehicleDetails = new VehicleDetails(
            vehicleModel,
            vehicleWeaponDetailsList,
            vehicleMods,
            vehicleHealth,
            vehicleLiveries,
            vehicleTasks
        );

        // Create PilotInformation object -  Associate the DispatchSet's pilot LIST with EACH VehicleInformation
        var pilotInfo = new PilotInformation (pilots ?? new List<string>(), new List<WeaponInformation> { new WeaponInformation(primaryWeapons, secondaryWeapons) } );

        // Create SoldierInformation object - Associate the DispatchSet's soldier LIST with EACH VehicleInformation
        SoldierInformation soldierInfo = null;
        if (soldiers != null)
        {
            soldierInfo = new SoldierInformation(
                soldiers,
                new List<WeaponInformation> { new WeaponInformation(primaryWeapons, secondaryWeapons) } // WeaponInfo is also at DispatchSet level
            );
        }

        // Return ONE VehicleInformation object - for EACH <Vehicle> parsed in the DispatchSet
        return new VehicleInformation(
            vehicleDetails,
            pilotInfo,
            soldierInfo
        );
    }


    private static List<WeaponDetails> ParseSoldierWeapons(XElement dispatchElement, string weaponsElementName)
    {
        return dispatchElement.Element("Weapons")?.Element(weaponsElementName)?.Elements("Weapon")?.Select(w => new WeaponDetails(
            w.Attribute("name")?.Value,
            w.Attribute("attachments")?.Value?.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
            int.TryParse(w.Attribute("ammo")?.Value, out int ammo) ? (int?)ammo : null,
            int.TryParse(w.Attribute("magazine")?.Value, out int mag) ? (int?)mag : null,
            null // Flag is not relevant for soldier weapons
        )).ToList() ?? new List<WeaponDetails>();
    }


    private static List<WeaponDetails> ParseVehicleWeapons(XElement vehicleElement)
    {
        return vehicleElement.Element("VehicleWeapons")?.Elements("Weapon")?.Select(w => new WeaponDetails(
                 w.Value,
                 null, // Attachments - not for vehicle weapons
                 int.TryParse(w.Attribute("ammo")?.Value, out int ammo) ? (int?)ammo : null,
                 null, // Magazine - not for vehicle weapons
                 w.Attribute("flag")?.Value
             )).ToList() ?? new List<WeaponDetails>();
    }


    private static List<Mod> ParseVehicleMods(XElement vehicleElement)
    {
        return vehicleElement.Element("Mods")?.Elements("Mod")?.Select(m => new Mod(
            m.Attribute("type")?.Value,
            int.TryParse(m.Attribute("index")?.Value, out int index) ? index : 0
        )).ToList() ?? new List<Mod>();
    }

    private static VehicleHealth ParseVehicleHealth(XElement vehicleElement)
    {
        XElement healthElement = vehicleElement.Element("Health");
        return new VehicleHealth(
            int.TryParse(healthElement?.Attribute("engine")?.Value, out int engineHealth) ? (int?)engineHealth : null,
            int.TryParse(healthElement?.Attribute("body")?.Value, out int bodyHealth) ? (int?)bodyHealth : null,
            int.TryParse(healthElement?.Attribute("petrol")?.Value, out int petrolHealth) ? (int?)petrolHealth : null
        );
    }

    private static List<VehicleLivery> ParseVehicleLiveries(XElement vehicleElement)
    {
        return vehicleElement.Element("Liveries")?.Elements("Livery")?.Select(liveryElement => new VehicleLivery(
            liveryElement.Attribute("set")?.Value,
            int.TryParse(liveryElement.Attribute("index")?.Value, out int index) ? index : 0
        )).ToList() ?? new List<VehicleLivery>();
    }
}
