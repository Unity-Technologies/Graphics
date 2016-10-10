using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
internal class LayeredLitGUI : LitGUI
{
    private class Styles
    {
        public readonly GUIContent[] materialLayerLabels =
        {
                new GUIContent("Material Layer 0"),
                new GUIContent("Material Layer 1"),
                new GUIContent("Material Layer 2"),
                new GUIContent("Material Layer 3"),
        };
        public readonly GUIContent syncButton = new GUIContent("Sync", "Re-synchronize this layer's properties with the referenced Material");
        public readonly GUIContent layers = new GUIContent("Layers");
    }

    static Styles s_Styles = null;
    private static Styles styles { get { if (s_Styles == null) s_Styles = new Styles(); return s_Styles; } }

    // Needed for json serialization to work
    [Serializable]
    internal struct SerializeableGUIDs
    {
        public string[] GUIDArray;
    }

    private int kSyncButtonWidth = 58;   
    private Material[] m_MaterialLayers = new Material[4];

    void SynchronizeLayerProperties(int layerIndex)
    {
        Material material = m_MaterialEditor.target as Material;
        Material layerMaterial = m_MaterialLayers[layerIndex];

        if(layerMaterial != null)
        {
            Shader layerShader = layerMaterial.shader;
            int propertyCount = ShaderUtil.GetPropertyCount(layerShader);
            for(int i = 0 ; i < propertyCount ; ++i)
            {
                string propertyName = ShaderUtil.GetPropertyName(layerShader, i);
                string layerPropertyName = propertyName + layerIndex;
                if(material.HasProperty(layerPropertyName))
                {
                    switch(ShaderUtil.GetPropertyType(layerShader, i))
                    {
                        case ShaderUtil.ShaderPropertyType.Color:
                            {
                                material.SetColor(layerPropertyName, layerMaterial.GetColor(propertyName));
                                break;
                            }
                        case ShaderUtil.ShaderPropertyType.Float:
                            {
                                material.SetFloat(layerPropertyName, layerMaterial.GetFloat(propertyName));
                                break;
                            }
                        case ShaderUtil.ShaderPropertyType.Vector:
                            {
                                material.SetVector(layerPropertyName, layerMaterial.GetVector(propertyName));
                                break;
                            }
                        case ShaderUtil.ShaderPropertyType.TexEnv:
                            {
                                material.SetTexture(layerPropertyName, layerMaterial.GetTexture(propertyName));
                                break;
                            }
                    }
                }
            }
        }
    }

    void InitializeMaterialLayers(AssetImporter materialImporter)
    {
        if (materialImporter.userData != string.Empty)
        {
            SerializeableGUIDs layersGUID = JsonUtility.FromJson<SerializeableGUIDs>(materialImporter.userData);
            if (layersGUID.GUIDArray.Length > 0)
            {
                m_MaterialLayers = new Material[layersGUID.GUIDArray.Length];
                for (int i = 0; i < layersGUID.GUIDArray.Length; ++i)
                {
                    m_MaterialLayers[i] = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(layersGUID.GUIDArray[i]), typeof(Material)) as Material;
                }
            }
        }
    }

    void SaveMaterialLayers(AssetImporter materialImporter)
    {
        SerializeableGUIDs layersGUID;
        layersGUID.GUIDArray = new string[m_MaterialLayers.Length];
        for(int i = 0 ; i < m_MaterialLayers.Length; ++i)
        {
            if (m_MaterialLayers[i] != null)
                layersGUID.GUIDArray[i] = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_MaterialLayers[i].GetInstanceID()));

        }

        materialImporter.userData = JsonUtility.ToJson(layersGUID);
    }

    bool LayersGUI(AssetImporter materialImporter)
    {
        bool saveMaterialLayers = false;

        EditorGUI.indentLevel++;
        GUILayout.Label(styles.layers, EditorStyles.boldLabel);
        for (int i = 0; i < m_MaterialLayers.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUI.BeginChangeCheck();
                m_MaterialLayers[i] = EditorGUILayout.ObjectField(styles.materialLayerLabels[i], m_MaterialLayers[i], typeof(Material), true) as Material;
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(materialImporter, "Change layer material");
                    SynchronizeLayerProperties(i);
                    saveMaterialLayers = true;
                }

                if (GUILayout.Button(styles.syncButton, GUILayout.Width(kSyncButtonWidth)))
                {
                    SynchronizeLayerProperties(i);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        if (saveMaterialLayers)
        {
            SaveMaterialLayers(materialImporter);
        }

        EditorGUI.indentLevel--;

        return saveMaterialLayers;
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
    {
        FindOptionProperties(props);

        m_MaterialEditor = materialEditor;

        m_MaterialEditor.serializedObject.Update();

        Material material = materialEditor.target as Material;
        AssetImporter materialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));

        InitializeMaterialLayers(materialImporter);

        EditorGUI.BeginChangeCheck();
        {
            ShaderOptionsGUI();
            EditorGUILayout.Space();

            ShaderInputOptionsGUI();
            EditorGUILayout.Space();
        }
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var obj in m_MaterialEditor.targets)
                MaterialChanged((Material)obj);
        }

        bool saveMaterialLayers = LayersGUI(materialImporter);

        m_MaterialEditor.serializedObject.ApplyModifiedProperties();

        if (saveMaterialLayers)
        {
            materialImporter.SaveAndReimport();
        }
    }
}

} // namespace UnityEditor
