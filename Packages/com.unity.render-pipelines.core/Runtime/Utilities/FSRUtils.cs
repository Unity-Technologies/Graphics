using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
#if ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE
using UnityEngine.AMD;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Utility functions for FidelityFX Super Resolution (FSR).
    /// - FSR1: Spatial upscaling via EASU/RCAS shaders (used with FSRCommon.hlsl)
    /// - FSR2/3/4: Temporal upscaling via AMD plugin
    /// </summary>
    public static class FSRUtils
    {
        #region FSR1 Spatial Upscaling

        /// Shader constant ids used to communicate with the FSR shader implementation
        static class ShaderConstants
        {
            // EASU
            public static readonly int _FsrEasuConstants0 = Shader.PropertyToID("_FsrEasuConstants0");
            public static readonly int _FsrEasuConstants1 = Shader.PropertyToID("_FsrEasuConstants1");
            public static readonly int _FsrEasuConstants2 = Shader.PropertyToID("_FsrEasuConstants2");
            public static readonly int _FsrEasuConstants3 = Shader.PropertyToID("_FsrEasuConstants3");

            // RCAS
            public static readonly int _FsrRcasConstants = Shader.PropertyToID("_FsrRcasConstants");
        }

        /// <summary>
        /// Sets the constant values required by the FSR EASU shader on the provided command buffer
        ///
        /// Logic ported from "FsrEasuCon()" in Runtime/PostProcessing/Shaders/ffx/ffx_fsr1.hlsl
        /// </summary>
        /// <param name="cmd">Command buffer to modify</param>
        /// <param name="inputViewportSizeInPixels">This the rendered image resolution being upscaled</param>
        /// <param name="inputImageSizeInPixels">This is the resolution of the resource containing the input image (useful for dynamic resolution)</param>
        /// <param name="outputImageSizeInPixels">This is the display resolution which the input image gets upscaled to</param>
        public static void SetEasuConstants(CommandBuffer cmd, Vector2 inputViewportSizeInPixels, Vector2 inputImageSizeInPixels, Vector2 outputImageSizeInPixels)
        {
            Vector4 constants0;
            Vector4 constants1;
            Vector4 constants2;
            Vector4 constants3;

            // Output integer position to a pixel position in viewport.
            constants0.x = (inputViewportSizeInPixels.x / outputImageSizeInPixels.x);
            constants0.y = (inputViewportSizeInPixels.y / outputImageSizeInPixels.y);
            constants0.z = (0.5f * inputViewportSizeInPixels.x / outputImageSizeInPixels.x - 0.5f);
            constants0.w = (0.5f * inputViewportSizeInPixels.y / outputImageSizeInPixels.y - 0.5f);

            // Viewport pixel position to normalized image space.
            // This is used to get upper-left of 'F' tap.
            constants1.x = (1.0f / inputImageSizeInPixels.x);
            constants1.y = (1.0f / inputImageSizeInPixels.y);

            // Centers of gather4, first offset from upper-left of 'F'.
            //      +---+---+
            //      |   |   |
            //      +--(0)--+
            //      | b | c |
            //  +---F---+---+---+
            //  | e | f | g | h |
            //  +--(1)--+--(2)--+
            //  | i | j | k | l |
            //  +---+---+---+---+
            //      | n | o |
            //      +--(3)--+
            //      |   |   |
            //      +---+---+
            constants1.z = (1.0f / inputImageSizeInPixels.x);
            constants1.w = (-1.0f / inputImageSizeInPixels.y);

            // These are from (0) instead of 'F'.
            constants2.x = (-1.0f / inputImageSizeInPixels.x);
            constants2.y = (2.0f / inputImageSizeInPixels.y);
            constants2.z = (1.0f / inputImageSizeInPixels.x);
            constants2.w = (2.0f / inputImageSizeInPixels.y);

            constants3.x = (0.0f / inputImageSizeInPixels.x);
            constants3.y = (4.0f / inputImageSizeInPixels.y);

            // Fill the last constant with zeros to avoid using uninitialized memory
            constants3.z = 0.0f;
            constants3.w = 0.0f;

            cmd.SetGlobalVector(ShaderConstants._FsrEasuConstants0, constants0);
            cmd.SetGlobalVector(ShaderConstants._FsrEasuConstants1, constants1);
            cmd.SetGlobalVector(ShaderConstants._FsrEasuConstants2, constants2);
            cmd.SetGlobalVector(ShaderConstants._FsrEasuConstants3, constants3);
        }

        /// <summary>
        /// Sets the constant values required by the FSR EASU shader on the provided command buffer
        ///
        /// Logic ported from "FsrEasuCon()" in Runtime/PostProcessing/Shaders/ffx/ffx_fsr1.hlsl
        /// </summary>
        /// <param name="cmd">RasterCommandBuffer/ComputeCommandBuffer/UnsafeCommandBuffer to modify</param>
        /// <param name="inputViewportSizeInPixels">This the rendered image resolution being upscaled</param>
        /// <param name="inputImageSizeInPixels">This is the resolution of the resource containing the input image (useful for dynamic resolution)</param>
        /// <param name="outputImageSizeInPixels">This is the display resolution which the input image gets upscaled to</param>
        public static void SetEasuConstants(BaseCommandBuffer cmd, Vector2 inputViewportSizeInPixels,
            Vector2 inputImageSizeInPixels, Vector2 outputImageSizeInPixels)
        {
            SetEasuConstants(cmd.m_WrappedCommandBuffer, inputViewportSizeInPixels, inputImageSizeInPixels, outputImageSizeInPixels);
        }

        /// <summary>
        /// The maximum sharpness value in stops before the effect of RCAS is no longer visible.
        /// This value is used to map between linear and stops.
        /// </summary>
        internal const float kMaxSharpnessStops = 2.5f;

        /// <summary>
        /// AMD's FidelityFX Super Resolution integration guide recommends a value of 0.2 for the RCAS sharpness parameter when specified in stops
        /// </summary>
        public const float kDefaultSharpnessStops = 0.2f;

        /// <summary>
        /// The default RCAS sharpness parameter as a linear value
        /// </summary>
        public const float kDefaultSharpnessLinear = (1.0f - (kDefaultSharpnessStops / kMaxSharpnessStops));

        /// <summary>
        /// Sets the constant values required by the FSR RCAS shader on the provided command buffer
        ///
        /// Logic ported from "FsrRcasCon()" in Runtime/PostProcessing/Shaders/ffx/ffx_fsr1.hlsl
        /// For a more user-friendly version of this function, see SetRcasConstantsLinear().
        /// </summary>
        /// <param name="cmd">Command buffer to modify</param>
        /// <param name="sharpnessStops">The scale is {0.0 := maximum, to N>0, where N is the number of stops(halving) of the reduction of sharpness</param>
        public static void SetRcasConstants(CommandBuffer cmd, float sharpnessStops = kDefaultSharpnessStops)
        {
            // Transform from stops to linear value.
            float sharpnessLinear = Mathf.Pow(2.0f, -sharpnessStops);

            Vector4 constants;

            uint sharpnessAsHalf = Mathf.FloatToHalf(sharpnessLinear);
            int packedSharpness = (int)(sharpnessAsHalf | (sharpnessAsHalf << 16));
            float packedSharpnessAsFloat = BitConverter.Int32BitsToSingle(packedSharpness);

            constants.x = sharpnessLinear;
            constants.y = packedSharpnessAsFloat;

            // Fill the last constant with zeros to avoid using uninitialized memory
            constants.z = 0.0f;
            constants.w = 0.0f;

            cmd.SetGlobalVector(ShaderConstants._FsrRcasConstants, constants);
        }

        /// <summary>
        /// Sets the constant values required by the FSR RCAS shader on the provided command buffer
        ///
        /// Logic ported from "FsrRcasCon()" in Runtime/PostProcessing/Shaders/ffx/ffx_fsr1.hlsl
        /// For a more user-friendly version of this function, see SetRcasConstantsLinear().
        /// </summary>
        /// <param name="cmd">Command buffer to modify</param>
        /// <param name="sharpnessStops">The scale is {0.0 := maximum, to N>0, where N is the number of stops(halving) of the reduction of sharpness</param>
        public static void SetRcasConstants(BaseCommandBuffer cmd, float sharpnessStops = kDefaultSharpnessStops)
        {
            SetRcasConstants(cmd.m_WrappedCommandBuffer, sharpnessStops);
        }

        /// <summary>
        /// Sets the constant values required by the FSR RCAS shader on the provided command buffer
        ///
        /// Equivalent to SetRcasConstants(), but handles the sharpness parameter as a linear value instead of one specified in stops.
        /// This is intended to simplify code that allows users to configure the sharpening behavior from a GUI.
        /// </summary>
        /// <param name="cmd">Command buffer to modify</param>
        /// <param name="sharpnessLinear">The level of intensity of the sharpening filter where 0.0 is the least sharp and 1.0 is the most sharp</param>
        public static void SetRcasConstantsLinear(CommandBuffer cmd, float sharpnessLinear = kDefaultSharpnessLinear)
        {
            // Ensure that the input value is between 0.0 and 1.0 prevent incorrect results
            Assertions.Assert.IsTrue((sharpnessLinear >= 0.0f) && (sharpnessLinear <= 1.0f));

            float sharpnessStops = (1.0f - sharpnessLinear) * kMaxSharpnessStops;

            SetRcasConstants(cmd, sharpnessStops);
        }

        /// <summary>
        /// Sets the constant values required by the FSR RCAS shader on the provided command buffer
        ///
        /// Equivalent to SetRcasConstants(), but handles the sharpness parameter as a linear value instead of one specified in stops.
        /// This is intended to simplify code that allows users to configure the sharpening behavior from a GUI.
        /// </summary>
        /// <param name="cmd">RasterCommandBuffer to modify</param>
        /// <param name="sharpnessLinear">The level of intensity of the sharpening filter where 0.0 is the least sharp and 1.0 is the most sharp</param>
        public static void SetRcasConstantsLinear(RasterCommandBuffer cmd, float sharpnessLinear = kDefaultSharpnessLinear)
        {
            SetRcasConstantsLinear(cmd.m_WrappedCommandBuffer, sharpnessLinear);
        }

        /// <summary>
        /// Returns true if FidelityFX Super Resolution (FSR) is supported on the current system
        /// FSR requires the textureGather shader instruction which wasn't supported by OpenGL ES until version 3.1
        /// </summary>
        /// <returns>True if supported</returns>
        public static bool IsSupported()
        {
            return SystemInfo.graphicsShaderLevel >= 45;
        }

        #endregion

        #region FSR2/3/4 Temporal Upscaling (AMD Plugin)

#if ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE

        /// <summary>
        /// Checks if the AMD plugin and graphics device are available for FSR2/3/4.
        /// </summary>
        public static bool CheckPluginAvailable()
        {
            if (!AMDUnityPlugin.IsLoaded())
            {
                Debug.LogWarning("AMDUnityPlugin not loaded.");
                return false;
            }

            // Check if device already exists to avoid redundant re-initialization
            if (GraphicsDevice.device != null)
                return true;

            GraphicsDevice device = GraphicsDevice.CreateGraphicsDevice();
            if (device == null)
            {
                Debug.LogWarning("AMDUnityPlugin failed to create device.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates the number of jitter phases based on the upscale ratio.
        /// FSR uses Halton sequence jittering for temporal anti-aliasing.
        /// </summary>
        public static int CalculateJitterPhaseCount(float upscaleRatio)
        {
            const float basePhaseCount = 8.0f;
            // Round half up, don't truncate: upscaleRatio is reconstructed as display/round(renderSize), so a clean preset
            // (e.g. 1.5x) arrives slightly low (~1.497) and 8*1.497^2 = 17.93 would truncate to 17 instead of 18.
            return Mathf.FloorToInt(basePhaseCount * upscaleRatio * upscaleRatio + 0.5f);
        }

        /// <summary>
        /// Calculates jitter offset using Halton sequence for temporal upscaling.
        /// </summary>
        public static Vector2 CalculateJitter(int frameIndex, float upscaleRatio)
        {
            int numPhases = CalculateJitterPhaseCount(upscaleRatio);
            int haltonIndex = (frameIndex % numPhases) + 1;
            float x = HaltonSequence.Get(haltonIndex, 2) - 0.5f;
            float y = HaltonSequence.Get(haltonIndex, 3) - 0.5f;
            return new Vector2(x, y);
        }

        /// <summary>
        /// Calculates mip bias to compensate for upscaling resolutions.
        /// </summary>
        public static float CalculateMipBias(Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution)
        {
            float xBias = Mathf.Log((float)preUpscaleResolution.x / postUpscaleResolution.x, 2f);
            float yBias = Mathf.Log((float)preUpscaleResolution.y / postUpscaleResolution.y, 2f);
            return Mathf.Min(xBias, yBias) - 1.0f;
        }

        /// <summary>
        /// Creates output texture for upscaled result in RenderGraph.
        /// </summary>
        public static TextureHandle CreateOutputTexture(RenderGraph renderGraph, UpscalingIO io, string textureName)
        {
            TextureDesc inputDesc = io.cameraColor.GetDescriptor(renderGraph);
            TextureDesc outputDesc = inputDesc;
            outputDesc.width = io.postUpscaleResolution.x;
            outputDesc.height = io.postUpscaleResolution.y;
            outputDesc.format = GraphicsFormatUtility.GetLinearFormat(inputDesc.format);
            outputDesc.msaaSamples = MSAASamples.None;
            outputDesc.useMipMap = false;
            outputDesc.autoGenerateMips = false;
            outputDesc.useDynamicScale = false;
            outputDesc.anisoLevel = 0;
            outputDesc.discardBuffer = false;
            outputDesc.enableRandomWrite = true;
            outputDesc.name = textureName;
            outputDesc.clearBuffer = false;
            outputDesc.filterMode = FilterMode.Bilinear;
            return renderGraph.CreateTexture(outputDesc);
        }

        /// <summary>
        /// Populates FSR2/3/4 execution data from UpscalingIO.
        /// </summary>
        public static void PopulateExecutionData(
            ref FSR2CommandExecutionData execData,
            UpscalingIO io,
            Vector2 jitter,
            bool enableSharpening,
            float sharpness)
        {
            float motionVectorSign = io.motionVectorDirection == UpscalingIO.MotionVectorDirection.PreviousFrameToCurrentFrame ? -1.0f : 1.0f;
            float motionVectorScaleX = io.motionVectorDomain == UpscalingIO.MotionVectorDomain.NDC ? io.motionVectorTextureSize.x : 1.0f;
            float motionVectorScaleY = io.motionVectorDomain == UpscalingIO.MotionVectorDomain.NDC ? io.motionVectorTextureSize.y : 1.0f;

            execData.enableSharpening = enableSharpening ? 1 : 0;
            execData.sharpness = sharpness;
            execData.MVScaleX = motionVectorSign * motionVectorScaleX;
            execData.MVScaleY = motionVectorSign * motionVectorScaleY;
            execData.renderSizeWidth = (uint)io.preUpscaleResolution.x;
            execData.renderSizeHeight = (uint)io.preUpscaleResolution.y;
            execData.jitterOffsetX = jitter.x;
            execData.jitterOffsetY = jitter.y;
            execData.cameraNear = io.nearClipPlane;
            execData.cameraFar = io.farClipPlane;
            execData.cameraFovAngleVertical = 2.0f * (float)Math.PI * (1 / 360.0f) * io.fieldOfViewDegrees;
            execData.preExposure = 1.0f;
            execData.frameTimeDelta = io.deltaTime * 1000.0f;
            execData.reset = io.resetHistory ? 1 : 0;
        }

        internal class ResolutionSuggestion
        {
            // Below it FSR still runs but quality degrades. Advisory only, never clamped.
            // Two bounds, because the recommended max upscale is tighter under dynamic resolution (~1.5x → ~0.667)
            // than for static scaling (~3x → ~0.33).
            public float m_CachedScaleStatic = -1.0f;  // < 0 means not yet queried
            public float m_CachedScaleDynamic = -1.0f; // < 0 means not yet queried

            // Last render scale (rounded to 2 decimals) we logged a notice for; NaN means none. Tracked so a sustained or
            // jittering below-recommended scale logs once per distinct value instead of every frame.
            public float m_LastNotifiedBelowScale = float.NaN;

            private string m_tag; // "FSR2", "FSR3", or "FSR4"
            private Func<float> m_staticMinScaleQuery;
            private Func<float> m_dynamicMinScaleQuery;

            public ResolutionSuggestion(string tag, Func<float> staticMinScaleQuery, Func<float> dynamicMinScaleQuery)
            {
                m_tag = tag;
                m_staticMinScaleQuery = staticMinScaleQuery;
                m_dynamicMinScaleQuery = dynamicMinScaleQuery;
            }

            public float GetRecommendedMinScale()
            {
                return QueryRecommendedMinScale(m_staticMinScaleQuery, ref m_CachedScaleStatic);
            }

            private float QueryRecommendedMinScale(Func<float> ratioQuery, ref float cache)
            {
                if (cache >= 0.0f)
                    return cache;
                if (GraphicsDevice.device == null)
                    return 0.0f; // 0 = no recommendation
                float upscaleRatio = ratioQuery();
                if (upscaleRatio <= 0.0f)
                    return 0.0f; // not cached; allow retry
                return cache = 1.0f / upscaleRatio;
            }

            // FSR keeps working below its recommended render scale but image quality degrades, so log an informational notice
            // (never clamp the user's chosen scale). INFO severity matches the editor's HelpBox for this soft floor; hard
            // [min,max] clamps warn instead. preUpscaleResolution reflects both Render Scale and hardware DRS
            // (ScalableBufferManager), so this covers both.
            public void NotifyIfBelowRecommendedRenderScale(UpscalingIO io)
            {
                bool dynamicResolution = io.dynamicResolution.HasValue;
                float recommendedMinScale = dynamicResolution
                    ? QueryRecommendedMinScale(m_dynamicMinScaleQuery, ref m_CachedScaleDynamic)
                    : QueryRecommendedMinScale(m_staticMinScaleQuery, ref m_CachedScaleStatic);

                if (recommendedMinScale <= 0.0f)
                    return; // device provided no ratio

                if (io.postUpscaleResolution.x <= 0.0f)
                    return; // guard against a degenerate display size (early init / resize) before dividing

                float scale = (float)io.preUpscaleResolution.x / io.postUpscaleResolution.x;
                if (scale >= recommendedMinScale)
                {
                    m_LastNotifiedBelowScale = float.NaN; // at/above the recommendation: clear it so dropping below notifies again
                    return;
                }

                float roundedScale = Mathf.Floor(scale * 100f + 0.5f) / 100f;
                if (Mathf.Approximately(m_LastNotifiedBelowScale, roundedScale))
                    return;

                m_LastNotifiedBelowScale = roundedScale;

                string mode = dynamicResolution ? "with dynamic resolution" : "at a fixed Render Scale";
                Debug.Log(
                    $"{m_tag} is rendering at Render Scale {scale:0.00} {mode}, below its recommended minimum of " +
                    $"{recommendedMinScale:0.00} (beyond {m_tag}'s recommended maximum upscale ratio for this mode). {m_tag} still " +
                    "runs, but image quality degrades at this scale. Raise Render Scale / dynamic resolution to stay at or " +
                    "above the recommended minimum.");
            }
        }

#endif // ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE

        #endregion
    }
}
