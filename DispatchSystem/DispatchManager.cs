using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace Guarding.DispatchSystem
{
    public class DispatchManager
    {
        private List<Helicopter> airUnits;
        private List<GroundVehicle> groundUnits;
        private List<Boat> seaUnits;
        private VehicleInformation vehicleInfo;
        private Random rand;

        public DispatchManager(VehicleInformation info)
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
            //UpdateGroundUnits();
            //UpdateSeaUnits();
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 1, false);
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 2, false);
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 4, false);
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 12, false); ;
        }

        private void UpdateAirUnits()
        {
            int wantedLevel = Game.Player.WantedLevel;
            int helicoptersToDispatch = 0;

            if (wantedLevel == 3)
            {
                helicoptersToDispatch = 1;
            }
            else if (wantedLevel == 4)
            {
                helicoptersToDispatch = 2;
            }
            else if (wantedLevel >= 5)
            {
                helicoptersToDispatch = 4;
            }

            if (airUnits.Count < helicoptersToDispatch)
            {
                var heli = new Helicopter(vehicleInfo);
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
            int groundVehiclesToDispatch = 0;

            if (wantedLevel == 1)
            {
                groundVehiclesToDispatch = 1;
            }
            else if (wantedLevel == 2)
            {
                groundVehiclesToDispatch = 2;
            }
            else if (wantedLevel == 3)
            {
                groundVehiclesToDispatch = 3;
            }
            else if (wantedLevel == 4)
            {
                groundVehiclesToDispatch = 5;
            }
            else if (wantedLevel == 5)
            {
                groundVehiclesToDispatch = 7;
            }

            while (groundUnits.Count < groundVehiclesToDispatch)
            {
                var groundVehicle = new GroundVehicle(vehicleInfo);
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
            int seaVehiclesToDispatch = 0;

            if (wantedLevel == 1)
            {
                seaVehiclesToDispatch = 1;
            }
            else if (wantedLevel == 2)
            {
                seaVehiclesToDispatch = 2;
            }
            else if (wantedLevel == 3)
            {
                seaVehiclesToDispatch = 3;
            }
            else if (wantedLevel == 4)
            {
                seaVehiclesToDispatch = 5;
            }
            else if (wantedLevel == 5)
            {
                seaVehiclesToDispatch = 7;
            }

            while (seaUnits.Count < seaVehiclesToDispatch)
            {
                var seaVehicle = new Boat(vehicleInfo);
                seaUnits.Add(seaVehicle);
            }

            foreach (var vehicle in seaUnits)
            {
                vehicle.UpdateProcess();
            }

            seaUnits.RemoveAll(v => !v.IsBoatValid());
        }
    }
}