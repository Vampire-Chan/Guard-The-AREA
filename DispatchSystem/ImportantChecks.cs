using GTA;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using GTA.Math;
using System.Text;
using System.Threading.Tasks;
using GTA.Chrono;

    internal class ImportantChecks : Script
    {
        public ImportantChecks()
        {
            Tick += OnTick;
        }
        public static Vector3 LastKnownLocation;

        public static bool IsInOrAroundWater
        {
            get
            {
                return Game.Player.Character.IsInBoat ||
                                  Game.Player.Character.IsInSub ||
                                  Game.Player.Character.IsSwimming ||
                                  Game.Player.Character.IsSwimmingUnderWater || Game.Player.Character.IsInWaterStrict;
            }
        }

        // Enum to represent the different parts of the day.
        public enum DayTimeType
        {   // are there more? 
            Dawn,
            Morning,
            Afternoon,
            Evening,
            Night
        }

        public static DayTimeType CurrentDayTime
        {
            get
            {
                int hour = GameClock.Hour;

                if (hour >= 5 && hour < 7) //Morning Sharp 5AM-7AM
                    return DayTimeType.Dawn;
                else if (hour >= 7 && hour < 12) //Fresh Morning 7AM-12PM
                    return DayTimeType.Morning;
                else if (hour >= 12 && hour < 18) //Afternoon 12PM-6PM
                    return DayTimeType.Afternoon;
                else if (hour >= 18 && hour < 21) //Evening 6PM-9PM
                    return DayTimeType.Evening;
                else //Rest is nighttime 9PM-5AM
                    return DayTimeType.Night;
            }
        }

        bool IsOutside => Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, new InputArgument[] { LastKnownLocation.X, LastKnownLocation.Y, LastKnownLocation.Z }) == 0 || Function.Call<bool>(Hash.IS_POINT_ON_ROAD, new InputArgument[] { LastKnownLocation.X, LastKnownLocation.Y, LastKnownLocation.Z });

        private void OnTick(object sender, EventArgs e)
        {
            Updater();
        }

        void Updater()
        {
            if (Game.Player.WantedLevel == 0 || !Game.Player.AreWantedStarsGrayedOut)
            {
                LastKnownLocation = Game.Player.Character.Position;
            }

            OutputArgument waterHeight = new OutputArgument();
            OutputArgument groundHeight = new OutputArgument();

            Function.Call(Hash.GET_WATER_HEIGHT, LastKnownLocation.X, LastKnownLocation.Y, LastKnownLocation.Z, waterHeight);
            Function.Call(Hash.GET_GROUND_Z_FOR_3D_COORD, LastKnownLocation.X, LastKnownLocation.Y, LastKnownLocation.Z, groundHeight, true);

            float waterZ = waterHeight.GetResult<float>();
            float groundZ = groundHeight.GetResult<float>();
        }
    }
