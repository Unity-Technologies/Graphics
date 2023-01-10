using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;


namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(WaterRendering))]
    class WaterRenderingEditor : VolumeComponentEditor
    {
        // General
        SerializedDataParameter m_Enable;

        // LOD
        SerializedDataParameter m_MinGridSize;
        SerializedDataParameter m_MaxGridSize;
        SerializedDataParameter m_ElevationTransition;
        SerializedDataParameter m_NumLevelOfDetails;

        // Tessellation
        SerializedDataParameter m_MaxTessellationFactor;
        SerializedDataParameter m_TessellationFactorFadeStart;
        SerializedDataParameter m_TessellationFactorFadeRange;

        // Lighting
        SerializedDataParameter m_AmbientProbeDimmer;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<WaterRendering>(serializedObject);
            // General
            m_Enable = Unpack(o.Find(x => x.enable));

            // LOD
            m_MinGridSize = Unpack(o.Find(x => x.minGridSize));
            m_MaxGridSize = Unpack(o.Find(x => x.maxGridSize));
            m_ElevationTransition = Unpack(o.Find(x => x.elevationTransition));
            m_NumLevelOfDetails = Unpack(o.Find(x => x.numLevelOfDetails));

            // Tessellation
            m_MaxTessellationFactor = Unpack(o.Find(x => x.maxTessellationFactor));
            m_TessellationFactorFadeStart = Unpack(o.Find(x => x.tessellationFactorFadeStart));
            m_TessellationFactorFadeRange = Unpack(o.Find(x => x.tessellationFactorFadeRange));

            // Lighting
            m_AmbientProbeDimmer = Unpack(o.Find(x => x.ambientProbeDimmer));
        }

        public override void OnInspectorGUI()
        {
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportWater ?? false)
            {
                EditorGUILayout.Space();
                HDEditorUtils.QualitySettingsHelpBox("The current HDRP Asset does not support Water Surfaces.", MessageType.Error,
                    HDRenderPipelineUI.Expandable.Water, "m_RenderPipelineSettings.supportWater");
                return;
            }

            EditorGUILayout.LabelField("General", EditorStyles.miniLabel);
            PropertyField(m_Enable, EditorGUIUtility.TrTextContent("State"));

            EditorGUILayout.LabelField("Level of Detail", EditorStyles.miniLabel);
            PropertyField(m_MinGridSize);
            PropertyField(m_MaxGridSize);
            PropertyField(m_ElevationTransition);
            PropertyField(m_NumLevelOfDetails);

            if (showAdditionalProperties)
                EditorGUILayout.LabelField("Tessellation", EditorStyles.miniLabel);
            PropertyField(m_MaxTessellationFactor);
            PropertyField(m_TessellationFactorFadeStart);
            PropertyField(m_TessellationFactorFadeRange);

            EditorGUILayout.LabelField("Lighting", EditorStyles.miniLabel);
            PropertyField(m_AmbientProbeDimmer);
        }
    }
}
