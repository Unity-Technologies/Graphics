Shader "Hidden/Universal Render Pipeline/Debug/Replacement"
{
    SubShader
    {
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}

        Pass
        {
            Tags {"LightMode" = "UniversalForward"}

            Blend One One
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debugging.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return kRedColor * half4(0.1, 0.1, 0.1, 1.0);
            }

            ENDHLSL
        }

        // Wireframe
        Pass
        {
            Tags {"LightMode" = "UniversalForward"}

            HLSLPROGRAM
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _DebugColor;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(_DebugColor, 1.0);
            }

            ENDHLSL
        }

        //Attribute debugger
        Pass
        {
            Tags {"LightMode" = "UniversalForward"}

            HLSLPROGRAM
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debugging.hlsl"

            struct Attributes
            {
                float4 positionOS        : POSITION;
                float4 texcoord0         : TEXCOORD0;
                float4 texcoord1         : TEXCOORD1;
                float4 texcoord2         : TEXCOORD2;
                float4 texcoord3         : TEXCOORD3;
                float4 color             : COLOR;
                float4 normal            : NORMAL;
                float4 tangent           : TANGENT;

            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 texcoord0         : TEXCOORD0;
                float4 texcoord1         : TEXCOORD1;
                float4 texcoord2         : TEXCOORD2;
                float4 texcoord3         : TEXCOORD3;
                float4 color             : COLOR;
                float4 normal            : NORMAL;
                float4 tangent           : TANGENT;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.texcoord0 = input.texcoord0;
                output.texcoord1 = input.texcoord1;
                output.texcoord2 = input.texcoord2;
                output.texcoord3 = input.texcoord3;
                output.color     = input.color;
                output.normal    = input.normal;
                output.tangent   = input.tangent;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                half4 color;
                switch (_DebugAttributesIndex)
                {
                    case DEBUG_ATTRIBUTE_TEXCOORD0:
                        color = input.texcoord0;//half4(,0,1);
                        break;
                    case DEBUG_ATTRIBUTE_TEXCOORD1:
                        color = input.texcoord1;//half4(input.texcoord1,0,1);
                        break;
                    case DEBUG_ATTRIBUTE_TEXCOORD2:
                        color = input.texcoord2;
                        break;
                    case DEBUG_ATTRIBUTE_TEXCOORD3:
                        color = input.texcoord3;
                        break;
                    case DEBUG_ATTRIBUTE_COLOR:
                        color = input.color;
                        break;
                    case DEBUG_ATTRIBUTE_TANGENT:
                        color = input.tangent;
                        break;
                    case DEBUG_ATTRIBUTE_NORMAL:
                        color = input.normal;
                        break;
                    default:
                        break;
                }
                return color;
            }

            ENDHLSL

        }
    }
}
