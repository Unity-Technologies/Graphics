//-----------------------------------------------------------------------------
// Configuration
//-----------------------------------------------------------------------------
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    //Do not change these numbers!!
    //Its not a full power of 2 because the last light slot is reserved.
    internal enum FPTLMaxLightSizes
    {
        Low = 31,
        High = 63,
        Higher = 127,
        Ultra = 255
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

        /// <summary>Support for area lights.</summary>
        AreaLights = 1,

        /// <summary>Support for barn doors.</summary>
        BarnDoor = 0,

        /// <summary>Support to apply a global mip bias on all texture samplers of HDRP.</summary>
        GlobalMipBias = 1,

        /// <summary>
        /// Maximum number of lights for a fine pruned light tile. This number can only be the prespecified possibilities in FPTLMaxLightSizes
        /// Lower count will mean some memory savings.
        /// Note: For any rendering bigger than 4k (in native) it is recommended to use Low count per tile, to avoid possible artifacts.
        /// </summary>
        FPTLMaxLightCount = FPTLMaxLightSizes.High
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

        /// <summary>Indicates whether to support area lights.</summary>
        ///<seealso cref="ShaderOptions.AreaLights"/>
        public static int s_AreaLights = (int)ShaderOptions.AreaLights;
        /// <summary>Indicates whether to support barn doors.</summary>
        ///<seealso cref="ShaderOptions.BarnDoor"/>
        public static int s_BarnDoor = (int)ShaderOptions.BarnDoor;
        /// <summary>Indicates whether to support application of global mip bias on all texture samplers of hdrp.</summary>
        ///<seealso cref="ShaderOptions.GlobalMipBias"/>
        public static bool s_GlobalMipBias = (int)ShaderOptions.GlobalMipBias != 0;
        /// <summary>Indicates the maximum number of lights available for Fine Prunning Tile Lighting.</summary>
        /// <seealso cref="ShaderOptions.FPTLMaxLightCount"/>
        public static int FPTLMaxLightCount = (int)ShaderOptions.FPTLMaxLightCount;
    }

    /// <summary>
    /// Internal definitions for FPTL and light culling algorithms. Do not modify, these variables are
    /// generated from the FPTLMaxLightCount setting automatically.
    /// </summary>
    [GenerateHLSL]
    public class InternalLightCullingDefs
    {
        /// <summary>Maximum number of lights for a big tile. Do not modify value, since its generated from ShaderConfig.FPTLMaxLightCount.</summary>
        public static int s_MaxNrBigTileLightsPlusOne = Math.Clamp((ShaderConfig.FPTLMaxLightCount + 1) * 8, 512, 1024);      // if light count is 64, may be overkill but the footprint is 2 bits per pixel using uint16.

        // light list limits
        /// <summary>Maximum number of lights for coarse entries (before fine prunning). Do not modify value, since its generated from ShaderConfig.FPTLMaxLightCount.</summary>
        public static int s_LightListMaxCoarseEntries = Math.Clamp(ShaderConfig.FPTLMaxLightCount + 1, 64, 256);
        /// <summary>Maximum number of lights for cluster coarse entries. Do not modify value, since its generated from ShaderConfig.FPTLMaxLightCount.</summary>
        public static int s_LightClusterMaxCoarseEntries = Math.Clamp((ShaderConfig.FPTLMaxLightCount + 1) * 2, 128, 256); // we expect coarse cluster max count to grow with FPTL light count

        // We have room for ShaderConfig.FPTLMaxLightCount lights, plus 1 implicit value for length.
        // We allocate only 16 bits per light index & length, thus we divide by 2, and store in a word buffer.
        /// <summary>Maximum number of dwords per tile. Each light occupies 16 bits. Do not modify value, since its generated from ShaderConfig.FPTLMaxLightCount.</summary>
        public static int s_LightDwordPerFptlTile = ((ShaderConfig.FPTLMaxLightCount + 1)) / 2;
        /// <summary>Number of bits required to pack cluster count. Do not modify value, since its generated from ShaderConfig.FPTLMaxLightCount.</summary>
        public static int s_LightClusterPackingCountBits = (int)Mathf.Ceil(Mathf.Log(Mathf.NextPowerOfTwo(ShaderConfig.FPTLMaxLightCount), 2));
        /// <summary>Bit mask for packing cluster count. Do not modify value, since its generated from ShaderConfig.FPTLMaxLightCount.</summary>
        public static int s_LightClusterPackingCountMask = (1 << s_LightClusterPackingCountBits) - 1;
        /// <summary>Number of bits required to pack a cluster offset. Do not modify value, since its generated from ShaderConfig.FPTLMaxLightCount.</summary>
        public static int s_LightClusterPackingOffsetBits = 32 - s_LightClusterPackingCountBits;
        /// <summary>Bit mask for packing cluster light offset. Do not modify value, since its generated from ShaderConfig.FPTLMaxLightCount.</summary>
        public static int s_LightClusterPackingOffsetMask = (1 << s_LightClusterPackingOffsetBits) - 1;
    }
}
