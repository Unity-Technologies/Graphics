using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Mesh Deformation", "Compute Deformation")]
    class ComputeDeformNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent, IMayRequireVertexID
    {
        public const int kPositionOutputSlotId = 0;
        public const int kNormalOutputSlotId = 1;
        public const int kTangentOutputSlotId = 2;

        public const string kOutputSlotPositionName = "Deformed Position";
        public const string kOutputSlotNormalName = "Deformed Normal";
        public const string kOutputSlotTangentName = "Deformed Tangent";

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
#if !HYBRID_RENDERER_0_6_0_OR_NEWER
            owner.AddSetupError(objectId, "Could not find version 0.6.0 or newer of the Hybrid Renderer package installed in the project.");
            hasError = true;
#endif
#if !ENABLE_COMPUTE_DEFORMATIONS
            owner.AddSetupError(objectId, "For the Compute Deformation node to work, you must go to Project Settings>Player>Other Settings and add the ENABLE_COMPUTE_DEFORMATIONS define to Scripting Define Symbols.");
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
                displayName = "Compute Mesh Buffer Index Offset",
                overrideReferenceName = "_ComputeMeshIndex",
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.HybridPerInstance,
#if ENABLE_HYBRID_RENDERER_V2
                hidden = true,
#endif
                value = 0
            });

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
                    $"IN.VertexID, " +
                    $"{GetVariableNameForSlot(kPositionOutputSlotId)}, " +
                    $"{GetVariableNameForSlot(kNormalOutputSlotId)}, " +
                    $"{GetVariableNameForSlot(kTangentOutputSlotId)});");
            }
#if ENABLE_HYBRID_RENDERER_V2
            sb.AppendLine("#else");
            sb.AppendLine("$precision3 {0} = IN.ObjectSpacePosition;", GetVariableNameForSlot(kPositionOutputSlotId));
            sb.AppendLine("$precision3 {0} = IN.ObjectSpaceNormal;", GetVariableNameForSlot(kNormalOutputSlotId));
            sb.AppendLine("$precision3 {0} = IN.ObjectSpaceTangent;", GetVariableNameForSlot(kTangentOutputSlotId));
            sb.AppendLine("#endif");
#endif
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
                    sb.AppendLine("float3 Tangent;");
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
                    sb.AppendLine("tangentOut = vertexData.Tangent;");
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
