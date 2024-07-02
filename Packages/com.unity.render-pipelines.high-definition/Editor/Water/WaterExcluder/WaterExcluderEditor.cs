using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditor.EditorGUI;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(WaterExcluder))]
    sealed partial class WaterExcluderEditor : Editor
    {
        // General parameters
        SerializedProperty M_Mesh;

        void OnEnable()
        {
            var o = new PropertyFetcher<WaterExcluder>(serializedObject);
            M_Mesh = o.Find(x => x.m_InternalMesh);
        }

        static public readonly GUIContent k_MeshText = EditorGUIUtility.TrTextContent("Mesh", "Specifies the mesh filter used for the exclusion.");

        public override void OnInspectorGUI()
        {
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportWaterExclusion ?? false)
            {
                EditorGUILayout.Space();
                HDEditorUtils.QualitySettingsHelpBox("The current HDRP Asset does not support exclusion for Water Surfaces.", MessageType.Error,
                    HDRenderPipelineUI.ExpandableGroup.Rendering, HDRenderPipelineUI.ExpandableRendering.Water, "m_RenderPipelineSettings.supportWaterExclusion");
                return;
            }

            serializedObject.Update();

            // Mesh
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(M_Mesh, k_MeshText);
            if (EditorGUI.EndChangeCheck())
            {
                var excluder = (WaterExcluder)serializedObject.targetObject;
                excluder.SetExclusionMesh((Mesh)M_Mesh.objectReferenceValue);
            }

            // Apply the properties
            serializedObject.ApplyModifiedProperties();
        }

        // Anis 11/09/21: Currently, there is a bug that makes the icon disappear after the first selection
        // if we do not have this. Given that the geometry is procedural, we need this to be able to
        // select the water surfaces.
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(WaterExcluder waterSurface, GizmoType gizmoType)
        {
        }

        void OnSceneGUI()
        {
            WaterExcluder deformer = target as WaterExcluder;
            var mesh = deformer.m_InternalMesh;
            var tr = deformer.transform;

            if (mesh != null && deformer.m_ExclusionRenderer != null)
            {
                Handles.DrawOutline(new GameObject[] { deformer.m_ExclusionRenderer }, Color.white);
            }
        }
    }
}
