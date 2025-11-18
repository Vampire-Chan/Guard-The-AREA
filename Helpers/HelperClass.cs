using GTA.Native;
using GTA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;
using System.Text.RegularExpressions;


public static class HelperClass
{

    #region Shared Resources and Caching
    // Consolidated random instance
    public static readonly Random SharedRandom = new Random();
    [Obsolete("Use SharedRandom instead")]
    public static readonly Random rand = SharedRandom; // Backward compatibility

    // Hash caching for performance
    private static readonly Dictionary<string, uint> _hashCache = new Dictionary<string, uint>();
    private static readonly Dictionary<string, Model> _modelCache = new Dictionary<string, Model>();

    // Reusable OutputArguments to reduce allocations
    private static readonly OutputArgument _reusableOutputArg = new OutputArgument();
    private static readonly OutputArgument _reusableOutputArg2 = new OutputArgument();
    #endregion

    #region Utility Methods with Caching
    private static uint GetHashCached(string input)
    {
        if (!_hashCache.TryGetValue(input, out uint hash))
        {
            hash = (uint)Function.Call<int>(Hash.GET_HASH_KEY, input);
            _hashCache[input] = hash;
        }
        return hash;
    }

    private static Model GetModelCached(string modelName)
    {
        if (!_modelCache.TryGetValue(modelName, out Model model))
        {
            model = new Model(modelName);
            _modelCache[modelName] = model;
        }
        return model;
    }

    public static Vector3 GetCrosshairCoords()
    {
        try
        {
            Logger.Log.Info("Getting Crosshair Coordinates!!");
            return World.Raycast(GameplayCamera.Position, GameplayCamera.Direction, 1000f, IntersectFlags.Everything, Game.Player.Character).HitPosition;
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Error getting crosshair coordinates: {ex.Message}");
            return Vector3.Zero;
        }
    }

    public static void Subtitle(string msg)
    {
        GTA.UI.Screen.ShowSubtitle(msg);
    }

    public static void Notification(string msg)
    {
        GTA.UI.Notification.PostTicker(msg, false);
    }

    public static float GetRandomFloat(double min, double max)
    {
        return (float)(SharedRandom.NextDouble() * (max - min) + min);
    }

    public static double GetDouble()
    {
        return SharedRandom.NextDouble();
    }

    public static bool GetBool()
    {
        return GetDouble() >= 0.5;
    }

    public static Vector3 RandomPointInsideCircle(Vector3 center, float radius)
    {
        double distance = (double)radius * Math.Sqrt(GetDouble());
        double angle = GetDouble() * 2.0 * Math.PI;
        return new Vector3(center.X + (float)(distance * Math.Cos(angle)), center.Y + (float)(distance * Math.Sin(angle)), center.Z);
    }

    public static float NormalizeAngle(float value)
    {
        const float fullCircle = 360f;
        return value - (float)Math.Floor(value / fullCircle) * fullCircle;
    }

    public static Vector3 GetPointBetweenTwoVectors(Vector3 start, Vector3 end, float ratio)
    {
        return new Vector3(
            start.X + (end.X - start.X) * ratio,
            start.Y + (end.Y - start.Y) * ratio,
            start.Z + (end.Z - start.Z) * ratio
        );
    }

    public static float GetAngleBetweenTwoPoints(Vector3 source, Vector3 target)
    {
        return (target - source).Normalized.ToHeading();
    }
    #endregion

    #region Ped Extensions with Error Handling
    public static int GetLastDamageBone(this Ped ped)
    {
        try
        {
            Function.Call<bool>(Hash.GET_PED_LAST_DAMAGE_BONE, ped, _reusableOutputArg);
            return _reusableOutputArg.GetResult<int>();
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Error getting last damage bone: {ex.Message}");
            return -1;
        }
    }

    public static void ClearLastDamageBone(this Ped ped)
    {
        try
        {
            Function.Call(Hash.CLEAR_PED_LAST_DAMAGE_BONE, ped);
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Error clearing last damage bone: {ex.Message}");
        }
    }

    public static void PlayAmbientSpeech(this Ped ped, string speechFile, bool immediately)
    {
        try
        {
            if (immediately)
            {
                Function.Call(Hash.STOP_CURRENT_PLAYING_AMBIENT_SPEECH, ped);
            }
            Function.Call(Hash.SET_AUDIO_FLAG, "IsDirectorModeActive", 1);
            Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE, ped, speechFile, "SPEECH_PARAMS_FORCE");
            Function.Call(Hash.SET_AUDIO_FLAG, "IsDirectorModeActive", 0);
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Error playing ambient speech: {ex.Message}");
        }
    }

    public static bool HasBeenDamagedByWeapon(this Ped ped, WeaponHash weapon)
    {
        try
        {
            return Function.Call<bool>(Hash.HAS_PED_BEEN_DAMAGED_BY_WEAPON, ped, weapon.GetHashCode(), 0);
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Error checking weapon damage: {ex.Message}");
            return false;
        }
    }

    public static void ClearLastWeaponDamage(this Ped ped)
    {
        try
        {
            Function.Call(Hash.CLEAR_PED_LAST_WEAPON_DAMAGE, ped);
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"Error clearing last weapon damage: {ex.Message}");
        }
    }

    public static bool IsTaskActive(this Ped ped, PedTask taskId)
    {
        return Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, ped.Handle, (int)taskId);
    }

    public static void SetPedCycleVehicleWeapon(this Ped ped)
    {
        Function.Call(Hash.SET_PED_CYCLE_VEHICLE_WEAPONS_ONLY, ped);
    }

    public static void SetDriverAbility(this Ped ped, float value)
    {
        Function.Call(Hash.SET_DRIVER_ABILITY, ped, value);
    }

    public static void AssignDefaultTask(this Ped ped)
    {
        Function.Call(Hash.CLEAR_DEFAULT_PRIMARY_TASK, ped.Handle);
    }

    public static void StandGuard(this Ped ped, Vector3 defend, float heading, string animScenario)
    {
        Function.Call(Hash.TASK_STAND_GUARD, ped, defend.X, defend.Y, defend.Z, heading, animScenario);
    }

    public static void GuardCurrentPosition(this Ped ped, bool defensive)
    {
        Function.Call(Hash.TASK_GUARD_CURRENT_POSITION, ped, 40f, 35f, defensive);
    }
    #endregion

    #region Weapon Extensions with Cached Hashes
    public static void GiveWeaponWithComponent(this WeaponHash weapon, Ped ped, WeaponComponentHash component)
    {
        Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED, ped, weapon, component);
    }

    public static void RemoveWeaponWithComponent(this WeaponHash weapon, Ped ped, WeaponComponentHash component)
    {
        Function.Call(Hash.REMOVE_WEAPON_COMPONENT_FROM_PED, ped, weapon, component);
    }

    public static void GiveSpecialAmmo(this WeaponHash weapon, Ped ped, string ammoType)
    {
        Function.Call(Hash.ADD_PED_AMMO_BY_TYPE, ped, StringHash.AtStringHash(ammoType));
    }

    public static int GetWeaponComponentExtraCount(this WeaponComponent component, WeaponHash weapon)
    {
        return Function.Call<int>(Hash.GET_WEAPON_COMPONENT_VARIANT_EXTRA_COUNT, weapon);
    }
    #endregion

    #region Prop Extensions
    public static bool PlaceOnGround(this Prop prop)
    {
        return Function.Call<bool>(Hash.PLACE_OBJECT_ON_GROUND_PROPERLY, prop.Handle);
    }

    public static void ForceVehiclesToAvoid(this Prop prop, bool toggle)
    {
        Function.Call(Hash.SET_OBJECT_FORCE_VEHICLES_TO_AVOID, prop.Handle, toggle);
    }
    #endregion

    #region Relationship Management
    public static Relationship GetRelationshipBetweenGroups(int group1, int group2)
    {
        return (Relationship)Function.Call<int>(Hash.GET_RELATIONSHIP_BETWEEN_GROUPS, group1, group2);
    }

    public static void SetRelationshipBetweenGroups(Relationship relationship, int group1, int group2)
    {
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, (int)relationship, group1, group2);
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, (int)relationship, group2, group1);
    }
    #endregion


    #region Water and Height Utilities with Reusable OutputArguments
    public static float GetWaterHeight(Vector3 position)
    {
        try
        {
            Function.Call(Hash.GET_WATER_HEIGHT, position.X, position.Y, position.Z, _reusableOutputArg);
            return _reusableOutputArg.GetResult<float>();
        }
        catch (Exception ex)
        {
            Logger.Log.Fatal($"GetWaterHeight error: {ex.Message}");
            return 0f;
        }
    }

    public static unsafe bool GetWaterLevelNoWaves(Vector3 startPoint, out float height)
    {
        height = 0f;
        try
        {
            if (Function.Call<bool>(Hash.GET_WATER_HEIGHT_NO_WAVES, startPoint.X, startPoint.Y, startPoint.Z, _reusableOutputArg))
            {
                height = _reusableOutputArg.GetResult<float>();
                return true;
            }
        }
        catch (Exception ex)
        {
            Logger.Log.Error($"GetWaterLevelNoWaves error: {ex.Message}");
        }
        return false;
    }
    #endregion

    #region Native Function Utilities with Optimized OutputArguments
    public static Vector3 GetHeliSpawnCoordinates(this Ped ped)
    {
        return Function.Call<Vector3>(Hash.FIND_SPAWN_COORDINATES_FOR_HELI, ped);
    }

    public static int GetWantedLevelThreshold(int wantedLevel)
    {
        return Function.Call<int>(Hash.GET_WANTED_LEVEL_THRESHOLD, wantedLevel);
    }

    public static int GetNumberOfResourcesAssignedToWantedLevel(DispatchType dispatchType)
    {
        return Function.Call<int>(Hash.GET_NUMBER_RESOURCES_ALLOCATED_TO_WANTED_LEVEL, (int)dispatchType);
    }

    public static bool DoesScenarioExistInArea(Vector3 searchArea, float radius, bool unoccupied)
    {
        return Function.Call<bool>(Hash.DOES_SCENARIO_EXIST_IN_AREA, searchArea.X, searchArea.Y, searchArea.Z, radius, unoccupied);
    }

    public static bool IsSphereVisibleToPlayer(Vector3 center, float radius)
    {
        return Function.Call<bool>(Hash.IS_SPHERE_VISIBLE, center.X, center.Y, center.Z, radius);
    }

    public static int AddSpeedZone(Vector3 position, float radius, float speed, bool affectsMissionVehs = false)
    {
        return Function.Call<int>(Hash.ADD_ROAD_NODE_SPEED_ZONE, position.X, position.Y, position.Z, radius, speed, affectsMissionVehs);
    }

    public static void RemoveSpeedZone(int id)
    {
        Function.Call(Hash.REMOVE_ROAD_NODE_SPEED_ZONE, id);
    }

    public static unsafe bool FindSpawnPointInDirection(Vector3 position, Vector3 direction, float idealDistance, out Vector3 spawnPoint)
    {
        if (Function.Call<bool>(Hash.FIND_SPAWN_POINT_IN_DIRECTION,
            position.X, position.Y, position.Z,
            direction.X, direction.Y, direction.Z,
            idealDistance, _reusableOutputArg))
        {
            spawnPoint = _reusableOutputArg.GetResult<Vector3>();
            return true;
        }
        spawnPoint = Vector3.Zero;
        return false;
    }

    public static unsafe void GetSpawnCoordsForVehicleNode(int nodeAddress, Vector3 targetDirection, out Vector3 spawnPosition, out float heading)
    {
        Function.Call(Hash.GET_SPAWN_COORDS_FOR_VEHICLE_NODE, nodeAddress,
            targetDirection.X, targetDirection.Y, targetDirection.Z,
            _reusableOutputArg, _reusableOutputArg2);
        spawnPosition = _reusableOutputArg.GetResult<Vector3>();
        heading = _reusableOutputArg2.GetResult<float>();
    }

    public static unsafe bool GetRandomVehicleNode(Vector3 center, float radius, int minLanes, bool avoidDeadEnds, bool avoidHighways, out Vector3 nodePosition, out int nodeAddress)
    {
        nodeAddress = 0;
        nodePosition = Vector3.Zero;

        if (Function.Call<bool>(Hash.GET_RANDOM_VEHICLE_NODE,
            center.X, center.Y, center.Z, radius, minLanes, avoidDeadEnds, avoidHighways,
            _reusableOutputArg, _reusableOutputArg2))
        {
            nodePosition = _reusableOutputArg.GetResult<Vector3>();
            nodeAddress = _reusableOutputArg2.GetResult<int>();
            return true;
        }
        return false;
    }
    #endregion

    #region Search Point Methods with Consistent Random Usage
    public static Vector3 FindSearchPointForAutomobile(Vector3 startPosition, float maxRadius, bool useLastSeenPosition = false)
    {
        Vector3 position = RandomPointInsideCircle(startPosition, maxRadius);
        if (useLastSeenPosition)
        {
            Vector3 lastKnown = ImportantChecks.LastKnownLocation.Around(20);
            if (lastKnown.DistanceTo2D(startPosition) < maxRadius)
            {
                position = lastKnown;
            }
            else
            {
                Vector3 playerPos = Game.Player.Character.Position;
                float distance = Math.Max(maxRadius / 2f, 120f);
                position = GetPointBetweenTwoVectors(playerPos, lastKnown, distance / playerPos.DistanceTo2D(lastKnown));
            }
        }
        return World.GetNextPositionOnStreet(position);
    }

    public static Vector3 FindSearchPointForBoat(Vector3 startPosition, float maxRadius, bool useLastSeenPosition = false)
    {
        Vector3 result = RandomPointInsideCircle(startPosition, maxRadius);
        if (useLastSeenPosition)
        {
            Vector3 lastKnown = ImportantChecks.LastKnownLocation.Around(50);
            if (lastKnown.DistanceTo2D(startPosition) < maxRadius)
            {
                result = lastKnown;
            }
            else
            {
                Vector3 playerPos = Game.Player.Character.Position;
                float distance = Math.Max(maxRadius / 2f, 120f);
                result = GetPointBetweenTwoVectors(playerPos, lastKnown, distance / playerPos.DistanceTo2D(lastKnown));
            }
        }
        result.Z = startPosition.Z;
        if (GetWaterLevelNoWaves(new Vector3(result.X, result.Y, 200f), out var waterHeight))
        {
            result.Z = waterHeight;
        }
        return result;
    }

    public static Vector3 FindSearchPointForHelicopter(Vector3 startPosition, float maxRadius, float height, bool useLastSeenPosition = false)
    {
        Vector3 result = RandomPointInsideCircle(startPosition, maxRadius);
        if (useLastSeenPosition)
        {
            Vector3 lastKnown = ImportantChecks.LastKnownLocation.Around(60);
            if (lastKnown.DistanceTo2D(startPosition) < maxRadius)
            {
                result = lastKnown;
            }
            else
            {
                Vector3 playerPos = Game.Player.Character.Position;
                float distance = Math.Max(maxRadius / 2f, 120f);
                result = GetPointBetweenTwoVectors(playerPos, lastKnown, distance / playerPos.DistanceTo2D(lastKnown));
            }
        }
        result.Z += height;
        return result;
    }

    public static Vector3 FindSearchPointForPlane(Vector3 startPosition, float maxRadius, float height, bool useLastSeenPosition = false)
    {
        Vector3 result = FindSearchPointForHelicopter(startPosition, maxRadius, height, useLastSeenPosition);
        Vector3 testPoint = new Vector3(result.X, result.Y, 1000f);
        if (!World.GetGroundHeight(testPoint, out var groundHeight, GetGroundHeightMode.ConsiderWaterAsGroundNoWaves))
        {
            groundHeight = World.GetApproxHeightForPoint(testPoint);
        }
        result.Z = Math.Max(groundHeight + height, result.Z);
        return result;
    }

    public static Vector3 FindSearchPointForSubmarine(Vector3 startPosition, float maxRadius, bool useLastSeenPosition = false)
    {
        Vector3 result = RandomPointInsideCircle(startPosition, maxRadius);
        if (useLastSeenPosition)
        {
            Vector3 lastKnown = ImportantChecks.LastKnownLocation.Around(50f) + Vector3.WorldDown * 20;
            if (lastKnown.DistanceTo2D(startPosition) < maxRadius)
            {
                result = lastKnown;
            }
            else
            {
                Vector3 playerPos = Game.Player.Character.Position;
                float distance = Math.Max(maxRadius / 2f, 120f);
                result = GetPointBetweenTwoVectors(playerPos, lastKnown, distance / playerPos.DistanceTo2D(lastKnown));
            }
        }
        Vector3 testPoint = new Vector3(result.X, result.Y, 200f);
        if (!GetWaterLevelNoWaves(testPoint, out var waterHeight))
        {
            return Vector3.Zero;
        }
        result.Z = Math.Min(result.Z, waterHeight - 10f);
        if (World.GetGroundHeight(testPoint, out var groundHeight))
        {
            result.Z = Math.Max(result.Z, groundHeight + 15f);
        }
        return result;
    }
    #endregion

    #region Spawn Point Finding Methods with Optimized Performance

    public static bool FindSpawnPointForAutomobile(Ped target, Vector3 startPosition, float minDistance, float maxDistance, out Vector3 spawnPoint, out float spawnHeading, int maxTries = 5)
    {
        float speed = target.Speed;
        Vector3 targetPos = target.Position;

        if (speed >= 14f)
        {
            return FindSpawnPointForFastMovingTarget(target, startPosition, minDistance, maxDistance, out spawnPoint, out spawnHeading, maxTries);
        }
        else
        {
            return FindSpawnPointForSlowMovingTarget(target, startPosition, minDistance, maxDistance, out spawnPoint, out spawnHeading, maxTries);
        }
    }

    private static bool FindSpawnPointForFastMovingTarget(Ped target, Vector3 startPosition, float minDistance, float maxDistance, out Vector3 spawnPoint, out float spawnHeading, int maxTries)
    {
        Vector2 velocity = target.Velocity;
        spawnHeading = 0f;
        spawnPoint = Vector3.Zero;

        for (int i = 0; i < maxTries; i++)
        {
            if (!FindSpawnPointInDirection(startPosition, new Vector3(velocity.X, velocity.Y, 0f), GetRandomFloat(minDistance, maxDistance), out var candidateSpawn))
            {
                continue;
            }

            if (IsSpawnPointValid(candidateSpawn))
            {
                spawnPoint = candidateSpawn;
                break;
            }
        }

        if (spawnPoint == Vector3.Zero)
            return false;

        spawnHeading = GetAngleBetweenTwoPoints(spawnPoint, target.Position);
        OptimizeSpawnPointWithVehicleNode(ref spawnPoint, ref spawnHeading, target.Position);

        return true;
    }

    private static bool FindSpawnPointForSlowMovingTarget(Ped target, Vector3 startPosition, float minDistance, float maxDistance, out Vector3 spawnPoint, out float spawnHeading, int maxTries)
    {
        spawnHeading = 0f;
        spawnPoint = Vector3.Zero;

        int attempts = 0;
        bool allowSwitchedOff = false;
        PathNode pathNode = null;

        while (attempts < maxTries)
        {
            Vector3 searchPos = startPosition.Around(GetRandomFloat(minDistance, maxDistance));
            pathNode = PathFind.GetClosestVehicleNode(searchPos, maxDistance,
                (flags) => allowSwitchedOff || (!flags.HasFlag(VehiclePathNodePropertyFlags.SwitchedOff) &&
                                              !flags.HasFlag(VehiclePathNodePropertyFlags.Boat) &&
                                              !flags.HasFlag(VehiclePathNodePropertyFlags.LeadsToDeadEnd)));

            if (pathNode != null)
            {
                Vector3 nodePos = pathNode.Position;
                if (IsSpawnPointValid(nodePos))
                {
                    spawnPoint = nodePos;
                    break;
                }
            }

            attempts++;
            allowSwitchedOff = attempts > maxTries / 2;
        }

        if (spawnPoint == Vector3.Zero)
            return false;

        spawnHeading = GetAngleBetweenTwoPoints(spawnPoint, target.Position);
        if (pathNode != null)
        {
            OptimizeSpawnPointWithVehicleNode(pathNode.Handle, ref spawnPoint, ref spawnHeading, target.Position);
        }

        return true;
    }

    private static bool IsSpawnPointValid(Vector3 position)
    {
        ShapeTestHandle shapeTest = ShapeTest.StartTestCapsule(position, position, 5f, IntersectFlags.Vehicles);
        ShapeTestResult result;

        while (shapeTest.GetResult(out result) == ShapeTestStatus.NonExistent)
        {
            Script.Yield();
        }

        return !result.DidHit && !IsSphereVisibleToPlayer(position, 5f);
    }

    private static void OptimizeSpawnPointWithVehicleNode(ref Vector3 spawnPoint, ref float spawnHeading, Vector3 targetPosition)
    {
        if (GetRandomVehicleNode(spawnPoint, 5f, 0, true, false, out _, out var nodeAddress))
        {
            GetSpawnCoordsForVehicleNode(nodeAddress, targetPosition, out var optimizedPos, out var optimizedHeading);
            if (optimizedPos != Vector3.Zero)
            {
                spawnPoint = optimizedPos;
                spawnHeading = optimizedHeading;
            }
        }
    }

    private static void OptimizeSpawnPointWithVehicleNode(int nodeHandle, ref Vector3 spawnPoint, ref float spawnHeading, Vector3 targetPosition)
    {
        GetSpawnCoordsForVehicleNode(nodeHandle, targetPosition, out var optimizedPos, out var optimizedHeading);
        if (optimizedPos != Vector3.Zero)
        {
            spawnPoint = optimizedPos;
            spawnHeading = optimizedHeading;
        }
    }

    public static bool FindSpawnPointForAircraft(Ped target, Vector3 startPosition, float minDistance, float maxDistance, float height, out Vector3 spawnPoint, out float spawnHeading, int maxTries = 3)
    {
        spawnPoint = Vector3.Zero;

        for (int i = 0; i < maxTries; i++)
        {
            Vector3 candidatePos = startPosition.Around(GetRandomFloat(minDistance, maxDistance));
            float targetHeight = candidatePos.Z + height;

            if (World.GetGroundHeight(new Vector3(candidatePos.X, candidatePos.Y, 1000f), out var groundHeight, GetGroundHeightMode.ConsiderWaterAsGroundNoWaves))
            {
                candidatePos.Z = Math.Max(groundHeight + Math.Max(height / 2f, 20f), targetHeight);
            }
            else
            {
                float approxHeight = World.GetApproxHeightForPoint(candidatePos);
                candidatePos.Z = Math.Max(approxHeight + Math.Max(height / 2f, 20f), targetHeight);
            }

            spawnPoint = candidatePos;
            if (!IsSphereVisibleToPlayer(candidatePos, 5f))
            {
                break;
            }
        }

        spawnHeading = GetAngleBetweenTwoPoints(spawnPoint, target.Position);
        return spawnPoint != Vector3.Zero;
    }

    public static bool FindSpawnPointForBoat(Ped target, Vector3 startPosition, float minDistance, float maxDistance, out Vector3 spawnPoint, out float spawnHeading, int maxTries = 30)
    {
        spawnPoint = Vector3.Zero;

        for (int i = 0; i < maxTries; i++)
        {
            Vector3 candidatePos = startPosition.Around(GetRandomFloat(minDistance, maxDistance));
            if (GetWaterLevelNoWaves(new Vector3(candidatePos.X, candidatePos.Y, 200f), out var waterHeight))
            {
                if (World.GetGroundHeight(new Vector3(candidatePos.X, candidatePos.Y, 1000f), out var groundHeight))
                {
                    // Ensure sufficient water depth
                    if (waterHeight - groundHeight >= 1f)
                    {
                        candidatePos.Z = waterHeight;
                        spawnPoint = candidatePos;
                        break;
                    }
                }
            }
        }

        spawnHeading = GetAngleBetweenTwoPoints(spawnPoint, target.Position);
        return spawnPoint != Vector3.Zero;
    }

    public static bool FindSpawnPointForSubmarine(Ped target, Vector3 startPosition, float minDistance, float maxDistance, out Vector3 spawnPoint, out float spawnHeading, int maxTries = 5)
    {
        spawnPoint = Vector3.Zero;

        for (int i = 0; i < maxTries; i++)
        {
            Vector3 candidatePos = startPosition.Around(GetRandomFloat(minDistance, maxDistance));
            if (GetWaterLevelNoWaves(new Vector3(candidatePos.X, candidatePos.Y, 200f), out var waterHeight))
            {
                if (World.GetGroundHeight(new Vector3(candidatePos.X, candidatePos.Y, 1000f), out var groundHeight))
                {
                    // Need deeper water for submarines
                    if (waterHeight - groundHeight >= 10f)
                    {
                        candidatePos.Z = Math.Max(groundHeight + 4f, Math.Min(candidatePos.Z, waterHeight - 4f));
                        spawnPoint = candidatePos;
                        break;
                    }
                }
            }
        }

        spawnHeading = GetAngleBetweenTwoPoints(spawnPoint, target.Position);
        return spawnPoint != Vector3.Zero;
    }
    #endregion

}
