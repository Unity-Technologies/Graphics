using System.Linq;
using RMGUI.GraphView;
using RMGUI.GraphView.Demo;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums.Values;

namespace UnityEditor.Graphing.Drawing
{
    [GUISkinStyle("window")]
    [CustomDataView(typeof(MaterialNodeData))]
    public class MaterialGraphNode : GraphElement
    {
        VisualContainer m_InputContainer;
        VisualContainer m_OutputContainer;

        public MaterialGraphNode()
        {
            content = new GUIContent("");

            m_InputContainer = new VisualContainer
            {
                name = "input", // for USS&Flexbox
                flexDirection = FlexDirection.Column,
                pickingMode = PickingMode.Ignore,
            };
            m_OutputContainer = new VisualContainer
            {
                name = "output", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
                flexDirection = FlexDirection.Column,
            };

            AddChild(m_InputContainer);
            AddChild(m_OutputContainer);
        }

        public override void DoRepaint(PaintContext painter)
        {
            base.DoRepaint(painter);
            if (GetData<GraphElementData>() != null && GetData<GraphElementData>().selected)
            {
                painter.DrawRectangleOutline(transform, position, Color.yellow);
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            m_OutputContainer.ClearChildren();
            m_InputContainer.ClearChildren();

            var nodeData = (MaterialNodeData) dataProvider;

            if (nodeData == null)
                return;

            content = new GUIContent(nodeData.name);
            foreach (var anchor in nodeData.elements.OfType<NodeAnchorData>())
            {
                if (anchor.direction == Direction.Input)
                    m_InputContainer.AddChild(new RMGUI.GraphView.Demo.NodeAnchor(anchor));
                else
                    m_OutputContainer.AddChild(new RMGUI.GraphView.Demo.NodeAnchor(anchor));
            }
        }
    }
}
