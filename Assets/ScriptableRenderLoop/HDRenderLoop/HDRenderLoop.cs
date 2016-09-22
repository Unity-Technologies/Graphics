using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System;

using UnityEditor;

namespace UnityEngine.ScriptableRenderLoop
{
	public class HDRenderLoop : ScriptableRenderLoop
	{
        [MenuItem("Renderloop/CreateHDRenderLoop")]
		static void CreateHDRenderLoop()
		{
			var instance = ScriptableObject.CreateInstance<HDRenderLoop>();
			AssetDatabase.CreateAsset(instance, "Assets/HDRenderLoop.asset");
		}

		//[SerializeField]
		//ShadowSettings m_ShadowSettings = ShadowSettings.Default;
		//ShadowRenderPass m_ShadowPass;

        static private ComputeBuffer m_punctualLightList;
        public const int MAX_LIGHTS = 1024;


		void OnEnable()
		{
			Rebuild ();
		}

		void OnValidate()
		{
			Rebuild ();
		}

        void ClearComputeBuffers()
        {
            if (m_punctualLightList != null)
                m_punctualLightList.Release();
        }

		void Rebuild()
		{
            ClearComputeBuffers();

			// m_ShadowPass = new ShadowRenderPass (m_ShadowSettings);
            m_punctualLightList = new ComputeBuffer(MAX_LIGHTS, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PunctualLightData)));
		}

        void OnDisable()
        {
            m_punctualLightList.Release();
        }

		//---------------------------------------------------------------------------------------------------------------------------------------------------

        void UpdatePunctualLights(ActiveLight[] activeLights)
        {
            int punctualLightCount = 0;
            List<PunctualLightData> lights = new List<PunctualLightData>();

            for (int lightIndex = 0; lightIndex < Math.Min(activeLights.Length, MAX_LIGHTS); lightIndex++)
            {
                ActiveLight light = activeLights[lightIndex];
                if (light.lightType == LightType.Spot || light.lightType == LightType.Point)
                {
                    Matrix4x4 lightToWorld = light.localToWorld;

                    PunctualLightData l = new PunctualLightData();

                    l.positionWS = lightToWorld.GetColumn(3);
                    l.invSqrAttenuationRadius  = 1.0f / (light.range * light.range);
                    l.color = new Vec3(light.finalColor.r, light.finalColor.g, light.finalColor.b);
                    l.forward = lightToWorld.GetColumn(0);
                    l.up =  lightToWorld.GetColumn(1);
                    l.right =  lightToWorld.GetColumn(2);
                    
                    l.diffuseScale = 1.0f;
                    l.specularScale = 1.0f;
                    l.shadowDimmer = 1.0f;

        	        if (light.lightType == LightType.Spot)
	                {
                        // TODO: Add support of cosine inner and outer for spot light to get nicer spotlight...                        
                        float cosSpotOuterHalfAngle = 1.0f / light.invCosHalfSpotAngle;
                        float cosSpotInnerHalfAngle = cosSpotOuterHalfAngle;

                        float val = Mathf.Max(0.001f, (cosSpotInnerHalfAngle - cosSpotOuterHalfAngle));
		                l.angleScale	= 1.0f / val;
		                l.angleOffset	= -cosSpotOuterHalfAngle * l.angleScale;
	                }
	                else
	                {
		                // 1.0f, 2.0f are neutral value allowing GetAngleAnttenuation in shader code to return 1.0
                        l.angleScale = 1.0f;
                        l.angleOffset = 2.0f;
	                }                  

                    lights.Add(l);
                    punctualLightCount++;
                }
            }
            m_punctualLightList.SetData(lights.ToArray());

            Shader.SetGlobalBuffer("g_punctualLightList", m_punctualLightList);
            Shader.SetGlobalInt("g_punctualLightCount", punctualLightCount);
        }

		void UpdateLightConstants(ActiveLight[] activeLights /*, ref ShadowOutput shadow */)
		{
			/*
			int nNumLightsIncludingTooMany = 0;

			int g_nNumLights = 0;

			Vector4[] g_vLightColor = new Vector4[ MAX_LIGHTS ];
			Vector4[] g_vLightPosition_flInvRadius = new Vector4[ MAX_LIGHTS ];
			Vector4[] g_vLightDirection = new Vector4[ MAX_LIGHTS ];
			Vector4[] g_vLightShadowIndex_vLightParams = new Vector4[ MAX_LIGHTS ];
			Vector4[] g_vLightFalloffParams = new Vector4[ MAX_LIGHTS ];
			Vector4[] g_vSpotLightInnersuterConeCosines = new Vector4[ MAX_LIGHTS ];
			Matrix4x4[] g_matWorldToShadow = new Matrix4x4[ MAX_LIGHTS * MAX_SHADOWMAP_PER_LIGHTS ];
			Vector4[] g_vDirShadowSplitSpheres = new Vector4[ MAX_DIRECTIONAL_SPLIT ];

			for ( int nLight = 0; nLight < activeLights.Length; nLight++ )
			{

				nNumLightsIncludingTooMany++;
				if ( nNumLightsIncludingTooMany > MAX_LIGHTS )
					continue;

				ActiveLight light = activeLights [nLight];
				LightType lightType = light.lightType;
				Vector3 position = light.light.transform.position;
				Vector3 lightDir = light.light.transform.forward.normalized;
				AdditionalLightData additionalLightData = light.light.GetComponent<AdditionalLightData> ();

				// Setup shadow data arrays
				bool hasShadows = shadow.GetShadowSliceCountLightIndex (nLight) != 0;

				if ( lightType == LightType.Directional )
				{
					g_vLightColor[ g_nNumLights ] = light.finalColor;
					g_vLightPosition_flInvRadius[ g_nNumLights ] = new Vector4(
						position.x - ( lightDir.x * DIRECTIONAL_LIGHT_PULLBACK_DISTANCE ),
						position.y - ( lightDir.y * DIRECTIONAL_LIGHT_PULLBACK_DISTANCE ),
						position.z - ( lightDir.z * DIRECTIONAL_LIGHT_PULLBACK_DISTANCE ),
						-1.0f );
					g_vLightDirection[ g_nNumLights ] = new Vector4( lightDir.x, lightDir.y, lightDir.z );
					g_vLightShadowIndex_vLightParams[ g_nNumLights ] = new Vector4( 0, 0, 1, 1 );
					g_vLightFalloffParams[ g_nNumLights ] = new Vector4( 0.0f, 0.0f, float.MaxValue, (float)lightType );
					g_vSpotLightInnerOuterConeCosines[ g_nNumLights ] = new Vector4( 0.0f, -1.0f, 1.0f );

					if (hasShadows)
					{
						for (int s = 0; s < MAX_DIRECTIONAL_SPLIT; ++s)
						{
							g_vDirShadowSplitSpheres[s] = shadow.directionalShadowSplitSphereSqr[s];
						}
					}
				}
				else if ( lightType == LightType.Point )
				{
					g_vLightColor[ g_nNumLights ] = light.finalColor;

					g_vLightPosition_flInvRadius[ g_nNumLights ] = new Vector4( position.x, position.y, position.z, 1.0f / light.range );
					g_vLightDirection[ g_nNumLights ] = new Vector4( 0.0f, 0.0f, 0.0f );
					g_vLightShadowIndex_vLightParams[ g_nNumLights ] = new Vector4( 0, 0, 1, 1 );
					g_vLightFalloffParams[ g_nNumLights ] = new Vector4( 1.0f, 0.0f, light.range * light.range, (float)lightType );
					g_vSpotLightInnerOuterConeCosines[ g_nNumLights ] = new Vector4( 0.0f, -1.0f, 1.0f );
				}
				else if ( lightType == LightType.Spot )
				{
					g_vLightColor[ g_nNumLights ] = light.finalColor;
					g_vLightPosition_flInvRadius[ g_nNumLights ] = new Vector4( position.x, position.y, position.z, 1.0f / light.range );
					g_vLightDirection[ g_nNumLights ] = new Vector4( lightDir.x, lightDir.y, lightDir.z );
					g_vLightShadowIndex_vLightParams[ g_nNumLights ] = new Vector4( 0, 0, 1, 1 );
					g_vLightFalloffParams[ g_nNumLights ] = new Vector4( 1.0f, 0.0f, light.range * light.range, (float)lightType );

					float flInnerConePercent = AdditionalLightData.GetInnerSpotPercent01(additionalLightData);
					float spotAngle = light.light.spotAngle;
					float flPhiDot = Mathf.Clamp( Mathf.Cos( spotAngle * 0.5f * Mathf.Deg2Rad ), 0.0f, 1.0f ); // outer cone
					float flThetaDot = Mathf.Clamp( Mathf.Cos( spotAngle * 0.5f * flInnerConePercent * Mathf.Deg2Rad ), 0.0f, 1.0f ); // inner cone
					g_vSpotLightInnerOuterConeCosines[ g_nNumLights ] = new Vector4( flThetaDot, flPhiDot, 1.0f / Mathf.Max( 0.01f, flThetaDot - flPhiDot ) );

				}

				if ( hasShadows )
				{
					// Enable shadows
					g_vLightShadowIndex_vLightParams[ g_nNumLights ].x = 1; 
					for(int s=0; s < shadow.GetShadowSliceCountLightIndex (nLight); ++s)
					{
						int shadowSliceIndex = shadow.GetShadowSliceIndex (nLight, s);
						g_matWorldToShadow [g_nNumLights * MAX_SHADOWMAP_PER_LIGHTS + s] = shadow.shadowSlices[shadowSliceIndex].shadowTransform.transpose;
					}
				}

				g_nNumLights++;
			}

			// Warn if too many lights found
			if ( nNumLightsIncludingTooMany > MAX_LIGHTS )
			{
				if ( nNumLightsIncludingTooMany > m_nWarnedTooManyLights )
				{
					Debug.LogError( "ERROR! Found " + nNumLightsIncludingTooMany + " runtime lights! Valve renderer supports up to " + MAX_LIGHTS +
						" active runtime lights at a time!\nDisabling " + ( nNumLightsIncludingTooMany - MAX_LIGHTS ) + " runtime light" +
						( ( nNumLightsIncludingTooMany - MAX_LIGHTS ) > 1 ? "s" : "" ) + "!\n" );
				}
				m_nWarnedTooManyLights = nNumLightsIncludingTooMany;
			}
			else
			{
				if ( m_nWarnedTooManyLights > 0 )
				{
					m_nWarnedTooManyLights = 0;
					Debug.Log( "SUCCESS! Found " + nNumLightsIncludingTooMany + " runtime lights which is within the supported number of lights, " + MAX_LIGHTS + ".\n\n" );
				}
			}

			// Send constants to shaders
			Shader.SetGlobalInt( "g_nNumLights", g_nNumLights );

			// New method for Unity 5.4 to set arrays of constants
			Shader.SetGlobalVectorArray( "g_vLightPosition_flInvRadius", g_vLightPosition_flInvRadius );
			Shader.SetGlobalVectorArray( "g_vLightColor", g_vLightColor );
			Shader.SetGlobalVectorArray( "g_vLightDirection", g_vLightDirection );
			Shader.SetGlobalVectorArray( "g_vLightShadowIndex_vLightParams", g_vLightShadowIndex_vLightParams );
			Shader.SetGlobalVectorArray( "g_vLightFalloffParams", g_vLightFalloffParams );
			Shader.SetGlobalVectorArray( "g_vSpotLightInnerOuterConeCosines", g_vSpotLightInnerOuterConeCosines );
			Shader.SetGlobalMatrixArray( "g_matWorldToShadow", g_matWorldToShadow );
			Shader.SetGlobalVectorArray( "g_vDirShadowSplitSpheres", g_vDirShadowSplitSpheres );

			// Time
			#if ( UNITY_EDITOR )
			{
				Shader.SetGlobalFloat( "g_flTime", Time.realtimeSinceStartup );
				//Debug.Log( "Time " + Time.realtimeSinceStartup );
			}
			#else
			{
			Shader.SetGlobalFloat( "g_flTime", Time.timeSinceLevelLoad );
			//Debug.Log( "Time " + Time.timeSinceLevelLoad );
			}
			#endif

			// PCF 3x3 Shadows
			float flTexelEpsilonX = 1.0f / m_ShadowSettings.shadowAtlasWidth;
			float flTexelEpsilonY = 1.0f / m_ShadowSettings.shadowAtlasHeight;
			Vector4 g_vShadow3x3PCFTerms0 = new Vector4( 20.0f / 267.0f, 33.0f / 267.0f, 55.0f / 267.0f, 0.0f );
			Vector4 g_vShadow3x3PCFTerms1 = new Vector4( flTexelEpsilonX, flTexelEpsilonY, -flTexelEpsilonX, -flTexelEpsilonY );
			Vector4 g_vShadow3x3PCFTerms2 = new Vector4( flTexelEpsilonX, flTexelEpsilonY, 0.0f, 0.0f );
			Vector4 g_vShadow3x3PCFTerms3 = new Vector4( -flTexelEpsilonX, -flTexelEpsilonY, 0.0f, 0.0f );

			Shader.SetGlobalVector( "g_vShadow3x3PCFTerms0", g_vShadow3x3PCFTerms0 );
			Shader.SetGlobalVector( "g_vShadow3x3PCFTerms1", g_vShadow3x3PCFTerms1 );
			Shader.SetGlobalVector( "g_vShadow3x3PCFTerms2", g_vShadow3x3PCFTerms2 );
			Shader.SetGlobalVector( "g_vShadow3x3PCFTerms3", g_vShadow3x3PCFTerms3 );
			 */
		}

		public override void Render(Camera[] cameras, RenderLoop renderLoop)
		{
			// Set Frame constant buffer
            // TODO...

			foreach (var camera in cameras)
			{
				// Set camera constant buffer
                // TODO...

				CullResults cullResults;
				CullingParameters cullingParams;
				if (!CullResults.GetCullingParameters (camera, out cullingParams))
					continue;

				//m_ShadowPass.UpdateCullingParameters (ref cullingParams);

				cullResults = CullResults.Cull (ref cullingParams, renderLoop);
					
				//ShadowOutput shadows;
				//m_ShadowPass.Render (renderLoop, cullResults, out shadows);

				renderLoop.SetupCameraProperties (camera);

				//UpdateLightConstants(cullResults.culledLights /*, ref shadows */);

                UpdatePunctualLights(cullResults.culledLights);

				DrawRendererSettings settings = new DrawRendererSettings (cullResults, camera, new ShaderPassName("Forward"));
				settings.rendererConfiguration = RendererConfiguration.ConfigureOneLightProbePerRenderer | RendererConfiguration.ConfigureReflectionProbesProbePerRenderer;
				settings.sorting.sortOptions = SortOptions.SortByMaterialThenMesh;

				renderLoop.DrawRenderers (ref settings);
				renderLoop.Submit ();
			}

			// Post effects
		}

		#if UNITY_EDITOR
		public override UnityEditor.SupportedRenderingFeatures GetSupportedRenderingFeatures()
		{
			var features = new UnityEditor.SupportedRenderingFeatures();

			features.reflectionProbe = UnityEditor.SupportedRenderingFeatures.ReflectionProbe.Rotation;

			return features;
		}
		#endif
	}
}