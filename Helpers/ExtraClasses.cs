using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    // PilotInformation.cs
    public class PilotInformation
    {
        public List<string> PilotModelList { get; set; } // List of pilot model names
    public List<WeaponInformation> WeaponInfoList { get; set; }

    public PilotInformation(List<string> pilotModels, List<WeaponInformation> weaponsInfoList)
        {
        PilotModelList = pilotModels;
        WeaponInfoList = weaponsInfoList;
        }
    }

    // SoldierInformation.cs
    public class SoldierInformation
    {
        public List<string> SoldierModels { get; set; }
        public List<WeaponInformation> WeaponInfoList { get; set; }

        public SoldierInformation(List<string> soldierModels, List<WeaponInformation> weaponInfoList)
        {
            SoldierModels = soldierModels;
            WeaponInfoList = weaponInfoList;
        }
}
public enum GuardType
{
    Ped,        // guard, ped, soldier, npc
    Vehicle,    // vehicle, car, bike, cycle
    Helicopter, // heli, helicopter, chopper, copter
    Boat,       // boat, ship, seabike, jetski
    Mounted,    // gunner, mounted, turret
    LargeVehicle,    // bus, truck, trailer
    LargeHelicopter, // cargobob, bigheli, largehelicopter
    CargoPlane,      // cargoplane, globemaster
    LargeBoat        // warship, tugboat, largeboat, submarine, bigship
}
public class RelationshipManager
{
    public List<string> Hate { get; set; } = new List<string>();
    public List<string> Dislike { get; set; } = new List<string>();
    public string Respect { get; set; }
    public List<string> Like { get; set; } = new List<string>();

    public RelationshipManager(List<string> hate, List<string> dislike, string respect, List<string> like)
    {
        Hate = hate;
        Dislike = dislike;
        Respect = respect;
        Like = like;
    }
}
// VehicleHealth.cs
public class VehicleHealth
    {
        public int? Engine { get; set; }
        public int? Body { get; set; }
        public int? Petrol { get; set; }

        public VehicleHealth(int? engine, int? body, int? petrol)
        {
            Engine = engine;
            Body = body;
            Petrol = petrol;
        }
    }

    // VehicleInformation.cs --
    public class VehicleInformation
    {
        public VehicleDetails VehicleDetails { get; set; } // Single VehicleDetails, not a list
        public PilotInformation PilotInfo { get; set; } // Renamed to PilotInfo, single object
        public SoldierInformation SoldierInfo { get; set; } // Single SoldierInfo object

        public VehicleInformation(VehicleDetails vehicleDetails, PilotInformation pilotInfo, SoldierInformation soldierInfo)
        {
            VehicleDetails = vehicleDetails;
            PilotInfo = pilotInfo;
            SoldierInfo = soldierInfo;
        }
    }

    // VehicleLivery.cs
    public class VehicleLivery
    {
        public string Set { get; set; }
        public int Index { get; set; }

        /// <summary>
        /// Use Set for LiveryType, and for LiveryType2
        /// </summary>
        /// <param name="set">the Livery, Livery2</param>
        /// <param name="index">the index.</param>
        public VehicleLivery(string set, int index)
        {
            Set = set;
            Index = index;
        }
    }

    // WeaponDetails.cs
    public class WeaponDetails
    {
        public string Name { get; set; }
        public List<string> Attachments { get; set; }
        public int? Ammo { get; set; }
        public int? Magazine { get; set; }
        public string Flag { get; set; }

        public WeaponDetails(string name, List<string> attachments, int? ammo, int? magazine, string flag)
        {
            Name = name;
            Attachments = attachments;
            Ammo = ammo;
            Magazine = magazine;
            Flag = flag;
        }
    }

    // WeaponInformation.cs
    public class WeaponInformation
    {
        public List<WeaponDetails> PrimaryWeaponList { get; set; }
        public List<WeaponDetails> SecondaryWeaponList { get; set; }

        public WeaponInformation(List<WeaponDetails> primaryWeaponList, List<WeaponDetails> secondaryWeaponList)
        {
            PrimaryWeaponList = primaryWeaponList;
            SecondaryWeaponList = secondaryWeaponList;
        }
    }

    // DispatchInfoBase.cs
    public class DispatchInfoAir
    {
        public List<VehicleInformation> VehicleInfo { get; set; }

        public DispatchInfoAir(List<VehicleInformation> vehicleInfo)
        {
            VehicleInfo = vehicleInfo;
        }
    }

    public class DispatchInfoGround
    {
        public List<VehicleInformation> VehicleInfo { get; set; }

        public DispatchInfoGround(List<VehicleInformation> vehicleInfo)
        {
            VehicleInfo = vehicleInfo;
        }
    }

    public class DispatchInfoSea
    {
        public List<VehicleInformation> VehicleInfo { get; set; }

        public DispatchInfoSea(List<VehicleInformation> vehicleInfo)
        {
            VehicleInfo = vehicleInfo;
        }
    }

    // WantedStarBase.cs
    // Base class and derived classes for Wanted Stars
    public class WantedStarBase
    {
        public List<DispatchInfoAir> AirList { get; set; }
        public List<DispatchInfoGround> GroundList { get; set; }
        public List<DispatchInfoSea> SeaList { get; set; }

        public WantedStarBase()
        {
            AirList = new List<DispatchInfoAir>();
            GroundList = new List<DispatchInfoGround>();
            SeaList = new List<DispatchInfoSea>();
        }
    }

    public class WantedStarOne : WantedStarBase { public WantedStarOne() : base() { } }
    public class WantedStarTwo : WantedStarBase { public WantedStarTwo() : base() { } }
    public class WantedStarThree : WantedStarBase { public WantedStarThree() : base() { } }
    public class WantedStarFour : WantedStarBase { public WantedStarFour() : base() { } }
    public class WantedStarFive : WantedStarBase { public WantedStarFive() : base() { } }

    //VehicleDetails.cs
    public class VehicleDetails
    {
        public string Name { get; set; }
        public List<WeaponDetails> VehicleWeaponDetailsList { get; set; }
        public List<Mod> VehicleMods { get; set; }
        public VehicleHealth VehicleHealth { get; set; }
        public List<VehicleLivery> VehicleLiveries { get; set; }
        public List<string> VehicleTasks { get; set; }

        public VehicleDetails(string name, List<WeaponDetails> vehicleWeaponDetailsList, List<Mod> vehicleMods, VehicleHealth vehicleHealth, List<VehicleLivery> vehicleLiveries, List<string> vehicleTasks)
        {
            Name = name;
            VehicleWeaponDetailsList = vehicleWeaponDetailsList;
            VehicleMods = vehicleMods;
            VehicleHealth = vehicleHealth;
            VehicleLiveries = vehicleLiveries;
            VehicleTasks = vehicleTasks;
        }
    }

    // Mod.cs
    public class Mod
    {
        public string Type { get; set; }
        public int Index { get; set; }

        public Mod(string type, int index)
        {
            Type = type;
            Index = index;
        }
    }

    public enum PedRelationship : byte // Enums at top - 1. Enums
    {
        None = 255,
        Respect = 0,
        Like,         //  1
        Ignore,       //  2
        Dislike,     //  3
        Wanted,       //  4
        Hate,         //  5
        Dead          //  6
    }

    public enum DispatchType : uint
    {
        DT_Invalid,
        DT_PoliceAutomobile,
        DT_PoliceHelicopter,
        DT_FireDepartment,
        DT_SwatAutomobile,
        DT_AmbulanceDepartment,
        DT_PoliceRiders,
        DT_PoliceVehicleRequest,
        DT_PoliceRoadBlock,
        DT_PoliceAutomobileWaitPulledOver,
        DT_PoliceAutomobileWaitCruising,
        DT_Gangs,
        DT_SwatHelicopter,
        DT_PoliceBoat,
        DT_ArmyVehicle,
        DT_BikerBackup,
        DT_Assassins
    }

public class VehicleGroup
{
    public string Name { get; set; }
}
