using System;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    struct CoordinateSpaceConversion : IEnumConversion
    {
        public CoordinateSpace from;
        public CoordinateSpace to;

        public CoordinateSpaceConversion(CoordinateSpace from, CoordinateSpace to)
        {
            this.from = from;
            this.to = to;
        }

        Enum IEnumConversion.from
        {
            get { return from; }
            set { from = (CoordinateSpace)value; }
        }

        Enum IEnumConversion.to
        {
            get { return to; }
            set { to = (CoordinateSpace)value; }
        }
    }

    [Title("Math", "Vector", "Transform")]
    class TransformNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireTangent, IMayRequireBitangent, IMayRequireNormal, IMayRequireTransform, IMayRequirePosition
    {
        public override int latestVersion => 2;

        private const int InputSlotId = 0;
        private const int OutputSlotId = 1;
        private const string kInputSlotName = "In";
        private const string kOutputSlotName = "Out";

        public TransformNode()
        {
            name = "Transform";
            synonyms = new string[] { "world", "tangent", "object", "view", "screen", "convert" };
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        CoordinateSpaceConversion m_Conversion = new CoordinateSpaceConversion(CoordinateSpace.Object, CoordinateSpace.World);

        [EnumConversionControl]
        public CoordinateSpaceConversion conversion
        {
            get { return m_Conversion; }
            set
            {
                if (Equals(m_Conversion, value))
                    return;
                m_Conversion = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        public ConversionType m_ConversionType = ConversionType.Position;

        [EnumControl("Type")]
        public ConversionType conversionType
        {
            get { return m_ConversionType; }
            set
            {
                if (Equals(m_ConversionType, value))
                    return;
                m_ConversionType = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_Normalize = true;
        public bool normalize
        {
            get { return m_Normalize; }
            set
            {
                if (Equals(m_Normalize, value))
                    return;
                m_Normalize = value;
                Dirty(ModificationScope.Graph);
            }
        }

        internal SpaceTransform spaceTransform => new SpaceTransform(conversion.from, conversion.to, conversionType, normalize, sgVersion);

        public override bool hasPreview
        {
            get { return true; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlotId }, new[] { OutputSlotId });

            string inputValue = $"{GetSlotValue(InputSlotId, generationMode)}.xyz";
            string outputVariable = GetVariableNameForSlot(OutputSlotId);
            string outputType = FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString();

            // declare output variable and fill it out
            sb.AddLine(outputType, " ", outputVariable, ";");
            SpaceTransformUtil.GenerateTransformCodeStatement(spaceTransform, inputValue, outputVariable, sb);
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
        {
            return spaceTransform.RequiresTangent;
        }

        public NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability)
        {
            return spaceTransform.RequiresBitangent;
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            return spaceTransform.RequiresNormal;
        }

        public NeededTransform[] RequiresTransform(ShaderStageCapability stageCapability)
        {
            return spaceTransform.RequiresTransform.ToArray();
        }

        NeededCoordinateSpace IMayRequirePosition.RequiresPosition(ShaderStageCapability stageCapability)
        {
            if (sgVersion > 1)
                return spaceTransform.RequiresPosition;

            return NeededCoordinateSpace.None;
        }
    }
}
