using GTA;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

public class Boat
{
    private Vehicle Watercraft { get; set; }
    private Ped Driver { get; set; } // Single pilot Ped
    private Dictionary<VehicleWeaponHash, bool> weaponStates;
    public VehicleTask CurrentTask { get; private set; }
    private bool _isSearchTaskActive = false;
    private bool _isGoToPlayerTaskActive = false;


    public enum VehicleTask
    {
        SearchGoToZone,
        Pursue,
        Attack = 1,
        Flee,
        None
    }

    // Updated constructor to accept VehicleDetails, PilotInformation, SoldierInformation
    public Boat(Vehicle boat, Vector3 pos)
    {
        Watercraft = boat;
        Driver = Watercraft.Driver; //Initialize Driver here
        if (Driver == null || !Driver.Exists())
        {
            //Handle case where driver is null, maybe spawn one or log an error
            Debug.WriteLine("Warning: Boat Driver is null or invalid in constructor!");
            return; // Early exit if driver is invalid.
        }
        TaskSearch(pos); // Initial task could be search or pursue based on your need
    }

    public void UpdateProcess()
    {
        if (!IsBoatValid())
            return;

        UpdateTaskLogic();
        PerformCurrentTask();

    }

    private void UpdateTaskLogic()
    {
        if (Game.Player.AreWantedStarsGrayedOut)
        {
            if (CurrentTask != VehicleTask.SearchGoToZone)
            {
                CurrentTask = VehicleTask.SearchGoToZone;
                _isSearchTaskActive = false; // Allow SearchTask to run again
                _isGoToPlayerTaskActive = false; // Ensure other tasks are not active
            }
        }
        else
        {
            if (CurrentTask != VehicleTask.Pursue)
            {
                CurrentTask = VehicleTask.Pursue;
                _isSearchTaskActive = false; // Ensure other tasks are not active
                _isGoToPlayerTaskActive = false; // Allow GoToPlayerTask to run again
            }
        }
    }

    private void PerformCurrentTask()
    {
        switch (CurrentTask)
        {
            case VehicleTask.SearchGoToZone:
                PerformSearchTask();
                break;
            case VehicleTask.Pursue:
                PerformGoToPlayerTask();
                break;
            case VehicleTask.None:
            default:
                break;
        }
    }


    void PerformGoToPlayerTask()
    {
        if (!_isGoToPlayerTaskActive)
        {
            Driver.Task.StartBoatMission(Watercraft, Game.Player.Character.Position, VehicleMissionType.GoTo, 80, (VehicleDrivingFlags)835636u, 20, BoatMissionFlags.StopAtShore);
            CurrentTask = VehicleTask.Pursue; // Redundant, task already set in UpdateTaskLogic
            _isGoToPlayerTaskActive = true;
        }
    }

    //this gets processed only if police aint know us
    public void TaskSearch(Vector3 target)
    {
        CurrentTask = VehicleTask.SearchGoToZone; // Ensure CurrentTask is set when called externally
        PerformSearchTask(); // Directly call PerformSearchTask to execute it immediately if needed
    }

    private void PerformSearchTask()
    {
        if (!_isSearchTaskActive)
        {
            float cruiseSpeed = 30f;
            float targetReachedDist = 20f;

            BoatMissionFlags missionFlags = BoatMissionFlags.DefaultSettings | BoatMissionFlags.PreferForward | BoatMissionFlags.NeverNavMesh | BoatMissionFlags.NeverPause;
            Vector3 searchTarget = Vector3.Zero; // Use the target passed to TaskSearch, or calculate if needed within PerformSearchTask
            if (Game.Player.AreWantedStarsGrayedOut) //Check for Vector3.Zero as well
                searchTarget = HelperClass.FindSearchPointForBoat(Game.Player.Character.Position, 250);

            TaskSequence taskSequence = new TaskSequence();
            taskSequence.AddTask.StartBoatMission(Watercraft, searchTarget, VehicleMissionType.GoTo, cruiseSpeed, (VehicleDrivingFlags)835636u, targetReachedDist, missionFlags);
            taskSequence.AddTask.CruiseWithVehicle(Watercraft, 0f, VehicleDrivingFlags.DrivingModeStopForVehicles);
            taskSequence.Close();
            Watercraft.IsSirenActive = true;
            Driver.BlockPermanentEvents = false;
            Driver.Task.PerformSequence(taskSequence);
            taskSequence.Dispose();
            CurrentTask = VehicleTask.SearchGoToZone; // Redundant, task already set in UpdateTaskLogic and TaskSearch function
            _isSearchTaskActive = true;
        }
    }

    public bool IsBoatValid()
    {
        return Watercraft != null && Watercraft.Exists() && !Watercraft.IsDead && Driver != null && Driver.Exists() && !Driver.IsDead;
    }

    public bool ApplyWeaponAmmo(VehicleWeaponHash weaponHash, int numammo)
    {
        Watercraft.SetWeaponRestrictedAmmo((int)weaponHash, numammo);
        return true;
    }
}