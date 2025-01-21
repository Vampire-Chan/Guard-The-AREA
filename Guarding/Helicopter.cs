using GTA;
using GTA.Math;
using System;
using GTA.Native;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using GTA.UI;

public class Helicopter
{
    private Vehicle helicopter;
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

    private const float HOVER_HEIGHT = 10f;
    private const float DEPLOYMENT_HEIGHT = 150f;

    // Add these constants at the top of the Helicopter class
    private const float PURSUIT_SPEED = 70f;
    private const float LANDING_SPEED = 30f;
    private const float RAPPEL_SPEED = 20f;
    private const float FLEE_SPEED = 90f;

    private const float PURSUIT_RADIUS = 30f;
    private const float LANDING_RADIUS = 10f;
    private const float RAPPEL_RADIUS = 15f;

    private const float PURSUIT_HEIGHT = 40f;
    private const float LANDING_HEIGHT = 20f;
    private const float RAPPEL_HEIGHT = 40f;
    private const float FLEE_HEIGHT = 50f;


    public Vehicle Vehicle
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

    public bool IsArmed { get; private set; }

    // Constructor for unarmed/transport helicopter
    public Helicopter(string heliModel, string pilotModel, List<string> crewModels, List<string> primaryWeapons, List<string> secondaryWeapons)
    {

        Initialize(heliModel, pilotModel, crewModels, null, primaryWeapons, secondaryWeapons);
    }

    // Constructor for armed helicopter
    public Helicopter(string heliModel, string pilotModel, List<string> crewModels,
        Dictionary<VehicleWeaponHash, string> weapons, List<string> primaryWeapons, List<string> secondaryWeapons)
    {
        Initialize(heliModel, pilotModel, crewModels, weapons, primaryWeapons, secondaryWeapons);
    }

    // Constructor for existing helicopter
    public Helicopter(Vehicle existingHeli, List<string> primaryWeapons, List<string> secondaryWeapons)
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

    private void Initialize(
    string heliModel,
    string pilotModel,
    List<string> crewModels,
    Dictionary<VehicleWeaponHash, string> vehicleWeapons = null,
    List<string> primaryWeapons = null,
    List<string> secondaryWeapons = null)
    {
        // Validate helicopter model
        var heliModelInstance = new Model(heliModel);
        heliModelInstance.Request(1000);

        if (!heliModelInstance.IsValid || !heliModelInstance.IsInCdImage)
            throw new ArgumentException($"Invalid helicopter model: {heliModel}");

        // Create helicopter above the player's position
        helicopter = World.CreateVehicle(heliModelInstance, Game.Player.Character.Position.Around(100) + new Vector3(0, 0, 50f));
       
        CanRappel = helicopter.AllowRappel;
        GTA.UI.Screen.ShowSubtitle("Helicopter Spwned!");
        // Cleanup model
        //heliModelInstance.MarkAsNoLongerNeeded();

        // Initialize crew list
        crew = new List<Ped>();

        // Create and assign pilot
        var pilot = CreateAndAssignPed(pilotModel, helicopter, VehicleSeat.Driver);
        crew.Add(pilot);
        Print("Pilot Spawned");
        // Assign passengers (limit to available seats)
        //var maxPassengers = Math.Min(crewModels.Count, helicopter.PassengerCapacity);
        for (int i = 0; i < helicopter.PassengerCapacity; i++)
        {
            var crewPed = CreateAndAssignPed(crewModels[new Random().Next(0, crewModels.Count)], helicopter, (VehicleSeat)(i));
            crew.Add(crewPed);
            
        }

        helicopter.IsEngineRunning = true;
        helicopter.HeliBladesSpeed = 1f;
        // Initialize vehicle weapons (specific to the helicopter)
        IsArmed = vehicleWeapons?.Count > 0;
        weaponStates = new Dictionary<VehicleWeaponHash, bool>();

        if (IsArmed)
        {
            foreach (var weapon in vehicleWeapons)
            {
                //weaponStates[weapon.Key] = true;
               // EnableVehicleWeapon(weapon.Key, pilot);
            }
        }

        // Randomly assign weapons to the crew
        AssignRandomWeapons(crew, primaryWeapons, secondaryWeapons);
    }

    private Ped CreateAndAssignPed(string pedModelName, Vehicle vehicle, VehicleSeat seat)
    {
        var pedModel = new Model(pedModelName);
        pedModel.Request(1000);

        if (!pedModel.IsValid || !pedModel.IsInCdImage)
            throw new ArgumentException($"Invalid ped model: {pedModelName}");

        var ped = helicopter.CreatePedOnSeat(seat, pedModel);
       // ped.(vehicle, seat);
        //pedModel.MarkAsNoLongerNeeded();

        return ped;
    }

    private void AssignRandomWeapons(List<Ped> crewMembers, List<string> primaryWeapons, List<string> secondaryWeapons)
    {
        var random = new Random();

        foreach (var crewMember in crewMembers)
        {
            // Assign a random primary weapon
            if (primaryWeapons != null && primaryWeapons.Count > 0)
            {
                var primaryWeaponHash = GetValidWeaponHash(primaryWeapons[random.Next(primaryWeapons.Count)]);
                if (primaryWeaponHash.HasValue)
                {
                    Function.Call(Hash.GIVE_WEAPON_TO_PED, crewMember, primaryWeaponHash.Value, 9999, false, true);
                }
            }

            // Assign a random secondary weapon
            if (secondaryWeapons != null && secondaryWeapons.Count > 0)
            {
                var secondaryWeaponHash = GetValidWeaponHash(secondaryWeapons[random.Next(secondaryWeapons.Count)]);
                if (secondaryWeaponHash.HasValue)
                {
                    Function.Call(Hash.GIVE_WEAPON_TO_PED, crewMember, secondaryWeaponHash.Value, 9999, false, false);
                }
            }
        }
    }

    private uint? GetValidWeaponHash(string weaponHashString)
    {
       
            return StringHash.AtStringHash(weaponHashString); // Valid hash
        

    }
    private bool IsHelicopterValid()
    {
        return helicopter != null                     // Ensure the helicopter object exists
            && helicopter.Exists()                    // Check if the helicopter exists in the game world
            && !helicopter.IsDead                     // Verify the helicopter is not destroyed
            && helicopter.Driver != null              // Ensure there is a driver
            && helicopter.Driver.Exists()             // Check if the driver exists in the game world
            && !helicopter.Driver.IsDead;             // Verify the driver is not dead
    }


    public void UpdateProcess()
    {
        if (!IsHelicopterValid())
            return;

        ParatrooperDeployment();
        RappelPeds();
        LandHelicopter();
        HandlePursuit();
    }

    private void HandlePursuit()
    {
        if (Pursuit)
        {
            if (Game.Player.Character.IsInVehicle() && Game.Player.Character.CurrentVehicle.IsAircraft)
            {
                if (helicopter.GetActiveMissionType() != VehicleMissionType.Attack)
                {
                    TaskHeliMission(Game.Player.Character, VehicleMissionType.Attack);
                }
            }
            else
            {
                if (helicopter.GetActiveMissionType() != VehicleMissionType.Follow)
                    TaskHeliMission(Game.Player.Character, VehicleMissionType.Follow);
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
        var rearOccupants = helicopter.Occupants.Where(p => IsRearSeat(p.SeatIndex)).ToList();
        var nonRearOccupants = helicopter.Occupants.Where(p =>
            p != helicopter.Driver &&
            !IsRearSeat(p.SeatIndex) &&
            !deployedParatroopers.Contains(p) &&
            (AllowCoPilotJump || p.SeatIndex != VehicleSeat.RightFront)) // Only include co-pilot if allowed
            .ToList();

        // If rear seats are empty and we have more troops, move them to rear
        if (rearOccupants.Count == 0 && nonRearOccupants.Any())
        {
            foreach (var seat in new[] { VehicleSeat.LeftRear, VehicleSeat.RightRear })
            {
                if (helicopter.IsSeatFree(seat) && nonRearOccupants.Any())
                {
                    var pedToMove = nonRearOccupants.First();
                    pedToMove.SetIntoVehicle(helicopter, seat);
                    Print($"Moving trooper to {seat}");
                    return;
                }
            }
        }

        // Step 4: Deploy paratroopers from rear seats
        if (!lastJumpTime.HasValue || (DateTime.Now - lastJumpTime.Value).TotalMilliseconds >= JUMP_DELAY)
        {
            foreach (var paratrooper in rearOccupants)
            {
                if (!deployedParatroopers.Contains(paratrooper))
                {
                    // Deploy paratrooper
                    DeployParatrooper(paratrooper);
                    deployedParatroopers.Add(paratrooper);
                    lastJumpTime = DateTime.Now;

                    // Set target landing zone
                    Vector3 landingZone = Game.Player.Character.Position.Around(20);
                    paratrooper.Task.ParachuteTo(landingZone);

                    Print($"Paratrooper deployed - {GetRemainingTroopsCount()} remaining");
                    return;
                }
            }
        }

        // Check if deployment is complete
        if (IsDeploymentComplete())
        {
            Print("All paratroopers deployed - mission complete");
            LeaveTheScene(Game.Player.Character.Position.Around(500));
            Paratroop = false;
            return;
        }
    }

    private int GetRemainingTroopsCount()
    {
        // Count remaining troops (excluding pilot and potentially co-pilot)
        return helicopter.Occupants.Count() - 1 - (AllowCoPilotJump ? 0 : 1);
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
        Function.Call(Hash.DISABLE_VEHICLE_WEAPON, false, weaponHash, helicopter, ped);
    }

    public bool IsHelicopterLanding()
    {
        VehicleMissionType type = Function.Call<VehicleMissionType>(Hash.GET_ACTIVE_VEHICLE_MISSION_TYPE, helicopter);
        return (type == VehicleMissionType.Land);
    }

    public VehicleMissionType GetVehicleMissionType()
    {
        return Function.Call<VehicleMissionType>(Hash.GET_ACTIVE_VEHICLE_MISSION_TYPE, helicopter);
    }
    public bool GetTaskStatus(int status)
    {
        return Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, helicopter.Driver, status);
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
            Print("Helicopter is not valid or rappelling is not enabled");
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
            }

            // If helicopter is rappelling and all crew are out, mark as complete
            if (!IsPedRappelingFromHelicopter() && allCrewOutside)
            {
                isRappelComplete = true;
                Notification.PostTicker("All crew members have rappelled successfully", false);

                LeaveTheScene(PositionToReach);
                return;
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
        helicopter.Driver.Task.StartHeliMission(helicopter, fleepos + Vector3.WorldUp*50, VehicleMissionType.Flee, 90, 10, -1, 50);
    }

    public void EnableHeliWeapons()
    {
        // we give and then allow to use them.
        helicopter.Driver.SetCombatAttribute(CombatAttributes.PreferAirCombatWhenInAircraft, true); //this flag is set when player is in aircraft
        helicopter.Driver.SetCombatAttribute(CombatAttributes.UseVehicleAttackIfVehicleHasMountedGuns, true);
        helicopter.Driver.SetCombatAttribute(CombatAttributes.ForceCheckAttackAngleForMountedGuns, true);
        helicopter.Driver.SetCombatAttribute(CombatAttributes.UseRocketsAgainstVehiclesOnly, false);
    }

    public void DisableHeliWeapons()
    {
        //remove their ability to use them
        helicopter.Driver.SetCombatAttribute(CombatAttributes.PreferAirCombatWhenInAircraft, false); //this flag is set when player is in aircraft
        helicopter.Driver.SetCombatAttribute(CombatAttributes.UseVehicleAttackIfVehicleHasMountedGuns, false);
        helicopter.Driver.SetCombatAttribute(CombatAttributes.ForceCheckAttackAngleForMountedGuns, false);
        helicopter.Driver.SetCombatAttribute(CombatAttributes.UseRocketsAgainstVehiclesOnly, false);
    }

    public bool GetVehicleWeaponHash(Ped ped, out VehicleWeaponHash WeapHash)
    {
        OutputArgument arg = new();
        bool nat = Function.Call<bool>(GTA.Native.Hash.GET_CURRENT_PED_VEHICLE_WEAPON, ped, arg);
        WeapHash = arg.GetResult<VehicleWeaponHash>();
        return nat;
    }


    public void GotoPosition(Vector3 pos)
    {
        TaskHeliMission(pos, VehicleMissionType.GoTo);
    }

    public void Follow(Ped ped)
    {
        TaskHeliMission(ped, VehicleMissionType.Follow);
    }

    public void EnableAttack()
    {
        EnableHeliWeapons();
        TaskHeliMission(helicopter.Driver, VehicleMissionType.Attack);
    }

    public void DisableAttack()
    {
        DisableHeliWeapons();
        TaskHeliMission(helicopter.Driver, VehicleMissionType.PoliceBehaviour);
    }

    public bool IsPedRappelingFromHelicopter()
    {
        return Function.Call<bool>(Hash.IS_ANY_PED_RAPPELLING_FROM_HELI, helicopter);  //is used to check if rappel is happening by ped or not.
    }

    public void TaskHeliMission(Vector3 position, VehicleMissionType type) //go position
    {
        helicopter.Driver.Task.StartHeliMission(Vehicle, position, type, 30, 20, -1, 30);
    }

    public void TaskHeliMission(Ped ped, VehicleMissionType type) //chase ped, if chase vehicle then use othermethod
    {
        helicopter.Driver.Task.StartHeliMission(Vehicle, ped, type, 30, 20, -1, 30);
    }

    public void LeaveCargobob(Ped ped, VehicleSeat seat)
    {
        if (ped.SeatIndex == seat)
        {
            ped.Task.LeaveVehicle(LeaveVehicleFlags.BailOut);
        }
    }
}


public class HeliManager : Script
{
    private List<Helicopter> helicopters;
    private Dictionary<int, int> maxHelisPerWantedLevel; // Wanted level -> Max helis allowed
    private Random random;

    // Lists for random models and weapons
    private List<string> heliModels = new List<string>
    {
        "polmav", "buzzard2", "annihilator2", "valkyrie2", "buzzard", "maverick", "frogger", "annihilator", "savage"
    };

    private List<string> pilotModels = new List<string>
    {
        "s_m_y_marine_01", "s_m_y_marine_02", "s_m_y_marine_03"
    };

    private List<string> crewModelsList = new List<string>
    {
        "s_m_y_swat_01", "s_m_y_cop_01", "s_m_m_snowcop_01",
        "s_m_y_sheriff_01", "s_m_y_ranger_01", "s_m_m_security_01"
    };

    private List<string> primaryWeaponsList = new List<string>
    {
        "WEAPON_CARBINERIFLE", "WEAPON_SPECIALCARBINE",
        "WEAPON_ADVANCEDRIFLE", "WEAPON_BULLPUPRIFLE"
    };

    private List<string> secondaryWeaponsList = new List<string>
    {
        "WEAPON_COMBATPISTOL", "WEAPON_PISTOL50",
        "WEAPON_HEAVYPISTOL", "WEAPON_APPISTOL"
    };

    private Dictionary<VehicleWeaponHash, string> vehicleWeaponsList;
    private DateTime lastDispatchCheck = DateTime.MinValue;
    private const int DISPATCH_CHECK_INTERVAL = 10000; // 10 seconds

    public HeliManager()
    {
        helicopters = new List<Helicopter>();
        random = new Random();

        // Initialize max helicopters per wanted level
        maxHelisPerWantedLevel = new Dictionary<int, int>
        {
            { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 0 },
            { 4, 1 }, // 1 helicopter at wanted level 4
            { 5, 3 }  // 3 helicopters at wanted level 5
        };

        vehicleWeaponsList = new Dictionary<VehicleWeaponHash, string>
        {
            { VehicleWeaponHash.PlayerBuzzard, "Vehicle Weapon Heli Minigun" },
            { VehicleWeaponHash.PlayerLaser, "Vehicle Weapon Buzzard" },
            { VehicleWeaponHash.Tank, "Vehicle Weapon Tank" }
        };

        Tick += OnTick;
        //KeyDown += OnKeyDown;
        Interval = 1000; // Update every second
    }

    private void OnTick(object sender, EventArgs e)
    {
       // ManageHelicopterDispatch();
       // UpdateExistingHelicopters();
    }

    private void ManageHelicopterDispatch()
    {
        // Only check periodically to avoid constant checks
        if ((DateTime.Now - lastDispatchCheck).TotalMilliseconds < DISPATCH_CHECK_INTERVAL)
            return;

        lastDispatchCheck = DateTime.Now;

        int currentWantedLevel = Game.Player.WantedLevel;
        int maxHelis = maxHelisPerWantedLevel.ContainsKey(currentWantedLevel) ?
                      maxHelisPerWantedLevel[currentWantedLevel] : 0;

        // Clean up destroyed or invalid helicopters
        helicopters.RemoveAll(h => h.Vehicle == null || !h.Vehicle.Exists() || h.Vehicle.IsDead);

        // Spawn new helicopters if needed
        while (helicopters.Count < maxHelis)
        {
            SpawnPoliceHelicopter();
        }

        // Remove excess helicopters if wanted level decreased
        while (helicopters.Count > maxHelis)
        {
            int lastIndex = helicopters.Count - 1;
            if (helicopters[lastIndex].Vehicle != null && helicopters[lastIndex].Vehicle.Exists())
            {
                helicopters[lastIndex].Vehicle.Delete();
            }
            helicopters.RemoveAt(lastIndex);
        }
    }

    private void UpdateExistingHelicopters()
    {
        foreach (var heli in helicopters)
        {
            if (!heli.IsArmed && random.NextDouble() < 0.001) // 0.1% chance to change behavior each update
            {
                AssignRandomBehavior(heli);
            }
            heli.UpdateProcess();
        }
    }

    private void AssignRandomBehavior(Helicopter heli)
    {
        // Reset all behaviors
        heli.Pursuit = false;
        heli.Rappel = false;
        heli.Land = false;
        heli.Paratroop = false;

        // Randomly assign new behavior
        switch (random.Next(4))
        {
            case 0:
                heli.Pursuit = true;
                GTA.UI.Notification.Show("~r~Police Helicopter: ~w~Pursuing target");
                break;
            case 1:
                heli.Rappel = true;
                GTA.UI.Notification.Show("~r~Police Helicopter: ~w~Deploying SWAT team");
                break;
            case 2:
                heli.Land = true;
                GTA.UI.Notification.Show("~r~Police Helicopter: ~w~Landing to deploy units");
                break;
            case 3:
                heli.Paratroop = true;
                GTA.UI.Notification.Show("~r~Police Helicopter: ~w~Paratrooper deployment");
                break;
        }
    }

    private void SpawnPoliceHelicopter()
    {
        // Random model selection
        string heliModel = heliModels[random.Next(heliModels.Count)];
        string pilotModel = pilotModels[random.Next(pilotModels.Count)];

        // Create random crew models list
        List<string> selectedCrewModels = new List<string>();
        for (int i = 0; i < 4; i++) // Assuming 4 crew members
        {
            selectedCrewModels.Add(crewModelsList[random.Next(crewModelsList.Count)]);
        }

        // Random weapons selection
        List<string> selectedPrimaryWeapons = primaryWeaponsList
            .OrderBy(x => random.Next())
            .Take(2)
            .ToList();

        List<string> selectedSecondaryWeapons = secondaryWeaponsList
            .OrderBy(x => random.Next())
            .Take(2)
            .ToList();

        // Create helicopter with random chance of being armed
        Helicopter newHelicopter;
        if (random.NextDouble() < 0.3) // 30% chance of being armed
        {
            newHelicopter = new Helicopter(
                heliModel,
                pilotModel,
                selectedCrewModels,
                selectedPrimaryWeapons,
                selectedSecondaryWeapons
            );
        }
        else
        {
            newHelicopter = new Helicopter(
                heliModel,
                pilotModel,
                selectedCrewModels,
                selectedPrimaryWeapons,
                selectedSecondaryWeapons
            );
        }

        // Assign random initial behavior
        AssignRandomBehavior(newHelicopter);

        helicopters.Add(newHelicopter);
    }



    public class HeliManagers : Script
    {
        private List<Helicopter> helicopters;
        private List<string> primaryWeapons;
        private List<string> secondaryWeapons;
        private List<string> crewModels;
        private Dictionary<VehicleWeaponHash, string> vehicleWeapons;

        public HeliManagers()
        {
            helicopters = new List<Helicopter>();
            primaryWeapons = new List<string> { "WEAPON_CARBINERIFLE" }; // Example primary weapon
            secondaryWeapons = new List<string> { "WEAPON_COMBATPISTOL" }; // Example secondary weapon
            crewModels = new List<string> { "s_m_y_swat_01", "s_m_y_swat_01", "s_m_y_swat_01" }; // Example crew models
            vehicleWeapons = new Dictionary<VehicleWeaponHash, string>
        {
            { VehicleWeaponHash.PlayerBuzzard, "Vehicle Weapon Heli Minigun" } // Example vehicle weapon
        };

            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        private void OnTick(object sender, EventArgs e)
        {
            //foreach (var heli in helicopters)
            //{
            //    heli.UpdateProcess();
            // }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.L)
            {
                //    SpawnHelicopter();
            }
            if (e.KeyCode == Keys.R)
            {
            //    CleanupEntities();
            }
        }

        private void SpawnHelicopter()
        {
            string heliModel = "annihilator2"; // Example helicopter model
            string pilotModel = "s_m_y_cop_01"; // Example pilot model

            // Create and initialize a new helicopter
            Helicopter newHelicopter = new Helicopter(heliModel, pilotModel, crewModels, vehicleWeapons, primaryWeapons, secondaryWeapons);
            helicopters.Add(newHelicopter);
            newHelicopter.Land = true;

            //    newHelicopter.GotoPosition(Game.Player.Character.Position);
        }
        private void CleanupEntities()
        {
            try
            {
                GTA.UI.Screen.ShowSubtitle("Starting cleanup...", 2000);

                // Clean vehicles (and their occupants)
                foreach (Vehicle vehicle in World.GetAllVehicles())
                {
                    if (vehicle != null && vehicle.Exists())
                    {
                        foreach (Ped occupant in vehicle.Occupants)
                        {
                            if (occupant != null && !occupant.IsPlayer)
                                occupant.Delete();
                        }
                        vehicle.Delete();
                    }
                }

                // Clean peds
                foreach (Ped ped in World.GetAllPeds())
                {
                    if (ped != null && ped.Exists() && !ped.IsPlayer)
                        ped.Delete();
                }

                // Clean props
                foreach (Prop prop in World.GetAllProps())
                {
                    if (prop != null && prop.Exists())
                        prop.Delete();
                }

                GTA.UI.Screen.ShowSubtitle("Cleanup complete!", 2000);
            }
            catch (Exception ex)
            {
                GTA.UI.Screen.ShowSubtitle($"Cleanup error: {ex.Message}", 3000);
            }
        }
    }
}