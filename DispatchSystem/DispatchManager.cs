using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Native;

    public class DispatchManager
    {
        private List<Helicopter> airUnits;
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

        public void UpdateDispatch()
        {
            UpdateAirUnits();
            UpdateGroundUnits();
            UpdateSeaUnits();
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 1, false);
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 2, false);
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 4, false);
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 12, false);
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

                int state = rand.Next(3);
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
