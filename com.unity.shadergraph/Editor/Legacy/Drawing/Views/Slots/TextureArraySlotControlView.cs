using System;
using UnityEditor.Graphing;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    class TextureArraySlotControlView : VisualElement
    {
        Texture2DArrayInputMaterialSlot m_Slot;

        public TextureArraySlotControlView(Texture2DArrayInputMaterialSlot slot)
        {
            m_Slot = slot;
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/TextureArraySlotControlView"));
            var objectField = new ObjectField { objectType = typeof(Texture2DArray), value = m_Slot.textureArray };
            objectField.RegisterValueChangedCallback(OnValueChanged);
            Add(objectField);
        }

        void OnValueChanged(ChangeEvent<Object> evt)
        {
            var textureArray = evt.newValue as Texture2DArray;
            if (textureArray != m_Slot.textureArray)
            {
                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change Texture Array");
                m_Slot.textureArray = textureArray;
                m_Slot.owner.Dirty(ModificationScope.Node);
            }
        }
    }
}
