using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    [StyleSheet("Assets/GraphFramework/SerializableGraph/Editor/Drawing/Styles/SerializableGraph.uss")]
    public class SerializableGraphView : GraphView
    {
        public SerializableGraphView()
        {
            // Shortcut handler to delete elements
            var dictionary = new Dictionary<Event, ShortcutDelegate>();
            dictionary[Event.KeyboardEvent("delete")] = DeleteSelection;
            AddManipulator(new ShortcutHandler(dictionary));

            AddManipulator(new ContentZoomer());
            AddManipulator(new ContentDragger());
            AddManipulator(new RectangleSelector());
            AddManipulator(new SelectionDragger());
            AddManipulator(new ClickSelector());
            AddDecorator(new GridBackground());

            dataMapper[typeof(NodeDrawData)] = typeof(NodeDrawer);
        }

        private EventPropagation DeleteSelection()
        {
            var nodalViewData = dataSource as AbstractGraphDataSource;
            if (nodalViewData == null)
                return EventPropagation.Stop;

            nodalViewData.RemoveElements(
                selection.OfType<NodeDrawer>().Select(x => x.dataProvider as NodeDrawData),
                selection.OfType<Edge>().Select(x => x.dataProvider as EdgeDrawData)
                );

            return EventPropagation.Stop;
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var graphDataSource = dataSource as AbstractGraphDataSource;
            if (graphDataSource == null) return;

            var graphAsset = graphDataSource.graphAsset;
            if (graphAsset == null || selection.Count != 0 || !graphAsset.drawingData.selection.Any()) return;

            var selectedDrawers = graphDataSource.graphAsset.drawingData.selection
                .Select(guid => contentViewContainer.children
                            .OfType<NodeDrawer>()
                            .FirstOrDefault(drawer => ((NodeDrawData) drawer.dataProvider).node.guid == guid))
                .ToList();

            foreach (var drawer in selectedDrawers)
                AddToSelection(drawer);
        }

        private void PropagateSelection()
        {
            var graphDataSource = dataSource as AbstractGraphDataSource;
            if (graphDataSource == null) return;

            var selectedNodeGuids = selection.OfType<NodeDrawer>().Select(x => ((NodeDrawData) x.dataProvider).node.guid);
            graphDataSource.graphAsset.drawingData.selection = selectedNodeGuids;

            // TODO: Maybe put somewhere else
            Selection.activeObject = graphDataSource.graphAsset.GetScriptableObject();
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
    }
}
