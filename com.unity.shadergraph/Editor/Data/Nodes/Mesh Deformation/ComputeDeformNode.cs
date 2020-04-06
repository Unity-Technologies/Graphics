using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Mesh Deformation", "Compute Deformation")]
    class ComputeDeformNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent, IMayRequireVertexID
    {
        public const int kPositionSlotId = 0;
        public const int kNormalSlotId = 1;
        public const int kTangentSlotId = 2;
        public const int kPositionOutputSlotId = 3;
        public const int kNormalOutputSlotId = 4;
        public const int kTangentOutputSlotId = 5;
        public const int kVertexIndexOffsetSlotId = 6;

        public const string kSlotPositionName = "Position";
        public const string kSlotNormalName = "Normal";
        public const string kSlotTangentName = "Tangent";
        public const string kOutputSlotPositionName = "Position";
        public const string kOutputSlotNormalName = "Normal";
        public const string kOutputSlotTangentName = "Tangent";
        public const string kVertexIndexOffsetName = "Vertex Index Offset";

        public ComputeDeformNode()
        {
            name = "Compute Deformation";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new PositionMaterialSlot(kPositionSlotId, kSlotPositionName, kSlotPositionName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new NormalMaterialSlot(kNormalSlotId, kSlotNormalName, kSlotNormalName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new TangentMaterialSlot(kTangentSlotId, kSlotTangentName, kSlotTangentName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(kPositionOutputSlotId, kOutputSlotPositionName, kOutputSlotPositionName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(kNormalOutputSlotId, kOutputSlotNormalName, kOutputSlotNormalName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(kTangentOutputSlotId, kOutputSlotTangentName, kOutputSlotTangentName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector1MaterialSlot(kVertexIndexOffsetSlotId, kVertexIndexOffsetName, kVertexIndexOffsetName, SlotType.Input, 0f, ShaderStageCapability.Vertex));
            RemoveSlotsNameNotMatching(new[] { kPositionSlotId, kNormalSlotId, kTangentSlotId, kPositionOutputSlotId, kNormalOutputSlotId, kTangentOutputSlotId, kVertexIndexOffsetSlotId });
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

        public bool RequiresVertexID(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return true;
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            //TODO: Not break old vertex skinning?
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kPositionOutputSlotId));
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kNormalOutputSlotId));
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kTangentOutputSlotId));
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine($"{GetFunctionName()}(");
                using (sb.IndentScope())
                {
                    sb.AppendLine(
                           /*0*/ "IN.VertexID,");
                    sb.AppendLine(
                           /*1*/ $"{GetSlotValue(kPositionSlotId, generationMode)}, " +
                           /*2*/ $"{GetSlotValue(kNormalSlotId, generationMode)}, " +
                           /*3*/ $"{GetSlotValue(kTangentSlotId, generationMode)}, ");
                    sb.AppendLine(
                           /*4*/ $"{GetVariableNameForSlot(kPositionOutputSlotId)}, " +
                           /*5*/ $"{GetVariableNameForSlot(kNormalOutputSlotId)}, " +
                           /*6*/ $"{GetVariableNameForSlot(kTangentOutputSlotId)}, ");
                    sb.AppendLine(
                           /*7*/$"(int)(({GetSlotValue(kVertexIndexOffsetSlotId, generationMode)}))");

                    sb.AppendLine(");");
                }
            }

            
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction("SkinningMatrices", sb =>
            {
                sb.AppendLine("struct VertexData");
                sb.AppendLine("{");
                using (sb.IndentScope())
                {
                    sb.AppendLine("float3 Position;");
                    sb.AppendLine("float3 Normal;");
                    sb.AppendLine("float4 Tangent;");
                }
                sb.AppendLine("};");
                sb.AppendLine("uniform StructuredBuffer<VertexData> _DeformedMeshData : register(t1);");
            });

            registry.ProvideFunction(GetFunctionName(), sb =>
            {
                sb.AppendLine($"void {GetFunctionName()}(");
                using (sb.IndentScope())
                {
                    sb.AppendLine(
                            /*0*/"int vertexID, ");
                    sb.AppendLine(
                            /*1*/"$precision3 positionIn, " +
                            /*2*/"$precision3 normalIn, " +
                            /*3*/"$precision3 tangentIn,");
                    sb.AppendLine(
                            /*4*/"out $precision3 positionOut, " +
                            /*5*/"out $precision3 normalOut, " +
                            /*6*/"out $precision3 tangentOut, ");
                    sb.AppendLine(
                            /*7*/"int vertIndexOffset");

                    sb.AppendLine(")");
                }

                sb.AppendLine("{");
                using (sb.IndentScope())
                {
                    sb.AppendLine("const VertexData vertData = _DeformedMeshData[vertIndexOffset + vertexID];");
                    sb.AppendLine("positionOut = vertData.Position;");
                    sb.AppendLine("normalOut = vertData.Normal;");
                    sb.AppendLine("tangentOut = vertData.Tangent;");
                }
                sb.AppendLine("}");
            });
        }

        string GetFunctionName()
        {
            return $"Unity_LinearBlendSkinning_{concretePrecision.ToShaderString()}";
        }
    }
}
