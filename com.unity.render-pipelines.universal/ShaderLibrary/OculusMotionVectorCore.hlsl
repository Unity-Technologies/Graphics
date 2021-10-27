#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 previousPositionOS : TEXCOORD4;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 curPositionCS : TEXCOORD8;
    float4 prevPositionCS : TEXCOORD9;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = vertexInput.positionCS;

    float3 curWS = TransformObjectToWorld(input.positionOS.xyz);
    output.curPositionCS = TransformWorldToHClip(curWS);
    if (unity_MotionVectorsParams.y == 0.0)
    {
        output.prevPositionCS = float4(0.0, 0.0, 0.0, 1.0);
    }
    else
    {
        bool hasDeformation = unity_MotionVectorsParams.x > 0.0;
        float3 effectivePositionOS = (hasDeformation ? input.previousPositionOS : input.positionOS.xyz);
        float3 previousWS = TransformPreviousObjectToWorld(effectivePositionOS);
        output.prevPositionCS = TransformWorldToPrevHClip(previousWS);
    }

    return output;
}

half4 frag(Varyings i) : SV_Target
{
    float3 screenPos = i.curPositionCS.xyz / i.curPositionCS.w;
    float3 screenPosPrev = i.prevPositionCS.xyz / i.prevPositionCS.w;
    half4 color = (1);
    color.xyz = screenPos - screenPosPrev;
    return color;
}
