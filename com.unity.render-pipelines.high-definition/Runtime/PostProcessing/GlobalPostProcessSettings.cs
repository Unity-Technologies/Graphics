using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum GradingLutFormat
    {
        RGB111110Float = RenderTextureFormat.RGB111110Float,
        ARGBHalf = RenderTextureFormat.ARGBHalf,
        ARGBFloat = RenderTextureFormat.ARGBFloat
    }

    [Serializable]
    public class GlobalPostProcessSettings
    {
        // Note: A lut size of 16^3 is barely usable (noticeable color banding in highly contrasted
        // areas and harsh tonemappers like ACES'). 32 should be the minimum, the lut being encoded
        // in log. Lower sizes would work better with an additional 1D shaper lut but for now we'll
        // keep it simple.
        public const int k_MinLutSize = 16;
        public const int k_MaxLutSize = 65;

        [SerializeField]
        int m_LutSize = 32;

        public int lutSize
        {
            get => m_LutSize;
            set => m_LutSize = Mathf.Clamp(value, k_MinLutSize, k_MaxLutSize);
        }

        public GradingLutFormat lutFormat = GradingLutFormat.ARGBHalf;
    }
}
