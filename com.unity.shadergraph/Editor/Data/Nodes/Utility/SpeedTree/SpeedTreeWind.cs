using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
	[Title("Utility", "SpeedTree", "SpeedTreeWind")]
	class SpeedTreeWindNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent, IMayRequireVertexColor, IMayRequireMeshUV, IGeneratesFunction
	{
		public const int OutPosSlotId = 0;
		private const string kOutPosSlotName = "OutPosition";
        public const int OutNormSlotId = 1;
        private const string kOutNormSlotName = "OutNormal";
        public const int OutTanSlotId = 2;
        private const string kOutTanSlotName = "OutTangent";
        public const int OutUVSlotId = 3;
        private const string kOutUVSlotName = "OutUV0";
        public const int OutAlphaSlotId = 4;
        private const string kOutAlphaSlotName = "OutAlphaMultiplier";

        public override bool hasPreview { get { return false; } }

		public SpeedTreeWindNode()
		{
			name = "SpeedTree Wind";
			UpdateNodeAfterDeserialization();
		}

		public override void UpdateNodeAfterDeserialization()
		{
			AddSlot(new Vector3MaterialSlot(OutPosSlotId, kOutPosSlotName, kOutPosSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutNormSlotId, kOutNormSlotName, kOutNormSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutTanSlotId, kOutTanSlotName, kOutTanSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector2MaterialSlot(OutUVSlotId, kOutUVSlotName, kOutUVSlotName, SlotType.Output, Vector2.zero));
            AddSlot(new Vector1MaterialSlot(OutAlphaSlotId, kOutAlphaSlotName, kOutAlphaSlotName, SlotType.Output, 1.0f));

            RemoveSlotsNameNotMatching(new[] { OutPosSlotId, OutNormSlotId, OutTanSlotId, OutUVSlotId, OutAlphaSlotId });
        }

		public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
		{
            // Declare the output variables with their respective types
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutPosSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutPosSlotId));
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutNormSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutNormSlotId));
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutTanSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutTanSlotId));
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutUVSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutUVSlotId));
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutAlphaSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutAlphaSlotId));
            // Call the Wind Transformation function
            sb.AppendLine("ApplyWindTransformation(IN.{0}, IN.{1}, IN.{2}, IN.{3}, IN.{4}, IN.{5}, IN.{6}, IN.{7}, {8}, {9}, {10}, {11}, {12});",
                            CoordinateSpace.Object.ToVariableName(InterpolatorType.Position),
                            CoordinateSpace.Object.ToVariableName(InterpolatorType.Normal),
                            CoordinateSpace.Object.ToVariableName(InterpolatorType.Tangent),
                            ShaderGeneratorNames.VertexColor,
                            UVChannel.UV0.GetUVName(),
                            UVChannel.UV1.GetUVName(),
                            UVChannel.UV2.GetUVName(),
                            UVChannel.UV3.GetUVName(),
                            GetVariableNameForSlot(OutPosSlotId),
                            GetVariableNameForSlot(OutNormSlotId),
                            GetVariableNameForSlot(OutTanSlotId),
                            GetVariableNameForSlot(OutUVSlotId),
                            GetVariableNameForSlot(OutAlphaSlotId));
        }

		public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
		{
			return NeededCoordinateSpace.Object;
		}

		public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
		{
            return NeededCoordinateSpace.Object;
		}

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.Object;
        }

        public bool RequiresVertexColor(ShaderStageCapability stageCapability)
        {
            return true;
        }

		public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
		{
			return true;
		}

		public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
		{
            // Inject SpeedTree wind code at global scope
            registry.ProvideFunction("ApplyWindTransformation", sb => { sb.AppendLines("#include \"Packages/com.unity.render-pipelines.core/ShaderLibrary/SpeedTree/SpeedTreeCommonWind.hlsl\""); });
		}
	}
}
