using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
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
        const string kInputSlotName = "In";
        const string kOutputSlotName = "Out";

        public override bool hasPreview
        {
            get { return true; }
        }

        string GetFunctionName()
        {
            return $"Unity_NormalFromHeight_{outputSpace.ToString()}_{concretePrecision.ToShaderString()}";
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, 0));
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var inputValue = GetSlotValue(InputSlotId, generationMode);
            var outputValue = GetSlotValue(OutputSlotId, generationMode);
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputSlotId));
            sb.AppendLine("$precision3x3 _{0}_TangentMatrix = $precision3x3(IN.{1}SpaceTangent, IN.{1}SpaceBiTangent, IN.{1}SpaceNormal);", GetVariableNameForNode(), NeededCoordinateSpace.World.ToString());
            sb.AppendLine("$precision3 _{0}_Position = IN.{1}SpacePosition;", GetVariableNameForNode(), NeededCoordinateSpace.World.ToString());
            sb.AppendLine("{0}({1},_{2}_Position,_{2}_TangentMatrix, {3});", GetFunctionName(), inputValue, GetVariableNameForNode(), outputValue);
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("void {0}({1} In, $precision3 Position, $precision3x3 TangentMatrix, out {2} Out)",
                        GetFunctionName(),
                        FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType.ToShaderString(),
                        FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString());
                    using (s.BlockScope())
                    {
                        s.AppendLine("$precision3 worldDirivativeX = ddx(Position * 100);");
                        s.AppendLine("$precision3 worldDirivativeY = ddy(Position * 100);");
                        s.AppendNewLine();
                        s.AppendLine("$precision3 crossX = cross(TangentMatrix[2].xyz, worldDirivativeX);");
                        s.AppendLine("$precision3 crossY = cross(TangentMatrix[2].xyz, worldDirivativeY);");
                        s.AppendLine("$precision3 d = abs(dot(crossY, worldDirivativeX));");
                        s.AppendLine("$precision3 inToNormal = ((((In + ddx(In)) - In) * crossY) + (((In + ddy(In)) - In) * crossX)) * sign(d);");
                        s.AppendLine("inToNormal.y *= -1.0;");
                        s.AppendNewLine();
                        s.AppendLine("Out = SafeNormalize((d * TangentMatrix[2].xyz) - inToNormal);");

                        if(outputSpace == OutputSpace.Tangent)
                            s.AppendLine("Out = TransformWorldToTangent(Out, TangentMatrix);");
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
