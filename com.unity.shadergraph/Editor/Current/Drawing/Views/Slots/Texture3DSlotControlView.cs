using System;
using UnityEditor.Graphing;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    class Texture3DSlotControlView : VisualElement
    {
        Texture3DInputMaterialSlot m_Slot;

        public Texture3DSlotControlView(Texture3DInputMaterialSlot slot)
        {
            m_Slot = slot;
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/Texture3DSlotControlView"));
            var objectField = new ObjectField { objectType = typeof(Texture3D), value = m_Slot.texture };
            objectField.RegisterValueChangedCallback(OnValueChanged);
            Add(objectField);
        }

        void OnValueChanged(ChangeEvent<Object> evt)
        {
            var texture = evt.newValue as Texture3D;
            if (texture != m_Slot.texture)
            {
                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change Texture");
                m_Slot.texture = texture;
                m_Slot.owner.Dirty(ModificationScope.Node);
            }
        }
    }
}
