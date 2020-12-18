using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(RayTracingSettings))]
    class RayTracingSettingsEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_RayBias;
        SerializedDataParameter m_ExtendShadowCulling;
        SerializedDataParameter m_ExtendCameraCulling;
        SerializedDataParameter m_DirectionalShadowRayLength;
        SerializedDataParameter m_DirectionalShadowFallbackIntensity;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<RayTracingSettings>(serializedObject);

            m_RayBias = Unpack(o.Find(x => x.rayBias));
            m_ExtendShadowCulling = Unpack(o.Find(x => x.extendShadowCulling));
            m_ExtendCameraCulling = Unpack(o.Find(x => x.extendCameraCulling));
            m_DirectionalShadowRayLength = Unpack(o.Find(x => x.directionalShadowRayLength));
            m_DirectionalShadowFallbackIntensity = Unpack(o.Find(x => x.directionalShadowFallbackIntensity));
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

            PropertyField(m_RayBias);
            PropertyField(m_ExtendShadowCulling);
            PropertyField(m_ExtendCameraCulling);
            PropertyField(m_DirectionalShadowRayLength);
            PropertyField(m_DirectionalShadowFallbackIntensity);
        }
    }
}
