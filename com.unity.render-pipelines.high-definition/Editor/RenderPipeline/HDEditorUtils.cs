using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class HDEditorUtils
    {
        delegate void MaterialResetter(Material material);
        static Dictionary<string, MaterialResetter> k_MaterialResetters = new Dictionary<string, MaterialResetter>()
        {
            { "HDRP/LayeredLit",  LayeredLitGUI.SetupMaterialKeywordsAndPass },
            { "HDRP/LayeredLitTessellation", LayeredLitGUI.SetupMaterialKeywordsAndPass },
            { "HDRP/Lit", LitGUI.SetupMaterialKeywordsAndPass },
            { "HDRP/LitTessellation", LitGUI.SetupMaterialKeywordsAndPass },
            { "HDRP/Unlit", UnlitGUI.SetupMaterialKeywordsAndPass },
            { "HDRP/Decal", DecalUI.SetupMaterialKeywordsAndPass },
            { "HDRP/TerrainLit", TerrainLitGUI.SetupMaterialKeywordsAndPass }
        };

        public static T LoadAsset<T>(string relativePath) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(HDUtils.GetHDRenderPipelinePath() + relativePath);
        }

        public static bool ResetMaterialKeywords(Material material)
        {
            MaterialResetter resetter;
            if (k_MaterialResetters.TryGetValue(material.shader.name, out resetter))
            {
                CoreEditorUtils.RemoveMaterialKeywords(material);
                // We need to reapply ToggleOff/Toggle keyword after reset via ApplyMaterialPropertyDrawers
                MaterialEditor.ApplyMaterialPropertyDrawers(material);
                resetter(material);
                EditorUtility.SetDirty(material);
                return true;
            }
            return false;
        }

        public static List<BaseShaderPreprocessor> GetBaseShaderPreprocessorList()
        {
            var baseType = typeof(BaseShaderPreprocessor);
            var assembly = baseType.Assembly;

            var types = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(baseType))
                .Select(Activator.CreateInstance)
                .Cast<BaseShaderPreprocessor>()
                .ToList();

            return types;
        }
    }
}
