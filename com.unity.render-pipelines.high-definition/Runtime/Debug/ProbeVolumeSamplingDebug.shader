Shader "Hidden/HDRP/ProbeVolumeSamplingDebug"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Opaque" }

        LOD 100

        HLSLINCLUDE
        #pragma editor_sync_compilation
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma multi_compile_fragment PROBE_VOLUMES_OFF PROBE_VOLUMES_L1 PROBE_VOLUMES_L2

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        #include "Packages/com.unity.render-pipelines.core/Runtime/Debug/ProbeVolumeDebugBase.hlsl"

        v2f vert(appdata v)
        {
            v2f o;

            float4 debugPosition = _positionNormalBuffer[0];
            float4 debugNormal = _positionNormalBuffer[1];

            float4 wsPos = float4(0.0f, 0.0f, 0.0f, 1.0f);
            float samplingFactor = 0.0f; // probe sampling weight (when needed) is compute in vertex shader. Usefull for drawing 8 debug quads showing weights

            float3 snappedProbePosition_WS; // worldspace position of main probe (a corner of the 8 probes cube)
            float3 samplingPositionNoAntiLeak_WS; // worldspace sampling position after applying 'NormalBias', 'ViewBias'
            float3 samplingPosition_WS; // worldspace sampling position after applying 'NormalBias', 'ViewBias' and 'ValidityAndNormalBased Leak Reduction'
            float probeDistance;
            float3 normalizedOffset; // normalized offset between sampling position and snappedProbePosition
            float validityWeights[8];
            float validityWeight = 1.0f;

            FindSamplingData(debugPosition.xyz, debugNormal.xyz, snappedProbePosition_WS, samplingPosition_WS, samplingPositionNoAntiLeak_WS, probeDistance, normalizedOffset, validityWeights);

            // QUADS to write the sampling factor of each probe
            // each QUAD has an individual ID in vertex color blue channel
            if (v.color.z)
            {
                // QUAD 01
                float3 quadPosition = snappedProbePosition_WS;
                validityWeight = validityWeights [0];

                // QUAD 02
                if (abs(v.color.z-0.2f)<0.02f)
                {
                    quadPosition = snappedProbePosition_WS + float3(0.0f, 1.0f, 0.0f)*probeDistance;
                    validityWeight = validityWeights [2];
                }

                // QUAD 03
                if (abs(v.color.z-0.3f)<0.02f)
                {
                    quadPosition = snappedProbePosition_WS + float3(1.0f, 1.0f, 0.0f)*probeDistance;
                    validityWeight = validityWeights [3];
                }

                // QUAD 04
                if (abs(v.color.z-0.4f)<0.02f)
                {
                    quadPosition = snappedProbePosition_WS + float3(1.0f, 0.0f, 0.0f)*probeDistance;
                    validityWeight = validityWeights [1];
                }

                // QUAD 05
                if (abs(v.color.z-0.5f)<0.02f)
                {
                    quadPosition = snappedProbePosition_WS + float3(0.0f, 0.0f, 1.0f)*probeDistance;
                    validityWeight = validityWeights [4];
                }

                // QUAD 06
                if (abs(v.color.z-0.6f)<0.02f)
                {
                    quadPosition = snappedProbePosition_WS + float3(0.0f, 1.0f, 1.0f)*probeDistance;
                    validityWeight = validityWeights [6];
                }

                // QUAD 07
                if (abs(v.color.z-0.7f)<0.02f)
                {
                    quadPosition = snappedProbePosition_WS + float3(1.0f, 1.0f, 1.0f)*probeDistance;
                    validityWeight = validityWeights [7];
                }

                // QUAD 08
                if (abs(v.color.z-0.8f)<0.02f)
                {
                    quadPosition = snappedProbePosition_WS + float3(1.0f, 0.0f, 1.0f)*probeDistance;
                    validityWeight = validityWeights [5];
                }

                samplingFactor = ComputeSamplingFactor(quadPosition, snappedProbePosition_WS, normalizedOffset, probeDistance);

                float4 cameraUp = mul(UNITY_MATRIX_I_V, float4(0.0f, 1.0f, 0.0f, 0.0f));
                float4 cameraRight = -mul(UNITY_MATRIX_I_V, float4(1.0f, 0.0f, 0.0f, 0.0f));

                wsPos = mul(UNITY_MATRIX_M, float4(0.0f, 0.0f, 0.0f, 1.0f));
                wsPos += float4(quadPosition + cameraUp.xyz * _ProbeSize/1.5f, 0.0f);
                wsPos += float4((v.vertex.x*cameraRight.xyz + v.vertex.y*cameraUp.xyz * 0.5f)*20.0f*_ProbeSize, 0.0f);
            }

            // ARROW to show the position and normal of the debugged fragment
            else if (v.color.y)
            {
                float3 forward = normalize(debugNormal.xyz);
                float3 up = float3(0.0f, 1.0f, 0.0f); if (dot(up, forward) > 0.9f) {up = float3(1.0f, 0.0f, 0.0f);}
                float3 right = normalize(cross(forward, up));
                up = cross(right, forward);
                float3x3  orientation = float3x3  ( right.x,   up.x,    forward.x,
                                                    right.y,   up.y,    forward.y,
                                                    right.z,   up.z,    forward.z);

                wsPos = float4(mul(orientation, (v.vertex.xyz * _ProbeSize * 5.0f)), 1.0f);
                wsPos = mul(UNITY_MATRIX_M, wsPos);
                wsPos.xyz += debugPosition.xyz;
            }

            // LOCATOR to debug sampling position
            else
            {
                if (v.color.x) // DEBUG NORMAL + VIEW BIAS
                {
                    if (_ForceDebugNormalViewBias)
                    {
                        wsPos = mul(UNITY_MATRIX_M, float4(v.vertex.xyz * _ProbeSize * 1.5f, 1.0f));
                        wsPos += float4(samplingPositionNoAntiLeak_WS, 0.0f);
                    }
                    else
                    {
                        DoCull(o);
                        return o;
                    }
                }
                else // DEBUG NORMAL + VIEW BIAS + ANTI LEAK
                {
                    wsPos = mul(UNITY_MATRIX_M, float4(v.vertex.xyz * _ProbeSize * 3.0f, 1.0f));
                    wsPos += float4(snappedProbePosition_WS + normalizedOffset*probeDistance, 0.0f);
                }
            }

            float4 pos = mul(UNITY_MATRIX_VP, wsPos);
            float remappedDepth = Remap(-1.0f, 1.0f, 0.6f, 1.0f, pos.z); // remapped depth to draw gizmo on top of most other objects
            o.vertex = float4(pos.x, pos.y, remappedDepth * pos.w, pos.w);
            o.normal = normalize(mul(v.normal, (float3x3)UNITY_MATRIX_M));
            o.color = v.color;
            o.texCoord = v.texCoord;
            o.samplingFactor_ValidityWeight = float2(samplingFactor, validityWeight);

            return o;
        }


        float4 frag(v2f i) : SV_Target
        {

            // QUADS to write the sampling factor of each probe
            if (i.color.z)
            {
                float samplingFactor = i.samplingFactor_ValidityWeight.x;
                float validityWeight = i.samplingFactor_ValidityWeight.y;
                half4 color = WriteFractNumber(samplingFactor,  i.texCoord);
                if (validityWeight>0.0f)
                    color = lerp(half4(0.0f, 0.0f, 0.0f, 1.0f), half4(0.0f, 1.0f, 0.0f, 1.0f), color.x);
                else
                    color = lerp(half4(1.0f, 1.0f, 1.0f, 1.0f), half4(1.0f, 0.0f, 0.0f, 1.0f), color.x);
                return color;
            }

            // ARROW to show debugging position and normal
            else if (i.color.y)
            {
                return _DebugArrowColor;
            }

            // LOCATOR to debug sampling position
            else
            {
                if (i.color.x) // DEBUG NORMAL + VIEW BIAS
                    return _DebugLocator02Color;
                else // DEBUG NORMAL + VIEW BIAS + ANTILEAK MODE
                    return _DebugLocator01Color;
            }
        }
        ENDHLSL

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
