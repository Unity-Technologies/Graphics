using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using RMGUI.GraphView.Demo;
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
    }
}
