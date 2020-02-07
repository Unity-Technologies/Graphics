using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [Title("Vertex Skinning", "Linear Blend Skinning")]
    class LinearBlendSkinningNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireVertexSkinning, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {

        public enum BoneCountType
        {
            PerVerteBoneCount4,
            PerVerteBoneCount8
        };

        [SerializeField]
        private BoneCountType m_BoneCountType = BoneCountType.PerVerteBoneCount4;

        [EnumControl("BoneCount")]
        public BoneCountType boneCountType
        {
            get { return m_BoneCountType; }
            set
            {
                if (m_BoneCountType == value)
                    return;

                m_BoneCountType = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }

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
                if(m_BoneCountType == BoneCountType.PerVerteBoneCount8)
                {
                    sb.AppendLine("$precision4 BoneIndices4_8 = $precision4(IN.uv4.x, IN.uv5.x, IN.uv6.x, IN.uv7.x);");
                    sb.AppendLine("$precision4 BoneWeights4_8 = $precision4(IN.uv4.y, IN.uv5.y, IN.uv6.y, IN.uv7.y);");
                    sb.AppendLine("$precision1 denormalizeScale = 1.0f - (BoneWeights4_8.x + BoneWeights4_8.y + BoneWeights4_8.z + BoneWeights4_8.w);");                
                    sb.AppendLine("{0}(IN.BoneIndices, BoneIndices4_8, (int)(({1})), IN.BoneWeights * denormalizeScale, BoneWeights4_8, {2}, {3}, {4}, {5}, {6}, {7});",
                        GetFunctionName(),
                        GetSlotValue(kSkinMatricesOffsetSlotId, generationMode),
                        GetSlotValue(kPositionSlotId, generationMode),
                        GetSlotValue(kNormalSlotId, generationMode),
                        GetSlotValue(kTangentSlotId, generationMode),
                        GetVariableNameForSlot(kPositionOutputSlotId),
                        GetVariableNameForSlot(kNormalOutputSlotId),
                        GetVariableNameForSlot(kTangentOutputSlotId));
                }
                else
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
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction("SkinningMatrices", sb =>
            {
                sb.AppendLine("uniform StructuredBuffer<float3x4> _SkinMatrices;");
            });
            registry.ProvideFunction(GetFunctionName(), sb =>
            {
                if(m_BoneCountType == BoneCountType.PerVerteBoneCount8)
                {
                    sb.AppendLine("void {0}(uint4 indices, uint4 indices4_8, int indexOffset, $precision4 weights, $precision4 weights4_8, $precision3 positionIn, $precision3 normalIn, $precision3 tangentIn, out $precision3 positionOut, out $precision3 normalOut, out $precision3 tangentOut)",
                        GetFunctionName());
                }
                else
                {
                    sb.AppendLine("void {0}(uint4 indices, int indexOffset, $precision4 weights, $precision3 positionIn, $precision3 normalIn, $precision3 tangentIn, out $precision3 positionOut, out $precision3 normalOut, out $precision3 tangentOut)",
                        GetFunctionName());            
                }
                sb.AppendLine("{");
                using (sb.IndentScope())
                {
                    sb.AppendLine("int i;");
                    sb.AppendLine("for (i = 0; i < 4; i++)");
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
                    if(m_BoneCountType == BoneCountType.PerVerteBoneCount8)
                    {
                        sb.AppendLine("for (i = 0; i < 4; i++)");
                        sb.AppendLine("{");
                        using (sb.IndentScope())
                        {
                            sb.AppendLine("$precision3x4 skinMatrix = _SkinMatrices[indices4_8[i] + indexOffset];");
                            sb.AppendLine("$precision3 vtransformed = mul(skinMatrix, $precision4(positionIn, 1));");
                            sb.AppendLine("$precision3 ntransformed = mul(skinMatrix, $precision4(normalIn, 0));");
                            sb.AppendLine("$precision3 ttransformed = mul(skinMatrix, $precision4(tangentIn, 0));");
                            sb.AppendLine("");
                            sb.AppendLine("positionOut += vtransformed * weights4_8[i];");
                            sb.AppendLine("normalOut += ntransformed * weights4_8[i];");
                            sb.AppendLine("tangentOut += ttransformed * weights4_8[i];");
                        }
                        sb.AppendLine("}");
                    }
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
