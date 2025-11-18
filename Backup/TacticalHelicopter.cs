using GTA;
using GTA.Math;
using GTA.Native;
using Guarding.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Vector3 = GTA.Math.Vector3;

//Specific changes for : Guard Backup
public class TacticalHelicopter
{
    public bool Rappel { get; set; }
    public bool Land { get; set; }
    public List<Ped> Crew = new List<Ped>();
    public bool CanRappel { get; private set; }
    public Vehicle Helicopter { get; private set; }
    public Ped Pilot { get; private set; }
    private Vector3 DropZone { get; set; }
    public Task CurrentTask { get; set; } = Task.None;
    private Task PreviousTask { get; set; } = Task.None;

    private DateTime lastStateChange = DateTime.Now;
    private DateTime rappelCooldown = DateTime.MinValue;
    private DateTime landingCooldown;
    private DateTime lastHeightCheck = DateTime.Now;
    private DateTime lastSpeedUpdate = DateTime.Now;
    private DateTime lastPlayerSightCheck = DateTime.Now;
    private DateTime searchStartTime = DateTime.MinValue;
    private float lastRecordedHeight = 0f;

    // Guard configuration for spawning extra rappellers
    private GuardConfig _guardConfig;
    private Area _area;
    private Random _random = new Random();

#pragma warning disable CS0414 // Field is assigned but never used - kept for future enhancements
    private int stuckHeightCounter = 0;
    private bool shouldForceHeightMapAvoidance;
    private bool isPlayerOnFoot = true;
    private bool troopsDeploying = false;
    private bool playerInSight = true;
    private bool isSearching = false;
    private bool missionInterrupted = false;
#pragma warning restore CS0414

    bool createExtra = false;
    private bool crewDeploymentStarted = false; // Add this flag

    public enum CrewLeaveOption
    {
        OnlyCrew,
        CoPilotAndCrew,
        All
    }

    public struct HeliRappelData
    {
        public static readonly CrClipDictionary EntryLeftSeatDictionary = new CrClipDictionary("veh@helicopter@rds@enter_exit");
        public static readonly CrClipDictionary EntryRightSeatDictionary = new CrClipDictionary("veh@helicopter@rps@enter_exit");
        public static readonly CrClipAsset EntryLeftSeatAnimation = new CrClipAsset(EntryLeftSeatDictionary, "get_in_extra");
        public static readonly CrClipAsset EntryRightSeatAnimation = new CrClipAsset(EntryRightSeatDictionary, "get_in_extra");
    }

    public enum Task
    {
        None,
        GoToPosition,
        WaitingForRappel,
        RappelingInProgress,
        RappelComplete,
        StartLanding,
        LandingInProgress,
        CooldownBeforeCrewExit, // New - cooldown phase before crew exits
        LandingComplete,
        Paratrooping,
        ParatroopInProgress,
        ParatroopComplete,
        Flee,
        EmergencyBailout,
        Following,
        Searching,
        Intercepting
    }
    private enum LandingState
    {
        GoToPosition,
        CheckReadyToLand,
        HoverAboveLanding,       // New - approach + hover point above target
        ExecuteLanding,
        LandingInProgress,
        CooldownBeforeCrewExit,  // New - cooldown after touching down
        LandingComplete,
        CrewDeployment
    }

    private enum RappelState
    {
        GoToPosition,
        CheckReadyToRappel,
        PositionForRappel,
        ExecuteRappel,
        RappelInProgress,
        RappelComplete
    }

    public TacticalHelicopter(Vehicle helicopter, Vector3 deployzone, GuardConfig guardConfig = null, Area area = null)
    {
        Helicopter = helicopter ?? throw new ArgumentNullException(nameof(helicopter));
        _guardConfig = guardConfig;
        _area = area;
        Vector3 dropzone = Vector3.Zero;

        helicopter.HeliBladesSpeed = 1f;
        if (Rappel)
            FindLocationForDeployment(deployzone.Around(10), out dropzone, true, false);
        else if (Land)
            FindLocationForDeployment(deployzone.Around(10), out dropzone, false);
        else
            dropzone = deployzone.Around(25f); // or use fallback logic

        DropZone = dropzone;

        Pilot = Helicopter.Driver;
        InitializeHelicopter();
        DetermineInitialTask();
    }
    private void InitializeHelicopter()
    {
        Vector3 testStart = Helicopter.Position;
        Vector3 testEnd = new Vector3(DropZone.X, DropZone.Y, DropZone.Z);

        ShapeTestHandle shapeTest = ShapeTest.StartTestLOSProbe(testEnd, testStart);
        ShapeTestResult result;

        while (shapeTest.GetResult(out result) == ShapeTestStatus.NotReady)
        {
            Script.Yield();
        }

        // Enable extra rappel spawning ONLY for small helicopters (4 seats or less)
        // 6+ seat helicopters have enough capacity, no need for extras
        createExtra = Helicopter.PassengerCapacity <= 4;
        Logger.Log.Info($"TacticalHelicopter: PassengerCapacity={Helicopter.PassengerCapacity}, createExtra={createExtra}");

        shouldForceHeightMapAvoidance = result.DidHit;
        CanRappel = Helicopter.AllowRappel;
        Crew = Helicopter.Passengers.ToList();
        Pilot.SeeingRange = 200f;
        lastRecordedHeight = GetHeightAboveGround();
    }

    private void DetermineInitialTask()
    {

        if (!HasAnyPassengers)
        {
            StartFleeTask();
            return;
        }

        isPlayerOnFoot = !Game.Player.Character.IsInVehicle();
        Pilot.BlockPermanentEvents = true;

        if (Land)
        {
            StartLandingSequence();
        }
        else if (CanRappel)
        {
            StartRappelSequence();
        }

    }
    public void Update()
    {
        if (!IsHelicopterValid())
        {
            return;
        }

        // Check for damage first - highest priority
        if (ShouldFleeFromDamage() || !HasAnyPassengers)
        {
            CurrentTask = Task.None;
            currentLandingState = LandingState.LandingComplete;
            if (currentRappelState != RappelState.RappelInProgress) currentRappelState = RappelState.RappelComplete;
            StartFleeTask();
            return;
        }

        float heightAboveGround = GetHeightAboveGround();


        UpdateHelicopterState();
    }

    private bool ShouldFleeFromDamage()
    {
        float healthPercentage = (float)Helicopter.Health / Helicopter.MaxHealth;
        return healthPercentage < .6f;
    }


    private static readonly CrClipDictionary EntryLeftSeatDictionary = new CrClipDictionary("veh@helicopter@rds@enter_exit");

    private static readonly CrClipDictionary EntryRightSeatDictionary = new CrClipDictionary("veh@helicopter@rps@enter_exit");

    private static readonly CrClipAsset EntryLeftSeatAnimation = new CrClipAsset(EntryLeftSeatDictionary, "get_in_extra");

    private static readonly CrClipAsset EntryRightSeatAnimation = new CrClipAsset(EntryRightSeatDictionary, "get_in_extra");


    // Add these fields to your Properties and Fields section
    private RappelState currentRappelState = RappelState.GoToPosition;
    private LandingState currentLandingState = LandingState.GoToPosition;

    private void UpdateHelicopterState()
    {
        switch (CurrentTask)
        {
            case Task.Flee:

                break;

            default:
                if (Rappel && CanRappel)
                {
                    ProcessRappel();
                }
                else if (Land)
                {
                    ProcessLanding();
                }
                break;
        }
    }


    private DateTime lastRappelAttemptTime = DateTime.MinValue;


    // Main Rappel Processing Method
    private void ProcessRappel()
    {
        float distanceToTarget = GetHorizontalDistanceToDropZone();
        float currentSpeed = Helicopter.Speed;
        float heightAboveGround = GetHeightAboveGround();

        //HelperClass.Subtitle($"Rappel State: {currentRappelState} | Dist: {distanceToTarget:F1}m | Speed: {currentSpeed:F1}mph | Height: {heightAboveGround:F1}m");

        switch (currentRappelState)
        {
            case RappelState.GoToPosition:
                Pilot.Task.StartHeliMission(Helicopter, DropZone, VehicleMissionType.GoTo, 75f, -1, 20, (int)20, -1, -1, HeliMissionFlags.HeightMapOnlyAvoidance);
                currentRappelState = RappelState.PositionForRappel;
                break;

            case RappelState.PositionForRappel:

                if (distanceToTarget <= 40f)
                {
                    //Pilot.Task.StartHeliMission(Helicopter, DropZone, VehicleMissionType.GoTo, 20, 0, 15, (int)15f, -1, -1, HeliMissionFlags.None);
                    Pilot.BlockPermanentEvents = true;
                    currentRappelState = RappelState.CheckReadyToRappel;
                }
                break;


            case RappelState.CheckReadyToRappel:
                // First, ensure the helicopter is horizontally above the drop zone.
                if (distanceToTarget <= 20f)
                {
                    // Define the acceptable altitude range for rappelling.
                    float minRappelHeight = 15f;
                    float maxRappelHeight = 40f;

                    float actualHeight = GetHeightAboveGround();

                    // CHECK 1: Is the helicopter within the ideal altitude range?
                    if (actualHeight >= minRappelHeight && actualHeight <= maxRappelHeight)
                    {
                        // --- CONDITION MET: Altitude is correct. ---
                        // The helicopter is in the sweet spot. Command it to stop and hover.
                        Pilot.Task.StartHeliMission(
                            Helicopter,
                            DropZone, // The point to hover over
                            VehicleMissionType.Stop,
                            0, -1, 20, 20
                        );

                        // Prepare the crew for combat while hovering.
                        foreach (var crew in Crew)
                        {
                            crew.Task.CombatHatedTargetsAroundPed(300, TaskCombatFlags.None);
                        }

                        // Move to the next state to begin the rappel.
                        currentRappelState = RappelState.ExecuteRappel;
                    }
                    // CHECK 2: Is the helicopter TOO LOW?
                    else if (actualHeight < minRappelHeight)
                    {
                        // --- CONDITION NOT MET: Helicopter must ASCEND. ---
                        // Command the pilot to fly to the drop zone at the MINIMUM safe height.
                        Pilot.Task.StartHeliMission(
                            Helicopter,
                            DropZone,
                            VehicleMissionType.GoTo,
                            30f, // Speed
                            0f,  // Heading
                            20,  // Radius
                            (int)minRappelHeight // Target Altitude
                        );
                        // We stay in this state to give the helicopter time to ascend.
                    }
                    // CHECK 3: Is the helicopter TOO HIGH?
                    else // This covers the case where (actualHeight > maxRappelHeight)
                    {
                        // --- CONDITION NOT MET: Helicopter must DESCEND. ---
                        // Command the pilot to fly to the drop zone at the MAXIMUM safe height.
                        Pilot.Task.StartHeliMission(
                            Helicopter,
                            DropZone,
                            VehicleMissionType.GoTo,
                            30f, // Speed
                            -1, 
                            30, 
                            (int)maxRappelHeight // Target Altitude
                        );
                        // We stay in this state to give the helicopter time to descend.
                    }
                }
                else
                {
                    // If we are not horizontally close, go back to the state for moving into position.
                    currentRappelState = RappelState.GoToPosition;
                }
                break;


            //rappeling guys
            case RappelState.ExecuteRappel:

                foreach (Ped crew in Helicopter.Passengers)
                {
                    if (crew.SeatIndex == VehicleSeat.Driver || crew.SeatIndex == VehicleSeat.RightFront) { continue; }

                    if (crew.IsInVehicle(Helicopter))
                    {
                        // Setup combat attributes BEFORE rappelling
                        crew.KeepTaskWhenMarkedAsNoLongerNeeded = true;
                        crew.BlockPermanentEvents = false; // Allow them to react to combat
                        crew.SetCombatAttribute(CombatAttributes.AlwaysFight, true);
                        //crew.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, true);

                        //TaskSequence ts = new TaskSequence();
                        ////Function.Call(Hash.TASK_SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, 0, true);
                        crew.Task.RappelFromHelicopter(); // Use rappel flag
                                                          //Function.Call(Hash.TASK_SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, 0, false);
                                                          //ts.AddTask.CombatHatedTargetsAroundPed(300, TaskCombatFlags.None);
                                                          //ts.Close();

                        crew.Task.ClearAll();
                        // crew.Task.PerformSequence(ts);
                        //.Dispose();
                        //
                        Logger.Log.Info($"TacticalHeli: Assigned rappel + combat sequence to crew member in seat {crew.SeatIndex}");
                    }
                }

                currentRappelState = RappelState.RappelInProgress;
                TransitionToTask(Task.RappelingInProgress);
                troopsDeploying = true;
                break;

            //checking case if rappeling is in process or no... 
            case RappelState.RappelInProgress:
                bool allDeployed = true;

                // First check if any existing crew members are still in the helicopter
                foreach (Ped crew in Helicopter.Passengers)
                {
                    if (!crew.Exists() || crew.IsDead) continue;
                    if (crew.SeatIndex == VehicleSeat.Driver || crew.SeatIndex == VehicleSeat.RightFront) continue;

                    if (crew.IsInVehicle(Helicopter))
                    {
                        // Check if rappel task failed and needs to be restarted
                        var taskStatus = crew.GetScriptTaskStatus(ScriptTaskNameHash.RappelFromHeli);
                        if (taskStatus != ScriptTaskStatus.Performing && taskStatus != ScriptTaskStatus.WaitingToStart)
                        {
                            currentRappelState = RappelState.ExecuteRappel;
                            TransitionToTask(Task.WaitingForRappel);
                            return; // Exit early to restart rappel
                        }
                        allDeployed = false;
                    }
                }

                // Handle additional rappellers if helicopter is actively rappelling
                if (!Helicopter.IsPedRappelingFromHelicopter())
                {
                    if (allDeployed)
                    {
                        currentRappelState = RappelState.RappelComplete;
                        rappelCooldown = DateTime.Now;
                        troopsDeploying = false; // Mark deployment as complete
                    }
                }
                break;


            case RappelState.RappelComplete:
                Logger.Log.Info("TacticalHeli: Rappel complete, starting flee task");
                troopsDeploying = false;
                StartFleeTask();
                EntryLeftSeatDictionary.MarkAsNoLongerNeeded();
                EntryRightSeatDictionary.MarkAsNoLongerNeeded();
                TransitionToTask(Task.Flee);
                Logger.Log.Info($"TacticalHeli: CurrentTask set to {CurrentTask}");
                break;
        }
    }

    private float GetHorizontalDistanceToDropZone()
    {
        return Helicopter.Position.DistanceTo2D(DropZone);
    }

#pragma warning disable CS0169 // Field is never used - kept for future landing logic enhancements
    Vector3 PositionToReach;
#pragma warning restore CS0169

    // Main Landing Processing Method
    private void ProcessLanding()
    {
        float distanceToTarget = Helicopter.Position.DistanceTo2D(DropZone);
        float currentSpeed = Helicopter.Speed;
        float heightAboveGround = GetHeightAboveGround();

        //    HelperClass.Subtitle($"Landing State: {currentLandingState} | Dist: {distanceToTarget:F1}m | Speed: {currentSpeed:F1}mph | Height: {heightAboveGround:F1}m");

        switch (currentLandingState)
        {
            case LandingState.GoToPosition:
                // Send helicopter to position with good speed
                Pilot.Task.StartHeliMission(Helicopter, DropZone, VehicleMissionType.GoTo, 70, 30, -1, 30, -1, 50, HeliMissionFlags.HeightMapOnlyAvoidance);
                currentLandingState = LandingState.CheckReadyToLand;
                break;

            case LandingState.CheckReadyToLand:
                // Check if we're close enough to start landing approach
                if (distanceToTarget < 60f) // Increased speed threshold
                {
                    Pilot.Task.StartHeliMission(Helicopter, DropZone, VehicleMissionType.GoTo, 20, -1, -1, 30, -1, -1, HeliMissionFlags.None);
                    Pilot.BlockPermanentEvents = true;
                    currentLandingState = LandingState.HoverAboveLanding;
                }
                break;

            case LandingState.HoverAboveLanding:
                // Slow approach to landing zone
                if (distanceToTarget < 25f)
                {
                    Pilot.Task.StartHeliMission(Helicopter, DropZone, VehicleMissionType.GoTo, 10, -1, -1, 30);
                    currentLandingState = LandingState.ExecuteLanding;
                }
                else
                {
                    currentLandingState = LandingState.CheckReadyToLand;
                }
                break;

            case LandingState.ExecuteLanding:
                // Execute the actual landing
                if (Helicopter.GetActiveMissionType() != VehicleMissionType.Land && Helicopter.Model.IsHelicopter) Pilot.Task.StartHeliMission(Helicopter, DropZone, VehicleMissionType.Land, 15f, 20, 0, 0);

                if (Helicopter.GetActiveMissionType() != VehicleMissionType.Land && Helicopter.Model.IsPlane) Pilot.Task.LandPlane(DropZone, DropZone);
                currentLandingState = LandingState.LandingInProgress;
                TransitionToTask(Task.LandingInProgress);
                break;

            case LandingState.LandingInProgress:
                CheckLandingStuck();
                if (IsHelicopterLanded())
                {
                    //      HelperClass.Subtitle("Helicopter successfully landed");
                    currentLandingState = LandingState.LandingComplete;
                    TransitionToTask(Task.LandingComplete);
                }
                break;

            case LandingState.LandingComplete:
                currentLandingState = LandingState.CrewDeployment;
                landingCooldown = DateTime.Now;
                troopsDeploying = true;
                crewDeploymentStarted = false; // Reset deployment flag
                break;

            case LandingState.CrewDeployment:
                // Start crew deployment process
                if (!crewDeploymentStarted)
                {
                    if (Pilot.IsDead)
                    {
                        Logger.Log.Info("TacticalHeli: Pilot dead - deploying copilot and crew");
                        DeployCrew(CrewLeaveOption.CoPilotAndCrew);
                    }
                    else if (Helicopter.Health <= 500)
                    {
                        Logger.Log.Info("TacticalHeli: Helicopter damaged - deploying all crew");
                        DeployCrew(CrewLeaveOption.All);
                        Pilot.BlockPermanentEvents = false;
                    }
                    else
                    {
                        Logger.Log.Info("TacticalHeli: Normal deployment - deploying crew only");
                        DeployCrew(CrewLeaveOption.OnlyCrew);
                    }

                    crewDeploymentStarted = true;
                    landingCooldown = DateTime.Now; // Start cooldown timer
                }

                // Wait for cooldown before checking if all exited
                TimeSpan timeSinceDeployment = DateTime.Now - landingCooldown;
                if (timeSinceDeployment.TotalSeconds < 3) // Give 3 seconds before checking
                {
                    break;
                }

                // Check if crew has exited
                bool allCrewExited = CheckAllCrewExited(CrewLeaveOption.OnlyCrew);
                if (allCrewExited)
                {
                    Logger.Log.Info("TacticalHeli: All crew exited - starting flee");
                    StartFleeTask();
                }
                else
                {
                    // Force crew to exit if they're stuck after 10 seconds
                    if (timeSinceDeployment.TotalSeconds > 10)
                    {
                        Logger.Log.Info("TacticalHeli: Crew stuck - forcing exit");
                        ForceCrewExit(CrewLeaveOption.OnlyCrew);
                        StartFleeTask();
                    }
                }
                break;
        }
    }
    int Interval = 5000;
    int Timer = 0;


    private void CheckLandingStuck()
    {
        float currentHeight = GetHeightAboveGround();

        // Wait for interval before rechecking
        if (Game.GameTime < Timer) return;
        Timer = Game.GameTime + Interval;

        var speed = Helicopter.Speed;
        // Helicopter is suspiciously low, but not landed 

        //use the time counter as well
        if (currentHeight >= 2 && currentHeight < 25f && speed < 0.3f)
        {
            if (!CanRappel)
            {
                //     HelperClass.Subtitle("Helicopter stuck below 5m – but rappel unsupported. Fleeing.");
                TransitionToTask(Task.Flee);
                return;
            }

            //   HelperClass.Subtitle("Landing stuck – switching to rappel mode (with height correction)");

            // Update mode
            Land = false;
            Rappel = true;
            troopsDeploying = false;

            // Ascend first
            Vector3 safeAscendPosition = Helicopter.Position + Vector3.WorldUp * 30f;

            Pilot.Task.StartHeliMission(
                Helicopter,
                safeAscendPosition,
                VehicleMissionType.GoTo,
                50f, 5f, -1, 40
            );

            // Begin proper rappel flow
            currentRappelState = RappelState.CheckReadyToRappel;
            CurrentTask = Task.GoToPosition;
            TransitionToTask(Task.GoToPosition);
        }
    }

    private void StartRappelSequence()
    {
        TransitionToTask(Task.GoToPosition);
        Pilot.Task.StartHeliMission(Helicopter, DropZone, VehicleMissionType.GoTo, 100f, 0f, -1, 20);
    }

    private void StartLandingSequence()
    {
        TransitionToTask(Task.GoToPosition);
        Pilot.Task.StartHeliMission(Helicopter, DropZone, VehicleMissionType.GoTo, 100f, 0f, -1, 20);
    }

    private bool IsHelicopterLanded()
    {
        return GetHeightAboveGround() < 2f;
    }

    private float GetHeightAboveGround()
    {
        OutputArgument groundZ = new OutputArgument();
        Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD,
            Helicopter.Position.X, Helicopter.Position.Y, Helicopter.Position.Z, groundZ);

        return Helicopter.Position.Z - groundZ.GetResult<float>();
    }

    // CLASS VARIABLES
    private DateTime _cargobobTimer = DateTime.MinValue;
    private DateTime _cargoDoorOpenedAt = DateTime.MinValue;

#pragma warning disable CS0414 // Field is assigned but never used - kept for future cargo helicopter features
    private bool _cargoTroopsSpawned = false;
#pragma warning restore CS0414

    private bool _cargoDoorOpen = false;

    private const int CARGO_DOOR_INDEX = 2; // rear-left ramp
    private const int CARGO_TIMEOUT_SEC = 10;


    // Updated DeployCrew method with better exit logic
    public void DeployCrew(CrewLeaveOption leaveOption)
    {
        Logger.Log.Info($"TacticalHeli: DeployCrew called with option {leaveOption}");

        foreach (var p in Helicopter.Passengers)
        {
            if (p == null || !p.Exists() || p.IsDead) continue;

            bool shouldLeave = false;

            switch (leaveOption)
            {
                case CrewLeaveOption.OnlyCrew:
                    if (p.SeatIndex != VehicleSeat.Driver && p.SeatIndex != VehicleSeat.RightFront)
                    {
                        shouldLeave = true;
                    }
                    break;

                case CrewLeaveOption.CoPilotAndCrew:
                    if (p.SeatIndex != VehicleSeat.Driver)
                    {
                        shouldLeave = true;
                    }
                    break;

                case CrewLeaveOption.All:
                    shouldLeave = true;
                    break;
            }

            if (shouldLeave && p.IsInVehicle(Helicopter))
            {
                // Setup combat attributes BEFORE leaving
                p.KeepTaskWhenMarkedAsNoLongerNeeded = true;
                p.BlockPermanentEvents = false;
                p.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, true);
                p.SetCombatAttribute(CombatAttributes.AlwaysFight, true);

                // Clear any blocking tasks and leave
                p.Task.ClearAll();
                p.Task.LeaveVehicle(LeaveVehicleFlags.None);

                Logger.Log.Info($"TacticalHeli: Ordered ped in seat {p.SeatIndex} to leave vehicle");
            }
        }
    }

    // New method to force crew exit if stuck
    private void ForceCrewExit(CrewLeaveOption leaveOption)
    {
        Logger.Log.Info($"TacticalHeli: ForceCrewExit called with option {leaveOption}");

        foreach (var p in Helicopter.Passengers)
        {
            if (p == null || !p.Exists() || p.IsDead) continue;

            bool shouldLeave = false;

            switch (leaveOption)
            {
                case CrewLeaveOption.OnlyCrew:
                    if (p.SeatIndex != VehicleSeat.Driver && p.SeatIndex != VehicleSeat.RightFront)
                    {
                        shouldLeave = true;
                    }
                    break;

                case CrewLeaveOption.CoPilotAndCrew:
                    if (p.SeatIndex != VehicleSeat.Driver)
                    {
                        shouldLeave = true;
                    }
                    break;

                case CrewLeaveOption.All:
                    shouldLeave = true;
                    break;
            }

            if (shouldLeave && p.IsInVehicle(Helicopter))
            {
                // Force warp out if still in vehicle after timeout
                p.Task.ClearAllImmediately();
                Function.Call(Hash.TASK_LEAVE_VEHICLE, p, Helicopter, (int)LeaveVehicleFlags.WarpOut);

                // Setup combat after warping out
                p.KeepTaskWhenMarkedAsNoLongerNeeded = true;
                p.BlockPermanentEvents = false;
                p.SetCombatAttribute(CombatAttributes.AlwaysFight, true);

                Logger.Log.Info($"TacticalHeli: Force warped ped in seat {p.SeatIndex} out of vehicle");
            }
        }
    }

    // Updated CheckAllCrewExited - now only checks, doesn't deploy
    private bool CheckAllCrewExited(CrewLeaveOption crew)
    {
        int crewStillInside = 0;

        foreach (var p in Helicopter.Passengers)
        {
            if (p == null || !p.Exists() || p.IsDead) continue;

            // Skip pilot and copilot for OnlyCrew option
            if (crew == CrewLeaveOption.OnlyCrew)
            {
                if (p.SeatIndex == VehicleSeat.Driver || p.SeatIndex == VehicleSeat.RightFront)
                    continue;
            }

            // Skip pilot for CoPilotAndCrew option
            if (crew == CrewLeaveOption.CoPilotAndCrew)
            {
                if (p.SeatIndex == VehicleSeat.Driver)
                    continue;
            }

            if (p.IsInVehicle(Helicopter))
            {
                crewStillInside++;
                Logger.Log.Info($"TacticalHeli: Crew member still in vehicle - Seat: {p.SeatIndex}");
            }
        }

        bool allExited = crewStillInside == 0;

        if (Helicopter.Model.IsCargobob && allExited)
        {
            // Start cooldown when first detected
            if (_cargobobTimer == DateTime.MinValue)
            {
                _cargobobTimer = DateTime.UtcNow;
                Logger.Log.Info("TacticalHeli: Starting Cargobob door close cooldown");
            }

            bool timeout = (DateTime.UtcNow - _cargobobTimer).TotalSeconds >= CARGO_TIMEOUT_SEC;
            if (!timeout)
            {
                Logger.Log.Info($"TacticalHeli: Waiting for Cargobob cooldown - {(DateTime.UtcNow - _cargobobTimer).TotalSeconds:F1}s elapsed");
                return false;
            }

            // After cooldown — CLOSE ramp door
            if (_cargoDoorOpen)
            {
                Function.Call(Hash.SET_VEHICLE_DOOR_SHUT, Helicopter, CARGO_DOOR_INDEX, false);
                _cargoDoorOpen = false;
                Logger.Log.Info("TacticalHeli: Closed Cargobob door");
            }
        }
        else
        {
            _cargobobTimer = DateTime.MinValue; // reset
        }

        Logger.Log.Info($"TacticalHeli: CheckAllCrewExited returning {allExited} (crew inside: {crewStillInside})");
        return allExited;
    }


    private void TransitionToTask(Task newTask)
    {
        if (CurrentTask != newTask)
        {
            CurrentTask = newTask;
            lastStateChange = DateTime.Now;
        }
    }

    private void StartFleeTask()
    {
        TransitionToTask(Task.Flee);
        troopsDeploying = false;

        if (!IsHelicopterValid()) return;

        Helicopter.IsSirenActive = false;
        Pilot.BlockPermanentEvents = true;

        Vector3 fleeTarget = Helicopter.Position;
        if (Helicopter.GetActiveMissionType() != VehicleMissionType.Flee && Helicopter.Model.IsHelicopter) Pilot.Task.StartHeliMission(Helicopter, fleeTarget + Vector3.WorldEast * 40 + Vector3.WorldUp * 60, VehicleMissionType.Flee, 120f, -1, 90, 100);
        if (Helicopter.GetActiveMissionType() != VehicleMissionType.Flee && Helicopter.Model.IsPlane) Pilot.Task.StartPlaneMission(Helicopter, fleeTarget, VehicleMissionType.Flee, 80, -1, -1, 80);
        //HelperClass.Subtitle("Helicopter fleeing the area");
    }

    public bool HasAnyPassengers
    {
        get
        {
            if (Crew == null || Crew.Count == 0) return false;

            foreach (Ped soldier in Crew)
            {
                if (soldier == null || !soldier.Exists() || soldier.IsDead)
                    continue;

                if (soldier.IsInVehicle(Helicopter))
                    return true;
            }

            return false;
        }
    }


    public bool IsHelicopterValid()
    {
        return Helicopter != null &&
               Helicopter.Exists() &&
               !Helicopter.IsDead &&
               Pilot != null &&
               Pilot.Exists() &&
               !Pilot.IsDead;
    }

    public bool ApplyWeaponAmmo(VehicleWeaponHash weaponHash, int numammo)
    {
        if (!IsHelicopterValid()) return false;

        Helicopter.SetWeaponRestrictedAmmo((int)weaponHash, numammo);
        return true;
    }

    public static bool FindLocationForDeployment(Vector3 targetPosition, out Vector3 deploymentPosition, bool pedPath, bool doHeightFix = true)
    {
        deploymentPosition = Vector3.Zero;

        // Try up to 10 times to find a valid position
        for (int attempt = 0; attempt < 10; attempt++)
        {
            // First: Try finding position on street
            deploymentPosition = World.GetNextPositionOnStreet(targetPosition, unoccupied: true);
            if (deploymentPosition != Vector3.Zero)
            {
                if (doHeightFix)
                    GroundZFix(ref deploymentPosition);
                return true;
            }

            // Second: Try safe ped path
            if (pedPath && World.GetSafePositionForPed(targetPosition.Around(20f), out deploymentPosition,
                GetSafePositionFlags.NotIsolated | GetSafePositionFlags.NotInterior | GetSafePositionFlags.NotWater))
            {
                if (doHeightFix)
                    GroundZFix(ref deploymentPosition);
                return true;
            }

            // Third: Try custom random position with ground check
            Vector3 randomVec = targetPosition.Around(GetRandomFloat(40.0f, 50.0f));
            OutputArgument zArg = new OutputArgument();
            if (Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, randomVec.X, randomVec.Y, randomVec.Z + 100f, zArg))
            {
                randomVec.Z = zArg.GetResult<float>();
                deploymentPosition = randomVec;
                return true;
            }
        }

        // Final fallback after 10 failed attempts
        deploymentPosition = targetPosition.Around(45f);
        if (doHeightFix)
            GroundZFix(ref deploymentPosition);

        return false; // Clearly mark that nothing ideal was found
    }


    private static void GroundZFix(ref Vector3 position)
    {
        OutputArgument groundZ = new OutputArgument();
        bool foundGround = Function.Call<bool>(
            Hash.GET_GROUND_Z_FOR_3D_COORD,
            position.X, position.Y, position.Z + 100f,
            groundZ
        );

        position.Z = foundGround ? groundZ.GetResult<float>() : position.Z - 1.5f;
    }


    public static float GetRandomFloat(double min, double max)
    {
        return (float)(new Random().NextDouble() * (max - min) + min);
    }

    public static double GetDouble()
    {
        return new Random().NextDouble();
    }

    public static bool GetBool()
    {
        return GetDouble() >= 0.5;
    }
}
