using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Mesh Deformation", "Compute Deformation")]
    class ComputeDeformNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent, IMayRequireVertexID
    {
        public const int kPositionOutputSlotId = 0;
        public const int kNormalOutputSlotId = 1;
        public const int kTangentOutputSlotId = 2;

        public const string kOutputSlotPositionName = "Position";
        public const string kOutputSlotNormalName = "Normal";
        public const string kOutputSlotTangentName = "Tangent";

        public ComputeDeformNode()
        {
            name = "Compute Deformation";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kPositionOutputSlotId, kOutputSlotPositionName, kOutputSlotPositionName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(kNormalOutputSlotId, kOutputSlotNormalName, kOutputSlotNormalName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(kTangentOutputSlotId, kOutputSlotTangentName, kOutputSlotTangentName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            RemoveSlotsNameNotMatching(new[] { kPositionOutputSlotId, kNormalOutputSlotId, kTangentOutputSlotId });
        }

        protected override void CalculateNodeHasError()
        {
#if !ENABLE_COMPUTE_DEFORMATIONS
            owner.AddSetupError(guid, "ENABLE_COMPUTE_DEFORMATIONS define needs to be enabled in Player Settings for Compute Deformation node to work");
            hasError = true;
#endif
        }

        public bool RequiresVertexID(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return true;
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            if (stageCapability == ShaderStageCapability.Vertex || stageCapability == ShaderStageCapability.All)
                return NeededCoordinateSpace.Object;
            else
                return NeededCoordinateSpace.None;
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            if (stageCapability == ShaderStageCapability.Vertex || stageCapability == ShaderStageCapability.All)
                return NeededCoordinateSpace.Object;
            else
                return NeededCoordinateSpace.None;
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            if (stageCapability == ShaderStageCapability.Vertex || stageCapability == ShaderStageCapability.All)
                return NeededCoordinateSpace.Object;
            else
                return NeededCoordinateSpace.None;
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new Vector1ShaderProperty()
            {
                overrideReferenceName = "_ComputeMeshIndex",
                gpuInstanced = true,
                hidden = true,
                value = 0
            });

            base.CollectShaderProperties(properties, generationMode);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kPositionOutputSlotId));
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kNormalOutputSlotId));
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kTangentOutputSlotId));
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine($"{GetFunctionName()}(" +
                           $"IN.VertexID, " +
                           $"{GetVariableNameForSlot(kPositionOutputSlotId)}, " +
                           $"{GetVariableNameForSlot(kNormalOutputSlotId)}, " +
                           $"{GetVariableNameForSlot(kTangentOutputSlotId)});");
            }
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction("DeformedMeshData", sb =>
            {
                sb.AppendLine("struct DeformedVertexData");
                sb.AppendLine("{");
                using (sb.IndentScope())
                {
                    sb.AppendLine("float3 Position;");
                    sb.AppendLine("float3 Normal;");
                    sb.AppendLine("float4 Tangent;");
                }
                sb.AppendLine("};");
                sb.AppendLine("uniform StructuredBuffer<DeformedVertexData> _DeformedMeshData : register(t1);");
            });

            registry.ProvideFunction(GetFunctionName(), sb =>
            {
                sb.AppendLine($"void {GetFunctionName()}(" +
                            "uint vertexID, " +
                            "out $precision3 positionOut, " +
                            "out $precision3 normalOut, " +
                            "out $precision3 tangentOut)");

                sb.AppendLine("{");
                using (sb.IndentScope())
                {
                    sb.AppendLine("const DeformedVertexData vertexData = _DeformedMeshData[asuint(_ComputeMeshIndex) + vertexID];");
                    sb.AppendLine("positionOut = vertexData.Position;");
                    sb.AppendLine("normalOut = vertexData.Normal;");
                    sb.AppendLine("tangentOut = vertexData.Tangent.xyz;");
                }
                sb.AppendLine("}");
            });
        }

        string GetFunctionName()
        {
            return $"Unity_ComputeDeformedVertex_{concretePrecision.ToShaderString()}";
        }
    }
}
