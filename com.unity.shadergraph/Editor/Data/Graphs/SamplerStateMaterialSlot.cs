using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class SamplerStateMaterialSlot : MaterialSlot
    {
        public SamplerStateMaterialSlot()
        {
        }

        public SamplerStateMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
        }

        [SerializeField]
        bool m_BareResource = false;
        internal override bool bareResource
        {
            get { return m_BareResource; }
            set { m_BareResource = value; }
        }

        // NOT serialized -- this is always set by the parent node if they care about it
        public TextureSamplerState defaultSamplerState { get; set; }
        public string defaultSamplerStateName => defaultSamplerState?.defaultPropertyName ?? "SamplerState_Linear_Repeat";

        public override void AppendHLSLParameterDeclaration(ShaderStringBuilder sb, string paramName)
        {
            if (m_BareResource)
            {
                // we have to use our modified macro declaration here, to ensure that something is declared for GLES2 platforms
                // (the standard SAMPLER macro doesn't declare anything, so the commas will be messed up in the parameter list)
                sb.Append("UNITY_BARE_SAMPLER(");
                sb.Append(paramName);
                sb.Append(")");
            }
            else
                base.AppendHLSLParameterDeclaration(sb, paramName);
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            var nodeOwner = owner as AbstractMaterialNode;
            if (nodeOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            return $"UnityBuildSamplerStateStruct({defaultSamplerStateName})";
        }

        public override SlotValueType valueType { get { return SlotValueType.SamplerState; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.SamplerState; } }
        public override bool isDefaultValue => true;
        
        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            var nodeOwner = owner as AbstractMaterialNode;
            if (nodeOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            properties.AddShaderProperty(new SamplerStateShaderProperty()
            {
                value = defaultSamplerState ?? new TextureSamplerState()
                {
                    filter = TextureSamplerState.FilterMode.Linear,
                    wrap = TextureSamplerState.WrapMode.Repeat
                },
                overrideReferenceName = defaultSamplerStateName,
                generatePropertyBlock = false,
            });
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {}


        public override void CopyDefaultValue(MaterialSlot other)
        {
            base.CopyDefaultValue(other);
            if (other is SamplerStateMaterialSlot ms)
            {
                defaultSamplerState = ms.defaultSamplerState;
            }
        }
    }
}
