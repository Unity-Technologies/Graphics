using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        // Note: This function returns null if used with a package setup.
        static string GetScriptableRenderPipelinePath()
        {
            // User can create their own directory for SRP, so we need to find the current path that they use.
            // We do that using the SRPMARKER file.
            var srpMarkerPath = Directory.GetFiles(Application.dataPath, "SRPMARKER", SearchOption.AllDirectories).FirstOrDefault();
            if (srpMarkerPath != null)
                return new Uri(Application.dataPath).MakeRelativeUri(new Uri(Directory.GetParent(srpMarkerPath).ToString())).ToString();
            return null;
        }

        public static string GetHDRenderPipelinePath()
        {
            var srpPath = GetScriptableRenderPipelinePath();
            if (srpPath != null)
                return Path.Combine(srpPath, "ScriptableRenderPipeline/HDRenderPipeline/HDRP/");
            // If the SRPMARKER is not found, we assume that a package setup is used.
            return "Packages/com.unity.render-pipelines.high-definition/HDRP/";
        }

        public static string GetPostProcessingPath()
        {
            var srpPath = GetScriptableRenderPipelinePath();
            if (srpPath != null)
                return Path.Combine(srpPath, "PostProcessing/PostProcessing/");
            // If the SRPMARKER is not found, we assume that a package setup is used.
            return "Packages/com.unity.postprocessing/";
        }

        public static string GetCorePath()
        {
            var srpPath = GetScriptableRenderPipelinePath();
            if (srpPath != null)
                return Path.Combine(srpPath, "ScriptableRenderPipeline/Core/CoreRP/");
            // If the SRPMARKER is not found, we assume that a package setup is used.
            return "Packages/com.unity.render-pipelines.core/CoreRP/";
        }

        public static bool ResetMaterialKeywords(Material material)
        {
            MaterialResetter resetter;
            if (k_MaterialResetters.TryGetValue(material.shader.name, out resetter))
            {
                CoreEditorUtils.RemoveMaterialKeywords(material);
                resetter(material);
                EditorUtility.SetDirty(material);
                return true;
            }
            return false;
        }
    }
}
