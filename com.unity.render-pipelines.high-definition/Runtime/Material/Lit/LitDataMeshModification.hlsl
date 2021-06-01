// Note: positionWS can be either in camera relative space or not
float3 GetVertexDisplacement(float3 positionRWS, float3 normalWS, float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3, float4 vertexColor)
{
    // This call will work for both LayeredLit and Lit shader
    LayerTexCoord layerTexCoord;
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);
    GetLayerTexCoord(texCoord0, texCoord1, texCoord2, texCoord3, positionRWS, normalWS, layerTexCoord);

    // TODO: do this algorithm for lod fetching as lod not available in vertex/domain shader
    // http://www.sebastiansylvan.com/post/the-problem-with-tessellation-in-directx-11/
    float lod = 0.0;
    return ComputePerVertexDisplacement(layerTexCoord, vertexColor, lod) * normalWS;
}

// Note: positionWS can be either in camera relative space or not
void ApplyVertexModification(AttributesMesh input, float3 normalWS, inout float3 positionRWS, float3 timeParameters)
{
#if defined(_VERTEX_DISPLACEMENT)

    positionRWS += GetVertexDisplacement(positionRWS, normalWS,
    #ifdef ATTRIBUTES_NEED_TEXCOORD0
        input.uv0,
    #else
        float2(0.0, 0.0),
    #endif
    #ifdef ATTRIBUTES_NEED_TEXCOORD1
        input.uv1,
    #else
        float2(0.0, 0.0),
    #endif
    #ifdef ATTRIBUTES_NEED_TEXCOORD2
        input.uv2,
    #else
        float2(0.0, 0.0),
    #endif
    #ifdef ATTRIBUTES_NEED_TEXCOORD3
        input.uv3,
    #else
        float2(0.0, 0.0),
    #endif
    #ifdef ATTRIBUTES_NEED_COLOR
        input.color
    #else
        float4(0.0, 0.0, 0.0, 0.0)
    #endif
        );
#endif
}

#ifdef TESSELLATION_ON

float GetTessellationFactor(AttributesMesh input)
{
    return _TessellationFactor;
}

VaryingsMeshToDS ApplyTessellationModification(VaryingsMeshToDS input, float3 timeParameters)
{
#if defined(_TESSELLATION_DISPLACEMENT)

    input.positionRWS += GetVertexDisplacement(input.positionRWS, input.normalWS,
    #ifdef VARYINGS_DS_NEED_TEXCOORD0
        input.texCoord0.xy,
    #else
        float2(0.0, 0.0),
    #endif
    #ifdef VARYINGS_DS_NEED_TEXCOORD1
        input.texCoord1.xy,
    #else
        float2(0.0, 0.0),
    #endif
    #ifdef VARYINGS_DS_NEED_TEXCOORD2
        input.texCoord2.xy,
    #else
        float2(0.0, 0.0),
    #endif
    #ifdef VARYINGS_DS_NEED_TEXCOORD3
        input.texCoord3.xy,
    #else
        float2(0.0, 0.0),
    #endif
    #ifdef VARYINGS_DS_NEED_COLOR
        input.color
    #else
        float4(0.0, 0.0, 0.0, 0.0)
    #endif
        );

#endif // _TESSELLATION_DISPLACEMENT

    return input;
}

#endif // #ifdef TESSELLATION_ON
