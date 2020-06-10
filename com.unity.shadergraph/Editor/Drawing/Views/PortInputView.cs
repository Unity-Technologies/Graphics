using System;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class PortInputView : GraphElement, IDisposable
    {
        readonly CustomStyleProperty<Color> k_EdgeColorProperty = new CustomStyleProperty<Color>("--edge-color");

        Color m_EdgeColor = Color.red;

        public Color edgeColor
        {
            get { return m_EdgeColor; }
        }

        public MaterialSlot slot
        {
            get { return m_Slot; }
        }

        MaterialSlot m_Slot;
        ConcreteSlotValueType m_SlotType;
        VisualElement m_Control;
        VisualElement m_Container;
        EdgeControl m_EdgeControl;

        public PortInputView(MaterialSlot slot)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/PortInputView"));
            pickingMode = PickingMode.Ignore;
            ClearClassList();
            m_Slot = slot;
            m_SlotType = slot.concreteValueType;
            AddToClassList("type" + m_SlotType);

            m_EdgeControl = new EdgeControl
            {
                @from = new Vector2(232f - 21f, 11.5f),
                to = new Vector2(232f, 11.5f),
                edgeWidth = 2,
                pickingMode = PickingMode.Ignore
            };
            Add(m_EdgeControl);

            m_Container = new VisualElement { name = "container" };
            {
                CreateControl();

                var slotElement = new VisualElement { name = "slot" };
                {
                    slotElement.Add(new VisualElement { name = "dot" });
                }
                m_Container.Add(slotElement);
            }
            Add(m_Container);

            m_Container.Add(new VisualElement() { name = "disabledOverlay", pickingMode = PickingMode.Ignore });
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            Color colorValue;

            if (e.customStyle.TryGetValue(k_EdgeColorProperty, out colorValue))
                m_EdgeColor = colorValue;

            m_EdgeControl.UpdateLayout();
            m_EdgeControl.inputColor = edgeColor;
            m_EdgeControl.outputColor = edgeColor;
        }

        public void UpdateSlot(MaterialSlot newSlot)
        {
            m_Slot = newSlot;
            Recreate();
        }

        public void UpdateSlotType()
        {
            if (slot.concreteValueType != m_SlotType)
                Recreate();
        }

        void Recreate()
        {
            RemoveFromClassList("type" + m_SlotType);
            m_SlotType = slot.concreteValueType;
            AddToClassList("type" + m_SlotType);
            if (m_Control != null)
            {
                if (m_Control is IDisposable disposable)
                    disposable.Dispose();
                m_Container.Remove(m_Control);
            }
            CreateControl();
        }

        void CreateControl()
        {
            m_Control = slot.InstantiateControl();
            if (m_Control != null)
            {
                m_Container.Insert(0, m_Control);
            }
            else
            {
                // Some slot types don't support an input control, so hide this
                m_Container.visible = m_EdgeControl.visible = false;
            }
        }

        public void Dispose()
        {
            if (m_Control is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
