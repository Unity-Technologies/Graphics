using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Lens Flare effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Lens Flare")]
    [HDRPHelpURLAttribute("Post-Processing-Lens-Flare")]
    public sealed class LensFlare : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Set the level of brightness to filter out pixels under this level. This value is expressed in gamma-space. A value above 0 will disregard energy conservation rules.
        /// </summary>
        [Header("Lens Flare")]
        [Tooltip("Set the level of brightness to filter out pixels under this level. This value is expressed in gamma-space. A value above 0 will disregard energy conservation rules.")]
        public ClampedFloatParameter threshold = new ClampedFloatParameter(0.25f, 0f, 1f);

        /// <summary>
        /// Controls the strength of the lens flare filter.
        /// </summary>
        [Tooltip("Controls the strength of the lens flare filter.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0.5f, 0f, 1f);

        /// <summary>
        /// Controls the extent of the veiling effect.
        /// </summary>
        [Tooltip("Controls the extent of the veiling effect.")]
        public ClampedFloatParameter scatter = new ClampedFloatParameter(0.7f, 0f, 1f);

        /// <summary>
        /// Specifies the tint of the lens flare filter.
        /// </summary>
        [Tooltip("Specifies the tint of the lens flare filter.")]
        public ColorParameter tint = new ColorParameter(Color.white, false, false, true);

        /// <summary>
        /// When enabled, lens flare stretches horizontally depending on the current physical Camera's Anamorphism property value.
        /// </summary>
        [Tooltip("When enabled, lens flare stretches horizontally depending on the current physical Camera's Anamorphism property value.")]
        [AdditionalProperty]
        public BoolParameter anamorphic = new BoolParameter(true);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return threshold.value > 0.0f && intensity.value > 0.0f;
        }
    }
}
