﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public enum PedTask
{
    CTaskHandsUp = 0,
    CTaskClimbLadder = 1,
    CTaskExitVehicle = 2,
    CTaskCombatRoll = 3,
    CTaskAimGunOnFoot = 4,
    CTaskMovePlayer = 5,
    CTaskPlayerOnFoot = 6,
    CTaskWeapon = 8,
    CTaskPlayerWeapon = 9,
    CTaskPlayerIdles = 10,
    CTaskAimGun = 12,
    CTaskComplex = 12,
    CTaskFSMClone = 12,
    CTaskMotionBase = 12,
    CTaskMove = 12,
    CTaskMoveBase = 12,
    CTaskNMBehaviour = 12,
    CTaskNavBase = 12,
    CTaskScenario = 12,
    CTaskSearchBase = 12,
    CTaskSearchInVehicleBase = 12,
    CTaskShockingEvent = 12,
    CTaskTrainBase = 12,
    CTaskVehicleFSM = 12,
    CTaskVehicleGoTo = 12,
    CTaskVehicleMissionBase = 12,
    CTaskVehicleTempAction = 12,
    CTaskPause = 14,
    CTaskDoNothing = 15,
    CTaskGetUp = 16,
    CTaskGetUpAndStandStill = 17,
    CTaskFallOver = 18,
    CTaskFallAndGetUp = 19,
    CTaskCrawl = 20,
    CTaskComplexOnFire = 25,
    CTaskDamageElectric = 26,
    CTaskTriggerLookAt = 28,
    CTaskClearLookAt = 29,
    CTaskSetCharDecisionMaker = 30,
    CTaskSetPedDefensiveArea = 31,
    CTaskUseSequence = 32,
    CTaskMoveStandStill = 34,
    CTaskComplexControlMovement = 35,
    CTaskMoveSequence = 36,
    CTaskAmbientClips = 38,
    CTaskMoveInAir = 39,
    CTaskNetworkClone = 40,
    CTaskUseClimbOnRoute = 41,
    CTaskUseDropDownOnRoute = 42,
    CTaskUseLadderOnRoute = 43,
    CTaskSetBlockingOfNonTemporaryEvents = 44,
    CTaskForceMotionState = 45,
    CTaskSlopeScramble = 46,
    CTaskGoToAndClimbLadder = 47,
    CTaskClimbLadderFully = 48,
    CTaskRappel = 49,
    CTaskVault = 50,
    CTaskDropDown = 51,
    CTaskAffectSecondaryBehaviour = 52,
    CTaskAmbientLookAtEvent = 53,
    CTaskOpenDoor = 54,
    CTaskShovePed = 55,
    CTaskSwapWeapon = 56,
    CTaskGeneralSweep = 57,
    CTaskPolice = 58,
    CTaskPoliceOrderResponse = 59,
    CTaskPursueCriminal = 60,
    CTaskArrestPed = 62,
    CTaskArrestPed2 = 63,
    CTaskBusted = 64,
    CTaskFirePatrol = 65,
    CTaskHeliOrderResponse = 66,
    CTaskHeliPassengerRappel = 67,
    CTaskAmbulancePatrol = 68,
    CTaskPoliceWantedResponse = 69,
    CTaskSwat = 70,
    CTaskSwatWantedResponse = 72,
    CTaskSwatOrderResponse = 73,
    CTaskSwatGoToStagingArea = 74,
    CTaskSwatFollowInLine = 75,
    CTaskWitness = 76,
    CTaskGangPatrol = 77,
    CTaskArmy = 78,
    CTaskShockingEventWatch = 80,
    CTaskShockingEventGoto = 82,
    CTaskShockingEventHurryAway = 83,
    CTaskShockingEventReactToAircraft = 84,
    CTaskShockingEventReact = 85,
    CTaskShockingEventBackAway = 86,
    CTaskShockingPoliceInvestigate = 87,
    CTaskShockingEventStopAndStare = 88,
    CTaskShockingNiceCarPicture = 89,
    CTaskShockingEventThreatResponse = 90,
    CTaskTakeOffHelmet = 92,
    CTaskCarReactToVehicleCollision = 93,
    CTaskCarReactToVehicleCollisionGetOut = 95,
    CTaskDyingDead = 97,
    CTaskWanderingScenario = 100,
    CTaskWanderingInRadiusScenario = 101,
    CTaskMoveBetweenPointsScenario = 103,
    CTaskChatScenario = 104,
    CTaskCowerScenario = 106,
    CTaskDeadBodyScenario = 107,
    CTaskSayAudio = 114,
    CTaskWaitForSteppingOut = 116,
    CTaskCoupleScenario = 117,
    CTaskUseScenario = 118,
    CTaskUseVehicleScenario = 119,
    CTaskUnalerted = 120,
    CTaskStealVehicle = 121,
    CTaskReactToPursuit = 122,
    CTaskHitWall = 125,
    CTaskCower = 126,
    CTaskCrouch = 127,
    CTaskMelee = 128,
    CTaskMoveMeleeMovement = 129,
    CTaskMeleeActionResult = 130,
    CTaskMeleeUpperbodyAnims = 131,
    CTaskMoVEScripted = 133,
    CTaskScriptedAnimation = 134,
    CTaskSynchronizedScene = 135,
    CTaskComplexEvasiveStep = 137,
    CTaskWalkRoundCarWhileWandering = 138,
    CTaskComplexStuckInAir = 140,
    CTaskWalkRoundEntity = 141,
    CTaskMoveWalkRoundVehicle = 142,
    CTaskReactToGunAimedAt = 144,
    CTaskDuckAndCover = 146,
    CTaskAggressiveRubberneck = 147,
    CTaskInVehicleBasic = 150,
    CTaskCarDriveWander = 151,
    CTaskLeaveAnyCar = 152,
    CTaskComplexGetOffBoat = 153,
    CTaskCarSetTempAction = 155,
    CTaskBringVehicleToHalt = 156,
    CTaskCarDrive = 157,
    CTaskPlayerDrive = 159,
    CTaskEnterVehicle = 160,
    CTaskEnterVehicleAlign = 161,
    CTaskOpenVehicleDoorFromOutside = 162,
    CTaskEnterVehicleSeat = 163,
    CTaskCloseVehicleDoorFromInside = 164,
    CTaskInVehicleSeatShuffle = 165,
    CTaskExitVehicleSeat = 167,
    CTaskCloseVehicleDoorFromOutside = 168,
    CTaskControlVehicle = 169,
    CTaskMotionInAutomobile = 170,
    CTaskMotionOnBicycle = 171,
    CTaskMotionOnBicycleController = 172,
    CTaskMotionInVehicle = 173,
    CTaskMotionInTurret = 174,
    CTaskReactToBeingJacked = 175,
    CTaskReactToBeingAskedToLeaveVehicle = 176,
    CTaskTryToGrabVehicleDoor = 177,
    CTaskGetOnTrain = 178,
    CTaskGetOffTrain = 179,
    CTaskRideTrain = 180,
    CTaskMountThrowProjectile = 190,
    CTaskGoToCarDoorAndStandStill = 195,
    CTaskMoveGoToVehicleDoor = 196,
    CTaskSetPedInVehicle = 197,
    CTaskSetPedOutOfVehicle = 198,
    CTaskVehicleMountedWeapon = 199,
    CTaskVehicleGun = 200,
    CTaskVehicleProjectile = 201,
    CTaskSmashCarWindow = 204,
    CTaskMoveGoToPoint = 205,
    CTaskMoveAchieveHeading = 206,
    CTaskMoveFaceTarget = 207,
    CTaskComplexGoToPointAndStandStillTimed = 208,
    CTaskMoveGoToPointAndStandStill = 208,
    CTaskMoveFollowPointRoute = 209,
    CTaskMoveSeekEntity_CEntitySeekPosCalculatorStandard = 210,
    CTaskMoveSeekEntity_CEntitySeekPosCalculatorLastNavMeshIntersection = 211,
    CTaskMoveSeekEntity_CEntitySeekPosCalculatorLastNavMeshIntersection2 = 212,
    CTaskMoveSeekEntity_CEntitySeekPosCalculatorXYOffsetFixed = 213,
    CTaskMoveSeekEntity_CEntitySeekPosCalculatorXYOffsetFixed2 = 214,
    CTaskExhaustedFlee = 215,
    CTaskGrowlAndFlee = 216,
    CTaskScenarioFlee = 217,
    CTaskSmartFlee = 218,
    CTaskFlyAway = 219,
    CTaskWalkAway = 220,
    CTaskWander = 221,
    CTaskWanderInArea = 222,
    CTaskFollowLeaderInFormation = 223,
    CTaskGoToPointAnyMeans = 224,
    CTaskTurnToFaceEntityOrCoord = 225,
    CTaskFollowLeaderAnyMeans = 226,
    CTaskFlyToPoint = 228,
    CTaskFlyingWander = 229,
    CTaskGoToPointAiming = 230,
    CTaskGoToScenario = 231,
    CTaskSeekEntityAiming = 233,
    CTaskSlideToCoord = 234,
    CTaskSwimmingWander = 235,
    CTaskMoveTrackingEntity = 237,
    CTaskMoveFollowNavMesh = 238,
    CTaskMoveGoToPointOnRoute = 239,
    CTaskEscapeBlast = 240,
    CTaskMoveWander = 241,
    CTaskMoveBeInFormation = 242,
    CTaskMoveCrowdAroundLocation = 243,
    CTaskMoveCrossRoadAtTrafficLights = 244,
    CTaskMoveWaitForTraffic = 245,
    CTaskMoveGoToPointStandStillAchieveHeading = 246,
    CTaskMoveGetOntoMainNavMesh = 251,
    CTaskMoveSlideToCoord = 252,
    CTaskMoveGoToPointRelativeToEntityAndStandStill = 253,
    CTaskHelicopterStrafe = 254,
    CTaskGetOutOfWater = 256,
    CTaskMoveFollowEntityOffset = 259,
    CTaskFollowWaypointRecording = 261,
    CTaskMotionPed = 264,
    CTaskMotionPedLowLod = 265,
    CTaskHumanLocomotion = 268,
    CTaskMotionBasicLocomotionLowLod = 269,
    CTaskMotionStrafing = 270,
    CTaskMotionTennis = 271,
    CTaskMotionAiming = 272,
    CTaskBirdLocomotion = 273,
    CTaskFlightlessBirdLocomotion = 274,
    CTaskFishLocomotion = 278,
    CTaskQuadLocomotion = 279,
    CTaskMotionDiving = 280,
    CTaskMotionSwimming = 281,
    CTaskMotionParachuting = 282,
    CTaskMotionDrunk = 283,
    CTaskRepositionMove = 284,
    CTaskMotionAimingTransition = 285,
    CTaskThrowProjectile = 286,
    CTaskCover = 287,
    CTaskMotionInCover = 288,
    CTaskAimAndThrowProjectile = 289,
    CTaskGun = 290,
    CTaskAimFromGround = 291,
    CTaskAimGunVehicleDriveBy = 295,
    CTaskAimGunScripted = 296,
    CTaskReloadGun = 298,
    CTaskWeaponBlocked = 299,
    CTaskEnterCover = 300,
    CTaskExitCover = 301,
    CTaskAimGunFromCoverIntro = 302,
    CTaskAimGunFromCoverOutro = 303,
    CTaskAimGunBlindFire = 304,
    CTaskCombatClosestTargetInArea = 307,
    CTaskCombatAdditionalTask = 308,
    CTaskInCover = 309,
    CTaskAimSweep = 313,
    CTaskSharkCircle = 319,
    CTaskSharkAttack = 320,
    CTaskAgitated = 321,
    CTaskAgitatedAction = 322,
    CTaskConfront = 323,
    CTaskIntimidate = 324,
    CTaskShove = 325,
    CTaskShoved = 326,
    CTaskCrouchToggle = 328,
    CTaskRevive = 329,
    CTaskParachute = 335,
    CTaskParachuteObject = 336,
    CTaskTakeOffPedVariation = 337,
    CTaskCombatSeekCover = 340,
    CTaskCombatFlank = 342,
    CTaskCombat = 343,
    CTaskCombatMounted = 344,
    CTaskMoveCircle = 345,
    CTaskMoveCombatMounted = 346,
    CTaskSearch = 347,
    CTaskSearchOnFoot = 348,
    CTaskSearchInAutomobile = 349,
    CTaskSearchInBoat = 350,
    CTaskSearchInHeli = 351,
    CTaskThreatResponse = 352,
    CTaskInvestigate = 353,
    CTaskStandGuardFSM = 354,
    CTaskPatrol = 355,
    CTaskShootAtTarget = 356,
    CTaskSetAndGuardArea = 357,
    CTaskStandGuard = 358,
    CTaskSeparate = 359,
    CTaskStayInCover = 360,
    CTaskVehicleCombat = 361,
    CTaskVehiclePersuit = 362,
    CTaskVehicleChase = 363,
    CTaskDraggingToSafety = 364,
    CTaskDraggedToSafety = 365,
    CTaskVariedAimPose = 366,
    CTaskMoveWithinAttackWindow = 367,
    CTaskMoveWithinDefensiveArea = 368,
    CTaskShootOutTire = 369,
    CTaskShellShocked = 370,
    CTaskBoatChase = 371,
    CTaskBoatCombat = 372,
    CTaskBoatStrafe = 373,
    CTaskHeliChase = 374,
    CTaskHeliCombat = 375,
    CTaskSubmarineCombat = 376,
    CTaskSubmarineChase = 377,
    CTaskPlaneChase = 378,
    CTaskTargetUnreachable = 379,
    CTaskTargetUnreachableInInterior = 380,
    CTaskTargetUnreachableInExterior = 381,
    CTaskStealthKill = 382,
    CTaskWrithe = 383,
    CTaskAdvance = 384,
    CTaskCharge = 385,
    CTaskMoveToTacticalPoint = 386,
    CTaskToHurtTransit = 387,
    CTaskAnimatedHitByExplosion = 388,
    CTaskNMRelax = 389,
    CTaskNMPose = 391,
    CTaskNMBrace = 392,
    CTaskNMBuoyancy = 393,
    CTaskNMInjuredOnGround = 394,
    CTaskNMShot = 395,
    CTaskNMHighFall = 396,
    CTaskNMBalance = 397,
    CTaskNMElectrocute = 398,
    CTaskNMPrototype = 399,
    CTaskNMExplosion = 400,
    CTaskNMOnFire = 401,
    CTaskNMScriptControl = 402,
    CTaskNMJumpRollFromRoadVehicle = 403,
    CTaskNMFlinch = 404,
    CTaskNMSit = 405,
    CTaskNMFallDown = 406,
    CTaskBlendFromNM = 407,
    CTaskNMControl = 408,
    CTaskNMDangle = 409,
    CTaskNMGenericAttach = 412,
    CTaskNMDraggingToSafety = 414,
    CTaskNMThroughWindscreen = 415,
    CTaskNMRiverRapids = 416,
    CTaskNMSimple = 417,
    CTaskRageRagdoll = 418,
    CTaskJumpVault = 421,
    CTaskJump = 422,
    CTaskFall = 423,
    CTaskReactAimWeapon = 425,
    CTaskChat = 426,
    CTaskMobilePhone = 427,
    CTaskReactToDeadPed = 428,
    CTaskSearchForUnknownThreat = 430,
    CTaskBomb = 432,
    CTaskDetonator = 433,
    CTaskAnimatedAttach = 435,
    CTaskCutScene = 441,
    CTaskReactToExplosion = 442,
    CTaskReactToImminentExplosion = 443,
    CTaskDiveToGround = 444,
    CTaskReactAndFlee = 445,
    CTaskSidestep = 446,
    CTaskCallPolice = 447,
    CTaskReactInDirection = 448,
    CTaskReactToBuddyShot = 449,
    CTaskVehicleGoToAutomobileNew = 454,
    CTaskVehicleGoToPlane = 455,
    CTaskVehicleGoToHelicopter = 456,
    CTaskVehicleGoToSubmarine = 457,
    CTaskVehicleGoToBoat = 458,
    CTaskVehicleGoToPointAutomobile = 459,
    CTaskVehicleGoToPointWithAvoidanceAutomobile = 460,
    CTaskVehiclePursue = 461,
    CTaskVehicleRam = 462,
    CTaskVehicleSpinOut = 463,
    CTaskVehicleApproach = 464,
    CTaskVehicleThreePointTurn = 465,
    CTaskVehicleDeadDriver = 466,
    CTaskVehicleCruiseNew = 467,
    CTaskVehicleCruiseBoat = 468,
    CTaskVehicleStop = 469,
    CTaskVehiclePullOver = 470,
    CTaskVehiclePassengerExit = 471,
    CTaskVehicleFlee = 472,
    CTaskVehicleFleeAirborne = 473,
    CTaskVehicleFleeBoat = 474,
    CTaskVehicleFollowRecording = 475,
    CTaskVehicleFollow = 476,
    CTaskVehicleBlock = 477,
    CTaskVehicleBlockCruiseInFront = 478,
    CTaskVehicleBlockBrakeInFront = 479,
    CTaskVehicleBlockBackAndForth = 478,
    CTaskVehicleCrash = 481,
    CTaskVehicleLand = 482,
    CTaskVehicleLandPlane = 483,
    CTaskVehicleHover = 484,
    CTaskVehicleAttack = 485,
    CTaskVehicleAttackTank = 486,
    CTaskVehicleCircle = 487,
    CTaskVehiclePoliceBehaviour = 488,
    CTaskVehiclePoliceBehaviourHelicopter = 489,
    CTaskVehiclePoliceBehaviourBoat = 490,
    CTaskVehicleEscort = 491,
    CTaskVehicleHeliProtect = 492,
    CTaskVehiclePlayerDriveAutomobile = 494,
    CTaskVehiclePlayerDriveBike = 495,
    CTaskVehiclePlayerDriveBoat = 496,
    CTaskVehiclePlayerDriveSubmarine = 497,
    CTaskVehiclePlayerDriveSubmarineCar = 498,
    CTaskVehiclePlayerDriveAmphibiousAutomobile = 499,
    CTaskVehiclePlayerDrivePlane = 500,
    CTaskVehiclePlayerDriveHeli = 501,
    CTaskVehiclePlayerDriveAutogyro = 502,
    CTaskVehiclePlayerDriveDiggerArm = 503,
    CTaskVehiclePlayerDriveTrain = 504,
    CTaskVehiclePlaneChase = 505,
    CTaskVehicleNoDriver = 506,
    CTaskVehicleAnimation = 507,
    CTaskVehicleConvertibleRoof = 508,
    CTaskVehicleParkNew = 509,
    CTaskVehicleFollowWaypointRecording = 510,
    CTaskVehicleGoToNavmesh = 511,
    CTaskVehicleReactToCopSiren = 512,
    CTaskVehicleGotoLongRange = 513,
    CTaskVehicleWait = 514,
    CTaskVehicleReverse = 515,
    CTaskVehicleBrake = 516,
    CTaskVehicleHandBrake = 517,
    CTaskVehicleTurn = 518,
    CTaskVehicleGoForward = 519,
    CTaskVehicleSwerve = 520,
    CTaskVehicleFlyDirection = 521,
    CTaskVehicleHeadonCollision = 522,
    CTaskVehicleBoostUseSteeringAngle = 523,
    CTaskVehicleShotTire = 524,
    CTaskVehicleBurnout = 525,
    CTaskVehicleRevEngine = 526,
    CTaskVehicleSurfaceInSubmarine = 527,
    CTaskVehiclePullAlongside = 528,
    CTaskVehicleTransformToSubmarine = 529,
    CTaskAnimatedFallback = 530,
    MaxNumTasks = 531
}
// PilotInformation.cs
public class PilotInformation
    {
        public List<string> PilotModelList { get; set; } // List of pilot model names
    public List<WeaponInformation> WeaponInfoList { get; set; }

    public PilotInformation(List<string> pilotModels, List<WeaponInformation> weaponsInfoList)
        {
        PilotModelList = pilotModels;
        WeaponInfoList = weaponsInfoList;
        }
    }

    // SoldierInformation.cs
    public class SoldierInformation
    {
        public List<string> SoldierModels { get; set; }
        public List<WeaponInformation> WeaponInfoList { get; set; }

        public SoldierInformation(List<string> soldierModels, List<WeaponInformation> weaponInfoList)
        {
            SoldierModels = soldierModels;
            WeaponInfoList = weaponInfoList;
        }
}
public enum GuardType
{
    Ped,        // guard, ped, soldier, npc
    Vehicle,    // vehicle, car, bike, cycle
    Helicopter, // heli, helicopter, chopper, copter
    Boat,       // boat, ship, seabike, jetski
    Mounted,    // gunner, mounted, turret
    LargeVehicle,    // bus, truck, trailer
    LargeHelicopter, // cargobob, bigheli, largehelicopter
    CargoPlane,      // cargoplane, globemaster
    LargeBoat        // warship, tugboat, largeboat, submarine, bigship
}

    public enum ScenarioType
    {
        Guard, //guard
        Vehicle, //no scenarios
        Ambient, //non guard like eating, looking and all
        Patrol,
        Random //like ?
    }
public class RelationshipManager
{
    public List<string> Hate { get; set; } = new List<string>();
    public List<string> Dislike { get; set; } = new List<string>();
    public string Respect { get; set; }
    public List<string> Like { get; set; } = new List<string>();

    public RelationshipManager(List<string> hate, List<string> dislike, string respect, List<string> like)
    {
        Hate = hate;
        Dislike = dislike;
        Respect = respect;
        Like = like;
    }
}
// VehicleHealth.cs
public class VehicleHealth
    {
        public int? Engine { get; set; }
        public int? Body { get; set; }
        public int? Petrol { get; set; }

        public VehicleHealth(int? engine, int? body, int? petrol)
        {
            Engine = engine;
            Body = body;
            Petrol = petrol;
        }
    }

    // VehicleInformation.cs --
    public class VehicleInformation
    {
        public VehicleDetails VehicleDetails { get; set; } // Single VehicleDetails, not a list
        public PilotInformation PilotInfo { get; set; } // Renamed to PilotInfo, single object
        public SoldierInformation SoldierInfo { get; set; } // Single SoldierInfo object

        public VehicleInformation(VehicleDetails vehicleDetails, PilotInformation pilotInfo, SoldierInformation soldierInfo)
        {
            VehicleDetails = vehicleDetails;
            PilotInfo = pilotInfo;
            SoldierInfo = soldierInfo;
        }
    }

    // VehicleLivery.cs
    public class VehicleLivery
    {
        public string Set { get; set; }
        public int Index { get; set; }

        /// <summary>
        /// Use Set for LiveryType, and for LiveryType2
        /// </summary>
        /// <param name="set">the Livery, Livery2</param>
        /// <param name="index">the index.</param>
        public VehicleLivery(string set, int index)
        {
            Set = set;
            Index = index;
        }
    }

    // WeaponDetails.cs
    public class WeaponDetails
    {
        public string Name { get; set; }
        public List<string> Attachments { get; set; }
        public int Ammo { get; set; }
        public int Magazine { get; set; }
        public string Flag { get; set; }

        public WeaponDetails(string name, List<string> attachments, int ammo, int magazine, string flag)
        {
            Name = name;
            Attachments = attachments;
            Ammo = ammo;
            Magazine = magazine;
            Flag = flag;
        }
    }

// Scenario class
public class Scenarios
{
    public string Name { get; set; }
    public List<string> ScenarioList { get; set; }

    public Scenarios(string name, List<string> scenarioList)
    {
        Name = name;
        ScenarioList = scenarioList;
    }
}

// GuardConfig class
public class GuardConfig
{
    public string Name { get; set; }
    public List<string> PedModels { get; set; } = new();
    public List<string> Weapons { get; set; } = new();
    public List<string> VehicleModels { get; set; } = new();
    public List<string> MVehicleModels { get; set; } = new();
    public List<string> HVehicleModels { get; set; } = new();
    public List<string> PVehicleModels { get; set; } = new();
    public List<string> BVehicleModels { get; set; } = new();
    public List<string> LVehicleModels { get; set; } = new();
    public List<string> Hate { get; set; } = new();
    public List<string> Dislike { get; set; } = new();
    public string Respect { get; set; }
    public List<string> Like { get; set; } = new();
    public string RelationshipGroup { get; set; }
}

// WeaponInformation.cs
public class WeaponInformation
    {
        public List<WeaponDetails> PrimaryWeaponList { get; set; }
        public List<WeaponDetails> SecondaryWeaponList { get; set; }

        public WeaponInformation(List<WeaponDetails> primaryWeaponList, List<WeaponDetails> secondaryWeaponList)
        {
            PrimaryWeaponList = primaryWeaponList;
            SecondaryWeaponList = secondaryWeaponList;
        }
    }

    // DispatchInfoBase.cs
    public class DispatchInfoAir
    {
        public List<VehicleInformation> VehicleInfo { get; set; }

        public DispatchInfoAir(List<VehicleInformation> vehicleInfo)
        {
            VehicleInfo = vehicleInfo;
        }
    }

    public class DispatchInfoGround
    {
        public List<VehicleInformation> VehicleInfo { get; set; }

        public DispatchInfoGround(List<VehicleInformation> vehicleInfo)
        {
            VehicleInfo = vehicleInfo;
        }
    }

    public class DispatchInfoSea
    {
        public List<VehicleInformation> VehicleInfo { get; set; }

        public DispatchInfoSea(List<VehicleInformation> vehicleInfo)
        {
            VehicleInfo = vehicleInfo;
        }
    }

    // WantedStarBase.cs
    // Base class and derived classes for Wanted Stars
    public class WantedStarBase
    {
        public List<DispatchInfoAir> AirList { get; set; }
        public List<DispatchInfoGround> GroundList { get; set; }
        public List<DispatchInfoSea> SeaList { get; set; }

        public WantedStarBase()
        {
            AirList = new List<DispatchInfoAir>();
            GroundList = new List<DispatchInfoGround>();
            SeaList = new List<DispatchInfoSea>();
        }
    }

    public class WantedStarOne : WantedStarBase { public WantedStarOne() : base() { } }
    public class WantedStarTwo : WantedStarBase { public WantedStarTwo() : base() { } }
    public class WantedStarThree : WantedStarBase { public WantedStarThree() : base() { } }
    public class WantedStarFour : WantedStarBase { public WantedStarFour() : base() { } }
    public class WantedStarFive : WantedStarBase { public WantedStarFive() : base() { } }

    //VehicleDetails.cs
    public class VehicleDetails
    {
        public string Name { get; set; }
        public List<WeaponDetails> VehicleWeaponDetailsList { get; set; }
        public List<Mod> VehicleMods { get; set; }
        public VehicleHealth VehicleHealth { get; set; }
        public List<VehicleLivery> VehicleLiveries { get; set; }
        public List<string> VehicleTasks { get; set; }

        public VehicleDetails(string name, List<WeaponDetails> vehicleWeaponDetailsList, List<Mod> vehicleMods, VehicleHealth vehicleHealth, List<VehicleLivery> vehicleLiveries, List<string> vehicleTasks)
        {
            Name = name;
            VehicleWeaponDetailsList = vehicleWeaponDetailsList;
            VehicleMods = vehicleMods;
            VehicleHealth = vehicleHealth;
            VehicleLiveries = vehicleLiveries;
            VehicleTasks = vehicleTasks;
        }
    }

    // Mod.cs
    public class Mod
    {
        public string Type { get; set; }
        public int Index { get; set; }

        public Mod(string type, int index)
        {
            Type = type;
            Index = index;
        }
    }

    public enum PedRelationship : byte // Enums at top - 1. Enums
    {
        None = 255,
        Respect = 0,
        Like,         //  1
        Ignore,       //  2
        Dislike,     //  3
        Wanted,       //  4
        Hate,         //  5
        Dead          //  6
    }

    public enum DispatchType : uint
    {
        DT_Invalid,
        DT_PoliceAutomobile,
        DT_PoliceHelicopter,
        DT_FireDepartment,
        DT_SwatAutomobile,
        DT_AmbulanceDepartment,
        DT_PoliceRiders,
        DT_PoliceVehicleRequest,
        DT_PoliceRoadBlock,
        DT_PoliceAutomobileWaitPulledOver,
        DT_PoliceAutomobileWaitCruising,
        DT_Gangs,
        DT_SwatHelicopter,
        DT_PoliceBoat,
        DT_ArmyVehicle,
        DT_BikerBackup,
        DT_Assassins
    }

public class VehicleGroup
{
    public string Name { get; set; }
}
