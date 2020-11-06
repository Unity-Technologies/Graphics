Shader "Hidden/ShadowProjected2D"
{
    Properties
    {
        [PerRendererData][HideInInspector] _ColorMask("__ColorMask", Int) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull Off
        BlendOp Add
        Blend One Zero
        ZWrite Off

        // This pass writes projected shadows.
        // Bit 0 of stencil is whether we have written a pixel for this shadow group
        // Bit 1-7 is how many times a group has written to this pixel
        // If the group or a non grouped shadow has been written, fail. Otherwise pass and write to the group. (even numbers mean a shadow group has not been written, odd means they have)
        Pass
        {
            Stencil
            {
                WriteMask   1
                Ref         1
                Comp        NotEqual
                Pass        Replace
                Fail        Keep
            }

            //ColorMask [_ColorMask]
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 vertex : POSITION;
                float4 tangent: TANGENT;
                float4 extrusion : COLOR;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
            };

            uniform float3 _LightPos;
            uniform float  _ShadowRadius;

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 vertexWS = TransformObjectToWorld(v.vertex);  // This should be in world space
                float3 lightDir = _LightPos - vertexWS;
                lightDir.z = 0;

                // Start of code to see if this point should be extruded
                float3 lightDirection = normalize(lightDir);  

                float3 endpoint = vertexWS + (_ShadowRadius * -lightDirection);

                float3 worldTangent = TransformObjectToWorldDir(v.tangent.xyz);
                float sharedShadowTest = saturate(ceil(dot(lightDirection, worldTangent)));

                // Start of code to calculate offset
                float3 vertexWS0 = TransformObjectToWorld(float3(v.extrusion.xy, 0));
                float3 vertexWS1 = TransformObjectToWorld(float3(v.extrusion.zw, 0));
                float3 shadowDir0 = vertexWS0 - _LightPos;
                shadowDir0.z = 0;
                shadowDir0 = normalize(shadowDir0);

                float3 shadowDir1 = vertexWS1 -_LightPos;
                shadowDir1.z = 0;
                shadowDir1 = normalize(shadowDir1);

                float3 shadowDir = normalize(shadowDir0 + shadowDir1);


                float3 sharedShadowOffset = sharedShadowTest * _ShadowRadius * shadowDir;

                float3 position;
                position = vertexWS + sharedShadowOffset;

                o.vertex = TransformWorldToHClip(position);

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return half4(1,1,1,1);
            }
            ENDHLSL
        }
        // Set shadow bit and clear group bit.
        Pass
        {
            Stencil
            {
                Ref         2
                Comp        Always
                Pass        Replace
            }

            // We only want to change the stencil value in this pass
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 vertex : POSITION;
                float4 tangent: TANGENT;
                float4 extrusion : COLOR;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
            };

            uniform float3 _LightPos;
            uniform float  _ShadowRadius;

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 vertexWS = TransformObjectToWorld(v.vertex);  // This should be in world space
                float3 lightDir = _LightPos - vertexWS;
                lightDir.z = 0;

                // Start of code to see if this point should be extruded
                float3 lightDirection = normalize(lightDir);  

                float3 endpoint = vertexWS + (_ShadowRadius * -lightDirection);

                float3 worldTangent = TransformObjectToWorldDir(v.tangent.xyz);
                float sharedShadowTest = saturate(ceil(dot(lightDirection, worldTangent)));

                // Start of code to calculate offset
                float3 vertexWS0 = TransformObjectToWorld(float3(v.extrusion.xy, 0));
                float3 vertexWS1 = TransformObjectToWorld(float3(v.extrusion.zw, 0));
                float3 shadowDir0 = vertexWS0 - _LightPos;
                shadowDir0.z = 0;
                shadowDir0 = normalize(shadowDir0);

                float3 shadowDir1 = vertexWS1 -_LightPos;
                shadowDir1.z = 0;
                shadowDir1 = normalize(shadowDir1);

                float3 shadowDir = normalize(shadowDir0 + shadowDir1);


                float3 sharedShadowOffset = sharedShadowTest * _ShadowRadius * shadowDir;

                float3 position;
                position = vertexWS + sharedShadowOffset;

                o.vertex = TransformWorldToHClip(position);

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return half4(1,1,1,1);
            }
            ENDHLSL
        }
    }
}
