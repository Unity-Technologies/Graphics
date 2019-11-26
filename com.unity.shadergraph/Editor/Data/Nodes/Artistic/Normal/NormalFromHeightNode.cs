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
            get => m_OutputSpace;
            set
            {
                if (m_OutputSpace == value)
                    return;

                m_OutputSpace = value;
                Dirty(ModificationScope.Graph);
            }
        }

        const int k_InputSlotId = 0;
        const int k_OutputSlotId = 1;
        const int k_InputStrengthId = 2;
        const string k_InputSlotName = "In";
        const string k_OutputSlotName = "Out";
        const string k_InputStrengthName = "Strength";

        public override bool hasPreview => true;

        string GetFunctionName() => $"Unity_NormalFromHeight_{outputSpace.ToString()}_{concretePrecision.ToShaderString()}";

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(k_InputSlotId, k_InputSlotName, k_InputSlotName, SlotType.Input, 0));
            AddSlot(new Vector1MaterialSlot(k_InputStrengthId, k_InputStrengthName, k_InputStrengthName, SlotType.Input, 1));
            AddSlot(new Vector3MaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { k_InputSlotId, k_InputStrengthId, k_OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var inputValue = GetSlotValue(k_InputSlotId, generationMode);
            var outputValue = GetSlotValue(k_OutputSlotId, generationMode);
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(k_OutputSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(k_OutputSlotId));
            sb.AppendLine("$precision3x3 _{0}_TangentMatrix = $precision3x3(IN.{1}SpaceTangent, IN.{1}SpaceBiTangent, IN.{1}SpaceNormal);", GetVariableNameForNode(), NeededCoordinateSpace.World.ToString());
            sb.AppendLine("$precision3 _{0}_Position = IN.{1}SpacePosition;", GetVariableNameForNode(), NeededCoordinateSpace.World.ToString());
            sb.AppendLine("$precision _{0}_scale = 1.0 / ({1});", GetVariableNameForNode(), GetSlotValue(k_InputStrengthId, generationMode));
            sb.AppendLine("{0}({1}, _{2}_TangentMatrix, _{2}_scale, {3});", GetFunctionName(), inputValue, GetVariableNameForNode(), outputValue);
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
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

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability) => NeededCoordinateSpace.World;
        public NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability) => NeededCoordinateSpace.World;
        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability) => NeededCoordinateSpace.World;
        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability) => NeededCoordinateSpace.World;
	}
}
