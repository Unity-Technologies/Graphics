using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Lift-Gamma-Gain effect.
    /// </summary>
    /// <remarks>
    /// You can add <see cref="VolumeComponent"/> to a <see cref="VolumeProfile"/> in the Editor to apply a Lift-Gamma-Gain post-processing effect.
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
    ///     private LiftGammaGain m_VolumeComponent;
    ///
    ///     [Serializable]
    ///     private struct VolumeSettings
    ///     {
    ///         public bool active;
    ///         public Vector4Parameter lift;
    ///         public Vector4Parameter gamma;
    ///         public Vector4Parameter gain;
    ///
    ///         public void SetVolumeComponentSettings(ref LiftGammaGain volumeComponent)
    ///         {
    ///             volumeComponent.active = active;
    ///             volumeComponent.lift = lift;
    ///             volumeComponent.gamma = gamma;
    ///             volumeComponent.gain = gain;
    ///         }
    ///
    ///         public void GetVolumeComponentSettings(ref LiftGammaGain volumeComponent)
    ///         {
    ///             active = volumeComponent.active;
    ///             lift = volumeComponent.lift;
    ///             gamma = volumeComponent.gamma;
    ///             gain = volumeComponent.gain;
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
    ///     private static bool GetVolumeComponent(in VolumeProfile volumeProfile, ref LiftGammaGain volumeComponent)
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
    ///         volumeProfile.TryGet(out LiftGammaGain component);
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
    /// <seealso cref="Vector4Parameter"/>
    [Serializable, VolumeComponentMenu("Post-processing/Lift, Gamma, Gain")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [URPHelpURL("Post-Processing-Lift-Gamma-Gain")]
    public sealed class LiftGammaGain : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Use this to control and apply a hue to the dark tones. This has a more exaggerated effect on shadows.
        /// </summary>
        public Vector4Parameter lift = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Use this to control and apply a hue to the mid-range tones with a power function.
        /// </summary>
        public Vector4Parameter gamma = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Use this to increase and apply a hue to the signal and make highlights brighter.
        /// </summary>
        public Vector4Parameter gain = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Tells if the post process needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            var defaultState = new Vector4(1f, 1f, 1f, 0f);
            return lift != defaultState
                || gamma != defaultState
                || gain != defaultState;
        }

        /// <summary>
        /// Tells if the post process can run the effect on-tile or if it needs a full pass.
        /// </summary>
        /// <returns><c>true</c> if it can run on-tile, <c>false</c> otherwise.</returns>
        [Obsolete("Unused #from(2023.1)", false)]
        public bool IsTileCompatible() => true;
    }
}
