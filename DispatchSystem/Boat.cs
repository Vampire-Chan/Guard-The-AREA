using GTA;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.Linq;


    public class Boat
    {
        private GTA.Vehicle boat;
        private List<Ped> crew;
        private Dictionary<VehicleWeaponHash, bool> weaponStates;

        public bool Pursuit { get; set; }
        public bool Patrol { get; set; }

        public Boat(VehicleInformation info)
        {
            var boatModel = info.VehicleModels[new Random().Next(info.VehicleModels.Count)];
            var pilotModel = info.Pilot[new Random().Next(info.Pilot.Count)];
            var soldierModels = info.Soldiers.Soldiers;

            Initialize(boatModel, pilotModel, soldierModels);
        }

        public Boat(GTA.Vehicle existingBoat)
        {
            if (existingBoat == null || !existingBoat.Exists())
                throw new ArgumentNullException(nameof(existingBoat));

            boat = existingBoat;
            crew = new List<Ped>();
            foreach (Ped occupant in boat.Occupants)
            {
                crew.Add(occupant);
            }
        }

        public void UpdateProcess()
        {
            if (!IsBoatValid())
                return;

            HandlePursuit();
            HandlePatrol();
        }

        public bool IsBoatValid()
        {
            return boat != null && boat.Exists() && !boat.IsDead && boat.Driver != null && boat.Driver.Exists() && !boat.Driver.IsDead;
        }

        private void Initialize(string boatModel, string pilotModel, List<string> crewModels)
        {
            boat = World.CreateVehicle(boatModel, Game.Player.Character.Position.Around(300));

            crew = new List<Ped>();

            var pilot = CreateAndAssignPed(pilotModel, VehicleSeat.Driver);
            crew.Add(pilot);

            for (int i = 0; i < boat.PassengerCapacity; i++)
            {
                var crewPed = CreateAndAssignPed(crewModels[new Random().Next(crewModels.Count)], (VehicleSeat)i);
                crew.Add(crewPed);
            }

            boat.IsEngineRunning = true;
        }

        private Ped CreateAndAssignPed(string pedModelName, VehicleSeat seat)
        {
            var pedModel = new Model(pedModelName);
            pedModel.Request(1000);

            if (!pedModel.IsValid || !pedModel.IsInCdImage)
                throw new ArgumentException($"Invalid ped model: {pedModelName}");

            var ped = boat.CreatePedOnSeat(seat, pedModel);
           // ped.SetCombatAttributes(CombatAttributes.CanUseCover, true);

            return ped;
        }

        private void HandlePursuit()
        {
            if (Pursuit)
            {
                boat.Driver.Task.VehicleChase(Game.Player.Character);
            }
        }

        private void HandlePatrol()
        {
            if (Patrol)
            {
                Vector3 patrolPoint = Game.Player.Character.Position.Around(400);
                boat.Driver.Task.DriveTo(boat, patrolPoint, 10f, VehicleDrivingFlags.PreferNavmeshRoute, 20f);
            }
        }
    }
