using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(RayTracingSettings))]
    class RayTracingSettingsEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_RayBias;
        SerializedDataParameter m_DistantRayBias;
        SerializedDataParameter m_ExtendShadowCulling;
        SerializedDataParameter m_ExtendCameraCulling;
        SerializedDataParameter m_DirectionalShadowRayLength;
        SerializedDataParameter m_DirectionalShadowFallbackIntensity;
        SerializedDataParameter m_RTASBuildMode;
        SerializedDataParameter m_CullingMode;
        SerializedDataParameter m_CullingDistance;

        static public readonly GUIContent k_RTASBuildModeText = EditorGUIUtility.TrTextContent("Acceleration Structure Build Mode", "Specifies if HDRP handles automatically the building of the ray tracing acceleration structure internally or if it's provided by the user through the camera. If manual is selected and no acceleration structure is fed to the camera, ray-traced effects are not executed and fallback to rasterization.");
        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<RayTracingSettings>(serializedObject);

            m_RayBias = Unpack(o.Find(x => x.rayBias));
            m_DistantRayBias = Unpack(o.Find(x => x.distantRayBias));
            m_ExtendShadowCulling = Unpack(o.Find(x => x.extendShadowCulling));
            m_ExtendCameraCulling = Unpack(o.Find(x => x.extendCameraCulling));
            m_DirectionalShadowRayLength = Unpack(o.Find(x => x.directionalShadowRayLength));
            m_DirectionalShadowFallbackIntensity = Unpack(o.Find(x => x.directionalShadowFallbackIntensity));
            m_RTASBuildMode = Unpack(o.Find(x => x.buildMode));
            m_CullingMode = Unpack(o.Find(x => x.cullingMode));
            m_CullingDistance = Unpack(o.Find(x => x.cullingDistance));
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

            if (RenderPipelineManager.currentPipeline is not HDRenderPipeline { rayTracingSupported: true })
                HDRenderPipelineUI.DisplayRayTracingSupportBox();

            PropertyField(m_RayBias);

            // Make sure the distant ray bias is always bigger or equal to the regular ray bias.
            PropertyField(m_DistantRayBias);
            m_DistantRayBias.value.floatValue = Mathf.Max(m_DistantRayBias.value.floatValue, m_RayBias.value.floatValue);
            PropertyField(m_ExtendShadowCulling);
            PropertyField(m_ExtendCameraCulling);
            PropertyField(m_DirectionalShadowRayLength);
            PropertyField(m_DirectionalShadowFallbackIntensity);

            PropertyField(m_RTASBuildMode, k_RTASBuildModeText);
            if ((RTASBuildMode)m_RTASBuildMode.value.enumValueIndex == RTASBuildMode.Manual)
            {
                EditorGUILayout.HelpBox("When set to Manual, the RTAS build mode expects a ray tracing acceleration structure to be set on the camera. If not, all ray traced effects will be disabled. This option does not affect the scene view.", MessageType.Info, wide: true);
            }

            PropertyField(m_CullingMode);
            if ((RTASCullingMode)m_CullingMode.value.enumValueIndex == RTASCullingMode.Sphere)
            {
                using (new IndentLevelScope())
                {
                    PropertyField(m_CullingDistance);
                }
            }
        }
    }
}
