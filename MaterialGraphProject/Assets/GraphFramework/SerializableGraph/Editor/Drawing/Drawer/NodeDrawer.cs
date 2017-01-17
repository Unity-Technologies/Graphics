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
        private readonly List<GraphControlPresenter> m_CurrentControlPresenter;

        public NodeDrawer()
        {
            content = new GUIContent("");

            m_ControlsContainer = new VisualContainer
            {
                name = "controls",
                pickingMode = PickingMode.Ignore,
            };

            leftContainer.AddChild(m_ControlsContainer);
            m_CurrentControlPresenter = new List<GraphControlPresenter>();
        }

        private void AddControls(GraphNodePresenter nodeData)
        {
            var controlPresenters = nodeData.elements.OfType<GraphControlPresenter>().ToList();

            if (!nodeData.expanded)
            {
                m_ControlsContainer.ClearChildren();
                m_CurrentControlPresenter.Clear();
                return;
            }

            if (controlPresenters.ItemsReferenceEquals(m_CurrentControlPresenter))
            {
                for (int i = 0; i < controlPresenters.Count; i++)
                {
                    var controlData = controlPresenters[i];
                    var imContainer = m_ControlsContainer.GetChildAtIndex(i) as IMGUIContainer;
                    imContainer.OnGUIHandler = controlData.OnGUIHandler;
                    imContainer.height = controlData.GetHeight();
                }
            }
            else
            {
                m_ControlsContainer.ClearChildren();
                m_CurrentControlPresenter.Clear();

                foreach (var controlData in controlPresenters)
                {
                    var imContainer = new IMGUIContainer()
                    {
                        name = "element",
                        OnGUIHandler = controlData.OnGUIHandler,
                        pickingMode = PickingMode.Position,
                        height = controlData.GetHeight()
                    };
                    m_ControlsContainer.AddChild(imContainer);
                    m_CurrentControlPresenter.Add(controlData);
                }
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var nodeData = GetPresenter<GraphNodePresenter>();

            if (nodeData == null)
            {
                ClearChildren();
                return;
            }

            AddControls(nodeData);
        }
    }
}
