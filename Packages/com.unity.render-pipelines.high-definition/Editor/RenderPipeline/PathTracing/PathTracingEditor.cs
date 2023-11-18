using System;

using UnityEditor.Rendering;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

// Enable the denoising UI only on windows 64
#if UNITY_64 && ENABLE_UNITY_DENOISING_PLUGIN && UNITY_EDITOR_WIN
using UnityEngine.Rendering.Denoising;
#endif

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
        SerializedDataParameter m_SeedMode;
        SerializedDataParameter m_Denoising;
        SerializedDataParameter m_UseAOV;
        SerializedDataParameter m_Temporal;

#if UNITY_64 && !ENABLE_UNITY_DENOISING_PLUGIN && UNITY_EDITOR_WIN
        // This is used to prevent users from spamming the denoising package install button
        bool s_DisplayDenoisingButtonInstall = true;
#endif

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
            m_SeedMode = Unpack(o.Find(x => x.seedMode));

#if UNITY_64 && ENABLE_UNITY_DENOISING_PLUGIN && UNITY_EDITOR_WIN
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
                PropertyField(m_Enable, EditorGUIUtility.TrTextContent("State"));

                if (m_Enable.overrideState.boolValue && m_Enable.value.boolValue)
                {
                    using (new IndentLevelScope())
                    {
                        if (RenderPipelineManager.currentPipeline is not HDRenderPipeline { rayTracingSupported: true })
                            HDRenderPipelineUI.DisplayRayTracingSupportBox();

                        PropertyField(m_LayerMask);
                        PropertyField(m_MaxSamples);
                        PropertyField(m_MinDepth);
                        PropertyField(m_MaxDepth);
                        PropertyField(m_MaxIntensity);
                        PropertyField(m_SkyImportanceSampling);
                    	PropertyField(m_SeedMode);
#if UNITY_64 && ENABLE_UNITY_DENOISING_PLUGIN && UNITY_EDITOR_WIN
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
                                    if (m_Denoising.value.intValue == (int)DenoiserType.Optix)
                                    {
                                        PropertyField(m_Temporal);
                                    }
                                }
                                else
                                {
                                    EditorGUILayout.HelpBox($"The denoiser selected is not supported by this hardware configuration.", MessageType.Error, wide: true);
                                }
                            }
                        }
#elif UNITY_64 && UNITY_EDITOR_WIN
                        if (s_DisplayDenoisingButtonInstall)
                        {
                            CoreEditorUtils.DrawFixMeBox("Path Tracing Denoising is not active in this project. To activate it, install the Unity Denoising package.", MessageType.Info, () =>
                            {
                                PackageManager.Client.Add("com.unity.rendering.denoising");
                                s_DisplayDenoisingButtonInstall = false;
                            });
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Installing the denoising package. Please wait...", MessageType.Info, wide: true);
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
