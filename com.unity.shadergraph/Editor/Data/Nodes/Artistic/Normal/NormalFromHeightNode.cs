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
        const int StrengthSlotId = 2;
        const int OutputSlotId = 1;
        const string kInputSlotName = "In";
        const string kStrengthSlotName = "Strength";
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
            AddSlot(new Vector1MaterialSlot(StrengthSlotId, kStrengthSlotName, kStrengthSlotName, SlotType.Input, 0.01f));
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, StrengthSlotId, OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var inputValue = GetSlotValue(InputSlotId, generationMode);
            var strengthValue = GetSlotValue(StrengthSlotId, generationMode);
            var outputValue = GetSlotValue(OutputSlotId, generationMode);
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputSlotId));
            sb.AppendLine("$precision3x3 _{0}_TangentMatrix = $precision3x3(IN.{1}SpaceTangent, IN.{1}SpaceBiTangent, IN.{1}SpaceNormal);", GetVariableNameForNode(), NeededCoordinateSpace.World.ToString());
            sb.AppendLine("$precision3 _{0}_Position = IN.{1}SpacePosition;", GetVariableNameForNode(), NeededCoordinateSpace.World.ToString());
            sb.AppendLine("{0}({1},{2},_{3}_Position,_{3}_TangentMatrix, {4});", GetFunctionName(), inputValue, strengthValue, GetVariableNameForNode(), outputValue);
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
            {
                s.AppendLine("void {0}({1} In, {2} Strength, $precision3 Position, $precision3x3 TangentMatrix, out {3} Out)",
                    GetFunctionName(),
                    FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType.ToShaderString(),
                    FindInputSlot<MaterialSlot>(StrengthSlotId).concreteValueType.ToShaderString(),
                    FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString());
                using (s.BlockScope())
                {
                    s.AppendLine("$precision3 worldDerivativeX = ddx(Position);");
                    s.AppendLine("$precision3 worldDerivativeY = ddy(Position);");
                    s.AppendNewLine();
                    s.AppendLine("$precision3 crossX = cross(TangentMatrix[2].xyz, worldDerivativeX);");
                    s.AppendLine("$precision3 crossY = cross(worldDerivativeY, TangentMatrix[2].xyz);");
                    s.AppendLine("$precision d = dot(worldDerivativeX, crossY);");
                    s.AppendLine("$precision sgn = d < 0.0 ? (-1.0f) : 1.0f;");
                    s.AppendLine("$precision surface = sgn / max(0.000000000000001192093f, abs(d));");
                    s.AppendNewLine();
                    s.AppendLine("$precision dHdx = ddx(In);");
                    s.AppendLine("$precision dHdy = ddy(In);");
                    s.AppendLine("$precision3 surfGrad = surface * (dHdx*crossY + dHdy*crossX);");
                    s.AppendLine("Out = SafeNormalize(TangentMatrix[2].xyz - (Strength * surfGrad));");

                    if (outputSpace == OutputSpace.Tangent)
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
