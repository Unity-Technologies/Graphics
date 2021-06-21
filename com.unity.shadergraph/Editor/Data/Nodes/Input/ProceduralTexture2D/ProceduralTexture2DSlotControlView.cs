using UnityEditor.Graphing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    class ProceduralTexture2DSlotControlView : VisualElement
    {
        ProceduralTexture2DInputMaterialSlot m_Slot;

        ObjectField proceduralTexture2DField;

        public ProceduralTexture2DSlotControlView(ProceduralTexture2DInputMaterialSlot slot)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("ProceduralTexture2DSlotControlView"));
            m_Slot = slot;
            proceduralTexture2DField = new ObjectField
            {
                value = m_Slot.proceduralTexture2D,
                allowSceneObjects = false,
                objectType = typeof(ProceduralTexture2D)
            };
            proceduralTexture2DField.RegisterCallback<ChangeEvent<Object>>(RegisterValueChangedCallback, TrickleDown.NoTrickleDown);
            Add(proceduralTexture2DField);
        }

        void RegisterValueChangedCallback(ChangeEvent<UnityEngine.Object> evt)
        {
            if (evt.newValue != evt.previousValue)
            {
                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change Stochastic Sampling Data");

                m_Slot.proceduralTexture2D = evt.newValue as ProceduralTexture2D;
                m_Slot.owner.Dirty(ModificationScope.Graph);
            }
        }

        public void UpdateSlotValue()
        {
            proceduralTexture2DField.value = m_Slot.proceduralTexture2D;
        }
    }
}
