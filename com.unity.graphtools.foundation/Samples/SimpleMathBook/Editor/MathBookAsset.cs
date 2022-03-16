using System;
using System.Linq;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathBookAsset : GraphAssetModel
    {
        protected override Type GraphModelType => typeof(MathBook);

        [MenuItem("Assets/Create/GTF Samples/Math Book/Math Book")]
        public static void CreateGraph(MenuCommand menuCommand)
        {
            const string path = "Assets";
            var template = new GraphTemplate<MathBookStencil>(MathBookStencil.GraphName);
            ICommandTarget target = null;
            var window = GraphViewEditorWindow.FindOrCreateGraphWindow<SimpleGraphViewWindow>();
            if (window != null)
                target = window.GraphTool;

            GraphAssetCreationHelpers<MathBookAsset>.CreateInProjectWindow(template, target, path);
        }

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is MathBookAsset graphAssetModel)
            {
                var window = GraphViewEditorWindow.FindOrCreateGraphWindow<SimpleGraphViewWindow>(graphAssetModel.GetPath());
                window.SetCurrentSelection(window.GraphTool?.ToolState?.AssetModel?? graphAssetModel, GraphViewEditorWindow.OpenMode.OpenAndFocus);
                return true;
            }

            return false;
        }

        public override bool CanBeSubgraph() => GraphModel.VariableDeclarations.Any(variable => variable.IsInputOrOutput());
    }
}
