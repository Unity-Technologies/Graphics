#if SHADERPASS != SHADERPASS_DYNAMIC_GIDATA_SAMPLE
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings Vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
    output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
    return output;
}


// One per material.
float4 _MaterialRequestsInfo;

#define _RequestCount      _MaterialRequestsInfo.x
#define _RequestStart      _MaterialRequestsInfo.y
#define _RequestsPerColumn _MaterialRequestsInfo.z
#define _HasBakedEmissive  _MaterialRequestsInfo.w

struct ExtraDataRequest
{
    float2 uv;
    float2 uvDdx;
    float2 uvDdy;
    float3 position;
    float3 positionDdx;
    float3 positionDdy;
    float3 normalWS;
    uint requestIdx;
};

struct ExtraDataRequestOutput
{
    float3 albedo;
    float3 emission;
};

StructuredBuffer<ExtraDataRequest>  _RequestsInputData;
RWStructuredBuffer<ExtraDataRequestOutput>  _RWRequestsOutputData : register(u1);

#ifdef _DEPTHOFFSET_ON
#undef _DEPTHOFFSET_ON
#endif


void Frag(  Varyings varInput,
            out float3 dummy : SV_Target0
    )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input;
    ZERO_INITIALIZE(FragInputs, input);

    input.positionSS = varInput.positionCS;

    float requestsPerRow = ceil(_RequestCount / _RequestsPerColumn);
    float2 drawSize = float2(requestsPerRow, _RequestsPerColumn) * 2;

    int2 texel = floor(input.positionSS.xy);
    int2 requestCoord = texel / 2;
    int localIdx = requestCoord.y * requestsPerRow + requestCoord.x;

    // Silence compiler warning about potentially uninitialized variable.
    dummy = 0.0;

    if (localIdx < _RequestCount)
    {
        ExtraDataRequest req = _RequestsInputData[localIdx];

        // Modify input with hit data
        int2 requestFragment = texel % 2;

        float2 uv = req.uv + req.uvDdx * requestFragment.x + req.uvDdy * requestFragment.y;
        float3 position = req.position + req.positionDdx * requestFragment.x + req.positionDdy * requestFragment.y;
        
        input.texCoord0 = float4(uv, 0, 1);
        input.positionRWS = position;
        input.tangentToWorld = GetLocalFrame(req.normalWS);

        PositionInputs posInput = GetPositionInput(input.positionSS.xy, rcp(drawSize), input.positionSS.z, input.positionSS.w, input.positionRWS);

#ifdef VARYINGS_NEED_POSITION_WS
        float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
        // Unused
        float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

        // The following is way too overkill but simpler.
        SurfaceData surfaceData;
        BuiltinData builtinData;
        GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

        float3 outAlbedo = float3(0,1,0);
        BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);
        outAlbedo.xyz = GetDiffuseOrDefaultColor(bsdfData, 1.0).xyz;

        // Output
        ExtraDataRequestOutput output;
        output.albedo = outAlbedo;
        output.emission = _HasBakedEmissive ? builtinData.emissiveColor : 0;
        int globalIdx = localIdx + _RequestStart;
        if (all(requestFragment == 0))
        {
            _RWRequestsOutputData[globalIdx] = output;
        }
        // To make PS happy with just UAV.
        dummy = outAlbedo;
    }
}
