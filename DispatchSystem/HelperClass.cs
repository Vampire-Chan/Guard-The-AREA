using GTA.Native;
using GTA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guarding.DispatchSystem
{

    public class HelperClass
    {

        public enum PedRelationship : byte
        {
            None = 255,
            Respect = 0,
            Like,         //  1
            Ignore,       //  2
            Dislike,      //  3
            Wanted,       //  4       
            Hate,         //  5
            Dead          //  6   

        }

        public static void Subtitle(string msg)
        {
            GTA.UI.Screen.ShowSubtitle(msg);
        }

        public static void Notification(string msg)
        {
            GTA.UI.Notification.PostTicker(msg, false);
        }

        public static Ped CreateAndAssignPedPassenger(Vehicle veh, string pedModelName, VehicleSeat seat, bool ascop)
        {
            var pedModel = new Model(pedModelName);
            pedModel.Request(1000);

            if (!pedModel.IsValid || !pedModel.IsInCdImage)
                throw new ArgumentException($"Invalid ped model: {pedModelName}");

            var ped = Function.Call<Ped>(Hash.CREATE_PED_INSIDE_VEHICLE, veh, (int)PedType.Cop, pedModel, seat, 1, 1);
           
            ped.Model.MarkAsNoLongerNeeded();
            ped.MarkAsNoLongerNeeded();
             
            if(ascop) ped.SetAsCop(true);

            ped.SetCombatAttribute(CombatAttributes.CanFightArmedPedsWhenNotArmed, true);
            ped.SetCombatAttribute(CombatAttributes.AlwaysFlee, false);
            ped.SetCombatAttribute(CombatAttributes.CanLeaveVehicle, false);
            ped.SetCombatAttribute(CombatAttributes.RequiresLosToShoot, true);
            ped.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true);
            ped.SetCombatAttribute(CombatAttributes.ForceCheckAttackAngleForMountedGuns, true);

            ped.SeeingRange = 70;
            ped.HearingRange = 100;

            ped.FiringPattern = FiringPattern.FullAuto;
            ped.CombatAbility = CombatAbility.Average;

            ped.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
            ped.SetConfigFlag(PedConfigFlagToggles.CreatedByFactory, true);
            ped.SetConfigFlag(PedConfigFlagToggles.LawWillOnlyAttackIfPlayerIsWanted, true);
         
            //var grp = StringHash.AtStringHash("COP");
            //RelationshipGroup relationshipGroup = grp;
            //Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, grp, grp);
            //Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, grp, 1862763509);
            //Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, 1862763509, grp);

            return ped;
        }

        public static bool ControlMountedWeapon(Ped ped)
        {
            return Function.Call<bool>(Hash.CONTROL_MOUNTED_WEAPON, ped);
        }

        public static bool SetVehicleWeaponHash(Ped ped, VehicleWeaponHash WeapHash)
        {
            bool nat = Function.Call<bool>(GTA.Native.Hash.SET_CURRENT_PED_VEHICLE_WEAPON, ped, WeapHash);
            return nat;
        }

        public static void DisableVehicleWeapon(VehicleWeaponHash weaponHash, Ped ped, Vehicle veh)
        {
            Function.Call(Hash.DISABLE_VEHICLE_WEAPON, true, weaponHash, veh, ped);
        }

        public static void EnableVehicleWeapon(VehicleWeaponHash weaponHash, Ped ped, Vehicle veh)
        {
            Function.Call(Hash.DISABLE_VEHICLE_WEAPON, false, weaponHash, veh, ped);
        }

        public static void AssignWeapons(Ped ped, List<string> primaryWeapons, List<string> secondaryWeapons)
        {
            var random = new Random();

            // Assign a random primary weapon if available
            if (primaryWeapons != null && primaryWeapons.Count > 0)
            {
                var primaryWeaponName = primaryWeapons[random.Next(primaryWeapons.Count)];

                ped.Weapons.Give(primaryWeaponName, 900, true, true);
               
            }

            // Assign a random secondary weapon if available
            if (secondaryWeapons != null && secondaryWeapons.Count > 0)
            {
                var secondaryWeaponName = secondaryWeapons[random.Next(secondaryWeapons.Count)];


                ped.Weapons.Give(secondaryWeaponName, 200, false, true);
              
            }
        }

        public static void EnableHeliWeapons(Vehicle helicopter)
        {
            // we give and then allow to use them.
            helicopter.Driver.SetCombatAttribute(CombatAttributes.PreferAirCombatWhenInAircraft, true); //this flag is set when player is in aircraft
            helicopter.Driver.SetCombatAttribute(CombatAttributes.UseVehicleAttackIfVehicleHasMountedGuns, true);
            helicopter.Driver.SetCombatAttribute(CombatAttributes.ForceCheckAttackAngleForMountedGuns, true);
            helicopter.Driver.SetCombatAttribute(CombatAttributes.UseRocketsAgainstVehiclesOnly, true);
            helicopter.Driver.SetCombatAttribute(CombatAttributes.UseVehicleAttack, true);
        }

        public static void DisableHeliWeapons(Vehicle helicopter)
        {
            //remove their ability to use them
            helicopter.Driver.SetCombatAttribute(CombatAttributes.PreferAirCombatWhenInAircraft, false); //this flag is set when player is in aircraft
            helicopter.Driver.SetCombatAttribute(CombatAttributes.UseVehicleAttackIfVehicleHasMountedGuns, false);
            helicopter.Driver.SetCombatAttribute(CombatAttributes.UseRocketsAgainstVehiclesOnly, false);
            helicopter.Driver.SetCombatAttribute(CombatAttributes.UseVehicleAttack, false);

        }

        public static bool GetVehicleWeaponHash(Ped ped, out VehicleWeaponHash WeapHash)
        {
            OutputArgument arg = new();
            bool nat = Function.Call<bool>(GTA.Native.Hash.GET_CURRENT_PED_VEHICLE_WEAPON, ped, arg);
            WeapHash = arg.GetResult<VehicleWeaponHash>();
            return nat;
        }

        public static void EnableAttack(Vehicle helicopter)
        {
            EnableHeliWeapons(helicopter);
        }

        public static void DisableAttack(Vehicle helicopter)
        {
            DisableHeliWeapons(helicopter);
            }

        public static bool IsPedRappelingFromHelicopter(Vehicle helicopter)
        {
            return Function.Call<bool>(Hash.IS_ANY_PED_RAPPELLING_FROM_HELI, helicopter);  //is used to check if rappel is happening by ped or not.
        }

    }
}
