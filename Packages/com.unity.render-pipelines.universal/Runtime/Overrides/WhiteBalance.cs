using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the White Balance effect.
    /// </summary>
    /// <remarks>
    /// You can add <see cref="VolumeComponent"/> to a <see cref="VolumeProfile"/> in the Editor to apply a White Balance post-processing effect.
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
    ///     private WhiteBalance m_VolumeComponent;
    ///
    ///     [Serializable]
    ///     private struct VolumeSettings
    ///     {
    ///         public bool active;
    ///         public ClampedFloatParameter temperature;
    ///         public ClampedFloatParameter tint;
    ///
    ///         public void SetVolumeComponentSettings(ref WhiteBalance volumeComponent)
    ///         {
    ///             volumeComponent.active = active;
    ///             volumeComponent.temperature = temperature;
    ///             volumeComponent.tint = tint;
    ///         }
    ///
    ///         public void GetVolumeComponentSettings(ref WhiteBalance volumeComponent)
    ///         {
    ///             active = volumeComponent.active;
    ///             temperature = volumeComponent.temperature;
    ///             tint = volumeComponent.tint;
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
    ///     private static bool GetVolumeComponent(in VolumeProfile volumeProfile, ref WhiteBalance volumeComponent)
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
    ///         volumeProfile.TryGet(out WhiteBalance component);
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
    /// <seealso cref="ClampedFloatParameter"/>
    [Serializable, VolumeComponentMenu("Post-processing/White Balance")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [URPHelpURL("Post-Processing-White-Balance")]
    public sealed class WhiteBalance : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Controls the color temperature URP uses for white balancing.
        /// </summary>
        [Tooltip("Sets the white balance to a custom color temperature.")]
        public ClampedFloatParameter temperature = new ClampedFloatParameter(0f, -100, 100f);

        /// <summary>
        /// Controls the white balance color to compensate for a green or magenta tint.
        /// </summary>
        [Tooltip("Sets the white balance to compensate for a green or magenta tint.")]
        public ClampedFloatParameter tint = new ClampedFloatParameter(0f, -100, 100f);

        /// <summary>
        /// Tells if the post process needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive() => temperature.value != 0f || tint.value != 0f;

        /// <summary>
        /// Tells if the post process can run the effect on-tile or if it needs a full pass.
        /// </summary>
        /// <returns><c>true</c> if it can run on-tile, <c>false</c> otherwise.</returns>
        [Obsolete("Unused #from(2023.1)", false)]
        public bool IsTileCompatible() => true;
    }
}
