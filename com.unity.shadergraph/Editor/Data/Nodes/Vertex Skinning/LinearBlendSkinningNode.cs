using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Vertex Skinning", "Linear Blend Skinning")]
    class LinearBlendSkinningNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireVertexSkinning, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        public const int kPositionSlotId = 0;
        public const int kNormalSlotId = 1;
        public const int kTangentSlotId = 2;
        public const int kPositionOutputSlotId = 3;
        public const int kNormalOutputSlotId = 4;
        public const int kTangentOutputSlotId = 5;
        public const int kSkinMatrixIndexOffsetSlotId = 6;

        public const string kSlotPositionName = "Position";
        public const string kSlotNormalName = "Normal";
        public const string kSlotTangentName = "Tangent";
        public const string kOutputSlotPositionName = "Position";
        public const string kOutputSlotNormalName = "Normal";
        public const string kOutputSlotTangentName = "Tangent";
        public const string kSkinMatrixIndexOffsetName = "Skin Matrix Index Offset";

        public LinearBlendSkinningNode()
        {
            name = "Linear Blend Skinning";
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
            AddSlot(new Vector1MaterialSlot(kSkinMatrixIndexOffsetSlotId, kSkinMatrixIndexOffsetName, kSkinMatrixIndexOffsetName, SlotType.Input, 0f, ShaderStageCapability.Vertex));
            RemoveSlotsNameNotMatching(new[] { kPositionSlotId, kNormalSlotId, kTangentSlotId, kPositionOutputSlotId, kNormalOutputSlotId, kTangentOutputSlotId, kSkinMatrixIndexOffsetSlotId });
        }

        public bool RequiresVertexSkinning(ShaderStageCapability stageCapability = ShaderStageCapability.All)
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

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kPositionOutputSlotId));
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kNormalOutputSlotId));
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kTangentOutputSlotId));
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine($"{GetFunctionName()}(");
                using (sb.IndentScope())
                {
                    sb.AppendLine(
                           /*0*/ "IN.BoneIndices, " +
                           /*1*/ "IN.BoneWeights, ");
                    sb.AppendLine(
                           /*2*/ $"{GetSlotValue(kPositionSlotId, generationMode)}, " +
                           /*3*/ $"{GetSlotValue(kNormalSlotId, generationMode)}, " +
                           /*4*/ $"{GetSlotValue(kTangentSlotId, generationMode)}, ");
                    sb.AppendLine(
                           /*5*/ $"{GetVariableNameForSlot(kPositionOutputSlotId)}, " +
                           /*6*/ $"{GetVariableNameForSlot(kNormalOutputSlotId)}, " +
                           /*7*/ $"{GetVariableNameForSlot(kTangentOutputSlotId)} ");
                    sb.AppendLine(");");
                }
            }
        }
        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction("SkinningMatrices", sb =>
            {
                sb.AppendLine("uniform StructuredBuffer<float3x4> _SkinMatrices;");
            });
            registry.ProvideFunction(GetFunctionName(), sb =>
            {
                sb.AppendLine($"void {GetFunctionName()}(");
                using (sb.IndentScope())
                {
                    sb.AppendLine(
                            /*0*/"uint4 indices," +
                            /*1*/"$precision4 weights, ");
                    sb.AppendLine(
                            /*2*/"$precision3 positionIn, " +
                            /*3*/"$precision3 normalIn, " +
                            /*4*/"$precision3 tangentIn,");
                    sb.AppendLine(
                            /*5*/"out $precision3 positionOut, " +
                            /*6*/"out $precision3 normalOut, " +
                            /*7*/"out $precision3 tangentOut ");
                    sb.AppendLine(")");
                }
                sb.AppendLine("{");
                using (sb.IndentScope())
                {
                    sb.AppendLine("for (int i = 0; i < 4; i++)");
                    sb.AppendLine("{");
                    using (sb.IndentScope())
                    {
                        sb.AppendLine("$precision3x4 skinMatrix = _SkinMatrices[indices[i] + asint(_SkinMatrixIndex)];");
                        sb.AppendLine("$precision3 vtransformed = mul(skinMatrix, float4(positionIn, 1));");
                        sb.AppendLine("$precision3 ntransformed = mul(skinMatrix, float4(normalIn, 0));");
                        sb.AppendLine("$precision3 ttransformed = mul(skinMatrix, float4(tangentIn, 0));");
                        sb.AppendLine("");
                        sb.AppendLine("positionOut += vtransformed * weights[i];");
                        sb.AppendLine("normalOut += ntransformed * weights[i];");
                        sb.AppendLine("tangentOut += ttransformed * weights[i];");
                    }
                    sb.AppendLine("}");
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
