using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements;
using UnityEngine;
using System.Linq;
using Type = System.Type;

namespace UnityEditor.VFX.UI
{
    class VFXFlowAnchor : NodeAnchor, IEdgeConnectorListener
    {
        public static VFXFlowAnchor Create(VFXFlowAnchorPresenter presenter)
        {
            var anchor = new VFXFlowAnchor(presenter.orientation, presenter.direction, presenter.anchorType);
            anchor.m_EdgeConnector = new EdgeConnector<VFXFlowEdge>(anchor);
            anchor.AddManipulator(anchor.m_EdgeConnector);
            anchor.presenter = presenter;
            return anchor;
        }

        protected VFXFlowAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type) : base(anchorOrientation, anchorDirection, type)
        {
            AddToClassList("EdgeConnector");
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

            switch (presenter.direction)
            {
                case Direction.Input:
                    RemoveFromClassList("Output");
                    AddToClassList("Input");
                    break;
                case Direction.Output:
                    RemoveFromClassList("Input");
                    AddToClassList("Output");
                    break;
            }
        }

        void IEdgeConnectorListener.OnDrop(GraphView graphView, Edge edge)
        {
            VFXFlowEdgePresenter edgePresenter = VFXFlowEdgePresenter.CreateInstance<VFXFlowEdgePresenter> ();
            edge.presenter = edgePresenter;
            edgePresenter.input = edge.input.GetPresenter<VFXFlowAnchorPresenter>();
            edgePresenter.output = edge.output.GetPresenter<VFXFlowAnchorPresenter>();

            graphView.GetPresenter<VFXViewPresenter>().AddElement(edgePresenter);
        }

        void IEdgeConnectorListener.OnDropOutsideAnchor(Edge edge, Vector2 position)
        {
            VFXFlowAnchorPresenter presenter = GetPresenter<VFXFlowAnchorPresenter>();

            VFXView view = this.GetFirstAncestorOfType<VFXView>();
            VFXViewPresenter viewPresenter = view.GetPresenter<VFXViewPresenter>();


            VFXContextUI endContext = null;
            foreach (var node in view.GetAllContexts())
            {
                if (node.worldBound.Contains(position))
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
                        case VFXContextType.kInit:
                            targetContextType = VFXContextType.kSpawner;
                            break;
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
                        case VFXContextType.kSpawner:
                            targetContextType = VFXContextType.kInit;
                            break;
                    }
                }

                if (targetContextType != VFXContextType.kNone)
                {
                    var contextDesc = VFXLibrary.GetContexts().FirstOrDefault(t => t.CreateInstance().contextType == targetContextType);
                    if (contextDesc != null)
                    {
                        VFXContext newContext = viewPresenter.AddVFXContext(view.contentViewContainer.GlobalToBound(position) - new Vector2(188, presenter.direction == Direction.Input ? 92 : 16), contextDesc);

                        VFXContextPresenter newContextPresenter = viewPresenter.elements.OfType<VFXContextPresenter>().FirstOrDefault(t => t.model == newContext);

                        VFXFlowEdgePresenter edgePresenter = VFXFlowEdgePresenter.CreateInstance<VFXFlowEdgePresenter>();
                        edgePresenter.input = presenter.direction == Direction.Input ? presenter : newContextPresenter.flowInputAnchors.First();
                        edgePresenter.output = presenter.direction == Direction.Output ? presenter : newContextPresenter.flowOutputAnchors.First();

                        viewPresenter.AddElement(edgePresenter);
                    }
                }
            }
        }
    }
}
