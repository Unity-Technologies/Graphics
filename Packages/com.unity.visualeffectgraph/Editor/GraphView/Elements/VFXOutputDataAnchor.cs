using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

using Type = System.Type;

namespace UnityEditor.VFX.UI
{
    class VFXOutputDataAnchor : VFXDataAnchor
    {
        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public new static VFXOutputDataAnchor Create(VFXDataAnchorController controller, VFXNodeUI node)
        {
            var anchor = new VFXOutputDataAnchor(controller.orientation, controller.direction, controller.portType, node);

            anchor.m_EdgeConnector = new VFXEdgeConnector(anchor);
            anchor.controller = controller;
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        protected VFXOutputDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type, VFXNodeUI node) : base(anchorOrientation, anchorDirection, type, node)
        {
            AddToClassList("VFXOutputDataAnchor");
        }

        void OnToggleExpanded(PointerDownEvent evt)
        {
            if (controller.expandedSelf)
            {
                controller.RetractPath();
            }
            else
            {
                controller.ExpandPath();
            }
        }

        VisualElement[] m_Lines;


        Clickable m_ExpandClickable;

        public override void SelfChange(int change)
        {
            base.SelfChange(change);

            if (controller.depth != 0 && m_Lines == null)
            {
                AddToClassList("hasDepth");
                m_Lines = new VisualElement[controller.depth - 1];
                for (int i = 0; i < controller.depth - 1; ++i)
                {
                    var line = new VisualElement();
                    line.style.width = 1;
                    line.name = "line";
                    line.style.marginRight = 0;

                    Insert(childCount - 1, line);
                    m_Lines[i] = line;
                }
            }


            if (controller.expandable)
            {
                if (controller.expandedSelf)
                {
                    AddToClassList("icon-expanded");
                }
                else
                {
                    RemoveFromClassList("icon-expanded");
                }
                AddToClassList("expandable");

                if (m_ExpandClickable == null)
                {
                    var label = this.Q<Label>("type");
                    label.pickingMode = PickingMode.Position;
                    label.RegisterCallback<PointerDownEvent>(OnToggleExpanded, TrickleDown.TrickleDown);
                }
            }
            else
            {
                if (m_ExpandClickable != null)
                {
                    var label = this.Q<Label>("type");
                    label.pickingMode = PickingMode.Ignore;
                    label.UnregisterCallback<PointerDownEvent>(OnToggleExpanded, TrickleDown.TrickleDown);
                    m_ExpandClickable = null;
                }
                RemoveFromClassList("expandable");
            }


            string text = "";
            string tooltip = null;
            controller.attributes.ApplyToGUI(ref text, ref tooltip);

            this.tooltip = tooltip;
        }

        public Rect internalRect
        {
            get
            {
                Rect layout = this.layout;
                return new Rect(0.0f, 0.0f, layout.width, layout.height);
            }
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return internalRect.Contains(localPoint);
        }
    }
}
