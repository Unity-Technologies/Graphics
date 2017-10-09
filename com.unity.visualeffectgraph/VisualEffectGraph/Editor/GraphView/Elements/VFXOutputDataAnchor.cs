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
        public static new VFXOutputDataAnchor Create(VFXDataAnchorPresenter presenter)
        {
            var anchor = new VFXOutputDataAnchor(presenter.orientation, presenter.direction, presenter.anchorType);

            anchor.m_EdgeConnector = new EdgeConnector<VFXDataEdge>(anchor);
            anchor.presenter = presenter;
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        Texture2D[] m_Icons;
        VisualElement m_Icon;

        protected VFXOutputDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type) : base(anchorOrientation, anchorDirection, type)
        {
            m_Icon = new VisualElement()
            {
                name = "icon"
            };

            Add(m_Icon); //insert between text and connector
        }

        void OnToggleExpanded()
        {
            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();

            if (presenter.expanded)
            {
                presenter.RetractPath();
            }
            else
            {
                presenter.ExpandPath();
            }
        }

        VisualElement[] m_Lines;

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();


            if (presenter.depth != 0 && m_Lines == null)
            {
                m_Lines = new VisualElement[presenter.depth];
                for (int i = 0; i < presenter.depth; ++i)
                {
                    var line = new VisualElement();
                    line.style.width = 1;
                    line.name = "line";
                    line.style.marginLeft = 0.5f * VFXPropertyIM.depthOffset;
                    line.style.marginRight = VFXPropertyIM.depthOffset * 0.5f;

                    Insert(0, line);
                    m_Lines[i] = line;
                }
            }


            if (presenter.expandable)
            {
                if (m_Icons == null)
                    m_Icons = new Texture2D[2];

                m_Icons[0] = GetTypeIcon(presenter.anchorType, IconType.plus);
                m_Icons[1] = GetTypeIcon(presenter.anchorType, IconType.minus);

                m_Icon.style.backgroundImage = presenter.expanded ? m_Icons[1] : m_Icons[0];

                m_Icon.AddManipulator(new Clickable(OnToggleExpanded));
            }
            else
            {
                m_Icon.style.backgroundImage = GetTypeIcon(presenter.anchorType, IconType.simple);
            }

            if (presenter.expandable)
                m_Icon.style.backgroundImage = presenter.expanded ? m_Icons[1] : m_Icons[0];


            string text = "";
            string tooltip = null;
            VFXPropertyAttribute.ApplyToGUI(presenter.attributes, ref text, ref tooltip);

            this.AddTooltip(tooltip);
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return rect.Contains(localPoint);
        }
    }
}
