using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class PortInputView : GraphElement, IDisposable
    {
        MaterialSlot m_Slot;
        ConcreteSlotValueType m_SlotType;
        VisualElement m_Control;
        VisualElement m_ControlContainer;

        public PortInputView(MaterialSlot slot)
        {
            m_Slot = slot;
            ClearClassList();
            m_SlotType = slot.concreteValueType;
            m_ControlContainer = new VisualElement { name = "controlContainer" };
            Add(m_ControlContainer);
            m_Control = m_Slot.InstantiateControl();
            if (m_Control != null)
                m_ControlContainer.Add(m_Control);
            else
                m_ControlContainer.visible = false;
        }

        public void UpdateSlotType()
        {
            if (m_Slot.concreteValueType != m_SlotType)
            {
                m_SlotType = m_Slot.concreteValueType;
                if (m_Control != null)
                {
                    var disposable = m_Control as IDisposable;
                    if (disposable != null)
                        disposable.Dispose();
                    m_ControlContainer.Remove(m_Control);
                }
                m_Control = m_Slot.InstantiateControl();
                m_ControlContainer.visible = true;
                if (m_Control != null)
                    m_ControlContainer.Add(m_Control);
                else
                    m_ControlContainer.visible = false;
            }
        }

        public void Dispose()
        {
            var disposable = m_Control as IDisposable;
            if (disposable != null)
                disposable.Dispose();
        }
    }
}
