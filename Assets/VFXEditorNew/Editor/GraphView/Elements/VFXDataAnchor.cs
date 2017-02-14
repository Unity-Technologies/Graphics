using RMGUI.GraphView;
using UnityEngine.RMGUI.StyleSheets;
using UnityEngine;

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
                    AddToClassList("InputEdgeConnector");
                    break;
                case Direction.Output:
                    AddToClassList("OutputEdgeConnector");
                    break;
            }
            clipChildren = false;
        }




        public override void OnDataChanged()
        {
            base.OnDataChanged();
            m_ConnectorText.content.text = "";

            NodeAnchorPresenter presenter = GetPresenter<NodeAnchorPresenter>();

            // reverse because we want the flex to choose the position of the connector
            presenter.position = position;


            if (presenter.connected)
                AddToClassList("connected");
            else
                RemoveFromClassList("connected");

            // update the css type of the class
            m_ConnectorBox.RemoveFromClassList(VFXTypeDefinition.GetTypeCSSClasses());
            m_ConnectorBox.AddToClassList(VFXTypeDefinition.GetTypeCSSClass(presenter.anchorType));
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return position.Contains(localPoint);
            //return GraphElement.ContainsPoint(localPoint);
            // Here local point comes without position offset...
            //localPoint -= position.position;
            //return m_ConnectorBox.ContainsPoint(m_ConnectorBox.transform.MultiplyPoint3x4(localPoint));
        }

    }
}
