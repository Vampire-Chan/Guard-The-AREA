using GTA;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System;

namespace Guarding.DispatchSystem
{
    public class DispatchSystemManager : Script
    {
        private DispatchManager dispatchManager;

        public DispatchSystemManager()
        {
            Tick += OnTick;
            var vehicleInfo = XMLDataLoader.LoadVehicleInformation("./scripts/DispatchData.xml");
            dispatchManager = new DispatchManager(vehicleInfo);
        }

        private void OnTick(object sender, EventArgs e)
        {
            dispatchManager.UpdateDispatch();
        }
    }
}