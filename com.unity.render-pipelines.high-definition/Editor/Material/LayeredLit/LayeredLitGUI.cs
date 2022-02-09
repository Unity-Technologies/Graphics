using System;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    // Needed for json serialization to work
    [Serializable]
    internal struct SerializeableGUIDs
    {
        public string[] GUIDArray;
        public bool[] withUV;
    }

    /// <summary>
    /// GUI for HDRP Layered Lit materials (and tesselation), does not include shader graph + function to setup material keywords for Lit
    /// </summary>
    class LayeredLitGUI : HDShaderGUI
    {
        const LitSurfaceInputsUIBlock.Features commonLitSurfaceInputsFeatures = LitSurfaceInputsUIBlock.Features.LayerOptions;

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.ExpandableBit.Base, 4, SurfaceOptionUIBlock.Features.Lit),
            new TessellationOptionsUIBlock(MaterialUIBlock.ExpandableBit.Tessellation),
            new LitSurfaceInputsUIBlock(MaterialUIBlock.ExpandableBit.Input, kMaxLayerCount, features: commonLitSurfaceInputsFeatures),
            new LayerListUIBlock(MaterialUIBlock.ExpandableBit.MaterialReferences),
            new LayersUIBlock(),
            new EmissionUIBlock(MaterialUIBlock.ExpandableBit.Emissive),
            new AdvancedOptionsUIBlock(MaterialUIBlock.ExpandableBit.Advance, features: AdvancedOptionsUIBlock.Features.StandardLit),
        };

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            uiBlocks.OnGUI(materialEditor, props);
        }

        public override void ValidateMaterial(Material material) => LayeredLitAPI.ValidateMaterial(material);

        // This function is call by a script to help artists to have up to date material
        // that why it is static
        public static void SynchronizeAllLayers(Material material)
        {
            int layerCount = (int)material.GetFloat(kLayerCount);
            AssetImporter materialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));

            Material[] layers = null;
            bool[] withUV = null;

            // Material importer can be null when the selected material doesn't exists as asset (Material saved inside the scene)
            if (materialImporter != null)
                InitializeMaterialLayers(material, ref layers, ref withUV);

            // We could have no userData in the assets, so test if we have load something
            if (layers != null && withUV != null)
            {
                for (int i = 0; i < layerCount; ++i)
                {
                    SynchronizeLayerProperties(material, i, layers[i], withUV[i]);
                }
            }
        }

        // This function will look for all referenced lit material, and assign value from Lit to layered lit layers.
        // This is based on the naming of the variables, i.E BaseColor will match BaseColor0, if a properties shouldn't be override
        // put the name in the exclusionList below
        public static void SynchronizeLayerProperties(Material material, int layerIndex, Material layerMaterial, bool includeUVMappingProperties)
        {
            string[] exclusionList = { kTexWorldScale, kUVBase, kUVMappingMask, kUVDetail, kUVDetailsMappingMask, kObjectSpaceUVMapping };

            if (layerMaterial != null)
            {
                Shader layerShader = layerMaterial.shader;
                int propertyCount = ShaderUtil.GetPropertyCount(layerShader);
                for (int i = 0; i < propertyCount; ++i)
                {
                    string propertyName = ShaderUtil.GetPropertyName(layerShader, i);
                    string layerPropertyName = propertyName + layerIndex;

                    if (includeUVMappingProperties || !exclusionList.Contains(propertyName))
                    {
                        if (material.HasProperty(layerPropertyName))
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
                                    if (includeUVMappingProperties)
                                    {
                                        material.SetTextureOffset(layerPropertyName, layerMaterial.GetTextureOffset(propertyName));
                                        material.SetTextureScale(layerPropertyName, layerMaterial.GetTextureScale(propertyName));
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        // We use the user data to save a string that represent the referenced lit material
        // so we can keep reference during serialization
        public static void InitializeMaterialLayers(Material material, ref Material[] layers, ref bool[] withUV)
        {
            AssetImporter materialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));
            InitializeMaterialLayers(materialImporter, ref layers, ref withUV);
        }

        public static void InitializeMaterialLayers(AssetImporter materialImporter, ref Material[] layers, ref bool[] withUV)
        {
            if (materialImporter.userData != string.Empty)
            {
                SerializeableGUIDs layersGUID = JsonUtility.FromJson<SerializeableGUIDs>(materialImporter.userData);
                if (layersGUID.GUIDArray.Length > 0)
                {
                    layers = new Material[layersGUID.GUIDArray.Length];
                    for (int i = 0; i < layersGUID.GUIDArray.Length; ++i)
                    {
                        layers[i] = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(layersGUID.GUIDArray[i]), typeof(Material)) as Material;
                    }
                }
                if (layersGUID.withUV != null && layersGUID.withUV.Length > 0)
                {
                    withUV = new bool[layersGUID.withUV.Length];
                    for (int i = 0; i < layersGUID.withUV.Length; ++i)
                        withUV[i] = layersGUID.withUV[i];
                }
            }
            else
            {
                if (layers != null)
                {
                    for (int i = 0; i < layers.Length; ++i)
                        layers[i] = null;
                }
                if (withUV != null)
                {
                    for (int i = 0; i < withUV.Length; ++i)
                        withUV[i] = true;
                }
            }
        }

        public static void SaveMaterialLayers(Material material, Material[] materialLayers, bool[] withUV)
        {
            AssetImporter materialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));

            SerializeableGUIDs layersGUID;
            layersGUID.GUIDArray = new string[materialLayers.Length];
            layersGUID.withUV = new bool[withUV.Length];
            for (int i = 0; i < materialLayers.Length; ++i)
            {
                if (materialLayers[i] != null)
                    layersGUID.GUIDArray[i] = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(materialLayers[i].GetInstanceID()));
                layersGUID.withUV[i] = withUV[i];
            }

            materialImporter.userData = JsonUtility.ToJson(layersGUID);
        }
    }
} // namespace UnityEditor
