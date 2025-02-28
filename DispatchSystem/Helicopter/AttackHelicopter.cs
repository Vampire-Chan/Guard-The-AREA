using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
public class AttackHelicopter
{
    // --- Fields ---
    public Ped Pilot { get; private set; }
    public Vehicle Vehicle { get; private set; }
    private Vector3 GoToPosition;
    private DateTime _lastPassengerCheckTime;
    private bool _hasAnyPassengersCache;
    private const double PassengerCheckCacheIntervalSeconds = 1.0; // 1 second cache interval
    private float _originalHealth;
    public VehicleTask CurrentTask { get; private set; }
    public float SearchRadiusGoToZone { get; set; } = 200f;
    public int TaskSequenceProgress { get; set; }

    private bool _isSearchGoToZoneTaskActive = false; // Flag for SearchGoToZone task
    private bool _isPursueTaskActive = false;       // Flag for Pursue task
    private bool _isFleeTaskActive = false;          // Flag for Flee task

    public enum TaskHeli
    {
        Attack,
        Rappel,
        Land
    }

    public enum VehicleTask
    {
        SearchGoToZone,
        Pursue,
        Attack = 1,
        Flee,
        None
    }

    public AttackHelicopter(Vehicle vehicle, Vector3 pos)
    {
        Vehicle = vehicle;
        Pilot = Vehicle.Driver;
        GoToPosition = pos;
        _originalHealth = Vehicle.HealthFloat; // Initialize original health here
        ManageWeaponsAvailability();
        PerformSearchGoToZoneTask(); // Initial task is SearchGoToZone
        if (CanAttack) vehicle.SetArriveDistanceOverrideForVehiclePersuitAttack(70f);
    }

    // --- Weapon Management ---
    void RemoveAbility()
    {
        Pilot.SetCombatAttribute(CombatAttributes.UseVehicleAttack, false);
        Pilot.SetCombatAttribute(CombatAttributes.ForceCheckAttackAngleForMountedGuns, false);
    }

    void GiveAbility()
    {
        Pilot.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true);
        Pilot.SetCombatAttribute(CombatAttributes.ForceCheckAttackAngleForMountedGuns, true);
    }

    public void SetupWeaponForHelicopter(VehicleWeaponHash weaponHash)
    {
        if (Pilot.GetVehicleWeaponHash(out VehicleWeaponHash currentWeaponHash))
        {
            if (currentWeaponHash == weaponHash) return;
        }
        Pilot.SetVehicleWeaponHash(weaponHash);
    }

    public bool ApplyWeaponAmmo(VehicleWeaponHash weaponHash, int numammo)
    {
        Vehicle.SetWeaponRestrictedAmmo((int)weaponHash, numammo);
        return true;
    }

    // --- Update Logic ---
    public void UpdateProcess()
    {
        if (!IsHelicopterValid()) return;

        UpdateTaskLogic();
        PerformCurrentTask();
        CheckFleeCondition(); // Still check flee condition every frame, as it's a reactive state
    }

    public bool IsHelicopterValid()
    {
        return Vehicle != null && Vehicle.Exists() && !Vehicle.IsDead && Pilot != null && Pilot.Exists() && !Pilot.IsDead;
    }
    bool CanAttack;
    private void ManageWeaponsAvailability()
    {
        CanAttack = Vehicle.Driver.GetVehicleWeaponHash(out var hash);
    }
    bool FoundPlayer = true; //Initialize to true, assume player is initially found
    private void UpdateTaskLogic()
    {
        if (Vehicle.HealthFloat < (_originalHealth * 0.5f) || !HasAnyPassengers && CurrentTask != VehicleTask.Flee) // Keep Flee task until explicitly changed
        {
            FoundPlayer = false;
            if (CurrentTask != VehicleTask.Flee) // Transition to Flee only if not already fleeing
            {
                CurrentTask = VehicleTask.Flee;
                _isSearchGoToZoneTaskActive = false; // Reset other task flags on task switch
                _isPursueTaskActive = false;
                _isFleeTaskActive = false; // Reset flag so Flee task can be performed again if needed later
            }

        }
        else if (Game.Player.AreWantedStarsGrayedOut)
        {
            FoundPlayer = false;
            if (CurrentTask != VehicleTask.SearchGoToZone) //Transition to SearchGoToZone if not already searching
            {
                CurrentTask = VehicleTask.SearchGoToZone;
                _isSearchGoToZoneTaskActive = false; // Reset flag so SearchGoToZone task can be performed again
                _isPursueTaskActive = false;
                _isFleeTaskActive = false;
            }
        }
        else if (FoundPlayer)
        {
            if (CurrentTask != VehicleTask.Pursue) // Transition to Pursue only if not already pursuing
            {
                CurrentTask = VehicleTask.Pursue;
                _isSearchGoToZoneTaskActive = false;
                _isPursueTaskActive = false; // Reset flag so Pursue task can be performed again
                _isFleeTaskActive = false;
            }
        }
        else if (!FoundPlayer)
        {
            if (CurrentTask != VehicleTask.SearchGoToZone) //Transition to SearchGoToZone if not already searching
            {
                CurrentTask = VehicleTask.SearchGoToZone;
                _isSearchGoToZoneTaskActive = false; // Reset flag so SearchGoToZone task can be performed again
                _isPursueTaskActive = false;
                _isFleeTaskActive = false;
            }
        }
    }

    private void PerformCurrentTask()
    {
        switch (CurrentTask)
        {
            case VehicleTask.SearchGoToZone:
                PerformSearchGoToZoneTask();
                break;
            case VehicleTask.Pursue:
                PerformPursuePlayerTask();
                break;
            case VehicleTask.Flee:
                PerformFleeTask();
                break;
            case VehicleTask.None:
            default:
                break;
        }
    }

    private void PerformSearchGoToZoneTask()
    {
        if (!_isSearchGoToZoneTaskActive) // Check if task is already active
        {
            Vector3 searchTarget = HelperClass.FindSearchPointForHelicopter(GoToPosition, SearchRadiusGoToZone, 40f, _isSearchGoToZoneTaskActive); //Pass flag to helper
            TaskSequence taskSequence = new TaskSequence();
            taskSequence.AddTask.StartHeliMission(Vehicle, searchTarget, VehicleMissionType.GoTo, 15f, -1f, -1, 40);
            taskSequence.AddTask.StartHeliMission(Vehicle, searchTarget, VehicleMissionType.GoTo, 0f, 0f, -1, 40);
            taskSequence.Close();
            Vehicle.IsSirenActive = true;
            Pilot.BlockPermanentEvents = false;
            Pilot.Task.PerformSequence(taskSequence);
            taskSequence.Dispose();
            CurrentTask = VehicleTask.SearchGoToZone; // Redundant, CurrentTask is already set by UpdateTaskLogic
            _isSearchGoToZoneTaskActive = true; // Set flag to indicate task is now active
        }
    }

    private void PerformPursuePlayerTask()
    {
        if (!_isPursueTaskActive) // Check if pursue task is already active
        {
            Vehicle.ClearPrimaryTask();
            Pilot.Task.ClearAll();
            Pilot.Task.StartHeliMission(Vehicle, Game.Player.Character.Position, VehicleMissionType.GoTo, 80f, 0f, -1, 80); // Go to Player's position initially
            CurrentTask = VehicleTask.Pursue; // Redundant
            _isPursueTaskActive = true; // Set pursue task active flag
        }
    }

    // --- Flee Logic ---
    private void CheckFleeCondition()
    {
        if (Vehicle.HealthFloat < (_originalHealth * 0.5f) || !HasAnyPassengers || Game.Player.WantedLevel == 0 && CurrentTask != VehicleTask.Flee) //Added CurrentTask!=VehicleTask.Flee to prevent reset flee
        {
            if (CurrentTask != VehicleTask.Flee)
                CurrentTask = VehicleTask.Flee; // Ensure task is set to flee if condition met
        }
    }


    protected virtual void PerformFleeTask(bool forceFlee = true)
    {
        if (!_isFleeTaskActive) // Check if flee task is already active
        {
            Vehicle.IsSirenActive = false;
            Pilot.BlockPermanentEvents = forceFlee;
            Vehicle.ClearPrimaryTask();
            Pilot.Task.ClearAll();
            Pilot.Task.StartHeliMission(Vehicle, Game.Player.Character, VehicleMissionType.Flee, 90, 0f, -1, 100);
            CurrentTask = VehicleTask.Flee; // Redundant
            _isFleeTaskActive = true; // Set flee task active flag
        }
    }

    // --- Passenger Check Logic ---
    private bool HasAnyPassengers
    {
        get
        {
            if ((DateTime.Now - _lastPassengerCheckTime).TotalSeconds < PassengerCheckCacheIntervalSeconds)
            {
                return _hasAnyPassengersCache;
            }
            _hasAnyPassengersCache = false;
            for (int i = 0; i < Vehicle.PassengerCapacity; i++)
            {
                VehicleSeat seat = (VehicleSeat)i;
                if (seat == VehicleSeat.Driver || seat == VehicleSeat.RightFront) continue;
                if (!Vehicle.IsSeatFree(seat))
                {
                    _hasAnyPassengersCache = true;
                    break;
                }
            }
            _lastPassengerCheckTime = DateTime.Now;
            return _hasAnyPassengersCache;
        }
    }
}