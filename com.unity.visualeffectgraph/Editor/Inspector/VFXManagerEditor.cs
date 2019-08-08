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
public class VFXManagerEditor : Editor
{
    SerializedProperty[] m_TimeProperties;
    SerializedProperty[] m_ShaderProperties;

    const string HDRPPath = "Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/HDRP";
    const string UniversalPath = "Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/Universal";
#if VFX_HAS_HDRP
    const string RPPath = HDRPPath;
#elif VFX_HAS_UNIVERSAL
    const string RPPath = UniversalPath;
#else
    const string RPPath = "";
#endif


    void OnEnable()
    {
        m_TimeProperties = new SerializedProperty[] {
            serializedObject.FindProperty("m_FixedTimeStep"),
            serializedObject.FindProperty("m_MaxDeltaTime")
        };

        m_ShaderProperties = new SerializedProperty[] {
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
        if(!m_ShaderProperties.Any(t => t != null && t.objectReferenceValue != null))
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

    public static void CheckVFXManager()
    {
        UnityObject vfxmanager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/VFXManager.asset").FirstOrDefault();
        if (vfxmanager == null)
            return;

        SerializedObject obj = new SerializedObject(vfxmanager);
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

        var updateStripProperty = obj.FindProperty("m_StripUpdateShader");
        if (updateStripProperty != null && updateStripProperty.objectReferenceValue == null)
        {
            updateStripProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.unity.visualeffectgraph/Shaders/UpdateStrips.compute");
        }

        obj.ApplyModifiedPropertiesWithoutUndo();
    }
}
