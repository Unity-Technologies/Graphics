using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditorInternal;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeReferenceVolumeProfile))]
    internal class ProbeReferenceVolumeProfileEditor : Editor
    {
        private SerializedProperty m_CellSize;
        private SerializedProperty m_BrickSize;
        private SerializedProperty m_MaxSubdivision;
        private SerializedProperty m_NormalBias;
        private SerializedProperty m_IndexDimensions;

        private static bool IndexDimensionGroupEnabled;

        private void OnEnable()
        {
            m_CellSize = serializedObject.FindProperty("CellSize");
            m_BrickSize = serializedObject.FindProperty("BrickSize");
            m_MaxSubdivision = serializedObject.FindProperty("MaxSubdivision");
            m_NormalBias = serializedObject.FindProperty("NormalBias");
            m_IndexDimensions = serializedObject.FindProperty("IndexDimensions");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            m_CellSize.intValue = EditorGUILayout.IntField("Cell Size", m_CellSize.intValue);
            m_BrickSize.intValue = EditorGUILayout.IntField("Brick Size", m_BrickSize.intValue);
            m_MaxSubdivision.intValue = EditorGUILayout.IntField("Max Subdivision Level", m_MaxSubdivision.intValue);
            m_NormalBias.floatValue = EditorGUILayout.FloatField("Normal Bias", m_NormalBias.floatValue);

            // TODO: This needs to be moved to advanced settings as soon as the refactor lands. Also tmp naming.
            IndexDimensionGroupEnabled = EditorGUILayout.BeginFoldoutHeaderGroup(IndexDimensionGroupEnabled, "Index field dimensions");
            if (IndexDimensionGroupEnabled)
            {
                int newIndexDimX = EditorGUILayout.DelayedIntField("Index Field Dimension X", m_IndexDimensions.vector3IntValue.x);
                int newIndexDimY = EditorGUILayout.DelayedIntField("Index Field Dimension Y", m_IndexDimensions.vector3IntValue.y);
                int newIndexDimZ = EditorGUILayout.DelayedIntField("Index Field Dimension Z", m_IndexDimensions.vector3IntValue.z);
                if (newIndexDimX != m_IndexDimensions.vector3IntValue.x ||
                    newIndexDimY != m_IndexDimensions.vector3IntValue.y ||
                    newIndexDimZ != m_IndexDimensions.vector3IntValue.z)
                {
                    m_IndexDimensions.vector3IntValue = new Vector3Int(newIndexDimX, newIndexDimY, newIndexDimZ);
                    var refVol = ProbeReferenceVolume.instance;
                    refVol.AddPendingIndexDimensionChange(m_IndexDimensions.vector3IntValue);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();


            if (EditorGUI.EndChangeCheck())
            {
                Constrain();
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void Constrain()
        {
            m_CellSize.intValue = Mathf.Max(m_CellSize.intValue, 1);
            m_BrickSize.intValue = Mathf.Max(m_BrickSize.intValue, 1);
            m_MaxSubdivision.intValue = Mathf.Clamp(m_MaxSubdivision.intValue, 0, 15);
            m_NormalBias.floatValue = Mathf.Max(m_NormalBias.floatValue, 0);
        }
    }
}
