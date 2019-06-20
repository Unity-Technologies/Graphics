using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum GradingLutFormat
    {
        RGB111110Float = GraphicsFormat.B10G11R11_UFloatPack32,
        ARGBHalf = GraphicsFormat.R16G16B16A16_SFloat,
        ARGBFloat = GraphicsFormat.R32G32B32A32_SFloat
    }

    [Serializable]
    public struct GlobalPostProcessSettings
    {
        /// <summary>Default GlobalPostProcessSettings</summary>
        public static readonly GlobalPostProcessSettings @default = new GlobalPostProcessSettings()
        {
            m_LutSize = 32,
            m_LutFormat = GradingLutFormat.ARGBHalf
        };

        // Note: A lut size of 16^3 is barely usable (noticeable color banding in highly contrasted
        // areas and harsh tonemappers like ACES'). 32 should be the minimum, the lut being encoded
        // in log. Lower sizes would work better with an additional 1D shaper lut but for now we'll
        // keep it simple.
        public const int k_MinLutSize = 16;
        public const int k_MaxLutSize = 65;

        [SerializeField]
        int m_LutSize;

        public int lutSize
        {
            get => m_LutSize;
            set => m_LutSize = Mathf.Clamp(value, k_MinLutSize, k_MaxLutSize);
        }

        [SerializeField]
        GradingLutFormat m_LutFormat;

        public GradingLutFormat lutFormat
        {
            get => m_LutFormat;
            set => m_LutFormat = value;
        }
    }
}
