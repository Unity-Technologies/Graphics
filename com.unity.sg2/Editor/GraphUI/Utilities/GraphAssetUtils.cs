using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public static class GraphAssetUtils
    {
        public class CreateGraphAssetAction : ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                ShaderGraphAsset.HandleCreate(pathName);
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pathName);
                Selection.activeObject = obj;
            }
        }

        public class CreateSubGraphAssetAction : ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                ShaderSubGraphAsset.HandleCreate(pathName);
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pathName);
                Selection.activeObject = obj;
            }
        }

        [MenuItem("Assets/Create/Shader Graph 2/Blank Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateBlankGraphInProjectWindow()
        {
            var newGraphAction = ScriptableObject.CreateInstance<CreateGraphAssetAction>();

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                newGraphAction,
                $"{ShaderGraphStencil.DefaultAssetName}.{ShaderGraphStencil.GraphExtension}",
                null,
                null);
        }

        [MenuItem("Assets/Create/Shader Graph 2/Blank Shader SubGraph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateBlankSubGraphInProjectWindow()
        {
            var newGraphAction = ScriptableObject.CreateInstance<CreateSubGraphAssetAction>();

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                newGraphAction,
                $"{ShaderGraphStencil.DefaultAssetName}.{ShaderGraphStencil.SubGraphExtension}",
                null,
                null);
        }

        private static void SaveImplementation(BaseGraphTool GraphTool, Action<string, ShaderGraphAssetModel> SaveAction)
        {
            // If no currently opened graph, early out
            if (GraphTool.ToolState.CurrentGraph.GetGraphAsset() == null)
                return;
            if (GraphTool.ToolState.CurrentGraph.GetGraphAsset() is ShaderGraphAssetModel assetModel)
            {
                var assetPath = GraphTool.ToolState.CurrentGraph.GetGraphAssetPath();
                SaveAction(assetPath, assetModel);

                // Set to false after saving to clear modification state from editor window tab
                assetModel.Dirty = false;
            }
        }

        private static string SaveAsImplementation(BaseGraphTool GraphTool, Action<string, ShaderGraphAssetModel> SaveAction, string extension)
        {
            // If no currently opened graph, early out
            if (GraphTool.ToolState.CurrentGraph.GetGraphAsset() == null)
                return String.Empty;

            if (GraphTool.ToolState.CurrentGraph.GetGraphAsset() is ShaderGraphAssetModel assetModel)
            {
                // Get folder of current shader graph asset
                var path = GraphTool.ToolState.CurrentGraph.GetGraphAssetPath();
                path = path.Remove(path.LastIndexOf('/'));

                var destinationPath = EditorUtility.SaveFilePanel("Save Shader Graph Asset at: ", path, GraphTool.ToolState.CurrentGraph.GetGraphAsset().Name, extension);
                // If User cancelled operation or provided an invalid path
                if (string.IsNullOrEmpty(destinationPath))
                    return string.Empty;

                SaveAction(destinationPath, assetModel);

                // Refresh asset database so newly saved asset shows up
                AssetDatabase.Refresh();

                return destinationPath;
            }

            return string.Empty;
        }

        public static void SaveGraphImplementation(BaseGraphTool GraphTool) => SaveImplementation(GraphTool, ShaderGraphAsset.HandleSave);
        public static void SaveSubGraphImplementation(BaseGraphTool GraphTool) => SaveImplementation(GraphTool, ShaderSubGraphAsset.HandleSave);

        public static string SaveAsGraphImplementation(BaseGraphTool GraphTool) => SaveAsImplementation(GraphTool, ShaderGraphAsset.HandleSave, ShaderGraphStencil.GraphExtension);
        public static string SaveAsSubgraphImplementation(BaseGraphTool GraphTool) => SaveAsImplementation(GraphTool, ShaderGraphAsset.HandleSave, ShaderGraphStencil.SubGraphExtension);
    }
}
