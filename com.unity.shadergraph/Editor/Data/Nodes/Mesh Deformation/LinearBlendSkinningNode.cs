using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Mesh Deformation", "Linear Blend Skinning")]
    class LinearBlendSkinningNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireVertexSkinning, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        public const int kPositionSlotId = 0;
        public const int kNormalSlotId = 1;
        public const int kTangentSlotId = 2;
        public const int kPositionOutputSlotId = 3;
        public const int kNormalOutputSlotId = 4;
        public const int kTangentOutputSlotId = 5;

        public const string kSlotPositionName = "Vertex Position";
        public const string kSlotNormalName = "Vertex Normal";
        public const string kSlotTangentName = "Vertex Tangent";
        public const string kOutputSlotPositionName = "Skinned Position";
        public const string kOutputSlotNormalName = "Skinned Normal";
        public const string kOutputSlotTangentName = "Skinned Tangent";

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
            RemoveSlotsNameNotMatching(new[] { kPositionSlotId, kNormalSlotId, kTangentSlotId, kPositionOutputSlotId, kNormalOutputSlotId, kTangentOutputSlotId });
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

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
#if HYBRID_RENDERER_0_6_0_OR_NEWER
            properties.AddShaderProperty(new Vector1ShaderProperty()
            {
                displayName = "Skin Matrix Index Offset",
                overrideReferenceName = "_SkinMatrixIndex",
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.HybridPerInstance,
#if ENABLE_HYBRID_RENDERER_V2
                hidden = true,
#endif
                value = 0
            });

#else
            properties.AddShaderProperty(new Vector1ShaderProperty()
            {
                overrideReferenceName = "_SkinMatricesOffset",
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.HybridPerInstance,
#if ENABLE_HYBRID_RENDERER_V2
                hidden = true,
#endif
                value = 0
            });
#endif


            base.CollectShaderProperties(properties, generationMode);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
#if ENABLE_HYBRID_RENDERER_V2
            sb.AppendLine("#if defined(UNITY_DOTS_INSTANCING_ENABLED)");
#endif
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kPositionOutputSlotId));
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kNormalOutputSlotId));
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kTangentOutputSlotId));
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine($"{GetFunctionName()}(" +
                           $"IN.BoneIndices, " +
                           $"IN.BoneWeights, " +
                           $"{GetSlotValue(kPositionSlotId, generationMode)}, " +
                           $"{GetSlotValue(kNormalSlotId, generationMode)}, " +
                           $"{GetSlotValue(kTangentSlotId, generationMode)}, " +
                           $"{GetVariableNameForSlot(kPositionOutputSlotId)}, " +
                           $"{GetVariableNameForSlot(kNormalOutputSlotId)}, " +
                           $"{GetVariableNameForSlot(kTangentOutputSlotId)});");
            }
#if ENABLE_HYBRID_RENDERER_V2
            sb.AppendLine("#else");
            sb.AppendLine("$precision3 {0} = {1};", GetVariableNameForSlot(kPositionOutputSlotId), GetSlotValue(kPositionSlotId, generationMode));
            sb.AppendLine("$precision3 {0} = {1};", GetVariableNameForSlot(kNormalOutputSlotId), GetSlotValue(kNormalSlotId, generationMode));
            sb.AppendLine("$precision3 {0} = {1};", GetVariableNameForSlot(kTangentOutputSlotId), GetSlotValue(kTangentSlotId, generationMode));
            sb.AppendLine("#endif");
#endif
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction("SkinMatrices", sb =>
            {
                sb.AppendLine("uniform StructuredBuffer<float3x4> _SkinMatrices;");
            });

            registry.ProvideFunction(GetFunctionName(), sb =>
            {
                sb.AppendLine($"void {GetFunctionName()}(" +
                            "uint4 indices, " +
                            "$precision4 weights, " +
                            "$precision3 positionIn, " +
                            "$precision3 normalIn, " +
                            "$precision3 tangentIn, " +
                            "out $precision3 positionOut, " +
                            "out $precision3 normalOut, " +
                            "out $precision3 tangentOut)");
                sb.AppendLine("{");
                using (sb.IndentScope())
                {
                    sb.AppendLine("for (int i = 0; i < 4; ++i)");
                    sb.AppendLine("{");
                    using (sb.IndentScope())
                {
#if HYBRID_RENDERER_0_6_0_OR_NEWER
                        sb.AppendLine("$precision3x4 skinMatrix = _SkinMatrices[indices[i] + asint(_SkinMatrixIndex)];");
#else
                        sb.AppendLine("$precision3x4 skinMatrix = _SkinMatrices[indices[i] + (int)_SkinMatricesOffset];");
#endif
                        sb.AppendLine("$precision3 vtransformed = mul(skinMatrix, $precision4(positionIn, 1));");
                        sb.AppendLine("$precision3 ntransformed = mul(skinMatrix, $precision4(normalIn, 0));");
                        sb.AppendLine("$precision3 ttransformed = mul(skinMatrix, $precision4(tangentIn, 0));");
                        sb.AppendLine("");
                        sb.AppendLine("positionOut += vtransformed * weights[i];");
                        sb.AppendLine("normalOut   += ntransformed * weights[i];");
                        sb.AppendLine("tangentOut  += ttransformed * weights[i];");
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
