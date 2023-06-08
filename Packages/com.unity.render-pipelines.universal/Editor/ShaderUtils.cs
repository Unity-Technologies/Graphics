using System;
using UnityEngine;
using UnityEditor;
using ShaderPathID = UnityEngine.Rendering.Universal.ShaderPathID;
using UnityEditor.ShaderGraph;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.Rendering.Universal.ShaderGUI;

namespace Unity.Rendering.Universal
{
    /// <summary>
    /// Various utility functions for shaders in URP.
    /// </summary>
    public static class ShaderUtils
    {
        internal enum ShaderID
        {
            Unknown = -1,

            Lit = ShaderPathID.Lit,
            SimpleLit = ShaderPathID.SimpleLit,
            Unlit = ShaderPathID.Unlit,
            TerrainLit = ShaderPathID.TerrainLit,
            ParticlesLit = ShaderPathID.ParticlesLit,
            ParticlesSimpleLit = ShaderPathID.ParticlesSimpleLit,
            ParticlesUnlit = ShaderPathID.ParticlesUnlit,
            BakedLit = ShaderPathID.BakedLit,
            SpeedTree7 = ShaderPathID.SpeedTree7,
            SpeedTree7Billboard = ShaderPathID.SpeedTree7Billboard,
            SpeedTree8 = ShaderPathID.SpeedTree8,
            ComplexLit = ShaderPathID.ComplexLit,

            // ShaderGraph IDs start at 1000, correspond to subtargets
            SG_Unlit = 1000,        // UniversalUnlitSubTarget
            SG_Lit,                 // UniversalLitSubTarget
            SG_Decal,               // UniversalDecalSubTarget
            SG_SpriteUnlit,         // UniversalSpriteUnlitSubTarget
            SG_SpriteLit,           // UniversalSpriteLitSubTarget
            SG_SpriteCustomLit,      // UniversalSpriteCustomLitSubTarget
            SG_SixWaySmokeLit       // UniversalSixWaySubTarget
        }

        internal static bool IsShaderGraph(this ShaderID id)
        {
            return ((int)id >= 1000);
        }

        // NOTE: this won't work for non-Asset shaders... (i.e. shadergraph preview shaders)
        internal static ShaderID GetShaderID(Shader shader)
        {
            if (shader.IsShaderGraphAsset())
            {
                UniversalMetadata meta;
                if (!shader.TryGetMetadataOfType<UniversalMetadata>(out meta))
                    return ShaderID.Unknown;
                return meta.shaderID;
            }
            else
            {
                ShaderPathID pathID = UnityEngine.Rendering.Universal.ShaderUtils.GetEnumFromPath(shader.name);
                return (ShaderID)pathID;
            }
        }

        internal static bool HasMotionVectorLightModeTag(ShaderID id)
        {
            // Currently only these ShaderIDs have a pass with a { "LightMode" = "MotionVectors" } tag in URP
            // (this is a more efficient check than looping over all sub-shaders and their passes and checking the
            // "LightMode" tag value with FindPassTagValue)
            switch(id)
            {
                case ShaderID.Lit:
                case ShaderID.Unlit:
                case ShaderID.SimpleLit:
                case ShaderID.ComplexLit:
                case ShaderID.BakedLit:
                case ShaderID.SG_Unlit:
                case ShaderID.SG_Lit:
                    return true;
            }

            return false;
        }

        internal enum MaterialUpdateType
        {
            CreatedNewMaterial,
            ChangedAssignedShader,
            ModifiedShader,
            ModifiedMaterial
        }

        //Helper used by VFX, allow retrieval of ShaderID on another object than material.shader
        //In case of ShaderGraph integration, the material.shader is actually pointing to VisualEffectAsset
        internal static void UpdateMaterial(Material material, MaterialUpdateType updateType, UnityEngine.Object assetWithURPMetaData)
        {
            var currentShaderId = ShaderUtils.ShaderID.Unknown;
            if (assetWithURPMetaData != null)
            {
                var path = AssetDatabase.GetAssetPath(assetWithURPMetaData);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is UniversalMetadata metadataAsset)
                    {
                        currentShaderId = metadataAsset.shaderID;
                        break;
                    }
                }
            }
            UpdateMaterial(material, updateType, currentShaderId);
        }

        // this is used to update a material's keywords, applying any shader-associated logic to update dependent properties and keywords
        // this is also invoked when a material is created, modified, or the material's shader is modified or reassigned
        internal static void UpdateMaterial(Material material, MaterialUpdateType updateType, ShaderID shaderID = ShaderID.Unknown)
        {
            // if unknown, look it up from the material's shader
            // NOTE: this will only work for asset-based shaders..
            if (shaderID == ShaderID.Unknown)
                shaderID = GetShaderID(material.shader);

            switch (shaderID)
            {
                case ShaderID.Lit:
                    LitShader.SetMaterialKeywords(material, LitGUI.SetMaterialKeywords);
                    break;
                case ShaderID.SimpleLit:
                    SimpleLitShader.SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords);
                    break;
                case ShaderID.Unlit:
                    UnlitShader.SetMaterialKeywords(material);
                    break;
                case ShaderID.ParticlesLit:
                    ParticlesLitShader.SetMaterialKeywords(material, LitGUI.SetMaterialKeywords, ParticleGUI.SetMaterialKeywords);
                    break;
                case ShaderID.ParticlesSimpleLit:
                    ParticlesSimpleLitShader.SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords, ParticleGUI.SetMaterialKeywords);
                    break;
                case ShaderID.ParticlesUnlit:
                    ParticlesUnlitShader.SetMaterialKeywords(material, null, ParticleGUI.SetMaterialKeywords);
                    break;
                case ShaderID.SpeedTree8:
                    ShaderGraphLitGUI.UpdateMaterial(material, updateType);
                    break;
                case ShaderID.SG_Lit:
                    ShaderGraphLitGUI.UpdateMaterial(material, updateType);
                    break;
                case ShaderID.SG_Unlit:
                    ShaderGraphUnlitGUI.UpdateMaterial(material, updateType);
                    break;
                case ShaderID.SG_Decal:
                    break;
                case ShaderID.SG_SpriteUnlit:
                    break;
                case ShaderID.SG_SpriteLit:
                    break;
                case ShaderID.SG_SpriteCustomLit:
                    break;
                case ShaderID.SG_SixWaySmokeLit:
                    ShaderGraphLitGUI.UpdateMaterial(material, updateType);
                    SixWayGUI.UpdateSixWayKeywords(material);
                    break;
                default:
                    break;
            }
        }
    }
}
