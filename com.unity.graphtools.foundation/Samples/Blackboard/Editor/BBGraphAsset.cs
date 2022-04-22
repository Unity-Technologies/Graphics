using System;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Blackboard
{
    public class BBGraphAsset : GraphAsset
    {
        [MenuItem("Assets/Create/GTF Samples/Blackboard")]
        public static void CreateGraph(MenuCommand menuCommand)
        {
            const string path = "Assets";
            var template = new GraphTemplate<BBStencil>(BBStencil.graphName);
            BaseGraphTool graphTool = null;
            var window = GraphViewEditorWindow.FindOrCreateGraphWindow<BBGraphWindow>();
            if (window != null)
                graphTool = window.GraphTool;

            GraphAssetCreationHelpers.CreateInProjectWindow<BBGraphAsset>(template, graphTool, path);
        }

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is BBGraphAsset graphAsset)
            {
                var window = GraphViewEditorWindow.FindOrCreateGraphWindow<BBGraphWindow>(graphAsset.FilePath);
                graphAsset = window.GraphTool?.ToolState?.CurrentGraph.GetGraphAsset() as BBGraphAsset ?? graphAsset;
                window.SetCurrentSelection(graphAsset, GraphViewEditorWindow.OpenMode.OpenAndFocus);
                return true;
            }

            return false;
        }

        protected override Type GraphModelType => typeof(BBGraphModel);
    }
}
