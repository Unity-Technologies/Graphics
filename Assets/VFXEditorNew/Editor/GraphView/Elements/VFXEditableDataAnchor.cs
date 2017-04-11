using UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using System.Collections.Generic;
using Type = System.Type;

namespace UnityEditor.VFX.UI
{
    partial class VFXEditableDataAnchor : VFXDataAnchor
    {
        VFXPropertyIM   m_PropertyIM;
        IMGUIContainer  m_Container;
        GUIStyles m_GUIStyles = null;


        PropertyRM      m_PropertyRM;

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


        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public static new VFXEditableDataAnchor Create<TEdgePresenter>(VFXDataAnchorPresenter presenter) where TEdgePresenter : VFXDataEdgePresenter
        {
            var anchor = new VFXEditableDataAnchor(presenter) {
                m_EdgeConnector = new EdgeConnector<TEdgePresenter>()
            };
            anchor.presenter = presenter;
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        protected VFXEditableDataAnchor(VFXDataAnchorPresenter presenter) : base(presenter)
        {
            clipChildren = false;

            m_PropertyRM = PropertyRM.Create(presenter);
            if(m_PropertyRM != null)
            {
                AddChild(m_PropertyRM);
            }
            else
            {
                m_GUIStyles = new GUIStyles();
                m_GUIStyles.baseStyle = new GUIStyle();
                m_PropertyIM = VFXPropertyIM.Create(presenter.anchorType);

                m_Container = new IMGUIContainer(OnGUI) { name = "IMGUI" };
                AddChild(m_Container);
            }
        }
        void OnGUI()
        {
            // update the GUISTyle from the element style defined in USS


            //try
            {
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

                bool changed = m_PropertyIM.OnGUI(GetPresenter<VFXBlockDataAnchorPresenter>(), m_GUIStyles);

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

            if (m_PropertyRM != null)
            {
                m_PropertyRM.enabled = ! presenter.connected;
                m_PropertyRM.Update();
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
