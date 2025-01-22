using GTA;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guarding.DispatchSystem
{
    public class DispatchSystemManager : Script
    {
        public DispatchSystemManager() 
        {
            Tick += OnTick;

        }

        int WantedLevel;
        int Speed;

        //Vehicle Healths
        public struct Health
        {
            int EngineHealth;
            int BodyHealth;
            int OverallHealth;
        }

        private List<Helicopter> helicopters = new();

        private void OnTick(object sender, EventArgs e) 
        {
            foreach (Helicopter h in helicopters) { h.UpdateProcess(); }
            CheckWantedLevels(); //check the wanted levels
            CheckDynamicSpeeds(); //vehicle speed of every vehicle in the Spawned Lists 
            CheckHealth(); //vehicle health checks of every vehicle in the spawn lists
            CheckVehicleUpdateStates(); //this manages the accuracy/shoot rate/and firing patters and well as the ped should jump of heli or land or flee based on helicopter health
            DispatchLawEnforcements(); //Manager to Dispatch Vehicles based on how many are already spawned. Air Units are at max 3 at only wanted level 5. where it has 5 different tasks which are assigned randomly- Land, Rappel, Paratroop, Pursuit and Attack (with weapons)
            //Ground and Sea arent done yet. so only the helicopter.
        }

        void CheckDynamicSpeeds() { }
        void CheckHealth() { }
        void CheckVehicleUpdateStates()
        {
            foreach (var heli in helicopters.ToList()) // Use ToList() to avoid modifying the list while iterating
            {
                try
                {
                    // Check if the helicopter is null or invalid
                    if (heli == null ||
                        heli.Vehicle == null || heli.Vehicle.IsDead ||
                        heli.Pilot == null || heli.Pilot.IsDead || !heli.Pilot.Exists() ||
                        heli.Crew == null || heli.Crew.Any(c => c == null || c.IsDead || !c.Exists()))
                    {
                        // Safely mark the vehicle and crew members as no longer needed
                        heli?.Vehicle?.MarkAsNoLongerNeeded();  // Check if heli and vehicle are not null
                        heli?.Pilot?.MarkAsNoLongerNeeded();    // Check if heli and pilot are not null
                        heli?.Crew?.ForEach(c => c?.MarkAsNoLongerNeeded()); // Safely mark all soldiers as no longer needed

                        // Remove the helicopter from the list
                        helicopters.Remove(heli);
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle the exception to avoid breaking the loop
                    //use GTA.UI.Notification.PostTicker(msg, false); instead
                    
                }
            }

        }

        private static Player Player = Game.Player;
        private static Ped PlayerPed = Player.Character;
        private Vector3 PlayerPosition = PlayerPed.Position;
        private float PlayerHeading = PlayerPed.Heading;

        void CheckWantedLevels()
        {
            WantedLevel = Player.WantedLevel;
        }

        bool isPopulated = false;
        VehicleInformation info;

        void PopulateData()
        {
            if (!isPopulated)
            {
                isPopulated = true;
                info = new VehicleInformation(
                    new List<string> { "polmav", "annihilator", "annihilator2" }, // Helicopter model
                    new List<string> { "s_m_y_cop_01", "s_m_y_hwaycop_01" }, // Pilot model
                    new SoldierInformation(
                        new List<string> { "s_m_y_swat_01", "s_m_y_cop_01" }, // Soldier models
                        new List<WeaponInformation>
                        {
                new WeaponInformation(
                    new List<string> { "WEAPON_CARBINERIFLE, WEAPON_SMG" }, // Primary weapons
                    new List<string> { "WEAPON_PISTOL, WEAPON_HEAVYPISTOL" } // Secondary weapons
                )
                        }
                    )
                );
            }
        }

        void DispatchLawEnforcements()
        {
            PopulateData(); // Populate vehicle and crew information

            // Dispatch 1 helicopter at WantedLevel 3, 2 helicopters at WantedLevel 4, and 3 helicopters at WantedLevel 5
            int helicoptersToDispatch = 0;

            if (WantedLevel == 3)
            {
                helicoptersToDispatch = 1;
            }
            else if (WantedLevel == 4)
            {
                helicoptersToDispatch = 2;
            }
            else if (WantedLevel == 5)
            {
                helicoptersToDispatch = 3;
            }

            // Add helicopters as long as the number doesn't exceed the required number based on wanted level
            if (helicopters.Count < helicoptersToDispatch)
            {
                var heli = new Helicopter(info);
                
                // Assign a random task for the helicopter
                switch (new Random().Next(4))
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
                    case 3:
                        heli.Paratroop = true;
                        break;
                    default:
                        heli.EnableHeliWeapons();
                        break;
                }
                helicopters.Add(heli);

            }

            // If wanted level drops below 3, remove all helicopters
            if (WantedLevel < 3)
            {
                foreach (var heli in helicopters.ToList()) // ToList() to avoid modifying the collection while iterating
                {
                    heli.LeaveTheScene(heli.Vehicle.Position); 
                    helicopters.Remove(heli);
                    heli.Vehicle.MarkAsNoLongerNeeded();  // Mark helicopter vehicle as no longer needed
                    heli.Pilot.MarkAsNoLongerNeeded();    // Mark pilot as no longer needed
                    heli.Crew.ForEach(c => c.MarkAsNoLongerNeeded()); // Mark all crew members as no longer needed

                }
            }

            foreach (var helli in helicopters.ToList())
            {
                if (helli.Vehicle.Position.DistanceTo(PlayerPosition) > 400)
                {
                    helli.Vehicle.MarkAsNoLongerNeeded();
                    helicopters.Remove(helli);
                    helli.Vehicle.Delete();
                }
            }
        }

    }
}
