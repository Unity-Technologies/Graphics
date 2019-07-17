using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    [Title("Procedural", "Terrain Normal")]
    class TerrainNormalNode : AbstractMaterialNode, IGeneratesFunction, IGeneratesProceduralCode, IMayRequireMeshUV
    {
        public TerrainNormalNode()
        {
            name = "Terrain Normal";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return false; } }

        const int kNormalTextureInputSlotId = 0;
        const int kNormalOutputSlotId = 1;
        const int kTangentOutputSlotId = 2;
        const string kNormalTextureSlotName = "NormalmapTexture";
        const string kNormalOutputSlotName = "Normal";
        const string kTangentOutputSlotName = "Tangent";

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new TerrainNormalmapTextureInputSlot(kNormalTextureInputSlotId, "Normal (World) ", kNormalTextureSlotName, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kNormalOutputSlotId, kNormalOutputSlotName, kNormalOutputSlotName, SlotType.Output, UnityEngine.Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector4MaterialSlot(kTangentOutputSlotId, kTangentOutputSlotName, kTangentOutputSlotName, SlotType.Output, UnityEngine.Vector4.zero, ShaderStageCapability.Fragment));

            RemoveSlotsNameNotMatching(new[] {
                kNormalTextureInputSlotId,
                kNormalOutputSlotId,
                kTangentOutputSlotId,
            });
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            // Requires UV0 if we are not in vertex
            return channel == UVChannel.UV0 && stageCapability != ShaderStageCapability.Vertex;
        }

        public void GenerateProceduralCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            sb.AppendLine("$precision3 {0};", GetVariableNameForSlot(kNormalOutputSlotId));
            sb.AppendLine("$precision4 {0};", GetVariableNameForSlot(kTangentOutputSlotId));
            sb.AppendLine("ConstructTerrainMesh(IN.texCoord0.xy, TEXTURE2D_ARGS({0}, sampler{0}), {1}, {2});", GetSlotValue(kNormalTextureInputSlotId, generationMode), GetVariableNameForSlot(kNormalOutputSlotId), GetVariableNameForSlot(kTangentOutputSlotId));
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction("TerrainProperties", sb =>
            {
                sb.AppendLines(@"CBUFFER_START(UnityTerrain)
    float4 _TerrainHeightmapRecipSize;  // float4(1.0f/width, 1.0f/height, 1.0f/(width-1), 1.0f/(height-1))
    float4 _TerrainHeightmapScale;      // float4(hmScale.x, hmScale.y / (float)(kMaxHeight), hmScale.z, 0.0f)
CBUFFER_END

TEXTURE2D(_TerrainHeightmapTexture);");
            });

            registry.ProvideFunction("TerrainUtilities", sb =>
            {
                sb.AppendLines(@"float4 ConstructTerrainTangent(float3 normal, float3 positiveZ)
{
    // Consider a flat terrain. It should have tangent be (1, 0, 0) and bitangent be (0, 0, 1) as the UV of the terrain grid mesh is a scale of the world XZ position.
    // In CreateWorldToTangent function (in SpaceTransform.hlsl), it is cross(normal, tangent) * sgn for the bitangent vector.
    // It is not true in a left-handed coordinate system for the terrain bitangent, if we provide 1 as the tangent.w. It would produce (0, 0, -1) instead of (0, 0, 1).
    // Also terrain's tangent calculation was wrong in a left handed system because cross((0,0,1), terrainNormalOS) points to the wrong direction as negative X.
    // Therefore all the 4 xyzw components of the tangent needs to be flipped to correct the tangent frame.
    // (See TerrainLitData.hlsl - GetSurfaceAndBuiltinData)
    float3 tangent = cross(normal, positiveZ);
    return float4(tangent, -1);
}

void ConstructTerrainMesh(float2 uv, TEXTURE2D_PARAM(normalTexture, samplerNormalTexture), out float3 normalWS, out float4 tangentWS)
{
    float2 sampleCoords = uv / _TerrainHeightmapRecipSize.zw;
    float2 terrainNormalMapUV = (sampleCoords + 0.5f) * _TerrainHeightmapRecipSize.xy;
    float3 normalOS = SAMPLE_TEXTURE2D(normalTexture, samplerNormalTexture, terrainNormalMapUV).rgb * 2 - 1;
    normalWS = mul((float3x3)GetObjectToWorldMatrix(), normalOS);
    tangentWS = ConstructTerrainTangent(normalWS, GetObjectToWorldMatrix()._13_23_33);
}");
            });
        }
    }
}
