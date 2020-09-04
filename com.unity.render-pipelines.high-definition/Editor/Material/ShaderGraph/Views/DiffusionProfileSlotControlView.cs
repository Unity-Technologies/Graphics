using UnityEditor.Graphing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.Drawing.Slots
{
    class DiffusionProfileSlotControlView : VisualElement
    {
        DiffusionProfileInputMaterialSlot m_Slot;

        ObjectField     diffusionProfileField;

        public DiffusionProfileSlotControlView(DiffusionProfileInputMaterialSlot slot)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("DiffusionProfileSlotControlView"));
            m_Slot = slot;
            diffusionProfileField = new ObjectField
            {
                value = m_Slot.diffusionProfile,
                allowSceneObjects = false,
                objectType = typeof(DiffusionProfileSettings)
            };
            diffusionProfileField.RegisterCallback<ChangeEvent<Object>>(RegisterValueChangedCallback, TrickleDown.NoTrickleDown);
            Add(diffusionProfileField);
        }

        void RegisterValueChangedCallback(ChangeEvent<UnityEngine.Object> evt)
        {
            if (evt.newValue != evt.previousValue)
            {
                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change Diffusion Profile");

                m_Slot.diffusionProfile = evt.newValue as DiffusionProfileSettings;
                m_Slot.owner.Dirty(ModificationScope.Graph);
            }
        }

        public void UpdateSlotValue()
        {
            diffusionProfileField.value = m_Slot.diffusionProfile;
        }
    }
}
