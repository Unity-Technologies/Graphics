using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using System.Collections.Generic;
using Type = System.Type;

namespace UnityEditor.VFX.UI
{
    partial class VFXOutputDataAnchor : VFXDataAnchor
    {
        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public static new VFXOutputDataAnchor Create(VFXDataAnchorController controller, VFXNodeUI node)
        {
            var anchor = new VFXOutputDataAnchor(controller.orientation, controller.direction, controller.portType, node);

            anchor.m_EdgeConnector = new EdgeConnector<VFXDataEdge>(anchor);
            anchor.controller = controller;
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        Texture2D[] m_Icons;
        VisualElement m_Icon;

        protected VFXOutputDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type, VFXNodeUI node) : base(anchorOrientation, anchorDirection, type, node)
        {
            m_Icon = new VisualElement()
            {
                name = "icon"
            };

            Add(new VisualElement() { name = "lineSpacer" });
            AddToClassList("VFXOutputDataAnchor");
            Insert(0, m_Icon); //insert at first ( right since reversed)
        }

        void OnToggleExpanded()
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

        public override void SelfChange(int change)
        {
            base.SelfChange(change);

            if (controller.depth != 0 && m_Lines == null)
            {
                m_Lines = new VisualElement[controller.depth + 1];

                for (int i = 0; i < controller.depth; ++i)
                {
                    var line = new VisualElement();
                    line.style.width = 1;
                    line.name = "line";
                    line.style.marginLeft = 0.5f * PropertyRM.depthOffset;
                    line.style.marginRight = PropertyRM.depthOffset * 0.5f;

                    Insert(2, line);
                    m_Lines[i] = line;
                }
            }


            if (controller.expandable)
            {
                if (m_Icons == null)
                    m_Icons = new Texture2D[]
                    {
                        Resources.Load<Texture2D>("VFX/plus"),
                        Resources.Load<Texture2D>("VFX/minus")
                    };

                m_Icon.style.backgroundImage = controller.expandedSelf ? m_Icons[1] : m_Icons[0];

                m_Icon.AddManipulator(new Clickable(OnToggleExpanded));
            }
            else
            {
                m_Icon.style.backgroundImage = null;
            }


            string text = "";
            string tooltip = null;
            VFXPropertyAttribute.ApplyToGUI(controller.attributes, ref text, ref tooltip);

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
