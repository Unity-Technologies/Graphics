using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Global lighting quality settings.
    /// </summary>
    [Serializable]
    public sealed class GlobalLightingQualitySettings
    {
        static int s_QualitySettingCount = Enum.GetNames(typeof(ScalableSettingLevelParameter.Level)).Length;

        internal GlobalLightingQualitySettings()
        {
            /* Ambient Occlusion */
            AOStepCount[(int)ScalableSettingLevelParameter.Level.Low] = 4;
            AOStepCount[(int)ScalableSettingLevelParameter.Level.Medium] = 6;
            AOStepCount[(int)ScalableSettingLevelParameter.Level.High] = 16;

            AOFullRes[(int)ScalableSettingLevelParameter.Level.Low] = false;
            AOFullRes[(int)ScalableSettingLevelParameter.Level.Medium] = false;
            AOFullRes[(int)ScalableSettingLevelParameter.Level.High] = true;

            AOBilateralUpsample[(int)ScalableSettingLevelParameter.Level.Low] = false;
            AOBilateralUpsample[(int)ScalableSettingLevelParameter.Level.Medium] = true;
            AOBilateralUpsample[(int)ScalableSettingLevelParameter.Level.High] = true; // N/A

            AODirectionCount[(int)ScalableSettingLevelParameter.Level.Low] = 1;
            AODirectionCount[(int)ScalableSettingLevelParameter.Level.Medium] = 2;
            AODirectionCount[(int)ScalableSettingLevelParameter.Level.High] = 4;

            AOMaximumRadiusPixels[(int)ScalableSettingLevelParameter.Level.Low] = 32;
            AOMaximumRadiusPixels[(int)ScalableSettingLevelParameter.Level.Medium] = 40;
            AOMaximumRadiusPixels[(int)ScalableSettingLevelParameter.Level.High] = 80;

            /* Contact Shadow */
            ContactShadowSampleCount[(int)ScalableSettingLevelParameter.Level.Low] = 6;
            ContactShadowSampleCount[(int)ScalableSettingLevelParameter.Level.Medium] = 10;
            ContactShadowSampleCount[(int)ScalableSettingLevelParameter.Level.High] = 16;

            /* Screen Space Reflection */
            SSRMaxRaySteps[(int)ScalableSettingLevelParameter.Level.Low] = 16;
            SSRMaxRaySteps[(int)ScalableSettingLevelParameter.Level.Medium] = 32;
            SSRMaxRaySteps[(int)ScalableSettingLevelParameter.Level.High] = 64;

            /* Screen Space Global Illumination */
            SSGIRaySteps[(int)ScalableSettingLevelParameter.Level.Low] = 32;
            SSGIRaySteps[(int)ScalableSettingLevelParameter.Level.Medium] = 64;
            SSGIRaySteps[(int)ScalableSettingLevelParameter.Level.High] = 128;

            SSGIDenoise[(int)ScalableSettingLevelParameter.Level.Low] = true;
            SSGIDenoise[(int)ScalableSettingLevelParameter.Level.Medium] = true;
            SSGIDenoise[(int)ScalableSettingLevelParameter.Level.High] = true;

            SSGIHalfResDenoise[(int)ScalableSettingLevelParameter.Level.Low] = true;
            SSGIHalfResDenoise[(int)ScalableSettingLevelParameter.Level.Medium] = false;
            SSGIHalfResDenoise[(int)ScalableSettingLevelParameter.Level.High] = false;

            SSGIDenoiserRadius[(int)ScalableSettingLevelParameter.Level.Low] = 0.75f;
            SSGIDenoiserRadius[(int)ScalableSettingLevelParameter.Level.Medium] = 0.5f;
            SSGIDenoiserRadius[(int)ScalableSettingLevelParameter.Level.High] = 0.5f;

            SSGISecondDenoise[(int)ScalableSettingLevelParameter.Level.Low] = true;
            SSGISecondDenoise[(int)ScalableSettingLevelParameter.Level.Medium] = true;
            SSGISecondDenoise[(int)ScalableSettingLevelParameter.Level.High] = true;

            // Ray Traced Ambient Occlusion
            RTAORayLength[(int)ScalableSettingLevelParameter.Level.Low] = 0.5f;
            RTAORayLength[(int)ScalableSettingLevelParameter.Level.Medium] = 3.0f;
            RTAORayLength[(int)ScalableSettingLevelParameter.Level.High] = 20.0f;

            RTAOSampleCount[(int)ScalableSettingLevelParameter.Level.Low] = 1;
            RTAOSampleCount[(int)ScalableSettingLevelParameter.Level.Medium] = 2;
            RTAOSampleCount[(int)ScalableSettingLevelParameter.Level.High] = 8;

            RTAODenoise[(int)ScalableSettingLevelParameter.Level.Low] = true;
            RTAODenoise[(int)ScalableSettingLevelParameter.Level.Medium] = true;
            RTAODenoise[(int)ScalableSettingLevelParameter.Level.High] = true;

            RTAODenoiserRadius[(int)ScalableSettingLevelParameter.Level.Low] = 0.25f;
            RTAODenoiserRadius[(int)ScalableSettingLevelParameter.Level.Medium] = 0.5f;
            RTAODenoiserRadius[(int)ScalableSettingLevelParameter.Level.High] = 0.65f;

            // RTGI
            RTGIRayLength[(int)ScalableSettingLevelParameter.Level.Low] = 50.0f;
            RTGIRayLength[(int)ScalableSettingLevelParameter.Level.Medium] = 50.0f;
            RTGIRayLength[(int)ScalableSettingLevelParameter.Level.High] = 50.0f;

            RTGIFullResolution[(int)ScalableSettingLevelParameter.Level.Low] = false;
            RTGIFullResolution[(int)ScalableSettingLevelParameter.Level.Medium] = false;
            RTGIFullResolution[(int)ScalableSettingLevelParameter.Level.High] = true;

            RTGIClampValue[(int)ScalableSettingLevelParameter.Level.Low] = 2.0f;
            RTGIClampValue[(int)ScalableSettingLevelParameter.Level.Medium] = 3.0f;
            RTGIClampValue[(int)ScalableSettingLevelParameter.Level.High] = 5.0f;

            RTGIRaySteps[(int)ScalableSettingLevelParameter.Level.Low] = 32;
            RTGIRaySteps[(int)ScalableSettingLevelParameter.Level.Medium] = 48;
            RTGIRaySteps[(int)ScalableSettingLevelParameter.Level.High] = 64;

            RTGIDenoise[(int)ScalableSettingLevelParameter.Level.Low] = true;
            RTGIDenoise[(int)ScalableSettingLevelParameter.Level.Medium] = true;
            RTGIDenoise[(int)ScalableSettingLevelParameter.Level.High] = true;

            RTGIHalfResDenoise[(int)ScalableSettingLevelParameter.Level.Low] = true;
            RTGIHalfResDenoise[(int)ScalableSettingLevelParameter.Level.Medium] = false;
            RTGIHalfResDenoise[(int)ScalableSettingLevelParameter.Level.High] = false;

            RTGIDenoiserRadius[(int)ScalableSettingLevelParameter.Level.Low] = 1.0f;
            RTGIDenoiserRadius[(int)ScalableSettingLevelParameter.Level.Medium] = 1.0f;
            RTGIDenoiserRadius[(int)ScalableSettingLevelParameter.Level.High] = 1.0f;

            RTGISecondDenoise[(int)ScalableSettingLevelParameter.Level.Low] = true;
            RTGISecondDenoise[(int)ScalableSettingLevelParameter.Level.Medium] = true;
            RTGISecondDenoise[(int)ScalableSettingLevelParameter.Level.High] = true;

            // RTR
            RTRMinSmoothness[(int)ScalableSettingLevelParameter.Level.Low] = 0.6f;
            RTRMinSmoothness[(int)ScalableSettingLevelParameter.Level.Medium] = 0.4f;
            RTRMinSmoothness[(int)ScalableSettingLevelParameter.Level.High] = 0.0f;

            RTRSmoothnessFadeStart[(int)ScalableSettingLevelParameter.Level.Low] = 0.7f;
            RTRSmoothnessFadeStart[(int)ScalableSettingLevelParameter.Level.Medium] = 0.5f;
            RTRSmoothnessFadeStart[(int)ScalableSettingLevelParameter.Level.High] = 0.0f;

            RTRRayLength[(int)ScalableSettingLevelParameter.Level.Low] = 50.0f;
            RTRRayLength[(int)ScalableSettingLevelParameter.Level.Medium] = 50.0f;
            RTRRayLength[(int)ScalableSettingLevelParameter.Level.High] = 50.0f;

            RTRClampValue[(int)ScalableSettingLevelParameter.Level.Low] = 0.8f;
            RTRClampValue[(int)ScalableSettingLevelParameter.Level.Medium] = 1.0f;
            RTRClampValue[(int)ScalableSettingLevelParameter.Level.High] = 1.2f;

            RTRFullResolution[(int)ScalableSettingLevelParameter.Level.Low] = false;
            RTRFullResolution[(int)ScalableSettingLevelParameter.Level.Medium] = false;
            RTRFullResolution[(int)ScalableSettingLevelParameter.Level.High] = true;

            RTRRayMaxIterations[(int)ScalableSettingLevelParameter.Level.Low] = 32;
            RTRRayMaxIterations[(int)ScalableSettingLevelParameter.Level.Medium] = 48;
            RTRRayMaxIterations[(int)ScalableSettingLevelParameter.Level.High] = 64;

            RTRDenoise[(int)ScalableSettingLevelParameter.Level.Low] = true;
            RTRDenoise[(int)ScalableSettingLevelParameter.Level.Medium] = true;
            RTRDenoise[(int)ScalableSettingLevelParameter.Level.High] = true;

            RTRDenoiserRadius[(int)ScalableSettingLevelParameter.Level.Low] = 8;
            RTRDenoiserRadius[(int)ScalableSettingLevelParameter.Level.Medium] = 12;
            RTRDenoiserRadius[(int)ScalableSettingLevelParameter.Level.High] = 16;

            RTRSmoothDenoising[(int)ScalableSettingLevelParameter.Level.Low] = true;
            RTRSmoothDenoising[(int)ScalableSettingLevelParameter.Level.Medium] = false;
            RTRSmoothDenoising[(int)ScalableSettingLevelParameter.Level.High] = false;

            // Fog
            Fog_ControlMode[(int)ScalableSettingLevelParameter.Level.Low] = FogControl.Balance;
            Fog_ControlMode[(int)ScalableSettingLevelParameter.Level.Medium] = FogControl.Balance;
            Fog_ControlMode[(int)ScalableSettingLevelParameter.Level.High] = FogControl.Balance;

            Fog_Budget[(int)ScalableSettingLevelParameter.Level.Low] = 0.166f;
            Fog_Budget[(int)ScalableSettingLevelParameter.Level.Medium] = 0.33f;
            Fog_Budget[(int)ScalableSettingLevelParameter.Level.High] = 0.666f;

            Fog_DepthRatio[(int)ScalableSettingLevelParameter.Level.Low] = 0.666f;
            Fog_DepthRatio[(int)ScalableSettingLevelParameter.Level.Medium] = 0.666f;
            Fog_DepthRatio[(int)ScalableSettingLevelParameter.Level.High] = 0.50f;
        }

        internal static GlobalLightingQualitySettings NewDefault() => new GlobalLightingQualitySettings();

        // SSAO
        /// <summary>Ambient Occlusion step count for each quality level. The array must have one entry per scalable setting level, and elements must be between 2 and 32.</summary>
        [Range(2, 32)]
        public int[] AOStepCount = new int[s_QualitySettingCount];
        /// <summary>Ambient Occlusion uses full resolution buffer for each quality level. The array must have one entry per scalable setting level.</summary>
        public bool[] AOFullRes = new bool[s_QualitySettingCount];
        /// <summary>Ambient Occlusion maximum radius for each quality level. The array must have one entry per scalable setting level, and elements must be between 16 and 256.</summary>
        [Range(16, 256)]
        public int[] AOMaximumRadiusPixels = new int[s_QualitySettingCount];
        /// <summary>Ambient Occlusion uses bilateral upsample for each quality level. The array must have one entry per scalable setting level.</summary>
        public bool[] AOBilateralUpsample = new bool[s_QualitySettingCount];
        /// <summary>Ambient Occlusion direction count for each quality level. The array must have one entry per scalable setting level, and elements must be between 1 and 6.</summary>
        [Range(1, 6)]
        public int[] AODirectionCount = new int[s_QualitySettingCount];

        // Contact Shadows
        /// <summary>Contact shadow sample count for each quality level. The array must have one entry per scalable setting level, and elements must be between 4 and 64.</summary>
        [Range(4, 64)]
        public int[] ContactShadowSampleCount = new int[s_QualitySettingCount];

        // Screen Space Reflections
        /// <summary>Maximum number of rays for Screen Space Reflection for each quality level. The array must have one entry per scalable setting level, and elements must above 0.</summary>
        [Min(0)]
        public int[] SSRMaxRaySteps = new int[s_QualitySettingCount];

        // Screen Space Global Illumination
        /// <summary>Screen space global illumination step count for the ray marching. The array must have one entry per scalable setting level, and elements must above 0.</summary>
        [Min(0)]
        public int[] SSGIRaySteps = new int[s_QualitySettingCount];

        /// <summary>Flag that enables the first denoising pass.</summary>
        public bool[] SSGIDenoise = new bool[s_QualitySettingCount];
        /// <summary>Flag that defines if the denoiser should be evaluated at half resolution.</summary>
        public bool[] SSGIHalfResDenoise = new bool[s_QualitySettingCount];
        /// <summary>Flag that defines the radius of the first denoiser.</summary>
        public float[] SSGIDenoiserRadius = new float[s_QualitySettingCount];
        /// <summary>Flag that enables the second denoising pass.</summary>
        public bool[] SSGISecondDenoise = new bool[s_QualitySettingCount];

        // Ray Traced Ambient Occlusion
        /// <summary>Controls the length of ray traced ambient occlusion rays. The array must have one entry per scalable setting level, and elements must above 0.01.</summary>
        [Min(0.01f)]
        public float[] RTAORayLength = new float[s_QualitySettingCount];
        /// <summary>Number of samples for evaluating the effect. The array must have one entry per scalable setting level, and elements must be between 1 and 64.</summary>
        [Range(1, 64)]
        public int[] RTAOSampleCount = new int[s_QualitySettingCount];
        /// <summary>Defines if the ray traced ambient occlusion should be denoised. The array must have one entry per scalable setting level.</summary>
        public bool[] RTAODenoise = new bool[s_QualitySettingCount];
        /// <summary>Controls the radius of the ray traced ambient occlusion denoiser. The array must have one entry per scalable setting level, and elements must be between 0.001 and 1.</summary>
        [Range(0.001f, 1.0f)]
        public float[] RTAODenoiserRadius = new float[s_QualitySettingCount];

        // Ray Traced Global Illumination
        /// <summary>Controls the length of ray traced global illumination rays. The array must have one entry per scalable setting level, and elements must above 0.01.</summary>
        [Min(0.01f)]
        public float[] RTGIRayLength = new float[s_QualitySettingCount];
        /// <summary>Controls if the effect should be computed at full resolution. The array must have one entry per scalable setting level.</summary>
        public bool[] RTGIFullResolution = new bool[s_QualitySettingCount];
        /// <summary>Clamp value used to reduce the variance in the integration signal. The array must have one entry per scalable setting level, and elements must be between 0.001 and 10.</summary>
        [Range(0.001f, 10.0f)]
        public float[] RTGIClampValue = new float[s_QualitySettingCount];
        /// <summary>Controls the number of ray steps for hybrid tracing. The array must have one entry per scalable setting level, and elements must above 0.</summary>
        [Min(0)]
        public int[] RTGIRaySteps = new int[s_QualitySettingCount];
        /// <summary>Flag that enables the first denoising pass. The array must have one entry per scalable setting level.</summary>
        public bool[] RTGIDenoise = new bool[s_QualitySettingCount];
        /// <summary>Flag that defines if the denoiser should be evaluated at half resolution. The array must have one entry per scalable setting level.</summary>
        public bool[] RTGIHalfResDenoise = new bool[s_QualitySettingCount];
        /// <summary>Flag that defines the radius of the first denoiser. The array must have one entry per scalable setting level, and elements must be between 0.001 and 1.0.</summary>
        [Range(0.001f, 1.0f)]
        public float[] RTGIDenoiserRadius = new float[s_QualitySettingCount];
        /// <summary>Flag that enables the second denoising pass. The array must have one entry per scalable setting level.</summary>
        public bool[] RTGISecondDenoise = new bool[s_QualitySettingCount];

        // Ray Traced Reflections
        /// <summary>Controls the minimal smoothness. The array must have one entry per scalable setting level, and elements must be between 0 and 1.</summary>
        [Range(0.0f, 1.0f)]
        public float[] RTRMinSmoothness = new float[s_QualitySettingCount];
        /// <summary>Controls the minimal smoothness. The array must have one entry per scalable setting level, and elements must be between 0 and 1.</summary>
        [Range(0.0f, 1.0f)]
        public float[] RTRSmoothnessFadeStart = new float[s_QualitySettingCount];
        /// <summary>Controls the length of ray traced reflection rays. The array must have one entry per scalable setting level, and elements must above 0.01.</summary>
        [Min(0.01f)]
        public float[] RTRRayLength = new float[s_QualitySettingCount];
        /// <summary>Clamp value used to reduce the variance in the integration signal. The array must have one entry per scalable setting level, and elements must be between 0.001 and 10.</summary>
        [Range(0.001f, 10.0f)]
        public float[] RTRClampValue = new float[s_QualitySettingCount];
        /// <summary>Controls if the effect should be computed at full resolution. The array must have one entry per scalable setting level.</summary>
        public bool[] RTRFullResolution = new bool[s_QualitySettingCount];
        /// <summary>Controls if the effect should be computed at full resolution. The array must have one entry per scalable setting level, and elements must above 0.</summary>
        [Min(0)]
        public int[] RTRRayMaxIterations = new int[s_QualitySettingCount];
        /// <summary>Flag that enables the first denoising pass. The array must have one entry per scalable setting level.</summary>
        public bool[] RTRDenoise = new bool[s_QualitySettingCount];
        /// <summary>Flag that defines the radius of the first denoiser. The array must have one entry per scalable setting level, and elements must be between 1 and 32.</summary>
        [Range(1, 32)]
        public int[] RTRDenoiserRadius = new int[s_QualitySettingCount];
        /// <summary>Flag that defines smooth denoising status. The array must have one entry per scalable setting level.</summary>
        public bool[] RTRSmoothDenoising = new bool[s_QualitySettingCount];

        // TODO: Volumetric fog quality
        /// <summary>Controls which control mode should be used to define the volumetric fog parameters. The array must have one entry per scalable setting level.</summary>
        public FogControl[] Fog_ControlMode = new FogControl[s_QualitySettingCount];
        /// <summary>Controls the budget of the volumetric fog effect. The array must have one entry per scalable setting level, and elements must be between 0 and 1.</summary>
        [Range(0.0f, 1.0f)]
        public float[] Fog_Budget = new float[s_QualitySettingCount];
        /// <summary>Controls how the budget is shared between screen resolution and depth. The array must have one entry per scalable setting level, and elements must be between 0 and 1.</summary>
        [Range(0.0f, 1.0f)]
        public float[] Fog_DepthRatio = new float[s_QualitySettingCount];

        // TODO: Shadows. This needs to be discussed further as there is an idiosyncracy here as we have different level of quality settings,
        //some for resolution per light (4 levels) some per volume (which are 3 levels everywhere). This needs to be discussed more.
    }
}
