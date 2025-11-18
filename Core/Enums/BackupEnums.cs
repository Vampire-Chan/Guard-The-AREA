using System;

namespace Guarding.Core.Enums
{
    /// <summary>
    /// Types of backup available to call
    /// </summary>
    public enum BackupType
    {
        /// <summary>Attack helicopter airstrike</summary>
        Airstrike,
        /// <summary>Aerial tactical helicopter support</summary>
        AerialBackup,
        /// <summary>Ground vehicle with guards</summary>
        GroundVehicle,
        /// <summary>Complex backup with multiple units</summary>
        ComplexBackup,
        /// <summary>Attack helicopter (legacy support)</summary>
        AttackHelicopter
    }

    /// <summary>
    /// State of attack helicopter
    /// </summary>
    public enum AttackHelicopterState
    {
        /// <summary>Waiting for orders</summary>
        Idle,
        /// <summary>Ready to move to initial position</summary>
        ReadyToInitial,
        /// <summary>Flying to initial position</summary>
        GoToInitial,
        /// <summary>Ready to engage targets</summary>
        ReadyToEngage,
        /// <summary>Engaging in combat</summary>
        Engage,
        /// <summary>Ready to flee area</summary>
        ReadyToFlee,
        /// <summary>Fleeing combat area</summary>
        Flee
    }

    /// <summary>
    /// Tactical helicopter crew leave options
    /// </summary>
    public enum CrewLeaveOption
    {
        /// <summary>Crew rappels down from helicopter</summary>
        Rappel,
        /// <summary>Helicopter lands and crew exits</summary>
        Land,
        /// <summary>Crew does not leave helicopter</summary>
        NoLeave
    }

    /// <summary>
    /// Tactical helicopter primary task
    /// </summary>
    public enum HelicopterTask
    {
        /// <summary>Follow player from above</summary>
        Follow,
        /// <summary>Land at player location</summary>
        Land,
        /// <summary>Attack enemies from air</summary>
        AttackFromAir,
        /// <summary>Deploy crew to ground</summary>
        DeployCrew
    }

    /// <summary>
    /// Landing state for tactical helicopter
    /// </summary>
    public enum LandingState
    {
        /// <summary>Not attempting to land</summary>
        NotLanding,
        /// <summary>Searching for landing spot</summary>
        SearchingForSpot,
        /// <summary>Flying to landing spot</summary>
        FlyingToSpot,
        /// <summary>Landing procedure in progress</summary>
        Landing,
        /// <summary>Landed successfully</summary>
        Landed
    }

    /// <summary>
    /// Rappel state for crew deployment
    /// </summary>
    public enum RappelState
    {
        /// <summary>Not rappelling</summary>
        NotRappelling,
        /// <summary>Preparing to rappel</summary>
        Preparing,
        /// <summary>Currently rappelling down</summary>
        Rappelling,
        /// <summary>Rappel complete</summary>
        Complete
    }
}
