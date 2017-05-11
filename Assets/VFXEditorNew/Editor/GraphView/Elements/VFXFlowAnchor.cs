using UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements;
using UnityEngine;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXFlowAnchor : NodeAnchor, IEdgeConnectorListener
  {
        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public static VFXFlowAnchor Create<TEdgePresenter>(VFXFlowAnchorPresenter presenter) where TEdgePresenter : VFXFlowEdgePresenter
        {
            var anchor = new VFXFlowAnchor(presenter);
            anchor.m_EdgeConnector = new EdgeConnector<TEdgePresenter>(anchor);
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

        void IEdgeConnectorListener.OnDropOutsideAnchor(EdgePresenter edge, Vector2 position)
        {
            VFXFlowAnchorPresenter presenter = GetPresenter<VFXFlowAnchorPresenter>();

            VFXView view = this.GetFirstAncestorOfType<VFXView>();
            VFXViewPresenter viewPresenter = view.GetPresenter<VFXViewPresenter>();


            VFXContextUI endContext = null;
            foreach (var node in view.GetAllContexts())
            {
                if (node.localBound.Contains(position))
                {
                    endContext = node;
                }
            }

            if (endContext != null)
            {
                VFXContextPresenter nodePresenter = endContext.GetPresenter<VFXContextPresenter>();

                var compatibleAnchors = viewPresenter.GetCompatibleAnchors(presenter, null);

                if (presenter.direction == Direction.Input)
                {
                    foreach (var outputAnchor in nodePresenter.outputAnchors)
                    {
                        if (compatibleAnchors.Contains(outputAnchor))
                        {
                            VFXFlowEdgePresenter edgePresenter = new VFXFlowEdgePresenter();
                            edgePresenter.input = presenter;
                            edgePresenter.output = outputAnchor;

                            viewPresenter.AddElement(edgePresenter);
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var inputAnchor in nodePresenter.inputAnchors)
                    {
                        if (compatibleAnchors.Contains(inputAnchor))
                        {
                            VFXFlowEdgePresenter edgePresenter = new VFXFlowEdgePresenter();
                            edgePresenter.input = inputAnchor;
                            edgePresenter.output = presenter;

                            viewPresenter.AddElement(edgePresenter);
                            break;
                        }
                    }
                }
            }
            else if (Event.current.modifiers == EventModifiers.Alt)
            {
                //TODO create the most obvious context and link
            }
        }
    }
}
