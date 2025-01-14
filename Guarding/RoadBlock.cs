using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

public class RoadblockSystem : Script
{
    private const float MIN_ROAD_WIDTH = 8.0f;
    private const float CHECK_DISTANCE = 20.0f;
    private const float PROP_SPACING = 3.0f;
    private const float VEHICLE_CLEARANCE = 1.0f;

    private readonly List<string> barrierProps = new List<string>
    {
        "prop_barrier_work01a",
        "prop_barrier_work05",
        "prop_barrier_wat_03a"
    };

    private readonly List<string> policeVehicles = new List<string>
    {
        "police",
        "police2",
        "police3"
    };

    public RoadblockSystem()
    {
        Tick += OnTick;
        KeyDown += OnKeyDown;
    }

    private struct VehicleDimensions
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }
        public float Width { get; set; }
        public float Length { get; set; }
        public float Height { get; set; }
    }

    private VehicleDimensions GetVehicleDimensions(Model vehicleModel)
    {
        OutputArgument minArg = new OutputArgument();
        OutputArgument maxArg = new OutputArgument();

        Function.Call(Hash.GET_MODEL_DIMENSIONS, vehicleModel.Hash, minArg, maxArg);

        Vector3 min = minArg.GetResult<Vector3>();
        Vector3 max = maxArg.GetResult<Vector3>();

        return new VehicleDimensions
        {
            Min = min,
            Max = max,
            Width = Math.Abs(max.X - min.X),
            Length = Math.Abs(max.Y - min.Y),
            Height = Math.Abs(max.Z - min.Z)
        };
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.B)
        {
            Vector3 playerPos = Game.Player.Character.Position;
            Vector3 forward = Game.Player.Character.ForwardVector * 30;

            // Load a random police vehicle model
            string randomPoliceVehicle = policeVehicles[new Random().Next(policeVehicles.Count)];
            Model vehicleModel = new Model(randomPoliceVehicle);
            vehicleModel.Request(1000);

            if (!vehicleModel.IsLoaded)
            {
                Notification.PostTicker("Failed to load vehicle model", false);
                return;
            }

            VehicleDimensions dimensions = GetVehicleDimensions(vehicleModel);
            RoadInfo roadInfo = AnalyzeRoad(playerPos, forward);

            if (roadInfo.IsValidLocation)
            {
                roadInfo.PlannedVehicleDimensions = dimensions;
                CreateRoadblock(roadInfo);
            }
            else
            {
                Notification.PostTicker("Roadblock created successfully!", true);
            }

            vehicleModel.MarkAsNoLongerNeeded();
        }
    }

    private class RoadInfo
    {
        public bool IsValidLocation { get; set; }
        public string ErrorMessage { get; set; }
        public Vector3 Center { get; set; }
        public float Width { get; set; }
        public bool IsHighway { get; set; }
        public bool HasLeftBarrier { get; set; }
        public bool HasRightBarrier { get; set; }
        public Vector3 Direction { get; set; }
        public VehicleDimensions PlannedVehicleDimensions { get; set; }
    }

    private RoadInfo AnalyzeRoad(Vector3 startPos, Vector3 direction)
    {
        RoadInfo info = new RoadInfo
        {
            IsValidLocation = false,
            Direction = direction,
            Center = startPos
        };

        if (!Function.Call<bool>(Hash.IS_POINT_ON_ROAD, startPos.X, startPos.Y, startPos.Z))
        {
            info.ErrorMessage = "Not on a road";
            return info;
        }

        Vector3 right = Vector3.Cross(direction, Vector3.WorldUp);

        float rightDistance = CheckRoadSide(startPos, right, out bool rightBarrier);
        float leftDistance = CheckRoadSide(startPos, -right, out bool leftBarrier);

        info.Width = rightDistance + leftDistance;
        info.Center = startPos + right * (rightDistance - leftDistance) / 2;
        info.HasRightBarrier = rightBarrier;
        info.HasLeftBarrier = leftBarrier;

        if (info.Width < MIN_ROAD_WIDTH)
        {
            info.ErrorMessage = "Road too narrow";
            return info;
        }

        info.IsValidLocation = true;
        return info;
    }

    private float CheckRoadSide(Vector3 startPos, Vector3 direction, out bool hasBarrier)
    {
        hasBarrier = false;
        RaycastResult groundRay = World.Raycast(
            startPos + new Vector3(0, 0, 0.5f),
            startPos + direction * CHECK_DISTANCE,
            IntersectFlags.Map
        );

        float distance = groundRay.DidHit ? groundRay.HitPosition.DistanceTo(startPos) : CHECK_DISTANCE;
        return distance;
    }

    private void CreateRoadblock(RoadInfo roadInfo)
    {
        Vector3 right = Vector3.Cross(roadInfo.Direction, Vector3.WorldUp);

        string randomBarrier = barrierProps[new Random().Next(barrierProps.Count)];
        float maxBarrierOffset = (roadInfo.Width / 2) - PROP_SPACING;

        PlaceBarriers(roadInfo.Center, right, maxBarrierOffset, randomBarrier);
    }

    private void PlaceBarriers(Vector3 center, Vector3 right, float maxOffset, string barrierModel)
    {
        for (float offset = PROP_SPACING; offset <= maxOffset; offset += PROP_SPACING)
        {
            Vector3 posRight = center + right * offset;
            Vector3 posLeft = center - right * offset;

            Prop barrierRight = World.CreateProp(new Model(barrierModel), posRight, false, false);
            Prop barrierLeft = World.CreateProp(new Model(barrierModel), posLeft, false, false);

            if (barrierRight != null) barrierRight.IsPersistent = true;
            if (barrierLeft != null) barrierLeft.IsPersistent = true;
        }
    }

    private void OnTick(object sender, EventArgs e)
    {
        // Add logic here if needed
    }
}
