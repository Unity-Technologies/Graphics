using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MaterialPlacer))]
public class MaterialPlacerEditor : Editor
{
    SerializedProperty m_TargetType;
    SerializedProperty m_Prefab;
    SerializedProperty m_Rows;
    SerializedProperty m_Cols;
    SerializedProperty m_Size;
    SerializedProperty m_PropertyName;
    SerializedProperty m_FromValueFloat;
    SerializedProperty m_ToValueFloat;
    SerializedProperty m_FromValueColor;
    SerializedProperty m_ToValueColor;

    void OnEnable()
    {
        m_TargetType = serializedObject.FindProperty("m_TargetType");
        m_Prefab = serializedObject.FindProperty("m_Prefab");
        m_Rows = serializedObject.FindProperty("m_Rows");
        m_Cols = serializedObject.FindProperty("m_Cols");
        m_Size = serializedObject.FindProperty("m_Size");
        m_PropertyName = serializedObject.FindProperty("m_PropertyName");
        m_FromValueFloat = serializedObject.FindProperty("m_FromValueFloat");
        m_ToValueFloat = serializedObject.FindProperty("m_ToValueFloat");
        m_FromValueColor = serializedObject.FindProperty("m_FromValueColor");
        m_ToValueColor = serializedObject.FindProperty("m_ToValueColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(m_Prefab);
        EditorGUILayout.PropertyField(m_Rows);
        EditorGUILayout.PropertyField(m_Cols);
        EditorGUILayout.PropertyField(m_Size);
        EditorGUILayout.PropertyField(m_PropertyName);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(m_TargetType);
        var targetTypeValue = (MaterialPlacer.TargetType)m_TargetType.enumValueIndex;

        switch (targetTypeValue)
        {
            case MaterialPlacer.TargetType.Color:
            {
                EditorGUILayout.PropertyField(m_FromValueColor);
                EditorGUILayout.PropertyField(m_ToValueColor);
                break;
            }
            case MaterialPlacer.TargetType.Float:
            {
                EditorGUILayout.PropertyField(m_FromValueFloat);
                EditorGUILayout.PropertyField(m_ToValueFloat);
                break;
            }
        }

        if (EditorGUI.EndChangeCheck())
            serializedObject.ApplyModifiedProperties();
    }
}
