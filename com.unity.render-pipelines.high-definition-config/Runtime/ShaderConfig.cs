//-----------------------------------------------------------------------------
// Configuration
//-----------------------------------------------------------------------------

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Probe Volume Evaluation Mode.
    /// </summary>
    [GenerateHLSL(PackingRules.Exact)]
    public enum ProbeVolumesEvaluationModes
    {
        /// <summary>Probe volumes are disabled.</summary>
        Disabled = 0,
        /// <summary>Probe volumes are evaluated in the light loop.</summary>
        LightLoop = 1,
        /// <summary>Probe volumes are evaluated in the material pass.</summary>
        MaterialPass = 2,
    }

    /// <summary>
    /// Encoding of Probe Volumes.
    /// </summary>
    [GenerateHLSL(PackingRules.Exact)]
    public enum ProbeVolumesEncodingModes
    {
        /// <summary>Encoded using L0 spherical harmonics.</summary>
        SphericalHarmonicsL0 = 0,
        /// <summary>Encoded using L1 spherical harmonics.</summary>
        SphericalHarmonicsL1 = 1,
        /// <summary>Encoded using L2 spherical harmonics.</summary>
        SphericalHarmonicsL2 = 2
    }

    /// <summary>
    /// Probe Volume bilateral filtering mode.
    /// </summary>
    [GenerateHLSL(PackingRules.Exact)]
    public enum ProbeVolumesBilateralFilteringModes
    {
        /// <summary>Bilateral filtering is disabled.</summary>
        Disabled = 0,
        /// <summary>Bilateral filtering using validity.</summary>
        Validity = 1,
        /// <summary>Bilateral filtering using octahedral depth.</summary>
        OctahedralDepth = 2
    }

    /// <summary>
    /// Project wide shader configuration options.
    /// This enum will generate the proper shader defines.
    /// </summary>
    [GenerateHLSL(PackingRules.Exact)]
    public enum ShaderOptions
    {
        /// <summary>Support for colored shadows.</summary>
        ColoredShadow = 1,
        /// <summary>Use camera relative rendering to enhance precision.</summary>
        CameraRelativeRendering = 1,
        /// <summary>Use pre-exposition to enhance color precision.</summary>
        PreExposition = 1,
        /// <summary>Precomputes atmospheric attenuation for the directional light on the CPU, which makes it independent from the fragment's position, which is faster but wrong.</summary>
        PrecomputedAtmosphericAttenuation = 0,

        /// <summary>Maximum number of views for XR.</summary>
#if ENABLE_VR
        XrMaxViews = 2,
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
        // Also uncomment in the HDRP package all ".../Experimental/Probe Volume" menu

        /// <summary>Probe volume evaluation mode.</summary>
        ProbeVolumesEvaluationMode = ProbeVolumesEvaluationModes.Disabled,
        /// <summary>Probe volume supports additive blending.</summary>
        ProbeVolumesAdditiveBlending = 1,
        /// <summary>Probe volume filtering mode.</summary>
        ProbeVolumesBilateralFilteringMode = ProbeVolumesBilateralFilteringModes.Validity,
        /// <summary>Probe volume encoding.</summary>
        ProbeVolumesEncodingMode = ProbeVolumesEncodingModes.SphericalHarmonicsL1,

        /// <summary>Support for area lights.</summary>
        AreaLights = 1,

        /// <summary>Support for barn doors.</summary>
        BarnDoor = 0
    };

    // Note: #define can't be use in include file in C# so we chose this way to configure both C# and hlsl
    // Changing a value in this enum Config here require to regenerate the hlsl include and recompile C# and shaders
    /// <summary>
    /// Project wide shader configuration options.
    /// This class reflects the ShaderOptions enum. It's meant to be used in C# code to check current configuration.
    /// </summary>
    public class ShaderConfig
    {
        // REALLY IMPORTANT! This needs to be the maximum possible XrMaxViews for any supported platform!
        // this needs to be constant and not vary like XrMaxViews does as it is used to generate the cbuffer declarations
        /// <summary>Maximum number of XR views for constant buffer allocation.</summary>
        public const int k_XRMaxViewsForCBuffer = 2;

        /// <summary>Use camera relative rendering to enhance precision.</summary>
        public static int s_CameraRelativeRendering = (int)ShaderOptions.CameraRelativeRendering;
        /// <summary>Use pre-exposition to enhance color precision.</summary>
        public static int s_PreExposition = (int)ShaderOptions.PreExposition;
        /// <summary>Maximum number of views for XR.</summary>
        public static int s_XrMaxViews = (int)ShaderOptions.XrMaxViews;
        /// <summary>Precomputes atmospheric attenuation for the directional light on the CPU, which makes it independent from the fragment's position, which is faster but wrong.</summary>
        public static int s_PrecomputedAtmosphericAttenuation = (int)ShaderOptions.PrecomputedAtmosphericAttenuation;
        /// <summary>Probe volume evaluation mode.</summary>
        public static ProbeVolumesEvaluationModes s_ProbeVolumesEvaluationMode = (ProbeVolumesEvaluationModes)ShaderOptions.ProbeVolumesEvaluationMode;
        /// <summary>Probe volume supports additive blending.</summary>
        public static int s_ProbeVolumesAdditiveBlending = (int)ShaderOptions.ProbeVolumesAdditiveBlending;
        /// <summary>Probe volume filtering mode.</summary>
        public static ProbeVolumesBilateralFilteringModes s_ProbeVolumesBilateralFilteringMode = (ProbeVolumesBilateralFilteringModes)ShaderOptions.ProbeVolumesBilateralFilteringMode;
        /// <summary>Probe volume encoding.</summary>
        public static ProbeVolumesEncodingModes s_ProbeVolumesEncodingMode = (ProbeVolumesEncodingModes)ShaderOptions.ProbeVolumesEncodingMode;
        /// <summary>Support for area lights.</summary>
        public static int s_AreaLights = (int)ShaderOptions.AreaLights;
        /// <summary>Support for barn doors.</summary>
        public static int s_BarnDoor = (int)ShaderOptions.BarnDoor;
    }
}
