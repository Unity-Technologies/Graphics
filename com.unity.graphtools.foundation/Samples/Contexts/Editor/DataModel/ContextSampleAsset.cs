using System;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts.UI;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    [Serializable]
    public class ContextSampleAsset : GraphAssetModel
    {
        protected override Type GraphModelType => typeof(ContextSample);

        [MenuItem("Assets/Create/Contexts")]
        public static void CreateGraph(MenuCommand menuCommand)
        {
            const string path = "Assets";
            var template = new GraphTemplate<ContextSampleStencil>(ContextSampleStencil.GraphName);
            BaseGraphTool graphTool = null;
            if (EditorWindow.HasOpenInstances<ContextGraphViewWindow>())
            {
                var window = EditorWindow.GetWindow<ContextGraphViewWindow>();
                if (window != null)
                {
                    graphTool = window.GraphTool;
                }
            }

            GraphAssetCreationHelpers<ContextSampleAsset>.CreateInProjectWindow(template, graphTool, path);
        }

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is ContextSampleAsset graphAssetModel)
            {
                var window = GraphViewEditorWindow.FindOrCreateGraphWindow<ContextGraphViewWindow>();
                window.SetCurrentSelection(graphAssetModel, GraphViewEditorWindow.OpenMode.OpenAndFocus);
                return window != null;
            }

            return false;
        }
    }
}
