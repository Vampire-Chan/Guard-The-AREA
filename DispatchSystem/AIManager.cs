using GTA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    internal class AIManager : Script
    {
        public AIManager()
        {
            Tick += OnTick;
            AttackHelicopters = new List<AttackHelicopter>();
            DropOffHelicopters = new List<DropOffHelicopter>();
            Boats = new List<Boat>(); // Initialize Boat list
            GroundVehicles = new List<GroundVehicle>(); // Initialize GroundVehicle list
        }
        public static List<AttackHelicopter> AttackHelicopters;
        public static List<DropOffHelicopter> DropOffHelicopters;
        public static List<Boat> Boats;
        public static List<GroundVehicle> GroundVehicles;

        public static List<Ped> Cops;
        public static Ped PlayerPed = Game.Player.Character;
        public void OnTick(object sender, EventArgs e)
        {
            UpdateEverything();
        }

        public void UpdateEverything()
        {
            ManageAirUnits(); // Combined air unit management
            ManageGroundVehicles();
            ManageBoats();
        }


        public void ManageAirUnits()
        {
            DropOffHelicopters.RemoveAll(h => !h.IsHelicopterValid());
            AttackHelicopters.RemoveAll(h => !h.IsHelicopterValid());
            ManageAttackHelicopters();
            ManageDropOffHelicopters();
        }

        public void ManageAttackHelicopters()
        {
            // Iterate through AttackHelicopters list and call Update method using foreach
            foreach (AttackHelicopter attackHeli in AttackHelicopters)
            {
                attackHeli.UpdateProcess();
            }
        }


        public void ManageDropOffHelicopters()
        {
            // Iterate through DropOffHelicopter lists and call Update method using foreach
            foreach (var dropOffHeli in DropOffHelicopters)
            {
                dropOffHeli.UpdateProcess(); // Assuming DropOffHeli has an UpdateProcess() method
            }
        }

        public void ManageGroundVehicles()
        {
            // Iterate through GroundVehicles list and call UpdateProcess method using foreach
            foreach (var groundVehicle in GroundVehicles)
            {
                groundVehicle.UpdateProcess();
            }
        }

        public void ManageBoats()
        {
            // Iterate through Boats list and call UpdateProcess method using foreach
            foreach (var boat in Boats)
            {
                boat.UpdateProcess();
            }
        }
    }
