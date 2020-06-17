//-----------------------------------------------------------------------------
// Configuration
//-----------------------------------------------------------------------------

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(PackingRules.Exact)]
    public enum ProbeVolumesEvaluationModes
    {
        Disabled = 0,
        LightLoop = 1,
        MaterialPass = 2,
    }

    [GenerateHLSL(PackingRules.Exact)]
    public enum ProbeVolumesEncodingModes
    {
        SphericalHarmonicsL0 = 0,
        SphericalHarmonicsL1 = 1,
        SphericalHarmonicsL2 = 2
    }

    [GenerateHLSL(PackingRules.Exact)]
    public enum ProbeVolumesBilateralFilteringModes
    {
        Disabled = 0,
        Validity = 1,
        OctahedralDepth = 2
    }

    [GenerateHLSL(PackingRules.Exact)]
    public enum ShaderOptions
    {
        ColoredShadow = 1, // Allow to defined if colored shadow are supported in shaders or not
        CameraRelativeRendering = 1, // Rendering sets the origin of the world to the position of the primary (scene view) camera
        PreExposition = 1,
        PrecomputedAtmosphericAttenuation = 0, // Precomputes atmospheric attenuation for the directional light on the CPU, which makes it independent from the fragment's position, which is faster but wrong
#if ENABLE_RAYTRACING
        Raytracing = 1,
#else
        Raytracing = 0,
#endif
#if ENABLE_VR
        XrMaxViews = 2, // Used for single-pass rendering (with fast path in vertex shader code when forced to 2)
#else
        XrMaxViews = 1,
#endif

        // Warning: Probe Volumes is a highly experimental feature. It is disabled by default for this reason.
        // It's functionality is subject to breaking changes and whole sale removal.
        // It is not recommended for use outside of for providing feedback. It should not be used in production.
        // To enable, set:
        // ProbeVolumesEvaluationMode = ProbeVolumesEvaluationModes.MaterialPass
        // and inside of the editor run:
        // Edit->Render Pipeline->Generate Shader Includes
        // Probe Volumes feature must also be enabled inside of your HDRenderPipelineAsset.
        ProbeVolumesEvaluationMode = ProbeVolumesEvaluationModes.Disabled,
        ProbeVolumesAdditiveBlending = 1,
        ProbeVolumesBilateralFilteringMode = ProbeVolumesBilateralFilteringModes.Validity,
        ProbeVolumesEncodingMode = ProbeVolumesEncodingModes.SphericalHarmonicsL1,

        AreaLights = 1,

        BarnDoor = 0
    };

    // Note: #define can't be use in include file in C# so we chose this way to configure both C# and hlsl
    // Changing a value in this enum Config here require to regenerate the hlsl include and recompile C# and shaders
    public class ShaderConfig
    {
        public const int k_XRMaxViewsForCBuffer = 2;         // REALLY IMPORTANT! This needs to be the maximum possible XrMaxViews for any supported platform!
                                                             // this needs to be constant and not vary like XrMaxViews does as it is used to generate the cbuffer declarations

        public static int s_CameraRelativeRendering = (int)ShaderOptions.CameraRelativeRendering;
        public static int s_PreExposition = (int)ShaderOptions.PreExposition;
        public static int s_XrMaxViews = (int)ShaderOptions.XrMaxViews;
        public static int s_PrecomputedAtmosphericAttenuation = (int)ShaderOptions.PrecomputedAtmosphericAttenuation;
        public static ProbeVolumesEvaluationModes s_ProbeVolumesEvaluationMode = (ProbeVolumesEvaluationModes)ShaderOptions.ProbeVolumesEvaluationMode;
        public static int s_ProbeVolumesAdditiveBlending = (int)ShaderOptions.ProbeVolumesAdditiveBlending;
        public static ProbeVolumesBilateralFilteringModes s_ProbeVolumesBilateralFilteringMode = (ProbeVolumesBilateralFilteringModes)ShaderOptions.ProbeVolumesBilateralFilteringMode;
        public static ProbeVolumesEncodingModes s_ProbeVolumesEncodingMode = (ProbeVolumesEncodingModes)ShaderOptions.ProbeVolumesEncodingMode;
        public static int s_AreaLights = (int)ShaderOptions.AreaLights;
        public static int s_BarnDoor = (int)ShaderOptions.BarnDoor;
    }
}
