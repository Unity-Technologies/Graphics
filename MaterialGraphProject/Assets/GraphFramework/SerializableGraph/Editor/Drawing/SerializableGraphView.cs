using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using RMGUI.GraphView.Demo;
using UnityEditor.MaterialGraph.Drawing;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    [StyleSheet("Assets/UnityShaderEditor/Editor/Styles/NodalView.uss")]
    public class SerializableGraphView : GraphView
    {
        public SerializableGraphView()
        {
            // Shortcut handler to delete elements
            var dictionary = new Dictionary<Event, ShortcutDelegate>();
            dictionary[Event.KeyboardEvent("delete")] = DeleteSelection;
            contentViewContainer.AddManipulator(new ShortcutHandler(dictionary));

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
                                         selection.OfType<MaterialNodeDrawer>().Select(x => x.dataProvider as MaterialNodeDrawData),
                selection.OfType<RMGUI.GraphView.Edge>().Select(x => x.dataProvider as EdgeDrawData)
                );

            return EventPropagation.Stop;
        }
    }
}
