using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    // TODO JOCE Maybe bring SimpleGraphView public. This implements pretty much all that it does.
    public class SerializableGraphView : GraphView
    {
        public SerializableGraphView()
        {
            // Shortcut handler to delete elements
            AddManipulator(new ShortcutHandler(
                    new Dictionary<Event, ShortcutDelegate>
            {
                {Event.KeyboardEvent("a"), FrameAll},
                {Event.KeyboardEvent("f"), FrameSelection},
                {Event.KeyboardEvent("o"), FrameOrigin},
                {Event.KeyboardEvent("delete"), DeleteSelection},
                {Event.KeyboardEvent("#tab"), FramePrev},
                {Event.KeyboardEvent("tab"), FrameNext},
                {Event.KeyboardEvent("#c"), CopySelection},
                {Event.KeyboardEvent("#v"), Paste},
                {Event.KeyboardEvent("#d"), DuplicateSelection}
            }));

            AddManipulator(new ClickGlobalSelector());
            AddManipulator(new ContentZoomer());
            AddManipulator(new ContentDragger());
            AddManipulator(new RectangleSelector());
            AddManipulator(new SelectionDragger());
            AddManipulator(new ClickSelector());

            InsertChild(0, new GridBackground());

            dataMapper[typeof(NodeDrawData)] = typeof(NodeDrawer);
        }

        // TODO JOCE Remove the "new" here. Use the base class' impl
        private new EventPropagation DeleteSelection()
        {
            var nodalViewData = GetPresenter<AbstractGraphDataSource>();
            if (nodalViewData == null)
                return EventPropagation.Stop;

            nodalViewData.RemoveElements(
                selection.OfType<NodeDrawer>().Select(x => x.GetPresenter<NodeDrawData>()),
                selection.OfType<Edge>().Select(x => x.GetPresenter<EdgeDrawData>())
                );

            return EventPropagation.Stop;
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var graphDataSource = GetPresenter<AbstractGraphDataSource>();
            if (graphDataSource == null)
                return;

            var graphAsset = graphDataSource.graphAsset;
            if (graphAsset == null || graphAsset.drawingData.selection.SequenceEqual(selection.OfType<NodeDrawer>().Select(d => ((NodeDrawData) d.presenter).node.guid))) return;

            var selectedDrawers = graphDataSource.graphAsset.drawingData.selection
                .Select(guid => contentViewContainer.children
                            .OfType<NodeDrawer>()
                            .FirstOrDefault(drawer => ((NodeDrawData) drawer.presenter).node.guid == guid))
                .ToList();

            ClearSelection();
            foreach (var drawer in selectedDrawers)
                AddToSelection(drawer);
        }

        public void SetGlobalSelection()
        {
            var graphDataSource = GetPresenter<AbstractGraphDataSource>();
            if (graphDataSource == null || graphDataSource.graphAsset == null)
                return;
            Selection.activeObject = graphDataSource.graphAsset.GetScriptableObject();
        }

        private void PropagateSelection()
        {
            var graphDataSource = GetPresenter<AbstractGraphDataSource>();
            if (graphDataSource == null || graphDataSource.graphAsset == null)
                return;

            var selectedNodeGuids = selection.OfType<NodeDrawer>().Select(x => ((NodeDrawData) x.presenter).node.guid);
            graphDataSource.graphAsset.drawingData.selection = selectedNodeGuids;
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            PropagateSelection();
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            PropagateSelection();
        }

        public override void ClearSelection()
        {
            base.ClearSelection();
            PropagateSelection();
        }

        public EventPropagation CopySelection()
        {
            var graphDataSource = GetPresenter<AbstractGraphDataSource>();
            if (selection.Any() && graphDataSource != null)
                graphDataSource.Copy(selection.OfType<GraphElement>().Select(ge => ge.presenter));
            return EventPropagation.Stop;
        }

        public EventPropagation DuplicateSelection()
        {
            var graphDataSource = GetPresenter<AbstractGraphDataSource>();
            if (selection.Any() && graphDataSource != null)
                graphDataSource.Duplicate(selection.OfType<GraphElement>().Select(ge => ge.presenter));
            return EventPropagation.Stop;
        }

        public EventPropagation Paste()
        {
            var graphDataSource = GetPresenter<AbstractGraphDataSource>();
            if (graphDataSource != null)
                graphDataSource.Paste();
            return EventPropagation.Stop;
        }
    }
}
