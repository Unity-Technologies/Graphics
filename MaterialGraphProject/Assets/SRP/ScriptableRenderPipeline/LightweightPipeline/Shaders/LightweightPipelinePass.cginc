#ifndef LIGHTWEIGHT_PASS_INCLUDED
#define LIGHTWEIGHT_PASS_INCLUDED

			float4 shadowVert(float4 pos : POSITION) : SV_POSITION
            {
                float4 clipPos = UnityObjectToClipPos(pos);
#if defined(UNITY_REVERSED_Z)
                clipPos.z = min(clipPos.z, UNITY_NEAR_CLIP_VALUE);
#else
                clipPos.z = max(clipPos.z, UNITY_NEAR_CLIP_VALUE);
#endif
                return clipPos;
            }

            half4 shadowFrag() : SV_TARGET
            {
                return 0;
            }

			float4 depthVert(float4 pos : POSITION) : SV_POSITION
			{
				return UnityObjectToClipPos(pos);
			}

			half4 depthFrag() : SV_TARGET
			{
				return 0;
			}

			#include "UnityStandardMeta.cginc"
			#include "LightweightPipelineInput.cginc"

			fixed4 frag_meta_ld(v2f_meta i) : SV_Target
            {
                UnityMetaInput o;
                UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

                o.Albedo = Albedo(i.uv);

                half4 specularColor;
                SpecularGloss(i.uv.xy, 1.0, specularColor);
                o.SpecularColor = specularColor;

#ifdef _EMISSION
                o.Emission += LIGHTWEIGHT_GAMMA_TO_LINEAR(tex2D(_EmissionMap, i.uv).rgb) * _EmissionColor;
#else
                o.Emission += _EmissionColor;
#endif

                return UnityMetaFragment(o);
            }

#endif