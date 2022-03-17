using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class TestGraphViewWindow : GraphViewEditorWindow
    {
        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<TestGraphViewWindow>(GraphViewTestGraphTool.toolName);
        }

        public TestGraphViewWindow()
        {
            this.SetDisableInputEvents(true);
#if !UNITY_2022_2_OR_NEWER
            WithSidePanel = false;
#endif
        }

        protected override BaseGraphTool CreateGraphTool()
        {
            return CsoTool.Create<GraphViewTestGraphTool>(WindowID);
        }

        protected override GraphView CreateGraphView()
        {
            return new TestGraphView(this, GraphTool);
        }

        protected override bool CanHandleAssetType(IGraphAssetModel asset)
        {
            return true;
        }
    }

    class NonInteractiveTestGraphViewWindow : TestGraphViewWindow
    {
        protected override GraphView CreateGraphView()
        {
            return new TestGraphView(this, GraphTool, GraphViewDisplayMode.NonInteractive);
        }
    }
}
