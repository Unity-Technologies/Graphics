
using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public static class GraphAssetUtils
    {
        public class CreateAssetAction : ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                ShaderGraphAsset.HandleCreate(pathName);
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pathName);
                Selection.activeObject = obj;
            }
        }

        [MenuItem("Assets/Create/Shader Graph 2/Blank Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateBlankGraphInProjectWindow()
        {
            var newGraphAction = ScriptableObject.CreateInstance<CreateAssetAction>();

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                newGraphAction,
                $"{ShaderGraphStencil.DefaultAssetName}.{ShaderGraphStencil.Extension}",
                null,
                null);
        }

        public static void SaveImplementation(BaseGraphTool GraphTool)
        {
            // If no currently opened graph, early out
            if (GraphTool.ToolState.AssetModel == null)
                return;
            if (GraphTool.ToolState.AssetModel is ShaderGraphAssetModel assetModel)
            {
                var assetPath = GraphTool.ToolState.CurrentGraph.GetGraphAssetModelPath();
                ShaderGraphAsset.HandleSave(assetPath, assetModel);
                // Set to false after saving to clear modification state from editor window tab
                assetModel.Dirty = false;
            }
        }

        public static string SaveAsImplementation(BaseGraphTool GraphTool)
        {
            // If no currently opened graph, early out
            if (GraphTool.ToolState.AssetModel == null)
                return String.Empty;

            if (GraphTool.ToolState.AssetModel is ShaderGraphAssetModel assetModel)
            {
                // Get folder of current shader graph asset
                var path = GraphTool.ToolState.CurrentGraph.GetGraphAssetModelPath();
                path = path.Remove(path.LastIndexOf('/'));

                var destinationPath = EditorUtility.SaveFilePanel("Save Shader Graph Asset at: ", path, GraphTool.ToolState.CurrentGraph.GetGraphAssetModel().Name, "sg2");
                // If User cancelled operation or provided an invalid path
                if (destinationPath == String.Empty)
                    return String.Empty;

                ShaderGraphAsset.HandleSave(destinationPath, assetModel);
                // Refresh asset database so newly saved asset shows up
                AssetDatabase.Refresh();

                return destinationPath;
            }

            return String.Empty;
        }

    }
}
