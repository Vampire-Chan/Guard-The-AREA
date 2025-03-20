using GTA.Native;
using GTA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;
using static VehicleExtensions;

public static class HelperClass
{
    public static int GetWeaponComponentExtraCount(this WeaponComponent component, WeaponHash weapon)
    {
        return Function.Call<int>(Hash.GET_WEAPON_COMPONENT_VARIANT_EXTRA_COUNT, weapon);
    }
    public static bool IsTaskActive(this Ped ped, PedTask taskId)
    {
        return Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, new InputArgument[2]
        {
            ped.Handle,
            (int)taskId
        });
    }
    public static void SetPedCycleVehicleWeapon(this Ped ped)
    {
        Function.Call(Hash.SET_PED_CYCLE_VEHICLE_WEAPONS_ONLY, ped);
    }
    public static void GiveWeaponWithComponent(this WeaponHash weap, Ped ped, WeaponComponentHash component)
    {
        Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED, ped, weap, component);
    }

    public static void RemoveWeaponWithComponent(this WeaponHash weap, Ped ped, WeaponComponentHash component)
    {
        Function.Call(Hash.REMOVE_WEAPON_COMPONENT_FROM_PED, ped, weap, component);
    }

    public static void GiveSpecialAmmo(this WeaponHash weap, Ped ped, string ammotype)
    {
        Function.Call(Hash.ADD_PED_AMMO_BY_TYPE, ped, StringHash.AtStringHash(ammotype));
    }
    public static bool PlaceOnGround(this Prop prop)
    {
        return Function.Call<bool>(Hash.PLACE_OBJECT_ON_GROUND_PROPERLY, new InputArgument[1] { prop.Handle });
    }

    public static void ForceVehiclesToAvoid(this Prop prop, bool toggle)
    {
        Function.Call(Hash.SET_OBJECT_FORCE_VEHICLES_TO_AVOID, prop.Handle, toggle);
    }
    public static Relationship GetRelationshipBetweenGroups(int group1, int group2)
    {
        return (Relationship)Function.Call<int>(Hash.GET_RELATIONSHIP_BETWEEN_GROUPS, group1, group2);
    }

    // Set the relationship between two groups
    public static void SetRelationshipBetweenGroups(Relationship relationship, int group1, int group2)
    {
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, (int)relationship, group1, group2);
        Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, (int)relationship, group2, group1);
    }

    public static Vector3 GetHeliSpawnCoordinates(this Ped ped)
    {
        return Function.Call<Vector3>(Hash.FIND_SPAWN_COORDINATES_FOR_HELI, ped);
    }

    public static void StandGuard(this Ped ped, Vector3 defend, float heading, string anim_scenario)
    {
        Function.Call(Hash.TASK_STAND_GUARD, ped, defend.X, defend.Y, defend.Z, heading, anim_scenario);
    }
    public static void GuardCurrentPosition(this Ped ped, bool defensive)
    {
        Function.Call(Hash.TASK_GUARD_CURRENT_POSITION, ped, 40f, 35f, defensive);
    }

    public static void SetDriverAbility(this Ped p, float value)
    {
        Function.Call(Hash.SET_DRIVER_ABILITY, p, value);
    }
    public static void AssignDefaultTask(this Ped ped)
    {
        Function.Call(Hash.CLEAR_DEFAULT_PRIMARY_TASK, ped.Handle);
    }
    // Functions - 3. Functions
    public static void Subtitle(string msg)
    {
        GTA.UI.Screen.ShowSubtitle(msg);
    }

    public static void Notification(string msg)
    {
        GTA.UI.Notification.PostTicker(msg, false);
    }

    public static Vehicle CreateVehicle(VehicleInformation info, Vector3 pos, float head)
    {
        try
        {
            if (info == null) throw new Exception("Invalid Vehicle Information");
            var v = World.CreateVehicle(info.VehicleDetails.Name, pos, head);

            if (v != null) // Ensure vehicle creation was successful
            {
                v.InstallModKit(); //install modkit first

                //// Apply Vehicle Mods
                //if (info.VehicleDetails.VehicleMods != null && info.VehicleDetails.VehicleMods.Count > 0)
                //{
                //    foreach (var mod in info.VehicleDetails.VehicleMods)
                //    {
                //        if (TryParseModType(mod.Type, out ModType modTypeEnum)) //Convert string to ModType enum
                //        {
                //            v.SetVehicleMod(modTypeEnum, mod.Index, false); // Apply each mod
                //        }
                //        else
                //        {
                //            // Handle invalid ModType string if needed, e.g., log an error
                //            //Game.Console.output($"[WARNING] Invalid ModType string: {mod.Type}");
                //        }
                //    }
                //}

                //// Apply Vehicle Liveries
                //if (info.VehicleDetails.VehicleLiveries != null && info.VehicleDetails.VehicleLiveries.Count > 0)
                //{
                //    foreach (var livery in info.VehicleDetails.VehicleLiveries)
                //    {
                //        if (livery.Set == "Livery")
                //        {
                //            v.SetLivery(0, livery.Index); // Apply Livery
                //        }
                //        else if (livery.Set == "Livery2")
                //        {
                //            v.SetLivery(1, livery.Index); // Apply Livery2
                //        }
                //        else
                //        {
                //            // Handle invalid Livery set string if needed, e.g., log a warning
                //            //Game.Console.output($"[WARNING] Invalid Livery Set string: {livery.Set}");
                //        }
                //    }
                //}


                //if (info.VehicleDetails.VehicleHealth.Engine.HasValue) v.EngineHealth = info.VehicleDetails.VehicleHealth.Engine.Value;
                //if (info.VehicleDetails.VehicleHealth.Body.HasValue) v.BodyHealth = info.VehicleDetails.VehicleHealth.Body.Value;
                //if (info.VehicleDetails.VehicleHealth.Petrol.HasValue) v.PetrolTankHealth = info.VehicleDetails.VehicleHealth.Petrol.Value;

                //// Apply Vehicle Weapons (Placeholder - Implementation Needed) - to be applied during Peds Initialization.
                //if (info.VehicleDetails.VehicleWeaponDetailsList != null && info.VehicleDetails.VehicleWeaponDetailsList.Count > 0)
                //{
                //    // TODO: Implement vehicle weapon handling logic here
                //    // This might involve attaching weapon entities to the vehicle,
                //    // setting up weapon parameters, etc.
                //    //Game.Console.output("[INFO] Vehicle Weapons configuration is not yet implemented in CreateVehicle function.");
                //}

                var p = v.CreatePed(info.PilotInfo.PilotModelList[new Random().Next(0, info.SoldierInfo.SoldierModels.Count)], info.SoldierInfo.WeaponInfoList[new Random().Next(0, info.SoldierInfo.WeaponInfoList.Count)], VehicleSeat.Driver, PedType.Cop);

                //AIManager.Cops.Add(p); // Add driver to Cops list in AIManager
                for (int i = 0; i < v.PassengerCapacity-1; i++)
                {
                    var ped = v.CreatePed(info.SoldierInfo.SoldierModels[new Random().Next(0, info.SoldierInfo.SoldierModels.Count)], info.SoldierInfo.WeaponInfoList[new Random().Next(0, info.SoldierInfo.WeaponInfoList.Count)], (VehicleSeat)i, PedType.Cop);
                    //AIManager.Cops.Add(ped);
                }
            }
            return v;
        }
        catch (Exception ex)
        {
            Notification($"{ex.Message}, {ex.InnerException}, {ex.StackTrace}");
            return null; // Vehicle creation failed
        } 
        
    }

    public static Ped CreatePed(this Vehicle veh, string pedModelName, WeaponInformation info, VehicleSeat seat, PedType type)
    {
        try
        {
            var pedModel = new Model(pedModelName);
            pedModel.Request(1000);

            if (!pedModel.IsValid || !pedModel.IsInCdImage)
                throw new ArgumentException($"Invalid ped model: {pedModelName}");

            var ped = Function.Call<Ped>(Hash.CREATE_PED_INSIDE_VEHICLE, veh, (int)type, pedModel, seat, 1, 1);
            ped.Model.MarkAsNoLongerNeeded();
            
            ped.SetAsCop(true);

            //ped.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, true);
            //ped.SetCombatAttribute(CombatAttributes.AlwaysFlee, false);
            //ped.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, true);
            ////ped.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, false);  //use this true for heli/plane
            //if (veh.ClassType == VehicleClass.Helicopters || veh.ClassType == VehicleClass.Planes)
            //{
            //    ped.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, false);
            //    ped.SetCombatAttribute(CombatAttributes.ForceCheckAttackAngleForMountedGuns, true);
            //    ped.SetCombatAttribute(CombatAttributes.PreferAirCombatWhenInAircraft, true);
            //}
            //ped.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true);
            //ped.SetCombatAttribute(CombatAttributes.RequiresLosToShoot, true);

            //if (ped.PedType == PedType.Swat || ped.PedType == PedType.Army) ped.SetCombatAttribute(CombatAttributes.CanThrowSmokeGrenade, true); //use for swat/army type ped only

            //if (veh.ClassType == VehicleClass.Boats)
            //{
            //    ped.SetCombatAttribute(CombatAttributes.CanSeeUnderwaterPeds, true);
            //}

            //ped.SetCombatAttribute(CombatAttributes.MoveToLocationBeforeCoverSearch, true);

            //ped.SetCombatAttribute(CombatAttributes.PreferNonAircraftTargets, true); //for ground ones, non air peds.

            //ped.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
            //ped.SetConfigFlag(PedConfigFlagToggles.CreatedByDispatch, true);
            //ped.SetConfigFlag(PedConfigFlagToggles.LawWillOnlyAttackIfPlayerIsWanted, true);
            //ped.SetConfigFlag(PedConfigFlagToggles.KeepRelationshipGroupAfterCleanUp, true);
            //ped.PopulationType = EntityPopulationType.RandomAmbient;

            if (seat != VehicleSeat.Driver)
            {
                if (info.SecondaryWeaponList != null)
                {
                    foreach (var gun in info.SecondaryWeaponList)
                    {
                        ped.Weapons.Give(gun.Name, gun.Ammo, true, true);
                        //component setups here.
                    }
                }
                if (info.PrimaryWeaponList != null)
                {
                    foreach (var gun in info.PrimaryWeaponList)
                    {
                        ped.Weapons.Give(gun.Name, gun.Ammo, true, true);
                        
                        //component setups here.
                    }
                }
            }
            else
            {
                if (info.PrimaryWeaponList != null)
                {
                    foreach (var gun in info.PrimaryWeaponList)
                    {
                        ped.Weapons.Give(gun.Name, gun.Ammo, true, true);
                        //component setups here.
                    }
                }
            }


            return ped;
        }
        catch (Exception ex)
        {
            Notification($"{ex.Message}, {ex.InnerException}, {ex.StackTrace}");
            return null;
        }
    }

    public static Vector3 FindEmergencyLandingSpotForPed(Ped ped)
    {
        Vector3 position = ped.Position;
        float approxHeightForPoint = World.GetApproxHeightForPoint(position);
        Vector3 vector = RandomPointInsideCircle(ped.GetOffsetPosition(new Vector3(0f, Math.Max(position.Z - approxHeightForPoint, 50f), 0f)), 25f);
        if (!World.GetSafePositionForPed(vector, out var safePosition, GetSafePositionFlags.NotInterior | GetSafePositionFlags.NotWater | GetSafePositionFlags.OnlyNetworkSpawn) && !World.GetSafePositionForPed(vector, out safePosition, GetSafePositionFlags.NotInterior | GetSafePositionFlags.OnlyNetworkSpawn))
        {
            return vector;
        }
        return safePosition;
    }

    public static Vector3 FindSearchPointForAutomobile(Vector3 vStartPosition, float fMaxRadius, bool bUseLastSeenPosition = false)
    {
        Vector3 position = RandomPointInsideCircle(vStartPosition, fMaxRadius);
        if (bUseLastSeenPosition)
        {
            Vector3 vector = Game.Player.WantedCenterPosition.Around(40f);
            if (vector.DistanceTo2D(vStartPosition) < fMaxRadius)
            {
                position = vector;
            }
            else
            {
                Vector3 position2 = Game.Player.Character.Position;
                float num = Math.Max(fMaxRadius / 2f, 120f);
                position = GetPointBetweenTwoVectors(position2, vector, num / position2.DistanceTo2D(vector));
            }
        }
        return World.GetNextPositionOnStreet(position);
    }

    public static Vector3 FindSearchPointForBoat(Vector3 vStartPosition, float fMaxRadius, bool bUseLastSeenPosition = false)
    {
        Vector3 result = RandomPointInsideCircle(vStartPosition, fMaxRadius);
        if (bUseLastSeenPosition)
        {
            Vector3 vector = Game.Player.WantedCenterPosition.Around(40f);
            if (vector.DistanceTo2D(vStartPosition) < fMaxRadius)
            {
                result = vector;
            }
            else
            {
                Vector3 position = Game.Player.Character.Position;
                float num = Math.Max(fMaxRadius / 2f, 120f);
                result = GetPointBetweenTwoVectors(position, vector, num / position.DistanceTo2D(vector));
            }
        }
        result.Z = vStartPosition.Z;
        if (GetWaterLevelNoWaves(new Vector3(result.X, result.Y, 200f), out var fHeight))
        {
            result.Z = fHeight;
        }
        return result;
    }

    public static int GetWantedLevelThreshold(int wantedLvl)
    {
        return Function.Call<int>(Hash.GET_WANTED_LEVEL_THRESHOLD, new InputArgument[1] { wantedLvl });
    }

    public static int GetNumberOfResourcesAssignedToWantedLvl(DispatchType dispatchType)
    {
        return Function.Call<int>(Hash.GET_NUMBER_RESOURCES_ALLOCATED_TO_WANTED_LEVEL, new InputArgument[1] { (int)dispatchType });
    }

    public static bool DoesScenarioExistInArea(Vector3 vSearchArea, float fRadius, bool bUnoccupied)
    {
        return Function.Call<bool>(Hash.DOES_SCENARIO_EXIST_IN_AREA, new InputArgument[5] { vSearchArea.X, vSearchArea.Y, vSearchArea.Z, fRadius, bUnoccupied });
    }

    public static bool IsSphereVisibleToPlayer(Vector3 vCenter, float fRadius)
    {
        return Function.Call<bool>(Hash.IS_SPHERE_VISIBLE, new InputArgument[4] { vCenter.X, vCenter.Y, vCenter.Z, fRadius });
    }

    public static int AddSpeedZone(Vector3 position, float radius, float speed, bool affectsMissionVehs = false)
    {
        return Function.Call<int>(Hash.ADD_ROAD_NODE_SPEED_ZONE, new InputArgument[6] { position.X, position.Y, position.Z, radius, speed, affectsMissionVehs });
    }

    public static void RemoveSpeedZone(int id)
    {
        Function.Call(Hash.REMOVE_ROAD_NODE_SPEED_ZONE, id);
    }

    //use OutputArgument instead of NativeVector3 thingy (original ScriptHookVDotNet way
    public static unsafe bool FindSpawnPointInDirection(Vector3 vPosition, Vector3 vDirection, float fIdealDistance, out Vector3 vSpawnPoint)
    {
        OutputArgument nativeVector = new OutputArgument();
        if (Function.Call<bool>(Hash.FIND_SPAWN_POINT_IN_DIRECTION, new InputArgument[8]
        {
                vPosition.X,
                vPosition.Y,
                vPosition.Z,
                vDirection.X,
                vDirection.Y,
                vDirection.Z,
                fIdealDistance,
                nativeVector
        }))
        {
            vSpawnPoint = nativeVector.GetResult<Vector3>();
            return true;
        }
        vSpawnPoint = Vector3.Zero;
        return false;
    }

    public static unsafe bool GetWaterLevelNoWaves(Vector3 vStartPoint, out float fHeight)
    {
        fHeight = 0f;
        OutputArgument num = new OutputArgument();
        if (Function.Call<bool>(Hash.GET_WATER_HEIGHT_NO_WAVES, new InputArgument[4]
        {
                vStartPoint.X,
                vStartPoint.Y,
                vStartPoint.Z,
                num
        }))
        {
            fHeight = num.GetResult<float>();
            return true;
        }
        return false;
    }

    public static unsafe void GetSpawnCoordsForVehicleNode(int iNodeAddress, Vector3 vTargetDirection, out Vector3 vSpawnPosition, out float fHeading)
    {//same thing OutputArguemnt
        OutputArgument nativeVector = new OutputArgument();
        OutputArgument num = new OutputArgument();
        Function.Call(Hash.GET_SPAWN_COORDS_FOR_VEHICLE_NODE, iNodeAddress, vTargetDirection.X, vTargetDirection.Y, vTargetDirection.Z, nativeVector, num);
        vSpawnPosition = nativeVector.GetResult<Vector3>();
        fHeading = num.GetResult<float>();
    }

    public static unsafe bool GetRandomVehicleNode(Vector3 vCenter, float fRadius, int iMinLanes, bool bAvoidDeadEnds, bool bAvoidHighways, out Vector3 vNodePosition, out int iNodeAddress)
    { //here as well OutputArg
        iNodeAddress = 0;
        vNodePosition = Vector3.Zero;
        OutputArgument num = new OutputArgument();
        OutputArgument nativeVector = new OutputArgument();
        if (Function.Call<bool>(Hash.GET_RANDOM_VEHICLE_NODE, new InputArgument[9]
        {
                vCenter.X,
                vCenter.Y,
                vCenter.Z,
                fRadius,
                iMinLanes,
                bAvoidDeadEnds,
                bAvoidHighways,
                nativeVector,
                num
        }))
        {
            iNodeAddress = num.GetResult<int>();
            vNodePosition = nativeVector.GetResult<Vector3>();
            return true;
        }
        return false;
    }
    public static Vector3 FindSearchPointForHelicopter(Vector3 vStartPosition, float fMaxRadius, float fHeight, bool bUseLastSeenPosition = false)
    {
        Vector3 result = RandomPointInsideCircle(vStartPosition, fMaxRadius);
        if (bUseLastSeenPosition)
        {
            Vector3 vector = Game.Player.WantedCenterPosition.Around(40f);
            if (vector.DistanceTo2D(vStartPosition) < fMaxRadius)
            {
                result = vector;
            }
            else
            {
                Vector3 position = Game.Player.Character.Position;
                float num = Math.Max(fMaxRadius / 2f, 120f);
                result = GetPointBetweenTwoVectors(position, vector, num / position.DistanceTo2D(vector));
            }
        }
        result.Z += fHeight;
        return result;
    }

    public static Vector3 FindSearchPointForPlane(Vector3 vStartPosition, float fMaxRadius, float fHeight, bool bUseLastSeenPosition = false)
    {
        Vector3 result = FindSearchPointForHelicopter(vStartPosition, fMaxRadius, fHeight, bUseLastSeenPosition);
        Vector3 vector = new Vector3(result.X, result.Y, 1000f);
        if (!World.GetGroundHeight(vector, out var height, GetGroundHeightMode.ConsiderWaterAsGroundNoWaves))
        {
            height = World.GetApproxHeightForPoint(vector);
        }
        result.Z = Math.Max(height + fHeight, result.Z);
        return result;
    }

    public static Vector3 FindSearchPointForSubmarine(Vector3 vStartPosition, float fMaxRadius, bool bUseLastSeenPosition = false)
    {
        Vector3 result = RandomPointInsideCircle(vStartPosition, fMaxRadius);
        if (bUseLastSeenPosition)
        {
            Vector3 vector = Game.Player.WantedCenterPosition.Around(40f);
            if (vector.DistanceTo2D(vStartPosition) < fMaxRadius)
            {
                result = vector;
            }
            else
            {
                Vector3 position = Game.Player.Character.Position;
                float num = Math.Max(fMaxRadius / 2f, 120f);
                result = GetPointBetweenTwoVectors(position, vector, num / position.DistanceTo2D(vector));
            }
        }
        Vector3 vector2 = new Vector3(result.X, result.Y, 200f);
        if (!GetWaterLevelNoWaves(vector2, out var fHeight))
        {
            return Vector3.Zero;
        }
        result.Z = Math.Min(result.Z, fHeight - 10f);
        if (World.GetGroundHeight(vector2, out var height))
        {
            result.Z = Math.Max(result.Z, height + 15f);
        }
        return result;
    }

    public static bool FindSpawnPointForAutomobile(Ped pTarget, Vector3 vStartPosition, float fMinDistance, float fMaxDistance, out Vector3 vSpawnPoint, out float fSpawnHeading, int iMaxTries = 5)
    {
        float speed = pTarget.Speed;
        Vector3 position = pTarget.Position;
        if (speed >= 14f)
        {
            Vector2 vector = pTarget.Velocity;
            fSpawnHeading = 0f;
            vSpawnPoint = Vector3.Zero;
            for (int i = 0; i < iMaxTries; i++)
            {
                if (!FindSpawnPointInDirection(vStartPosition, new Vector3(vector.X, vector.Y, 0f), GetRandomFloat(fMinDistance, fMaxDistance), out var vSpawnPoint2))
                {
                    continue;
                }
                ShapeTestHandle shapeTestHandle = ShapeTest.StartTestCapsule(vSpawnPoint2, vSpawnPoint2, 5f, IntersectFlags.Vehicles);
                ShapeTestStatus shapeTestStatus = ShapeTestStatus.NonExistent;
                ShapeTestResult result = default;
                if (!shapeTestHandle.IsRequestFailed)
                {
                    while ((shapeTestStatus = shapeTestHandle.GetResult(out result)) == ShapeTestStatus.NonExistent)
                    {
                        Script.Yield();
                    }
                }
                if (shapeTestStatus == ShapeTestStatus.Ready && !result.DidHit)
                {
                    vSpawnPoint = vSpawnPoint2;
                    if (!IsSphereVisibleToPlayer(vSpawnPoint2, 5f))
                    {
                        break;
                    }
                }
            }
            if (vSpawnPoint == Vector3.Zero)
            {
                return false;
            }
            fSpawnHeading = GetAngleBetweenTwoPoints(vSpawnPoint, position);
            if (GetRandomVehicleNode(vSpawnPoint, 5f, 0, bAvoidDeadEnds: true, bAvoidHighways: false, out var _, out var iNodeAddress))
            {
                GetSpawnCoordsForVehicleNode(iNodeAddress, position, out var vSpawnPosition, out var fHeading);
                if (vSpawnPosition != Vector3.Zero)
                {
                    vSpawnPoint = vSpawnPosition;
                    fSpawnHeading = fHeading;
                }
            }
        }
        else
        {
            fSpawnHeading = 0f;
            vSpawnPoint = Vector3.Zero;
            int num = 0;
            bool bAllowSwitchedOff = false;
            PathNode pathNode = null;
            while (num < iMaxTries)
            {
                if ((pathNode = PathFind.GetClosestVehicleNode(vStartPosition.Around(GetRandomFloat(fMinDistance, fMaxDistance)), fMaxDistance, (flags) => bAllowSwitchedOff || !flags.HasFlag(VehiclePathNodePropertyFlags.SwitchedOff) && !flags.HasFlag(VehiclePathNodePropertyFlags.Boat) && !flags.HasFlag(VehiclePathNodePropertyFlags.LeadsToDeadEnd))) != null)
                {
                    Vector3 position2 = pathNode.Position;
                    ShapeTestHandle shapeTestHandle2 = ShapeTest.StartTestCapsule(position2, position2, 5f, IntersectFlags.Vehicles);
                    ShapeTestStatus shapeTestStatus2 = ShapeTestStatus.NonExistent;
                    ShapeTestResult result2 = default;
                    if (!shapeTestHandle2.IsRequestFailed)
                    {
                        while ((shapeTestStatus2 = shapeTestHandle2.GetResult(out result2)) == ShapeTestStatus.NonExistent)
                        {
                            Script.Yield();
                        }
                    }
                    if (shapeTestStatus2 == ShapeTestStatus.Ready && !result2.DidHit)
                    {
                        vSpawnPoint = position2;
                        if (!IsSphereVisibleToPlayer(position2, 5f))
                        {
                            break;
                        }
                    }
                }
                num++;
                bAllowSwitchedOff = num > iMaxTries / 2;
            }
            if (vSpawnPoint == Vector3.Zero)
            {
                return false;
            }
            fSpawnHeading = GetAngleBetweenTwoPoints(vSpawnPoint, position);
            GetSpawnCoordsForVehicleNode(pathNode.Handle, position, out var vSpawnPosition2, out var fHeading2);
            if (vSpawnPosition2 != Vector3.Zero)
            {
                vSpawnPoint = vSpawnPosition2;
                fSpawnHeading = fHeading2;
            }
        }
        return true;
    }

    public static bool FindSpawnPointForAircraft(Ped pTarget, Vector3 vStartPosition, float fMinDistance, float fMaxDistance, float fHeight, out Vector3 vSpawnPoint, out float fSpawnHeading, int iMaxTries = 3)
    {
        vSpawnPoint = Vector3.Zero;
        for (int i = 0; i < iMaxTries; i++)
        {
            Vector3 vector = vStartPosition.Around(GetRandomFloat(fMinDistance, fMaxDistance));
            float val = vector.Z + fHeight;
            if (World.GetGroundHeight(new Vector3(vector.X, vector.Y, 1000f), out var height, GetGroundHeightMode.ConsiderWaterAsGroundNoWaves))
            {
                vector.Z = Math.Max(height + Math.Max(fHeight / 2f, 20f), val);
            }
            else
            {
                float approxHeightForPoint = World.GetApproxHeightForPoint(vector);
                vector.Z = Math.Max(approxHeightForPoint + Math.Max(fHeight / 2f, 20f), val);
            }
            vSpawnPoint = vector;
            if (!IsSphereVisibleToPlayer(vector, 5f))
            {
                break;
            }
        }
        fSpawnHeading = GetAngleBetweenTwoPoints(vSpawnPoint, pTarget.Position);
        return vSpawnPoint != Vector3.Zero;
    }

    public static bool FindSpawnPointForBoat(Ped pTarget, Vector3 vStartPosition, float fMinDistance, float fMaxDistance, out Vector3 vSpawnPoint, out float fSpawnHeading, int iMaxTries = 5)
    {
        vSpawnPoint = Vector3.Zero;
        for (int i = 0; i < iMaxTries; i++)
        {
            Vector3 vector = vStartPosition.Around(GetRandomFloat(fMinDistance, fMaxDistance));
            if (GetWaterLevelNoWaves(new Vector3(vector.X, vector.Y, 200f), out var fHeight))
            {
                if (!World.GetGroundHeight(new Vector3(vector.X, vector.Y, 1000f), out var height))
                {
                    height = World.GetApproxHeightForPoint(vSpawnPoint);
                }
                if (!(fHeight - height < 1f))
                {
                    vector.Z = fHeight;
                    vSpawnPoint = vector;
                    break;
                }
            }
        }
        fSpawnHeading = GetAngleBetweenTwoPoints(vSpawnPoint, pTarget.Position);
        return vSpawnPoint != Vector3.Zero;
    }

    public static float NormalizeAngle(float value)
    {
        float num = 360f;
        return value - (float)Math.Floor(value / num) * num;
    }

    public static Vector3 GetPointBetweenTwoVectors(Vector3 start, Vector3 end, float ratio)
    {
        Vector3 result = new Vector3(start.X, start.Y, start.Z);
        result.X = start.X > end.X ? result.X - Math.Abs(end.X - start.X) * ratio : result.X + Math.Abs(end.X - start.X) * ratio;
        result.Y = start.Y > end.Y ? result.Y - Math.Abs(end.Y - start.Y) * ratio : result.Y + Math.Abs(end.Y - start.Y) * ratio;
        result.Z = start.Z > end.Z ? result.Z - Math.Abs(end.Z - start.Z) * ratio : result.Z + Math.Abs(end.Z - start.Z) * ratio;
        return result;
    }

    public static bool IsAnglePointLookingAtPoint(Vector3 positionSource, Vector3 targetPoint, float sourceXHeading, float sourceYHeading, float angleX, float angleY)
    {
        float angleBetweenTwo2DPoints = GetAngleBetweenTwo2DPoints(positionSource.X, positionSource.Y, targetPoint.X, targetPoint.Y);
        float angleBetweenTwo2DPoints2 = GetAngleBetweenTwo2DPoints(positionSource.X, positionSource.Z, targetPoint.X, targetPoint.Z);
        if (Math.Abs(NormalizeAngle(angleBetweenTwo2DPoints) - NormalizeAngle(sourceXHeading)) < angleX)
        {
            return Math.Abs(NormalizeAngle(angleBetweenTwo2DPoints2) - NormalizeAngle(sourceYHeading)) < angleY;
        }
        return false;
    }
    public static Vector3 RotateVector(Vector3 input, Vector3 rotation)
    {
        input.Y = (float)(Math.Cos(rotation.X) * input.Y - Math.Sin(rotation.X) * input.Z);
        input.Z = (float)(Math.Sin(rotation.X) * input.Y + Math.Cos(rotation.X) * input.Z);
        input.X = (float)(Math.Cos(rotation.Y) * input.X + Math.Sin(rotation.Y) * input.Z);
        input.Z = (float)(Math.Cos(rotation.Y) * input.Z - Math.Sin(rotation.Y) * input.X);
        input.X = (float)(Math.Cos(rotation.Z) * input.X - Math.Sin(rotation.Z) * input.Y);
        input.Y = (float)(Math.Sin(rotation.Z) * input.X + Math.Cos(rotation.Z) * input.Y);
        return input;
    }

    public static bool IsAnglePointLookingAtPoint(Vector3 positionSource, Vector3 targetPoint, float sourceHeading, float angle)
    {
        return Math.Abs(NormalizeAngle(GetAngleBetweenTwoPoints(positionSource, targetPoint)) - NormalizeAngle(sourceHeading)) < angle;
    }

    public static float GetAngleBetweenTwoPoints(Vector3 source, Vector3 target)
    {
        return (target - source).Normalized.ToHeading();
    }

    public static float GetAngleBetweenTwoPoints(Vector2 source, Vector2 target)
    {
        return (float)(Math.Atan2(target.Y - source.Y, target.X - source.X) * (180.0 / Math.PI));
    }

    public static float GetAngleBetweenTwo2DPoints(float sourceX, float sourceY, float targetX, float targetY)
    {
        return (float)(Math.Atan2(targetY - sourceY, targetX - sourceX) * (180.0 / Math.PI));
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
    public static Vector3 RandomPointInsideCircle(Vector3 center, float radius)
    {
        double num = (double)radius * Math.Sqrt(GetDouble());
        double num2 = GetDouble() * 2.0 * Math.PI;
        return new Vector3(center.X + (float)(num * Math.Cos(num2)), center.Y + (float)(num * Math.Sin(num2)), center.Z);
    }

    public static bool FindSpawnPointForSubmarine(Ped pTarget, Vector3 vStartPosition, float fMinDistance, float fMaxDistance, out Vector3 vSpawnPoint, out float fSpawnHeading, int iMaxTries = 5)
    {
        vSpawnPoint = Vector3.Zero;
        for (int i = 0; i < iMaxTries; i++)
        {
            Vector3 vector = vStartPosition.Around(GetRandomFloat(fMinDistance, fMaxDistance));
            if (GetWaterLevelNoWaves(new Vector3(vector.X, vector.Y, 200f), out var fHeight))
            {
                if (!World.GetGroundHeight(new Vector3(vector.X, vector.Y, 1000f), out var height))
                {
                    height = World.GetApproxHeightForPoint(vSpawnPoint);
                }
                if (!(fHeight - height < 10f))
                {
                    vector.Z = Math.Max(height + 4f, Math.Min(vector.Z, fHeight - 4f));
                    vSpawnPoint = vector;
                    break;
                }
            }
        }
        fSpawnHeading = GetAngleBetweenTwoPoints(vSpawnPoint, pTarget.Position);
        return vSpawnPoint != Vector3.Zero;
    }
}