using UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using System.Collections.Generic;
using Type = System.Type;

namespace UnityEditor.VFX.UI
{
    public class VFXDataGUIStyles
    {
        public GUIStyle baseStyle;

        public VFXDataGUIStyles()
        {
            baseStyle = new GUIStyle();
        }

        public void ConfigureForElement(VisualElement elem)
        {
            bool different = false;

            if (baseStyle.font != elem.font)
            {
                baseStyle.font = elem.font;
                different = true;
            }
            if (baseStyle.fontSize != elem.fontSize)
            {
                baseStyle.fontSize = elem.fontSize;
                different = true;
            }
            if (baseStyle.focused.textColor != elem.textColor)
            {
                baseStyle.focused.textColor = baseStyle.active.textColor = baseStyle.normal.textColor = elem.textColor;
                different = true;
            }

            if (different)
            {
                Reset();
            }
        }

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

        static Dictionary<Type, GUIStyle> typeStyles = new Dictionary<Type, GUIStyle>();

        public void Reset()
        {
            typeStyles.Clear();
        }

        public float lineHeight
        { get { return baseStyle.fontSize * 1.25f; } }
    }

    partial class VFXEditableDataAnchor : VFXDataAnchor
    {
        VFXPropertyIM   m_PropertyIM;
        IMGUIContainer  m_Container;
        VFXDataGUIStyles m_GUIStyles = null;


        PropertyRM      m_PropertyRM;


        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public static new VFXEditableDataAnchor Create<TEdgePresenter>(VFXDataAnchorPresenter presenter) where TEdgePresenter : VFXDataEdgePresenter
        {
            var anchor = new VFXEditableDataAnchor(presenter);

            anchor.m_EdgeConnector = new EdgeConnector<TEdgePresenter>(anchor);
            anchor.presenter = presenter;
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        protected VFXEditableDataAnchor(VFXDataAnchorPresenter presenter) : base(presenter)
        {
            clipChildren = false;

            m_PropertyRM = PropertyRM.Create(presenter, 100);
            if (m_PropertyRM != null)
            {
                AddChild(m_PropertyRM);
            }
            else
            {
                m_GUIStyles = new VFXDataGUIStyles();
                m_PropertyIM = VFXPropertyIM.Create(presenter.anchorType, 100);

                m_Container = new IMGUIContainer(OnGUI) { name = "IMGUI" };
                AddChild(m_Container);
            }
        }

        void OnGUI()
        {
            // update the GUISTyle from the element style defined in USS


            //try
            {
                m_GUIStyles.ConfigureForElement(this);

                bool changed = m_PropertyIM.OnGUI(GetPresenter<VFXDataAnchorPresenter>(), m_GUIStyles);

                if (changed)
                {
                    Dirty(ChangeType.Transform | ChangeType.Repaint);
                }

                if (Event.current.type != EventType.Layout && Event.current.type != EventType.Used)
                {
                    /*  Rect r = GUILayoutUtility.GetLastRect();
                    m_Container.height = r.yMax;*/
                }
            }
            /*catch(System.Exception e)
            {
                Debug.LogError(e.Message);
            }*/
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();
            if (m_Container != null)
                m_Container.executionContext = presenter.GetInstanceID();

            OnRecompile();

            clipChildren = false;
        }

        public void OnRecompile()
        {
            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();
            if (m_PropertyRM != null && presenter != null)
            {
                m_PropertyRM.enabled = presenter.editable && !presenter.collapsed;
                m_PropertyRM.Update();
            }
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
