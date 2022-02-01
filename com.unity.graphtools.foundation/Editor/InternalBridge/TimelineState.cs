using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Bridge
{
    class TimelineState
    {
        internal const int FrameRate = 1;
        TimeArea m_TimeArea;

        public static float FrameToTime(int frame) => frame / (float)FrameRate;
        public static int TimeToFrame(float time) => (int)(time * FrameRate);

        public TimelineState(TimeArea timeArea)
        {
            m_TimeArea = timeArea;
        }

        public float CurrentTime { get; set; }
        public float MinVisibleTime => m_TimeArea.shownArea.xMin;
        public float MaxVisibleTime => m_TimeArea.shownArea.xMax;
        public float VisibleTimeSpan => MaxVisibleTime - MinVisibleTime;

        float PixelPerSecond => m_TimeArea.m_Scale.x;

        // The GUI x-coordinate, where time==0 (used for time-pixel conversions)
        float ZeroTimePixel => m_TimeArea.shownArea.xMin * m_TimeArea.m_Scale.x * -1f;

        public enum SnapMode
        {
            Disabled = 0,
            SnapToFrame = 1,
        }

        public float TimeToPixel(float time)
        {
            return TimeToPixel(time, SnapMode.Disabled);
        }

        float TimeToPixel(float time, SnapMode snap)
        {
            return SnapToFrame(time, snap) * PixelPerSecond + ZeroTimePixel;
        }

        public static float SnapToFrame(float time, SnapMode snap)
        {
            if (snap == SnapMode.Disabled)
                return time;

            return SnapToFrame(time, FrameRate);
        }

        static float SnapToFrame(float time, float fps)
        {
            float snapTime = Mathf.Round(time * fps) / fps;
            return snapTime;
        }
    }
}
