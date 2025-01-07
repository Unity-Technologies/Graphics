using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HighQualityLineRenderingVolumeComponent))]
    class HighQualityLineRenderingVolumeComponentEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Enable;

        SerializedDataParameter m_CompositionMode;
        SerializedDataParameter m_ClusterCount;
        SerializedDataParameter m_SortingQuality;
        SerializedDataParameter m_TileOpacityThreshold;
        SerializedDataParameter m_AlphaDepthWriteThreshold;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<HighQualityLineRenderingVolumeComponent>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));

            m_CompositionMode      = Unpack(o.Find(x => x.compositionMode));
            m_ClusterCount         = Unpack(o.Find(x => x.clusterCount));
            m_SortingQuality       = Unpack(o.Find(x => x.sortingQuality));
            m_TileOpacityThreshold = Unpack(o.Find(x => x.tileOpacityThreshold));
            m_AlphaDepthWriteThreshold = Unpack(o.Find(x => x.writeDepthAlphaThreshold));
        }


        public override void OnInspectorGUI()
        {
            HDEditorUtils.EnsureFrameSetting(FrameSettingsField.HighQualityLineRendering);

            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            bool notSupported = currentAsset != null && !currentAsset.currentPlatformRenderPipelineSettings.supportHighQualityLineRendering;
            if (notSupported)
            {
                EditorGUILayout.Space();
                HDEditorUtils.QualitySettingsHelpBox("The current HDRP Asset does not support High Quality Line Rendering.", MessageType.Warning,
                    HDRenderPipelineUI.ExpandableGroup.Rendering,
                    HDRenderPipelineUI.ExpandableRendering.Water, "m_RenderPipelineSettings.supportHighQualityLineRendering");
            }
            using var disableScope = new EditorGUI.DisabledScope(notSupported);

            EditorGUILayout.LabelField("General", EditorStyles.miniLabel);
            PropertyField(m_Enable);
            PropertyField(m_CompositionMode);

            EditorGUILayout.LabelField("Level of Detail");
            PropertyField(m_ClusterCount);
            PropertyField(m_SortingQuality);
            PropertyField(m_TileOpacityThreshold);
            PropertyField(m_AlphaDepthWriteThreshold);
        }
    }
}
