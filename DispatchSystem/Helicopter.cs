using GTA;
using GTA.Math;
using System;
using GTA.Native;
using System.Collections.Generic;
using System.Linq;
using GTA.UI;

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
    private const int LOW_HEALTH_THRESHOLD = 300;
    private const int CRITICAL_HEALTH_THRESHOLD = 100;
    // Add these constants at the top of the Helicopter class
    private const float PURSUIT_SPEED = 70f;
    private const float LANDING_SPEED = 30f;
    private const float RAPPEL_SPEED = 20f;
    private const int FLEE_SPEED = 90;

    private const int PURSUIT_RADIUS = 30;
    private const float LANDING_RADIUS = 10f;
    private const float RAPPEL_RADIUS = 15f;

    private const int PURSUIT_HEIGHT = 40;
    private const float LANDING_HEIGHT = 20f;
    private const float RAPPEL_HEIGHT = 40f;
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

        Initialize(info.VehicleModels, info.Pilot, info.Soldiers.Soldiers, null, primaryWeapons, secondaryWeapons);
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
        crew = new List<Ped>();
        foreach (Ped occupant in helicopter.Occupants)
        {
            crew.Add(occupant);
        }

        //AssignWeapons(crew, primaryWeapons, secondaryWeapons);
    }

    public void Print(string msg)
    {
        GTA.UI.Screen.ShowSubtitle(msg);
    }

    Random rand = new Random();

    private void Initialize(
    List<string> heliModel,
    List<string> pilotModel,
    List<string> crewModels,
    Dictionary<VehicleWeaponHash, string> vehicleWeapons = null,
    List<string> primaryWeapons = null,
    List<string> secondaryWeapons = null)
    {

        // Ensure the lists are not empty
        if (heliModel == null || heliModel.Count == 0)
            throw new ArgumentException("Helicopter models list is empty");
        if (pilotModel == null || pilotModel.Count == 0)
            throw new ArgumentException("Pilot models list is empty");
        if (crewModels == null || crewModels.Count == 0)
            throw new ArgumentException("Crew models list is empty");

        // Create helicopter above the player's position
        helicopter = World.CreateVehicle(heliModel[rand.Next(0, heliModel.Count)], Game.Player.Character.Position.Around(300) + new Vector3(0, 0, 50f));

        CanRappel = helicopter.AllowRappel;
        
        // Initialize crew list
        crew = new List<Ped>();

        // Create and assign pilot
        var pilot = CreateAndAssignPed(pilotModel[rand.Next(0, pilotModel.Count)], VehicleSeat.Driver);
       
        // Assign a handgun (secondary weapon) to the pilot
        AssignWeapons(pilot, null, secondaryWeapons);

        // Assign passengers (limit to available seats)
        for (int i = 0; i < helicopter.PassengerCapacity; i++)
        {
            var crewPed = CreateAndAssignPed(crewModels[rand.Next(0, crewModels.Count)], (VehicleSeat)(i));
            crew.Add(crewPed);

            // Assign both primary and secondary weapons to the crew members
            AssignWeapons(crewPed, primaryWeapons, secondaryWeapons);
        }

        helicopter.IsEngineRunning = true;
        helicopter.HeliBladesSpeed = 1f;

        helicopter.Mods.InstallModKit();
        //helicopter.

        
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

    private Ped CreateAndAssignPed(string pedModelName, VehicleSeat seat)
    {
        var pedModel = new Model(pedModelName);
        pedModel.Request(1000);

        if (!pedModel.IsValid || !pedModel.IsInCdImage)
            throw new ArgumentException($"Invalid ped model: {pedModelName}");

        var ped = helicopter.CreatePedOnSeat(seat, pedModel);
        ped.SetConfigFlag(PedConfigFlagToggles.EquippedAmbientLoadOutWeapon, true);
        ped.SetConfigFlag(PedConfigFlagToggles.DontAttackPlayerWithoutWantedLevel, true);
        ped.SetConfigFlag(PedConfigFlagToggles.LawWillOnlyAttackIfPlayerIsWanted, true);
        ped.SetConfigFlag(PedConfigFlagToggles.OnlyAttackLawIfPlayerIsWanted, true);
        return ped;
    }

    private void AssignWeapons(Ped ped, List<string> primaryWeapons, List<string> secondaryWeapons)
    {
        var random = new Random();

        // Assign a random primary weapon if available
        if (primaryWeapons != null && primaryWeapons.Count > 0)
        {
            var primaryWeaponName = primaryWeapons[random.Next(primaryWeapons.Count)];
            WeaponHash primaryWeaponHash;

            if (Enum.TryParse(primaryWeaponName, out primaryWeaponHash))
            {
                ped.Weapons.Give(primaryWeaponHash, 900, true, true);
                Print($"Assigned primary weapon {primaryWeaponName} to ped {ped.Handle}");
            }
            else
            {
                Print($"Invalid primary weapon name: {primaryWeaponName}");
            }
        }

        // Assign a random secondary weapon if available
        if (secondaryWeapons != null && secondaryWeapons.Count > 0)
        {
            var secondaryWeaponName = secondaryWeapons[random.Next(secondaryWeapons.Count)];
            WeaponHash secondaryWeaponHash;

            if (Enum.TryParse(secondaryWeaponName, out secondaryWeaponHash))
            {
                ped.Weapons.Give(secondaryWeaponHash, 200, false, true);
                Print($"Assigned secondary weapon {secondaryWeaponName} to ped {ped.Handle}");
            }
            else
            {
                Print($"Invalid secondary weapon name: {secondaryWeaponName}");
            }
        }
    }

    public bool IsHelicopterValid()
    {
        return helicopter != null                     // Ensure the helicopter object exists
            && helicopter.Exists()                    // Check if the helicopter exists in the game world
            && !helicopter.IsDead                     // Verify the helicopter is not destroyed
            && helicopter.Driver != null              // Ensure there is a driver
            && helicopter.Driver.Exists()             // Check if the driver exists in the game world
            && !helicopter.Driver.IsDead;             // Verify the driver is not dead
    }
    private void CheckHealth()
    {
        if (helicopter == null || !helicopter.Exists() || helicopter.IsDead)
        {
            MarkAsNoLongerNeeded();
            return;
        }

        if (helicopter.Health < CRITICAL_HEALTH_THRESHOLD)
        {
            HandleCriticalDamage();
        }
        else if (helicopter.Health < LOW_HEALTH_THRESHOLD)
        {
            HandleLowHealth();
        }
    }

    private void HandleLowHealth()
    {
        // Low health, reduce aggression and prepare for retreat
        SetFiringPattern(FiringPattern.SingleShot);
        SetShootRate(2000); // Less frequent shooting
        SetAccuracy(40); // Lower accuracy
        LeaveTheScene(Game.Player.Character.Position.Around(500));
    }

    private void HandleCriticalDamage()
    {
        // Critical health, initiate emergency landing and make everyone leave
        if (!hasLanded)
        {
            PositionToReach = World.GetNextPositionOnStreet(helicopter.Position);
            helicopter.Driver.Task.StartHeliMission(helicopter, PositionToReach, VehicleMissionType.None, 10, 10, -1, 0);
            foreach (var crewMember in crew.ToList())
            {
                crewMember.Task.LeaveVehicle();
                crewMember.MarkAsNoLongerNeeded();
                crew.Remove(crewMember);
            }
            hasLanded = true;
        }
        else
        {
            MarkAsNoLongerNeeded();
        }
    }

    private void AdjustFiringPatternBasedOnSpeed()
    {
        if (helicopter == null || !helicopter.Exists())
            return;

        float speed = helicopter.Speed;
        if (speed > 20 && speed <= 50)
        {
            SetFiringPattern(FiringPattern.BurstFire);
            SetShootRate(1000);
            SetAccuracy(80);
        }
        else if (speed > 50)
        {
            SetFiringPattern(FiringPattern.FullAuto);
            SetShootRate(500);
            SetAccuracy(60);
        }
        else
        {
            SetFiringPattern(FiringPattern.SingleShot);
            SetShootRate(2000);
            SetAccuracy(40);
        }
    }
    FiringPattern Pattern;
    int Accuracy;
    int RoF;

    private void SetFiringPattern(FiringPattern pattern)
    {
        foreach (var crewMember in crew)
        {
            crewMember.FiringPattern = pattern;
        }
    }

    private void SetShootRate(int rate)
    {
        foreach (var crewMember in crew)
        {
            crewMember.Accuracy = rate;
        }
    }

    private void SetAccuracy(int accuracy)
    {
        foreach (var crewMember in crew)
        {
            crewMember.Accuracy = accuracy;
        }
    }

    private void HandleFarOrDeadStates()
    {
        Vector3 playerPosition = Game.Player.Character.Position;

        if (helicopter != null && helicopter.Exists() && helicopter.Position.DistanceTo(playerPosition) > 500)
        {
            helicopter.MarkAsNoLongerNeeded();
        }

        if (helicopter.Driver != null && helicopter.Driver.Exists() && helicopter.Driver.Position.DistanceTo(playerPosition) > 500)
        {
            helicopter.Driver.MarkAsNoLongerNeeded();
        }

        foreach (var crewMember in crew.ToList())
        {
            if (crewMember == null || !crewMember.Exists() || crewMember.IsDead || crewMember.Position.DistanceTo(playerPosition) > 500)
            {
                crewMember?.MarkAsNoLongerNeeded();
            }
        }
    }

    public void MarkAsNoLongerNeeded()
    {
        helicopter?.MarkAsNoLongerNeeded();
        foreach (var crewMember in crew)
        {
            crewMember?.MarkAsNoLongerNeeded();
        }
        crew.Clear();
    }

    void CombatHatedTargets()
    {
        foreach(var crewMember in crew.ToList())
        {
            crewMember.Task.CombatHatedTargetsAroundPed(100);
        }
    }

    public void UpdateProcess()
    {
        if (!IsHelicopterValid())
            return;

        CombatHatedTargets();
        CheckHealth();

        // Adjust firing pattern, shoot rate, and accuracy based on speed
        AdjustFiringPatternBasedOnSpeed();

        // Check if helicopter, pilot, or crew are far away or dead/not existing
        HandleFarOrDeadStates();
        ParatrooperDeployment();
        RappelPeds();
        LandHelicopter();
        HandlePursuit();
    }

    private void HandlePursuit()
    {
        if (Pursuit)
        {
            // Reset other actions when in pursuit
            Land = false;
            Rappel = false;
            Paratroop = false;

            // Check if the player is in an aircraft
            if (Game.Player.Character.IsInVehicle() && Game.Player.Character.CurrentVehicle.IsAircraft)
            {
                // If the helicopter's current mission is not "Attack"
                if (helicopter.GetActiveMissionType() != VehicleMissionType.Attack)
                {
                    EnableAttack();
                    // Assign the helicopter to attack the player's vehicle
                    helicopter.Driver.Task.StartHeliMission(
                        helicopter,                  // The helicopter
                        Game.Player.Character.CurrentVehicle, // Target: player's aircraft
                        VehicleMissionType.Attack,          // Mission type: attack
                        PURSUIT_SPEED,                                // Mission radius (keep at a distance of 20 meters)
                        50,                                // Flight altitude
                        -1,                                // Speed (optional)
                        PURSUIT_HEIGHT                           
                    );
                }
            }
            else
            {
                // If the player is not in an aircraft
                if (helicopter.GetActiveMissionType() != VehicleMissionType.Follow)
                {
                    DisableAttack();
                    // Assign the helicopter to follow the player
                    helicopter.Driver.Task.StartHeliMission(
                        helicopter,                  // The helicopter
                        Game.Player.Character, // Target: player's aircraft
                        VehicleMissionType.Follow,         // Mission type: follow
                        PURSUIT_SPEED,                                // Mission radius (keep at a distance of 20 meters)
                        PURSUIT_RADIUS,                                // Flight altitude
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
    private const int JUMP_DELAY = 2000; // milliseconds between jumps

    private bool isAtDeploymentHeight = false;
    private bool isStabilized = false;
    private DateTime? lastJumpTime = null;
    private List<Ped> deployedParatroopers = new List<Ped>();
    public bool AllowCoPilotJump { get; set; } = false; // New flag to control co-pilot jumping
    List<Ped> parachuteStatePeds = new List<Ped>();

    private void ParatrooperDeployment()
    {
        if (!IsHelicopterValid() || !Paratroop)
            return;

        // Step 1: Get to deployment height if not there
        if (!isAtDeploymentHeight)
        {
            helicopter.Driver.BlockPermanentEvents = true;
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

                Print($"Ascending to deployment height: {helicopter.HeightAboveGround:F0}/{PARA_DESIRED_HEIGHT:F0}m");
                return;
            }

            isAtDeploymentHeight = true;
            Print("Reached deployment altitude");
            return;
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

                Print("Stabilizing helicopter for deployment");
                return;
            }

            isStabilized = true;
            Print("Helicopter stabilized - beginning deployment");
            return;
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
            Print("All paratroopers deployed - mission complete");
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

    private bool IsRearSeat(VehicleSeat seat)
    {
        return seat == VehicleSeat.LeftRear || seat == VehicleSeat.RightRear;
    }

    private void DeployParatrooper(Ped paratrooper)
    {
        if (paratrooper == null || !paratrooper.Exists())
            return;

        paratrooper.Task.LeaveVehicle(LeaveVehicleFlags.BailOut);
        Script.Wait(500); // Give time for the leave animation

        if (!paratrooper.IsInVehicle())
        {
            paratrooper.Task.Skydive();
            Print("Paratrooper beginning skydive");
        }
    }
    public bool ControlMountedWeapon(Ped ped)
    {
        return Function.Call<bool>(Hash.CONTROL_MOUNTED_WEAPON, ped);
    }

    public bool SetVehicleWeaponHash(Ped ped, VehicleWeaponHash WeapHash)
    {
        bool nat = Function.Call<bool>(GTA.Native.Hash.SET_CURRENT_PED_VEHICLE_WEAPON, ped, WeapHash);
        return nat;
    }

    public void DisableVehicleWeapon(VehicleWeaponHash weaponHash, Ped ped)
    {
        Function.Call(Hash.DISABLE_VEHICLE_WEAPON, true, weaponHash, helicopter, ped);
    }

    public void EnableVehicleWeapon(VehicleWeaponHash weaponHash, Ped ped)
    {
        ControlMountedWeapon(ped);
        Function.Call(Hash.DISABLE_VEHICLE_WEAPON, false, weaponHash, helicopter, ped);
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
                    else
                    {
                        // Pilot or co-pilot stays in the vehicle, block their events
                        crewMember.BlockPermanentEvents = true;
                        crewMember.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, false);
                    }
                    break;

                case CrewLeaveOption.CoPilotAndCrew:
                    // Co-pilot and crew members leave, but pilot stays
                    if (crewMember.SeatIndex == VehicleSeat.Driver)
                    {
                        // Pilot stays
                        crewMember.BlockPermanentEvents = true;
                    }
                    else
                    {
                        // Co-pilot and crew leave
                        crewMember.Task.LeaveVehicle();
                    }
                    break;

                case CrewLeaveOption.All:
                    // All crew members leave the vehicle
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

    public void LandHelicopter()
    {
        // Ensure valid helicopter and landing flag
        if (!IsHelicopterValid() || helicopter.Driver == null || !Land)
            return;

        if (!hasLanded)
        {
            // Find landing position once
            if (!isLandingPositionFound)
            {
                Notification.PostTicker("Finding suitable landing position...", true);
                potentialLandingPosition = World.GetNextPositionOnStreet(Game.Player.Character.Position.Around(20));

                if (potentialLandingPosition.DistanceTo(Game.Player.Character.Position) > 60)
                {
                    Print("Street position too far - using open area");
                    Rappel = true;
                    Land = false;
                    return;
                }

                helicopter.Driver.BlockPermanentEvents = true;
                isLandingPositionFound = true;

                // Set hover position above landing position
                PositionToReach = potentialLandingPosition + Vector3.WorldUp * 40;
                Notification.PostTicker($"Moving to hover position at height: {PositionToReach.Z}", false);
                helicopter.Driver.Task.StartHeliMission(helicopter, PositionToReach, VehicleMissionType.GoTo, 70, 10, 100, 0);
                return;
            }

            // Check if hover position is reached
            if (!isHoverPositionReached)
            {
                if (helicopter.Position.DistanceTo(PositionToReach) >= 20)
                {
                    Print($"Moving to hover position. Distance remaining: {helicopter.Position.DistanceTo(PositionToReach):F2}");
                    return;
                }

                isHoverPositionReached = true;
                Print($"Reached hover position. Current height: {helicopter.HeightAboveGround}");
            }

            // Start landing sequence
            if (helicopter.GetActiveMissionType() != VehicleMissionType.Land)
            {
                Notification.PostTicker("Initiating landing sequence", true);
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
                Notification.PostTicker("Landing successful - crew disembarking", true);
                LeaveLandedHelicopter(CrewLeaveOption.OnlyCrew);

                // Cooldown-based approach
                if (!hasCooldownStarted)
                {
                    hasCooldownStarted = true;
                    cooldownStartTime = DateTime.Now;
                    Notification.PostTicker("Crew disembarking... waiting 2 seconds", false);
                    return; // Exit to wait for cooldown
                }

                if ((DateTime.Now - cooldownStartTime).TotalSeconds < 2)
                    return; // Wait for 2 seconds

                hasCooldownStarted = false; // Reset cooldown flag
                hasLanded = true;

                // Helicopter departs the scene
                Notification.PostTicker("Helicopter departing the scene", true);
                isDeparting = true;
                LeaveTheScene(Game.Player.Character.Position.Around(500));
                return;
            }

            Print($"Helicopter altitude: {heliHeight}, Ground height: {groundHeight}. Waiting to land...");
        }
        else if (isDeparting)
        {
            Print("Helicopter is departing. Mission complete.");
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
                Print("Helicopter doesn't support rappelling - switching to pursuit mode");
                Rappel = false;
                Pursuit = true;
                return;
            }
            return;
        }

        helicopter.Driver.BlockPermanentEvents = true;

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
            Notification.PostTicker("Moving to rappel position - approaching slowly", false);
            return;
        }

        // Wait until helicopter reaches rappel position
        if (!isRappelPosition)
        {
            if (helicopter.Position.DistanceTo(PositionToReach) >= 20)
            {
                float distanceRatio = helicopter.Position.DistanceTo(PositionToReach) / 50f; // 50f is max distance considered
                float adjustedSpeed = Math.Max(RAPPEL_SPEED * distanceRatio, 5f); // Minimum speed of 5

                helicopter.Driver.Task.StartHeliMission(
               helicopter,
               PositionToReach,
               VehicleMissionType.GoTo,
               adjustedSpeed,
               RAPPEL_RADIUS,
               (int)RAPPEL_HEIGHT,
               (int)RAPPEL_HEIGHT
           );
                Print($"Adjusting approach speed: {adjustedSpeed:F1}");
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
                    5f,              // Tight radius for minimal drift
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
            DateTime? rappelStartTime = null;

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
                    crewMember.CanRagdoll = false;
                    rappelStartTime = DateTime.Now;
                    allCrewOutside = false;
                }
                else if (IsPedRappelingFromHelicopter())
                {
                    allCrewOutside = false;
                }
            }

            // If helicopter is rappelling and all crew are out, mark as complete
            if (!IsPedRappelingFromHelicopter() && allCrewOutside)
            {
                if (rappelStartTime.HasValue && (DateTime.Now - rappelStartTime.Value).TotalSeconds >= 2)
                {
                    foreach (var crewMember in crew.ToList())
                    {
                        crewMember.CanRagdoll = true;
                    }

                    isRappelComplete = true;
                    Notification.PostTicker("All crew members have rappelled successfully", false);

                    LeaveTheScene(PositionToReach);
                    return;
                }
            }

            // If crew members are still rappelling, wait
            if (!allCrewOutside)
            {
                Notification.PostTicker("Crew members are rappelling...", false);
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
        helicopter.Driver.Task.StartHeliMission(helicopter, fleepos + Vector3.WorldUp*FLEE_HEIGHT, VehicleMissionType.Flee, FLEE_SPEED, 10, -1, (int)FLEE_HEIGHT);
    }

    public void EnableHeliWeapons()
    {
        // we give and then allow to use them.
        helicopter.Driver.SetCombatAttribute(CombatAttributes.PreferAirCombatWhenInAircraft, true); //this flag is set when player is in aircraft
        helicopter.Driver.SetCombatAttribute(CombatAttributes.UseVehicleAttackIfVehicleHasMountedGuns, true);
        helicopter.Driver.SetCombatAttribute(CombatAttributes.ForceCheckAttackAngleForMountedGuns, true);
        helicopter.Driver.SetCombatAttribute(CombatAttributes.UseRocketsAgainstVehiclesOnly, true);
        helicopter.Driver.SetConfigFlag(PedConfigFlagToggles.LawWillOnlyAttackIfPlayerIsWanted, true);
    }

    public void DisableHeliWeapons()
    {
        //remove their ability to use them
        helicopter.Driver.SetCombatAttribute(CombatAttributes.PreferAirCombatWhenInAircraft, false); //this flag is set when player is in aircraft
        helicopter.Driver.SetCombatAttribute(CombatAttributes.UseVehicleAttackIfVehicleHasMountedGuns, false);
        helicopter.Driver.SetCombatAttribute(CombatAttributes.ForceCheckAttackAngleForMountedGuns, false);
        helicopter.Driver.SetCombatAttribute(CombatAttributes.UseRocketsAgainstVehiclesOnly, false);
        helicopter.Driver.SetConfigFlag(PedConfigFlagToggles.LawWillOnlyAttackIfPlayerIsWanted, false);
        
    }

    public bool GetVehicleWeaponHash(Ped ped, out VehicleWeaponHash WeapHash)
    {
        OutputArgument arg = new();
        bool nat = Function.Call<bool>(GTA.Native.Hash.GET_CURRENT_PED_VEHICLE_WEAPON, ped, arg);
        WeapHash = arg.GetResult<VehicleWeaponHash>();
        return nat;
    }

    public void EnableAttack()
    {
        EnableHeliWeapons();
        //helicopter.Driver.Task.StartHeliMission(helicopter, helicopter.Driver, Game.Player.Character.Position, VehicleMissionType.Attack, FLEE_SPEED, -1, (int)PURSUIT_HEIGHT);
    }

    public void DisableAttack()
    {
        DisableHeliWeapons();
        //TaskHeliMission(helicopter.Driver, VehicleMissionType.PoliceBehaviour); //use Helicopter.Driver.Task.StartHeliMission();
    }

    public bool IsPedRappelingFromHelicopter()
    {
        return Function.Call<bool>(Hash.IS_ANY_PED_RAPPELLING_FROM_HELI, helicopter);  //is used to check if rappel is happening by ped or not.
    }


    public void LeaveCargobob(Ped ped, VehicleSeat seat)
    {
        if (ped.SeatIndex == seat)
        {
            ped.Task.LeaveVehicle(LeaveVehicleFlags.BailOut);
        }
    }
}

