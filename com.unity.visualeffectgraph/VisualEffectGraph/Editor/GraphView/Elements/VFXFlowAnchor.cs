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
                    foreach (var outputAnchor in nodePresenter.flowOutputAnchors)
                    {
                        if (compatibleAnchors.Contains(outputAnchor))
                        {
                            VFXFlowEdgePresenter edgePresenter = VFXFlowEdgePresenter.CreateInstance<VFXFlowEdgePresenter>();
                            edgePresenter.input = presenter;
                            edgePresenter.output = outputAnchor;

                            viewPresenter.AddElement(edgePresenter);
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var inputAnchor in nodePresenter.flowInputAnchors)
                    {
                        if (compatibleAnchors.Contains(inputAnchor))
                        {
                            VFXFlowEdgePresenter edgePresenter = VFXFlowEdgePresenter.CreateInstance<VFXFlowEdgePresenter>();
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
                VFXContextType targetContextType = VFXContextType.kNone;
                //TODO create the most obvious context and link
                if (presenter.direction == Direction.Input)
                {
                    switch (presenter.Owner.contextType)
                    {
                        case VFXContextType.kUpdate:
                            targetContextType = VFXContextType.kInit;
                            break;
                        case VFXContextType.kOutput:
                            targetContextType = VFXContextType.kUpdate;
                            break;
                    }
                }
                else
                {
                    switch (presenter.Owner.contextType)
                    {
                        case VFXContextType.kUpdate:
                            targetContextType = VFXContextType.kOutput;
                            break;
                        case VFXContextType.kInit:
                            targetContextType = VFXContextType.kUpdate;
                            break;
                    }
                }

                if (targetContextType != VFXContextType.kNone)
                {
                    var contextDesc = VFXLibrary.GetContexts().FirstOrDefault(t => t.CreateInstance().contextType == targetContextType);
                    if (contextDesc != null)
                    {
                        VFXContext newContext = viewPresenter.AddVFXContext(position - new Vector2(188, presenter.direction == Direction.Input ? 92 : 16), contextDesc);

                        VFXContextPresenter newContextPresenter = viewPresenter.elements.OfType<VFXContextPresenter>().FirstOrDefault(t => t.model == newContext);

                        VFXFlowEdgePresenter edgePresenter = VFXFlowEdgePresenter.CreateInstance<VFXFlowEdgePresenter>();
                        edgePresenter.input = presenter.direction == Direction.Input ? presenter : newContextPresenter.inputAnchors[0];
                        edgePresenter.output = presenter.direction == Direction.Output ? presenter : newContextPresenter.outputAnchors[0];

                        viewPresenter.AddElement(edgePresenter);
                    }
                }
            }
        }
    }
}
