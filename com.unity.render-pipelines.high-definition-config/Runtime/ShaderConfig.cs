//-----------------------------------------------------------------------------
// Configuration
//-----------------------------------------------------------------------------

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Options for the mode HDRP uses to evaluate probe volumes.
    /// </summary>
    ///<seealso cref="ShaderOptions"/>
    [GenerateHLSL(PackingRules.Exact)]
    public enum ProbeVolumesEvaluationModes
    {
        /// <summary>Disables probe volumes.</summary>
        Disabled = 0,
        /// <summary>Evaluates probe volumes in the light loop.</summary>
        LightLoop = 1,
        /// <summary>Evaluates probe volumes in the material pass.</summary>
        MaterialPass = 2,
    }

    /// <summary>
    /// Options for the method HDRP uses to encode probe volumes.
    /// </summary>
    ///<seealso cref="ShaderOptions"/>
    [GenerateHLSL(PackingRules.Exact)]
    public enum ProbeVolumesEncodingModes
    {
        /// <summary>Uses L0 spherical harmonics to encode probe volumes.</summary>
        SphericalHarmonicsL0 = 0,
        /// <summary>Uses L1 spherical harmonics to encode probe volumes.</summary>
        SphericalHarmonicsL1 = 1,
        /// <summary>Uses L2 spherical harmonics to encode probe volumes.</summary>
        SphericalHarmonicsL2 = 2
    }

    /// <summary>
    /// Options for the mode HDRP uses for probe volume bilateral filtering.
    /// </summary>
    ///<seealso cref="ShaderOptions"/>
    [GenerateHLSL(PackingRules.Exact)]
    public enum ProbeVolumesBilateralFilteringModes
    {
        /// <summary>Disables bilateral filtering.</summary>
        Disabled = 0,
        /// <summary>Bilateral filtering using validity.</summary>
        Validity = 1,
        /// <summary>Bilateral filtering using octahedral depth.</summary>
        OctahedralDepth = 2
    }

    /// <summary>
    /// Project-wide shader configuration options.
    /// </summary>
    /// <remarks>This enum will generate the proper shader defines.</remarks>
    ///<seealso cref="ShaderConfig"/>
    [GenerateHLSL(PackingRules.Exact)]
    public enum ShaderOptions
    {
        /// <summary>Supports colored shadows in shaders.</summary>
        ColoredShadow = 1,
        /// <summary>Uses [camera-relative rendering](../manual/Camera-Relative-Rendering.md) to enhance precision.</summary>
        CameraRelativeRendering = 1,
        /// <summary>Uses pre-exposition to enhance color precision.</summary>
        PreExposition = 1,
        /// <summary>Precomputes atmospheric attenuation for the directional light on the CPU. This makes it independent from the fragment's position, which increases performance but reduces accuracy.</summary>
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

        /// <summary>The probe volume evaluation mode.</summary>
        /// <seealso cref = "ProbeVolumesEvaluationModes " />
        ProbeVolumesEvaluationMode = ProbeVolumesEvaluationModes.Disabled,
        /// <summary>Probe volume supports additive blending.</summary>
        ProbeVolumesAdditiveBlending = 1,
        /// <summary>The probe volume filtering mode.</summary>
        /// <seealso cref="ProbeVolumesBilateralFilteringModes"/>
        ProbeVolumesBilateralFilteringMode = ProbeVolumesBilateralFilteringModes.Validity,
        /// <summary>The probe volume encoding method.</summary>
        /// /// <seealso cref="ProbeVolumesEncodingModes"/>
        ProbeVolumesEncodingMode = ProbeVolumesEncodingModes.SphericalHarmonicsL1,

        /// <summary>Support for area lights.</summary>
        AreaLights = 1,

        /// <summary>Support for barn doors.</summary>
        BarnDoor = 0
    };

    // Note: #define can't be use in include file in C# so we chose this way to configure both C# and hlsl
    // Changing a value in this enum Config here require to regenerate the hlsl include and recompile C# and shaders
    /// <summary>
    /// Project-wide shader configuration options.
    /// <remarks>This class reflects the enum. Use it in C# code to check the current configuration.</remarks>
    /// </summary>
    public class ShaderConfig
    {
        // REALLY IMPORTANT! This needs to be the maximum possible XrMaxViews for any supported platform!
        // this needs to be constant and not vary like XrMaxViews does as it is used to generate the cbuffer declarations
        /// <summary>Maximum number of XR views for constant buffer allocation.</summary>
        public const int k_XRMaxViewsForCBuffer = 2;

        /// <summary>Indicates whether to use [camera-relative rendering](../manual/Camera-Relative-Rendering.md) to enhance precision.</summary>
        ///<seealso cref="ShaderOptions.CameraRelativeRendering"/>
        public static int s_CameraRelativeRendering = (int)ShaderOptions.CameraRelativeRendering;
        /// <summary>Indicates whether to use pre-exposition to enhance color prevision.</summary>
        ///<seealso cref="ShaderOptions.PreExposition"/>
        public static int s_PreExposition = (int)ShaderOptions.PreExposition;
        /// <summary>Specifies the maximum number of views to use for XR rendering.</summary>
        ///<seealso cref="ShaderOptions.XrMaxViews"/>
        public static int s_XrMaxViews = (int)ShaderOptions.XrMaxViews;
        /// <summary>Indicates whether to precompute atmosphere attenuation for the directional light on the CPU.</summary>
        ///<seealso cref="ShaderOptions.PrecomputedAtmosphericAttenuation"/>
        public static int s_PrecomputedAtmosphericAttenuation = (int)ShaderOptions.PrecomputedAtmosphericAttenuation;
        /// <summary>Specifies the probe volume evaluation mode.</summary>
        ///<seealso cref="ShaderOptions.ProbeVolumesEvaluationMode"/>
        public static ProbeVolumesEvaluationModes s_ProbeVolumesEvaluationMode = (ProbeVolumesEvaluationModes)ShaderOptions.ProbeVolumesEvaluationMode;
        /// <summary>Indicates whether probe volumes support additive blending.</summary>
        ///<seealso cref="ShaderOptions.ProbeVolumesAdditiveBlending"/>
        public static int s_ProbeVolumesAdditiveBlending = (int)ShaderOptions.ProbeVolumesAdditiveBlending;
        /// <summary>Specifies the probe volume filtering mode.</summary>
        ///<seealso cref="ShaderOptions.ProbeVolumesBilateralFilteringMode"/>
        public static ProbeVolumesBilateralFilteringModes s_ProbeVolumesBilateralFilteringMode = (ProbeVolumesBilateralFilteringModes)ShaderOptions.ProbeVolumesBilateralFilteringMode;
        /// <summary>Specifies the probe volume encoding method.</summary>
        ///<seealso cref="ShaderOptions.ProbeVolumesEncodingMode"/>
        public static ProbeVolumesEncodingModes s_ProbeVolumesEncodingMode = (ProbeVolumesEncodingModes)ShaderOptions.ProbeVolumesEncodingMode;
        /// <summary>Indicates whether to support area lights.</summary>
        ///<seealso cref="ShaderOptions.AreaLights"/>
        public static int s_AreaLights = (int)ShaderOptions.AreaLights;
        /// <summary>Indicates whether to support barn doors.</summary>
        ///<seealso cref="ShaderOptions.BarnDoor"/>
        public static int s_BarnDoor = (int)ShaderOptions.BarnDoor;
    }
}
