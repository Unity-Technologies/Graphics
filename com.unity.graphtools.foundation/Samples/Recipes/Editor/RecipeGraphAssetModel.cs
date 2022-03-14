using System;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeGraphAssetModel : GraphAssetModel
    {
        [MenuItem("Assets/Create/Recipe")]
        public static void CreateGraph(MenuCommand menuCommand)
        {
            const string path = "Assets";
            var template = new GraphTemplate<RecipeStencil>(RecipeStencil.graphName);
            ICommandTarget target = null;
            if (EditorWindow.HasOpenInstances<RecipeGraphWindow>())
            {
                var window = EditorWindow.GetWindow<RecipeGraphWindow>();
                if (window != null)
                {
                    target = window.GraphTool;
                }
            }

            GraphAssetCreationHelpers<RecipeGraphAssetModel>.CreateInProjectWindow(template, target, path);
        }

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is RecipeGraphAssetModel graphAssetModel)
            {
                var window = GraphViewEditorWindow.FindOrCreateGraphWindow<RecipeGraphWindow>();
                window.SetCurrentSelection(graphAssetModel, GraphViewEditorWindow.OpenMode.OpenAndFocus);
                return window != null;
            }

            return false;
        }

        protected override Type GraphModelType => typeof(RecipeGraphModel);
    }
}
