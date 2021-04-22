using System;

namespace UnityEngine.Rendering.Universal
{
    #region Material Settings

    /// <summary>
    /// Debug material modes.
    /// </summary>
    [GenerateHLSL]
    public enum DebugMaterialMode
    {
        /// <summary>No material debug.</summary>
        None,
        /// <summary>Display material albedo.</summary>
        Albedo,
        /// <summary>Display material specular.</summary>
        Specular,
        /// <summary>Display material alpha.</summary>
        Alpha,
        /// <summary>Display material smoothness.</summary>
        Smoothness,
        /// <summary>Display material ambient occlusion.</summary>
        AmbientOcclusion,
        /// <summary>Display material emission.</summary>
        Emission,
        /// <summary>Display material normal (world space).</summary>
        NormalWorldSpace,
        /// <summary>Display material normal (tangent space).</summary>
        NormalTangentSpace,
        /// <summary>Display evaluated lighting complexity.</summary>
        LightingComplexity,
        /// <summary>Display material metallic.</summary>
        Metallic,
        /// <summary>Display material sprite mask.</summary>
        SpriteMask,
    }

    /// <summary>
    /// Debug mode for displaying vertex attributes interpolated from vertex to pixel shader.
    /// </summary>
    [GenerateHLSL]
    public enum DebugVertexAttributeMode
    {
        /// <summary>No vertex attribute debug.</summary>
        None,
        /// <summary>Display texture coordinate 0.</summary>
        Texcoord0,
        /// <summary>Display texture coordinate 1.</summary>
        Texcoord1,
        /// <summary>Display texture coordinate 2.</summary>
        Texcoord2,
        /// <summary>Display texture coordinate 3.</summary>
        Texcoord3,
        /// <summary>Display vertex color.</summary>
        Color,
        /// <summary>Display tangent.</summary>
        Tangent,
        /// <summary>Display normal.</summary>
        Normal,
    }

    /// <summary>
    /// Debug mode for validating out-of-range values of different material channels.
    /// </summary>
    [GenerateHLSL]
    public enum DebugMaterialValidationMode
    {
        /// <summary>No material debug validation override.</summary>
        None,
        /// <summary>Validate albedo values according to validation settings.</summary>
        Albedo,
        /// <summary>Validate metallic values according to validation settings.</summary>
        Metallic
    }

    #endregion

    #region Rendering Settings

    /// <summary>
    /// Debug mode for displaying intermediate render targets.
    /// </summary>
    [GenerateHLSL]
    public enum DebugFullScreenMode
    {
        /// <summary>No intermediate render target displayed.</summary>
        None,
        /// <summary>Display depth buffer contents.</summary>
        Depth,

        /// <summary>Display the shadow map from additional lights.</summary>
        AdditionalLightsShadowMap,
        /// <summary>Display the main shadow map.</summary>
        MainLightShadowMap,
    }

    /// <summary>
    /// Debug mode that overrides how the renderer behaves.
    /// </summary>
    [GenerateHLSL]
    public enum DebugSceneOverrideMode
    {
        /// <summary>No debug override.</summary>
        None,
        /// <summary>Visualize overdraw by drawing geometry using a semitransparent material. Areas that look opaque contain more overdraw.</summary>
        Overdraw,
        /// <summary>Render using wireframe only.</summary>
        Wireframe,
        /// <summary>Render using a constant fill color and wireframe.</summary>
        SolidWireframe,
        /// <summary>Render shaded geometry in addition to wireframe.</summary>
        ShadedWireframe,
    }

    /// <summary>
    /// Debug modes for texture mipmaps.
    /// </summary>
    [GenerateHLSL]
    public enum DebugMipInfoMode
    {
        /// <summary>No mipmap debug.</summary>
        None,
        /// <summary>Display the mipmap level sampled.</summary>
        Level,
        /// <summary>Display the amount of mip levels available.</summary>
        Count,
        /// <summary>Display the mip ratio.</summary>
        Ratio
    }

    /// <summary>
    /// Mode that controls if post-processing is allowed.
    /// </summary>
    /// <remarks>
    /// When "Auto" is used, post-processing can be either on or off, depending on other active debug modes.
    /// </remarks>
    [GenerateHLSL]
    public enum DebugPostProcessingMode
    {
        /// <summary>Post-processing disabled.</summary>
        Disabled,
        /// <summary>Post-processing is either on or off, depending on other debug modes.</summary>
        Auto,
        /// <summary>Post-processing enabled.</summary>
        Enabled
    };

    /// <summary>
    /// Debug modes for validating illegal output values.
    /// </summary>
    [GenerateHLSL]
    public enum DebugValidationMode
    {
        /// <summary>No validation.</summary>
        None,

        /// <summary>Highlight all pixels containing NaN (not a number), infinite or negative values.</summary>
        [InspectorName("Highlight NaN, Inf and Negative Values")]
        HighlightNanInfNegative,

        /// <summary>Highlight all pixels with values outside the specified range.</summary>
        [InspectorName("Highlight Values Outside Range")]
        HighlightOutsideOfRange
    }

    /// <summary>
    /// The channels used by DebugValidationMode.HighlightOutsideOfRange.
    /// </summary>
    /// <remarks>
    /// When "RGB" is used, the pixel's RGB value is first converted to a luminance value.
    /// Individual channels (R, G, B, and A) are tested individually against the range.
    /// </remarks>
    [GenerateHLSL]
    public enum PixelValidationChannels
    {
        /// <summary>Use luminance calculated from RGB channels as the value to validate.</summary>
        RGB,
        /// <summary>Validate the red channel value.</summary>
        R,
        /// <summary>Validate the green channel value.</summary>
        G,
        /// <summary>Validate the blue channel value.</summary>
        B,
        /// <summary>Validate the alpha channel value.</summary>
        A
    }
    #endregion

    #region Lighting settings

    /// <summary>
    /// Debug modes for lighting.
    /// </summary>
    [GenerateHLSL]
    public enum DebugLightingMode
    {
        /// <summary>No lighting debug mode.</summary>
        None,
        /// <summary>Display shadow cascades using different colors.</summary>
        ShadowCascades,
        /// <summary>Display lighting result without applying normal maps.</summary>
        LightingWithoutNormalMaps,
        /// <summary>Display lighting result (including normal maps).</summary>
        LightingWithNormalMaps,
        /// <summary>Display only reflections.</summary>
        Reflections,
        /// <summary>Display only reflections with smoothness.</summary>
        ReflectionsWithSmoothness,
    }

    /// <summary>
    /// Debug mode that allows selective disabling of individual lighting components.
    /// </summary>
    [GenerateHLSL, Flags]
    public enum DebugLightingFeatureFlags
    {
        /// <summary>The debug mode is not active.</summary>
        None,
        /// <summary>Display contribution from global illumination.</summary>
        GlobalIllumination = 0x1,
        /// <summary>Display contribution from the main light.</summary>
        MainLight = 0x2,
        /// <summary>Display contribution from additional lights.</summary>
        AdditionalLights = 0x4,
        /// <summary>Display contribution from vertex lighting.</summary>
        VertexLighting = 0x8,
        /// <summary>Display contribution from emissive objects.</summary>
        Emission = 0x10,
        /// <summary>Display contribution from ambient occlusion.</summary>
        AmbientOcclusion = 0x20,
    }
    #endregion
}
