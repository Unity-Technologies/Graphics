Shader "Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion"
{

HLSLINCLUDE
        #pragma prefer_hlslcc gles
        #pragma exclude_renderers d3d11_9x
        //Keep compiler quiet about Shadows.hlsl.
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AO.hlsl"

        TEXTURE2D(_CameraDepthNormalTexture);
        SAMPLER(sampler_CameraDepthNormalTexture);


        struct Attributes
        {
            float4 positionOS   : POSITION;
            float2 texcoord : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            half4  positionCS   : SV_POSITION;
            half4  uv           : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vertex(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

            float4 projPos = output.positionCS * 0.5;
            projPos.xy = projPos.xy + projPos.w;

            output.uv.xy = UnityStereoTransformScreenSpaceTex(input.texcoord);
            output.uv.zw = projPos.xy;

            return output;
        }

        half4 Fragment(Varyings input) : SV_Target
        {
            const float3 sample_sphere[16] = {
                float3( 0.5381, 0.1856, 0.4319), float3( 0.1379, 0.2486, 0.4430),
                float3( 0.3371, 0.5679, 0.0057), float3(-0.6999,-0.0451, 0.0019),
                float3( 0.0689,-0.1598, 0.8547), float3( 0.0560, 0.0069, 0.1843),
                float3(-0.0146, 0.1402, 0.0762), float3( 0.0100,-0.1924, 0.0344),
                float3(-0.3577,-0.5301, 0.4358), float3(-0.3169, 0.1063, 0.0158),
                float3( 0.0103,-0.5869, 0.0046), float3(-0.0897,-0.4940, 0.3287),
                float3( 0.7119,-0.0154, 0.0918), float3(-0.0533, 0.0596, 0.5411),
                float3( 0.0352,-0.0631, 0.5460), float3(-0.4776, 0.2847, 0.0271)
            };

            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float deviceDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv.xy).r;
            deviceDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthNormalTexture, sampler_CameraDepthNormalTexture, input.uv.xy).r;
            //return deviceDepth;
            #if UNITY_REVERSED_Z
                deviceDepth = 1 - deviceDepth;
            #endif
            deviceDepth = 2 * deviceDepth - 1; //NOTE: Currently must massage depth before computing CS position.

            float3 vpos = ComputeViewSpacePosition(input.uv.zw, deviceDepth, unity_CameraInvProjection);
            float3 wpos = mul(unity_CameraToWorld, float4(vpos, 1)).xyz;

            //half4 ao = SSAO_V4(input.uv.xy, vpos);
            half4 ao = SSAO_V2(input.uv.xy, vpos);
            //return half4(ao);

            float ssao_bias = -0.1;
            float3 debug = 0;

            // Screen position of the pixel
            float2 frag_coord = input.uv.xy;
            float3 normal = ReconstructNormals(frag_coord);
            float3 position = vpos;
            //position.z = SAMPLE_DEPTH_AO(frag_coord);

            // Orientate the kernel sample hemisphere randomly
            float2 noiseCoords = frag_coord * ( _ScreenParams.xy / _NoiseTex_TexelSize.zw);
            float3 rvec = normalize((SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex , noiseCoords) * 2 - 1) * float3(1, 1, 0));
            float3 tangent = normalize(rvec - normal * dot(rvec, normal));
            float3 bitangent = cross(normal, tangent);
            float3x3 tbn = float3x3(tangent, bitangent, normal);

            half ambient_occlusion = 0.0;
            for (int i = 0; i < _SSAO_Samples; i++)
            {
                // 1. Get sample point (view space)
                float3 sampled = mul(sample_sphere[i], tbn);
                if (dot(normal, sampled) >= 0.0) { sampled = -sampled; }
                sampled = position + sampled * _AO_Radius;
                // 2. Generate sample depth from sample point
                float4 offset = float4(sampled, 1.0);
                offset = mul(ProjectionMatrix, offset);
                offset.xy /= offset.w;
                offset.xy = offset.xy * 0.5 + 0.5;
                offset.xy = 1 - offset.xy;

                debug = offset.xyz;
                // 3. Lookup depth at sample's position (screen space)
                float point_depth = SAMPLE_DEPTH_AO(offset.xy);

                // 4. Compare with geometry depth value
                if (point_depth >= sampled.z + ssao_bias) { ambient_occlusion += 1.0; }
            }

            // Calculate the average and invert to get OCCLUSION
            ambient_occlusion = (ambient_occlusion / float(_SSAO_Samples));
            // Enhance the effect
            ambient_occlusion = pow(ambient_occlusion, _AO_Intensity);

            /*
            // Screen position of the pixel
            vec2 frag_coord = vec2(gl_FragCoord.x / scr_w, gl_FragCoord.y / scr_h);
            vec3 normal = texture(normal_sampler, frag_coord.xy).xyz;
            vec3 position = texture(position_sampler, frag_coord.xy).xyz;

            // Orientate the kernel sample hemisphere randomly
            vec3 rvec = texture(noise_sampler, gl_FragCoord.xy * noise_scale).xyz; // Picks random vector to orient the hemisphere
            vec3 tangent = normalize(rvec - normal * dot(rvec, normal));
            vec3 bitangent = cross(normal, tangent);
            mat3 tbn = mat3(tangent, bitangent, normal); // f: Tangent -> View space

            ambient_occlusion = 0.0;
            const uint num_ssao_samples = 64;
            for (int i = 0; i < num_ssao_samples; i++) {
                // 1. Get sample point (view space)
                vec3 sampled = position + tbn * ssao_samples[i] * ssao_kernel_radius;

                // 2. Generate sample depth from sample point
                vec4 point = vec4(sampled, 1.0);
                point = projection * point;
                point.xy /= point.w;
                point.xy = point.xy * 0.5 + 0.5;

                // 3. Lookup depth at sample's position (screen space)
                float point_depth = texture(position_sampler, point.xy).z;

                // 4. Compare with geometry depth value
                if (point_depth >= sampled.z + ssao_bias) { ambient_occlusion += 1.0; }
            }
            // Calculate the average and invert to get OCCLUSION
            ambient_occlusion = 1.0 - (ambient_occlusion / float(num_ssao_samples));
            // Enhance the effect
            ambient_occlusion = pow(ambient_occlusion, ssao_power);
            */

            return half4(ambient_occlusion.xxx, 1);
            return half4(debug, 1);
        }

        #define E 2.71828182846

        half4 DepthBlur(TEXTURE2D_PARAM(tex, samplerTex), float2 uv, float2 texelSize)
        {
            float4 d = 0;
            half amount = 0;
            half4 s = 0;
            float sum = 0;
            //s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xy));

            int samples = 10;
            half _BlurSize = 0.1;
            half _StandardDeviation = 0.1;

            for(int i = 0; i < samples; i++)
            {
                //get the offset of the sample
                float offset = (i/(samples) - 0.5) * _BlurSize;
                //get uv coordinate of sample
                float2 offsetUV = uv + float2(offset,0);
                //calculate the result of the gaussian function
                float stDevSquared = _StandardDeviation*_StandardDeviation;
                float gauss = (1 / sqrt(2*PI*stDevSquared)) * pow(E, -((offset*offset)/(2*stDevSquared)));
                //add result to sum
                sum += gauss;
                //multiply color with influence from gaussian function and add it to sum color
                s += SAMPLE_TEXTURE2D(tex, samplerTex, offsetUV) * gauss;
            }

            return s / samples;
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Name "ScreenSpaceAmbientOcclusion"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex   Vertex
            #pragma fragment Fragment
            ENDHLSL
        }

        Pass
        {
            Name "DepthBlur"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma vertex Vertex
            #pragma fragment FragBoxDownsample

            TEXTURE2D(_ScreenSpaceAOTexture);
            SAMPLER(sampler_ScreenSpaceAOTexture);
            float4 _ScreenSpaceAOTexture_TexelSize;

            float _SampleOffset;

            half4 FragBoxDownsample(Varyings input) : SV_Target
            {
                half4 col = DepthBlur(TEXTURE2D_ARGS(_ScreenSpaceAOTexture, sampler_ScreenSpaceAOTexture), input.uv, _ScreenSpaceAOTexture_TexelSize.xy);
                return half4(col.rgb, 1);
            }
            ENDHLSL
        }
    }
}
