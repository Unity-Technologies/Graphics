#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

void GetVaryingsDataDebug(uint paramId, FragInput input, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
    case DEBUGVIEW_VARYING_TEXCOORD0:
        result = float3(input.texCoord0 * 0.5 + 0.5, 0.0);
        break;
    case DEBUGVIEW_VARYING_TEXCOORD1:
        result = float3(input.texCoord1 * 0.5 + 0.5, 0.0);
        break;
    case DEBUGVIEW_VARYING_TEXCOORD2:
        result = float3(input.texCoord2 * 0.5 + 0.5, 0.0);
        break;
    case DEBUGVIEW_VARYING_VERTEXTANGENTWS:
        result = input.tangentToWorld[0].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEW_VARYING_VERTEXBITANGENTWS:
        result = input.tangentToWorld[1].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEW_VARYING_VERTEXNORMALWS:
        result = input.tangentToWorld[2].xyz * 0.5 + 0.5;
        break;
    }
}
