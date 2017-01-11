using System;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums;
using UnityEngine.RMGUI.StyleSheets;

namespace UnityEditor.VFX.UI
{
    public class VFXFlowAnchor : NodeAnchor
    {
        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public static VFXFlowAnchor Create<TEdgePresenter>(VFXFlowAnchorPresenter presenter) where TEdgePresenter : VFXFlowEdgePresenter
        {
            var anchor = new VFXFlowAnchor(presenter) {
                m_EdgeConnector = new EdgeConnector<TEdgePresenter>()
            };
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        protected VFXFlowAnchor(VFXFlowAnchorPresenter presenter) : base(presenter)
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
