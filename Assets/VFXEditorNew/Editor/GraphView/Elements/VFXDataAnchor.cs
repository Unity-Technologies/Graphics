using RMGUI.GraphView;
using UnityEngine.RMGUI.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXDataAnchor : NodeAnchor
    {
        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public static VFXDataAnchor Create<TEdgePresenter>(VFXDataAnchorPresenter presenter) where TEdgePresenter : VFXDataEdgePresenter
        {
            var anchor = new VFXDataAnchor(presenter) {
                m_EdgeConnector = new EdgeConnector<TEdgePresenter>()
            };
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        protected VFXDataAnchor(VFXDataAnchorPresenter presenter) : base(presenter)
        {

            switch(presenter.direction)
            {
                case Direction.Input:
                    classList = new ClassList("InputEdgeConnector");
                    break;
                case Direction.Output:
                    classList = new ClassList("OutputEdgeConnector");
                    break;
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            m_ConnectorText.content.text = "";
        }

    }
}
