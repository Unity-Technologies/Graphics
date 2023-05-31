#if (SHADERPASS != SHADERPASS_ATLAS_PROJECTOR)
#error SHADERPASS_is_not_correctly_define
#endif

uniform float4 _DiffuseScaleBias;
uniform float4 _NormalScaleBias;
uniform float4 _MaskScaleBias;
uniform float4 _TextureTypes;

struct Attributes
{
    uint vertexID : SV_VertexID;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    uint type : TEXCOORD1;
};

float4 GetOutputPositionCS(float4 pos, float4 scaleBias)
{
    float4 positionCS = pos * float4(scaleBias.x, scaleBias.y, 1, 1) + float4(scaleBias.z, scaleBias.w, 0, 0);
    positionCS.xy = positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
    return positionCS;
}

Varyings Vert(Attributes input)
{
    Varyings output;
    uint vertexID = input.vertexID % 4;
    float4 pos = GetQuadVertexPosition(vertexID);    

    const uint type = _TextureTypes[input.vertexID / 4];
    float4 scaleBias = float4(0, 0, 0, 0);
    switch (type)
    {
        case DECALATLASTEXTURETYPE_DIFFUSE:
            scaleBias = _DiffuseScaleBias; break;
        case DECALATLASTEXTURETYPE_NORMAL:
            scaleBias = _NormalScaleBias; break;
        case DECALATLASTEXTURETYPE_MASK:
            scaleBias = _MaskScaleBias; break;
    }

    output.positionCS = GetOutputPositionCS(pos, scaleBias);
    output.uv = GetQuadTexCoord(vertexID);
    output.type = type;
    return output;
}

void Frag(Varyings input,
    out float4 outColor : SV_Target0)
{
    FragInputs fragInputs = (FragInputs)0;
    
#ifdef FRAG_INPUTS_USE_TEXCOORD0
    fragInputs.texCoord0.xy = input.uv;
#endif
#ifdef FRAG_INPUTS_USE_TEXCOORD1
    fragInputs.texCoord1.xy = input.uv;
#endif
#ifdef FRAG_INPUTS_USE_TEXCOORD2
    fragInputs.texCoord2.xy = input.uv;
#endif
#ifdef FRAG_INPUTS_USE_TEXCOORD3
    fragInputs.texCoord3.xy = input.uv;
#endif
    
    SurfaceDescriptionInputs inputs = FragInputsToSurfaceDescriptionInputs(fragInputs, float3(0,0,0));
    SurfaceDescription surface = SurfaceDescriptionFunction(inputs);
    switch (input.type)
    {
        case DECALATLASTEXTURETYPE_DIFFUSE:
        {
            outColor = float4(surface.BaseColor, surface.Alpha);
            break;
        }
        case DECALATLASTEXTURETYPE_NORMAL:
        {
            // Perform the same logic as UnpackNormalAG in Packing.hlsl in reverse
            const float2 compressedNormal = (surface.NormalTS.xy + 1.0f) * 0.5f;
            // Store alpha into b channel since that one is not being used during the unpacking
            outColor = float4(1.0f, compressedNormal.y, surface.NormalAlpha, compressedNormal.x);
            break;
        }
        case DECALATLASTEXTURETYPE_MASK:
        {
            outColor = float4(surface.Metallic, surface.Occlusion, surface.MAOSAlpha, surface.Smoothness);
            break;
        }
        default:
        {
            outColor = float4(0, 0, 0, 0);
            break;
        }
    }
}
