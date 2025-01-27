using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Threading.Tasks;
using GTA;
using GTA.Native;
using GTA.UI;

public class DispatchManager
    {
        private List<Helicopter> airUnits;
    //private List<Vehicle> airaUnits;
    private List<GroundVehicle> groundUnits;
        private List<Boat> seaUnits;
        private Dictionary<string, WantedStarBase> vehicleInfo;
        private Random rand;

        public DispatchManager(Dictionary<string, WantedStarBase> info)
        {
            vehicleInfo = info;
            airUnits = new List<Helicopter>();
            groundUnits = new List<GroundVehicle>();
            seaUnits = new List<Boat>();
            rand = new Random();
        }

    void LoadTasksOfHeli()
    {
        var vehicles = World.GetAllVehicles();
        foreach (var vehicle in vehicles)
        {
            // Check if the vehicle is a valid helicopter (aircraft and specific model)
            if (vehicle.IsAircraft && !vehicle.IsDead)
            {
                try
                {
                    // Get the active mission type of the vehicle
                    var missionType = vehicle.GetActiveMissionType().ToString();

                    // Initialize crew members
                    Ped driver = null, rightRear = null, leftRear = null;

                    // Loop through the occupants of the helicopter
                    foreach (var crew in vehicle.Occupants)
                    {
                        if (crew.Exists())
                        {
                            var taskStatus = crew.TaskSequenceProgress.ToString();
                            var seatName = Enum.GetName(typeof(VehicleSeat), crew.SeatIndex);

                            // Assign occupants to specific seats
                            switch (crew.SeatIndex)
                            {
                                case VehicleSeat.Driver:
                                    driver = crew;
                                    break;
                                case VehicleSeat.RightRear:
                                    rightRear = crew;
                                    break;
                                case VehicleSeat.LeftRear:
                                    leftRear = crew;
                                    break;
                            }

                            // Display individual crew member task statuses
                            //Screen.ShowSubtitle($"Seat: {seatName}, TaskStatus: {taskStatus}");
                        }
                    }

                    // Log the overall mission type and crew details
                    Screen.ShowSubtitle(
                        $"HeliTask: {missionType}, " +
                        $"Driver: {driver?.CurrentScriptTaskStatus.ToString()}, " +
                        $"RightRear: {rightRear?.CurrentScriptTaskStatus.ToString()}, " +
                        $"LeftRear: {leftRear?.CurrentScriptTaskStatus.ToString()}" + 
                        $"Script: "
                    );
                }
                catch (Exception ex)
                {
                    // Log any errors encountered
                    Screen.ShowSubtitle($"Vehicle task error: {ex.Message}");
                }
            }
        }
    }



    public void UpdateDispatch()
        {
      //  LoadTasksOfHeli();
        UpdateAirUnits();
        // UpdateGroundUnits();
        //UpdateSeaUnits();
        //Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 1, false);
       //     Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 2, false);
        //    Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 4, false);
        //    Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 12, false);
        }

        private void UpdateAirUnits()
        {
            int wantedLevel = Game.Player.WantedLevel;
            int helicoptersToDispatch = wantedLevel switch
            {
                3 => 1,
                4 => 2,
                >= 5 => 4,
                _ => 0
            };

            while (airUnits.Count < helicoptersToDispatch && helicoptersToDispatch!=0)
            {
                var vehicleInfos = GetVehicleInfos("Air", wantedLevel);

                if (vehicleInfos.Count == 0) break;

                var randomVehicleInfo = vehicleInfos[rand.Next(vehicleInfos.Count)];

            var heli = new Helicopter(randomVehicleInfo);
            airUnits.Add(heli);

            int state = rand.Next(5);
            switch (state)
            {
                case 0:
                    heli.Rappel = true;
                    break;
                case 1:
                    heli.Land = true;
                    break;
                case 2:
                    heli.Pursuit = true;
                    break;
            }
        }

            foreach (var heli in airUnits)
            {
                heli.UpdateProcess();
            }

            airUnits.RemoveAll(h => !h.IsHelicopterValid());
        }

        private void UpdateGroundUnits()
        {
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

            while (groundUnits.Count < groundVehiclesToDispatch)
            {
                var vehicleInfos = GetVehicleInfos("Ground", wantedLevel);
                if (vehicleInfos.Count == 0) break;

                var randomVehicleInfo = vehicleInfos[rand.Next(vehicleInfos.Count)];
                var groundVehicle = new GroundVehicle(randomVehicleInfo);
                groundUnits.Add(groundVehicle);
            }

            foreach (var vehicle in groundUnits)
            {
                vehicle.UpdateProcess();
            }

            groundUnits.RemoveAll(v => !v.IsVehicleValid());
        }

        private void UpdateSeaUnits()
        {
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

            while (seaUnits.Count < seaVehiclesToDispatch)
            {
                var vehicleInfos = GetVehicleInfos("Sea", wantedLevel);
                if (vehicleInfos.Count == 0) break;

                var randomVehicleInfo = vehicleInfos[rand.Next(vehicleInfos.Count)];
                var seaVehicle = new Boat(randomVehicleInfo);
                seaUnits.Add(seaVehicle);
            }

            foreach (var vehicle in seaUnits)
            {
                vehicle.UpdateProcess();
            }

            seaUnits.RemoveAll(v => !v.IsBoatValid());
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

            var wantedStar = vehicleInfo[levelKey];
            return type switch
            {
                "Air" => wantedStar.AirList.SelectMany(d => d.VehicleInfo).ToList(),
                "Ground" => wantedStar.GroundList.SelectMany(d => d.VehicleInfo).ToList(),
                "Sea" => wantedStar.SeaList.SelectMany(d => d.VehicleInfo).ToList(),
                _ => new List<VehicleInformation>()
            };
        }
    }

public abstract class WantedStarBase
{
    public List<DispatchInfoAir> AirList { get; set; } = new();
    public List<DispatchInfoGround> GroundList { get; set; } = new();
    public List<DispatchInfoSea> SeaList { get; set; } = new();
}

public class WantedStarOne : WantedStarBase { }
public class WantedStarTwo : WantedStarBase { }
public class WantedStarThree : WantedStarBase { }
public class WantedStarFour : WantedStarBase { }
public class WantedStarFive : WantedStarBase { }
