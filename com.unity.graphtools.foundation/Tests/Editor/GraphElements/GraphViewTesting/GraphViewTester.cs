using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphViewTester
    {
        protected static readonly Rect k_WindowRect = new Rect(Vector2.zero, new Vector2(Overdrive.GraphView.panAreaWidth * 8, Overdrive.GraphView.panAreaWidth * 6));

        bool m_SnapToPortEnabled;
        bool m_SnapToBorderEnabled;
        bool m_SnapToGridEnabled;
        bool m_SnapToSpacingEnabled;
        float m_SpacingMarginValue;

        protected virtual bool EnablePanning => true;

        // store PanSpeed to restore it after tests
        static float s_OriginalPanSpeed;

        protected TestGraphViewWindow Window { get; set; }
        protected TestGraphView GraphView { get; private set; }
        protected TestEventHelpers Helpers { get; private set; }
        protected IGraphModel GraphModel => Window.GraphTool.ToolState.GraphModel;

        bool m_EnablePersistence;

        public GraphViewTester(bool enablePersistence = false)
        {
            m_EnablePersistence = enablePersistence;
        }

        bool m_SavedUseNewStylesheets;

        protected virtual void CreateWindow()
        {
            Window = EditorWindow.GetWindowWithRect<TestGraphViewWindow>(k_WindowRect);
            Window.CloseAllOverlays();
        }

        [SetUp]
        public virtual void SetUp()
        {
            m_SnapToPortEnabled = GraphViewSettings.UserSettings.EnableSnapToPort;
            m_SnapToBorderEnabled = GraphViewSettings.UserSettings.EnableSnapToBorders;
            m_SnapToGridEnabled = GraphViewSettings.UserSettings.EnableSnapToGrid;
            m_SnapToSpacingEnabled = GraphViewSettings.UserSettings.EnableSnapToSpacing;
            m_SpacingMarginValue = GraphViewSettings.UserSettings.SpacingMarginValue;

            GraphViewSettings.UserSettings.EnableSnapToPort = false;
            GraphViewSettings.UserSettings.EnableSnapToBorders = false;
            GraphViewSettings.UserSettings.EnableSnapToGrid = false;
            GraphViewSettings.UserSettings.EnableSnapToSpacing = false;

            CreateWindow();

            if (!m_EnablePersistence)
                Window.DisableViewDataPersistence();
            else
                Window.ClearPersistentViewData();

            GraphView = Window.GraphView as TestGraphView;
            GraphView.AddTestStylesheet("Tests.uss");

            Helpers = new TestEventHelpers(Window);

            var graphAsset = GraphAssetCreationHelpers<TestGraphAsset>.CreateInMemoryGraphAsset(typeof(TestStencil), "Test");
            Window.GraphTool.Dispatch(new LoadGraphCommand(graphAsset.GraphModel));
            Window.GraphTool.Update();

            Vector3 frameTranslation = Vector3.zero;
            Vector3 frameScaling = Vector3.one;
            Window.GraphView.Dispatch(new ReframeGraphViewCommand(frameTranslation, frameScaling));
            Window.GraphTool.Update();

            s_OriginalPanSpeed = Overdrive.GraphView.basePanSpeed;
            if (!EnablePanning)
            {
                Overdrive.GraphView.basePanSpeed = 0f;
            }
        }

        [TearDown]
        public virtual void TearDown()
        {
            ViewForModel.Reset();

            if (m_EnablePersistence)
                Window.ClearPersistentViewData();

            Clear();

            GraphViewSettings.UserSettings.EnableSnapToPort = m_SnapToPortEnabled;
            GraphViewSettings.UserSettings.EnableSnapToBorders = m_SnapToBorderEnabled;
            GraphViewSettings.UserSettings.EnableSnapToGrid = m_SnapToGridEnabled;
            GraphViewSettings.UserSettings.EnableSnapToSpacing = m_SnapToSpacingEnabled;
            GraphViewSettings.UserSettings.SpacingMarginValue = m_SpacingMarginValue;

            Overdrive.GraphView.basePanSpeed = s_OriginalPanSpeed;
        }

        void Clear()
        {
            // See case: https://fogbugz.unity3d.com/f/cases/998343/
            // Clearing the capture needs to happen before closing the window
            MouseCaptureController.ReleaseMouse();
            if (Window != null)
            {
                Window.Close();
            }
        }

        protected void MarkGraphViewStateDirty()
        {
            using (var updater = GraphView.GraphViewModel.GraphModelState.UpdateScope)
            {
                updater.ForceCompleteUpdate();
            }
        }

        public IONodeModel CreateNode(string title = "", Vector2 position = default, int inCount = 0, int outCount = 0, int exeInCount = 0, int exeOutCount = 0, PortOrientation orientation = PortOrientation.Horizontal)
        {
            return CreateNode<IONodeModel>(title, position, inCount, outCount, exeInCount, exeOutCount, orientation);
        }

        protected ContextNodeModel CreateContext(string title = "", Vector2 position = default)
        {
            return GraphModel.CreateNode<ContextNodeModel>(title, position, initializationCallback: _ => {});
        }

        protected BlockNodeModel CreateBlock(IContextNodeModel context, int index = -1, string title = "", int inCount = 0, int outCount = 0)
        {
            BlockNodeModel blockNodeModel = context.CreateAndInsertBlock<BlockNodeModel>(index);
            blockNodeModel.InputCount = inCount;
            blockNodeModel.OuputCount = outCount;
            blockNodeModel.Title = title;
            blockNodeModel.DefineNode();

            return blockNodeModel;
        }

        protected TNodeModel CreateNode<TNodeModel>(string title, Vector2 position, int inCount = 0, int outCount = 0, int exeInCount = 0, int exeOutCount = 0, PortOrientation orientation = PortOrientation.Horizontal) where TNodeModel : IONodeModel, new()
        {
            var node = GraphModel.CreateNode<TNodeModel>(title, position, initializationCallback: model =>
            {
                model.InputCount = inCount;
                model.OutputCount = outCount;
                model.ExeInputCount = exeInCount;
                model.ExeOutputCount = exeOutCount;
            });

            foreach (var portModel in node.Ports.Cast<PortModel>())
            {
                portModel.Orientation = orientation;
            }

            return node;
        }

        protected TContextModel CreateContext<TContextModel>(string title, Vector2 position, int inCount = 0, int outCount = 0, int exeInCount = 0, int exeOutCount = 0, PortOrientation orientation = PortOrientation.Horizontal) where TContextModel : ContextNodeModel, new()
        {
            var node = GraphModel.CreateNode<TContextModel>(title, position, initializationCallback: model =>
            {
                model.InputCount = inCount;
                model.OuputCount = outCount;
                model.ExeInputCount = exeInCount;
                model.ExeOuputCount = exeOutCount;
            });

            foreach (var portModel in node.Ports.Cast<PortModel>())
            {
                portModel.Orientation = orientation;
            }

            return node;
        }

        protected IEnumerator ConnectPorts(IPortModel fromPort, IPortModel toPort)
        {
            var originalEdgeCount = GraphModel.EdgeModels.Count;
            var fromPortUI = fromPort.GetView<Port>(GraphView);
            var toPortUI = toPort.GetView<Port>(GraphView);

            Assert.IsNotNull(fromPortUI);
            Assert.IsNotNull(toPortUI);

            // Drag an edge between the two ports
            Helpers.DragTo(fromPortUI.GetGlobalCenter(), toPortUI.GetGlobalCenter());
            yield return null;

            Assert.AreEqual(originalEdgeCount + 1, GraphModel.EdgeModels.Count, "Edge has not been created");
        }

        protected IPlacematModel CreatePlacemat(Rect posAndDim, string title = "")
        {
            var pm = GraphModel.CreatePlacemat(posAndDim);
            pm.Title = title;
            return pm;
        }

        protected IStickyNoteModel CreateSticky(string title = "", string contents = "", Rect stickyRect = default)
        {
            var sticky = GraphModel.CreateStickyNote(stickyRect);
            sticky.Contents = contents;
            sticky.Title = title;
            return sticky;
        }

        protected ISubgraphNodeModel CreateSubgraphNode(IGraphAsset referenceGraphAsset, Vector2 position = default)
        {
            var subgraphNode = GraphModel.CreateSubgraphNode(referenceGraphAsset.GraphModel, position);
            return subgraphNode;
        }

        public static void AssertVector2AreEqualWithinDelta(Vector2 expected, Vector2 actual, float withinDelta, string message = null)
        {
            Assert.AreEqual(expected.x, actual.x, withinDelta, message);
            Assert.AreEqual(expected.y, actual.y, withinDelta, message);
        }
    }
}
