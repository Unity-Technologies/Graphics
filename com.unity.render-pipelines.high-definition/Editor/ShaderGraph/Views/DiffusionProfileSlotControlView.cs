using UnityEditor.Graphing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.Experimental.Rendering.HDPipeline.Drawing.Slots
{
    class DiffusionProfileSlotControlView : VisualElement
    {
        DiffusionProfileInputMaterialSlot m_Slot;

        PopupField<string> popupField;

        public DiffusionProfileSlotControlView(DiffusionProfileInputMaterialSlot slot)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("DiffusionProfileSlotControlView"));
            m_Slot = slot;
            popupField = new PopupField<string>(m_Slot.diffusionProfile.popupEntries, m_Slot.diffusionProfile.selectedEntry);
            popupField.RegisterValueChangedCallback(RegisterValueChangedCallback);
            Add(popupField);
        }

        void RegisterValueChangedCallback(ChangeEvent<string> evt)
        {
            var selectedIndex = popupField.index;

           if (selectedIndex != m_Slot.diffusionProfile.selectedEntry)
           {
                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change Diffusion Profile");

                PopupList popupList = m_Slot.diffusionProfile;
                popupList.selectedEntry = selectedIndex;
                m_Slot.diffusionProfile = popupList;
                m_Slot.owner.Dirty(ModificationScope.Graph);
           }
        }
    }
}
