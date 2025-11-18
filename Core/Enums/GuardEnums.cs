using System;

namespace Guarding.Core.Enums
{
    /// <summary>
    /// Defines the type of scenario a guard is performing
    /// </summary>
    public enum ScenarioType
    {
        /// <summary>Static guard position with specific animations</summary>
        Guard,
        /// <summary>Moving patrol behavior</summary>
        Patrol,
        /// <summary>Ambient scenarios (smoking, hanging out, phone)</summary>
        Ambient,
        /// <summary>Random scenarios (drinking, partying, tourist)</summary>
        Random,
        /// <summary>Vehicle-based scenarios</summary>
        Vehicle
    }

    /// <summary>
    /// Represents the current state of a guard in the state machine
    /// </summary>
    public enum GuardState
    {
        /// <summary>Idle state - nothing happening</summary>
        Idle,
        /// <summary>On duty and performing assigned task</summary>
        OnDuty,
        /// <summary>Currently in combat</summary>
        InCombat,
        /// <summary>Observing area after combat before returning</summary>
        PostCombat,
        /// <summary>Initiating return to post</summary>
        Return,
        /// <summary>Currently returning to duty position</summary>
        Returning,
        /// <summary>Greeting player or saying goodbye</summary>
        Greeting,
        /// <summary>New guards arriving on shift</summary>
        Arriving,
        /// <summary>Guards exiting vehicle after arrival</summary>
        ExitVehicle,
        /// <summary>Old guards leaving shift</summary>
        Departing
    }

    /// <summary>
    /// Represents the shift-related state for guard shift changes
    /// </summary>
    public enum GuardShiftType
    {
        /// <summary>Guard is going to reach their vehicle</summary>
        ReachVehicle,
        /// <summary>Guard is currently reaching vehicle</summary>
        ReachingVehicle,
        /// <summary>Driver waiting for all passengers to board</summary>
        WaitingForPassengers,
        /// <summary>Arriving as the driver of vehicle</summary>
        ArrivingAsDriver,
        /// <summary>Arriving as passenger in vehicle</summary>
        ArrivingAsPassenger,
        /// <summary>Leaving as driver of vehicle</summary>
        LeavingAsDriver,
        /// <summary>Leaving as passenger in vehicle</summary>
        LeavingAsPassenger,
        /// <summary>Reached destination after travel</summary>
        ReachedDestination,
        /// <summary>Currently on duty for this shift</summary>
        OnDutyShift,
    }
}
