using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Collections.Generic;
using Type = System.Type;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXDataAnchor : Port, IEdgeConnectorListener
    {
        VisualElement m_ConnectorHighlight;

        protected VFXDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type) : base(anchorOrientation, anchorDirection, type)
        {
            AddToClassList("VFXDataAnchor");

            m_ConnectorHighlight = new VisualElement();

            m_ConnectorHighlight.style.positionType = PositionType.Absolute;
            m_ConnectorHighlight.style.positionTop = 0;
            m_ConnectorHighlight.style.positionLeft = 0;
            m_ConnectorHighlight.style.positionBottom = 0;
            m_ConnectorHighlight.style.positionRight = 0;
            m_ConnectorHighlight.pickingMode = PickingMode.Ignore;

            VisualElement connector = m_ConnectorBox as VisualElement;

            connector.Add(m_ConnectorHighlight);
        }

        protected override VisualElement CreateConnector()
        {
            return new VisualElement();
        }

        public static VFXDataAnchor Create(VFXDataAnchorPresenter presenter)
        {
            var anchor = new VFXDataAnchor(presenter.orientation, presenter.direction, presenter.portType);
            anchor.m_EdgeConnector = new EdgeConnector<VFXDataEdge>(anchor);
            anchor.presenter = presenter;

            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        public enum IconType
        {
            plus,
            minus,
            simple
        }

        public static Texture2D GetTypeIcon(Type type, IconType iconType)
        {
            string suffix = "";
            switch (iconType)
            {
                case IconType.plus:
                    suffix = "_plus";
                    break;
                case IconType.minus:
                    suffix = "_minus";
                    break;
            }

            Texture2D result = Resources.Load<Texture2D>("VFX/" + type.Name + suffix);
            if (result == null)
                return Resources.Load<Texture2D>("VFX/Default" + suffix);
            return result;
        }

        const string AnchorColorProperty = "anchor-color";
        StyleValue<Color> m_AnchorColor;


        public Color anchorColor { get { return m_AnchorColor.GetSpecifiedValueOrDefault(GetPresenter<VFXDataAnchorPresenter>().direction == Direction.Input ? Color.red : Color.green); } }
        protected override void OnStyleResolved(ICustomStyle styles)
        {
            base.OnStyleResolved(styles);


            Color prevColor = m_AnchorColor.value;
            styles.ApplyCustomProperty(AnchorColorProperty, ref m_AnchorColor);
            if (m_AnchorColor.value != prevColor)
            {
                foreach (var edge in GetAllEdges())
                {
                    edge.OnPortChanged(direction == Direction.Input);
                }
            }
        }

        IEnumerable<VFXDataEdge> GetAllEdges()
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();

            foreach (var edgePresenter in GetPresenter<VFXDataAnchorPresenter>().connections)
            {
                VFXDataEdge edge = view.GetDataEdgeByPresenter(edgePresenter as VFXDataEdgePresenter);
                if (edge != null)
                    yield return edge;
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            m_ConnectorText.text = "";

            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();

            // reverse because we want the flex to choose the position of the connector
            presenter.position = layout;

            if (presenter.connected)
                AddToClassList("connected");
            else
                RemoveFromClassList("connected");


            // update the css type of the class
            foreach (var cls in VFXTypeDefinition.GetTypeCSSClasses())
            {
                m_ConnectorBox.RemoveFromClassList(cls);
                RemoveFromClassList(cls);
            }

            string className = VFXTypeDefinition.GetTypeCSSClass(presenter.portType);
            AddToClassList(className);
            m_ConnectorBox.AddToClassList(className);


            if (presenter.connections.FirstOrDefault(t => t.selected) != null)
            {
                AddToClassList("selected");
            }
            else
            {
                RemoveFromClassList("selected");
            }

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


            RemoveFromClassList("hidden");
            RemoveFromClassList("invisible");

            if (presenter.collapsed && !presenter.connected)
            {
                visible = false;

                AddToClassList("hidden");
                AddToClassList("invisible");
            }
            else if (!visible)
            {
                visible = true;
            }

            if (presenter.direction == Direction.Output)
                m_ConnectorText.text = presenter.name;
        }

        void IEdgeConnectorListener.OnDrop(GraphView graphView, Edge edge)
        {
            EdgePresenter edgePresenter = ScriptableObject.CreateInstance<VFXDataEdgePresenter>();
            edge.presenter = edgePresenter;
            edgePresenter.input = edge.input.GetPresenter<VFXDataAnchorPresenter>();
            edgePresenter.output = edge.output.GetPresenter<VFXDataAnchorPresenter>();

            graphView.GetPresenter<VFXViewPresenter>().AddElement(edgePresenter);
        }

        void IEdgeConnectorListener.OnDropOutsidePort(Edge edge, Vector2 position)
        {
            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();

            VFXSlot startSlot = presenter.model;

            VFXView view = this.GetFirstAncestorOfType<VFXView>();
            VFXViewPresenter viewPresenter = view.GetPresenter<VFXViewPresenter>();


            Node endNode = null;
            foreach (var node in view.GetAllNodes())
            {
                if (node.worldBound.Contains(position))
                {
                    endNode = node;
                }
            }

            if (endNode != null)
            {
                VFXSlotContainerPresenter nodePresenter = endNode.GetPresenter<VFXSlotContainerPresenter>();

                var compatibleAnchors = nodePresenter.viewPresenter.GetCompatiblePorts(presenter, null);

                if (nodePresenter != null)
                {
                    IVFXSlotContainer slotContainer = nodePresenter.slotContainer;
                    if (presenter.direction == Direction.Input)
                    {
                        foreach (var outputSlot in slotContainer.outputSlots)
                        {
                            var endPresenter = nodePresenter.allChildren.OfType<VFXDataAnchorPresenter>().First(t => t.model == outputSlot);
                            if (compatibleAnchors.Contains(endPresenter))
                            {
                                startSlot.Link(outputSlot);
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (var inputSlot in slotContainer.inputSlots)
                        {
                            var endPresenter = nodePresenter.allChildren.OfType<VFXDataAnchorPresenter>().First(t => t.model == inputSlot);
                            if (compatibleAnchors.Contains(endPresenter))
                            {
                                inputSlot.Link(startSlot);
                                break;
                            }
                        }
                    }
                }
                else
                {
                }
            }
            else if (presenter.direction == Direction.Input && Event.current.modifiers == EventModifiers.Alt)
            {
                VFXModelDescriptorParameters parameterDesc = VFXLibrary.GetParameters().FirstOrDefault(t => t.name == presenter.portType.UserFriendlyName());
                if (parameterDesc != null)
                {
                    VFXParameter parameter = viewPresenter.AddVFXParameter(view.contentViewContainer.GlobalToBound(position) - new Vector2(360, 0), parameterDesc);
                    startSlot.Link(parameter.outputSlots[0]);
                }
            }
        }

        public override void DoRepaint()
        {
            base.DoRepaint();
        }
    }
}
