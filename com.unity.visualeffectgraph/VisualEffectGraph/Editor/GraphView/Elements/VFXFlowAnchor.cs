using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements;
using UnityEngine;
using System.Linq;
using Type = System.Type;

namespace UnityEditor.VFX.UI
{
    class VFXFlowAnchor : Port, IControlledElement<VFXFlowAnchorPresenter>, IEdgeConnectorListener
    {
        VFXFlowAnchorPresenter m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXFlowAnchorPresenter controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != null)
                {
                    m_Controller.UnregisterHandler(this);
                }
                m_Controller = value;
                if (m_Controller != null)
                {
                    m_Controller.RegisterHandler(this);
                }
            }
        }

        public static VFXFlowAnchor Create(VFXFlowAnchorPresenter presenter)
        {
            var anchor = new VFXFlowAnchor(presenter.orientation, presenter.direction, typeof(int));
            anchor.m_EdgeConnector = new EdgeConnector<VFXFlowEdge>(anchor);
            anchor.AddManipulator(anchor.m_EdgeConnector);
            anchor.controller = presenter;
            return anchor;
        }

        protected VFXFlowAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type) : base(anchorOrientation, anchorDirection, type)
        {
            AddToClassList("EdgeConnector");
        }

        void OnChange(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                m_ConnectorText.text = "";

                VFXFlowAnchorPresenter presenter = controller;

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
        }

        public override Vector3 GetGlobalCenter()
        {
            return this.LocalToWorld(new Vector3(this.contentRect.center.x, direction == Direction.Output ? this.contentRect.yMax : this.contentRect.yMin, 0));
        }

        void IEdgeConnectorListener.OnDrop(GraphView graphView, Edge edge)
        {
            VFXView view = graphView as VFXView;
            VFXFlowEdge flowEdge = edge as VFXFlowEdge;
            VFXFlowEdgePresenter edgePresenter = VFXFlowEdgePresenter.CreateInstance<VFXFlowEdgePresenter>();
            edgePresenter.Init(flowEdge.input.controller, flowEdge.output.controller);
            flowEdge.controller = edgePresenter;

            view.controller.AddElement(edgePresenter);
        }

        bool ProviderFilter(VFXNodeProvider.Descriptor d)
        {
            if (!(d.modelDescriptor is VFXModelDescriptor<VFXContext>)) return false;

            var desc = d.modelDescriptor as VFXModelDescriptor<VFXContext>;

            if (direction == Direction.Input)
            {
                return VFXContext.CanLink(desc.model, controller.context.context);
            }
            else
            {
                return VFXContext.CanLink(controller.context.context, desc.model);
            }
        }

        void AddLinkedContext(VFXNodeProvider.Descriptor d, Vector2 mPos)
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();
            if (view == null) return;
            Vector2 tPos = view.ChangeCoordinatesTo(view.contentViewContainer, mPos);

            VFXContext context = view.controller.AddVFXContext(tPos, d.modelDescriptor as VFXModelDescriptor<VFXContext>);

            if (context == null) return;


            if (direction == Direction.Input)
            {
                controller.context.context.LinkFrom(context, 0, controller.slotIndex);
            }
            else
            {
                controller.context.context.LinkTo(context, controller.slotIndex, 0);
            }
        }

        void IEdgeConnectorListener.OnDropOutsidePort(Edge edge, Vector2 position)
        {
            VFXFlowAnchorPresenter presenter = controller;

            VFXView view = this.GetFirstAncestorOfType<VFXView>();
            VFXViewPresenter viewPresenter = view.controller;


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
                VFXContextPresenter nodePresenter = endContext.controller;

                var compatibleAnchors = viewPresenter.GetCompatiblePorts(presenter, null);

                if (presenter.direction == Direction.Input)
                {
                    foreach (var outputAnchor in nodePresenter.flowOutputAnchors)
                    {
                        if (compatibleAnchors.Contains(outputAnchor))
                        {
                            VFXFlowEdgePresenter edgePresenter = VFXFlowEdgePresenter.CreateInstance<VFXFlowEdgePresenter>();
                            edgePresenter.Init(presenter, outputAnchor);

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
                            edgePresenter.Init(inputAnchor, presenter);

                            viewPresenter.AddElement(edgePresenter);
                            break;
                        }
                    }
                }
            }
            else
            {
                VFXFilterWindow.Show(Event.current.mousePosition, new VFXNodeProvider(AddLinkedContext, ProviderFilter, new Type[] { typeof(VFXContext)}));
            }
            /*
            else if (Event.current.modifiers == EventModifiers.Alt)
            {
                VFXContextType targetContextType = VFXContextType.kNone;
                //TODO create the most obvious context and link
                if (presenter.direction == Direction.Input)
                {
                    switch (presenter.owner.contextType)
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
                    switch (presenter.owner.contextType)
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
            }*/
        }
    }
}
