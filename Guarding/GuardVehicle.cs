using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using Guarding.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

// Type alias to resolve ambiguity between GTA.VehicleType and Guarding.Core.Enums.VehicleType
using GuardVehicleType = Guarding.Core.Enums.VehicleType;

public class GuardVehicle
{
    public Vector3 Position { get; set; }
    public float Heading { get; set; }
    public string AreaName { get; set; }
    public GuardVehicleType Type { get; set; }
    public bool Interior { get; set; }
    public Vehicle guardVehicle;
    public Ped guardPedOnVehicle; // For mounted vehicles with gunners

    private readonly Area Area;
    private readonly GuardConfig GuardConfig;
    public static readonly Random _random = new Random();

    // Vehicle model names for different types
    private string VehicleModelName;
    private string MVehicleModelName; // Mounted vehicle
    private string PVehicleModelName; // Plane
    private string HVehicleModelName; // Helicopter
    private string LVehicleModelName; // Large vehicle
    private string BVehicleModelName; // Boat

    // State management
    public VehicleState CurrentState { get; set; } = VehicleState.Idle;

    // Guard assignments
    public List<GuardPed> AssignedPeds { get; private set; } = new List<GuardPed>();
    public List<GuardPed> AssignedGuards { get; private set; } = new List<GuardPed>();

    public void AssignPed(GuardPed ped)
    {
        if (!AssignedPeds.Contains(ped))
        {
            AssignedPeds.Add(ped);
            ped.AssignedVehicle = this; // Create a two-way link
        }
    }
    public bool AllAssignedPedsBoarded()
    {
        // Performance: Use Count > 0 instead of .Any()
        if (AssignedPeds.Count == 0) return false; // Can't be boarded if no one is assigned

        foreach (var ped in AssignedPeds)
        {
            // If any assigned ped is not valid or not in this specific vehicle, return false.
            if (ped.guardPed == null || !ped.guardPed.Exists() || !ped.guardPed.IsInVehicle(this.guardVehicle))
            {
                return false;
            }
        }
        return true; // Everyone is on board
    }

    public bool AllAssignedPedsReachedVehicle()
    {
        // Performance: Use Count == 0 instead of !.Any()
        if (AssignedPeds.Count == 0) return false;

        foreach (var ped in AssignedPeds)
        {
            if (ped.guardPed == null || !ped.guardPed.Exists())
                return false;
            
            float distance = ped.guardPed.Position.DistanceTo(this.guardVehicle.Position);
            if (distance > 6f) // Still too far from vehicle
                return false;
        }
        return true; // Everyone has reached the vehicle
    }

    public int GetBoardedPedCount()
    {
        return AssignedPeds.Count(ped => 
            ped.guardPed != null && 
            ped.guardPed.Exists() && 
            ped.guardPed.IsInVehicle(this.guardVehicle));
    }
    public GuardPed DriverGuard { get; set; }
    public int MaxCapacity { get; private set; }
    
    // Blip management
    public Blip VehicleBlip { get; private set; }
    
    // Arrival deployment tracking
    public Vector3 TargetDeploymentPoint { get; set; } = Vector3.Zero;
    public List<GuardPed> GuardsToDeployOnArrival { get; set; } = new List<GuardPed>();
    public Dictionary<GuardPed, GuardSpawnPoint> GuardSpawnAssignments { get; set; } = new Dictionary<GuardPed, GuardSpawnPoint>();

    public GuardVehicle(GuardSpawnPoint point, GuardConfig guardConfig, Area area)
    {
        Position = point.Position;
        Heading = point.Heading;
        AreaName = area.Name ?? throw new ArgumentNullException(nameof(area.Name));
        Type = GetVehicleTypeFromString(point.Type);
        GuardConfig = guardConfig;
        Area = area;
        Interior = point.Interior;

        RandomizeVehicleLoadout();
    }

    // Properly change state and update blip color
    public void ChangeState(VehicleState newState)
    {
        if (CurrentState != newState)
        {
            Logger.Log.Info($"Vehicle {AreaName} changing state from {CurrentState} to {newState}");
            CurrentState = newState;
            UpdateBlipColor();
        }
    }

    private GuardVehicleType GetVehicleTypeFromString(string typeString)
    {
        return typeString.ToLower() switch
        {
            "vehicle" => GuardVehicleType.Vehicle,
            "largevehicle" => GuardVehicleType.LargeVehicle,
            "helicopter" => GuardVehicleType.Helicopter,
            "plane" => GuardVehicleType.Plane,
            "boat" => GuardVehicleType.Boat,
            "mounted" => GuardVehicleType.Mounted,
            _ => GuardVehicleType.Vehicle,
        };
    }

    private void RandomizeVehicleLoadout()
    {
        MVehicleModelName = GetRandomElementOrDefault(GuardConfig.MVehicleModels, "Mounted Vehicle Models");
        PVehicleModelName = GetRandomElementOrDefault(GuardConfig.PVehicleModels, "Plane Models");
        BVehicleModelName = GetRandomElementOrDefault(GuardConfig.BVehicleModels, "Boat Models");
        LVehicleModelName = GetRandomElementOrDefault(GuardConfig.LVehicleModels, "Large Vehicle Models");
        HVehicleModelName = GetRandomElementOrDefault(GuardConfig.HVehicleModels, "Helicopter Models");
        VehicleModelName = GetRandomElementOrDefault(GuardConfig.VehicleModels, "Vehicle Models");
    }

    private string GetRandomElementOrDefault(List<string> list, string logContext)
    {
        // Performance: Use Count > 0 instead of .Any()
        if (list != null && list.Count > 0)
        {
            return GetRandomElement(list);
        }
        Logger.Log.Fatal($"Warning: No valid {logContext} found for GuardConfig '{GuardConfig.Name}'. Random selection skipped.");
        return null;
    }

    private T GetRandomElement<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("List cannot be null or empty");
        return list[_random.Next(list.Count)];
    }

    private string GetVehicleModelName()
    {
        return Type switch
        {
            GuardVehicleType.Vehicle => VehicleModelName,
            GuardVehicleType.LargeVehicle => LVehicleModelName,
            GuardVehicleType.Helicopter => HVehicleModelName,
            GuardVehicleType.Plane => PVehicleModelName,
            GuardVehicleType.Boat => BVehicleModelName,
            GuardVehicleType.Mounted => MVehicleModelName,
            _ => throw new ArgumentException($"Unknown vehicle type: {Type}")
        };
    }

    private Vehicle CreateVehicle(string modelName)
    {
        Vehicle vehicle = World.CreateVehicle(modelName, Position);
        if (vehicle == null)
        {
            Logger.Log.Fatal($"Failed to create guard vehicle with model {modelName}.");
            return null;
        }

        vehicle.Heading = Heading;
        vehicle.LockStatus = VehicleLockStatus.CanBeBrokenInto;
        
        // Set max capacity based on vehicle
        MaxCapacity = vehicle.PassengerCapacity + 1; // +1 for driver
        
        return vehicle;
    }

    // In the case of mounted guards, assign a ped to a vehicle seat (gunner).
    private void AssignPedToVehicle()
    {
        if (guardVehicle == null) return;

        for (int i = 0; i < guardVehicle.PassengerCapacity + 1; i++)
        {
            if (guardVehicle.IsSeatFree((VehicleSeat)i) && guardVehicle.IsTurretSeat((VehicleSeat)i))
            {
                string pedModelName = GetRandomElementOrDefault(GuardConfig.PedModels, "PedModels");
                guardPedOnVehicle = guardVehicle.CreatePedOnSeat((VehicleSeat)i, pedModelName);
                
                // Initialize gunner using the proper GuardPed initialization (with isGunner=true)
                InitializeGunnerPed(guardPedOnVehicle);
                break;
            }
        }
    }

    Ped TargetToShoot = null;

    private Ped[] FindNearbyEnemy()
    {
        List<Ped> getlist = new();
        try
        {
            if (guardPedOnVehicle == null || !guardPedOnVehicle.Exists())
                return null;

            // Search for nearby peds within attack range
            Ped[] nearbyPeds = World.GetNearbyPeds(guardPedOnVehicle.Position, 100f);

            if (nearbyPeds == null || nearbyPeds.Length == 0)
                return null;

            foreach (Ped ped in nearbyPeds)
            {
                if (ped == null || !ped.Exists() || ped.IsDead)
                    continue;

                // Skip player
                if (ped == Game.Player.Character)
                    continue;

                // Skip friendly guards (check relationship with gunner)
                if (guardPedOnVehicle.RelationshipGroup != null)
                {
                    Relationship relationship = ped.RelationshipGroup.GetRelationshipBetweenGroups(guardPedOnVehicle.RelationshipGroup);
                    if (relationship == Relationship.Companion || relationship == Relationship.Respect || relationship == Relationship.Like)
                        continue;
                }

                // Add if ped is in combat against player or gunner
                if (ped.IsInCombatAgainst(Game.Player.Character) || ped.IsInCombatAgainst(guardPedOnVehicle))
                {
                    getlist.Add(ped);
                    continue;
                }

                // Add if ped is shooting nearby (not player, not friendly)
                if (ped.IsShooting)
                {
                    getlist.Add(ped);
                }
            }

            return getlist.ToArray();
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"AttackHelicopter: Error finding enemy: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Initialize gunner using proper GuardPed initialization to ensure consistent behavior
    /// This ensures gunners have the same combat attributes and relationships as regular guards
    /// </summary>
    private void InitializeGunnerPed(Ped ped)
    {
        if (ped == null) return;

        ped.Heading = Heading;
        string weaponName = GetRandomElementOrDefault(GuardConfig.Weapons, "Weapons");
        ped.Weapons.Give(weaponName, 1500, true, true);
        ped.Armor = 200;
        ped.MaxHealth = 300;
        ped.Health = 300;
        ped.DrivingAggressiveness = 1f;
        ped.IsCollisionEnabled = true;
        ped.DiesOnLowHealth = false;
        
        // CRITICAL: Set up relationship group SAME as other guards in this area
        // This ensures proper relationships with all allied guards
        string groupName = $"{GuardConfig.RelationshipGroup}_{AreaName}";
        ped.RelationshipGroup = World.AddRelationshipGroup(groupName);
        
        Function.Call(Hash.SET_PED_RANDOM_PROPS, ped);
        Function.Call(Hash.SET_PED_RANDOM_COMPONENT_VARIATION, ped);

        // Combat attributes - SAME as all guards (from GuardPed.InitializePed)
        ped.SetCombatAttribute(CombatAttributes.AlwaysFight, true);
        ped.SetCombatAttribute(CombatAttributes.CanUseVehicles, true);
        ped.SetCombatAttribute(CombatAttributes.WillDragInjuredPedsToSafety, true);
        ped.SetCombatAttribute(CombatAttributes.CanCommandeerVehicles, true);
        ped.SetCombatAttribute(CombatAttributes.CanUseCover, true);
        ped.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
        ped.SetCombatAttribute(CombatAttributes.AlwaysFlee, false);
        ped.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, false);
        ped.SetCombatAttribute(CombatAttributes.WillScanForDeadPeds, true);
        ped.SetCombatAttribute(CombatAttributes.DisableBulletReactions, true);
        ped.SetCombatAttribute(CombatAttributes.CanChaseTargetOnFoot, true);
        ped.SetCombatAttribute(CombatAttributes.SwitchToDefensiveIfInCover, true);
        ped.SetCombatAttribute(CombatAttributes.BlindFireWhenInCover, true);
        ped.SetCombatAttribute(CombatAttributes.CanUsePeekingVariations, true);
        ped.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true);
        ped.SetCombatAttribute(CombatAttributes.CanTauntInVehicle, true);
        ped.SetCombatAttribute(CombatAttributes.AlwaysEquipBestWeapon, true);

        // Gunner-specific: Can't leave vehicle
        ped.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, false);
        ped.SetCombatAttribute(CombatAttributes.UseVehicleAttackIfVehicleHasMountedGuns, true);

        // Config flags - SAME as all guards
        ped.SetConfigFlag(PedConfigFlagToggles.DisableGoToWritheWhenInjured, true);
        ped.SetConfigFlag(PedConfigFlagToggles.CanDiveAwayFromApproachingVehicles, true);
        ped.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, true);
        ped.SetConfigFlag(PedConfigFlagToggles.AIDriverAllowFriendlyPassengerSeatEntry, true);
        ped.SetConfigFlag(PedConfigFlagToggles.KeepRelationshipGroupAfterCleanUp, true);
        ped.SetConfigFlag(PedConfigFlagToggles.AllowMedicsToReviveMe, false);
        ped.SetConfigFlag(PedConfigFlagToggles.KeepTasksAfterCleanUp, true);
        ped.SetConfigFlag(PedConfigFlagToggles.TargetWhenInjuredAllowed, false);
        ped.SetConfigFlag(PedConfigFlagToggles.CanAttackFriendly, false);
        ped.SetConfigFlag(PedConfigFlagToggles.DisableBlindFiringInShotReactions, false);
        ped.SetConfigFlag(PedConfigFlagToggles.AvoidTearGas, true);
        
        ped.PopulationType = EntityPopulationType.Mission;
        
        if (ped.PedType == PedType.Cop || ped.PedType == PedType.Swat || ped.PedType == PedType.Army)
        {
            ped.SetConfigFlag(PedConfigFlagToggles.LawWillOnlyAttackIfPlayerIsWanted, true);
            ped.SetConfigFlag(PedConfigFlagToggles.KeepRelationshipGroupAfterCleanUp, true);
            ped.SetConfigFlag(PedConfigFlagToggles.KeepTasksAfterCleanUp, true);
            ped.SetConfigFlag(PedConfigFlagToggles.KeepTargetLossResponseOnCleanup, true);
            ped.SetConfigFlag(PedConfigFlagToggles.DontAttackPlayerWithoutWantedLevel, true);
            ped.TargetLossResponse = TargetLossResponse.SearchForTarget;
            if (ped.PedType != PedType.Cop) ped.SetCombatAttribute(CombatAttributes.CanThrowSmokeGrenade, true);
        }
        
        // Set up relationships SAME as regular guards
        SetupMountedPedRelationships(ped);
        
        // Increase perception ranges for mounted gunners as well
        try
        {
            ped.SeeingRange = 100f;
            ped.HearingRange = 200f;
        }
        catch { }
        Logger.Log.Info($"Gunner initialized for vehicle in area {AreaName} with full guard combat attributes and relationships");
    }
    
    /// <summary>
    /// Setup relationships for mounted gunner - EXACT SAME logic as GuardPed.SetupRelationships
    /// This ensures gunners behave identically to regular guards
    /// </summary>
    private void SetupMountedPedRelationships(Ped ped)
    {
        if (ped == null)
        {
            Logger.Log.Fatal($"Warning: SetupMountedPedRelationships called but ped is null.");
            return;
        }
        
        try
        {
            // EXACT SAME law groups setup as GuardPed
            var lawGroups = new List<uint>
            {
                GuardPed.GetHash("PRIVATE_SECURITY"),
                GuardPed.GetHash("SECURITY_GUARD"),
                GuardPed.GetHash("ARMY"),
                GuardPed.GetHash("COP"),
                GuardPed.GetHash("GUARD_DOG"),
                GuardPed.GetHash("INVESTIGATE")
            };

            foreach (uint lawA in lawGroups)
            {
                foreach (uint lawB in lawGroups)
                {
                    ped.SetConfigFlag(PedConfigFlagToggles.CanAttackNonWantedPlayerAsLaw, false);
                    ped.SetConfigFlag(PedConfigFlagToggles.LawWillOnlyAttackIfPlayerIsWanted, true);
                    ped.TargetLossResponse = TargetLossResponse.SearchForTarget;
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, PedRelationship.Respect, lawA, lawB);
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, PedRelationship.Respect, lawB, lawA);
                }
            }

            // Setup the gunner's relationship group - EXACT SAME as GuardPed
            ped.RelationshipGroup = World.AddRelationshipGroup(GuardConfig.RelationshipGroup);
            ped.RelationshipGroup.SetRelationshipBetweenGroups(ped.RelationshipGroup, Relationship.Companion, true);

            // Respect rules: if area demands respect based on settings - EXACT SAME logic
            if (Area.Respect == "YES" || Area.Respect == "ANY" || Area.Respect == "ALL")
            {
                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(ped.RelationshipGroup, Relationship.Companion);
                ped.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Companion);
            }
            else if ((Area.Respect == "TREVOR" && Game.Player.Character.Model == PedHash.Trevor) ||
                     (Area.Respect == "MICHAEL" && Game.Player.Character.Model == PedHash.Michael) ||
                     (Area.Respect == "FRANKLIN" && Game.Player.Character.Model == PedHash.Franklin))
            {
                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(ped.RelationshipGroup, Relationship.Companion);
                ped.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Companion);
            }
            else
            {
                HandleMultipleRespectEntriesForMounted(ped);
            }
            
            // Note: Cross-area guard relationships are setup centrally in GuardSpawner
            // during initialization, so gunners automatically get those relationships too
            
            Logger.Log.Info($"Gunner relationships setup complete for area {AreaName} using GuardPed logic");
        }
        catch (Exception ex)
        {
            Logger.Log.Fatal($"Error in SetupMountedPedRelationships: {ex.Message} StackTrace: {ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// Handle multiple respect entries for mounted gunner - EXACT SAME as GuardPed.HandleMultipleRespectEntries
    /// </summary>
    private void HandleMultipleRespectEntriesForMounted(Ped ped)
    {
        bool respectedCharacter = false;
        string[] respectedCharactersList = Area.Respect?.Split(',') ?? new string[0];

        foreach (string characterName in respectedCharactersList)
        {
            string trimmedName = characterName.Trim().ToUpperInvariant();
            if ((trimmedName == "TREVOR" && Game.Player.Character.Model == PedHash.Trevor) ||
                (trimmedName == "MICHAEL" && Game.Player.Character.Model == PedHash.Michael) ||
                (trimmedName == "FRANKLIN" && Game.Player.Character.Model == PedHash.Franklin))
            {
                respectedCharacter = true;
                break;
            }
        }

        if (respectedCharacter)
        {
            Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(ped.RelationshipGroup, Relationship.Companion);
            ped.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Companion);
        }
    }

    public void Spawn()
    {
        Logger.Log.Info($"Spawning guard vehicle at position {Position}, heading {Heading}, area {AreaName}, type {Type}");
        
        string modelName = GetVehicleModelName();
        guardVehicle = CreateVehicle(modelName);
        if (guardVehicle == null) return;

        // For mounted vehicles, create the gunner
        if (Type == GuardVehicleType.Mounted)
        {
            AssignPedToVehicle();
        }

        CreateBlip();
        ChangeState(VehicleState.Idle);
    }

    public void Despawn()
    {
        Logger.Log.Info($"Despawning guard vehicle at position {Position}, type {Type}");

        RemoveBlip();

        // Remove all assigned guards from vehicle
        foreach (var guard in AssignedGuards.ToList())
        {
            RemoveGuard(guard);
        }

        // Despawn mounted gunner if exists
        if (guardPedOnVehicle != null && guardPedOnVehicle.Exists())
        {
            guardPedOnVehicle.MarkAsNoLongerNeeded();
        }

        // Despawn vehicle
        if (guardVehicle != null && guardVehicle.Exists())
        {
            guardVehicle.MarkAsNoLongerNeeded();
        }

        ChangeState(VehicleState.Destroyed);
    }

    // Assign a guard to this vehicle
    public bool AssignGuard(GuardPed guard, VehicleSeat preferredSeat = VehicleSeat.Any)
    {
        if (guardVehicle == null || !guardVehicle.Exists() || AssignedGuards.Count >= MaxCapacity)
            return false;

        // Find available seat
        VehicleSeat assignedSeat = preferredSeat;
        if (preferredSeat == VehicleSeat.Any)
        {
            // Try driver first, then passengers
            if (DriverGuard == null && guardVehicle.IsSeatFree(VehicleSeat.Driver))
                assignedSeat = VehicleSeat.Driver;
            else
            {
                for (int i = 0; i < guardVehicle.PassengerCapacity; i++)
                {
                    if (guardVehicle.IsSeatFree((VehicleSeat)i))
                    {
                        assignedSeat = (VehicleSeat)i;
                        break;
                    }
                }
            }
        }

        if (!guardVehicle.IsSeatFree(assignedSeat))
            return false;

        // Assign guard
        guard.AssignedVehicle = this;
        guard.AssignedSeatIndex = assignedSeat;
        AssignedGuards.Add(guard);

        if (assignedSeat == VehicleSeat.Driver)
            DriverGuard = guard;

        return true;
    }

    // Remove a guard from this vehicle
    public void RemoveGuard(GuardPed guard)
    {
        if (guard == null) return;

        guard.AssignedVehicle = null;
        guard.AssignedSeatIndex = VehicleSeat.Any;
        AssignedGuards.Remove(guard);

        if (DriverGuard == guard)
            DriverGuard = null;
    }

    // Properly unassign a ped from vehicle with full cleanup
    public void UnassignPed(GuardPed ped)
    {
        if (ped == null) return;

        if (AssignedPeds.Contains(ped))
        {
            AssignedPeds.Remove(ped);
            Logger.Log.Info($"Unassigned ped {ped.guardPed?.Handle} from vehicle {AreaName}");
        }

        if (AssignedGuards.Contains(ped))
        {
            AssignedGuards.Remove(ped);
        }

        if (DriverGuard == ped)
        {
            DriverGuard = null;
            Logger.Log.Info($"Cleared driver assignment for vehicle {AreaName}");
        }

        if (GuardsToDeployOnArrival.Contains(ped))
        {
            GuardsToDeployOnArrival.Remove(ped);
        }

        if (GuardSpawnAssignments.ContainsKey(ped))
        {
            GuardSpawnAssignments.Remove(ped);
        }

        // Clear the ped's vehicle reference
        ped.AssignedVehicle = null;
        ped.AssignedSeatIndex = VehicleSeat.Any;
    }

   

    // Update vehicle state (call periodically)
    public void UpdateVehicleState()
    {
        if (guardVehicle == null || !guardVehicle.Exists())
        {
            ChangeState(VehicleState.Destroyed);
            return;
        }

        // Update gunner combat target if mounted ped exists
        if (guardPedOnVehicle != null && guardPedOnVehicle.Exists() && guardPedOnVehicle.IsAlive)
        {
            // Check if current target is still valid
            if (TargetToShoot != null && (!TargetToShoot.Exists() || TargetToShoot.IsDead))
            {
                TargetToShoot = null;
            }

            // Find new target if needed
            if (TargetToShoot == null)
            {
                Ped[] enemies = FindNearbyEnemy();
                if (enemies != null && enemies.Length > 0)
                {
                    TargetToShoot = enemies[_random.Next(0, enemies.Length)];
                }
            }

            // Assign combat task if we have a valid target
            if (TargetToShoot != null && TargetToShoot.Exists() && TargetToShoot.IsAlive)
            {
                guardPedOnVehicle.Task.ShootAt(TargetToShoot, -1, FiringPattern.FullAuto);
            }
        }

        switch (CurrentState)
        {
            case VehicleState.Arriving:
                // Check if vehicle has reached its destination
                Vector3 destination = TargetDeploymentPoint != Vector3.Zero ? 
                                    TargetDeploymentPoint : 
                                    Position; // Original spawn position
                
                float distanceToDestination = guardVehicle.Position.DistanceTo(destination);
                bool vehicleStopped = guardVehicle.Speed < 2f;
                
                if (distanceToDestination < 5f && vehicleStopped)
                {
                    Logger.Log.Info($"Vehicle {AreaName} reached destination, transitioning guards to ExitVehicle");
                    
                    // Transition all assigned peds to ExitVehicle state
                    foreach (var ped in AssignedPeds.ToList())
                    {
                        if (ped.CurrentState == GuardState.Arriving)
                        {
                            ped.ChangeState(GuardState.ExitVehicle);
                        }
                    }
                    
                    ChangeState(VehicleState.Idle);
                }
                break;
                
            case VehicleState.Departing:
                // Monitor departing vehicle until it's far enough away or destroyed
                Vector3 originalPosition = Position;
                float distanceFromOrigin = guardVehicle.Position.DistanceTo(originalPosition);
                
                if (distanceFromOrigin > 300f)
                {
                    Logger.Log.Info($"Departing vehicle {AreaName} is far enough away, cleaning up");
                    // Vehicle is far enough away - clean up
                    Despawn();
                }
                break;
                
            case VehicleState.Idle:
                // Check if all assigned peds have exited and been unassigned
                var pedsStillInVehicle = AssignedPeds.Where(ped => 
                    ped.guardPed != null && 
                    ped.guardPed.Exists() && 
                    ped.guardPed.IsInVehicle(guardVehicle)).ToList();
                
                // Performance: Use Count > 0 instead of .Any()
                if (pedsStillInVehicle.Count > 0)
                {
                    // Some peds are still in vehicle - check if they should exit
                    foreach (var ped in pedsStillInVehicle)
                    {
                        if (ped.CurrentState != GuardState.ExitVehicle && 
                            ped.CurrentState != GuardState.Arriving)
                        {
                            Logger.Log.Info($"Ped {ped.guardPed.Handle} still in idle vehicle, transitioning to ExitVehicle");
                            ped.ChangeState(GuardState.ExitVehicle);
                        }
                    }
                }
                break;
                
            case VehicleState.Destroyed:
                // Handle cleanup for destroyed vehicle
                Logger.Log.Warning($"Vehicle {AreaName} destroyed, cleaning up assignments");
                
                // Emergency unassign all peds
                foreach (var ped in AssignedPeds.ToList())
                {
                    ped.AssignedVehicle = null;
                    if (ped.CurrentState == GuardState.Arriving || ped.CurrentState == GuardState.ExitVehicle)
                    {
                        ped.ChangeState(GuardState.OnDuty);
                        ped.ResumeNormalDuty();
                    }
                }
                ClearAllAssignments();
                break;
        }
    }

    // Helper methods for better vehicle coordination
    public bool HasAvailableSeats()
    {
        return AssignedGuards.Count < MaxCapacity;
    }

    public int GetAvailableSeatCount()
    {
        return MaxCapacity - AssignedGuards.Count;
    }

    public bool IsDriverAssigned()
    {
        return DriverGuard != null;
    }

    public bool IsReadyToDepart()
    {
        // Vehicle is ready to depart if it has at least a driver and all assigned guards are boarded
        return IsDriverAssigned() && AllAssignedPedsBoarded() && AssignedPeds.Count > 0;
    }

    public List<GuardPed> GetUnboardedGuards()
    {
        return AssignedPeds.Where(ped => 
            ped.guardPed != null && 
            ped.guardPed.Exists() && 
            !ped.guardPed.IsInVehicle(this.guardVehicle))
            .ToList();
    }

    public void ClearAllAssignments()
    {
        AssignedPeds.Clear();
        AssignedGuards.Clear();
        DriverGuard = null;
        GuardsToDeployOnArrival.Clear();
        GuardSpawnAssignments.Clear();
    }

    
    // Create blip for vehicle
    private void CreateBlip()
    {
        // Check if blips are enabled in INI and if this area respects the player
        if (!PlayerPositionLogger.GetEnableBlips() || !DoesAreaRespectPlayer() || guardVehicle == null || !guardVehicle.Exists())
            return;
            
        try
        {
            VehicleBlip = guardVehicle.AddBlip();
            if (VehicleBlip != null)
            {
                VehicleBlip.Sprite = Type switch
                {
                    GuardVehicleType.Helicopter => BlipSprite.Helicopter,
                    GuardVehicleType.Plane => BlipSprite.Plane,
                    GuardVehicleType.Boat => BlipSprite.Boat,
                    GuardVehicleType.LargeVehicle => BlipSprite.Truck,
                    GuardVehicleType.Mounted => BlipSprite.Tank,
                    _ => BlipSprite.PersonalVehicleCar
                };
                
                VehicleBlip.Scale = 0.8f;
                VehicleBlip.Name = $"Guard Vehicle - {AreaName}";
                
                // Color based on character respect (same as guards)
                VehicleBlip.Color = GetBlipColorForArea();
            }
        }
        catch (Exception ex)
        {
            Logger.Log.Warning($"Failed to create blip for vehicle: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Check if this area respects the current player character
    /// </summary>
    private bool DoesAreaRespectPlayer()
    {
        if (Area == null || string.IsNullOrEmpty(Area.Respect))
            return false;

        string respect = Area.Respect.ToUpperInvariant();
        
        // Check for universal respect
        if (respect == "YES" || respect == "ANY" || respect == "ALL")
            return true;

        // Get current player character
        GTA.Ped player = GTA.Game.Player.Character;
        if (player == null || !player.Exists())
            return false;

        // Check which character the player is using
        if (player.Model == PedHash.Michael && respect.Contains("MICHAEL"))
            return true;
        if (player.Model == PedHash.Franklin && respect.Contains("FRANKLIN"))
            return true;
        if (player.Model == PedHash.Trevor && respect.Contains("TREVOR"))
            return true;

        return false;
    }
    
    /// <summary>
    /// Get blip color based on which character the area respects
    /// Franklin = Green, Michael = Blue, Trevor = Orange
    /// </summary>
    private BlipColor GetBlipColorForArea()
    {
        if (string.IsNullOrEmpty(Area.Respect))
            return BlipColor.White;

        string respect = Area.Respect.ToUpperInvariant();

        if (respect.Contains("FRANKLIN") && Game.Player.Character.Model == PedHash.Franklin)
            return BlipColor.Green;

        if (respect.Contains("MICHAEL") && Game.Player.Character.Model == PedHash.Michael)
            return BlipColor.Blue;

        if (respect.Contains("TREVOR") && Game.Player.Character.Model == PedHash.Trevor)
            return BlipColor.Orange;

        return BlipColor.White;
    }
    
    // Update blip color based on vehicle state
    private void UpdateBlipColor()
    {
        if (VehicleBlip == null || !VehicleBlip.Exists())
            return;
            
        // Keep character-based color, but modify appearance based on state
        switch (CurrentState)
        {
            case VehicleState.Idle:
                VehicleBlip.Alpha = 255; // Full opacity
                VehicleBlip.Scale = 0.8f;
                break;
            case VehicleState.Arriving:
                VehicleBlip.Alpha = 200; // Slightly transparent
                VehicleBlip.Scale = 0.9f; // Larger to show active movement
                break;
            case VehicleState.Departing:
                VehicleBlip.Alpha = 150; // More transparent
                VehicleBlip.Scale = 0.7f; // Smaller when leaving
                break;
            case VehicleState.InTransit:
                VehicleBlip.Alpha = 180;
                VehicleBlip.Scale = 0.85f;
                break;
            case VehicleState.Destroyed:
                VehicleBlip.Alpha = 100; // Very transparent
                VehicleBlip.Scale = 0.6f;
                break;
        }
        
        // Color stays character-based (set during CreateBlip)
    }
    
    // Remove blip
    private void RemoveBlip()
    {
        if (VehicleBlip != null && VehicleBlip.Exists())
        {
            VehicleBlip.Delete();
            VehicleBlip = null;
        }
    }
}