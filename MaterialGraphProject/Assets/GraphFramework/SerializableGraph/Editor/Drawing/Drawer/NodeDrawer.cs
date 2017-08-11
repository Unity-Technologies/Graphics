using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Graphing.Drawing
{
    public class NodeDrawer : Node
    {
        private readonly VisualContainer m_ControlsContainer;
        private readonly List<GraphControlPresenter> m_CurrentControlPresenter;

        public NodeDrawer()
        {
            text = string.Empty;

            m_ControlsContainer = new VisualContainer
            {
                name = "controls",
                //pickingMode = PickingMode.Ignore,
            };

            leftContainer.Add(m_ControlsContainer);
            m_CurrentControlPresenter = new List<GraphControlPresenter>();

            usePixelCaching = false;
        }

        private void AddControls(GraphNodePresenter nodeData)
        {
            var controlPresenters = nodeData.elements.OfType<GraphControlPresenter>().ToList();

            m_ControlsContainer.ClearChildren();
            m_CurrentControlPresenter.Clear();

            if (!nodeData.expanded)
                return;

            foreach (var controlData in controlPresenters)
            {
                m_ControlsContainer.AddChild(CreateControl(controlData));
                m_CurrentControlPresenter.Add(controlData);
            }
        }

        IMGUIContainer CreateControl(GraphControlPresenter controlPresenter)
        {
            return new IMGUIContainer(controlPresenter.OnGUIHandler)
            {
                name = "element",
                executionContext = controlPresenter.GetInstanceID(),
            };
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var nodeData = GetPresenter<GraphNodePresenter>();

            if (nodeData == null)
            {
                m_ControlsContainer.ClearChildren();
                m_CurrentControlPresenter.Clear();
                return;
            }

            AddControls(nodeData);
        }
    }
}
