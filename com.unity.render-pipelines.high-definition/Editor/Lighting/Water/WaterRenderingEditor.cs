using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;


namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(WaterRendering))]
    class WaterRenderingEditor : VolumeComponentEditor
    {
        // General
        SerializedDataParameter m_Enable;
        SerializedDataParameter m_GridResolution;
        SerializedDataParameter m_GridSize;
        SerializedDataParameter m_NumLevelOfDetails;
        SerializedDataParameter m_AmbientProbeDimmer;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<WaterRendering>(serializedObject);
            // General
            m_Enable = Unpack(o.Find(x => x.enable));
            m_GridResolution = Unpack(o.Find(x => x.gridResolution));
            m_GridSize = Unpack(o.Find(x => x.gridSize));
            m_NumLevelOfDetails = Unpack(o.Find(x => x.numLevelOfDetails));
            m_AmbientProbeDimmer = Unpack(o.Find(x => x.ambientProbeDimmer));
        }

        public override void OnInspectorGUI()
        {
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportWater ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Water Surfaces.", MessageType.Error, wide: true);
                return;
            }

            EditorGUILayout.LabelField("General", EditorStyles.miniLabel);
            PropertyField(m_Enable);
            PropertyField(m_GridResolution);
            PropertyField(m_GridSize);
            PropertyField(m_NumLevelOfDetails);
            PropertyField(m_AmbientProbeDimmer);
        }
    }
}
