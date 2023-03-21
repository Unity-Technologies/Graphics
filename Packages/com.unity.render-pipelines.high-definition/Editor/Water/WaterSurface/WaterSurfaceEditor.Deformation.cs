using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditor.EditorGUI;
using static UnityEditor.Rendering.HighDefinition.HDProbeUI;

namespace UnityEditor.Rendering.HighDefinition
{
    sealed partial class WaterSurfaceEditor : Editor
    {
        // Deformation
        SerializedProperty m_Deformation;
        SerializedProperty m_DeformationRes;
        SerializedProperty m_DeformationAreaSize;
        SerializedProperty m_DeformationAreaOffset;

        void OnEnableDeformation(PropertyFetcher<WaterSurface> o)
        {
            m_Deformation = o.Find(x => x.deformation);
            m_DeformationRes = o.Find(x => x.deformationRes);
            m_DeformationAreaSize = o.Find(x => x.deformationAreaSize);
            m_DeformationAreaOffset = o.Find(x => x.deformationAreaOffset);
        }

        static public readonly GUIContent k_Deformation = EditorGUIUtility.TrTextContent("Enable", "Specifies if the water surfaces supports local deformations.");
        static public readonly GUIContent k_DeformationRes = EditorGUIUtility.TrTextContent("Resolution", "Specifies the resolution of the deformation texture used to represent the deformation area.");
        static public readonly GUIContent k_DeformationAreaSize = EditorGUIUtility.TrTextContent("Area Size", "Sets the size of the deformation area in meters.");
        static public readonly GUIContent k_DeformationAreaOffset = EditorGUIUtility.TrTextContent("Area Offset", "Sets the offset of the deformation area in meters.");

        static internal void WaterSurfaceDeformationSection(WaterSurfaceEditor serialized, Editor owner)
        {
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportWaterDeformation ?? false)
            {
                EditorGUILayout.Space();
                HDEditorUtils.QualitySettingsHelpBox("The current HDRP Asset does not support deformation for Water Surfaces.", MessageType.Error,
                    HDRenderPipelineUI.ExpandableGroup.Rendering, HDRenderPipelineUI.ExpandableRendering.Water, "m_RenderPipelineSettings.supportWaterDeformation");
                return;
            }

            using (new BoldLabelScope())
                EditorGUILayout.PropertyField(serialized.m_Deformation, k_Deformation);

            if (!serialized.m_Deformation.boolValue)
                return;

            using (new IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serialized.m_DeformationRes, k_DeformationRes);
                EditorGUILayout.PropertyField(serialized.m_DeformationAreaSize, k_DeformationAreaSize);
                SanitizeExtentsVector2(serialized.m_DeformationAreaSize);
                EditorGUILayout.PropertyField(serialized.m_DeformationAreaOffset, k_DeformationAreaOffset);
            }
        }
    }
}
