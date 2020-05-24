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
        public const int kSkinMatricesOffsetSlotId = 6;

        public const string kSlotPositionName = "Position";
        public const string kSlotNormalName = "Normal";
        public const string kSlotTangentName = "Tangent";
        public const string kOutputSlotPositionName = "Position";
        public const string kOutputSlotNormalName = "Normal";
        public const string kOutputSlotTangentName = "Tangent";
        public const string kSkinMatricesOffsetName = "Skin Matrix Index Offset"; 

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
            AddSlot(new Vector1MaterialSlot(kSkinMatricesOffsetSlotId, kSkinMatricesOffsetName, kSkinMatricesOffsetName, SlotType.Input, 0f, ShaderStageCapability.Vertex));
            RemoveSlotsNameNotMatching(new[] { kPositionSlotId, kNormalSlotId, kTangentSlotId, kPositionOutputSlotId, kNormalOutputSlotId, kTangentOutputSlotId, kSkinMatricesOffsetSlotId });
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
                sb.AppendLine("{0}(IN.BoneIndices, (int)(({1})), IN.BoneWeights, {2}, {3}, {4}, {5}, {6}, {7});",
                    GetFunctionName(),
                    GetSlotValue(kSkinMatricesOffsetSlotId, generationMode),
                    GetSlotValue(kPositionSlotId, generationMode),
                    GetSlotValue(kNormalSlotId, generationMode),
                    GetSlotValue(kTangentSlotId, generationMode),
                    GetVariableNameForSlot(kPositionOutputSlotId),
                    GetVariableNameForSlot(kNormalOutputSlotId),
                    GetVariableNameForSlot(kTangentOutputSlotId));
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
                sb.AppendLine("void {0}(uint4 indices, int indexOffset, $precision4 weights, $precision3 positionIn, $precision3 normalIn, $precision3 tangentIn, out $precision3 positionOut, out $precision3 normalOut, out $precision3 tangentOut)",
                    GetFunctionName());
                sb.AppendLine("{");
                using (sb.IndentScope())
                {
                    sb.AppendLine("for (int i = 0; i < 4; i++)");
                    sb.AppendLine("{");
                    using (sb.IndentScope())
                    {
                        sb.AppendLine("$precision3x4 skinMatrix = _SkinMatrices[indices[i] + indexOffset];");
                        sb.AppendLine("$precision3 vtransformed = mul(skinMatrix, $precision4(positionIn, 1));");
                        sb.AppendLine("$precision3 ntransformed = mul(skinMatrix, $precision4(normalIn, 0));");
                        sb.AppendLine("$precision3 ttransformed = mul(skinMatrix, $precision4(tangentIn, 0));");
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
