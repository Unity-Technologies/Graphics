using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Split Toning effect.
    /// </summary>
    /// <remarks>
    /// You can add <see cref="VolumeComponent"/> to a <see cref="VolumeProfile"/> in the Editor to apply a Split Toning post-processing effect.
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
    ///     private SplitToning m_VolumeComponent;
    ///
    ///     [Serializable]
    ///     private struct VolumeSettings
    ///     {
    ///         public bool active;
    ///         public ColorParameter shadows;
    ///         public ColorParameter highlights;
    ///         public ClampedFloatParameter balance;
    ///
    ///
    ///         public void SetVolumeComponentSettings(ref SplitToning volumeComponent)
    ///         {
    ///             volumeComponent.active = active;
    ///             volumeComponent.shadows = shadows;
    ///             volumeComponent.highlights = highlights;
    ///             volumeComponent.balance = balance;
    ///         }
    ///
    ///         public void GetVolumeComponentSettings(ref SplitToning volumeComponent)
    ///         {
    ///             active = volumeComponent.active;
    ///             shadows = volumeComponent.shadows;
    ///             highlights = volumeComponent.highlights;
    ///             balance = volumeComponent.balance;
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
    ///     private static bool GetVolumeComponent(in VolumeProfile volumeProfile, ref SplitToning volumeComponent)
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
    ///         volumeProfile.TryGet(out SplitToning component);
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
    /// <seealso cref="ColorParameter"/>
    /// <seealso cref="ClampedFloatParameter"/>
    [Serializable, VolumeComponentMenu("Post-processing/Split Toning")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [URPHelpURL("Post-Processing-Split-Toning")]
    public sealed class SplitToning : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// The color to use for shadows.
        /// </summary>
        [Tooltip("The color to use for shadows.")]
        public ColorParameter shadows = new ColorParameter(Color.grey, false, false, true);

        /// <summary>
        /// The color to use for highlights.
        /// </summary>
        [Tooltip("The color to use for highlights.")]
        public ColorParameter highlights = new ColorParameter(Color.grey, false, false, true);

        /// <summary>
        /// Balance between the colors in the highlights and shadows.
        /// </summary>
        [Tooltip("Balance between the colors in the highlights and shadows.")]
        public ClampedFloatParameter balance = new ClampedFloatParameter(0f, -100f, 100f);

        /// <summary>
        /// Tells if the post process needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive() => shadows != Color.grey || highlights != Color.grey;

        /// <summary>
        /// Tells if the post process can run the effect on-tile or if it needs a full pass.
        /// </summary>
        /// <returns><c>true</c> if it can run on-tile, <c>false</c> otherwise.</returns>
        [Obsolete("Unused #from(2023.1)", false)]
        public bool IsTileCompatible() => true;
    }
}
