using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    [Title("Procedural", "Terrain Mesh")]
    class TerrainMeshNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequirePosition
    {
        public TerrainMeshNode()
        {
            name = "Terrain Mesh";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return false; } }

        const int kPositionOutputSlotId = 0;
        const int kNormalOutputSlotId = 1;
        const int kTangentOutputSlotId = 2;
        const string kPositionOutputSlotName = "Position";
        const string kNormalOutputSlotName = "Normal";
        const string kTangentOutputSlotName = "Tangent";

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kPositionOutputSlotId, kPositionOutputSlotName, kPositionOutputSlotName, SlotType.Output, UnityEngine.Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(kNormalOutputSlotId, kNormalOutputSlotName, kNormalOutputSlotName, SlotType.Output, UnityEngine.Vector3.zero, ShaderStageCapability.All));
            AddSlot(new Vector4MaterialSlot(kTangentOutputSlotId, kTangentOutputSlotName, kTangentOutputSlotName, SlotType.Output, UnityEngine.Vector4.zero, ShaderStageCapability.All));

            RemoveSlotsNameNotMatching(new[] {
                kPositionOutputSlotId,
                kNormalOutputSlotId,
                kTangentOutputSlotId
            });
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            if (stageCapability == ShaderStageCapability.Vertex)
                return NeededCoordinateSpace.Object;
            return NeededCoordinateSpace.None;
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            if (graphContext.graphInputStructName == "VertexDescriptionInputs")
            {
                // Vertex
                sb.AppendLine("float2 patchVertex = IN.ObjectSpacePosition.xy;");
                sb.AppendLine("float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);");
                sb.AppendLine("float2 sampleCoords = (patchVertex.xy + instanceData.xy) * instanceData.z; // (xy + float2(xBase,yBase)) * skipScale");
                sb.AppendLine("float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));");
                sb.AppendLine("$precision3 {0} = {1};", GetVariableNameForSlot(kPositionOutputSlotId), "float3(sampleCoords.x, height, sampleCoords.y) * _TerrainHeightmapScale.xyz");
//                sb.AppendLine("$precision3 {0} = {1};", GetVariableNameForSlot(kNormalOutputSlotId), "_TerrainNormalmapTexture.Load(int3(sampleCoords, 0)).rgb * 2 - 1");
//                sb.AppendLine("$precision4 {0} = ConstructTerrainTangent({1}, float3(0, 0, 1));", GetVariableNameForSlot(kTangentOutputSlotId), GetVariableNameForSlot(kNormalOutputSlotId));
            }
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction("TerrainProperties", sb =>
            {
                sb.AppendLines(@"CBUFFER_START(UnityTerrain)
    float4 _TerrainHeightmapRecipSize;  // float4(1.0f/width, 1.0f/height, 1.0f/(width-1), 1.0f/(height-1))
    float4 _TerrainHeightmapScale;      // float4(hmScale.x, hmScale.y / (float)(kMaxHeight), hmScale.z, 0.0f)
CBUFFER_END

TEXTURE2D(_TerrainHeightmapTexture);
TEXTURE2D(_TerrainNormalmapTexture);
SAMPLER(sampler_TerrainNormalmapTexture);

UNITY_INSTANCING_BUFFER_START(Terrain)
    UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)  // float4(xBase, yBase, skipScale, ~)
UNITY_INSTANCING_BUFFER_END(Terrain)

float4 ConstructTerrainTangent(float3 normal, float3 positiveZ)
{
    // Consider a flat terrain. It should have tangent be (1, 0, 0) and bitangent be (0, 0, 1) as the UV of the terrain grid mesh is a scale of the world XZ position.
    // In CreateWorldToTangent function (in SpaceTransform.hlsl), it is cross(normal, tangent) * sgn for the bitangent vector.
    // It is not true in a left-handed coordinate system for the terrain bitangent, if we provide 1 as the tangent.w. It would produce (0, 0, -1) instead of (0, 0, 1).
    // Also terrain's tangent calculation was wrong in a left handed system because cross((0,0,1), terrainNormalOS) points to the wrong direction as negative X.
    // Therefore all the 4 xyzw components of the tangent needs to be flipped to correct the tangent frame.
    // (See TerrainLitData.hlsl - GetSurfaceAndBuiltinData)
    float3 tangent = cross(normal, positiveZ);
    return float4(tangent, -1);
}");
            });
        }
    }
}
