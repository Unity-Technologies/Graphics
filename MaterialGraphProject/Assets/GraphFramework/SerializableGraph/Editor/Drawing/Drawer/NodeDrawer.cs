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
        HeaderDrawer m_HeaderDrawer;
        HeaderDrawData m_HeaderData;
        VisualContainer m_SlotContainer;
        List<AnchorDrawData> m_CurrentAnchors;
        VisualContainer m_ControlsContainer;
        List<ControlDrawData> m_CurrentControlDrawData;
        bool m_CurrentExpanded;

        public NodeDrawer()
        {
            content = new GUIContent("");

            AddContainers();

            AddToClassList("Node");
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

            m_CurrentAnchors = new List<AnchorDrawData>();

            // Add controls container
            m_ControlsContainer = new VisualContainer
            {
                name = "controls", // for USS&Flexbox
                pickingMode = PickingMode.Ignore,
            };
            AddChild(m_ControlsContainer);

            m_CurrentControlDrawData = new List<ControlDrawData>();
        }

        private void AddHeader(NodeDrawData nodeData)
        {
            var headerData = nodeData.elements.OfType<HeaderDrawData>().FirstOrDefault();

            if (m_HeaderData != null)
            {
                m_HeaderDrawer.dataProvider = headerData;
                m_HeaderData = headerData;
            }
            else
            {
                m_HeaderDrawer = new HeaderDrawer(headerData);
                InsertChild(0, m_HeaderDrawer);
                m_HeaderData = headerData;
            }
        }

        private void AddSlots(NodeDrawData nodeData)
        {
            var anchors = nodeData.elements.OfType<AnchorDrawData>().ToList();

            if (anchors.Count == 0)
                return;

            var inputsContainer = m_SlotContainer.GetChildAtIndex(0) as VisualContainer;
            var outputsContainer = m_SlotContainer.GetChildAtIndex(1) as VisualContainer;

            if (anchors.ItemsReferenceEquals(m_CurrentAnchors) && m_CurrentExpanded == nodeData.expanded)
            {
                return;
            }

            m_CurrentAnchors = anchors;
            inputsContainer.ClearChildren();
            outputsContainer.ClearChildren();

            foreach (var anchor in anchors)
            {
                var hidden = !nodeData.expanded && !anchor.connected;
                if (!hidden && anchor.direction == Direction.Input)
                    inputsContainer.AddChild(new NodeAnchor(anchor));
                else if (!hidden && anchor.direction == Direction.Output)
                    outputsContainer.AddChild(new NodeAnchor(anchor));
            }
        }

        private void AddControls(NodeDrawData nodeData)
        {
            var controlDrawData = nodeData.elements.OfType<ControlDrawData>().ToList();

            if (controlDrawData.Count == 0)
                return;

            if (!nodeData.expanded)
            {
                m_ControlsContainer.ClearChildren();
                m_CurrentControlDrawData.Clear();
                return;
            }

            if (controlDrawData.ItemsReferenceEquals(m_CurrentControlDrawData))
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
                m_CurrentControlDrawData.Clear();

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
                    m_CurrentControlDrawData.Add(controlData);
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

            if (!nodeData.expanded)
            {
                if (!classList.Contains("collapsed"))
                    AddToClassList("collapsed");
            }
            else
            {
                if (classList.Contains("collapsed"))
                    RemoveFromClassList("collapsed");
            }

            AddHeader(nodeData);
            AddSlots(nodeData);
            AddControls(nodeData);

            m_CurrentExpanded = nodeData.expanded;
        }
    }
}
