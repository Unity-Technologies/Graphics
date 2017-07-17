using UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Collections.Generic;
using Type = System.Type;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXDataAnchor : NodeAnchor, IEdgeConnectorListener
    {
        VisualElement m_ConnectorHighlight;

        protected VFXDataAnchor(VFXDataAnchorPresenter presenter) : base(presenter)
        {
            AddToClassList("VFXDataAnchor");

            m_ConnectorHighlight = new VisualElement()
            {
                positionType = PositionType.Absolute,
                positionTop = 0,
                positionLeft = 0,
                positionBottom = 0,
                positionRight = 0,
                pickingMode = PickingMode.Ignore
            };

            VisualContainer connector = m_ConnectorBox as VisualContainer;

            connector.AddChild(m_ConnectorHighlight);
        }

        protected override VisualElement CreateConnector()
        {
            return new VisualContainer();
        }

        public static VFXDataAnchor Create<TEdgePresenter>(VFXDataAnchorPresenter presenter) where TEdgePresenter : VFXDataEdgePresenter
        {
            var anchor = new VFXDataAnchor(presenter);
            anchor.m_EdgeConnector = new EdgeConnector<TEdgePresenter>(anchor);
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
                m_ConnectorBox.RemoveFromClassList(cls);

            m_ConnectorBox.AddToClassList(VFXTypeDefinition.GetTypeCSSClass(presenter.anchorType));


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


            if (presenter.collapsed && !presenter.connected)
            {
                visible = false;

                AddToClassList("invisible");
            }
            else if (!visible)
            {
                visible = true;
                RemoveFromClassList("hidden");
                RemoveFromClassList("invisible");
            }

            if (presenter.direction == Direction.Output)
                m_ConnectorText.text = presenter.name;

            clipChildren = false;
        }

        public Vector3 GetLocalCenter()
        {
            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();

            var center = m_ConnectorBox.layout.position + new Vector2(presenter.direction == Direction.Input ? 1 : m_ConnectorBox.layout.width - 1, m_ConnectorBox.layout.height * 0.5f - 0.5f);
            center = m_ConnectorBox.transform.matrix.MultiplyPoint3x4(center);

            return center;
        }

        public override Vector3 GetGlobalCenter()
        {
            return this.LocalToGlobal(GetLocalCenter());
        }

        void IEdgeConnectorListener.OnDropOutsideAnchor(EdgePresenter edge, Vector2 position)
        {
            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();

            VFXSlot startSlot = presenter.model;


            VFXView view = this.GetFirstAncestorOfType<VFXView>();
            VFXViewPresenter viewPresenter = view.GetPresenter<VFXViewPresenter>();


            Node endNode = null;
            foreach (var node in view.GetAllNodes())
            {
                if (node.globalBound.Contains(position))
                {
                    endNode = node;
                }
            }

            if (endNode != null)
            {
                VFXSlotContainerPresenter nodePresenter = endNode.GetPresenter<VFXSlotContainerPresenter>();

                var compatibleAnchors = nodePresenter.viewPresenter.GetCompatibleAnchors(presenter, null);

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
            }
            else if (presenter.direction == Direction.Input && Event.current.modifiers == EventModifiers.Alt)
            {
                VFXModelDescriptorParameters parameterDesc = VFXLibrary.GetParameters().FirstOrDefault(t => t.name == presenter.anchorType.UserFriendlyName());
                if (parameterDesc != null)
                {
                    VFXParameter parameter = viewPresenter.AddVFXParameter(view.contentViewContainer.GlobalToBound(position) - new Vector2(360, 0), parameterDesc);
                    startSlot.Link(parameter.outputSlots[0]);
                }
            }
        }
    }
}
