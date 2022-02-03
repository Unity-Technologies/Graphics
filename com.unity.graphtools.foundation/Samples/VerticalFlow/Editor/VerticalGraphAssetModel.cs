using System;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalGraphAssetModel : GraphAssetModel
    {
        [MenuItem("Assets/Create/VerticalFlow")]
        public static void CreateGraph(MenuCommand menuCommand)
        {
            const string path = "Assets";
            var template = new GraphTemplate<VerticalStencil>(VerticalStencil.k_GraphName);
            ICommandTarget target = null;
            if (EditorWindow.HasOpenInstances<VerticalGraphWindow>())
            {
                var window = EditorWindow.GetWindow<VerticalGraphWindow>();
                if (window != null)
                {
                    target = window.GraphTool;
                }
            }

            GraphAssetCreationHelpers<VerticalGraphAssetModel>.CreateInProjectWindow(template, target, path);
        }

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is VerticalGraphAssetModel graphAssetModel)
            {
                var window = GraphViewEditorWindow.FindOrCreateGraphWindow<VerticalGraphWindow>();
                window.SetCurrentSelection(graphAssetModel, GraphViewEditorWindow.OpenMode.OpenAndFocus);
                return window != null;
            }

            return false;
        }

        protected override Type GraphModelType => typeof(VerticalGraphModel);
    }
}
