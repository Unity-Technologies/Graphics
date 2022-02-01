using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Bridge
{
    [Serializable]
    class TimeArea : UnityEditor.TimeArea
    {
        public TimeArea() : base(false)
        {
            hRangeLocked = false;
            vRangeLocked = true;
            hSlider = false;
            vSlider = false;
            margin = 10f;
            scaleWithWindow = true;
            hTicks.SetTickModulosForFrameRate(TimelineState.FrameRate);
            hAllowExceedBaseRangeMin = false;
            hBaseRangeMin = 0;
        }

        public void Draw(Rect timeRect)
        {
            hBaseRangeMax = Mathf.Max(Time.frameCount, 300);
            rect = timeRect;
            DrawMajorTicks(timeRect, TimelineState.FrameRate);
            TimeRuler(timeRect, TimelineState.FrameRate, true, false, 1, TimeFormat.Frame);
            DrawTimeOnSlider(20, Color.red, 5500, 5);
            BeginViewGUI();
            EndViewGUI();
        }

        public float FrameToPixel(int time) => TimeToPixel(time / (float)TimelineState.FrameRate, rect);
        public float FrameDeltaToPixel() => rect.width / shownArea.width;
        public void SetShownRange(float min, float max) => SetShownHRange(min, max);
    }
}
