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


    const string HDRPPath = "Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/HDRP";
    const string LWRPPath = "Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/LWRP";
#if VFX_HAS_HDRP
    const string RPPath = HDRPPath;
#elif VFX_HAS_LWRP
    const string RPPath = LWRPPath;
#else
    const string RPPath = "";
#endif


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


    static string GetDefaultPath()
    {
#if VFX_HAS_HDRP && VFX_HAS_LWRP
        if(GraphicsSettings.renderPipelineAsset != null && GraphicsSettings.renderPipelineAsset.GetType().Name == "LightweightRenderPipelineAsset")
            return LWRPPath;
        else
            return HDRPPath;
#else
        return RPPath;
#endif
    }

    public override void OnInspectorGUI()
    {
        // trying to detect a C++ reset by checking if all shaders have been reset to null and the path to ""
        if( string.IsNullOrEmpty(m_PathProperty.stringValue) && ! m_ShaderProperties.Any(t=>t.objectReferenceValue != null))
            CheckVFXManager();
        serializedObject.Update();
        bool recompile = false;
        
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(m_PathProperty.name));
        string resultPath = EditorGUILayout.DelayedTextField(m_PathProperty.stringValue);
        if (EditorGUI.EndChangeCheck())
        {
            m_PathProperty.stringValue = resultPath;
            recompile = true;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Default"))
        {
            string newPath = GetDefaultPath();
            if(m_PathProperty.stringValue != newPath)
            {
                m_PathProperty.stringValue = newPath;
                recompile = true;
            }
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
        if( recompile)
        {
            VFXCacheManager.Build();
            EditorGUIUtility.ExitGUI();
        }
    }

    public static void CheckVFXManager()
    {
        UnityObject vfxmanager = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/VFXManager.asset");
        if (vfxmanager == null)
            return;

        SerializedObject obj = new SerializedObject(vfxmanager);

        var pathProperty = obj.FindProperty("m_RenderPipeSettingsPath");
        bool recompile = false;
        if (string.IsNullOrEmpty(pathProperty.stringValue))
        {
            pathProperty.stringValue = GetDefaultPath();
            recompile = true;
        }

#if !VFX_HAS_HDRP
         if(pathProperty.stringValue == HDRPPath)
        {
            pathProperty.stringValue = GetDefaultPath();
            recompile = true;
        }
#endif
#if !VFX_HAS_LWRP
        if (pathProperty.stringValue == LWRPPath)
        {
            pathProperty.stringValue = GetDefaultPath();
            recompile = true;
        }
#endif

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
        if (recompile)
            VFXCacheManager.Build();
    }
}
