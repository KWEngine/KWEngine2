﻿using System;
using System.Diagnostics;

namespace KWEngine2.Helper
{
    internal static class DeltaTime
    {
        private static float smoothedDeltaRealTime_ms = 16.666667f; // initial value, Optionally you can save the new computed value (will change with each hardware) in Preferences to optimize the first drawing frames 
        private static float movAverageDeltaTime_ms = 16.666667f; // mov Average start with default value
        internal static float lastRealTimeMeasurement_ms = 0; // temporal storage for last time measurement
        internal static float movAveragePeriod = 60f; // #frames involved in average calc (suggested values 5-100)
        internal static float smoothFactor = 0.01f; // adjusting ratio (suggested values 0.01-0.5)
        private const float TargetFrameTime = 1f / 60f * 1000f;
        internal const double TargetFrameTimeDouble = 1.0 / 60.0;
        internal static Stopwatch Watch = new Stopwatch();
        private static float deltaTimeFactor = 1;
        
        internal static float GetDeltaTimeFactor()
        {
            return deltaTimeFactor;
        }

        internal static void UpdateDeltaTime()
        {
            float currTimePick_ms = Watch.ElapsedMilliseconds;
            float realTimeElapsed_ms;
            if (lastRealTimeMeasurement_ms > 0)
            {
                realTimeElapsed_ms = (currTimePick_ms - lastRealTimeMeasurement_ms);
            }
            else
            {
                realTimeElapsed_ms = GLWindow.CurrentWindow._vSync ? 16.66667f : smoothedDeltaRealTime_ms; // just the first time
            }
            movAverageDeltaTime_ms = (realTimeElapsed_ms + movAverageDeltaTime_ms * (movAveragePeriod - 1)) / movAveragePeriod;
            smoothedDeltaRealTime_ms = smoothedDeltaRealTime_ms + (movAverageDeltaTime_ms - smoothedDeltaRealTime_ms) * smoothFactor;
            deltaTimeFactor = HelperGL.Clamp(smoothedDeltaRealTime_ms / TargetFrameTime, 0.00001f, 10);
            lastRealTimeMeasurement_ms = currTimePick_ms;
        }
    }
}
