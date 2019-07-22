using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    [GeometryModule.DisplayName("Unity Terrain")]
    class TerrainGeometryModule : IGeometryModule
    {
        public IEnumerable<VisualElement> CreateVisualElements()
        {
            yield break;
        }

        public void OnVisualElementValueChanged(VisualElement visualElement)
        { }

        public InstancingSettings GenerateInstancingSettings()
            => new InstancingSettings()
            {
                Enabled = true,
                Options = InstancingOption.AssumeUniformScaling | InstancingOption.NoMatrices | InstancingOption.NoLODFade | InstancingOption.NoLightProbe | InstancingOption.NoLightmap,
                ProceduralFuncName = null
            };

        public LODFadeSettings GenerateLODFadeSettings()
            => new LODFadeSettings
            {
                Enabled = false
            };

        public bool ForceVertex() => true;

        public void OverrideActiveFields(ICollection<string> activeFields)
        {
            // Terrain constructs geometry from position.
            activeFields.Add("AttributesMesh.positionOS");
            // Terrain needs to pass uv0 to pixel.
            activeFields.Add("AttributesMesh.uv0");
            activeFields.Add("VaryingsMeshToPS.texCoord0");
            activeFields.Add("FragInputs.texCoord0");

            // Don't pass tangent/normal from vertex to pixels.
            activeFields.Remove("VaryingsMeshToPS.normalWS");
            activeFields.Remove("VaryingsMeshToPS.tangentWS");

            // Don't pass uv1-uv3 from vertex to pixel because they are assigned from UV0 in pixel.
            activeFields.Remove("VaryingsMeshToPS.texCoord1");
            activeFields.Remove("VaryingsMeshToPS.texCoord2");
            activeFields.Remove("VaryingsMeshToPS.texCoord3");

            // Don't compute FragInputs.tangentToWorld from VaryingsMeshToPS.tangentWS & VaryingsMeshToPS.tangentWS
            activeFields.Remove("FragInputs.tangentToWorld");
            // Don't assign VaryingsMeshToPS.texCoord[1..3] to FragInputs.texCoord[1..3]
            activeFields.Remove("FragInputs.texCoord1");
            activeFields.Remove("FragInputs.texCoord2");
            activeFields.Remove("FragInputs.texCoord3");
        }

        public void RegisterGlobalFunctions(FunctionRegistry functionRegistry)
        {
            functionRegistry.ProvideFunction("TerrainProperties", sb =>
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

float4 ConstructTerrainTangent(float3 normal, float3 zPositive)
{
    // Consider a flat terrain. It should have tangent be (1, 0, 0) and bitangent be (0, 0, 1) as the UV of the terrain grid mesh is a scale of the world XZ position.
    // In CreateTangentToWorld function (in SpaceTransform.hlsl), it is cross(normal, tangent) * sgn for the bitangent vector.
    // It is not true in a left-handed coordinate system for the terrain bitangent, if we provide 1 as the tangent.w. It would produce (0, 0, -1) instead of (0, 0, 1).
    // Also terrain's tangent calculation was wrong in a left handed system because cross((0,0,1), terrainNormalOS) points to the wrong direction as negative X.
    // Therefore all the 4 xyzw components of the tangent needs to be flipped to correct the tangent frame.
    // (See TerrainLitData.hlsl - GetSurfaceAndBuiltinData)
    float3 tangent = cross(normal, zPositive);
    return float4(tangent, -1);
}");
            });
        }

        public void GenerateVertexProlog(ShaderStringBuilder sb, string inputVariableName)
        {
            sb.AppendLines(string.Format(@"float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);
float2 sampleCoords = ({0}.positionOS.xy + instanceData.xy) * instanceData.z; // (xy + float2(xBase,yBase)) * skipScale
float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));
{0}.positionOS.xz = sampleCoords * _TerrainHeightmapScale.xz;
{0}.positionOS.y = height * _TerrainHeightmapScale.y;
{0}.uv0 = float4(sampleCoords * _TerrainHeightmapRecipSize.zw, sampleCoords);
#if defined(ATTRIBUTES_NEED_NORMAL) || defined(ATTRIBUTES_NEED_TANGENT)
    float3 normalOS = _TerrainNormalmapTexture.Load(int3(sampleCoords, 0)).rgb * 2 - 1;
    #ifdef ATTRIBUTES_NEED_NORMAL
        {0}.normalOS = normalOS;
    #endif
    #ifdef ATTRIBUTES_NEED_TANGENT
        {0}.tangentOS = ConstructTerrainTangent(normalOS, float3(0, 0, 1));
    #endif
#endif", inputVariableName));
        }

        public void GeneratePixelProlog(ShaderStringBuilder sb, string inputVariableName)
        {
            sb.AppendLines(string.Format(@"float2 terrainNormalMapUV = ({0}.texCoord0.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
float3 normalOS = SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, terrainNormalMapUV).rgb * 2 - 1;
float3 normalWS = mul((float3x3)GetObjectToWorldMatrix(), normalOS);
float4 tangentWS = ConstructTerrainTangent(normalWS, GetObjectToWorldMatrix()._13_23_33);
{0}.tangentToWorld = BuildTangentToWorld(tangentWS, normalWS);
{0}.texCoord0 = {0}.texCoord1 = {0}.texCoord2 = {0}.texCoord3 = float4({0}.texCoord0.xy, 0, 0);", inputVariableName));
        }
    }
}
