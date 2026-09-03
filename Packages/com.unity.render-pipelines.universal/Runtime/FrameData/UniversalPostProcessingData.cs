using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Settings used for Post Processing.
    /// </summary>
    public class UniversalPostProcessingData : ContextItem
    {
        /// <summary>
        /// True if post-processing effect is enabled while rendering the camera stack.
        /// </summary>
        public bool isEnabled;

        /// <summary>
        /// The <c>ColorGradingMode</c> to use.
        /// </summary>
        /// <seealso cref="ColorGradingMode"/>
        public ColorGradingMode gradingMode;

        /// <summary>
        /// The size of the Look Up Table (LUT)
        /// </summary>
        public int lutSize;

        /// <summary>
        /// True if fast approximation functions are used when converting between the sRGB and Linear color spaces, false otherwise.
        /// </summary>
        public bool useFastSRGBLinearConversion;

        /// <summary>
        /// Returns true if Screen Space Lens Flare are supported by this asset, false otherwise.
        /// </summary>
        public bool supportScreenSpaceLensFlare;

        /// <summary>
        /// Returns true if Data Driven Lens Flare are supported by this asset, false otherwise.
        /// </summary>
        public bool supportDataDrivenLensFlare;

#if ENABLE_UPSCALER_FRAMEWORK
        /// <summary>
        /// Returns null if there isn't an active upscaler
        /// </summary>
        internal IUpscaler activeUpscaler;

        /// <summary>
        /// True when the active upscaler is one of the built-in embedded upscalers.
        /// </summary>
        internal bool activeUpscalerIsEmbedded;
#endif

        /// <summary>
        /// Empty function added for the IDisposable interface.
        /// </summary>
        public override void Reset()
        {
            isEnabled = default;
            gradingMode = ColorGradingMode.LowDynamicRange;
            lutSize = 0;
            useFastSRGBLinearConversion = false;
            supportScreenSpaceLensFlare = false;
            supportDataDrivenLensFlare = false;

#if ENABLE_UPSCALER_FRAMEWORK
            activeUpscaler = null;
            activeUpscalerIsEmbedded = false;
#endif
        }
    }
}
