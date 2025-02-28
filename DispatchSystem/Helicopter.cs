﻿using GTA;
using GTA.Math;
using System;
using GTA.Native;
using System.Collections.Generic;
using System.Linq;
using GTA.UI;
using Guarding.DispatchSystem;
using GTA.NaturalMotion;

public class Helicopter
{
    private GTA.Vehicle helicopter;
    public bool Rappel { get; set; }
    public bool Land { get; set; }
    public bool Paratroop { get; set; }
    public bool Pursuit { get; set; }
    public bool CanRappel { get; private set; }

    private List<Ped> crew;
    private Dictionary<VehicleWeaponHash, bool> weaponStates;
    private bool isRappelComplete;
    private Vector3 PositionToReach;
    private bool hasLanded;

    private const int LOW_HEALTH_THRESHOLD = 50;
    private const int CRITICAL_HEALTH_THRESHOLD = 30;

    private const float PURSUIT_SPEED = 100f;
    private const float RAPPEL_SPEED = 5f;
    private const int FLEE_SPEED = 120;

    private const float RAPPEL_RADIUS = 15f;

    private const int PURSUIT_HEIGHT = 80;
    private const float RAPPEL_HEIGHT = 20;
    private const float FLEE_HEIGHT = 50f;

    public GTA.Vehicle Vehicle
    {
        get
        {
            if (helicopter.Exists())
            {
                return helicopter;
            }
            return null;
        }
    }

    public Ped Pilot
    {
        get {
            if (helicopter.Driver.Exists())
                return helicopter.Driver;
            return null;
        }

    }

    public List<Ped> Crew
    {
        get
        {
            List<Ped> validCrew = new List<Ped>();

            foreach (var crewMember in crew)
            {
                if (crewMember != null && crewMember.IsAlive) // Ensure the crew member is not null and alive
                {
                    validCrew.Add(crewMember); // Add valid crew member to the list
                }
            }

            return validCrew;
        }
    }

    public bool IsArmed { get; private set; }

    // Constructor for unarmed/transport helicopter

    public Helicopter(VehicleInformation info)
    {
        var primaryWeapons = info.Soldiers.Weapons.SelectMany(w => w.PrimaryWeapon).ToList();
        var secondaryWeapons = info.Soldiers.Weapons.SelectMany(w => w.SecondaryWeapon).ToList();

        Initialize(info.VehicleModels, info.Pilot, info.Soldiers.Soldiers, null, primaryWeapons, secondaryWeapons, true);
    }


    // Constructor for armed helicopter
    public Helicopter(string heliModel, string pilotModel, List<string> crewModels,
        Dictionary<VehicleWeaponHash, string> weapons, List<string> primaryWeapons, List<string> secondaryWeapons)
    {
        // Initialize(heliModel, pilotModel, crewModels, weapons, primaryWeapons, secondaryWeapons);
    }

    // Constructor for existing helicopter
    public Helicopter(GTA.Vehicle existingHeli, List<string> primaryWeapons, List<string> secondaryWeapons)
    {
        if (existingHeli == null || !existingHeli.Exists())
            throw new ArgumentNullException(nameof(existingHeli));

        helicopter = existingHeli;

        foreach (Ped occupant in helicopter.Occupants)
        {
            crew.Add(occupant);
        }

        //AssignWeapons(crew, primaryWeapons, secondaryWeapons);
    }

    Random rand = new Random();

    private void Initialize(
    List<string> heliModel,
    List<string> pilotModel,
    List<string> crewModels,
    Dictionary<VehicleWeaponHash, string> vehicleWeapons = null,
    List<string> primaryWeapons = null,
    List<string> secondaryWeapons = null, 
    bool isDispatch = false)
    {

        // Ensure the lists are not empty
        if (heliModel == null || heliModel.Count == 0)
            throw new ArgumentException("Helicopter models list is empty");
        if (pilotModel == null || pilotModel.Count == 0)
            throw new ArgumentException("Pilot models list is empty");
        if (crewModels == null || crewModels.Count == 0)
            throw new ArgumentException("Crew models list is empty");
        if (primaryWeapons == null || primaryWeapons.Count == 0)
            throw new ArgumentException("primary models list is empty");
        if (secondaryWeapons == null || secondaryWeapons.Count == 0)
            throw new ArgumentException("secondary models list is empty");

        // Create helicopter above the player's position
        helicopter = World.CreateVehicle(heliModel[rand.Next(0, heliModel.Count)], Game.Player.Character.Position.Around(300) + new Vector3(0, 0, 50f));
        IsDispatchHelicopter = isDispatch;
        helicopter.Model.MarkAsNoLongerNeeded();
        helicopter.MarkAsNoLongerNeeded();
        helicopter.IsEngineRunning = true;
        helicopter.HeliBladesSpeed = 1f;

        helicopter.Mods.InstallModKit();

        CanRappel = helicopter.AllowRappel;

        // Initialize crew list
        crew = new List<Ped>();

        // Create and assign pilot
        var pilot = HelperClass.CreateAndAssignPedPassenger(helicopter, pilotModel[rand.Next(0, pilotModel.Count)], VehicleSeat.Driver, true);
        
        pilot.BlockPermanentEvents = true;
        // Assign a handgun (secondary weapon) to the pilot
        HelperClass.AssignWeapons( pilot, null, secondaryWeapons);

        // Assign passengers (limit to available seats) excluding driver/pilot
        for (int i = 0; i < helicopter.PassengerCapacity; i++)
        {
            var crewPed = HelperClass.CreateAndAssignPedPassenger(helicopter, crewModels[rand.Next(0, crewModels.Count)], (VehicleSeat)(i), true);
            crew.Add(crewPed);
            //Function.Call(Hash.CLEAR_DEFAULT_PRIMARY_TASK, crewPed);
            // Assign both primary and secondary weapons to the crew members
            HelperClass.AssignWeapons(crewPed, primaryWeapons, secondaryWeapons);
        }

        //helicopter.
        //VehicleMod mod = helicopter.Mods[VehicleModType.Engine];



        // Initialize vehicle weapons (specific to the helicopter)

        //IsArmed = vehicleWeapons?.Count > 0;

        //weaponStates = new Dictionary<VehicleWeaponHash, bool>();

        //if (IsArmed)
        //{
        //    foreach (var weapon in vehicleWeapons)
        //    {
        //        weaponStates[weapon.Key] = true;
        //        EnableVehicleWeapon(weapon.Key, pilot);
        //    }
        //}
    }

  

    bool critical = false;
    bool low = false;

    public bool IsHelicopterValid()
    {
        return helicopter != null                     // Ensure the helicopter object exists
            && helicopter.Exists()                    // Check if the helicopter exists in the game world
            && !helicopter.IsDead                     // Verify the helicopter is not destroyed
            && helicopter.Driver != null              // Ensure there is a driver
            && helicopter.Driver.Exists()             // Check if the driver exists in the game world
            && !helicopter.Driver.IsDead;             // Verify the driver is not dead
    }

    private List<bool> crewStatus; // To track each crew member's alive state
    private bool ForceFlee = false;

    

    private void CrewManagement()
    {
        // If forced flee is not enabled, proceed with crew checks
        if (!ForceFlee)
        {
            // Update crewStatus list
            crewStatus = new List<bool>();

            foreach (var crewMember in crew)
            {
                if (crewMember != null && crewMember.Exists())
                {
                    crewStatus.Add(!crewMember.IsDead); // Add true if alive, false if dead
                }
            }

            // Check if all passengers except the copilot are dead
            bool allPassengersDead = true;

            foreach (var crewMember in crew)
            {
                if (crewMember != null && crewMember.Exists() && !crewMember.IsDead)
                {
                    // Skip the copilot (e.g., index 0 or based on seat condition)
                    if (crewMember.SeatIndex == VehicleSeat.RightFront)
                        continue;

                    // If any passenger is alive, update flag and stop the check
                    allPassengersDead = false;
                    break;
                }
            }

            // If all passengers (excluding the copilot) are dead, trigger flee behavior
            if (allPassengersDead)
            {
                LeaveTheScene(helicopter.Position);
               
            }

            // If not all passengers are dead, continue armed behavior if applicable
            else if (IsArmed)
            {
                HelperClass.EnableAttack(helicopter);
                Pursuit = true;
                EnableHeliAttacks = true;
            }
        }
    }


    public bool IsDispatchHelicopter = false;
    public bool Cleared = false;

    public void HandleLawBehavior()
    {
        if (!IsDispatchHelicopter || helicopter == null || !helicopter.Exists())
            return;

        // If wanted level is zero
        if (Game.Player.WantedLevel == 0 && !Cleared)
        {
            // Check if the helicopter is not landing or rappelling
            if (!HelperClass.IsPedRappelingFromHelicopter(helicopter) || helicopter.GetActiveMissionType() != VehicleMissionType.Land)
            {
                
                // Initiate helicopter fleeing
                LeaveTheScene(Game.Player.Character.Position);
                Cleared = true;
                // Handle crew behavior
                foreach (var crewMember in crew)
                {
                    if (crewMember != null && crewMember.Exists() && !crewMember.IsDead)
                    {
                        // Clear the current task without removing from the helicopter or dismissing
                        crewMember.Task.ClearSecondary();

                        // Optionally, reset the ped's behavior to idle if on foot
                        if (crewMember.IsOnFoot)
                        {
                            // Create a new task sequence
                            var taskSequence = new TaskSequence();

                            // Step 1: Investigate - Start searching for the player
                            taskSequence.AddTask.LookAt(Game.Player.Character);

                            // Step 2: Go to a nearby place (around 100 units from the player) and move to the target
                            Vector3 targetPosition = Game.Player.Character.Position.Around(20);
                            taskSequence.AddTask.FollowNavMeshTo(targetPosition, PedMoveBlendRatio.Walk, -1, 0.25f, FollowNavMeshFlags.Default); // Walk speed

                            // Step 3: Optionally, perform binoculars scenario if near the target position
                            taskSequence.AddTask.Pause(500); // Wait for 500ms to simulate arrival
                            taskSequence.AddTask.StartScenarioInPlace("WORLD_HUMAN_BINOCULARS"); // Replace with actual scenario name

                            // Step 4: Simulate a phone call when near the target
                            taskSequence.AddTask.Pause(6000); // Wait for 1 second before phone call
                            taskSequence.AddTask.UseMobilePhone(5000); // Phone call animation

                            // Step 5: Move to a final position (50 units around the player)
                            Vector3 finalPosition = Game.Player.Character.Position.Around(50);
                            taskSequence.AddTask.FollowNavMeshTo(finalPosition, PedMoveBlendRatio.Walk, -1, 0.25f, FollowNavMeshFlags.Default); // Move to final position at walk speed

                            // Execute the task sequence
                            crewMember.Task.PerformSequence(taskSequence);
                            taskSequence.Close(false);
                        }
                    }
                }
            }
        }
        else if (Game.Player.WantedLevel >0)
        {
            Cleared = false;
        }
    }

   


    private void HandleFarOrDeadStates()
    {
        Vector3 playerPosition = Game.Player.Character.Position;

        if (helicopter != null || helicopter.Exists() || helicopter.IsDead || helicopter.Position.DistanceTo(playerPosition) > 500)
        {
            helicopter.MarkAsNoLongerNeeded();
        }

        if (helicopter.Driver != null || helicopter.Driver.Exists() ||helicopter.Driver.IsDead || helicopter.Driver.Position.DistanceTo(playerPosition) > 500)
        {
            helicopter.Driver.MarkAsNoLongerNeeded();
        }

        foreach (var crewMember in crew.ToList())
        {
            if (crewMember == null || !crewMember.Exists() || crewMember.IsDead || crewMember.Position.DistanceTo(playerPosition) > 500)
            {
                crewMember.MarkAsNoLongerNeeded();
            }
        }
    }


    public void UpdateProcess()
    {
        if (!IsHelicopterValid())
            return;

        CrewManagement();

        // Check if helicopter, pilot, or crew are far away or dead/not existing
        HandleFarOrDeadStates();
        ParatrooperDeployment();
        RappelPeds();
        LandHelicopter();
        HandlePursuit();
    }

    bool EnableHeliAttacks=false;

    private void HandlePursuit()
    {
        if (Pursuit)
        {
            // Reset other actions when in pursuit
            Land = false;
            Rappel = false;
            Paratroop = false;

            // Check if the player is in an aircraft
            if (Game.Player.Character.IsInVehicle() && Game.Player.Character.CurrentVehicle.IsAircraft || EnableHeliAttacks)
            {
                // If the helicopter's current mission is not "Attack"
                if (helicopter.Position.DistanceTo(Game.Player.Character.Position.Around(20)) > 50)
                {
                    if (helicopter.Driver.BlockPermanentEvents == false) Pilot.BlockPermanentEvents = true;

                    HelperClass.EnableAttack(helicopter);
                    // Assign the helicopter to attack the player's vehicle
                    helicopter.Driver.Task.StartHeliMission(
                        helicopter,                  // The helicopter
                        Game.Player.Character.CurrentVehicle, // Target: player's aircraft
                        VehicleMissionType.Attack,          // Mission type: attack
                        PURSUIT_SPEED,                      // Mission radius (keep at a distance of 20 meters)
                        80,                                // Flight altitude
                        -1,                                // Speed (optional)
                        70
                    );
                }
            }

            else if (Game.Player.Character.IsOnFoot && Game.Player.Character.Position.DistanceTo(helicopter.Position) > 50)
            {
                if (helicopter.Driver.BlockPermanentEvents == true) Pilot.BlockPermanentEvents = false;

                if (Game.Player.Character.Weapons.Current.Group != WeaponGroup.Heavy)
                    HelperClass.DisableAttack(helicopter);
                else
                    HelperClass.EnableAttack(helicopter);

                helicopter.Driver.Task.StartHeliMission(
                       helicopter,                  // The helicopter
                       Game.Player.Character, // Target: player's position
                       VehicleMissionType.PoliceBehaviour,          // Mission type: attack
                       PURSUIT_SPEED,                      // Mission radius (keep at a distance of 20 meters)
                       80,                                // Flight altitude
                       -1,                                // Speed (optional)
                       70
                   );
            }
            else
            {
                // If the player is not in an aircraft
                if (helicopter.Position.DistanceTo(Game.Player.Character.Position) > 50)
                {
                    if(helicopter.Driver.BlockPermanentEvents == false) Pilot.BlockPermanentEvents = true;
                    HelperClass.DisableAttack(helicopter);
                    // Assign the helicopter to follow the player
                    helicopter.Driver.Task.StartHeliMission(
                        helicopter,                  // The helicopter
                        Game.Player.Character, // Target: player's aircraft
                        VehicleMissionType.Follow,         // Mission type: follow
                        PURSUIT_SPEED,                                // Mission radius (keep at a distance of 20 meters)
                        60,                                // Flight altitude
                        -1,                                // Speed (optional)
                        PURSUIT_HEIGHT                                // Firing pattern
                    );
                }
            }
        }
    }

    private const float PARA_APPROACH_SPEED = 50f;
    private const float PARA_HOVER_SPEED = 20f;
    private const float PARA_DESIRED_HEIGHT = 150f;
    private const float STABILITY_THRESHOLD = 1.0f;

    private bool isAtDeploymentHeight = false;
    private bool isStabilized = false;
    public bool AllowCoPilotJump { get; set; } = false; // New flag to control co-pilot jumping
    List<Ped> parachuteStatePeds = new List<Ped>();

    private void ParatrooperDeployment()
    {
        if (!IsHelicopterValid() || !Paratroop)
            return;

        // Step 1: Get to deployment height if not there
        if (!isAtDeploymentHeight)
        {
            //helicopter.Driver.BlockPermanentEvents = true;
            if (helicopter.HeightAboveGround < PARA_DESIRED_HEIGHT)
            {
                Vector3 deploymentPosition = helicopter.Position + Vector3.WorldUp * (PARA_DESIRED_HEIGHT - helicopter.HeightAboveGround);

                helicopter.Driver.Task.StartHeliMission(
                    helicopter,
                    deploymentPosition,
                    VehicleMissionType.GoTo,
                    PARA_APPROACH_SPEED,
                    20f,
                    (int)PARA_DESIRED_HEIGHT,
                    (int)PARA_DESIRED_HEIGHT
                );

                 return;
            }

            isAtDeploymentHeight = true;
        }

        // Step 2: Stabilize helicopter
        if (!isStabilized)
        {
            if (helicopter.Velocity.Length() > STABILITY_THRESHOLD)
            {
                helicopter.Driver.Task.StartHeliMission(
                    helicopter,
                    helicopter.Position,
                    VehicleMissionType.GoTo,
                    PARA_HOVER_SPEED,
                    10f,
                    (int)PARA_DESIRED_HEIGHT,
                    (int)PARA_DESIRED_HEIGHT
                );

                  return;
            }

            isStabilized = true;
            
        }

        // Step 3: Check and manage rear seats
        var rearSeats = new[] { VehicleSeat.LeftRear, VehicleSeat.RightRear };
        foreach (var seat in rearSeats)
        {
            if (!helicopter.IsSeatFree(seat))
            {
                var paratrooper = helicopter.GetPedOnSeat(seat);
                StartParachuting(paratrooper);
            }
        }

        // Move remaining crew to rear seats and deploy them
        foreach (var seat in rearSeats)
        {
            if (helicopter.IsSeatFree(seat))
            {
                var nextCrewMember = crew.FirstOrDefault(c => c != helicopter.Driver && c.SeatIndex != VehicleSeat.RightFront);
                if (nextCrewMember != null)
                {
                    nextCrewMember.SetIntoVehicle(helicopter, seat);
                    StartParachuting(nextCrewMember);
                }
            }
        }

        // Handle paratroopers in parachute state
        HandleParachuteState();

        // Check if deployment is complete
        if (IsDeploymentComplete())
        {
            LeaveTheScene(Game.Player.Character.Position.Around(500));
            Paratroop = false;
            return;
        }
    }

    private void StartParachuting(Ped paratrooper)
    {
        if (paratrooper != null && !parachuteStatePeds.Contains(paratrooper))
        {
            paratrooper.Task.LeaveVehicle(LeaveVehicleFlags.BailOut);
            paratrooper.Weapons.Give(WeaponHash.Pistol, 100, true, true); // Give handgun
            parachuteStatePeds.Add(paratrooper);
        }
    }

    private void HandleParachuteState()
    {
        foreach (var paratrooper in parachuteStatePeds.ToList())
        {
            if (paratrooper.IsDead)
            {
                // Remove dead paratroopers
                parachuteStatePeds.Remove(paratrooper);
                paratrooper.MarkAsNoLongerNeeded();
                crew.Remove(paratrooper);
            }
            else if (paratrooper.IsInParachuteFreeFall)
            {
                // Handle paratroopers that are in free fall
                paratrooper.Task.UseParachute();
                paratrooper.OpenParachute();
                paratrooper.Task.ParachuteTo(Game.Player.Character.Position.Around(20));
            }
            else if (paratrooper.IsOnFoot && !paratrooper.IsInParachuteFreeFall)
            {
                // Remove paratroopers that have landed
                parachuteStatePeds.Remove(paratrooper);
                crew.Remove(paratrooper);
                paratrooper.MarkAsNoLongerNeeded();
            }
        }
    }
    private bool IsDeploymentComplete()
    {
        if (AllowCoPilotJump)
        {
            return helicopter.Occupants.Count() <= 1; // Only pilot remains
        }
        else
        {
            return helicopter.Occupants.Count() <= 2; // Pilot and co-pilot remain
        }
    }

    

    Vector3 potentialLandingPosition;

    private bool isLandingPositionFound = false; // Flag to check if landing position is found

    private bool isHoverPositionReached = false; // Flag to check if hover position is reached

   

    public void LeaveLandedHelicopter(CrewLeaveOption leaveOption)
    {
        foreach (var crewMember in crew)
        {
            switch (leaveOption)
            {
                case CrewLeaveOption.OnlyCrew:
                    // Only non-pilot/co-pilot crew members leave
                    if (crewMember.SeatIndex != VehicleSeat.Driver && crewMember.SeatIndex != VehicleSeat.RightFront)
                    {
                        crewMember.Task.LeaveVehicle();
                    }
                    break;

                case CrewLeaveOption.CoPilotAndCrew:
                    // Co-pilot and crew members leave, but pilot stays
                    if (crewMember.SeatIndex == VehicleSeat.Driver)
                    {
                        // Pilot stays
                        //crewMember.BlockPermanentEvents = true; 
                        crewMember.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, false);
                    }
                    else
                    {
                        // Co-pilot and crew leave
                        crewMember.Task.LeaveVehicle();
                    }
                    break;

                case CrewLeaveOption.All:
                    // All crew members leave the vehicle
                    if (crewMember.SeatIndex == VehicleSeat.Driver)
                        crewMember.Task.LeaveVehicle();
                    crewMember.BlockPermanentEvents = false; // Allow them to interact freely after leaving
                    break;

                default:
                    // Default case: No action
                    break;
            }
        }
    }

    private bool isDeparting = false; // Flag to track if the helicopter is departing
    private bool hasCooldownStarted = false; // Flag for cooldown
    private DateTime cooldownStartTime; // Start time for cooldown

    public void LandHelicopter(CrewLeaveOption option=CrewLeaveOption.OnlyCrew)
    {
        // Ensure valid helicopter and landing flag
        if (!IsHelicopterValid() || helicopter.Driver == null || !Land)
            return;


        if (!hasLanded)
        {
            // Find landing position once
            if (!isLandingPositionFound)
            {
                potentialLandingPosition = World.GetNextPositionOnStreet(Game.Player.Character.Position.Around(20));

                if (potentialLandingPosition.DistanceTo(Game.Player.Character.Position) > 60)
                {
                    Rappel = true;
                    Land = false;
                    return;
                }

                //helicopter.Driver.BlockPermanentEvents = true;
                isLandingPositionFound = true;

                // Set hover position above landing position
                PositionToReach = potentialLandingPosition + Vector3.WorldUp * 40;
                helicopter.Driver.Task.StartHeliMission(helicopter, PositionToReach, VehicleMissionType.GoTo, 70, 10, 100, 0);
                return;
            }

            // Check if hover position is reached
            if (!isHoverPositionReached)
            {
                if (helicopter.Position.DistanceTo(PositionToReach) >= 20)
                {
                    return;
                }

                isHoverPositionReached = true;
                }

            // Start landing sequence
            if (helicopter.GetActiveMissionType() != VehicleMissionType.Land)
            {
                helicopter.Driver.Task.StartHeliMission(
                    helicopter,
                    potentialLandingPosition,
                    VehicleMissionType.Land,
                    50, 0, -1, 0
                );
                return;
            }

            // Check ground height and landing progress
            float groundHeight;
            OutputArgument unk = new OutputArgument();
            var success = Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, helicopter.Position.X, helicopter.Position.Y, helicopter.Position.Z, unk);
            float heliHeight = helicopter.Position.Z;
            groundHeight = unk.GetResult<float>();

            if (heliHeight - groundHeight < 1.5f) // Close enough to the ground
            {
                 LeaveLandedHelicopter(option);

                // Cooldown-based approach
                if (!hasCooldownStarted)
                {
                    hasCooldownStarted = true;
                    cooldownStartTime = DateTime.Now;
                    return; // Exit to wait for cooldown
                }

                if ((DateTime.Now - cooldownStartTime).TotalSeconds < 2)
                    return; // Wait for 2 seconds

                hasCooldownStarted = false; // Reset cooldown flag
                hasLanded = true;

                // Helicopter departs the scene
                isDeparting = true;
                if(option == CrewLeaveOption.OnlyCrew || option == CrewLeaveOption.CoPilotAndCrew) LeaveTheScene(Game.Player.Character.Position.Around(500));
                if (helicopter.Driver.IsDead) LeaveLandedHelicopter(CrewLeaveOption.All);
                return;
            }

             }
        else if (isDeparting)
        {
            return;
        }
    }

    bool isRappelPositionFound = false;
    bool isRappelPosition = false;

    public void RappelPeds()
    {
        // Check if the helicopter is valid and rappelling is enabled
        if (!IsHelicopterValid() || !Rappel)
        {
            if (!CanRappel)
            {
                Rappel = false;
                Pursuit = true;
                return;
            }
            return;
        }

        
        // If rappelling is already complete, no need to continue
        if (isRappelComplete)
        {
            return;
        }

        // Find and move to rappel position
        if (!isRappelPositionFound)
        {
            PositionToReach = Game.Player.Character.Position.Around(30) + Vector3.WorldUp * RAPPEL_HEIGHT;
            isRappelPositionFound = true;
            // Slower approach speed for initial positioning
            helicopter.Driver.Task.StartHeliMission(
                helicopter,
                PositionToReach,
                VehicleMissionType.GoTo,
                RAPPEL_SPEED,      // Slower speed for precise positioning
                RAPPEL_RADIUS,     // Tighter radius for better control
                -1,
                (int)RAPPEL_HEIGHT // Maintain consistent height
            );
            return;
        }

        // Wait until helicopter reaches rappel position
        if (!isRappelPosition)
        {
            if (helicopter.Position.DistanceTo(PositionToReach) >= 20)
            {
                float distanceRatio = helicopter.Position.DistanceTo(PositionToReach) / 50f; // 50f is max distance considered
                float adjustedSpeed = Math.Max(RAPPEL_SPEED * distanceRatio, 1f); // Minimum speed of 1

                helicopter.Driver.Task.StartHeliMission(
               helicopter,
               PositionToReach,
               VehicleMissionType.GoTo,
               adjustedSpeed,
               RAPPEL_RADIUS,
               (int)RAPPEL_HEIGHT,
               (int)RAPPEL_HEIGHT
           );
                return;
            }
            isRappelPosition = true;
        }

        // Start hovering at rappel position
        if (helicopter.GetActiveMissionType() != VehicleMissionType.GoTo)
        {
            if (helicopter.Position.DistanceTo(PositionToReach) < 20)
            {
                helicopter.Driver.Task.StartHeliMission(
                    helicopter,
                    PositionToReach,
                    VehicleMissionType.GoTo,
                    5f,               // Very slow speed for stable hover
                    10f,              // Tight radius for minimal drift
                    (int)RAPPEL_HEIGHT,
                    (int)RAPPEL_HEIGHT
                );
                return;
            }
        }

        // Begin rappelling sequence when in position
        if (helicopter.Position.DistanceTo(PositionToReach) < 20)
        {
            bool allCrewOutside = true;
            
            // Start rappelling for all crew members still in the helicopter
            foreach (var crewMember in crew)
            {
                // Skip pilot and co-pilot
                VehicleSeat seatIndex = crewMember.SeatIndex;
                if (seatIndex == VehicleSeat.Driver || seatIndex == VehicleSeat.RightFront) // -1: pilot, 0: co-pilot (adjust based on your system)
                {
                    continue;
                }

                if (crewMember.IsInVehicle(helicopter))
                {
                    crewMember.Task.RappelFromHelicopter();
                    
                    allCrewOutside = false;
                }
                else if (HelperClass.IsPedRappelingFromHelicopter(helicopter))
                {
                    allCrewOutside = false;
                }
            }

            // If helicopter is rappelling and all crew are out, mark as complete
            if (!HelperClass.IsPedRappelingFromHelicopter(helicopter) && allCrewOutside)
            {

                //i was wonderwing why peds ragdoll when rappel hahhaa i forgot that it was true
                isRappelComplete = true;
                 
                    LeaveTheScene(PositionToReach);
                    return;
                
            }

            // If crew members are still rappelling, wait
            if (!allCrewOutside)
            {
                return;
            }
        }
    }

    public enum CrewLeaveOption
    {
        OnlyCrew,            // Only non-pilot/co-pilot crew leaves
        CoPilotAndCrew,      // Co-pilot and other crew leave (pilot stays)
        All                  // All crew members leave the vehicle
    }

    public void LeaveTheScene(Vector3 fleepos)
    {
        //DisableHeliWeapons();
        Rappel = false;
        Land = false;
        Paratroop = false;
        Pursuit = false;
        low = false;
        critical = false;
       HelperClass.DisableAttack(helicopter);
        helicopter.Driver.Task.StartHeliMission(helicopter, fleepos + Vector3.WorldUp*FLEE_HEIGHT, VehicleMissionType.Flee, FLEE_SPEED, 10, -1, (int)FLEE_HEIGHT);
    }


    }


