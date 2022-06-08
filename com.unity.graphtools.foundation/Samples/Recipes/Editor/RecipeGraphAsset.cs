using System;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeGraphAsset : GraphAsset
    {
        [MenuItem("Assets/Create/GTF Samples/Recipe")]
        public static void CreateGraph()
        {
            const string path = "Assets";
            var template = new GraphTemplate<RecipeStencil>(RecipeStencil.graphName);

            GraphAssetCreationHelpers.CreateInProjectWindow<RecipeGraphAsset>(template, null, path,
                () => GraphViewEditorWindow.FindOrCreateGraphWindow<RecipeGraphWindow>());
        }

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is RecipeGraphAsset graphAsset)
            {
                var window = GraphViewEditorWindow.FindOrCreateGraphWindow<RecipeGraphWindow>(graphAsset.FilePath);
                graphAsset = window.GraphTool?.ToolState?.CurrentGraph.GetGraphAsset() as RecipeGraphAsset ?? graphAsset;
                window.SetCurrentSelection(graphAsset, GraphViewEditorWindow.OpenMode.OpenAndFocus);
                return true;
            }

            return false;
        }

        protected override Type GraphModelType => typeof(RecipeGraphModel);
    }
}
