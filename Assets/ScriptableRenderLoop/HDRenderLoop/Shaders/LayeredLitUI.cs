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
        public readonly GUIContent layerMapMask = new GUIContent("Layer Mask", "Layer mask (multiplied by vertex color)");
        public readonly GUIContent layerCount = new GUIContent("Layer Count", "Number of layers.");
    }

    static Styles s_Styles = null;
    private static Styles styles { get { if (s_Styles == null) s_Styles = new Styles(); return s_Styles; } }

    // Needed for json serialization to work
    [Serializable]
    internal struct SerializeableGUIDs
    {
        public string[] GUIDArray;
    }

    private const int kMaxLayerCount = 4;
    private const int kSyncButtonWidth = 58;
    private string kLayerMaskMap = "_LayerMaskMap";
    private string kLayerCount = "_LayerCount";

    private Material[] m_MaterialLayers = new Material[kMaxLayerCount];

    MaterialProperty layerCountProperty = null;
    MaterialProperty layerMaskMapProperty = null;

    int layerCount
    {
        set { layerCountProperty.floatValue = (float)value; }
        get { return (int)layerCountProperty.floatValue; }
    }

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
                    ShaderUtil.ShaderPropertyType type = ShaderUtil.GetPropertyType(layerShader, i);
                    switch (type)
                    {
                        case ShaderUtil.ShaderPropertyType.Color:
                            {
                                material.SetColor(layerPropertyName, layerMaterial.GetColor(propertyName));
                                break;
                            }
                        case ShaderUtil.ShaderPropertyType.Float:
                        case ShaderUtil.ShaderPropertyType.Range:
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

    bool CheckInputOptionConsistency(string optionName, string[] shortNames, ref string outValueNames)
    {
        bool result = true;
        outValueNames = "";
        for(int i = 0 ; i < layerCount ; ++i)
        {
            Material layer = m_MaterialLayers[i];
            if (layer != null)
            {
                int currentValue = (int)layer.GetFloat(optionName); // All options are in fact enums
                Debug.Assert(currentValue < shortNames.Length);
                outValueNames += shortNames[currentValue] + "    ";

                for (int j = i + 1; j < layerCount; ++j)
                {
                    Material otherLayer = m_MaterialLayers[j];
                    if(otherLayer != null)
                    {
                        if(currentValue != (int)otherLayer.GetFloat(optionName))
                        {

                            result = false;
                        }
                    }
                }
            }
            else
            {
                outValueNames += "X    ";
            }
        }

        return result;
    }

    bool CheckInputMapConsistency(string mapName, ref string outValueNames)
    {
        bool result = true;
        outValueNames = "";
        for (int i = 0; i < layerCount; ++i)
        {
            Material layer = m_MaterialLayers[i];
            if (layer != null)
            {
                bool currentValue = layer.GetTexture(mapName) != null;
                outValueNames += (currentValue ? "Y" : "N") + "    ";

                for (int j = i + 1; j < layerCount; ++j)
                {
                    Material otherLayer = m_MaterialLayers[j];
                    if (otherLayer != null)
                    {
                        bool otherValue = otherLayer.GetTexture(mapName) != null;
                        if (currentValue != otherValue)
                        {
                            result = false;
                        }
                    }
                }
            }
            else
            {
                outValueNames += "N    ";
            }
        }

        return result;
    }

    void CheckLayerConsistency()
    {
        string optionValueNames = "";
        // We need to check consistency between all layers.
        // Each input options and each input maps might can result in different #defines in the shader so all of them need to be consistent
        // otherwise the result will be undetermined

        // Input options consistency
        string[] smoothnessSourceShortNames = { "Mask", "Albedo" };
        string[] emissiveModeShortNames = { "Color", "Mask" };
        string[] normalMapShortNames = { "Tan", "Obj" };
        string[] heightMapShortNames = { "Parallax", "Disp" };

        string warningInputOptions = "";
        if (!CheckInputOptionConsistency(kSmoothnessTextureChannelProp, smoothnessSourceShortNames, ref optionValueNames))
        {
            warningInputOptions += "Smoothness Source:    " + optionValueNames + "\n";
        }
        if (!CheckInputOptionConsistency(kEmissiveColorMode, emissiveModeShortNames, ref optionValueNames))
        {
            warningInputOptions += "Emissive Mode:    " + optionValueNames + "\n";
        }
        if (!CheckInputOptionConsistency(kNormalMapSpace, normalMapShortNames, ref optionValueNames))
        {
            warningInputOptions += "Normal Map Space:    " + optionValueNames + "\n";
        }
        if (!CheckInputOptionConsistency(kHeightMapMode, heightMapShortNames, ref optionValueNames))
        {
            warningInputOptions += "Height Map Mode:    " + optionValueNames + "\n";
        }

        if (warningInputOptions != string.Empty)
        {
            warningInputOptions = "Input Option Consistency Error:\n" + warningInputOptions;
        }

        // Check input maps consistency
        string warningInputMaps = "";

        if (!CheckInputMapConsistency(kNormalMap, ref optionValueNames))
        {
            warningInputMaps += "Normal Map:    " + optionValueNames + "\n";
        }
        if (!CheckInputMapConsistency(kMaskMap, ref optionValueNames))
        {
            warningInputMaps += "Mask Map:    " + optionValueNames + "\n";
        }
        if (!CheckInputMapConsistency(kspecularOcclusionMap, ref optionValueNames))
        {
            warningInputMaps += "Specular Occlusion Map:    " + optionValueNames + "\n";
        }
        if (!CheckInputMapConsistency(kEmissiveColorMap, ref optionValueNames))
        {
            warningInputMaps += "Emissive Color Map:    " + optionValueNames + "\n";
        }
        if (!CheckInputMapConsistency(kHeightMap, ref optionValueNames))
        {
            warningInputMaps += "Height Map:    " + optionValueNames + "\n";
        }

        if (warningInputMaps != string.Empty)
        {
            warningInputMaps = "Input Maps Consistency Error:\n" + warningInputMaps;
            if (warningInputOptions != string.Empty)
                warningInputMaps = "\n" + warningInputMaps;
        }

        string warning = warningInputOptions + warningInputMaps;
        if (warning != string.Empty)
        {
            EditorGUILayout.HelpBox(warning, MessageType.Error);
        }
    }

    void SynchronizeInputOptions()
    {
        Material material = m_MaterialEditor.target as Material;

        // We synchronize input options with the firsts non null Layer (all layers should have consistent options)
        Material firstLayer = null;
        int i = 0;
        while (i < layerCount && !(firstLayer = m_MaterialLayers[i])) ++i;

        if(firstLayer != null)
        {
            material.SetFloat(kSmoothnessTextureChannelProp, firstLayer.GetFloat(kSmoothnessTextureChannelProp));
            material.SetFloat(kEmissiveColorMode, firstLayer.GetFloat(kEmissiveColorMode));
            material.SetFloat(kNormalMapSpace, firstLayer.GetFloat(kNormalMapSpace));
            material.SetFloat(kHeightMapMode, firstLayer.GetFloat(kHeightMapMode));
        }
    }

    bool LayersGUI(AssetImporter materialImporter)
    {
        Material material = m_MaterialEditor.target as Material;

        bool layerChanged = false;

        EditorGUI.indentLevel++;
        GUILayout.Label(styles.layers, EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        int newLayerCount = EditorGUILayout.IntSlider(styles.layerCount, (int)layerCountProperty.floatValue, 2, 4 );
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(material, "Change layer count");
            layerCountProperty.floatValue = (float)newLayerCount;
            layerChanged = true;
        }

        m_MaterialEditor.TexturePropertySingleLine(styles.layerMapMask, layerMaskMapProperty);

        for (int i = 0; i < layerCount; i++)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUI.BeginChangeCheck();
                m_MaterialLayers[i] = EditorGUILayout.ObjectField(styles.materialLayerLabels[i], m_MaterialLayers[i], typeof(Material), true) as Material;
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(materialImporter, "Change layer material");
                    SynchronizeLayerProperties(i);
                    layerChanged = true;
                }

                if (GUILayout.Button(styles.syncButton, GUILayout.Width(kSyncButtonWidth)))
                {
                    SynchronizeLayerProperties(i);
                    layerChanged = true;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUI.indentLevel--;

        return layerChanged;
    }
    protected override void SetupKeywordsForInputMaps(Material material)
    {
        // Find first non null layer
        int i = 0;
        while (i < layerCount && (m_MaterialLayers[i] == null)) ++i;

        if (i < layerCount)
        {
            SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap + i));
            SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap + i));
            SetKeyword(material, "_SPECULAROCCLUSIONMAP", material.GetTexture(kspecularOcclusionMap + i));
            SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap + i));
            SetKeyword(material, "_HEIGHTMAP", material.GetTexture(kHeightMap + i));
        }

        SetKeyword(material, "_LAYERMASKMAP", material.GetTexture(kLayerMaskMap));
    }

    void SetupMaterialForLayers(Material material)
    {
        if(layerCount == 4)
        {
            SetKeyword(material, "_LAYEREDLIT_4_LAYERS", true);
            SetKeyword(material, "_LAYEREDLIT_3_LAYERS", false);
        }
        else if (layerCount == 3)
        {
            SetKeyword(material, "_LAYEREDLIT_4_LAYERS", false);
            SetKeyword(material, "_LAYEREDLIT_3_LAYERS", true);
        }
        else
        {
            SetKeyword(material, "_LAYEREDLIT_4_LAYERS", false);
            SetKeyword(material, "_LAYEREDLIT_3_LAYERS", false);
        }
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
    {
        FindOptionProperties(props);
        layerMaskMapProperty = FindProperty(kLayerMaskMap, props);
        layerCountProperty = FindProperty(kLayerCount, props);

        m_MaterialEditor = materialEditor;

        m_MaterialEditor.serializedObject.Update();

        Material material = m_MaterialEditor.target as Material;
        AssetImporter materialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));

        InitializeMaterialLayers(materialImporter);

        bool optionsChanged = false;
        EditorGUI.BeginChangeCheck();
        {
            ShaderOptionsGUI();
            EditorGUILayout.Space();
        }
        if (EditorGUI.EndChangeCheck())
        {
            optionsChanged = true;
        }

        bool layerChanged = LayersGUI(materialImporter);

        CheckLayerConsistency();

        if (layerChanged || optionsChanged)
        {
            SynchronizeInputOptions();

            foreach (var obj in m_MaterialEditor.targets)
            {
                SetupMaterial((Material)obj);
                SetupMaterialForLayers((Material)obj);
            }
            
            SaveMaterialLayers(materialImporter);
        }

        m_MaterialEditor.serializedObject.ApplyModifiedProperties();

        if (layerChanged)
        {
            materialImporter.SaveAndReimport();
        }
    }
}

} // namespace UnityEditor
