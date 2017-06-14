using UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using System.Collections.Generic;
using Type = System.Type;

namespace UnityEditor.VFX.UI
{
    partial class VFXOutputDataAnchor : VFXDataAnchor
    {
        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public static new VFXOutputDataAnchor Create<TEdgePresenter>(VFXDataAnchorPresenter presenter) where TEdgePresenter : VFXDataEdgePresenter
        {
            var anchor = new VFXOutputDataAnchor(presenter);

            anchor.m_EdgeConnector = new EdgeConnector<TEdgePresenter>(anchor);
            anchor.presenter = presenter;
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        Texture2D[] m_Icons;
        VisualElement m_Icon;

        protected VFXOutputDataAnchor(VFXDataAnchorPresenter presenter) : base(presenter)
        {
            clipChildren = false;

            m_Icon = new VisualElement()
            {
                name = "icon"
            };

            AddChild(m_Icon); //insert between text and connector

            if (presenter.expandable)
            {
                m_Icons = new Texture2D[2];
                m_Icons[0] = GetTypeIcon(presenter.anchorType, IconType.plus);
                m_Icons[1] = GetTypeIcon(presenter.anchorType, IconType.minus);

                m_Icon.backgroundImage = presenter.expanded ? m_Icons[1] : m_Icons[0];

                m_Icon.AddManipulator(new Clickable(OnToggleExpanded));
            }
            else
            {
                m_Icon.backgroundImage = GetTypeIcon(presenter.anchorType, IconType.simple);
            }
            if (presenter.depth != 0)
            {
                for (int i = 0; i < presenter.depth; ++i)
                {
                    VisualElement line = new VisualElement()
                    {
                        width = 1,
                        name = "line",
                        marginLeft = 0.5f * VFXPropertyIM.depthOffset,
                        marginRight = VFXPropertyIM.depthOffset * 0.5f
                    };
                    InsertChild(0, line);
                }
            }
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

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();

            clipChildren = false;
            if (presenter.expandable)
                m_Icon.backgroundImage = presenter.expanded ? m_Icons[1] : m_Icons[0];
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return layout.Contains(localPoint);
            //return GraphElement.ContainsPoint(localPoint);
            // Here local point comes without position offset...
            //localPoint -= position.position;
            //return m_ConnectorBox.ContainsPoint(m_ConnectorBox.transform.MultiplyPoint3x4(localPoint));
        }
    }
}
