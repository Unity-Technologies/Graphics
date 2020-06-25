using System;

using UnityEditor.Rendering;

using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Experimental.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(PathTracing))]
    class PathTracingEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Enable;
        SerializedDataParameter m_LayerMask;
        SerializedDataParameter m_MaxSamples;
        SerializedDataParameter m_MinDepth;
        SerializedDataParameter m_MaxDepth;
        SerializedDataParameter m_MaxIntensity;
        SerializedDataParameter m_Filter;
        SerializedDataParameter m_FilterWidth;
        SerializedDataParameter m_FilterHeight;
        SerializedDataParameter m_AdaptiveSampling;
        SerializedDataParameter m_Threshold;
        SerializedDataParameter m_MinSamples;
        SerializedDataParameter m_Hits;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<PathTracing>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));
            m_LayerMask = Unpack(o.Find(x => x.layerMask));
            m_MaxSamples = Unpack(o.Find(x => x.maximumSamples));
            m_MinDepth = Unpack(o.Find(x => x.minimumDepth));
            m_MaxDepth = Unpack(o.Find(x => x.maximumDepth));
            m_MaxIntensity = Unpack(o.Find(x => x.maximumIntensity));
            m_Filter = Unpack(o.Find(x => x.filter));
            m_FilterWidth = Unpack(o.Find(x => x.filterWidth));
            m_FilterHeight = Unpack(o.Find(x => x.filterHeight));
            m_AdaptiveSampling = Unpack(o.Find(x => x.adaptive));
            m_Threshold = Unpack(o.Find(x => x.threshold));
            m_MinSamples = Unpack(o.Find(x => x.minimumSamples));
            m_Hits = Unpack(o.Find(x => x.hits));
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
                    PropertyField(m_MaxSamples);
                    PropertyField(m_MinDepth);
                    PropertyField(m_MaxDepth);
                    PropertyField(m_MaxIntensity);

                    PropertyField(m_AdaptiveSampling);
                    using (new EditorGUI.DisabledScope(!m_AdaptiveSampling.value.boolValue))
                    {
                        PropertyField(m_Threshold);
                        PropertyField(m_MinSamples);
                        PropertyField(m_Hits);
                    }

                    EditorGUILayout.LabelField("Antialiasing", EditorStyles.miniLabel);
                    PropertyField(m_Filter);
                    PropertyField(m_FilterWidth);
                    PropertyField(m_FilterHeight);

                    EditorGUI.indentLevel--;

                    // Make sure MaxDepth is always greater or equal than MinDepth
                    m_MaxDepth.value.intValue = Math.Max(m_MinDepth.value.intValue, m_MaxDepth.value.intValue);
                }
            }
        }
    }
}
