using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Panini Projection effect.
    /// </summary>
    /// <remarks>
    /// You can add <see cref="VolumeComponent"/> to a <see cref="VolumeProfile"/> in the Editor to apply a Panini Projection post-processing effect.
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
    ///     private PaniniProjection m_VolumeComponent;
    ///
    ///     [Serializable]
    ///     private struct VolumeSettings
    ///     {
    ///         public bool active;
    ///         public ClampedFloatParameter distance;
    ///         public ClampedFloatParameter cropToFit;
    ///
    ///         public void SetVolumeComponentSettings(ref PaniniProjection volumeComponent)
    ///         {
    ///             volumeComponent.active = active;
    ///             volumeComponent.distance = distance;
    ///             volumeComponent.cropToFit = cropToFit;
    ///         }
    ///
    ///         public void GetVolumeComponentSettings(ref PaniniProjection volumeComponent)
    ///         {
    ///             active = volumeComponent.active;
    ///             distance = volumeComponent.distance;
    ///             cropToFit = volumeComponent.cropToFit;
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
    ///     private static bool GetVolumeComponent(in VolumeProfile volumeProfile, ref PaniniProjection volumeComponent)
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
    ///         volumeProfile.TryGet(out PaniniProjection component);
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
    [Serializable, VolumeComponentMenu("Post-processing/Panini Projection")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [URPHelpURL("Post-Processing-Panini-Projection")]
    public sealed class PaniniProjection : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Controls the panini projection distance. This controls the strength of the distorion.
        /// </summary>
        [Tooltip("Panini projection distance.")]
        public ClampedFloatParameter distance = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Controls how much cropping HDRP applies to the screen with the panini projection effect. A value of 1 crops the distortion to the edge of the screen.
        /// </summary>
        [Tooltip("Panini projection crop to fit.")]
        public ClampedFloatParameter cropToFit = new ClampedFloatParameter(1f, 0f, 1f);

        /// <summary>
        /// Tells if the post process needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive() => distance.value > 0f;

        /// <summary>
        /// Tells if the post process can run the effect on-tile or if it needs a full pass.
        /// </summary>
        /// <returns><c>true</c> if it can run on-tile, <c>false</c> otherwise.</returns>
        [Obsolete("Unused #from(2023.1)", false)]
        public bool IsTileCompatible() => false;
    }
}
