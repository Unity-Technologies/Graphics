using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Lens Distortion effect.
    /// </summary>
    /// <remarks>
    /// You can add <see cref="VolumeComponent"/> to a <see cref="VolumeProfile"/> in the Editor to apply a Lens Distortion post-processing effect.
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
    ///     private LensDistortion m_VolumeComponent;
    ///
    ///     [Serializable]
    ///     private struct VolumeSettings
    ///     {
    ///         public bool active;
    ///         public ClampedFloatParameter intensity;
    ///         public ClampedFloatParameter xMultiplier;
    ///         public ClampedFloatParameter yMultiplier;
    ///         public Vector2Parameter center;
    ///         public ClampedFloatParameter scale;
    ///
    ///         public void SetVolumeComponentSettings(ref LensDistortion volumeComponent)
    ///         {
    ///             volumeComponent.active = active;
    ///             volumeComponent.intensity = intensity;
    ///             volumeComponent.xMultiplier = xMultiplier;
    ///             volumeComponent.yMultiplier = yMultiplier;
    ///             volumeComponent.center = center;
    ///             volumeComponent.scale = scale;
    ///         }
    ///
    ///         public void GetVolumeComponentSettings(ref LensDistortion volumeComponent)
    ///         {
    ///             active = volumeComponent.active;
    ///             intensity = volumeComponent.intensity;
    ///             xMultiplier = volumeComponent.xMultiplier;
    ///             yMultiplier = volumeComponent.yMultiplier;
    ///             center = volumeComponent.center;
    ///             scale = volumeComponent.scale;
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
    ///     private static bool GetVolumeComponent(in VolumeProfile volumeProfile, ref LensDistortion volumeComponent)
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
    ///         volumeProfile.TryGet(out LensDistortion component);
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
    /// <seealso cref="Vector2Parameter"/>
    [Serializable, VolumeComponentMenu("Post-processing/Lens Distortion")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [URPHelpURL("Post-Processing-Lens-Distortion")]
    public sealed class LensDistortion : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Total distortion amount.
        /// </summary>
        [Tooltip("Total distortion amount.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, -1f, 1f);

        /// <summary>
        /// Intensity multiplier on X axis. Set it to 0 to disable distortion on this axis.
        /// </summary>
        [Tooltip("Intensity multiplier on X axis. Set it to 0 to disable distortion on this axis.")]
        public ClampedFloatParameter xMultiplier = new ClampedFloatParameter(1f, 0f, 1f);

        /// <summary>
        /// Intensity multiplier on Y axis. Set it to 0 to disable distortion on this axis.
        /// </summary>
        [Tooltip("Intensity multiplier on Y axis. Set it to 0 to disable distortion on this axis.")]
        public ClampedFloatParameter yMultiplier = new ClampedFloatParameter(1f, 0f, 1f);

        /// <summary>
        /// Distortion center point. 0.5,0.5 is center of the screen
        /// </summary>
        [Tooltip("Distortion center point. 0.5,0.5 is center of the screen.")]
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));

        /// <summary>
        /// Controls global screen scaling for the distortion effect. Use this to hide the screen borders when using high \"Intensity.\"
        /// </summary>
        [Tooltip("Controls global screen scaling for the distortion effect. Use this to hide the screen borders when using high \"Intensity.\"")]
        public ClampedFloatParameter scale = new ClampedFloatParameter(1f, 0.01f, 5f);

        /// <summary>
        /// Tells if the post process needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return Mathf.Abs(intensity.value) > 0
                && (xMultiplier.value > 0f || yMultiplier.value > 0f);
        }

        /// <summary>
        /// Tells if the post process can run the effect on-tile or if it needs a full pass.
        /// </summary>
        /// <returns><c>true</c> if it can run on-tile, <c>false</c> otherwise.</returns>
        [Obsolete("Unused #from(2023.1)", false)]
        public bool IsTileCompatible() => false;
    }
}
