using System;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public static class AssetUtils
    {
        internal class CreateAssetGraphAction : ProjectWindowCallback.EndNameEditAction
        {
            internal LegacyTargetType legacyTargetType = LegacyTargetType.Blank;

            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                ShaderGraphAssetUtils.CreateNewAssetGraph(pathName, legacyTargetType);
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pathName);
                Selection.activeObject = obj;
            }
        }

        [MenuItem("Assets/Create/Shader Graph 2/URP Lit Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateURPLitGraphInProjectWindow()
        {
            var newGraphAction = ScriptableObject.CreateInstance<CreateAssetGraphAction>();
            newGraphAction.legacyTargetType = LegacyTargetType.URPLit;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                newGraphAction,
                $"{ShaderGraphStencil.DefaultGraphAssetName}.{ShaderGraphStencil.GraphExtension}",
                null,
                null);
        }

        [MenuItem("Assets/Create/Shader Graph 2/URP Unlit Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateURPUnlitGraphInProjectWindow()
        {
            var newGraphAction = ScriptableObject.CreateInstance<CreateAssetGraphAction>();
            newGraphAction.legacyTargetType = LegacyTargetType.URPUnlit;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                newGraphAction,
                $"{ShaderGraphStencil.DefaultGraphAssetName}.{ShaderGraphStencil.GraphExtension}",
                null,
                null);
        }

        // TODO: Reenable for users once subgraphs are behaving correctly again https://jira.unity3d.com/browse/GSG-1290
        //[MenuItem("Assets/Create/Shader Graph 2/Blank Shader SubGraph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        //public static void CreateBlankSubGraphInProjectWindow()
        //{
        //    var newGraphAction = ScriptableObject.CreateInstance<CreateAssetGraphAction>();
        //    newGraphAction.isSubGraph = true;

        //    ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
        //        0,
        //        newGraphAction,
        //        $"{ShaderGraphStencil.DefaultSubGraphAssetName}.{ShaderGraphStencil.SubGraphExtension}",
        //        null,
        //        null);
        //}

        private static void SaveImplementation(BaseGraphTool GraphTool)
        {
            // If no currently opened graph, early out
            if (GraphTool.ToolState.CurrentGraph.GetGraphAsset() == null)
                return;
            if (GraphTool.ToolState.CurrentGraph.GetGraphAsset() is ShaderGraphAsset graphAsset)
            {
                var assetPath = GraphTool.ToolState.CurrentGraph.GetGraphAssetPath();

                // Set to false after saving to clear modification state from editor window tab
                graphAsset.Dirty = false;
            }
        }

        private static string SaveAsImplementation(BaseGraphTool GraphTool, string dialogTitle, string extension)
        {
            // If no currently opened graph, early out
            if (GraphTool.ToolState.CurrentGraph.GetGraphAsset() == null)
                return String.Empty;

            if (GraphTool.ToolState.CurrentGraph.GetGraphAsset() is ShaderGraphAsset assetModel)
            {
                // Get folder of current shader graph asset
                var path = GraphTool.ToolState.CurrentGraph.GetGraphAssetPath();
                path = path.Remove(path.LastIndexOf('/'));

                var destinationPath = EditorUtility.SaveFilePanel(dialogTitle, path, GraphTool.ToolState.CurrentGraph.GetGraphAsset().Name, extension);
                // If User cancelled operation or provided an invalid path
                if (string.IsNullOrEmpty(destinationPath))
                    return string.Empty;

                // Refresh asset database so newly saved asset shows up
                AssetDatabase.Refresh();

                return destinationPath;
            }

            return string.Empty;
        }

        static void SaveSubGraphImplementation(BaseGraphTool GraphTool) =>
            SaveImplementation(GraphTool);

        static string SaveAsGraphImplementation(BaseGraphTool GraphTool) =>
            SaveAsImplementation(GraphTool,
                "Save Shader Graph Asset at: ",
                ShaderGraphStencil.GraphExtension);
        static string SaveAsSubGraphImplementation(BaseGraphTool GraphTool) =>
            SaveAsImplementation(GraphTool,
                "Save Shader SubGraph Asset at:",
                ShaderGraphStencil.SubGraphExtension);

        /// <summary>
        /// Saves the graph *or* subgraph that is currently open in the given GraphTool.
        /// Does nothing if there is no open graph.
        /// </summary>
        /// <param name="graphTool">Graph tool with an open Shader Graph asset.</param>
        internal static void SaveOpenGraphAsset(BaseGraphTool graphTool)
        {
            if (graphTool.ToolState.CurrentGraph.GetGraphAsset() is not ShaderGraphAsset graphAsset)
            {
                return;
            }

            if (graphAsset.ShaderGraphModel.IsSubGraph)
            {
                SaveSubGraphImplementation(graphTool);
            }
            else
            {
                graphAsset.Save();
            }
        }

        /// <summary>
        /// Saves a new file from the graph *or* subgraph that is currently open in the given GraphTool.
        /// Does nothing and returns an empty string if there is no open graph.
        /// </summary>
        /// <param name="graphTool">Graph tool with an open Shader Graph asset.</param>
        internal static string SaveOpenGraphAssetAs(BaseGraphTool graphTool)
        {
            if (graphTool.ToolState.CurrentGraph.GetGraphAsset() is not ShaderGraphAsset graphAsset)
            {
                return string.Empty;
            }

            return graphAsset.ShaderGraphModel.IsSubGraph
                ? SaveAsSubGraphImplementation(graphTool)
                : SaveAsGraphImplementation(graphTool);
        }
    }
}
