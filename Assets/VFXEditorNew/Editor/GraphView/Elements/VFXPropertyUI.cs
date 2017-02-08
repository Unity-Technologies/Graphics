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
        }


        void OnGUI()
        {
            Font savedFont = GUI.skin.font;

            m_Property.OnGUI(m_Presenter, m_PropertyIndex,this);

            if (Event.current.type != EventType.Layout && Event.current.type != EventType.Used)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                m_Container.height = r.yMax;
            }

            GUI.skin.font = savedFont;
        }

        public void DataChanged(VFXNodeBlockPresenter presenter, int propertyIndex)
        {
            m_PropertyIndex = propertyIndex;
            m_Presenter = presenter;
            m_Property = VFXPropertyIM.Create(presenter, propertyIndex);
        }
        
    }
}
