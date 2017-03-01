using RMGUI.GraphView;
using UnityEngine.RMGUI.StyleSheets;
using UnityEngine;
using UnityEngine.RMGUI;
using System.Collections.Generic;
using Type = System.Type;

namespace UnityEditor.VFX.UI
{
    partial class VFXDataAnchor : NodeAnchor
    {
        VFXPropertyIM   m_Property;
        IMGUIContainer  m_Container;
        VisualElement m_SpaceButton;

        static int s_ContextCount = 1;

        public class GUIStyles
        {
            public GUIStyle baseStyle;

            public GUIStyle GetGUIStyleForExpandableType(Type type)
            {
                GUIStyle style = null;

                if (typeStyles.TryGetValue(type, out style))
                {
                    return style;
                }

                GUIStyle typeStyle = new GUIStyle(baseStyle);
                typeStyle.normal.background = Resources.Load<Texture2D>("VFX/" + type.Name + "_plus");
                if (typeStyle.normal.background == null)
                    typeStyle.normal.background = Resources.Load<Texture2D>("VFX/Default_plus");
                typeStyle.active.background = typeStyle.focused.background = null;
                typeStyle.onNormal.background = Resources.Load<Texture2D>("VFX/" + type.Name + "_minus");
                if (typeStyle.onNormal.background == null)
                    typeStyle.onNormal.background = Resources.Load<Texture2D>("VFX/Default_minus");
                typeStyle.border.top = 0;
                typeStyle.border.left = 0;
                typeStyle.border.bottom = typeStyle.border.right = 0;
                typeStyle.padding.top = 3;

                typeStyles.Add(type, typeStyle);


                return typeStyle;
            }

            public GUIStyle GetGUIStyleForType(Type type)
            {
                GUIStyle style = null;

                if (typeStyles.TryGetValue(type, out style))
                {
                    return style;
                }

                GUIStyle typeStyle = new GUIStyle(baseStyle);
                typeStyle.normal.background = Resources.Load<Texture2D>("VFX/" + type.Name);
                if (typeStyle.normal.background == null)
                    typeStyle.normal.background = Resources.Load<Texture2D>("VFX/Default");
                typeStyle.active.background = typeStyle.focused.background = null;
                typeStyle.border.top = 0;
                typeStyle.border.left = 0;
                typeStyle.border.bottom = typeStyle.border.right = 0;

                typeStyles.Add(type, typeStyle);


                return typeStyle;
            }

            Dictionary<Type, GUIStyle> typeStyles = new Dictionary<Type, GUIStyle>();

            public void Reset()
            {
                typeStyles.Clear();
            }

            public float lineHeight
            { get { return baseStyle.fontSize * 1.25f; } }
        }

        GUIStyles m_GUIStyles = new GUIStyles();

        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public static VFXDataAnchor Create<TEdgePresenter>(VFXDataAnchorPresenter presenter) where TEdgePresenter : VFXDataEdgePresenter
        {
            var anchor = new VFXDataAnchor(presenter) {
                m_EdgeConnector = new EdgeConnector<TEdgePresenter>()
            };
            anchor.presenter = presenter;
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        protected VFXDataAnchor(VFXDataAnchorPresenter presenter) : base(presenter)
        {
            m_Container = new IMGUIContainer() { name = "IMGUI" };
            m_Container.OnGUIHandler = OnGUI;
            m_Container.executionContext = s_ContextCount++;
            AddChild(m_Container);

            m_SpaceButton = new VisualElement();
            m_SpaceButton.AddManipulator(new Clickable(SwitchSpace));
            m_SpaceButton.AddToClassList("space");

            clipChildren = false;

            

            m_GUIStyles.baseStyle = new GUIStyle();

            m_Property = VFXPropertyIM.Create(presenter.propertyInfo.type);
        }
        void SwitchSpace()
        {
            Spaceable spaceable = (Spaceable)GetPresenter<VFXDataAnchorPresenter>().propertyInfo.value;

            spaceable.space = (CoordinateSpace)((int)(spaceable.space + 1) % (int)CoordinateSpace.SpaceCount);

            GetPresenter<VFXDataAnchorPresenter>().SetPropertyValue(spaceable);
        }
        void OnGUI()
        {
            // update the GUISTyle from the element style defined in USS
            bool different = false;

            if (m_GUIStyles.baseStyle.font != font)
            {
                m_GUIStyles.baseStyle.font = font;
                different = true;
            }
            if (m_GUIStyles.baseStyle.fontSize != fontSize)
            {
                m_GUIStyles.baseStyle.fontSize = fontSize;
                different = true;
            }
            if (m_GUIStyles.baseStyle.focused.textColor != textColor)
            {
                m_GUIStyles.baseStyle.focused.textColor = m_GUIStyles.baseStyle.active.textColor = m_GUIStyles.baseStyle.normal.textColor = textColor;
                different = true;
            }

            if (different)
                m_GUIStyles.Reset();

            bool changed = m_Property.OnGUI(GetPresenter<VFXDataAnchorPresenter>(), m_GUIStyles);

            if (changed)
            {
                Dirty(ChangeType.Transform | ChangeType.Repaint);
            }

            if (Event.current.type != EventType.Layout && Event.current.type != EventType.Used)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                m_Container.height = r.yMax;
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            m_ConnectorText.text = "";

            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();

            // reverse because we want the flex to choose the position of the connector
            presenter.position = position;

            if (presenter.connected)
                AddToClassList("connected");
            else
                RemoveFromClassList("connected");

            // update the css type of the class
            m_ConnectorBox.RemoveFromClassList(VFXTypeDefinition.GetTypeCSSClasses());
            m_ConnectorBox.AddToClassList(VFXTypeDefinition.GetTypeCSSClass(presenter.anchorType));

            switch (presenter.direction)
            {
                case Direction.Input:
                    AddToClassList("InputEdgeConnector");
                    break;
                case Direction.Output:
                    AddToClassList("OutputEdgeConnector");
                    break;
            }


            if (typeof(Spaceable).IsAssignableFrom(presenter.propertyInfo.type))
            {
                if (m_SpaceButton.parent == null)
                {
                    AddChild(m_SpaceButton);
                }

                CoordinateSpace space = ((Spaceable)presenter.propertyInfo.value).space;
                m_SpaceButton.text = space.ToString();

                foreach (string spaceName in System.Enum.GetNames(typeof(CoordinateSpace)))
                {
                    m_SpaceButton.RemoveFromClassList(spaceName.ToLower());
                }

                m_SpaceButton.AddToClassList(space.ToString().ToLower());
                m_SpaceButton.Dirty(ChangeType.Styles | ChangeType.Repaint);
            }
            else
            {
                if (m_SpaceButton.parent != null)
                {
                    RemoveChild(m_SpaceButton);
                }
            }


            clipChildren = false;
        }
        
        public override bool ContainsPoint(Vector2 localPoint)
        {
            return position.Contains(localPoint);
            //return GraphElement.ContainsPoint(localPoint);
            // Here local point comes without position offset...
            //localPoint -= position.position;
            //return m_ConnectorBox.ContainsPoint(m_ConnectorBox.transform.MultiplyPoint3x4(localPoint));
        }

    }
}
