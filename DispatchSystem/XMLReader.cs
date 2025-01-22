using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Guarding.DispatchSystem
{
    public class XMLDataLoader
    {
        public static VehicleInformation LoadVehicleInformation(string xmlFilePath)
        {
            var doc = XDocument.Load(xmlFilePath);

            var dispatchElement = doc.Root.Element("DispatchVehicleInfo").Element("Dispatch");

            var vehicles = dispatchElement.Elements("VehicleModel")
                .Select(v => v.Value)
                .ToList();

            var pilots = dispatchElement.Element("Pilots")
                .Elements("Pilot")
                .Select(p => p.Value)
                .ToList();

            var soldiers = dispatchElement.Element("Soldiers")
                .Elements("Soldier")
                .Select(s => s.Value)
                .ToList();

            var primaryWeapons = dispatchElement.Element("Weapons").Element("PrimaryWeapons")
                .Elements("Weapon")
                .Select(w => w.Value)
                .ToList();

            var secondaryWeapons = dispatchElement.Element("Weapons").Element("SecondaryWeapons")
                .Elements("Weapon")
                .Select(w => w.Value)
                .ToList();

            return new VehicleInformation(
                vehicles,
                pilots,
                new SoldierInformation(soldiers, new List<WeaponInformation>
                {
                    new WeaponInformation(primaryWeapons, secondaryWeapons)
                })
            );
        }

        public static List<string> LoadDispatchSets(string xmlFilePath, string wantedLevel, string dispatchType)
        {
            var doc = XDocument.Load(xmlFilePath);

            var dispatchSets = doc.Root.Element("WantedLevels")
                .Elements("WantedLevel")
                .Where(wl => wl.Attribute("star").Value.Equals(wantedLevel, System.StringComparison.OrdinalIgnoreCase))
                .Elements("DispatchType")
                .Where(dt => dt.Attribute("type").Value.Equals(dispatchType, System.StringComparison.OrdinalIgnoreCase))
                .Elements("DispatchSet")
                .Select(ds => ds.Value)
                .ToList();

            return dispatchSets;
        }
    }
}