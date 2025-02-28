using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.UI;

public class DispatchManager
{
    private Dictionary<string, WantedStarBase> vehicleInfo;
    private Random rand;

    // Dispatch Timers
    private DateTime _lastAirDispatchTimeDropOff; // Timer for DropOff Helis
    private DateTime _lastAirDispatchTimeAttack;  // Timer for Attack Helis
    private DateTime _lastGroundDispatchTime;
    private DateTime _lastSeaDispatchTime;

    // Dispatch Intervals (in seconds) - Tunable as needed
    private readonly TimeSpan _airDispatchIntervalDropOff = TimeSpan.FromSeconds(30); // DropOff Helis every 30 seconds
    private readonly TimeSpan _airDispatchIntervalAttack = TimeSpan.FromSeconds(45);   // Attack Helis every 45 seconds
    private readonly TimeSpan _groundDispatchInterval = TimeSpan.FromSeconds(25);  // Ground vehicles every 25 seconds
    private readonly TimeSpan _seaDispatchInterval = TimeSpan.FromSeconds(25);     // Sea vehicles every 25 seconds


    public DispatchManager(Dictionary<string, WantedStarBase> info)
    {
        vehicleInfo = info;
        rand = new Random();
        _lastAirDispatchTimeDropOff = DateTime.MinValue; // Initialize timers
        _lastAirDispatchTimeAttack = DateTime.MinValue;
        _lastGroundDispatchTime = DateTime.MinValue;
        _lastSeaDispatchTime = DateTime.MinValue;
    }

    public void UpdateDispatch()
    {
        // Check if enough time has passed before dispatching each unit type
        if (DateTime.Now - _lastAirDispatchTimeDropOff >= _airDispatchIntervalDropOff || DateTime.Now - _lastAirDispatchTimeAttack >= _airDispatchIntervalAttack)
        {
            UpdateAirUnits();
        }
        if (DateTime.Now - _lastGroundDispatchTime >= _groundDispatchInterval)
        {
            UpdateGroundUnits();
        }
        if (DateTime.Now - _lastSeaDispatchTime >= _seaDispatchInterval)
        {
            UpdateSeaUnits();
        }
    }
    /*
     * these lists are defined in AIManager.cs so call by AIManager.AttackHelicopters, AIManager.DropOffHelicopters, AIManager.Boats, AIManager.GroundVehicles
     * public static List<AttackHelicopter> AttackHelicopters;
         public static List<DropOffHelicopter> DropOffHelicopters;
         public static List<Boat> Boats;
         public static List<GroundVehicle> GroundVehicles;
     */
    private void UpdateAirUnits()
    {
        int wantedLevel = Game.Player.WantedLevel;
        

        int maxHelis = wantedLevel switch
        {
            3 => 1,
            4 => 2,
            5 => 3,
            _ => 0
        };

        int dispatchedDropOffHelis = 0;
        int dispatchedAttackHelis = 0;
        int currentHelis = AIManager.DropOffHelicopters.Count + AIManager.AttackHelicopters.Count; // Total current air units

        while (currentHelis < maxHelis)
        {
            // Randomly choose between DropOffHelicopter (2/3 chance) and AttackHelicopter (1/3 chance) - adjust ratios as needed
            int randomChoice = rand.Next(3); // 0, 1 -> DropOff, 2 -> Attack
            bool dispatchDropOff = randomChoice < 2;

            if (dispatchDropOff && (DateTime.Now - _lastAirDispatchTimeDropOff >= _airDispatchIntervalDropOff)) // Dispatch DropOffHelicopter if timer allows
            {
                var vehicleInfos = GetVehicleInfos("Air", wantedLevel).Where(vInfo =>
                    vInfo.VehicleDetails != null &&
                    vInfo.VehicleDetails.VehicleTasks != null &&
                    (vInfo.VehicleDetails.VehicleTasks.Contains("RAPPEL") || vInfo.VehicleDetails.VehicleTasks.Contains("LAND"))
                ).ToList();

                if (vehicleInfos.Count > 0)
                {
                    var randomVehicleInfo = vehicleInfos[rand.Next(vehicleInfos.Count)];
                    HelperClass.FindSpawnPointForAircraft(AIManager.PlayerPed, ImportantChecks.LastKnownLocation, 190, 300, 80, out var SP, out var fSpawnHeading);
                    DropOffHelicopter.FindLocationForDeployment(ImportantChecks.LastKnownLocation.Around(10), out var pos);
                    var heli = new DropOffHelicopter(HelperClass.CreateVehicle(randomVehicleInfo, SP, fSpawnHeading), pos);

                    if (randomVehicleInfo.VehicleDetails.VehicleTasks.Contains("RAPPEL")) heli.Rappel = true;
                    if (randomVehicleInfo.VehicleDetails.VehicleTasks.Contains("LAND")) heli.Land = true;

                    AIManager.DropOffHelicopters.Add(heli);
                    _lastAirDispatchTimeDropOff = DateTime.Now; // Update DropOff Heli timer
                    dispatchedDropOffHelis++;
                }
            }
            else if (!dispatchDropOff && (DateTime.Now - _lastAirDispatchTimeAttack >= _airDispatchIntervalAttack)) // Dispatch AttackHelicopter if timer allows
            {
                var vehicleInfos = GetVehicleInfos("Air", wantedLevel).Where(vInfo =>
                    vInfo.VehicleDetails != null &&
                    vInfo.VehicleDetails.VehicleTasks != null &&
                    vInfo.VehicleDetails.VehicleTasks.Contains("ATTACK")).ToList();

                if (vehicleInfos.Count > 0)
                {
                    var randomVehicleInfo = vehicleInfos[rand.Next(vehicleInfos.Count)];
                    HelperClass.FindSpawnPointForAircraft(AIManager.PlayerPed, ImportantChecks.LastKnownLocation, 190, 300, 80, out var SP, out var fSpawnHeading);
                    var heli = new AttackHelicopter(HelperClass.CreateVehicle(randomVehicleInfo, SP, fSpawnHeading), ImportantChecks.LastKnownLocation);
                    AIManager.AttackHelicopters.Add(heli);
                    _lastAirDispatchTimeAttack = DateTime.Now; // Update Attack Heli timer
                    dispatchedAttackHelis++;
                }
            }
            currentHelis = AIManager.DropOffHelicopters.Count + AIManager.AttackHelicopters.Count; // Update count inside loop to reflect dispatches
            if (currentHelis >= maxHelis) break; // Ensure we don't exceed maxHelis even if loops could continue
        }

        if (dispatchedDropOffHelis > 0 || dispatchedAttackHelis > 0)
        {
            Notification.Show($"Dispatched {dispatchedDropOffHelis} DropOff Helicopters and {dispatchedAttackHelis} Attack Helicopters.");
        }

        // No need to remove invalid helis here, AIManager should handle list cleanup in its own update loop
    }


    private void UpdateGroundUnits()
    {
        if (!(DateTime.Now - _lastGroundDispatchTime >= _groundDispatchInterval)) return; // Timer Check

        int wantedLevel = Game.Player.WantedLevel;
        int groundVehiclesToDispatch = wantedLevel switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            4 => 5,
            5 => 7,
            _ => 0
        };

        int dispatchedVehicles = 0;
        while (AIManager.GroundVehicles.Count < groundVehiclesToDispatch)
        {
            var vehicleInfos = GetVehicleInfos("Ground", wantedLevel);
            if (vehicleInfos.Count == 0) break;

            var randomVehicleInfo = vehicleInfos[rand.Next(vehicleInfos.Count)];
            HelperClass.FindSpawnPointForAutomobile(AIManager.PlayerPed, ImportantChecks.LastKnownLocation, 190, 300, out var SP, out var fSpawnHeading);
            var groundVehicle = new GroundVehicle(HelperClass.CreateVehicle(randomVehicleInfo, SP, fSpawnHeading), ImportantChecks.LastKnownLocation);
            AIManager.GroundVehicles.Add(groundVehicle);
            dispatchedVehicles++;
        }

        if (dispatchedVehicles > 0)
        {
            Notification.Show($"Dispatched {dispatchedVehicles} Ground Vehicles.");
            _lastGroundDispatchTime = DateTime.Now; // Update Ground Vehicle timer after dispatch
        }


        // List cleanup is handled by AIManager, no need to remove here
    }

    private void UpdateSeaUnits()
    {
        if (!(DateTime.Now - _lastSeaDispatchTime >= _seaDispatchInterval)) return; // Timer Check

        int wantedLevel = Game.Player.WantedLevel;
        int seaVehiclesToDispatch = wantedLevel switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            4 => 5,
            5 => 7,
            _ => 0
        };

        int dispatchedBoats = 0;
        while (AIManager.Boats.Count < seaVehiclesToDispatch)
        {
            var vehicleInfos = GetVehicleInfos("Sea", wantedLevel);
            if (vehicleInfos.Count == 0) break;

            var randomVehicleInfo = vehicleInfos[rand.Next(vehicleInfos.Count)];
            HelperClass.FindSpawnPointForBoat(AIManager.PlayerPed, ImportantChecks.LastKnownLocation, 190, 300, out var SP, out var fSpawnHeading);
            var seaVehicle = new Boat(HelperClass.CreateVehicle(randomVehicleInfo, SP, fSpawnHeading), ImportantChecks.LastKnownLocation);
            AIManager.Boats.Add(seaVehicle);
            dispatchedBoats++;
        }

        if (dispatchedBoats > 0)
        {
            Notification.Show($"Dispatched {dispatchedBoats} Sea Units.");
            _lastSeaDispatchTime = DateTime.Now; // Update Sea Unit timer after dispatch
        }



        // List cleanup is handled by AIManager, no need to remove here
    }

    private List<VehicleInformation> GetVehicleInfos(string type, int wantedLevel)
    {
        string levelKey = wantedLevel switch
        {
            1 => "One",
            2 => "Two",
            3 => "Three",
            4 => "Four",
            5 => "Five",
            _ => "One"
        };

        if (vehicleInfo == null || !vehicleInfo.ContainsKey(levelKey))
        {
            return new List<VehicleInformation>();
        }

        var wantedStar = vehicleInfo[levelKey];
        if (wantedStar == null)
        {
            return new List<VehicleInformation>();
        }


        switch (type)
        {
            case "Air":
                return wantedStar.AirList?.SelectMany(d => d.VehicleInfo).ToList() ?? new List<VehicleInformation>();
            case "Ground":
                return wantedStar.GroundList?.SelectMany(d => d.VehicleInfo).ToList() ?? new List<VehicleInformation>();
            case "Sea":
                return wantedStar.SeaList?.SelectMany(d => d.VehicleInfo).ToList() ?? new List<VehicleInformation>();
            default:
                return new List<VehicleInformation>();
        }
    }

    // Public properties to access the lists (if needed for external monitoring) - Optional
    public List<AttackHelicopter> AttackHelis => AIManager.AttackHelicopters;
    public List<DropOffHelicopter> DropOffHelicopters => AIManager.DropOffHelicopters;
    public List<Boat> seaUnits => AIManager.Boats;
    public List<GroundVehicle> groundUnits => AIManager.GroundVehicles;
}