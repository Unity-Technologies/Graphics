#include "UnityCG.cginc"

float4 VFXTransformPositionWorldToClip(float3 posWS)
{
    return UnityWorldToClipPos(posWS);
}

float4 VFXTransformPositionObjectToClip(float3 posOS)
{
    return UnityObjectToClipPos(posOS);
}

float4x4 VFXGetObjectToWorldMatrix()
{
    return unity_ObjectToWorld;
}

float4x4 VFXGetWorldToObjectMatrix()
{
    return unity_WorldToObject;
}

float3x3 VFXGetWorldToViewRotMatrix()
{
    return (float3x3)UNITY_MATRIX_V;
}

float3 VFXGetViewWorldPosition()
{
    // Not using _WorldSpaceCameraPos as it's not what expected for the shadow pass
    // (It remains primary camera pos not transposed inverse view position)
    return UNITY_MATRIX_I_V._m03_m13_m23;
}
