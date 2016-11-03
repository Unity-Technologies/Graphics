using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEditor.Graphing.Util;

namespace UnityEditor.Graphing.Drawing
{
    public class NodeDrawer : GraphElement
    {
        VisualContainer m_SlotContainer;
        List<AnchorDrawData> m_currentAnchors;
        VisualContainer m_ControlsContainer;
        List<ControlDrawData> m_currentControlDrawData;

        public NodeDrawer()
        {
            content = new GUIContent("");

            AddContainers();

            AddToClassList("NodeDrawer");
        }

        public override void DoRepaint(IStylePainter painter)
        {
            base.DoRepaint(painter);
            if (GetData<GraphElementData>() != null && GetData<GraphElementData>().selected)
            {
                painter.DrawRect(position, backgroundColor, 0.0f, borderRadius);
            }
        }

        private void AddContainers()
        {
            // Add slots (with input & output sub-containers) container
            m_SlotContainer = new VisualContainer
            {
                name = "slots", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
            };
            AddChild(m_SlotContainer);

            var inputs = new VisualContainer
            {
                name = "input", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
            };
            m_SlotContainer.AddChild(inputs);

            var outputs = new VisualContainer
            {
                name = "output", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
            };
            m_SlotContainer.AddChild(outputs);

            m_currentAnchors = new List<AnchorDrawData>();

            // Add controls container
            m_ControlsContainer = new VisualContainer
            {
                name = "controls", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
            };
            //AddChild(m_ControlsContainer);

            m_currentControlDrawData = new List<ControlDrawData>();
        }

        private void AddSlots(NodeDrawData nodeData)
        {
            var anchors = nodeData.elements.OfType<AnchorDrawData>().ToList();

            if (anchors.Count == 0)
                return;

            var inputsContainer = m_SlotContainer.GetChildAtIndex(0) as VisualContainer;
            var outputsContainer = m_SlotContainer.GetChildAtIndex(1) as VisualContainer;

            if (!anchors.ItemsReferenceEquals(m_currentAnchors))
            {
                m_currentAnchors = anchors;
                inputsContainer.ClearChildren();
                outputsContainer.ClearChildren();

                foreach (var anchor in nodeData.elements.OfType<AnchorDrawData>())
                {
                    if (anchor.direction == Direction.Input)
                        inputsContainer.AddChild(new NodeAnchor(anchor));
                    else
                        outputsContainer.AddChild(new NodeAnchor(anchor));
                }
            }

            content = new GUIContent(nodeData.name);
        }

        private void AddControls(NodeDrawData nodeData)
        {
            var controlDrawData = nodeData.elements.OfType<ControlDrawData>().ToList();

            if (controlDrawData.Count == 0)
                return;

            if (controlDrawData.ItemsReferenceEquals(m_currentControlDrawData))
            {
                for (int i = 0; i < controlDrawData.Count; i++)
                {
                    var controlData = controlDrawData[i];
                    var imContainer = m_ControlsContainer.GetChildAtIndex(i) as IMGUIContainer;
                    imContainer.OnGUIHandler = controlData.OnGUIHandler;
                    imContainer.height = controlData.GetHeight();
                }
            }
            else
            {
                m_ControlsContainer.ClearChildren();
                m_currentControlDrawData.Clear();

                foreach (var controlData in controlDrawData)
                {
                    var imContainer = new IMGUIContainer()
                    {
                        name = "element",
                        OnGUIHandler = controlData.OnGUIHandler,
                        pickingMode = PickingMode.Position,
                        height = controlData.GetHeight()
                    };
                    m_ControlsContainer.AddChild(imContainer);
                    m_currentControlDrawData.Add(controlData);
                }
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var nodeData = dataProvider as NodeDrawData;

            if (nodeData == null)
            {
                ClearChildren();
                AddContainers();
                return;
            }

            AddSlots(nodeData);
            AddControls(nodeData);
        }
    }
}
