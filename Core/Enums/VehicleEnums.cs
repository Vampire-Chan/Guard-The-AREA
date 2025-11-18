using System;

namespace Guarding.Core.Enums
{
    /// <summary>
    /// Defines the type of guard vehicle
    /// </summary>
    public enum VehicleType
    {
        /// <summary>Standard ground vehicle</summary>
        Vehicle,
        /// <summary>Large ground vehicle (trucks, vans)</summary>
        LargeVehicle,
        /// <summary>Helicopter</summary>
        Helicopter,
        /// <summary>Plane/aircraft</summary>
        Plane,
        /// <summary>Boat/watercraft</summary>
        Boat,
        /// <summary>Mounted vehicle with gunner (tanks, APCs)</summary>
        Mounted
    }

    /// <summary>
    /// Represents the current state of a guard vehicle
    /// </summary>
    public enum VehicleState
    {
        /// <summary>Vehicle is idle/parked</summary>
        Idle,
        /// <summary>Vehicle is being used for patrol</summary>
        Patrol,
        /// <summary>Vehicle is departing with guards</summary>
        Departing,
        /// <summary>Vehicle is arriving with guards</summary>
        Arriving,
        /// <summary>Vehicle is in transit to destination</summary>
        InTransit,
        /// <summary>Vehicle has been destroyed</summary>
        Destroyed
    }

    /// <summary>
    /// Vehicle modification types for customization
    /// </summary>
    public enum ModType
    {
        Spoilers,
        FrontBumper,
        RearBumper,
        SideSkirt,
        Exhaust,
        Frame,
        Grille,
        Hood,
        Fender,
        RightFender,
        Roof,
        Engine,
        Brakes,
        Transmission,
        Horns,
        Suspension,
        Armor,
        FrontWheel,
        RearWheel,
        PlateHolder,
        TrimDesign,
        Ornaments,
        DialDesign,
        SteeringWheel,
        ShiftLever,
        Plaques,
        Hydraulics,
        EngineBlock,
        AirFilter,
        Struts,
        ArchCover,
        Aerial,
        Trim,
        Tank,
        Windows,
        Livery,
        RoofLivery,
        Roofrack,
        Engine5,
        Bodyset
    }

    /// <summary>
    /// Vehicle wheel types
    /// </summary>
    public enum ModWheelType
    {
        Sport,
        Muscle,
        Lowrider,
        SUV,
        Offroad,
        Tuner,
        BikeWheels,
        HighEnd,
        BennysOriginals,
        BennysBespoke
    }

    /// <summary>
    /// Backup vehicle task types
    /// </summary>
    public enum VehicleTask
    {
        /// <summary>Waiting for orders</summary>
        Idle,
        /// <summary>Driving to player</summary>
        Driving,
        /// <summary>Following player</summary>
        Following,
        /// <summary>In combat with enemies</summary>
        InCombat,
        /// <summary>Dismissed by player</summary>
        Dismissed
    }
}
