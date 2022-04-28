using System;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalGraphAsset : GraphAsset
    {
        [MenuItem("Assets/Create/GTF Samples/VerticalFlow")]
        public static void CreateGraph()
        {
            const string path = "Assets";
            var template = new GraphTemplate<VerticalStencil>(VerticalStencil.graphName);

            GraphAssetCreationHelpers.CreateInProjectWindow<VerticalGraphAsset>(template, null, path,
                () => GraphViewEditorWindow.FindOrCreateGraphWindow<VerticalGraphWindow>());
        }

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is VerticalGraphAsset graphAsset)
            {
                var window = GraphViewEditorWindow.FindOrCreateGraphWindow<VerticalGraphWindow>(graphAsset.FilePath);
                graphAsset = window.GraphTool?.ToolState?.CurrentGraph.GetGraphAsset() as VerticalGraphAsset ?? graphAsset;
                window.SetCurrentSelection(graphAsset, GraphViewEditorWindow.OpenMode.OpenAndFocus);
                return true;
            }

            return false;
        }

        protected override Type GraphModelType => typeof(VerticalGraphModel);
    }
}
