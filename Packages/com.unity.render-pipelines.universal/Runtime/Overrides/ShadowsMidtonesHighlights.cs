using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Shadows, Midtones, Highlights effect.
    /// </summary>
    /// <remarks>
    /// You can add <see cref="VolumeComponent"/> to a <see cref="VolumeProfile"/> in the Editor to apply a Shadows, Midtones, Highlights post-processing effect.
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
    ///     private ShadowsMidtonesHighlights m_VolumeComponent;
    ///
    ///     [Serializable]
    ///     private struct VolumeSettings
    ///     {
    ///         public bool active;
    ///         public Vector4Parameter shadows;
    ///         public Vector4Parameter midtones;
    ///         public Vector4Parameter highlights;
    ///         public MinFloatParameter shadowsStart;
    ///         public MinFloatParameter shadowsEnd;
    ///         public MinFloatParameter highlightsStart;
    ///         public MinFloatParameter highlightsEnd;
    ///
    ///
    ///         public void SetVolumeComponentSettings(ref ShadowsMidtonesHighlights volumeComponent)
    ///         {
    ///             volumeComponent.active = active;
    ///             volumeComponent.shadows = shadows;
    ///             volumeComponent.midtones = midtones;
    ///             volumeComponent.highlights = highlights;
    ///             volumeComponent.shadowsStart = shadowsStart;
    ///             volumeComponent.shadowsEnd = shadowsEnd;
    ///             volumeComponent.highlightsStart = highlightsStart;
    ///             volumeComponent.highlightsEnd = highlightsEnd;
    ///         }
    ///
    ///         public void GetVolumeComponentSettings(ref ShadowsMidtonesHighlights volumeComponent)
    ///         {
    ///             active = volumeComponent.active;
    ///             shadows = volumeComponent.shadows;
    ///             midtones = volumeComponent.midtones;
    ///             highlights = volumeComponent.highlights;
    ///             shadowsStart = volumeComponent.shadowsStart;
    ///             shadowsEnd = volumeComponent.shadowsEnd;
    ///             highlightsStart = volumeComponent.highlightsStart;
    ///             highlightsEnd = volumeComponent.highlightsEnd;
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
    ///     private static bool GetVolumeComponent(in VolumeProfile volumeProfile, ref ShadowsMidtonesHighlights volumeComponent)
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
    ///         volumeProfile.TryGet(out ShadowsMidtonesHighlights component);
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
    /// <seealso cref="MinFloatParameter"/>
    [Serializable, VolumeComponentMenu("Post-processing/Shadows, Midtones, Highlights")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [URPHelpURL("Post-Processing-Shadows-Midtones-Highlights")]
    public sealed class ShadowsMidtonesHighlights : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Use this to control and apply a hue to the shadows.
        /// </summary>
        public Vector4Parameter shadows = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Use this to control and apply a hue to the midtones.
        /// </summary>
        public Vector4Parameter midtones = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Use this to control and apply a hue to the highlights.
        /// </summary>
        public Vector4Parameter highlights = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Start point of the transition between shadows and midtones.
        /// </summary>
        [Header("Shadow Limits")]
        [Tooltip("Start point of the transition between shadows and midtones.")]
        public MinFloatParameter shadowsStart = new MinFloatParameter(0f, 0f);

        /// <summary>
        /// End point of the transition between shadows and midtones.
        /// </summary>
        [Tooltip("End point of the transition between shadows and midtones.")]
        public MinFloatParameter shadowsEnd = new MinFloatParameter(0.3f, 0f);

        /// <summary>
        /// Start point of the transition between midtones and highlights
        /// </summary>
        [Header("Highlight Limits")]
        [Tooltip("Start point of the transition between midtones and highlights.")]
        public MinFloatParameter highlightsStart = new MinFloatParameter(0.55f, 0f);

        /// <summary>
        /// End point of the transition between midtones and highlights.
        /// </summary>
        [Tooltip("End point of the transition between midtones and highlights.")]
        public MinFloatParameter highlightsEnd = new MinFloatParameter(1f, 0f);

        /// <summary>
        /// Tells if the post process needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            var defaultState = new Vector4(1f, 1f, 1f, 0f);
            return shadows != defaultState
                || midtones != defaultState
                || highlights != defaultState;
        }

        /// <summary>
        /// Tells if the post process can run the effect on-tile or if it needs a full pass.
        /// </summary>
        /// <returns><c>true</c> if it can run on-tile, <c>false</c> otherwise.</returns>
        [Obsolete("Unused #from(2023.1)", false)]
        public bool IsTileCompatible() => true;
    }
}
