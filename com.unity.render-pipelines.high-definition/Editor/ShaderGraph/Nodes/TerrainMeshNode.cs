using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    [Title("Procedural", "Terrain Mesh")]
    class TerrainMeshNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequirePosition, IMayRequireMeshUV
    {
        public TerrainMeshNode()
        {
            name = "Terrain Mesh";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return false; } }

        const int kPositionOutputSlotId = 0;
        const int kUVOutputSlotId = 1;
        const string kPositionOutputSlotName = "Position";
        const string kUVOutputSlotName = "UV";

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kPositionOutputSlotId, kPositionOutputSlotName, kPositionOutputSlotName, SlotType.Output, UnityEngine.Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector2MaterialSlot(kUVOutputSlotId, kUVOutputSlotName, kUVOutputSlotName, SlotType.Output, UnityEngine.Vector2.zero, ShaderStageCapability.Vertex));

            RemoveSlotsNameNotMatching(new[] {
                kPositionOutputSlotId,
                kUVOutputSlotId
            });
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            // Requires Position if we are not in fragment
            return stageCapability != ShaderStageCapability.Fragment ? NeededCoordinateSpace.Object : NeededCoordinateSpace.None;
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            // Requires UV0 if we are not in vertex
            return channel == UVChannel.UV0 && stageCapability != ShaderStageCapability.Vertex;
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            if (graphContext.graphInputStructName == "VertexDescriptionInputs")
            {
                // Vertex
                sb.AppendLine("float4 _{0}_instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);", GetVariableNameForNode());
                sb.AppendLine("float2 _{0}_sampleCoords = (IN.ObjectSpacePosition.xy + _{0}_instanceData.xy) * _{0}_instanceData.z; // (xy + float2(xBase,yBase)) * skipScale", GetVariableNameForNode());
                sb.AppendLine("float _{0}_height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(_{0}_sampleCoords, 0)));", GetVariableNameForNode());
                sb.AppendLine("$precision3 {0} = float3(_{1}_sampleCoords.x, _{1}_height, _{1}_sampleCoords.y) * _TerrainHeightmapScale.xyz;", GetVariableNameForSlot(kPositionOutputSlotId), GetVariableNameForNode());
                sb.AppendLine("$precision2 {0} = _{1}_sampleCoords * _TerrainHeightmapRecipSize.zw;", GetVariableNameForSlot(kUVOutputSlotId), GetVariableNameForNode());
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
SAMPLER(sampler_TerrainNormalmapTexture);");
            });

            registry.ProvideFunction("TerrainInstanceData", sb =>
            {
                sb.AppendLines(@"UNITY_INSTANCING_BUFFER_START(Terrain)
    UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)  // float4(xBase, yBase, skipScale, ~)
UNITY_INSTANCING_BUFFER_END(Terrain)");
            });
        }
    }
}
