Shader "Unlit/WorldPos"
{
	Properties
	{
		[HideInInspector] _Color("Color", Color) = (1,1,1)
	}
		SubShader
		{
			Tags { "RenderType" = "Transparent" "Queue" = "Transparent+100" "DisableBatching" = "True" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
			Blend One OneMinusSrcAlpha
			ZTest Always
			ZWrite Off

			Pass
			{
				Tags { "LightMode" = "UniversalForward" }
				HLSLPROGRAM
				#pragma prefer_hlslcc gles
				#pragma exclude_renderers d3d11_9x
				#pragma target 3.0
				#pragma vertex vert
				#pragma fragment frag

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

				struct appdata
				{
					float4 vertex : POSITION;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					float4 pos     : SV_POSITION;
					float4 scrPos  : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				v2f vert(appdata v)
				{
					v2f o;

					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

					o.pos = TransformObjectToHClip(v.vertex.xyz);
					o.scrPos = ComputeScreenPos(o.pos);

					return o;
				}

				half4 frag(v2f i) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(i);
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                    float2 uv = i.scrPos.xy / i.scrPos.w;
                    float depth = SampleSceneDepth(uv);
#if !UNITY_REVERSED_Z
                    // On OpenGL, we need to transform depth from the [0, 1] range used in the depth buffer to the
                    // [-1, 1] range used in clip space
                    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, depth);
#endif
                    float4 raw   = mul(UNITY_MATRIX_I_VP, float4(uv * 2 - 1, depth, 1));
                    float3 wpos  = raw.xyz / raw.w;
                    if (distance(wpos, _WorldSpaceCameraPos) > 8.0) return half4(0,0,1,1);
                    return 0;

				}
				ENDHLSL
			}

		}
}
