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
        int                     m_PropertyIndex;
        VFXNodeBlockPresenter   m_Presenter;
        VFXPropertyIM           m_Property;


        IMGUIContainer          m_Container;
        VisualContainer m_Slot;
        VisualElement           m_SlotIcon;

        static int s_ContextCount = 1;


        public VFXPropertyUI()
        {
            m_Slot = new VisualContainer();
            m_Slot.AddToClassList("slot");
            AddChild(m_Slot);
            m_SlotIcon = new VisualElement();
            m_Slot.AddChild(m_SlotIcon);
            m_Slot.clipChildren = false;
            clipChildren = false;

            m_Container = new IMGUIContainer();
            m_Container.OnGUIHandler = OnGUI;
            m_Container.executionContext = s_ContextCount++;
            AddChild(m_Container);

            m_Style = new GUIStyle();
            m_Style.active.background = Resources.Load<Texture2D>("VFX/SelectedField");
            m_Style.focused.background = m_Style.active.background;
        }

        public GUIStyle m_Style;


        void OnGUI()
        {
            // update the GUISTyle from the element style defined in USS
            m_Style.font = font;
            m_Style.fontSize = fontSize;
            m_Style.focused.textColor = m_Style.active.textColor = m_Style.normal.textColor = textColor;
            m_Style.border.top = m_Style.border.left = m_Style.border.right = m_Style.border.bottom = 4;
            m_Style.padding = new RectOffset(2,2,2,2);

            m_Property.OnGUI(m_Presenter, m_PropertyIndex, m_Style);

            if (Event.current.type != EventType.Layout && Event.current.type != EventType.Used)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                m_Container.height = r.yMax;
            }
        }

        public void DataChanged(VFXNodeBlockPresenter presenter, int propertyIndex)
        {
            m_PropertyIndex = propertyIndex;
            m_Presenter = presenter;
            m_Property = VFXPropertyIM.Create(presenter, propertyIndex);
        }
        
    }
}
