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
using VFXManager = UnityEngine.VFX.VFXManager;

[CustomEditor(typeof(UnityEditor.VFXManager))]
class VFXManagerEditor : Editor
{
    SerializedProperty[] m_TimeProperties;
    SerializedProperty[] m_ShaderProperties;
    SerializedProperty m_RuntimeResourcesProperty;

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

        m_RuntimeResourcesProperty = serializedObject.FindProperty("m_RuntimeResources");

        CheckVFXManager();
        serializedObject.Update();
    }

    public override void OnInspectorGUI()
    {
        // trying to detect a C++ reset by checking if all shaders have been reset to null
        if (!m_ShaderProperties.Any(t => t != null && t.objectReferenceValue != null))
            CheckVFXManager();

        serializedObject.Update();

        GUI.enabled = AssetDatabase.IsOpenForEdit(target, StatusQueryOptions.UseCachedIfPossible);

        EditorGUILayout.LabelField("Current Scriptable Render Pipeline: " + VFXLibrary.currentSRPBinder?.SRPAssetTypeStr);

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

        if (m_RuntimeResourcesProperty != null)
        {
            EditorGUILayout.PropertyField(m_RuntimeResourcesProperty);
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

    private static bool SetRuntimeResourcesIfNeeded(SerializedObject obj)
    {
        var resourcesProperty = obj.FindProperty("m_RuntimeResources");
        if (resourcesProperty == null)
        {
            return false;
        }
        if (resourcesProperty.objectReferenceValue == null)
        {
            var runtimeResources = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Packages/com.unity.visualeffectgraph/Runtime/Utilities/RuntimeResources.asset");
            if (runtimeResources != null)
            {
                resourcesProperty.objectReferenceValue = runtimeResources;
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

        if (!AssetDatabase.IsOpenForEdit(vfxmanager, StatusQueryOptions.UseCachedIfPossible))
            return;

        SerializedObject obj = new SerializedObject(vfxmanager);
        bool shaderModified = false;

        shaderModified |= SetBuiltInShaderIfNeeded(obj, "m_IndirectShader", "Packages/com.unity.visualeffectgraph/Shaders/VFXFillIndirectArgs.compute");
        shaderModified |= SetBuiltInShaderIfNeeded(obj, "m_CopyBufferShader", "Packages/com.unity.visualeffectgraph/Shaders/VFXCopyBuffer.compute");
        shaderModified |= SetBuiltInShaderIfNeeded(obj, "m_SortShader", "Packages/com.unity.visualeffectgraph/Shaders/Sort.compute");
        shaderModified |= SetBuiltInShaderIfNeeded(obj, "m_StripUpdateShader", "Packages/com.unity.visualeffectgraph/Shaders/UpdateStrips.compute");
        bool runtimeResourcesModified = false;
        runtimeResourcesModified = SetRuntimeResourcesIfNeeded(obj);

        if (shaderModified || runtimeResourcesModified)
            obj.ApplyModifiedPropertiesWithoutUndo();
    }
}
