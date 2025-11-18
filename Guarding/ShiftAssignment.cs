using GTA;
using GTA.Math;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Holds the complete shift assignment data for departures and arrivals.
/// Tracks which vehicle spawn points were used and which ped spawn points were filled.
/// This allows arriving guards to fill the exact positions that departing guards left.
/// </summary>
public class ShiftAssignment
{
    /// <summary>
    /// The area this assignment belongs to
    /// </summary>
    public string AreaName { get; set; }

    /// <summary>
    /// Ped spawn points that were occupied by departing guards
    /// These will be filled by arriving guards
    /// </summary>
    public List<GuardSpawnPoint> VacatedPedSpawnPoints { get; set; }

    /// <summary>
    /// Vehicle spawn points that were used during departure
    /// These same points will be used for arrival
    /// </summary>
    public List<GuardSpawnPoint> UsedVehicleSpawnPoints { get; set; }

    /// <summary>
    /// Guards that departed (for reference/debugging)
    /// </summary>
    public List<string> DepartedGuardNames { get; set; }

    /// <summary>
    /// Timestamp when departure occurred
    /// </summary>
    public System.DateTime DepartureTime { get; set; }

    /// <summary>
    /// Number of guards that departed
    /// </summary>
    public int DepartedGuardCount => VacatedPedSpawnPoints?.Count ?? 0;

    /// <summary>
    /// Number of vehicles used during departure
    /// </summary>
    public int UsedVehicleCount => UsedVehicleSpawnPoints?.Count ?? 0;

    public ShiftAssignment()
    {
        VacatedPedSpawnPoints = new List<GuardSpawnPoint>();
        UsedVehicleSpawnPoints = new List<GuardSpawnPoint>();
        DepartedGuardNames = new List<string>();
        DepartureTime = System.DateTime.Now;
    }

    public ShiftAssignment(string areaName) : this()
    {
        AreaName = areaName;
    }
}

/// <summary>
/// Result of vehicle assignment calculation
/// Contains guards assigned to vehicles and guards leaving on foot
/// </summary>
public class VehicleAssignmentResult
{
    /// <summary>
    /// Guards assigned to vehicles with their seats
    /// Key: Vehicle, Value: List of (Guard, Seat) tuples
    /// </summary>
    public Dictionary<GuardVehicle, List<(GuardPed guard, VehicleSeat seat)>> VehicleAssignments { get; set; }

    /// <summary>
    /// Guards leaving on foot (no vehicle available)
    /// </summary>
    public List<GuardPed> OnFootGuards { get; set; }

    /// <summary>
    /// Total number of guards assigned to vehicles
    /// </summary>
    public int GuardsInVehicles => VehicleAssignments?.Sum(kvp => kvp.Value.Count) ?? 0;

    /// <summary>
    /// Total number of guards leaving on foot
    /// </summary>
    public int GuardsOnFoot => OnFootGuards?.Count ?? 0;

    /// <summary>
    /// Total guards in this assignment
    /// </summary>
    public int TotalGuards => GuardsInVehicles + GuardsOnFoot;

    public VehicleAssignmentResult()
    {
        VehicleAssignments = new Dictionary<GuardVehicle, List<(GuardPed, VehicleSeat)>>();
        OnFootGuards = new List<GuardPed>();
    }
}
