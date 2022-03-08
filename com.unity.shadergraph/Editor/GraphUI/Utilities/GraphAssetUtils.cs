using System;
using System.IO;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphAssetUtils
    {
        [MenuItem("Assets/Create/Shader Graph/Blank Shader Graph", priority = CoreUtils.Sections.section1 +  CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateBlankGraphInProjectWindow()
        {
            var newGraphAction = ScriptableObject.CreateInstance<NewGraphAction>();

            var path = "";
            var obj = Selection.activeObject;
            path = obj == null ? "Assets" : AssetDatabase.GetAssetPath(obj.GetInstanceID());

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                newGraphAction,
                $"{ShaderGraphStencil.DefaultAssetName}.{ShaderGraphStencil.Extension}",
                null,
                null);
        }

        public static Object CreateShaderGraphAsset(string assetPath, ShaderGraphAssetModel assetModel = null)
        {
            //assetPath = SanitizeNewAssetPath(assetPath);
            var assetHelper = assetModel == null ? ShaderGraphAssetHelper.MakeDefault() : ShaderGraphAssetHelper.MakeFromModel(assetModel);
            File.WriteAllText(assetPath, ShaderGraphAssetHelper.ToJson(assetHelper));
            AssetDatabase.ImportAsset(assetPath);
            return AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        }

        public static string SanitizeNewAssetPath(string inputPath)
        {
            if (!AssetDatabase.IsValidFolder(inputPath) || String.IsNullOrEmpty(inputPath))
                throw new ArgumentException("Provided path is not valid.");

            if(!inputPath.Contains(ShaderGraphStencil.Extension))
                throw new ArgumentException("Provided path is not valid.");

            return inputPath;
        }
    }
}
