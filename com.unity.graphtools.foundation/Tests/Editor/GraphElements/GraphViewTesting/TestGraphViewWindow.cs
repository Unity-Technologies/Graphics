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
            WithSidePanel = false;
        }

        protected override BaseGraphTool CreateGraphTool()
        {
            return CsoTool.Create<GraphViewTestGraphTool>();
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
}
