using System.Linq;
using RMGUI.GraphView;
using RMGUI.GraphView.Demo;
using UnityEditor.MaterialGraph.Drawing;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    [GUISkinStyle("window")]
    public class NodeDrawer : GraphElement
    {
        VisualContainer m_SlotContainer;
        VisualContainer m_ControlsContainer;

        public NodeDrawer()
        {
            content = new GUIContent("");

            m_SlotContainer = new VisualContainer
            {
                name = "slots", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
            };

            m_ControlsContainer = new VisualContainer
            {
                name = "controls", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
            };
        }

        public override void DoRepaint(PaintContext painter)
        {
            base.DoRepaint(painter);
            if (GetData<GraphElementData>() != null && GetData<GraphElementData>().selected)
            {
                painter.DrawRectangleOutline(transform, position, Color.yellow);
            }
        }

        private void AddSlots(MaterialNodeDrawData nodeData)
        {
            m_SlotContainer.ClearChildren();

            if (!nodeData.elements.OfType<NodeAnchorData>().Any())
                return;

            var inputs = new VisualContainer
            {
                name = "input", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
            };
            m_SlotContainer.AddChild(inputs);

            // put a spacer here?
            //m_SlotContainer.AddChild(new f);

            var outputs = new VisualContainer
            {
                name = "output", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
            };
            m_SlotContainer.AddChild(outputs);

            content = new GUIContent(nodeData.name);
            foreach (var anchor in nodeData.elements.OfType<NodeAnchorData>())
            {
                if (anchor.direction == Direction.Input)
                    inputs.AddChild(new NodeAnchor(anchor));
                else
                    outputs.AddChild(new NodeAnchor(anchor));
            }

            AddChild(m_SlotContainer);
        }

        private void AddControls(MaterialNodeDrawData nodeData)
        {
            m_ControlsContainer.ClearChildren();

            if (!nodeData.elements.OfType<ControlDrawData>().Any())
                return;

            foreach (var controlData in nodeData.elements.OfType<ControlDrawData>())
            {
                var imContainer = new IMGUIContainer
                {
                    name = "element",
                    OnGUIHandler = controlData.OnGUIHandler,
                    pickingMode = PickingMode.Position,
                    height = controlData.GetHeight(),
                };
                m_ControlsContainer.AddChild(imContainer);
            }

            AddChild(m_ControlsContainer);
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            ClearChildren();

            m_ControlsContainer.ClearChildren();

            var nodeData = dataProvider as MaterialNodeDrawData;

            if (nodeData == null)
                return;

            AddSlots(nodeData);
            AddControls(nodeData);
        }
    }
}
