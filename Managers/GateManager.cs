using GTA;
using GTA.Math;
using GTA.Native;
using System;

/// <summary>
/// Gate and door manager for property access
/// Handles automatic door unlocking for properties
/// </summary>
public class GateManager : Script
{
    public GateManager()
    {
        // Currently disabled - uncomment Tick line to enable
        // Tick += OnTick;
    }

    // Door heading states: 0 (closed), 1 (opened), -1 (opened but weird)
    // States: Locked doors have 0 as heading (closed), other states are 1 (unlocked)

    private void OnTick(object sender, EventArgs e)
    {
        if (TryGetDoorInFront(out var doorProp, out var heading, out var isLocked))
        {
            if (isLocked)
            {
                float newHeading = heading == 0 ? 0 : heading;
                Function.Call(Hash.SET_STATE_OF_CLOSEST_DOOR_OF_TYPE, doorProp.Model.Hash,
                              doorProp.Position.X, doorProp.Position.Y, doorProp.Position.Z, false, newHeading, false);

                Logger.Log.Info($"Unlocked door: {doorProp.Model.Hash} at {doorProp.Position}, heading: {newHeading}");
                //HelperClass.Notification("Door unlocked.");
            }
        }
    }

    private bool TryGetDoorInFront(out Prop doorProp, out float heading, out bool isLocked)
    {
        Vector3 pos = Game.Player.Character.Position + Game.Player.Character.ForwardVector * 1f;
        float radius = 100f;

        try
        {
            foreach (Prop prop in World.GetNearbyProps(pos, radius))
            {
                if (IsDoor(prop, out heading, out isLocked))
                {
                    doorProp = prop;
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log.Fatal($"[GateManager] Error detecting door: {ex.Message}");
        }

        doorProp = null;
        heading = 0f;
        isLocked = false;
        return false;
    }

    private bool IsDoor(Prop prop, out float heading, out bool isLocked)
    {
        // Initialize output parameters
        heading = 0f;
        isLocked = false;

        if (prop == null)
            return false;

        int hash = prop.Model.Hash;

        // Use OutputArgument to capture the results of the native function
        OutputArgument lockedStatus = new OutputArgument();
        OutputArgument doorHeading = new OutputArgument();

        // Call the native function to get the door state
        Function.Call(Hash.GET_STATE_OF_CLOSEST_DOOR_OF_TYPE, hash, prop.Position.X, prop.Position.Y, prop.Position.Z, lockedStatus, doorHeading);

        // Extract the results from the OutputArgument objects
        isLocked = lockedStatus.GetResult<bool>();
        heading = doorHeading.GetResult<float>();

        // Additional checks can be added to determine if the prop qualifies as a door
        return true;
    }
}
