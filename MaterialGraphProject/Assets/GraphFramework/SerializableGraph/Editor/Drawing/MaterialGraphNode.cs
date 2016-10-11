using System.Linq;
using RMGUI.GraphView;
using RMGUI.GraphView.Demo;
using UnityEditor.MaterialGraph;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    class PreviewImage : Image
    {
        public override void DoRepaint(PaintContext args)
        {
            Handles.DrawSolidRectangleWithOutline(position, Color.blue, Color.blue);
            base.DoRepaint(args);
        }
    }

    [GUISkinStyle("window")]
    public class MaterialGraphNode : GraphElement
    {
        VisualContainer m_SlotContainer;
        VisualContainer m_ControlsContainer;
        VisualContainer m_PreviewContainer;

        public MaterialGraphNode()
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

            m_PreviewContainer = new VisualContainer
            {
                name = "preview", // for USS&Flexbox
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

        private void AddSlots(MaterialNodeData nodeData)
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

        private void AddControls(MaterialNodeData nodeData)
        {
            m_ControlsContainer.ClearChildren();

            if (!nodeData.elements.OfType<NodeControlData>().Any())
                return;

            foreach (var controlData in nodeData.elements.OfType<NodeControlData>())
            {
                var imContainer = new IMGUIContainer()
                {
                    name = "element",
                    OnGUIHandler = controlData.OnGUIHandler,
                    pickingMode = PickingMode.Position
                };
                m_ControlsContainer.AddChild(imContainer);
            }

            AddChild(m_ControlsContainer);
        }

        private void AddPreview(MaterialNodeData nodeData)
        {
            m_PreviewContainer.ClearChildren();

            if (!nodeData.elements.OfType<NodePreviewData>().Any())
                return;

            foreach (var preview in nodeData.elements.OfType<NodePreviewData>())
            {
                var image = preview.Render(new Vector2(200, 200));
                var thePreview = new PreviewImage
                {
                    image = image,
                    name = "image"
                };
                m_PreviewContainer.AddChild(thePreview);
            }

            AddChild(m_PreviewContainer);
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            ClearChildren();

            m_ControlsContainer.ClearChildren();
            m_PreviewContainer.ClearChildren();

            var nodeData = dataProvider as MaterialNodeData;

            if (nodeData == null)
                return;

            AddSlots(nodeData);
            AddControls(nodeData);
            AddPreview(nodeData);

            /*positionType = PositionType.Absolute;
            positionLeft = nodeData.node.drawState.position.x;
            positionTop = nodeData.node.drawState.position.y;*/
        }
    }
}
