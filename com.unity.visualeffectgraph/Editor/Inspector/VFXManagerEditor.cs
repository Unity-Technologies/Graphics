using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Rendering;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;

using UnityObject = UnityEngine.Object;

[CustomEditor(typeof(UnityEditor.VFXManager))]
public class VFXManagerEditor : Editor
{

    SerializedProperty m_PathProperty;
    SerializedProperty[] m_TimeProperties;
    SerializedProperty[] m_ShaderProperties;
    void OnEnable()
    {
        m_PathProperty = serializedObject.FindProperty("m_RenderPipeSettingsPath");
        m_TimeProperties = new SerializedProperty[]{ serializedObject.FindProperty("m_FixedTimeStep"), serializedObject.FindProperty("m_MaxDeltaTime")};
        m_ShaderProperties = new SerializedProperty[]{ serializedObject.FindProperty("m_IndirectShader"), serializedObject.FindProperty("m_CopyBufferShader"), serializedObject.FindProperty("m_SortShader")};
        CheckVFXManager();
        serializedObject.Update();
    }

    void OnDisable()
    {
    }

    public override void OnInspectorGUI()
    {
        // trying to detect a C++ reset by checking if all shaders have been reset to null and the path to ""
        if( string.IsNullOrEmpty(m_PathProperty.stringValue) && ! m_ShaderProperties.Any(t=>t.objectReferenceValue != null))
            CheckVFXManager();
        serializedObject.Update();
        
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(m_PathProperty.name));
        string resultPath = GUILayout.TextArea(m_PathProperty.stringValue, 500, GUILayout.Height(30));
        if (EditorGUI.EndChangeCheck())
        {
            m_PathProperty.stringValue = resultPath;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Default"))
        {
            m_PathProperty.stringValue = "Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/HDRP";
        }
        if (GUILayout.Button("Reveal"))
        {
            EditorUtility.RevealInFinder(resultPath);
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(15);

        foreach (var property in m_TimeProperties)
        {
            EditorGUILayout.PropertyField(property);
        }

        GUILayout.Space(15);

        foreach (var property in m_ShaderProperties)
        {
            EditorGUILayout.PropertyField(property);
        }
        serializedObject.ApplyModifiedProperties();
    }

    public static void CheckVFXManager()
    {
        UnityObject vfxmanager = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/VFXManager.asset");
        if (vfxmanager == null)
            return;

        SerializedObject obj = new SerializedObject(vfxmanager);

        var pathProperty = obj.FindProperty("m_RenderPipeSettingsPath");
        if (string.IsNullOrEmpty(pathProperty.stringValue))
        {
            pathProperty.stringValue = "Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/HDRP";
        }

        var indirectShaderProperty = obj.FindProperty("m_IndirectShader");
        if (indirectShaderProperty.objectReferenceValue == null)
        {
            indirectShaderProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.unity.visualeffectgraph/Shaders/VFXFillIndirectArgs.compute");
        }
        var copyShaderProperty = obj.FindProperty("m_CopyBufferShader");
        if (copyShaderProperty.objectReferenceValue == null)
        {
            copyShaderProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.unity.visualeffectgraph/Shaders/VFXCopyBuffer.compute");
        }
        var sortProperty = obj.FindProperty("m_SortShader");
        if (sortProperty.objectReferenceValue == null)
        {
            sortProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.unity.visualeffectgraph/Shaders/Sort.compute");
        }

        obj.ApplyModifiedPropertiesWithoutUndo();
    }
}
