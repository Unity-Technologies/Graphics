Shader "Hidden/HDRP/CircularDOF"
{
        HLSLINCLUDE

        #pragma target 4.5
        #pragma editor_sync_compilation
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        #define KERNEL_RADIUS 8

        TEXTURE2D_X(_SourceBackbuffer);
        TEXTURE2D_X(_IntermediateRed);
        TEXTURE2D_X(_IntermediateGreen);
        TEXTURE2D_X(_IntermediateBlue);
        SamplerState sampler_LinearClamp;
        uniform float4 _ScaleBias;
        uniform float2 _TexelSize;

        struct Attributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        struct HorizontalOutput
        {
            float4 red : SV_Target0;
            float4 green : SV_Target1;
            float4 blue : SV_Target2;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
            float2 uv = GetFullScreenTriangleTexCoord(input.vertexID);

            output.positionCS = pos;
            output.texcoord = uv * _ScaleBias.xy + _ScaleBias.zw;
            return output;
        }

        HorizontalOutput FragHorizontal(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float4 Kernel0_RealX_ImY_RealZ_ImW[17];
            Kernel0_RealX_ImY_RealZ_ImW[0] = float4(/*XY: Non Bracketed*/0.014096, -0.022658,/*Bracketed WZ:*/0.055991, 0.004413);
            Kernel0_RealX_ImY_RealZ_ImW[1] = float4(/*XY: Non Bracketed*/-0.020612, -0.025574,/*Bracketed WZ:*/0.019188, 0.000000);
            Kernel0_RealX_ImY_RealZ_ImW[2] = float4(/*XY: Non Bracketed*/-0.038708, 0.006957,/*Bracketed WZ:*/0.000000, 0.049223);
            Kernel0_RealX_ImY_RealZ_ImW[3] = float4(/*XY: Non Bracketed*/-0.021449, 0.040468,/*Bracketed WZ:*/0.018301, 0.099929);
            Kernel0_RealX_ImY_RealZ_ImW[4] = float4(/*XY: Non Bracketed*/0.013015, 0.050223,/*Bracketed WZ:*/0.054845, 0.114689);
            Kernel0_RealX_ImY_RealZ_ImW[5] = float4(/*XY: Non Bracketed*/0.042178, 0.038585,/*Bracketed WZ:*/0.085769, 0.097080);
            Kernel0_RealX_ImY_RealZ_ImW[6] = float4(/*XY: Non Bracketed*/0.057972, 0.019812,/*Bracketed WZ:*/0.102517, 0.068674);
            Kernel0_RealX_ImY_RealZ_ImW[7] = float4(/*XY: Non Bracketed*/0.063647, 0.005252,/*Bracketed WZ:*/0.108535, 0.046643);
            Kernel0_RealX_ImY_RealZ_ImW[8] = float4(/*XY: Non Bracketed*/0.064754, 0.000000,/*Bracketed WZ:*/0.109709, 0.038697);
            Kernel0_RealX_ImY_RealZ_ImW[9] = float4(/*XY: Non Bracketed*/0.063647, 0.005252,/*Bracketed WZ:*/0.108535, 0.046643);
            Kernel0_RealX_ImY_RealZ_ImW[10] = float4(/*XY: Non Bracketed*/0.057972, 0.019812,/*Bracketed WZ:*/0.102517, 0.068674);
            Kernel0_RealX_ImY_RealZ_ImW[11] = float4(/*XY: Non Bracketed*/0.042178, 0.038585,/*Bracketed WZ:*/0.085769, 0.097080);
            Kernel0_RealX_ImY_RealZ_ImW[12] = float4(/*XY: Non Bracketed*/0.013015, 0.050223,/*Bracketed WZ:*/0.054845, 0.114689);
            Kernel0_RealX_ImY_RealZ_ImW[13] = float4(/*XY: Non Bracketed*/-0.021449, 0.040468,/*Bracketed WZ:*/0.018301, 0.099929);
            Kernel0_RealX_ImY_RealZ_ImW[14] = float4(/*XY: Non Bracketed*/-0.038708, 0.006957,/*Bracketed WZ:*/0.000000, 0.049223);
            Kernel0_RealX_ImY_RealZ_ImW[15] = float4(/*XY: Non Bracketed*/-0.020612, -0.025574,/*Bracketed WZ:*/0.019188, 0.000000);
            Kernel0_RealX_ImY_RealZ_ImW[16] = float4(/*XY: Non Bracketed*/0.014096, -0.022658,/*Bracketed WZ:*/0.055991, 0.004413);

            float4 Kernel1_RealX_ImY_RealZ_ImW[17];
            Kernel1_RealX_ImY_RealZ_ImW[0] = float4(/*XY: Non Bracketed*/0.000115, 0.009116,/*Bracketed WZ:*/0.000000, 0.051147);
            Kernel1_RealX_ImY_RealZ_ImW[1] = float4(/*XY: Non Bracketed*/0.005324, 0.013416,/*Bracketed WZ:*/0.009311, 0.075276);
            Kernel1_RealX_ImY_RealZ_ImW[2] = float4(/*XY: Non Bracketed*/0.013753, 0.016519,/*Bracketed WZ:*/0.024376, 0.092685);
            Kernel1_RealX_ImY_RealZ_ImW[3] = float4(/*XY: Non Bracketed*/0.024700, 0.017215,/*Bracketed WZ:*/0.043940, 0.096591);
            Kernel1_RealX_ImY_RealZ_ImW[4] = float4(/*XY: Non Bracketed*/0.036693, 0.015064,/*Bracketed WZ:*/0.065375, 0.084521);
            Kernel1_RealX_ImY_RealZ_ImW[5] = float4(/*XY: Non Bracketed*/0.047976, 0.010684,/*Bracketed WZ:*/0.085539, 0.059948);
            Kernel1_RealX_ImY_RealZ_ImW[6] = float4(/*XY: Non Bracketed*/0.057015, 0.005570,/*Bracketed WZ:*/0.101695, 0.031254);
            Kernel1_RealX_ImY_RealZ_ImW[7] = float4(/*XY: Non Bracketed*/0.062782, 0.001529,/*Bracketed WZ:*/0.112002, 0.008578);
            Kernel1_RealX_ImY_RealZ_ImW[8] = float4(/*XY: Non Bracketed*/0.064754, 0.000000,/*Bracketed WZ:*/0.115526, 0.000000);
            Kernel1_RealX_ImY_RealZ_ImW[9] = float4(/*XY: Non Bracketed*/0.062782, 0.001529,/*Bracketed WZ:*/0.112002, 0.008578);
            Kernel1_RealX_ImY_RealZ_ImW[10] = float4(/*XY: Non Bracketed*/0.057015, 0.005570,/*Bracketed WZ:*/0.101695, 0.031254);
            Kernel1_RealX_ImY_RealZ_ImW[11] = float4(/*XY: Non Bracketed*/0.047976, 0.010684,/*Bracketed WZ:*/0.085539, 0.059948);
            Kernel1_RealX_ImY_RealZ_ImW[12] = float4(/*XY: Non Bracketed*/0.036693, 0.015064,/*Bracketed WZ:*/0.065375, 0.084521);
            Kernel1_RealX_ImY_RealZ_ImW[13] = float4(/*XY: Non Bracketed*/0.024700, 0.017215,/*Bracketed WZ:*/0.043940, 0.096591);
            Kernel1_RealX_ImY_RealZ_ImW[14] = float4(/*XY: Non Bracketed*/0.013753, 0.016519,/*Bracketed WZ:*/0.024376, 0.092685);
            Kernel1_RealX_ImY_RealZ_ImW[15] = float4(/*XY: Non Bracketed*/0.005324, 0.013416,/*Bracketed WZ:*/0.009311, 0.075276);
            Kernel1_RealX_ImY_RealZ_ImW[16] = float4(/*XY: Non Bracketed*/0.000115, 0.009116,/*Bracketed WZ:*/0.000000, 0.051147);

            HorizontalOutput result;

            result.red = result.green = result.blue = float4(0, 0, 0, 0);

            float filterRadius = 1; // MAX_FILTER_SIZE, TODO: Get from CoC
            for (int i = -KERNEL_RADIUS; i <= KERNEL_RADIUS; ++i)
            {
                float2 coords = input.texcoord.xy + _TexelSize * float2(i, 0) * filterRadius;
                float3 colorSample = SAMPLE_TEXTURE2D_X_LOD(_SourceBackbuffer, sampler_LinearClamp, coords, 0).rgb;

                int filterIndex = i + KERNEL_RADIUS;
                float4 kernelWeights = float4(Kernel0_RealX_ImY_RealZ_ImW[filterIndex].xy, Kernel1_RealX_ImY_RealZ_ImW[filterIndex].xy);

                result.red += colorSample.r * kernelWeights;
                result.green += colorSample.g * kernelWeights;
                result.blue += colorSample.b * kernelWeights;
            }

            return result;
        }

        //(Pr+Pi)*(Qr+Qi) = (Pr*Qr+Pr*Qi+Pi*Qr-Pi*Qi)
        float2 multComplex(float2 p, float2 q)
        {
            return float2(p.x * q.x - p.y * q.y, p.x * q.y + p.y * q.x);
        }

        float4 FragVertical(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float4 Kernel0_RealX_ImY_RealZ_ImW[17];
            Kernel0_RealX_ImY_RealZ_ImW[0] = float4(/*XY: Non Bracketed*/0.014096, -0.022658,/*Bracketed WZ:*/0.055991, 0.004413);
            Kernel0_RealX_ImY_RealZ_ImW[1] = float4(/*XY: Non Bracketed*/-0.020612, -0.025574,/*Bracketed WZ:*/0.019188, 0.000000);
            Kernel0_RealX_ImY_RealZ_ImW[2] = float4(/*XY: Non Bracketed*/-0.038708, 0.006957,/*Bracketed WZ:*/0.000000, 0.049223);
            Kernel0_RealX_ImY_RealZ_ImW[3] = float4(/*XY: Non Bracketed*/-0.021449, 0.040468,/*Bracketed WZ:*/0.018301, 0.099929);
            Kernel0_RealX_ImY_RealZ_ImW[4] = float4(/*XY: Non Bracketed*/0.013015, 0.050223,/*Bracketed WZ:*/0.054845, 0.114689);
            Kernel0_RealX_ImY_RealZ_ImW[5] = float4(/*XY: Non Bracketed*/0.042178, 0.038585,/*Bracketed WZ:*/0.085769, 0.097080);
            Kernel0_RealX_ImY_RealZ_ImW[6] = float4(/*XY: Non Bracketed*/0.057972, 0.019812,/*Bracketed WZ:*/0.102517, 0.068674);
            Kernel0_RealX_ImY_RealZ_ImW[7] = float4(/*XY: Non Bracketed*/0.063647, 0.005252,/*Bracketed WZ:*/0.108535, 0.046643);
            Kernel0_RealX_ImY_RealZ_ImW[8] = float4(/*XY: Non Bracketed*/0.064754, 0.000000,/*Bracketed WZ:*/0.109709, 0.038697);
            Kernel0_RealX_ImY_RealZ_ImW[9] = float4(/*XY: Non Bracketed*/0.063647, 0.005252,/*Bracketed WZ:*/0.108535, 0.046643);
            Kernel0_RealX_ImY_RealZ_ImW[10] = float4(/*XY: Non Bracketed*/0.057972, 0.019812,/*Bracketed WZ:*/0.102517, 0.068674);
            Kernel0_RealX_ImY_RealZ_ImW[11] = float4(/*XY: Non Bracketed*/0.042178, 0.038585,/*Bracketed WZ:*/0.085769, 0.097080);
            Kernel0_RealX_ImY_RealZ_ImW[12] = float4(/*XY: Non Bracketed*/0.013015, 0.050223,/*Bracketed WZ:*/0.054845, 0.114689);
            Kernel0_RealX_ImY_RealZ_ImW[13] = float4(/*XY: Non Bracketed*/-0.021449, 0.040468,/*Bracketed WZ:*/0.018301, 0.099929);
            Kernel0_RealX_ImY_RealZ_ImW[14] = float4(/*XY: Non Bracketed*/-0.038708, 0.006957,/*Bracketed WZ:*/0.000000, 0.049223);
            Kernel0_RealX_ImY_RealZ_ImW[15] = float4(/*XY: Non Bracketed*/-0.020612, -0.025574,/*Bracketed WZ:*/0.019188, 0.000000);
            Kernel0_RealX_ImY_RealZ_ImW[16] = float4(/*XY: Non Bracketed*/0.014096, -0.022658,/*Bracketed WZ:*/0.055991, 0.004413);

            float4 Kernel1_RealX_ImY_RealZ_ImW[17];
            Kernel1_RealX_ImY_RealZ_ImW[0] = float4(/*XY: Non Bracketed*/0.000115, 0.009116,/*Bracketed WZ:*/0.000000, 0.051147);
            Kernel1_RealX_ImY_RealZ_ImW[1] = float4(/*XY: Non Bracketed*/0.005324, 0.013416,/*Bracketed WZ:*/0.009311, 0.075276);
            Kernel1_RealX_ImY_RealZ_ImW[2] = float4(/*XY: Non Bracketed*/0.013753, 0.016519,/*Bracketed WZ:*/0.024376, 0.092685);
            Kernel1_RealX_ImY_RealZ_ImW[3] = float4(/*XY: Non Bracketed*/0.024700, 0.017215,/*Bracketed WZ:*/0.043940, 0.096591);
            Kernel1_RealX_ImY_RealZ_ImW[4] = float4(/*XY: Non Bracketed*/0.036693, 0.015064,/*Bracketed WZ:*/0.065375, 0.084521);
            Kernel1_RealX_ImY_RealZ_ImW[5] = float4(/*XY: Non Bracketed*/0.047976, 0.010684,/*Bracketed WZ:*/0.085539, 0.059948);
            Kernel1_RealX_ImY_RealZ_ImW[6] = float4(/*XY: Non Bracketed*/0.057015, 0.005570,/*Bracketed WZ:*/0.101695, 0.031254);
            Kernel1_RealX_ImY_RealZ_ImW[7] = float4(/*XY: Non Bracketed*/0.062782, 0.001529,/*Bracketed WZ:*/0.112002, 0.008578);
            Kernel1_RealX_ImY_RealZ_ImW[8] = float4(/*XY: Non Bracketed*/0.064754, 0.000000,/*Bracketed WZ:*/0.115526, 0.000000);
            Kernel1_RealX_ImY_RealZ_ImW[9] = float4(/*XY: Non Bracketed*/0.062782, 0.001529,/*Bracketed WZ:*/0.112002, 0.008578);
            Kernel1_RealX_ImY_RealZ_ImW[10] = float4(/*XY: Non Bracketed*/0.057015, 0.005570,/*Bracketed WZ:*/0.101695, 0.031254);
            Kernel1_RealX_ImY_RealZ_ImW[11] = float4(/*XY: Non Bracketed*/0.047976, 0.010684,/*Bracketed WZ:*/0.085539, 0.059948);
            Kernel1_RealX_ImY_RealZ_ImW[12] = float4(/*XY: Non Bracketed*/0.036693, 0.015064,/*Bracketed WZ:*/0.065375, 0.084521);
            Kernel1_RealX_ImY_RealZ_ImW[13] = float4(/*XY: Non Bracketed*/0.024700, 0.017215,/*Bracketed WZ:*/0.043940, 0.096591);
            Kernel1_RealX_ImY_RealZ_ImW[14] = float4(/*XY: Non Bracketed*/0.013753, 0.016519,/*Bracketed WZ:*/0.024376, 0.092685);
            Kernel1_RealX_ImY_RealZ_ImW[15] = float4(/*XY: Non Bracketed*/0.005324, 0.013416,/*Bracketed WZ:*/0.009311, 0.075276);
            Kernel1_RealX_ImY_RealZ_ImW[16] = float4(/*XY: Non Bracketed*/0.000115, 0.009116,/*Bracketed WZ:*/0.000000, 0.051147);

            float4 valR = float4(0, 0, 0, 0);
            float4 valG = float4(0, 0, 0, 0);
            float4 valB = float4(0, 0, 0, 0);

            float filterRadius = 1; // MAX_FILTER_SIZE, TODO: Get from CoC
            for (int i = -KERNEL_RADIUS; i <= KERNEL_RADIUS; ++i)
            {
                float2 coords = input.texcoord.xy + _TexelSize * float2(0, i) * filterRadius;
                float4 colorSampleR = SAMPLE_TEXTURE2D_X_LOD(_IntermediateRed, sampler_LinearClamp, coords, 0);
                float4 colorSampleG = SAMPLE_TEXTURE2D_X_LOD(_IntermediateGreen, sampler_LinearClamp, coords, 0);
                float4 colorSampleB = SAMPLE_TEXTURE2D_X_LOD(_IntermediateBlue, sampler_LinearClamp, coords, 0);

                int filterIndex = i + KERNEL_RADIUS;
                float4 kernelWeights = float4(Kernel0_RealX_ImY_RealZ_ImW[filterIndex].xy, Kernel1_RealX_ImY_RealZ_ImW[filterIndex].xy);

                valR.xy += multComplex(colorSampleR.xy, kernelWeights.xy);
                valR.zw += multComplex(colorSampleR.zw, kernelWeights.zw);

                valG.xy += multComplex(colorSampleG.xy, kernelWeights.xy);
                valG.zw += multComplex(colorSampleG.zw, kernelWeights.zw);

                valB.xy += multComplex(colorSampleB.xy, kernelWeights.xy);
                valB.zw += multComplex(colorSampleB.zw, kernelWeights.zw);
            }

            const float2 Kernel0Weights_RealX_ImY = float2(0.411259, -0.548794);
            const float2 Kernel1Weights_RealX_ImY = float2(0.513282, 4.561110);
            const float4 KernelWeights_RealX_ImY = float4(Kernel0Weights_RealX_ImY, Kernel1Weights_RealX_ImY);

            float4 result = float4(0, 0, 0, 1);
            result.r = dot(valR, KernelWeights_RealX_ImY);
            result.g = dot(valG, KernelWeights_RealX_ImY);
            result.b = dot(valB, KernelWeights_RealX_ImY);
            return result;
        }

        ENDHLSL

        SubShader
        {
            Tags{ "RenderPipeline" = "HDRenderPipeline" }

            // 0: Horizontal
            Pass
            {
                ZWrite Off ZTest Always Blend Off Cull Off

                HLSLPROGRAM
                    #pragma vertex Vert
                    #pragma fragment FragHorizontal
                ENDHLSL
            }

            // 1: Vertical
            Pass
            {
                ZWrite Off ZTest Always Blend Off Cull Off

                HLSLPROGRAM
                    #pragma vertex Vert
                    #pragma fragment FragVertical
                ENDHLSL
            }
        }

        Fallback Off
}
