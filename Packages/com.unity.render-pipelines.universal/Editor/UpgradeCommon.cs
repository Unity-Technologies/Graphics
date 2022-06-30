using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// The surface type for your material.
    /// </summary>
    public enum UpgradeSurfaceType
    {
        /// <summary>
        /// Use this for opaque surfaces.
        /// </summary>
        Opaque,

        /// <summary>
        /// Use this for transparent surfaces.
        /// </summary>
        Transparent
    }

    /// <summary>
    /// The blend mode for your material.
    /// </summary>
    public enum UpgradeBlendMode
    {
        /// <summary>
        /// Use this for alpha blend mode.
        /// </summary>
        Alpha,

        /// <summary>
        /// Use this for premultiply blend mode.
        /// </summary>
        Premultiply,

        /// <summary>
        /// Use this for additive blend mode.
        /// </summary>
        Additive,

        /// <summary>
        /// Use this for multiply blend mode.
        /// </summary>
        Multiply
    }

    /// <summary>
    /// Options for Specular source.
    /// </summary>
    public enum SpecularSource
    {
        /// <summary>
        /// Use this to use specular texture and color.
        /// </summary>
        SpecularTextureAndColor,

        /// <summary>
        /// Use this when not using specular.
        /// </summary>
        NoSpecular
    }

    /// <summary>
    /// Options to select the texture channel where the smoothness value is stored.
    /// </summary>
    public enum SmoothnessSource
    {
        /// <summary>
        /// Use this when smoothness is stored in the alpha channel of the Specular Map.
        /// </summary>
        SpecularAlpha,

        /// <summary>
        /// Use this when smoothness is stored in the alpha channel of the base Map.
        /// </summary>
        BaseAlpha,
    }

    /// <summary>
    /// Options to select the source for reflections.
    /// </summary>
    public enum ReflectionSource
    {
        /// <summary>
        /// Use this when there are no reflections.
        /// </summary>
        NoReflection,

        /// <summary>
        /// Use this when the source comes from a cubemap.
        /// </summary>
        Cubemap,

        /// <summary>
        /// Use this when the source comes from a reflection probe.
        /// </summary>
        ReflectionProbe
    }

    /// <summary>
    /// Container for the upgrade parameters.
    /// </summary>
    public struct UpgradeParams
    {
        /// <summary>
        /// Surface type upgrade parameters.
        /// </summary>
        public UpgradeSurfaceType surfaceType { get; set; }

        /// <summary>
        /// Blend mode upgrade parameters.
        /// </summary>
        public UpgradeBlendMode blendMode { get; set; }

        /// <summary>
        /// Alpha clip upgrade parameters.
        /// </summary>
        public bool alphaClip { get; set; }

        /// <summary>
        /// Specular source upgrade parameters.
        /// </summary>
        public SpecularSource specularSource { get; set; }

        /// <summary>
        /// Smoothness source upgrade parameters.
        /// </summary>
        public SmoothnessSource smoothnessSource { get; set; }
    }
}
