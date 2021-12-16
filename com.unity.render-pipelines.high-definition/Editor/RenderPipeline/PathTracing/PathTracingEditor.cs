using System;

using UnityEditor.Rendering;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Experimental.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PathTracing))]
    class PathTracingEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Enable;
        SerializedDataParameter m_LayerMask;
        SerializedDataParameter m_MaxSamples;
        SerializedDataParameter m_MinDepth;
        SerializedDataParameter m_MaxDepth;
        SerializedDataParameter m_MaxIntensity;
        SerializedDataParameter m_SkyImportanceSampling;
        SerializedDataParameter m_Denoising;
        SerializedDataParameter m_UseAOV;
        SerializedDataParameter m_Temporal;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<PathTracing>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));
            m_LayerMask = Unpack(o.Find(x => x.layerMask));
            m_MaxSamples = Unpack(o.Find(x => x.maximumSamples));
            m_MinDepth = Unpack(o.Find(x => x.minimumDepth));
            m_MaxDepth = Unpack(o.Find(x => x.maximumDepth));
            m_MaxIntensity = Unpack(o.Find(x => x.maximumIntensity));
            m_SkyImportanceSampling = Unpack(o.Find(x => x.skyImportanceSampling));

#if ENABLE_UNITY_DENOISERS
            m_Denoising = Unpack(o.Find(x => x.denoising));
            m_UseAOV = Unpack(o.Find(x => x.useAOVs));
            m_Temporal = Unpack(o.Find(x => x.temporal));
#endif
        }

        public override void OnInspectorGUI()
        {
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportRayTracing ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Ray Tracing.", MessageType.Error, wide: true);
                return;
            }

            // If ray tracing is supported display the content of the volume component
            if (HDRenderPipeline.assetSupportsRayTracing)
            {
                PropertyField(m_Enable);

                if (m_Enable.overrideState.boolValue && m_Enable.value.boolValue)
                {
                    using (new IndentLevelScope())
                    {
                        PropertyField(m_LayerMask);
                        PropertyField(m_MaxSamples);
                        PropertyField(m_MinDepth);
                        PropertyField(m_MaxDepth);
                        PropertyField(m_MaxIntensity);
                        PropertyField(m_SkyImportanceSampling);
#if ENABLE_UNITY_DENOISERS
                        PropertyField(m_Denoising);
                        var denoiserType = m_Denoising.value.GetEnumValue<DenoiserType>();
                        bool supported = Denoiser.IsDenoiserTypeSupported(denoiserType);

                        if (m_Denoising.value.intValue != (int) DenoiserType.None)
                        {
                            using (new IndentLevelScope())
                            {
                                if (supported)
                                {
                                    PropertyField(m_UseAOV);
                                    PropertyField(m_Temporal);
                                }
                                else
                                {
                                    EditorGUILayout.HelpBox($"The selected denoiser is not supported by this hardware configuration.", MessageType.Error, wide: true);
                                }
                            }
                        }
#endif
                    }

                    // Make sure MaxDepth is always greater or equal than MinDepth
                    m_MaxDepth.value.intValue = Math.Max(m_MinDepth.value.intValue, m_MaxDepth.value.intValue);
                }
            }
        }
    }
}
