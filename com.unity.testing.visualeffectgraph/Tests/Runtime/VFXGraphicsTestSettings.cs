using UnityEngine.TestTools.Graphics;

namespace UnityEngine.VFX.Test
{
    class VFXGraphicsTestSettings : GraphicsTestSettings
    {
        static public readonly int defaultCaptureFrameRate = 20;
        static public readonly float defaultFrequency = 1.0f / (float)defaultCaptureFrameRate;
        static public readonly float defaultSimulateTime = 6.0f;

        public int captureFrameRate = defaultCaptureFrameRate;
        public float simulateTime = defaultSimulateTime;
    }
}
