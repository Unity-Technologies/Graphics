using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    public class NodeDrawer : Node
    {
        private readonly VisualContainer m_ControlsContainer;
        private readonly List<ControlDrawData> m_CurrentControlDrawData;

        public NodeDrawer()
        {
            content = new GUIContent("");

            m_ControlsContainer = new VisualContainer
            {
                name = "controls",
                pickingMode = PickingMode.Ignore,
            };

            m_LeftContainer.AddChild(m_ControlsContainer);
            m_CurrentControlDrawData = new List<ControlDrawData>();
        }

        private void AddControls(NodeDrawData nodeData)
        {
            var controlDrawData = nodeData.elements.OfType<ControlDrawData>().ToList();

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

            var nodeData = GetPresenter<NodeDrawData>();

            if (nodeData == null)
            {
                ClearChildren();
                return;
            }

            if (!nodeData.expanded)
                AddToClassList("collapsed");
            else
                RemoveFromClassList("collapsed");

            AddControls(nodeData);
        }
    }
}
