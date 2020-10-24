using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class UV4MaterialSlot : Vector4MaterialSlot, IMayRequireMeshUV
    {
        [SerializeField]
        UVChannel m_Channel = UVChannel.UV0;

        public UVChannel channel
        {
            get { return m_Channel; }
            set { m_Channel = value; }
        }

        public override bool isDefaultValue => channel == UVChannel.UV0;

        public UV4MaterialSlot()
        { }

        public UV4MaterialSlot(int slotId, string displayName, string shaderOutputName, UVChannel channel,
                              ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector4.zero, stageCapability, hidden: hidden)
        {
            this.channel = channel;
        }

        public override VisualElement InstantiateControl()
        {
            return new UV4SlotControlView(this);
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return string.Format("IN.{0}", channel.GetUVName());
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            if (isConnected)
                return false;

            return m_Channel == channel;
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as UVMaterialSlot;
            if (slot != null)
                channel = slot.channel;
        }
    }

    [Serializable]
    class UVMaterialSlot : Vector2MaterialSlot, IMayRequireMeshUV
    {
        [SerializeField]
        UVChannel m_Channel = UVChannel.UV0;

        public UVChannel channel
        {
            get { return m_Channel; }
            set { m_Channel = value; }
        }

        public override bool isDefaultValue => channel == UVChannel.UV0;

        public UVMaterialSlot()
        {}

        public UVMaterialSlot(int slotId, string displayName, string shaderOutputName, UVChannel channel,
                              ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector2.zero, stageCapability, hidden: hidden)
        {
            this.channel = channel;
        }

        public override VisualElement InstantiateControl()
        {
            return new UVSlotControlView(this);
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return string.Format("IN.{0}.xy", channel.GetUVName());
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            if (isConnected)
                return false;

            return m_Channel == channel;
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as UVMaterialSlot;
            if (slot != null)
                channel = slot.channel;
        }
    }
}
