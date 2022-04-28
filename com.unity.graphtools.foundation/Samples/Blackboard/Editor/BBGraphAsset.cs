using System;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Blackboard
{
    public class BBGraphAsset : GraphAsset
    {
        [MenuItem("Assets/Create/GTF Samples/Blackboard")]
        public static void CreateGraph()
        {
            const string path = "Assets";
            var template = new GraphTemplate<BBStencil>(BBStencil.graphName);

            GraphAssetCreationHelpers.CreateInProjectWindow<BBGraphAsset>(template, null, path,
                () => GraphViewEditorWindow.FindOrCreateGraphWindow<BBGraphWindow>());
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
