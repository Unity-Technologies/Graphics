using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Color Curves effect.
    /// </summary>
    /// <remarks>
    /// You can add <see cref="VolumeComponent"/> to a <see cref="VolumeProfile"/> in the Editor to apply a Color Curves post-processing effect.
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
    ///     private ColorCurves m_VolumeComponent;
    ///
    ///     [Serializable]
    ///     private struct VolumeSettings
    ///     {
    ///         public bool active;
    ///         public TextureCurveParameter master;
    ///         public TextureCurveParameter red;
    ///         public TextureCurveParameter green;
    ///         public TextureCurveParameter blue;
    ///         public TextureCurveParameter hueVsHue;
    ///         public TextureCurveParameter hueVsSat;
    ///         public TextureCurveParameter satVsSat;
    ///         public TextureCurveParameter lumVsSat;
    ///
    ///         public void SetVolumeComponentSettings(ref ColorCurves volumeComponent)
    ///         {
    ///             volumeComponent.active = active;
    ///             volumeComponent.master = master;
    ///             volumeComponent.red = red;
    ///             volumeComponent.green = green;
    ///             volumeComponent.blue = blue;
    ///             volumeComponent.hueVsHue = hueVsHue;
    ///             volumeComponent.hueVsSat = hueVsSat;
    ///             volumeComponent.satVsSat = satVsSat;
    ///             volumeComponent.lumVsSat = lumVsSat;
    ///         }
    ///
    ///         public void GetVolumeComponentSettings(ref ColorCurves volumeComponent)
    ///         {
    ///             active = volumeComponent.active;
    ///             master = volumeComponent.master;
    ///             red = volumeComponent.red;
    ///             green = volumeComponent.green;
    ///             blue = volumeComponent.blue;
    ///             hueVsHue = volumeComponent.hueVsHue;
    ///             hueVsSat = volumeComponent.hueVsSat;
    ///             satVsSat = volumeComponent.satVsSat;
    ///             lumVsSat = volumeComponent.lumVsSat;
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
    ///     private static bool GetVolumeComponent(in VolumeProfile volumeProfile, ref ColorCurves volumeComponent)
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
    ///         volumeProfile.TryGet(out ColorCurves component);
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
    /// <seealso cref="TextureCurveParameter"/>
    [Serializable, VolumeComponentMenu("Post-processing/Color Curves")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [URPHelpURL("Post-Processing-Color-Curves")]
    public sealed class ColorCurves : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Affects the luminance across the whole image.
        /// </summary>
        [Tooltip("Affects the luminance across the whole image.")]
        public TextureCurveParameter master = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));

        /// <summary>
        /// Affects the red channel intensity across the whole image.
        /// </summary>
        [Tooltip("Affects the red channel intensity across the whole image.")]
        public TextureCurveParameter red = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));

        /// <summary>
        /// Affects the green channel intensity across the whole image.
        /// </summary>
        [Tooltip("Affects the green channel intensity across the whole image.")]
        public TextureCurveParameter green = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));

        /// <summary>
        /// Affects the blue channel intensity across the whole image.
        /// </summary>
        [Tooltip("Affects the blue channel intensity across the whole image.")]
        public TextureCurveParameter blue = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));

        /// <summary>
        /// Shifts the input hue (x-axis) according to the output hue (y-axis).
        /// </summary>
        [Tooltip("Shifts the input hue (x-axis) according to the output hue (y-axis).")]
        public TextureCurveParameter hueVsHue = new TextureCurveParameter(new TextureCurve(new Keyframe[] { }, 0.5f, true, new Vector2(0f, 1f)));

        /// <summary>
        /// Adjusts saturation (y-axis) according to the input hue (x-axis).
        /// </summary>
        [Tooltip("Adjusts saturation (y-axis) according to the input hue (x-axis).")]
        public TextureCurveParameter hueVsSat = new TextureCurveParameter(new TextureCurve(new Keyframe[] { }, 0.5f, true, new Vector2(0f, 1f)));

        /// <summary>
        /// Adjusts saturation (y-axis) according to the input saturation (x-axis).
        /// </summary>
        [Tooltip("Adjusts saturation (y-axis) according to the input saturation (x-axis).")]
        public TextureCurveParameter satVsSat = new TextureCurveParameter(new TextureCurve(new Keyframe[] { }, 0.5f, false, new Vector2(0f, 1f)));

        /// <summary>
        /// Adjusts saturation (y-axis) according to the input luminance (x-axis).
        /// </summary>
        [Tooltip("Adjusts saturation (y-axis) according to the input luminance (x-axis).")]
        public TextureCurveParameter lumVsSat = new TextureCurveParameter(new TextureCurve(new Keyframe[] { }, 0.5f, false, new Vector2(0f, 1f)));

        /// <summary>
        /// Tells if the post process needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive() => true;

        /// <summary>
        /// Tells if the post process can run the effect on-tile or if it needs a full pass.
        /// </summary>
        /// <returns><c>true</c> if it can run on-tile, <c>false</c> otherwise.</returns>
        [Obsolete("Unused #from(2023.1)", false)]
        public bool IsTileCompatible() => true;
    }
}
