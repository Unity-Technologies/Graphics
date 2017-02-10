using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.VFX.UI
{
    class VFXPropertyUI : VisualContainer
    {
        VFXNodeBlockPresenter.PropertyInfo m_PropertyInfo;
        VFXNodeBlockPresenter              m_Presenter;

        VFXPropertyIM                      m_Property;

        IMGUIContainer          m_Container;
        VisualContainer         m_Slot;
        VFXDataAnchor           m_SlotIcon;

        static int s_ContextCount = 1;


        GUIStyles m_GUIStyles = new GUIStyles();

        public VFXPropertyUI()
        {
            m_Slot = new VisualContainer();
            m_Slot.AddToClassList("slot");
            AddChild(m_Slot);
            m_Slot.clipChildren = false;
            clipChildren = false;

            m_Container = new IMGUIContainer();
            m_Container.OnGUIHandler = OnGUI;
            m_Container.executionContext = s_ContextCount++;
            AddChild(m_Container);

            m_GUIStyles.baseStyle = new GUIStyle();
            m_GUIStyles.baseStyle.active.background = Resources.Load<Texture2D>("VFX/SelectedField");
            m_GUIStyles.baseStyle.focused.background = m_GUIStyles.baseStyle.active.background;
            m_GUIStyles.baseStyle.border.top = m_GUIStyles.baseStyle.border.left = m_GUIStyles.baseStyle.border.right = m_GUIStyles.baseStyle.border.bottom = 4;
            m_GUIStyles.baseStyle.padding = new RectOffset(2, 2, 2, 2);
        }


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
                typeStyle.active.background = typeStyle.focused.background = null;
                typeStyle.onNormal.background = Resources.Load<Texture2D>("VFX/" + type.Name + "_minus");
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



        void OnGUI()
        {
            // update the GUISTyle from the element style defined in USS
            bool different = false;

            if( m_GUIStyles.baseStyle.font != font )
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

            m_Property.OnGUI(m_Presenter, ref m_PropertyInfo, m_GUIStyles);

            if (Event.current.type != EventType.Layout && Event.current.type != EventType.Used)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                m_Container.height = r.yMax;
            }
        }

        public void DataChanged(VFXNodeBlockUI nodeBlock,VFXNodeBlockPresenter.PropertyInfo info)
        {
            m_Presenter = nodeBlock.GetPresenter<VFXNodeBlockPresenter>();
            if( m_PropertyInfo.type != info.type)
            {
                m_Property = VFXPropertyIM.Create(info.type);
            }
            m_PropertyInfo = info;

            if( m_SlotIcon == null)
            {
                m_SlotIcon = VFXDataAnchor.Create<VFXDataEdgePresenter>(m_Presenter.GetPropertyPresenter(ref info));
                m_Slot.AddChild(m_SlotIcon);
            }
        }
        
    }
}
