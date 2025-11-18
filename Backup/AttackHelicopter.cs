using GTA;
using GTA.Math;
using GTA.Native;
using Guarding.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class AttackHelicopter
{
    // --- Configuration Constants ---
    private const float DETECTION_RADIUS = 300f;
    private const float ATTACK_DISTANCE = 70f;
    private const float PATROL_DISTANCE = 50f;

    // Heights for different operations
    private const int ATTACK_HEIGHT = 50;
    private const int PATROL_HEIGHT = 30;
    private const int SEARCH_HEIGHT = 80;
    private const int FLEE_HEIGHT = 80;

    // Speeds for different operations
    private const float ATTACK_SPEED = 100;
    private const float PATROL_SPEED = 110;
    private const float SEARCH_SPEED = 60;
    private const float FLEE_SPEED = 100;

    // Update intervals
    private const double STATE_UPDATE_INTERVAL = 0.5;
    private const double MISSION_CHECK_INTERVAL = 2.0;

    // Health and damage thresholds
    private const float CRITICAL_HEALTH_THRESHOLD = 0.6f;
    private const float INTENSE_CRITICAL_HEALTH_THRESHOLD = 0.3f;
    private const float CRASH_HEIGHT_THRESHOLD = 5f;
    private const float ENGAGEMENT_RANGE = 200f;

    public enum HelicopterState
    {
        Idle,
        ReadyToInitial,
        GoToInitial,
        ReadyToEngage,
        Engage,
        ReadyToFlee,
        Flee,
    }

    // --- Properties ---
    public Vehicle Helicopter { get; }
    public Ped Pilot { get; }
    public HelicopterState CurrentState { get;  set; } = HelicopterState.Idle;
    public bool IsArmed { get; private set; } = false;
    public bool HasArmedPassengers { get; private set; } = false;
    public Vector3 LastKnownTargetPosition { get; private set; }
    public float SearchRadius { get; set; } = 250f;
    public bool IsAnnihilatorType = false;

    private DateTime _lastStateUpdate = DateTime.MinValue;
    public List<Ped> Crew = new List<Ped>();
    private Random _random = new Random();
    private VehicleMissionType _currentMissionType = VehicleMissionType.None;
    private List<Vector3> _searchPoints = new List<Vector3>();
    private Ped _targetToDefend; //to defend with HeliProtect or any other mission.
    Ped _targetToAttack; //toattack with Attack mission.

    private GuardConfig guardConfig; // NEW - store faction info
    private Area sourceArea; // NEW - know which area spawned this
    private RelationshipGroup factionRelationshipGroup; // NEW - know our faction


    // --- Constructor ---
    public AttackHelicopter(Vehicle helicopter, GuardConfig config, Area area)
    {
        Helicopter = helicopter ?? throw new ArgumentNullException(nameof(helicopter));
        guardConfig = config;
        sourceArea = area;
        factionRelationshipGroup = Helicopter.Driver.RelationshipGroup;

        if (Game.Player?.Character == null)
            throw new InvalidOperationException("Player character not available");

        Pilot = Helicopter.Driver;
        helicopter.HeliBladesSpeed = 1f;

        if (Pilot == null || !Pilot.Exists())
        {
            Logger.Log.Error("AttackHelicopter: Pilot is null or doesn't exist!");
            throw new InvalidOperationException("Helicopter has no pilot");
        }

        // Initialize around combat location not player
        Vector3 combatPos = sourceArea.GetCentroid();
        LastKnownTargetPosition = GetOrbitPositionAroundPlayer(combatPos);

        try
        {
            InitializeHelicopter();
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"AttackHelicopter: Initialization failed: {ex.Message}");
            throw;
        }
    }

    public void DismissTeam()
    {
        CurrentState = HelicopterState.Flee;
    }
    
    /// <summary>
    /// Get a position orbiting around the player at a safe distance
    /// </summary>
    private Vector3 GetOrbitPositionAroundPlayer(Vector3 playerPos)
    {
        // Random angle for orbit
        float angle = (float)(_random.NextDouble() * Math.PI * 2);
        float distance = 5 + (float)(_random.NextDouble() * 10); //  from player
        
        Vector3 orbitPos = new Vector3(
            playerPos.X + (float)Math.Cos(angle) * distance,
            playerPos.Y + (float)Math.Sin(angle) * distance,
            playerPos.Z + ATTACK_HEIGHT // Use attack height above player
        );
        
        return orbitPos;
    }

    // --- Initialization ---
    private void InitializeHelicopter()
    {
        if (!IsHelicopterValid()) return;

        DetermineWeaponCapabilities();
        CurrentState = HelicopterState.ReadyToInitial;
        Crew = Helicopter.Passengers.ToList();

        // Enhanced helicopter setup
        Helicopter.SetFoldingWingsDeployed(true);
        Helicopter.SetArriveDistanceOverrideForVehiclePersuitAttack(50);
        Helicopter.CountermeasureAmmoCount = 9999;
        if (Helicopter.HasBombBay) Helicopter.BombAmmoCount = 9999;
        // Improved crew setup
        SetupCrew();
    }

    private void SetupCrew()
    {
        foreach (var crewMember in Crew)
        {
            if (crewMember == null || !crewMember.Exists()) continue;

            crewMember.CanSwitchWeapons = true;
            crewMember.SeeingRange = ENGAGEMENT_RANGE;
            crewMember.HearingRange = ENGAGEMENT_RANGE;
            //crewMember.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, true);
            crewMember.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, false); // Initially prevent leaving
            //crewMember.BlockPermanentEvents = true;

        }
    }

    private void DetermineWeaponCapabilities()
    {
        if (!IsHelicopterValid()) return;

        // Check pilot weapon capabilities
        IsArmed = Pilot.GetVehicleWeaponHash(out var weaponHash);

        // Enhanced weapon setup
        if (IsArmed)
        {
            //Pilot.CanSwitchWeapons = true;
            Pilot.SetPedCycleVehicleWeapon();

            // Set appropriate combat attributes for armed helicopters
            Pilot.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true); // Will be enabled when 
            Pilot.SetConfigFlag(PedConfigFlagToggles.CanAttackFriendly, false);
            Pilot.FiringPattern = FiringPattern.FullAuto;
            Pilot.ShootRate = 800;
            Pilot.Accuracy = 50;
        }

        // Check if should flee immediately
        if (!HasAnyPassengers && !IsArmed)
        {
            CurrentState = HelicopterState.Flee;
        }

        Pilot.SeeingRange = ENGAGEMENT_RANGE;
    }

    float heightAboveGround;
    // --- Main Update Loop ---

    public void Update()
    {
        if (!IsHelicopterValid()) return;

        heightAboveGround = GetHeightAboveGround();
        
        // Update target position to orbit around player instead of going to exact position
        Vector3 playerPos = GetTarget.Position;
        LastKnownTargetPosition = GetOrbitPositionAroundPlayer(playerPos);
        
        UpdateHelicopterBehavior();

    }

    private float GetHeightAboveGround()
    {
        OutputArgument groundZ = new OutputArgument();
        Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD,
            Helicopter.Position.X, Helicopter.Position.Y, Helicopter.Position.Z, groundZ);

        return Helicopter.Position.Z - groundZ.GetResult<float>();
    }

    private Ped GetTarget
    {
        get { return _targetToDefend ?? Game.Player.Character; }
    }

    // --- Main State Management ---
    private void UpdateHelicopterBehavior()
    {
        // Critical override conditions - but not during initial approach
        if (ShouldFleeFromDamage() || (!HasAnyPassengers && !IsArmed))
        {
            CurrentState = HelicopterState.ReadyToFlee;
        }

        float distanceToTarget = Helicopter.Position.DistanceTo(LastKnownTargetPosition);
        float currentSpeed = Helicopter.Speed;

        if (CriticalHealth())
        {
            CurrentState = HelicopterState.Idle;
            if (Helicopter.HeightAboveGround < 2 && !Pilot.IsOnFoot)
            {
                Pilot.Task.LeaveVehicle();
                Pilot.BlockPermanentEvents = false;
                Pilot.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, true);
                foreach (var p in Helicopter.Passengers)
                {
                    p.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, true);
                    p.Task.LeaveVehicle();
                }
            }
        }
        
        // Execute the state machine
        ExecuteCurrentState(distanceToTarget);
    }

       

    private void ExecuteCurrentState(float distanceToTarget)
    {
        // State machine logic within the same method
        switch (CurrentState)
        {
            case HelicopterState.ReadyToInitial:
                // Execute state action
                Helicopter.IsSirenActive = true;
                DisableWeapons();

                // Go to orbit position around player (not directly to player)
                if (Helicopter.Model.IsHelicopter) Pilot.Task.StartHeliMission(
                    Helicopter,
                    LastKnownTargetPosition, // This is now an orbit position around player
                    VehicleMissionType.GoTo,
                    PATROL_SPEED,
                    30,
                    -1,
                    PATROL_HEIGHT
                );

                if (Helicopter.Model.IsPlane) Pilot.Task.StartPlaneMission(Helicopter, LastKnownTargetPosition, VehicleMissionType.GoTo, 100, 30, -1, 70, -1, true);

                // if(Helicopter.)
                _currentMissionType = VehicleMissionType.GoTo;

                // State transition
                CurrentState = HelicopterState.GoToInitial;
                break;

            case HelicopterState.GoToInitial:
                // State transition based on conditions - check distance to player not orbit point
                float distanceToPlayer = Helicopter.Position.DistanceTo(GetTarget.Position);
                if (distanceToPlayer < 100f) // Within 100m of player
                {
                    //EnableWeapons();
                    CurrentState = HelicopterState.ReadyToEngage;
                }
                break;

            case HelicopterState.ReadyToEngage:
                // Execute state action
                if (IsArmed)
                {
                    EnableWeapons();
                }

                VehicleMissionType engageMissionType;
                Ped missionTarget;
                float speed, distance;
                int height;

                var enemyList = FindNearbyEnemy(); // Faction-aware enemies

                if (IsArmed)
                {
                    // Armed helicopter: Prioritize ATTACK mode
                    if (enemyList != null && enemyList.Length > 0)
                    {
                        // ATTACK MODE - enemies found
                        engageMissionType = VehicleMissionType.Attack;
                        speed = 100;
                        distance = 60;
                        height = 50;

                        // Assign primary target
                        _targetToAttack = enemyList[_random.Next(0, enemyList.Length)];
                        missionTarget = _targetToAttack;

                        Logger.Log.Info($"AttackHelicopter: ATTACK mode - targeting {enemyList.Length} hostiles");

                        // Assign combat tasks to passengers
                        AssignCombatTasksToPassengers(enemyList);
                    }
                    else
                    {
                        // DEFEND MODE - no enemies, find ally to protect
                        Ped allyToDefend = FindFriendlyToDefend();

                        if (allyToDefend != null)
                        {
                            engageMissionType = VehicleMissionType.HeliProtect;
                            speed = 80;
                            distance = 30;
                            height = 30;
                            missionTarget = allyToDefend;
                            _targetToDefend = allyToDefend;
                            _targetToAttack = null;

                            Logger.Log.Info($"AttackHelicopter: DEFEND mode - protecting ally {allyToDefend.Handle}");
                        }
                        else
                        {
                            // No enemies, no allies - patrol area
                            engageMissionType = VehicleMissionType.GoTo;
                            speed = 80;
                            distance = 20;
                            height = 40;
                            missionTarget = null;

                            // Orbit around area centroid
                            LastKnownTargetPosition = GetOrbitPositionAroundPlayer(sourceArea.GetCentroid());

                            Logger.Log.Info($"AttackHelicopter: PATROL mode - no targets in area");
                        }
                    }
                }
                else
                {
                    // Unarmed helicopter: ALWAYS defend allies
                    Ped allyToDefend = FindFriendlyToDefend();

                    if (allyToDefend != null)
                    {
                        engageMissionType = VehicleMissionType.HeliProtect;
                        speed = 100;
                        distance = 5;
                        height = 60;
                        missionTarget = allyToDefend;
                        _targetToDefend = allyToDefend;

                        // Passengers still shoot at enemies
                        if (enemyList != null && enemyList.Length > 0)
                        {
                            AssignCombatTasksToPassengers(enemyList);
                        }

                        Logger.Log.Info($"AttackHelicopter: Unarmed DEFEND mode - protecting ally {allyToDefend.Handle}");
                    }
                    else
                    {
                        // No ally found - patrol
                        engageMissionType = VehicleMissionType.Circle;
                        speed = 100;
                        distance = 30;
                        height = 40;
                        missionTarget = null;
                        LastKnownTargetPosition = GetOrbitPositionAroundPlayer(sourceArea.GetCentroid());
                    }
                }

                // Task the pilot with mission
                if (Helicopter.Model.IsHelicopter)
                {
                    if (missionTarget != null)
                    {
                        // Target the vehicle if ped is in one
                        if (missionTarget.IsSittingInVehicle())
                        {
                            Pilot.Task.StartHeliMission(Helicopter, missionTarget.CurrentVehicle, engageMissionType, speed, distance, -1, height);
                        }
                        else
                        {
                            Pilot.Task.StartHeliMission(Helicopter, missionTarget, engageMissionType, speed, distance, -1, height);
                        }
                    }
                    else
                    {
                        // No target - go to position
                        Pilot.Task.StartHeliMission(Helicopter, LastKnownTargetPosition, VehicleMissionType.GoTo, speed, distance, -1, height);
                    }
                }

                _currentMissionType = engageMissionType;
                CurrentState = HelicopterState.Engage;
                break;


            case HelicopterState.Engage:
                // Check if mission is still active or if the primary target is gone, restart if needed
                if (Helicopter.GetActiveMissionType() == VehicleMissionType.None || (_targetToAttack != null && (_targetToAttack.IsDead || !_targetToAttack.Exists())))
                {
                    CurrentState = HelicopterState.ReadyToEngage;
                    return;
                }

                // For Attack mission: Continuously check for new targets if the current one is dealt with
                if (IsArmed && _currentMissionType == VehicleMissionType.Attack)
                {
                    // Check if current attack target is dead or invalid
                    if (_targetToAttack == null || !_targetToAttack.Exists() || _targetToAttack.IsDead)
                    {
                        Logger.Log.Info("AttackHelicopter: Current attack target is dead/invalid, searching for new enemy");

                        // Just transition back to ReadyToEngage. It will handle finding new enemies
                        // and re-tasking the pilot AND the passengers cleanly.
                        CurrentState = HelicopterState.ReadyToEngage;
                        return;
                    }
                }

                // No additional action needed - helicopter is protecting/attacking
                // The Pilot's mission and Passengers' combat tasks are now active.
                break;

            case HelicopterState.ReadyToFlee:
                // Execute state action
                DisableWeapons();
                Helicopter.IsSirenActive = false;
                Pilot.BlockPermanentEvents = true;

                if (Helicopter.Model.IsHelicopter) Pilot.Task.StartHeliMission(
                    Helicopter,
                    LastKnownTargetPosition,
                    VehicleMissionType.Flee,
                    FLEE_SPEED,
                    0,
                    -1,
                    FLEE_HEIGHT
                );

                if (Helicopter.Model.IsPlane) Pilot.Task.StartPlaneMission(Helicopter, LastKnownTargetPosition, VehicleMissionType.Flee, 70, -1, -1, 90);
                _currentMissionType = VehicleMissionType.Flee;

                // State transition
                CurrentState = HelicopterState.Flee;
                break;

            case HelicopterState.Flee:
                // State transition
                CurrentState = HelicopterState.Idle;
                break;


            case HelicopterState.Idle:
            default:
                // No action required for idle state
                break;
        }
    }

    /// <summary>
    /// Assigns a random combat target to each passenger in the helicopter.
    /// </summary>
    /// <param name="enemies">The array of enemy peds to choose from.</param>
    private void AssignCombatTasksToPassengers(Ped[] enemies)
    {
        // Ensure there are enemies to target and the helicopter exists
        if (enemies == null || enemies.Length == 0 || !Helicopter.Exists())
        {
            return;
        }

        // Get all occupants of the helicopter
        foreach (Ped passenger in Helicopter.Occupants)
        {
            // Skip the pilot, as they are flying the helicopter
            if (passenger == Pilot)
            {
                continue;
            }

            // Assign a random enemy from the list to this passenger
            Ped targetForPassenger = enemies[_random.Next(0, enemies.Length)];

            // Task the passenger to fight the target
            // The game engine will handle the aiming and shooting from inside the vehicle
            passenger.Task.Combat(targetForPassenger);
        }
    }
    private bool ShouldFleeFromDamage()
    {
        if (!IsHelicopterValid()) return true;

        float healthPercentage = (float)Helicopter.Health / Helicopter.MaxHealth;
        return healthPercentage < CRITICAL_HEALTH_THRESHOLD;
    }

    private bool CriticalHealth()
    {
        if (!IsHelicopterValid()) return true;

        float healthPercentage = (float)Helicopter.Health / Helicopter.MaxHealth;
        return healthPercentage < INTENSE_CRITICAL_HEALTH_THRESHOLD;
    }

    // --- Enhanced Weapon Management ---
    private void DisableWeapons()
    {
        if (!IsHelicopterValid()) return;

        foreach (var crewMember in Crew)
        {
            if (crewMember == null || !crewMember.Exists()) continue;

            crewMember.SetCombatAttribute(CombatAttributes.UseVehicleAttackIfVehicleHasMountedGuns, false);
            crewMember.SetCombatAttribute(CombatAttributes.UseVehicleAttack, false);
        }
    }

    private void EnableWeapons()
    {
        if (!IsHelicopterValid()) return;

        foreach (var crewMember in Crew)
        {
            if (crewMember == null || !crewMember.Exists()) continue;

            crewMember.SetCombatAttribute(CombatAttributes.UseVehicleAttackIfVehicleHasMountedGuns, true);
            crewMember.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true);

        }
    }

    // --- Utility Methods ---

    /// <summary>
    /// Find nearby enemy to attack. Searches for hostile peds near the player.
    /// </summary>
    private Ped[] FindNearbyEnemy()
    {
        List<Ped> hostileList = new List<Ped>();

        try
        {
            Vector3 searchCenter = sourceArea.GetCentroid();

            // Search for nearby peds within engagement range
            Ped[] nearbyPeds = World.GetNearbyPeds(searchCenter, 90);

            if (nearbyPeds == null || nearbyPeds.Length == 0)
                return null;

            // Get our faction's relationship group
             var ourRelationshipGroup = factionRelationshipGroup;

            foreach (Ped ped in nearbyPeds)
            {
                if (ped == null || !ped.Exists() || ped.IsDead)
                    continue;

                // Skip our own crew
                if (Crew.Contains(ped) || ped == Pilot)
                    continue;

                // Check relationship between our faction and this ped's faction
                Relationship relationship = ourRelationshipGroup.GetRelationshipBetweenGroups(ped.RelationshipGroup);

                // Target hostile factions
                if (relationship == Relationship.Hate || relationship == Relationship.Dislike)
                {
                    hostileList.Add(ped);
                    Logger.Log.Info($"AttackHelicopter: Found hostile target - Faction: {ped.RelationshipGroup} vs Our Faction: {factionRelationshipGroup}");
                }

                // Also check if ped is actively in combat against our allies
                if (ped.IsInCombat)
                {
                    var pedTarget = ped.CombatTarget as Ped;
                    if (pedTarget != null && pedTarget.Exists())
                    {
                        // Check if target is our ally
                        Relationship pedTargetRelationship = ourRelationshipGroup.GetRelationshipBetweenGroups(pedTarget.RelationshipGroup);

                        if (pedTargetRelationship == Relationship.Respect || pedTargetRelationship == Relationship.Like || pedTargetRelationship == Relationship.Companion)
                        {
                            hostileList.Add(ped);
                            Logger.Log.Info($"AttackHelicopter: Found enemy attacking our ally - targeting {ped.Handle}");
                        }
                    }
                }
            }

            return hostileList.ToArray();
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"AttackHelicopter: Error finding enemies: {ex.Message}");
            return null;
        }
    }
    /// <summary>
    /// Find a friendly guard from our faction to defend
    /// Prioritize: Alive guards in combat > Alive guards > Any alive allied ped
    /// </summary>
    private Ped FindFriendlyToDefend()
    {
        try
        {
            Vector3 searchCenter = sourceArea.GetCentroid();
            Ped[] nearbyPeds = World.GetNearbyPeds(searchCenter, 90);

            if (nearbyPeds == null || nearbyPeds.Length == 0)
                return null;

            var ourRelationshipGroup = factionRelationshipGroup;

            List<Ped> alliesInCombat = new List<Ped>();
            List<Ped> alliesAlive = new List<Ped>();

            foreach (Ped ped in nearbyPeds)
            {
                if (ped == null || !ped.Exists() || ped.IsDead)
                    continue;

                // Skip crew
                if (Crew.Contains(ped) || ped == Pilot)
                    continue;

                // Check if same faction
                Relationship relationship = ourRelationshipGroup.GetRelationshipBetweenGroups(ped.RelationshipGroup);

                if (relationship == Relationship.Companion || relationship == Relationship.Respect || relationship == Relationship.Like)
                {
                    if (ped.IsInCombat)
                    {
                        alliesInCombat.Add(ped);
                    }
                    else
                    {
                        alliesAlive.Add(ped);
                    }
                }
            }

            // Prioritize allies in combat
            if (alliesInCombat.Count > 0)
            {
                return alliesInCombat[_random.Next(0, alliesInCombat.Count)];
            }

            // Otherwise any alive ally
            if (alliesAlive.Count > 0)
            {
                return alliesAlive[_random.Next(0, alliesAlive.Count)];
            }

            return null;
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"AttackHelicopter: Error finding friendly: {ex.Message}");
            return null;
        }
    }

    public bool IsHelicopterValid()
    {
        return Helicopter != null && Helicopter.Exists() && !Helicopter.IsDead &&
               Pilot != null && Pilot.Exists() && !Pilot.IsDead;
    }

    //while ignoring the pilotseats
    public bool HasAnyPassengers
    {
        get
        {
            if (Crew == null || Crew.Count == 0) return false;

            foreach (Ped soldier in Crew)
            {
                if (soldier == null || !soldier.Exists() || soldier.IsDead || soldier.SeatIndex == VehicleSeat.LeftFront)
                    continue;

                if (soldier.IsInVehicle(Helicopter))
                    return true;
            }

            return false;
        }
    }

    // --- Enhanced Weapon Setup Methods ---

    private Ped GetEntityToAttack()
    {
        //get a list of hostile entities around player or the guards itself ??
        return Helicopter.Driver.CombatTarget;
    }

    public bool ApplyWeaponAmmo(VehicleWeaponHash weaponHash, int ammoCount)
    {
        if (!IsHelicopterValid()) return false;

        try
        {
            Helicopter.SetWeaponRestrictedAmmo((int)weaponHash, ammoCount);
            return true;
        }
        catch
        {
            return false;
        }
    }
}