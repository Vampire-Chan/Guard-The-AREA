using System.Collections.Generic;


//public class WantedStarOne
//{
//    public List<DispatchInfoAir> AirList { get; set; } = new List<DispatchInfoAir>();
//    public List<DispatchInfoGround> GroundList { get; set; } = new List<DispatchInfoGround>();
//    public List<DispatchInfoSea> SeaList { get; set; } = new List<DispatchInfoSea>();
//}

//public class WantedStarTwo
//{
//    public List<DispatchInfoAir> AirList { get; set; } = new List<DispatchInfoAir>();
//    public List<DispatchInfoGround> GroundList { get; set; } = new List<DispatchInfoGround>();
//    public List<DispatchInfoSea> SeaList { get; set; } = new List<DispatchInfoSea>();
//}

//public class WantedStarThree
//{
//    public List<DispatchInfoAir> AirList { get; set; } = new List<DispatchInfoAir>();
//    public List<DispatchInfoGround> GroundList { get; set; } = new List<DispatchInfoGround>();
//    public List<DispatchInfoSea> SeaList { get; set; } = new List<DispatchInfoSea>();
//}

//public class WantedStarFour
//{
//    public List<DispatchInfoAir> AirList { get; set; } = new List<DispatchInfoAir>();
//    public List<DispatchInfoGround> GroundList { get; set; } = new List<DispatchInfoGround>();
//    public List<DispatchInfoSea> SeaList { get; set; } = new List<DispatchInfoSea>();
//}

//public class WantedStarFive
//{
//    public List<DispatchInfoAir> AirList { get; set; } = new List<DispatchInfoAir>();
//    public List<DispatchInfoGround> GroundList { get; set; } = new List<DispatchInfoGround>();
//    public List<DispatchInfoSea> SeaList { get; set; } = new List<DispatchInfoSea>();
//}
//somethin like this i want to categorize it, well give me idea on hwo to do it
public class DispatchInfoAir
{
    public List<VehicleInformation> VehicleInfo { get; set; }

}
public class DispatchInfoGround
{
    public List<VehicleInformation> VehicleInfo { get; set; }
}
public class DispatchInfoSea
{
    public List<VehicleInformation> VehicleInfo { get; set; }
}
public class VehicleInformation
{
    public List<string> VehicleModels { get; set; }
    public List<string> Pilot { get; set; }
    public SoldierInformation Soldiers { get; set; }
    public List<string> VehicleWeapons { get; set; }
    public List<Mod> VehicleMods { get; set; }

    public VehicleInformation(
        List<string> vehicleModels,
        List<string> pilot,
        SoldierInformation soldiers,
        List<string> vehicleWeapons,
        List<Mod> vehicleMods)
    {
        VehicleModels = vehicleModels ?? new List<string>();
        Pilot = pilot ?? new List<string>();
        Soldiers = soldiers;
        VehicleWeapons = vehicleWeapons ?? new List<string>();
        VehicleMods = vehicleMods ?? new List<Mod>();
    }
}

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

public class Mod
{
    public string Type { get; set; }
    public int Index { get; set; }
}
