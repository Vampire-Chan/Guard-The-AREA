
using GTA;
using GTA.Math;
using GTA.Native;
using Guarding.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

public class GroundVehicle
{
    public enum VehicleTask
    {
        Idle,
        Goto,
        Going,
        Follow,
        Following,
        Search,
        Searching,
        Flee,
        Fleeing,
    }

    public Ped Driver { get; set; }
    public Vehicle Vehicle { get; set; }
    private Dictionary<VehicleWeaponHash, bool> VehicleWeapon { get; set; }
    public VehicleTask Task { get; set; }
    private VehicleTask PreviousTask { get; set; }
    private int TaskStartTime { get; set; }

    public List<Ped> Crew = new List<Ped>();
    private List<Ped> WeaponCrewMembers = new List<Ped>();
    private Vector3 ReachTo;
    public GroundVehicle(Vehicle vehicle, Vector3 postogo)
    {
        Vehicle = vehicle;
        Driver = vehicle.Driver;
        Crew = vehicle.Passengers.ToList();
        VehicleWeapon = new Dictionary<VehicleWeaponHash, bool>();
        ReachTo = postogo;
        //Task = VehicleTask.Goto;
        //Driver.Task.GoToPointAnyMeans(ImportantChecks.LastKnownLocation, PedMoveBlendRatio.Sprint, Vehicle, false, VehicleDrivingFlags.DrivingModeAvoidVehicles);
        //Driver.PopulationType = EntityPopulationType.RandomAmbient;
        TaskStartTime = Game.GameTime;
        Driver.SetCombatAttribute(CombatAttributes.CanInvestigate, true);
        Initialize();
        //Driver.Task.StartVehicleMission(Vehicle, ImportantChecks.LastKnownLocation,
        //               VehicleMissionType.Stop, 50f, VehicleDrivingFlags.DrivingModeAvoidVehicles, 40, -1);

        Driver.SetCombatAttribute(CombatAttributes.UseVehicleAttack, false); //make police harass you 💀 by ramming you
        Driver.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, true);
        
        foreach (var crewMember in Crew)
        {
            if (crewMember.Exists() && crewMember.IsAlive)
            {
               crewMember.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, true);
            }
        }
    }

    void Initialize()
    {
        Driver.Task.DriveTo(Vehicle, ReachTo, 100, VehicleDrivingFlags.DrivingModeAvoidVehiclesReckless | VehicleDrivingFlags.SteerAroundPeds, 50);
    }
   

    public bool IsVehicleValid()
    {
        return Vehicle != null && Vehicle.Exists() && !Vehicle.IsDead &&
               Driver != null && Driver.Exists() && !Driver.IsDead;
    }

   
}
//Improve this File.dont change the public properties names, but rest flow to be improved as well as make sure it doesnt break the logic flow.