using System;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Blackboard
{
    public class BBGraphAssetModel : GraphAssetModel
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

            GraphAssetCreationHelpers<BBGraphAssetModel>.CreateInProjectWindow(template, graphTool, path);
        }

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is BBGraphAssetModel graphAssetModel)
            {
                var window = GraphViewEditorWindow.FindOrCreateGraphWindow<BBGraphWindow>(graphAssetModel.GetPath());
                window.SetCurrentSelection(window.GraphTool?.ToolState?.AssetModel?? graphAssetModel, GraphViewEditorWindow.OpenMode.OpenAndFocus);
                return true;
            }

            return false;
        }

        protected override Type GraphModelType => typeof(BBGraphModel);
    }
}
