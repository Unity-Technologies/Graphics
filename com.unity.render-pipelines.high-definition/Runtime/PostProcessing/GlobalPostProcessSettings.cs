using System;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Available color grading LUT formats.
    /// </summary>
    /// <seealso cref="GlobalPostProcessSettings.lutFormat"/>
    public enum GradingLutFormat
    {
        /// <summary>
        /// R11G11B10. Fastest lookup format but can result in a loss of precision in some extreme cases.
        /// </summary>
        R11G11B10 = GraphicsFormat.B10G11R11_UFloatPack32,

        /// <summary>
        /// 16 bit per channel.
        /// </summary>
        R16G16B16A16 = GraphicsFormat.R16G16B16A16_SFloat,

        /// <summary>
        /// 32 bit per channel. Should only be used in extreme grading cases.
        /// </summary>
        R32G32B32A32 = GraphicsFormat.R32G32B32A32_SFloat
    }

    /// <summary>
    /// Format of intermediate buffers for post processing.
    /// </summary>
    public enum PostProcessBufferFormat
    {
        /// <summary>
        /// R11G11B10. Fastest lookup format but can result in a loss of precision in some extreme cases.
        /// </summary>
        R11G11B10 = GraphicsFormat.B10G11R11_UFloatPack32,
        /// <summary>
        /// 16 bit per channel.
        /// </summary>
        R16G16B16A16 = GraphicsFormat.R16G16B16A16_SFloat,
        /// <summary>
        /// 32 bit per channel. Should only be used in extreme cases.
        /// </summary>
        R32G32B32A32 = GraphicsFormat.R32G32B32A32_SFloat
    }

    /// <summary>
    /// Project-wide settings related to post-processing.
    /// </summary>
    [Serializable]
    public struct GlobalPostProcessSettings
    {
        internal static GlobalPostProcessSettings NewDefault() => new GlobalPostProcessSettings()
        {
            lutSize = 32,
            lutFormat = GradingLutFormat.R16G16B16A16,
            bufferFormat = PostProcessBufferFormat.R11G11B10
        };

        // Returns true if the post-processing passes support an alpha channel
        internal bool supportsAlpha => bufferFormat != PostProcessBufferFormat.R11G11B10;

        // Note: A lut size of 16^3 is barely usable (noticeable color banding in highly contrasted
        // areas and harsh tonemappers like ACES'). 32 should be the minimum, the lut being encoded
        // in log. Lower sizes would work better with an additional 1D shaper lut but for now we'll
        // keep it simple.

        /// <summary>
        /// The minimum allowed size for the color grading LUT.
        /// </summary>
        public const int k_MinLutSize = 16;

        /// <summary>
        /// The maximum allowed size for the color grading LUT.
        /// </summary>
        public const int k_MaxLutSize = 65;

        [SerializeField]
        int m_LutSize;

        /// <summary>
        /// Project-wide LUT size used for the internal color grading LUT and external LUTs.
        /// </summary>
        public int lutSize
        {
            get => m_LutSize;
            set => m_LutSize = Mathf.Clamp(value, k_MinLutSize, k_MaxLutSize);
        }

        /// <summary>
        /// The texture format to use to store the internal color gradint LUT.
        /// </summary>
        /// <seealso cref="GradingLutFormat"/>
        [FormerlySerializedAs("m_LutFormat")]
        public GradingLutFormat lutFormat;

        /// <summary>
        /// The texture format to be used for the post-processing passes.
        /// </summary>
        public PostProcessBufferFormat bufferFormat;
    }
}
