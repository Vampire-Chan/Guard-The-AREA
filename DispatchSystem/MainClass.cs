using GTA;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System;
using GTA.Native;
using System.Windows.Forms;


public class DispatchSystemManager : Script
    {
        private DispatchManager dispatchManager;
        public DispatchSystemManager()
        {
            Tick += OnTick;
            InitializeDispatchManager();
         // KeyDown += OnKeyDown;   

    }

        public void InitializeDispatchManager()
        {
            string xmlFilePath = "./scripts/DispatchData.xml";
            var wantedStarData = XMLDataLoader.LoadDispatchData(xmlFilePath);
            dispatchManager = new(wantedStarData);
        }

        private void OnTick(object sender, EventArgs e)
        {
        if(PlayerPositionLogger._isNewHeliDispatchEnabled)
            dispatchManager.UpdateDispatch();
        }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F7)
        {
            var heli = World.CreateVehicle("buzzard", Game.Player.Character.Position.Around(50) + GTA.Math.Vector3.WorldUp * 50);
            var ped = heli.CreatePedOnSeat(VehicleSeat.Driver, "s_m_y_cop_01");
            heli.HeliBladesSpeed = 1.0f;
            heli.IsEngineRunning = true;

            var ped2 = World.CreatePed("s_m_y_cop_01", Game.Player.Character.Position.Around(300));
            var ped3 = World.CreatePed("s_m_y_cop_01", Game.Player.Character.Position.Around(300));
          //  heli.Driver.Task.StartHeliMission(Game.Player.Character.Position.Around(30));
            ped.Task.WarpIntoVehicle(heli, VehicleSeat.Driver);
            ped3.Task.WarpIntoVehicle(heli, VehicleSeat.RightRear);
            ped2.Task.WarpIntoVehicle(heli, VehicleSeat.LeftRear);
            ped2.SetAsCop(true);
            ped.SetAsCop(true);
            ped3.SetAsCop(true);
            ped.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
            ped2.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
            ped3.SetCombatAttribute(CombatAttributes.CanDoDrivebys, true);
            ped.Task.Combat(Game.Player.Character);
            Function.Call(Hash.GIVE_LOADOUT_TO_PED, ped, StringHash.AtStringHash("LOADOUT_COP_L3"));
            Function.Call(Hash.GIVE_LOADOUT_TO_PED, ped3, StringHash.AtStringHash("LOADOUT_COP_L3"));
            Function.Call(Hash.GIVE_LOADOUT_TO_PED, ped2, StringHash.AtStringHash("LOADOUT_COP_L3"));
             }
    }
}
