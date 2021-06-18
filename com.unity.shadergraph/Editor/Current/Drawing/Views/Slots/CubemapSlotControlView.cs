using System;
using UnityEditor.Graphing;
using UnityEngine;
using Object = UnityEngine.Object;

using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    class CubemapSlotControlView : VisualElement
    {
        CubemapInputMaterialSlot m_Slot;

        public CubemapSlotControlView(CubemapInputMaterialSlot slot)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/CubemapSlotControlView"));
            m_Slot = slot;
            var objectField = new ObjectField { objectType = typeof(Cubemap), value = m_Slot.cubemap };
            objectField.RegisterValueChangedCallback(OnValueChanged);
            Add(objectField);
        }

        void OnValueChanged(ChangeEvent<Object> evt)
        {
            var cubemap = evt.newValue as Cubemap;
            if (cubemap != m_Slot.cubemap)
            {
                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change Cubemap");
                m_Slot.cubemap = cubemap;
                m_Slot.owner.Dirty(ModificationScope.Node);
            }
        }
    }
}
