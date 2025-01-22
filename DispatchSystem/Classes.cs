using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guarding.DispatchSystem
{
    // Details about helicopter model, driver, soldiers, and vehicle weapons (currently unused)
    public class VehicleInformation
    {
        public List<string> Helicopters { get; set; }
        public List<string> Pilot { get; set; }
        public SoldierInformation Soldiers { get; set; }

        public VehicleInformation(List<string> helicopters, List<string> pilot, SoldierInformation soldiers)
        {
            Helicopters = helicopters;
            Pilot = pilot;
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
            Soldiers = soldiers;
            Weapons = weapons;
        }
    }

    // Details for model of weapon, primary, secondary
    public class WeaponInformation
    {
        public List<string> PrimaryWeapon { get; set; }
        public List<string> SecondaryWeapon { get; set; }

        public WeaponInformation(List<string> primaryWeapon, List<string> secondaryWeapon)
        {
            PrimaryWeapon = primaryWeapon;
            SecondaryWeapon = secondaryWeapon;
        }
    }
}