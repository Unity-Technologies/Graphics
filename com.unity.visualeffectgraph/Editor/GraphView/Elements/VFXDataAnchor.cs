using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Profiling;

using Type = System.Type;

using PositionType = UnityEngine.UIElements.Position;

namespace UnityEditor.VFX.UI
{
    class VFXDataAnchor : Port, IControlledElement<VFXDataAnchorController>, IEdgeConnectorListener
    {
        VFXDataAnchorController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXDataAnchorController controller
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
                    if(m_Controller.model != null)
                        m_Controller.model.RefreshErrors(m_Controller.sourceNode.viewController.graph);
                    m_Controller.RegisterHandler(this);
                }
            }
        }

        VFXNodeUI m_Node;

        public new VFXNodeUI node
        {
            get {return m_Node; }
        }

        protected VFXDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type, VFXNodeUI node) : base(anchorOrientation, anchorDirection, Capacity.Multi, type)
        {
            Profiler.BeginSample("VFXDataAnchor.VFXDataAnchor");
            this.AddStyleSheetPath("VFXDataAnchor");
            AddToClassList("VFXDataAnchor");
            this.AddStyleSheetPath("VFXTypeColor");

            m_Node = node;

            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
            Profiler.EndSample();
        }

        public VisualElement connector
        {
            get { return m_ConnectorBox; }
        }


        public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            var op = controller.sourceNode.model as VFXOperatorNumericCascadedUnified;

            if (op != null)
                evt.menu.AppendAction("Remove Slot", OnRemove, e => op.operandCount > 2 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        void OnRemove(DropdownMenuAction e)
        {
            var op = controller.sourceNode as VFXCascadedOperatorController;

            op.RemoveOperand(controller);
        }

        public static VFXDataAnchor Create(VFXDataAnchorController controller, VFXNodeUI node)
        {
            var anchor = new VFXDataAnchor(controller.orientation, controller.direction, controller.portType, node);
            anchor.m_EdgeConnector = new VFXEdgeConnector(anchor);
            anchor.controller = controller;

            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        bool m_EdgeDragging;
        public override void OnStartEdgeDragging()
        {
            m_EdgeDragging = true;
            highlight = false;
        }

        public override void OnStopEdgeDragging()
        {
            m_EdgeDragging = false;
            highlight = true;
        }

        void OnMouseEnter(MouseEnterEvent e)
        {
            if (m_EdgeDragging && !highlight)
                e.PreventDefault();
        }

        void OnMouseLeave(MouseLeaveEvent e)
        {
            if (m_EdgeDragging && !highlight)
                e.PreventDefault();
        }

        public override bool collapsed
        {
            get { return !controller.expandedInHierachy; }
        }

        IEnumerable<VFXDataEdge> GetAllEdges()
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();

            foreach (var edgeController in controller.connections)
            {
                VFXDataEdge edge = view.GetDataEdgeByController(edgeController as VFXDataEdgeController);
                if (edge != null)
                    yield return edge;
            }
        }

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                SelfChange(e.change);
            }
        }

        public virtual void SelfChange(int change)
        {
            if (change != VFXDataAnchorController.Change.hidden)
            {
                if (controller.connected)
                    AddToClassList("connected");
                else
                    RemoveFromClassList("connected");

                portType = controller.portType;


                string className = VFXTypeDefinition.GetTypeCSSClass(controller.portType);
                // update the css type of the class
                foreach (var cls in VFXTypeDefinition.GetTypeCSSClasses())
                {
                    if (cls != className)
                    {
                        m_ConnectorBox.RemoveFromClassList(cls);
                        RemoveFromClassList(cls);
                    }
                }

                AddToClassList(className);
                m_ConnectorBox.AddToClassList(className);

                AddToClassList("EdgeConnector");

                switch (controller.direction)
                {
                    case Direction.Input:
                        AddToClassList("Input");
                        break;
                    case Direction.Output:
                        AddToClassList("Output");
                        break;
                }

                portName = "";
            }

            if (controller.expandedInHierachy)
            {
                style.display = DisplayStyle.Flex;
                RemoveFromClassList("hidden");
            }
            else
            {
                style.display = DisplayStyle.None;
                AddToClassList("hidden");
            }

            UpdateCapColor();


            if (controller.direction == Direction.Output)
                m_ConnectorText.text = controller.name;
            else
                m_ConnectorText.text = "";
        }

        void IEdgeConnectorListener.OnDrop(GraphView graphView, Edge edge)
        {
            VFXView view = graphView as VFXView;
            VFXDataEdge dataEdge = edge as VFXDataEdge;
            VFXDataEdgeController edgeController = new VFXDataEdgeController(dataEdge.input.controller, dataEdge.output.controller);

            view.controller.AddElement(edgeController);
        }

        public override void Disconnect(Edge edge)
        {
            base.Disconnect(edge);
            UpdateCapColor();
        }

        void IEdgeConnectorListener.OnDropOutsidePort(Edge edge, Vector2 position)
        {
            VFXSlot startSlot = controller.model;

            VFXView view = this.GetFirstAncestorOfType<VFXView>();
            VFXViewController viewController = view.controller;


            List<VisualElement> picked = new List<VisualElement>();
            panel.PickAll(position, picked);
            VFXNodeUI endNode = null;

            foreach (var element in picked)
            {
                if (element is VFXNodeUI node)
                {
                    endNode = node;
                    break;
                }
            }


            VFXDataEdge dataEdge  = edge as VFXDataEdge;
            bool exists = false;
            if (dataEdge.controller != null)
            {
                exists = true;
                view.controller.RemoveElement(dataEdge.controller);
            }

            if (endNode != null)
            {
                VFXNodeController nodeController = endNode.controller;

                if (nodeController != null)
                {
                    IVFXSlotContainer slotContainer = nodeController.slotContainer;
                    if (controller.direction == Direction.Input)
                    {
                        foreach (var output in nodeController.outputPorts.Where(t => t.model == null || t.model.IsMasterSlot()))
                        {
                            if (viewController.CreateLink(controller, output))
                                break;
                        }
                    }
                    else
                    {
                        foreach (var input in nodeController.inputPorts.Where(t => t.model == null || t.model.IsMasterSlot() && !t.model.HasLink(true)))
                        {
                            if (viewController.CreateLink(input, controller))
                                break;
                        }
                    }
                }
            }
            else if (controller.direction == Direction.Input && Event.current.modifiers == EventModifiers.Alt)
            {
                VFXModelDescriptorParameters parameterDesc = VFXLibrary.GetParameters().FirstOrDefault(t => t.name == controller.portType.UserFriendlyName());
                if (parameterDesc != null)
                {
                    Vector2 pos = view.contentViewContainer.GlobalToBound(position) - new Vector2(140, 20);
                    VFXParameter parameter = viewController.AddVFXParameter(pos, parameterDesc, false);
                    parameter.SetSettingValue("m_Exposed", true);
                    startSlot.Link(parameter.outputSlots[0]);

                    CopyValueToParameter(parameter);

                    viewController.AddVFXModel(pos, parameter);
                }
            }
            else if (!exists)
            {
                if (direction == Direction.Input || viewController.model.visualEffectObject is VisualEffectSubgraphOperator || viewController.model.visualEffectObject is VisualEffectSubgraphBlock) // no context for subgraph operators.
                    VFXFilterWindow.Show(VFXViewWindow.currentWindow, Event.current.mousePosition, view.ViewToScreenPosition(Event.current.mousePosition), new VFXNodeProvider(viewController, AddLinkedNode, ProviderFilter, new Type[] { typeof(VFXOperator), typeof(VFXParameter)}));
                else
                VFXFilterWindow.Show(VFXViewWindow.currentWindow, Event.current.mousePosition, view.ViewToScreenPosition(Event.current.mousePosition), new VFXNodeProvider(viewController, AddLinkedNode, ProviderFilter, new Type[] { typeof(VFXOperator), typeof(VFXParameter), typeof(VFXContext) }));
            }
        }

        bool ProviderFilter(VFXNodeProvider.Descriptor d)
        {
            var mySlot = controller.model;
            var parameterDescriptor = d.modelDescriptor as VFXParameterController;
            IVFXSlotContainer container = null;
            if (parameterDescriptor != null)
            {
                container = parameterDescriptor.model;
            }
            else
            {
                VFXModelDescriptor desc = d.modelDescriptor as VFXModelDescriptor;
                if (desc == null)
                {
                    string path = d.modelDescriptor as string;

                    if (path != null && !path.StartsWith(VisualEffectAssetEditorUtility.templatePath))
                    {
                        if (Path.GetExtension(path) == VisualEffectSubgraphOperator.Extension)
                        {
                            var subGraph = AssetDatabase.LoadAssetAtPath<VisualEffectSubgraphOperator>(path);
                            if (subGraph != null && (!controller.viewController.model.isSubgraph || !subGraph.GetResource().GetOrCreateGraph().subgraphDependencies.Contains(controller.viewController.model.subgraph) && subGraph.GetResource() != controller.viewController.model))
                                return true;
                        }
                    }
                    return false;
                }

                container = desc.model as IVFXSlotContainer;
                if (container == null)
                    return false;

                if (direction == Direction.Output
                    &&  mySlot != null
                    && container is VFXOperatorDynamicOperand
                    && (container as VFXOperatorDynamicOperand).validTypes.Contains(mySlot.property.type))
                        return true;
            }

            IEnumerable<Type> validTypes = null;
            if (mySlot == null)
            {
                var op = controller.sourceNode.model as VFXOperatorDynamicOperand;
                if (op != null)
                    validTypes = op.validTypes;
            }

            var getSlots = direction == Direction.Input ? (System.Func<int, VFXSlot>)container.GetOutputSlot : (System.Func<int, VFXSlot>)container.GetInputSlot;
            int count = direction == Direction.Input ? container.GetNbOutputSlots() : container.GetNbInputSlots();
            for (int i = 0; i < count; ++i)
            {
                var slot = getSlots(i);
                if (mySlot != null && slot.CanLink(mySlot))
                    return true;
                else if (validTypes != null && validTypes.Contains(slot.property.type))
                    return true;
            }
            return false;
        }

        void AddLinkedNode(VFXNodeProvider.Descriptor d, Vector2 mPos)
        {
            var mySlot = controller.model;

            VFXView view = GetFirstAncestorOfType<VFXView>();
            VFXViewController viewController = controller.viewController;
            if (view == null) return;

            var newNodeController = view.AddNode(d, mPos);

            if (newNodeController == null)
                return;

            IEnumerable<Type> validTypes = null;

            var op = controller.sourceNode.model as VFXOperatorNumericCascadedUnified;
            if (mySlot == null && op != null)
            {
                validTypes = op.validTypes;
            }

            // If linking to a new parameter, copy the slot value and space
            if (direction == Direction.Input && controller.model != null) //model will be null for upcomming which won't have a value
            {
                if (newNodeController is VFXOperatorController)
                {
                    var inlineOperator = (newNodeController as VFXOperatorController).model as VFXInlineOperator;
                    if (inlineOperator != null)
                    {
                        var value = controller.model.value;
                        object convertedValue = null;
                        if (VFXConverter.TryConvertTo(value, inlineOperator.type, out convertedValue))
                        {
                            inlineOperator.inputSlots[0].value = convertedValue;
                        }

                        if (inlineOperator.inputSlots[0].spaceable && controller.model.spaceable)
                        {
                            inlineOperator.inputSlots[0].space = controller.model.space;
                        }
                    }
                }
            }

            var ports = direction == Direction.Input ? newNodeController.outputPorts : newNodeController.inputPorts;
            int count = ports.Count();
            for (int i = 0; i < count; ++i)
            {
                var port = ports[i];
                if (mySlot != null)
                {
                    if (viewController.CreateLink(direction == Direction.Input ? controller : port, direction == Direction.Input ? port : controller))
                    {
                        break;
                    }
                }
                else if (validTypes != null)
                {
                    if (validTypes.Contains(port.model.property.type))
                    {
                        if (viewController.CreateLink(controller, port))
                        {
                            break;
                        }
                    }
                }
            }
        }

        void CopyValueToParameter(VFXParameter parameter)
        {
            var value = controller.model.value;
            object convertedValue = null;
            if (VFXConverter.TryConvertTo(value, parameter.type, out convertedValue))
            {
                parameter.value = convertedValue;
            }
        }
    }
}
