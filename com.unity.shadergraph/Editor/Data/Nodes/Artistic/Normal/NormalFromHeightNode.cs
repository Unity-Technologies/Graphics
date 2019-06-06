using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{

    enum OutputSpace
    {
        Tangent,
        World
    };

    [Title("Artistic", "Normal", "Normal From Height")]
    class NormalFromHeightNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireTangent, IMayRequireBitangent, IMayRequireNormal, IMayRequirePosition
    {
        public NormalFromHeightNode()
        {
            name = "Normal From Height";
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private OutputSpace m_OutputSpace = OutputSpace.Tangent;

        [EnumControl("Output Space")]
        public OutputSpace outputSpace
        {
            get { return m_OutputSpace; }
            set
            {
                if (m_OutputSpace == value)
                    return;

                m_OutputSpace = value;
                Dirty(ModificationScope.Graph);
            }
        }

        const int InputSlotId = 0;
        const int OutputSlotId = 1;
        const int InputStrength = 2;
        const string kInputSlotName = "In";
        const string kOutputSlotName = "Out";
        const string kInputStrength = "Strength";

        public override bool hasPreview
        {
            get { return true; }
        }

        string GetFunctionName()
        {
            return string.Format("Unity_NormalFromHeight2_{0}", outputSpace.ToString());
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, 0));
            // Strength of 2 is the value to match the old version of the NormalFromHeight node
            AddSlot(new Vector1MaterialSlot(InputStrength, kInputStrength, kInputStrength, SlotType.Input, 2));
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, InputStrength, OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            var inputValue = GetSlotValue(InputSlotId, generationMode);
            var outputValue = GetSlotValue(OutputSlotId, generationMode);
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputSlotId));
            sb.AppendLine("$precision3x3 _{0}_TangentMatrix = $precision3x3(IN.{1}SpaceTangent, IN.{1}SpaceBiTangent, IN.{1}SpaceNormal);", GetVariableNameForNode(), NeededCoordinateSpace.World.ToString());
            sb.AppendLine("$precision3 _{0}_Position = IN.{1}SpacePosition;", GetVariableNameForNode(), NeededCoordinateSpace.World.ToString());
            sb.AppendLine("$precision _{0}_scale = rcp({1});", GetVariableNameForNode(), GetVariableNameForSlot(InputStrength));
            sb.AppendLine("{0}({1}, _{2}_TangentMatrix, _{2}_scale, {3});", GetFunctionName(), inputValue, GetVariableNameForNode(), outputValue);
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("void {0}($precision In, $precision3x3 TangentMatrix, $precision scale, out $precision3 Out)", GetFunctionName());
                    using (s.BlockScope())
                    {
                        s.AppendLine("$precision3 partialDerivativeX = float3(scale, 0.0, ddx(In));");
                        s.AppendLine("$precision3 partialDerivativeY = float3(0.0, scale, ddy(In));");
                        s.AppendNewLine();
                        s.AppendLine("Out = normalize(cross(partialDerivativeX, partialDerivativeY));");

                        if (outputSpace == OutputSpace.World)
                            s.AppendLine("Out = TransformTangentToWorld(Out, TangentMatrix);");
                    }
                });
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.World;
        }

        public NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.World;
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.World;
        }
        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.World;
        }
	}
}
