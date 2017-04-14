using UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXFlowAnchor : NodeAnchor
    {
        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public static VFXFlowAnchor Create<TEdgePresenter>(VFXFlowAnchorPresenter presenter) where TEdgePresenter : VFXFlowEdgePresenter
        {
            var anchor = new VFXFlowAnchor(presenter) {
                m_EdgeConnector = new EdgeConnector<TEdgePresenter>(null)
            };
            anchor.AddManipulator(anchor.m_EdgeConnector);
            anchor.presenter = presenter;
            return anchor;
        }

        protected VFXFlowAnchor(VFXFlowAnchorPresenter presenter) : base(presenter)
        {
            AddToClassList("EdgeConnector");
            switch (presenter.direction)
            {
                case Direction.Input:
                    AddToClassList("Input");
                    break;
                case Direction.Output:
                    AddToClassList("Output");
                    break;
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            m_ConnectorText.text = "";

            VFXFlowAnchorPresenter presenter = GetPresenter<VFXFlowAnchorPresenter>();

            if (presenter.connected)
                AddToClassList("connected");
            else
                RemoveFromClassList("connected");
        }

    }
}
