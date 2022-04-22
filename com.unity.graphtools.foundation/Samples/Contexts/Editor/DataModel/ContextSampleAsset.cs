using System;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts.UI;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    [Serializable]
    public class ContextSampleAsset : GraphAsset
    {
        protected override Type GraphModelType => typeof(ContextSample);

        [MenuItem("Assets/Create/GTF Samples/Contexts")]
        public static void CreateGraph(MenuCommand menuCommand)
        {
            const string path = "Assets";
            var template = new GraphTemplate<ContextSampleStencil>(ContextSampleStencil.GraphName);
            BaseGraphTool graphTool = null;
            var window = GraphViewEditorWindow.FindOrCreateGraphWindow<ContextGraphViewWindow>();
            if (window != null)
                graphTool = window.GraphTool;

            GraphAssetCreationHelpers.CreateInProjectWindow<ContextSampleAsset>(template, graphTool, path);
        }

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is ContextSampleAsset graphAsset)
            {
                var window = GraphViewEditorWindow.FindOrCreateGraphWindow<ContextGraphViewWindow>(graphAsset.FilePath);
                graphAsset = window.GraphTool?.ToolState?.CurrentGraph.GetGraphAsset() as ContextSampleAsset ?? graphAsset;
                window.SetCurrentSelection(graphAsset, GraphViewEditorWindow.OpenMode.OpenAndFocus);
                return true;
            }

            return false;
        }
    }
}
