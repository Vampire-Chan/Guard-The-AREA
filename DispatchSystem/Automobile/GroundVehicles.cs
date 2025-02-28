using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class GroundVehicle
{
    public Ped Driver { get; set; }
    public Vehicle Vehicle { get; set; }
    private Dictionary<VehicleWeaponHash, bool> VehicleWeapon { get; set; }

    public VehicleTask CurrentTask { get; private set; }
    private bool _isPursuitTaskActive = false;
    private bool _isSearchTaskActive = false; // Renamed from _isPatrolTaskActive

    public enum VehicleTask
    {
        SearchGoToZone, // Renamed from Patrol
        Pursue,
        Attack = 1,
        Flee,
        None
    }

    public GroundVehicle(Vehicle vehi, Vector3 pos)
    {
        Vehicle = vehi;
        Driver = Vehicle.Driver;
        if (Driver == null || !Driver.Exists())
        {
            Debug.WriteLine("Warning: GroundVehicle Driver is null or invalid in constructor!");
            return;
        }
        PerformSearchTask(); // Initial task is Search
    }

    public void UpdateProcess()
    {
        if (!IsVehicleValid())
            return;

        UpdateTaskLogic();
        PerformCurrentTask();
    }

    private void UpdateTaskLogic()
    {
        if (Game.Player.WantedLevel > 0 && !Game.Player.AreWantedStarsGrayedOut) // Basic Pursuit condition
        {
            if (CurrentTask != VehicleTask.Pursue)
            {
                CurrentTask = VehicleTask.Pursue;
                _isSearchTaskActive = false; // Reset Search flag when switching to Pursue //Renamed from _isPatrolTaskActive
                _isPursuitTaskActive = false;
            }
        }
        else if (Game.Player.AreWantedStarsGrayedOut) // When wanted stars are grayed out, go to search
        {
            if (CurrentTask != VehicleTask.SearchGoToZone) // Renamed from VehicleTask.Patrol
            {
                CurrentTask = VehicleTask.SearchGoToZone; // Renamed from VehicleTask.Patrol
                _isSearchTaskActive = false; // Allow Search task to run again //Renamed from _isPatrolTaskActive
                _isPursuitTaskActive = false;
            }
        }
        else // No wanted level and not grayed out, potentially go to search as well or do nothing (None)
        {
            if (CurrentTask != VehicleTask.SearchGoToZone) // Default to Search if no pursuit and not grayed out
            {
                CurrentTask = VehicleTask.SearchGoToZone; // Default to Search
                _isSearchTaskActive = false; // Allow Search task to run again //Renamed from _isPatrolTaskActive
                _isPursuitTaskActive = false;
            }
        }
        /*else // if you want to stop vehicle when no wanted stars and not grayed out, uncomment this and comment above else if
       {
           if (CurrentTask != VehicleTask.None)
           {
               CurrentTask = VehicleTask.None;
               _isSearchTaskActive = false;
               _isPursuitTaskActive = false;
           }
       }*/
    }

    private void PerformCurrentTask()
    {
        switch (CurrentTask)
        {
            case VehicleTask.Pursue:
                PerformPursueTask();
                break;
            case VehicleTask.SearchGoToZone: // Renamed from VehicleTask.Patrol
                PerformSearchTask(); // Renamed from PerformPatrolTask
                break;
            case VehicleTask.None:
            default:
                break;
        }
    }


    private void PerformPursueTask()
    {
        if (!_isPursuitTaskActive)
        {
            Driver.Task.StartVehicleMission(Vehicle, Game.Player.Character.Position, VehicleMissionType.GoTo, 80, VehicleDrivingFlags.DrivingModeAvoidVehiclesReckless, 30, 10, true);
            CurrentTask = VehicleTask.Pursue;
            _isPursuitTaskActive = true;
        }
    }

    private void PerformSearchTask() // Renamed from PerformPatrolTask
    {
        if (!_isSearchTaskActive) // Renamed from !_isPatrolTaskActive
        {
            Vector3 searchPosition = HelperClass.FindSearchPointForAutomobile(Game.Player.Character.Position, 250);

            TaskSequence taskSequence = new TaskSequence();
            taskSequence.AddTask.StartVehicleMission(Vehicle, searchPosition, VehicleMissionType.GoTo, 30f, VehicleDrivingFlags.SteerAroundObjects, 20f, 10f, true);
            taskSequence.AddTask.CruiseWithVehicle(Vehicle, 0f, VehicleDrivingFlags.DrivingModeStopForVehicles);
            taskSequence.Close();

            Driver.Task.PerformSequence(taskSequence);
            taskSequence.Dispose();

            CurrentTask = VehicleTask.SearchGoToZone; // Renamed from VehicleTask.Patrol
            _isSearchTaskActive = true; // Renamed from _isPatrolTaskActive
        }
    }


    public bool IsVehicleValid()
    {
        return Vehicle != null && Vehicle.Exists() && !Vehicle.IsDead && Driver != null && Driver.Exists() && !Driver.IsDead;
    }

    public bool ApplyWeaponAmmo(VehicleWeaponHash weaponHash, int numammo)
    {
        Vehicle.SetWeaponRestrictedAmmo((int)weaponHash, numammo);
        return true;
    }
}