using System.Collections.Generic;

namespace Guarding.DispatchSystem
{
    // Details about helicopter model, driver, soldiers, and vehicle weapons (currently unused)
    public class VehicleInformation
    {
        public List<string> VehicleModels { get; set; }
        public List<string> Pilot { get; set; }
        public SoldierInformation Soldiers { get; set; }

        public VehicleInformation(List<string> vehicleModels, List<string> pilot, SoldierInformation soldiers)
        {
            VehicleModels = vehicleModels ?? new List<string>();
            Pilot = pilot ?? new List<string>();
            Soldiers = soldiers;
        }
    }

    // Details about soldiers and their weapon-primary and secondary
    public class SoldierInformation
    {
        public List<string> Soldiers { get; set; }
        public List<WeaponInformation> Weapons { get; set; }

        public SoldierInformation(List<string> soldiers, List<WeaponInformation> weapons)
        {
            Soldiers = soldiers ?? new List<string>();
            Weapons = weapons ?? new List<WeaponInformation>();
        }
    }

    // Details for model of weapon, primary, secondary
    public class WeaponInformation
    {
        public List<string> PrimaryWeapon { get; set; }
        public List<string> SecondaryWeapon { get; set; }

        public WeaponInformation(List<string> primaryWeapon, List<string> secondaryWeapon)
        {
            PrimaryWeapon = primaryWeapon ?? new List<string>();
            SecondaryWeapon = secondaryWeapon ?? new List<string>();
        }
    }
}