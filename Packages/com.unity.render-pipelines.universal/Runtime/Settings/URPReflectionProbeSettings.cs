using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering.Universal;
#endif
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// ReflectionProbe global settings class.
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Lighting", Order = 21)]
    public class URPReflectionProbeSettings : IRenderPipelineGraphicsSettings
    {
        [SerializeField, HideInInspector] private int version = 1;

        int IRenderPipelineGraphicsSettings.version => version;

        [SerializeField, Tooltip("Use ReflectionProbe rotation. Enabling this will improve the appearance of reflections when the ReflectionProbe isn't axis aligned, but may worsen performance on lower end platforms.")]
        private bool useReflectionProbeRotation = true;

        /// <summary>
        /// Whether to take ReflectionProbe rotation into account when rendering.
        /// </summary>
        public bool UseReflectionProbeRotation
        {
            get
            {
#if UNITY_EDITOR
                var mode = useReflectionProbeRotation ? SupportedRenderingFeatures.ReflectionProbeModes.Rotation : SupportedRenderingFeatures.ReflectionProbeModes.None;
                if (mode != SupportedRenderingFeatures.active.reflectionProbeModes)
                {
                    SupportedRenderingFeatures.active.reflectionProbeModes = mode;
                }
#endif
                return useReflectionProbeRotation;
            }
#if UNITY_EDITOR
            internal set
            {
                this.SetValueAndNotify(ref useReflectionProbeRotation, value, nameof(useReflectionProbeRotation));
                if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset urpAsset)
                {
                    SupportedRenderingFeatures.active.reflectionProbeModes = value ? SupportedRenderingFeatures.ReflectionProbeModes.Rotation : SupportedRenderingFeatures.ReflectionProbeModes.None;
                }
            }
#endif
        }
    }
}
