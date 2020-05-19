using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(RecursiveRendering))]
    class RecursiveRenderingEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Enable;
        SerializedDataParameter m_LayerMask;
        SerializedDataParameter m_MaxDepth;
        SerializedDataParameter m_RayLength;
        SerializedDataParameter m_MinSmoothness;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<RecursiveRendering>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));
            m_LayerMask = Unpack(o.Find(x => x.layerMask));
            m_MaxDepth = Unpack(o.Find(x => x.maxDepth));
            m_RayLength = Unpack(o.Find(x => x.rayLength));
            m_MinSmoothness = Unpack(o.Find(x => x.minSmoothness));
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
            if (HDRenderPipeline.pipelineSupportsRayTracing)
            {
                PropertyField(m_Enable);

                if (m_Enable.overrideState.boolValue && m_Enable.value.boolValue)
                {
                    EditorGUI.indentLevel++;
                    PropertyField(m_LayerMask);
                    PropertyField(m_MaxDepth);
                    PropertyField(m_RayLength);
                    PropertyField(m_MinSmoothness);
                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
