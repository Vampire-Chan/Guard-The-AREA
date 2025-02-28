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
}