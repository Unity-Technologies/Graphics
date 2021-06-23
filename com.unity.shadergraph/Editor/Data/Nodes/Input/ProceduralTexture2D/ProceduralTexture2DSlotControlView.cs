using UnityEditor.Graphing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    class ProceduralTexture2DSlotControlView : VisualElement
    {
        StochasticTextureInputMaterialSlot m_Slot;
        ProceduralTexture2DInputMaterialSlot m_Slot2;

        ObjectField proceduralTexture2DField;

        public ProceduralTexture2DSlotControlView(StochasticTextureInputMaterialSlot slot)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("ProceduralTexture2DSlotControlView"));
            m_Slot = slot;
            proceduralTexture2DField = new ObjectField
            {
                value = m_Slot.texture,
                allowSceneObjects = false,
                objectType = typeof(ProceduralTexture2D)
            };
            proceduralTexture2DField.RegisterCallback<ChangeEvent<Object>>(RegisterValueChangedCallback, TrickleDown.NoTrickleDown);
            Add(proceduralTexture2DField);
        }

        public ProceduralTexture2DSlotControlView(ProceduralTexture2DInputMaterialSlot slot)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("ProceduralTexture2DSlotControlView"));
            m_Slot2 = slot;
            proceduralTexture2DField = new ObjectField
            {
                value = m_Slot2.proceduralTexture2D,
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
                if (m_Slot != null)
                {
                    m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change Stochastic Sampling Data");
                    m_Slot.texture = evt.newValue as ProceduralTexture2D;
                    m_Slot.owner.Dirty(ModificationScope.Graph);
                }
                else if (m_Slot2 != null)
                {
                    m_Slot2.owner.owner.owner.RegisterCompleteObjectUndo("Change Stochastic Sampling Data");
                    m_Slot2.proceduralTexture2D = evt.newValue as ProceduralTexture2D;
                    m_Slot2.owner.Dirty(ModificationScope.Graph);
                }
            }
        }

        public void UpdateSlotValue()
        {
            if (m_Slot != null)
                proceduralTexture2DField.value = m_Slot.texture;
            else if (m_Slot2 != null)
                proceduralTexture2DField.value = m_Slot2.proceduralTexture2D;
        }
    }
}
