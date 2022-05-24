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
#if !(HYBRID_RENDERER_0_6_0_OR_NEWER || ENTITIES_GRAPHICS_0_60_0_OR_NEWER)
            owner.AddSetupError(objectId, "Could not find a supported version (0.60.0 or newer) of the com.unity.entities.graphics package installed in the project.");
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
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
            properties.AddShaderProperty(new Vector4ShaderProperty()
            {
                displayName = "Compute Mesh Buffer Index Offset",
                overrideReferenceName = "_DotsDeformationParams",
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.HybridPerInstance,
                hidden = true,
                value = new Vector4(0, 0, 0, 0)
            });
#else
            properties.AddShaderProperty(new Vector1ShaderProperty()
            {
                displayName = "Compute Mesh Buffer Index Offset",
                overrideReferenceName = "_ComputeMeshIndex",
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.HybridPerInstance,
                hidden = true,
                value = 0
            });
#endif
            base.CollectShaderProperties(properties, generationMode);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine("#if defined(UNITY_DOTS_INSTANCING_ENABLED)");
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kPositionOutputSlotId));
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kNormalOutputSlotId));
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kTangentOutputSlotId));
            if (generationMode == GenerationMode.ForReals)
            {
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
                sb.AppendLine("ApplyDeformedVertexData(" +
#else
                sb.AppendLine($"{GetFunctionName()}(" +
#endif
                $"IN.VertexID, " +
                    $"{GetVariableNameForSlot(kPositionOutputSlotId)}, " +
                    $"{GetVariableNameForSlot(kNormalOutputSlotId)}, " +
                    $"{GetVariableNameForSlot(kTangentOutputSlotId)});");
            }
            sb.AppendLine("#else");
            sb.AppendLine("$precision3 {0} = IN.ObjectSpacePosition;", GetVariableNameForSlot(kPositionOutputSlotId));
            sb.AppendLine("$precision3 {0} = IN.ObjectSpaceNormal;", GetVariableNameForSlot(kNormalOutputSlotId));
            sb.AppendLine("$precision3 {0} = IN.ObjectSpaceTangent;", GetVariableNameForSlot(kTangentOutputSlotId));

            sb.AppendLine("#endif");
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
            registry.ProvideFunction("define", sb =>
            {
                sb.AppendLine("#if defined(UNITY_DOTS_INSTANCING_ENABLED)"); // start of UNITY_DOTS_INSTANCING_ENABLED
                sb.AppendLine("#define DOTS_DEFORMED");
                sb.AppendLine("#include \"Packages/com.unity.entities.graphics/Unity.Entities.Graphics/Deformations/ShaderLibrary/DotsDeformation.hlsl\"");
                sb.AppendLine("#endif");
            });
#else
            registry.ProvideFunction("DeformedVertexData", sb =>
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
                    sb.AppendLine("const DeformedVertexData vertexData = _DeformedMeshData[asuint(UNITY_ACCESS_HYBRID_INSTANCED_PROP(_ComputeMeshIndex, float)) + vertexID];");
                    sb.AppendLine("positionOut = vertexData.Position;");
                    sb.AppendLine("normalOut = vertexData.Normal;");
                    sb.AppendLine("tangentOut = vertexData.Tangent;");
                }
                sb.AppendLine("}");
            });
#endif
        }

        string GetFunctionName()
        {
            return "Unity_ComputeDeformedVertex_$precision";
        }
    }
}
