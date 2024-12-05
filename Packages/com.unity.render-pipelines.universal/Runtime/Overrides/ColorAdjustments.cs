using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Color Adjustments effect.
    /// </summary>
    /// <remarks>
    /// You can add <see cref="VolumeComponent"/> to a <see cref="VolumeProfile"/> in the Editor to apply a Color Adjustments post-processing effect.
    /// </remarks>
    /// <example>
    /// <para>This sample code shows how settings can be retrieved and modified in runtime:</para>
    /// <code>
    /// using System;
    /// using UnityEngine;
    /// using UnityEngine.Rendering;
    /// using UnityEngine.Rendering.Universal;
    ///
    /// public class ModifyVolumeComponent : MonoBehaviour
    /// {
    ///     [SerializeField] VolumeProfile volumeProfile;
    ///     [SerializeField] VolumeSettings volumeSettings;
    ///
    ///     private bool m_HasRetrievedVolumeComponent;
    ///     private ColorAdjustments m_VolumeComponent;
    ///
    ///     [Serializable]
    ///     private struct VolumeSettings
    ///     {
    ///         public bool active;
    ///         public FloatParameter postExposure;
    ///         public ClampedFloatParameter contrast;
    ///         public ColorParameter colorFilter;
    ///         public ClampedFloatParameter hueShift;
    ///         public ClampedFloatParameter saturation;
    ///
    ///         public void SetVolumeComponentSettings(ref ColorAdjustments volumeComponent)
    ///         {
    ///             volumeComponent.active = active;
    ///             volumeComponent.postExposure = postExposure;
    ///             volumeComponent.contrast = contrast;
    ///             volumeComponent.colorFilter = colorFilter;
    ///             volumeComponent.hueShift = hueShift;
    ///             volumeComponent.saturation = saturation;
    ///         }
    ///
    ///         public void GetVolumeComponentSettings(ref ColorAdjustments volumeComponent)
    ///         {
    ///             active = volumeComponent.active;
    ///             postExposure = volumeComponent.postExposure;
    ///             contrast = volumeComponent.contrast;
    ///             colorFilter = volumeComponent.colorFilter;
    ///             hueShift = volumeComponent.hueShift;
    ///             saturation = volumeComponent.saturation;
    ///         }
    ///     }
    ///
    ///     private void Start()
    ///     {
    ///         m_HasRetrievedVolumeComponent = GetVolumeComponent(in volumeProfile, ref m_VolumeComponent);
    ///         if (m_HasRetrievedVolumeComponent)
    ///             volumeSettings.GetVolumeComponentSettings(ref m_VolumeComponent);
    ///     }
    ///
    ///     private void Update()
    ///     {
    ///         if (!m_HasRetrievedVolumeComponent)
    ///             return;
    ///
    ///         volumeSettings.SetVolumeComponentSettings(ref m_VolumeComponent);
    ///     }
    ///
    ///     private static bool GetVolumeComponent(in VolumeProfile volumeProfile, ref ColorAdjustments volumeComponent)
    ///     {
    ///         if (volumeComponent != null)
    ///             return true;
    ///
    ///         if (volumeProfile == null)
    ///         {
    ///             Debug.LogError("ModifyVolumeComponent.GetVolumeComponent():\nvolumeProfile has not been assigned.");
    ///             return false;
    ///         }
    ///
    ///         volumeProfile.TryGet(out ColorAdjustments component);
    ///         if (component == null)
    ///         {
    ///             Debug.LogError($"ModifyVolumeComponent.GetVolumeComponent():\nMissing component in the \"{volumeProfile.name}\" VolumeProfile ");
    ///             return false;
    ///         }
    ///
    ///         volumeComponent = component;
    ///         return true;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="VolumeProfile"/>
    /// <seealso cref="VolumeComponent"/>
    /// <seealso cref="IPostProcessComponent"/>
    /// <seealso cref="VolumeParameter{T}"/>
    /// <seealso cref="FloatParameter"/>
    /// <seealso cref="ClampedFloatParameter"/>
    /// <seealso cref="ColorParameter"/>
    [Serializable, VolumeComponentMenu("Post-processing/Color Adjustments")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [URPHelpURL("Post-Processing-Color-Adjustments")]
    public sealed class ColorAdjustments : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Adjusts the overall exposure of the scene in EV100.
        /// This is applied after HDR effect and right before tonemapping so it won't affect previous effects in the chain.
        /// </summary>
        [Tooltip("Adjusts the overall exposure of the scene in EV100. This is applied after HDR effect and right before tonemapping so it won't affect previous effects in the chain.")]
        public FloatParameter postExposure = new FloatParameter(0f);

        /// <summary>
        /// Controls the overall range of the tonal values.
        /// </summary>
        [Tooltip("Expands or shrinks the overall range of tonal values.")]
        public ClampedFloatParameter contrast = new ClampedFloatParameter(0f, -100f, 100f);

        /// <summary>
        /// Specifies the color that URP tints the render to.
        /// </summary>
        [Tooltip("Tint the render by multiplying a color.")]
        public ColorParameter colorFilter = new ColorParameter(Color.white, true, false, true);

        /// <summary>
        /// Controls the hue of all colors in the render.
        /// </summary>
        [Tooltip("Shift the hue of all colors.")]
        public ClampedFloatParameter hueShift = new ClampedFloatParameter(0f, -180f, 180f);

        /// <summary>
        /// Controls the intensity of all colors in the render.
        /// </summary>
        [Tooltip("Pushes the intensity of all colors.")]
        public ClampedFloatParameter saturation = new ClampedFloatParameter(0f, -100f, 100f);

        /// <summary>
        /// Tells if the post process needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return postExposure.value != 0f
                || contrast.value != 0f
                || colorFilter != Color.white
                || hueShift != 0f
                || saturation != 0f;
        }

        /// <summary>
        /// Tells if the post process can run the effect on-tile or if it needs a full pass.
        /// </summary>
        /// <returns><c>true</c> if it can run on-tile, <c>false</c> otherwise.</returns>
        [Obsolete("Unused #from(2023.1)", false)]
        public bool IsTileCompatible() => true;
    }
}
