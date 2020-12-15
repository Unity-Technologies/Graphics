Shader "Hidden/Universal Render Pipeline/Debug/DebugReplacement"
{
    SubShader
    {
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}

        Pass
        {
            Name "Overdraw"
            Tags {"LightMode" = "UniversalForward"}

            Blend One One
            ZWrite On
            Cull Back

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard SRP library
            // All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
            #pragma prefer_hlslcc gles
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

        Pass
        {
            Name "Wireframe"
            Tags {"LightMode" = "UniversalForward"}

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard SRP library
            // All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
            #pragma prefer_hlslcc gles
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

        Pass
        {
            Name "Vertex Attributes"
            Tags {"LightMode" = "UniversalForward"}

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard SRP library
            // All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex vert
            #pragma fragment frag

            #define _DEBUG_SHADER

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
                switch (_DebugAttributesIndex)
                {
                    case VERTEXATTRIBUTEDEBUGMODE_TEXCOORD0:
                        return input.texcoord0;//half4(,0,1);
                    case VERTEXATTRIBUTEDEBUGMODE_TEXCOORD1:
                        return input.texcoord1;//half4(input.texcoord1,0,1);
                    case VERTEXATTRIBUTEDEBUGMODE_TEXCOORD2:
                        return input.texcoord2;
                    case VERTEXATTRIBUTEDEBUGMODE_TEXCOORD3:
                        return input.texcoord3;
                    case VERTEXATTRIBUTEDEBUGMODE_COLOR:
                        return input.color;
                    case VERTEXATTRIBUTEDEBUGMODE_TANGENT:
                        return input.tangent;
                    case VERTEXATTRIBUTEDEBUGMODE_NORMAL:
                        return input.normal;
                    default:
                        return half4(0, 0, 0, 1);
                }
            }
            ENDHLSL
        }
    }
}
