Shader "Hidden/FetchCameraPropertiesShader"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Nature/SpeedTree7BillboardInput.hlsl"

#define public
            struct CameraPropertiesData
            {
            public float4 _WorldSpaceCameraPos;
            public float4x4 _Reflection;
            public float4 _ScreenParams;
            public float4 _ProjectionParams;
            public float4 _ZBufferParams;
            public float4 unity_OrthoParams;
            public float4 unity_HalfStereoSeparation;
            public float4 unity_CameraWorldClipPlanes0;
            public float4 unity_CameraWorldClipPlanes1;
            public float4 unity_CameraWorldClipPlanes2;
            public float4 unity_CameraWorldClipPlanes3;
            public float4 unity_CameraWorldClipPlanes4;
            public float4 unity_CameraWorldClipPlanes5;
            public float4 unity_BillboardNormal;
            public float4 unity_BillboardTangent;
            public float4 unity_BillboardCameraParams;
            };

            uniform RWStructuredBuffer<CameraPropertiesData> _OutputData : register(u1);

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

                float4 frag() : SV_TARGET
            {
                _OutputData[0]._WorldSpaceCameraPos = float4(_WorldSpaceCameraPos, 0);
                //_OutputData[0]._Reflection = _Reflection;
                _OutputData[0]._ScreenParams = _ScreenParams;
                _OutputData[0]._ProjectionParams = _ProjectionParams;
                _OutputData[0]._ZBufferParams = _ZBufferParams;
                _OutputData[0].unity_OrthoParams = unity_OrthoParams;
                //_OutputData[0].unity_HalfStereoSeparation = unity_HalfStereoSeparation;
                _OutputData[0].unity_CameraWorldClipPlanes0 = unity_CameraWorldClipPlanes[0];
                _OutputData[0].unity_CameraWorldClipPlanes1 = unity_CameraWorldClipPlanes[1];
                _OutputData[0].unity_CameraWorldClipPlanes2 = unity_CameraWorldClipPlanes[2];
                _OutputData[0].unity_CameraWorldClipPlanes3 = unity_CameraWorldClipPlanes[3];
                _OutputData[0].unity_CameraWorldClipPlanes4 = unity_CameraWorldClipPlanes[4];
                _OutputData[0].unity_CameraWorldClipPlanes5 = unity_CameraWorldClipPlanes[5];
                _OutputData[0].unity_BillboardNormal = float4(unity_BillboardNormal, 0);
                _OutputData[0].unity_BillboardTangent = float4(unity_BillboardTangent, 0);
                _OutputData[0].unity_BillboardCameraParams = unity_BillboardCameraParams;
                return 1;
            }

            ENDHLSL
        }
    }
}
