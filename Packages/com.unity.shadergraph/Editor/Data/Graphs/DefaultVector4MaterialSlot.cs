using System;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine.UIElements;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class DefaultVector4MaterialSlot : Vector4MaterialSlot
    {
        public DefaultVector4MaterialSlot()
        { }

        public DefaultVector4MaterialSlot(int slotId, string displayName, string shaderOutputName, string defaultLabel = "Default",
            ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector4.zero, stageCapability, hidden: hidden)
        {
            m_DefaultLabel = defaultLabel;
        }

        [SerializeField]
        string m_DefaultLabel = "Default";

        public string defaultLabel
        {
            get => m_DefaultLabel;
            set => m_DefaultLabel = value;
        }

        public override VisualElement InstantiateControl()
        {
            return new LabelSlotControlView(m_DefaultLabel);
        }
    }
}
