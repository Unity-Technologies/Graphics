using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;
using UnityEditor.Callbacks;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;

using UnityObject = UnityEngine.Object;

[CustomEditor(typeof(UnityEditor.VFXManager))]
class VFXManagerEditor : Editor
{
    SerializedProperty[] m_TimeProperties;
    SerializedProperty[] m_ShaderProperties;

    void OnEnable()
    {
        m_TimeProperties = new SerializedProperty[]
        {
            serializedObject.FindProperty("m_FixedTimeStep"),
            serializedObject.FindProperty("m_MaxDeltaTime")
        };

        m_ShaderProperties = new SerializedProperty[]
        {
            serializedObject.FindProperty("m_IndirectShader"),
            serializedObject.FindProperty("m_CopyBufferShader"),
            serializedObject.FindProperty("m_SortShader"),
            serializedObject.FindProperty("m_StripUpdateShader"),
        };

        CheckVFXManager();
        serializedObject.Update();
    }

    public override void OnInspectorGUI()
    {
        // trying to detect a C++ reset by checking if all shaders have been reset to null
        if (!m_ShaderProperties.Any(t => t != null && t.objectReferenceValue != null))
            CheckVFXManager();

        serializedObject.Update();

        EditorGUILayout.LabelField("Current Scriptable Render Pipeline: " + VFXLibrary.currentSRPBinder.SRPAssetTypeStr);

        foreach (var property in m_TimeProperties)
        {
            EditorGUILayout.PropertyField(property);
        }

        GUILayout.Space(15);

        foreach (var property in m_ShaderProperties)
        {
            if (property != null)
                EditorGUILayout.PropertyField(property);
        }
        serializedObject.ApplyModifiedProperties();
    }

    private static bool SetBuiltInShaderIfNeeded(SerializedObject obj, string shaderName, string shaderPath)
    {
        var shaderProperty = obj.FindProperty(shaderName);
        if (shaderProperty.objectReferenceValue == null)
        {
            var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(shaderPath);
            if (shader != null)
            {
                shaderProperty.objectReferenceValue = shader;
                return true;
            }
        }

        return false;
    }

    public static void CheckVFXManager()
    {
        UnityObject vfxmanager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/VFXManager.asset").FirstOrDefault();

        if (vfxmanager == null)
            return;

        SerializedObject obj = new SerializedObject(vfxmanager);
        bool shaderModified = false;

        shaderModified |= SetBuiltInShaderIfNeeded(obj, "m_IndirectShader",     "Packages/com.unity.visualeffectgraph/Shaders/VFXFillIndirectArgs.compute");
        shaderModified |= SetBuiltInShaderIfNeeded(obj, "m_CopyBufferShader",   "Packages/com.unity.visualeffectgraph/Shaders/VFXCopyBuffer.compute");
        shaderModified |= SetBuiltInShaderIfNeeded(obj, "m_SortShader",         "Packages/com.unity.visualeffectgraph/Shaders/Sort.compute");
        shaderModified |= SetBuiltInShaderIfNeeded(obj, "m_StripUpdateShader",  "Packages/com.unity.visualeffectgraph/Shaders/UpdateStrips.compute");

        if (shaderModified)
            obj.ApplyModifiedPropertiesWithoutUndo();
    }
}
