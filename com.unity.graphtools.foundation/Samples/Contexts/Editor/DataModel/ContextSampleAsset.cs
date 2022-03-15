using System;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts.UI;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    [Serializable]
    public class ContextSampleAsset : GraphAssetModel
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

            GraphAssetCreationHelpers<ContextSampleAsset>.CreateInProjectWindow(template, graphTool, path);
        }

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is ContextSampleAsset graphAssetModel)
            {
                var window = GraphViewEditorWindow.FindOrCreateGraphWindow<ContextGraphViewWindow>(graphAssetModel.GetPath());
                window.SetCurrentSelection(window.GraphTool?.ToolState?.AssetModel?? graphAssetModel, GraphViewEditorWindow.OpenMode.OpenAndFocus);
                return true;
            }

            return false;
        }
    }
}
