using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class HDEditorUtils
    {
        delegate void MaterialResetter(Material material);
        static Dictionary<string, MaterialResetter> k_MaterialResetters = new Dictionary<string, MaterialResetter>()
        {
            { "HDRenderPipeline/LayeredLit",  LayeredLitGUI.SetupMaterialKeywordsAndPass },
            { "HDRenderPipeline/LayeredLitTessellation", LayeredLitGUI.SetupMaterialKeywordsAndPass },
            { "HDRenderPipeline/Lit", LitGUI.SetupMaterialKeywordsAndPass },
            { "HDRenderPipeline/LitTessellation", LitGUI.SetupMaterialKeywordsAndPass },
            { "HDRenderPipeline/Unlit", UnlitGUI.SetupMaterialKeywordsAndPass }
        };

        public static string GetHDRenderPipelinePath()
        {
            // User can create their own directory for SRP, so we need to find the current path that they use.
            // We know that DefaultHDMaterial exist and we know where it is, let's use that to find the current directory.
            var guid = AssetDatabase.FindAssets("DefaultHDMaterial t:material");
            string path = AssetDatabase.GUIDToAssetPath(guid[0]);
            path = Path.GetDirectoryName(path); // Asset is in HDRenderPipeline/RenderPipelineResources/DefaultHDMaterial.mat
            path = path.Replace("RenderPipelineResources", ""); // Keep only path with HDRenderPipeline

            return path;
        }

        // TODO: The two following functions depend on HDRP, they should be made generic
        public static string GetPostProcessingPath()
        {
            var hdrpPath = GetHDRenderPipelinePath();
            var fullPath = Path.GetFullPath(hdrpPath + "../../PostProcessing/PostProcessing");
            var relativePath = fullPath.Substring(fullPath.IndexOf("Assets"));
            return relativePath.Replace("\\", "/") + "/";
        }

        public static string GetCorePath()
        {
            var hdrpPath = GetHDRenderPipelinePath();
            var fullPath = Path.GetFullPath(hdrpPath + "../Core");
            var relativePath = fullPath.Substring(fullPath.IndexOf("Assets"));
            return relativePath.Replace("\\", "/") + "/";
        }

        public static bool ResetMaterialKeywords(Material material)
        {
            MaterialResetter resetter;
            if (k_MaterialResetters.TryGetValue(material.shader.name, out resetter))
            {
                RemoveMaterialKeywords(material);
                resetter(material);
                EditorUtility.SetDirty(material);
                return true;
            }
            return false;
        }

        public static void RemoveMaterialKeywords(Material material)
        {
            material.shaderKeywords = null;
        }
    }
}
