using GTA;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Guarding.DispatchSystem
{
    public class GroundVehicle
    {
        private GTA.Vehicle vehicle;
        private List<Ped> crew;
        private Dictionary<VehicleWeaponHash, bool> weaponStates;

        public bool Pursuit { get; set; }
        public bool Patrol { get; set; }

        public GroundVehicle(VehicleInformation info)
        {
            var vehicleModel = info.VehicleModels[new Random().Next(info.VehicleModels.Count)];
            var pilotModel = info.Pilot[new Random().Next(info.Pilot.Count)];
            var soldierModels = info.Soldiers.Soldiers;

            Initialize(vehicleModel, pilotModel, soldierModels);
        }

        public GroundVehicle(GTA.Vehicle existingVehicle)
        {
            if (existingVehicle == null || !existingVehicle.Exists())
                throw new ArgumentNullException(nameof(existingVehicle));

            vehicle = existingVehicle;
            crew = new List<Ped>();
            foreach (Ped occupant in vehicle.Occupants)
            {
                crew.Add(occupant);
            }
        }

        public void UpdateProcess()
        {
            if (!IsVehicleValid())
                return;

            HandlePursuit();
            HandlePatrol();
        }

        public bool IsVehicleValid()
        {
            return vehicle != null && vehicle.Exists() && !vehicle.IsDead && vehicle.Driver != null && vehicle.Driver.Exists() && !vehicle.Driver.IsDead;
        }

        private void Initialize(string vehicleModel, string pilotModel, List<string> crewModels)
        {
            vehicle = World.CreateVehicle(vehicleModel, Game.Player.Character.Position.Around(300));

            crew = new List<Ped>();

            var pilot = CreateAndAssignPed(pilotModel, VehicleSeat.Driver);
            crew.Add(pilot);

            for (int i = 0; i < vehicle.PassengerCapacity; i++)
            {
                var crewPed = CreateAndAssignPed(crewModels[new Random().Next(crewModels.Count)], (VehicleSeat)i);
                crew.Add(crewPed);
            }

            vehicle.IsEngineRunning = true;
        }

        private Ped CreateAndAssignPed(string pedModelName, VehicleSeat seat)
        {
            var pedModel = new Model(pedModelName);
            pedModel.Request(1000);

            if (!pedModel.IsValid || !pedModel.IsInCdImage)
                throw new ArgumentException($"Invalid ped model: {pedModelName}");

            var ped = vehicle.CreatePedOnSeat(seat, pedModel);
            ped.SetCombatAttribute(CombatAttributes.CanUseCover, true);

            return ped;
        }

        private void HandlePursuit()
        {
            if (Pursuit)
            {
                vehicle.Driver.Task.VehicleChase(Game.Player.Character);
            }
        }

        private void HandlePatrol()
        {
            if (Patrol)
            {
                Vector3 patrolPoint = Game.Player.Character.Position.Around(400);
                vehicle.Driver.Task.DriveTo(vehicle, patrolPoint, 10f, 20f, DrivingStyle.Normal);
            }
        }
    }
}