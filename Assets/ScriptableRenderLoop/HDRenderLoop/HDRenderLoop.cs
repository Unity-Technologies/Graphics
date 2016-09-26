using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System;

using UnityEditor;

namespace UnityEngine.ScriptableRenderLoop
{
    // This HDRenderLoop assume linear lighting. Don't work with gamma.
	public class HDRenderLoop : ScriptableRenderLoop
	{
        [MenuItem("Renderloop/CreateHDRenderLoop")]
		static void CreateHDRenderLoop()
		{
			var instance = ScriptableObject.CreateInstance<HDRenderLoop>();
			AssetDatabase.CreateAsset(instance, "Assets/HDRenderLoop.asset");
		}

        public class GBufferManager
        {
            public const int MaxGbuffer = 8;

            public void SetBufferDescription(int index, string stringID, RenderTextureFormat inFormat, RenderTextureReadWrite inSRGBWrite)
            {
                ID[index] = Shader.PropertyToID(stringID);
                RTID[index] = new RenderTargetIdentifier(ID[index]);
                format[index] = inFormat;
                sRGBWrite[index] = inSRGBWrite;
            }

            public void InitGBuffers(CommandBuffer cmd)
            {
                for (int index = 0; index < gbufferCount; index++)
                {
                    cmd.GetTemporaryRT(ID[index], -1, -1, 0, FilterMode.Point, format[index], sRGBWrite[index]);
                }
            }

            public RenderTargetIdentifier[] GetGBuffers(CommandBuffer cmd)
            {
                var colorMRTs = new RenderTargetIdentifier[gbufferCount];
                for (int index = 0; index < gbufferCount; index++)
                {
                    colorMRTs[index] = RTID[index];
                }

                return colorMRTs;
            }

        
            public int gbufferCount { get; set; }
            int[] ID = new int[MaxGbuffer];
            RenderTargetIdentifier[] RTID = new RenderTargetIdentifier[MaxGbuffer];
            RenderTextureFormat[] format = new RenderTextureFormat[MaxGbuffer];
            RenderTextureReadWrite[] sRGBWrite = new RenderTextureReadWrite[MaxGbuffer];
        }

        public const int MaxLights = 32;

		//[SerializeField]
		//ShadowSettings m_ShadowSettings = ShadowSettings.Default;
		//ShadowRenderPass m_ShadowPass;

        Material m_DeferredMaterial;
        Material m_FinalPassMaterial;

        GBufferManager gbufferManager = new GBufferManager();

        static private int s_CameraColorBuffer;
        static private int s_CameraDepthBuffer;

        static private ComputeBuffer s_punctualLightList;

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
            if (s_punctualLightList != null)
                s_punctualLightList.Release();
        }

		void Rebuild()
		{
            ClearComputeBuffers();

            gbufferManager.gbufferCount = 4;
            gbufferManager.SetBufferDescription(0, "_CameraGBufferTexture0", RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);     // Store diffuse color => sRGB
            gbufferManager.SetBufferDescription(1, "_CameraGBufferTexture1", RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            gbufferManager.SetBufferDescription(2, "_CameraGBufferTexture2", RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear); // Store normal => higher precision
            gbufferManager.SetBufferDescription(3, "_CameraGBufferTexture3", RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);

            s_CameraColorBuffer = Shader.PropertyToID("_CameraColorBuffer");
            s_CameraDepthBuffer = Shader.PropertyToID("_CameraDepthBuffer");

            s_punctualLightList = new ComputeBuffer(MaxLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PunctualLightData)));

            Shader deferredMaterial = Shader.Find("Hidden/Unity/LightingDeferred") as Shader;
            m_DeferredMaterial = new Material(deferredMaterial);
            m_DeferredMaterial.hideFlags = HideFlags.HideAndDontSave;

            Shader finalPassShader = Shader.Find("Hidden/Unity/FinalPass") as Shader;
            m_FinalPassMaterial = new Material(finalPassShader);
            m_FinalPassMaterial.hideFlags = HideFlags.HideAndDontSave;

            // m_ShadowPass = new ShadowRenderPass (m_ShadowSettings);
		}

        void OnDisable()
        {
            s_punctualLightList.Release();

            if (m_DeferredMaterial) DestroyImmediate(m_DeferredMaterial);
            if (m_FinalPassMaterial) DestroyImmediate(m_FinalPassMaterial);
        }

        void InitAndClearBuffer(Camera camera, RenderLoop renderLoop)
        {
            // We clear only the depth buffer, no need to clear the various color buffer as we overwrite them.          
            // Clear depth/stencil and init buffers
            {
                var cmd = new CommandBuffer();
                cmd.name = "InitGBuffers and clear Depth/Stencil";

                // Init buffer
                // With scriptable render loop we must allocate ourself depth and color buffer (We must be independent of backbuffer for now, hope to fix that later).
                // Also we manage ourself the HDR format, here allocating fp16 directly.
                // With scriptable render loop we can allocate temporary RT in a command buffer, they will not be release with ExecuteCommandBuffer
                // These temporary surface are release automatically at the end of the scriptable renderloop if not release explicitly
                cmd.GetTemporaryRT(s_CameraColorBuffer, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
                cmd.GetTemporaryRT(s_CameraDepthBuffer, -1, -1, 24, FilterMode.Point, RenderTextureFormat.Depth);
                gbufferManager.InitGBuffers(cmd);

                cmd.SetRenderTarget(new RenderTargetIdentifier(s_CameraColorBuffer), new RenderTargetIdentifier(s_CameraDepthBuffer));
                cmd.ClearRenderTarget(true, false, new Color(0, 0, 0, 0));
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }


            // TEMP: As we are in development and have not all the setup pass we still clear the color in emissive buffer and gbuffer, but this will be removed later.

            // Clear HDR target
            {
                var cmd = new CommandBuffer();
                cmd.name = "Clear HDR target";
                cmd.SetRenderTarget(new RenderTargetIdentifier(s_CameraColorBuffer), new RenderTargetIdentifier(s_CameraDepthBuffer));
                cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 0));
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }


            // Clear GBuffers
            {
                var cmd = new CommandBuffer();
                cmd.name = "Clear GBuffer";
                // Write into the Camera Depth buffer
                cmd.SetRenderTarget(gbufferManager.GetGBuffers(cmd), new RenderTargetIdentifier(s_CameraDepthBuffer));
                // Clear everything
                // TODO: Clear is not required for color as we rewrite everything, will save performance.
                cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 0));
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }

            // END TEMP
        }

        void RenderGBuffer(CullResults cull, Camera camera, RenderLoop renderLoop)
		{
			// setup GBuffer for rendering
			var cmd = new CommandBuffer();
			cmd.name = "GBuffer Pass";
            cmd.SetRenderTarget(gbufferManager.GetGBuffers(cmd), new RenderTargetIdentifier(s_CameraDepthBuffer));
            renderLoop.ExecuteCommandBuffer(cmd);
			cmd.Dispose();

			// render opaque objects into GBuffer
			DrawRendererSettings settings = new DrawRendererSettings(cull, camera, new ShaderPassName("GBuffer"));
			settings.sorting.sortOptions = SortOptions.SortByMaterialThenMesh;
			settings.inputCullingOptions.SetQueuesOpaque();
            renderLoop.DrawRenderers(ref settings);
		}

        void RenderDeferredLighting(CullResults cull, Camera camera, RenderLoop renderLoop)
        {
            // setup GBuffer for rendering
            var cmd = new CommandBuffer();
            cmd.name = "Deferred Ligthing Pass";
            cmd.SetRenderTarget(new RenderTargetIdentifier(s_CameraColorBuffer), new RenderTargetIdentifier(s_CameraDepthBuffer));
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        void RenderForward(CullResults cullResults, Camera camera, RenderLoop renderLoop)
        {
            // setup GBuffer for rendering
            var cmd = new CommandBuffer();
            cmd.name = "Forward Pass";
            cmd.SetRenderTarget(new RenderTargetIdentifier(s_CameraColorBuffer), new RenderTargetIdentifier(s_CameraDepthBuffer));
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            DrawRendererSettings settings = new DrawRendererSettings(cullResults, camera, new ShaderPassName("Forward"));
            settings.rendererConfiguration = RendererConfiguration.ConfigureOneLightProbePerRenderer | RendererConfiguration.ConfigureReflectionProbesProbePerRenderer;
            settings.sorting.sortOptions = SortOptions.SortByMaterialThenMesh;

            renderLoop.DrawRenderers(ref settings);
        }

        void FinalPass(RenderLoop renderLoop)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "FinalPass";
            // Resolve our HDR texture to CameraTarget.
            cmd.Blit(s_CameraColorBuffer, BuiltinRenderTextureType.CameraTarget, m_FinalPassMaterial, 0);
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

		//---------------------------------------------------------------------------------------------------------------------------------------------------

        void UpdatePunctualLights(ActiveLight[] activeLights)
        {
            int punctualLightCount = 0;
            List<PunctualLightData> lights = new List<PunctualLightData>();

            for (int lightIndex = 0; lightIndex < Math.Min(activeLights.Length, MaxLights); lightIndex++)
            {
                ActiveLight light = activeLights[lightIndex];
                if (light.lightType == LightType.Spot || light.lightType == LightType.Point)
                {
                    PunctualLightData l = new PunctualLightData();

                    l.positionWS = light.light.transform.position;
                    l.invSqrAttenuationRadius  = 1.0f / (light.range * light.range);

                    // Correct intensity calculation (Different from Unity)
                    float lightColorR = light.light.intensity * Mathf.GammaToLinearSpace(light.light.color.r);
                    float lightColorG = light.light.intensity * Mathf.GammaToLinearSpace(light.light.color.g);
                    float lightColorB = light.light.intensity * Mathf.GammaToLinearSpace(light.light.color.b);

                    l.color = new Vec3(lightColorR, lightColorG, lightColorB);

                    // Light direction is opposite to the forward direction...
                    l.forward = -light.light.transform.forward;
                    // CAUTION: For IES as we inverse forward maybe this will need rotation.
                    l.up = light.light.transform.up;
                    l.right = light.light.transform.right;
                    
                    l.diffuseScale = 1.0f;
                    l.specularScale = 1.0f;
                    l.shadowDimmer = 1.0f;

        	        if (light.lightType == LightType.Spot)
	                {
                        float spotAngle = light.light.spotAngle;
                        AdditionalLightData additionalLightData = light.light.GetComponent<AdditionalLightData>();
                        float innerConePercent = AdditionalLightData.GetInnerSpotPercent01(additionalLightData);
                        float cosSpotOuterHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * Mathf.Deg2Rad), 0.0f, 1.0f);
                        float cosSpotInnerHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * innerConePercent * Mathf.Deg2Rad), 0.0f, 1.0f); // inner cone

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
            s_punctualLightList.SetData(lights.ToArray());

            Shader.SetGlobalBuffer("g_punctualLightList", s_punctualLightList);
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

        /*
        void RenderDeferredLighting(Camera camera, CullingInputs inputs, RenderLoop loop)
        {
            var props = new MaterialPropertyBlock();

            var cmd = new CommandBuffer();
            cmd.SetRenderTarget(new RenderTargetIdentifier(kGBufferEmission), new RenderTargetIdentifier(kGBufferZ));
            foreach (var cl in inputs.culledLights)
            {
                bool renderAsQuad = (cl.flags & VisibleLightFlags.IntersectsNearPlane) != 0 || (cl.flags & VisibleLightFlags.IntersectsFarPlane) != 0 || (cl.lightType == LightType.Directional);

                Vector3 lightPos = cl.localToWorld.GetColumn(3);
                float range = cl.range;
                cmd.DisableShaderKeyword("POINT");
                cmd.DisableShaderKeyword("POINT_COOKIE");
                cmd.DisableShaderKeyword("SPOT");
                cmd.DisableShaderKeyword("DIRECTIONAL");
                cmd.DisableShaderKeyword("DIRECTIONAL_COOKIE");
                //cmd.EnableShaderKeyword ("UNITY_HDR_ON");
                switch (cl.lightType)
                {
                    case LightType.Point:
                        cmd.EnableShaderKeyword("POINT");
                        break;
                    case LightType.Spot:
                        cmd.EnableShaderKeyword("SPOT");
                        break;
                    case LightType.Directional:
                        cmd.EnableShaderKeyword("DIRECTIONAL");
                        break;
                }
                props.SetFloat("_LightAsQuad", renderAsQuad ? 1 : 0);
                props.SetVector("_LightPos", new Vector4(lightPos.x, lightPos.y, lightPos.z, 1.0f / (range * range)));
                props.SetVector("_LightColor", cl.finalColor);
                Debug.Log("Light color : " + cl.finalColor.ToString());
                props.SetMatrix("_WorldToLight", cl.worldToLocal);

                ///@TODO: cleanup, remove this from Internal-PrePassLighting shader
                //DeferredPrivate::s_LightMaterial->SetTexture (ShaderLab::Property ("_LightTextureB0"), builtintex::GetAttenuationTexture ());

                if (renderAsQuad)
                {
                    cmd.DrawMesh(m_QuadMesh, Matrix4x4.identity, m_DeferredMaterial, 0, 0, props);
                }
                else
                {
                    var matrix = Matrix4x4.TRS(lightPos, Quaternion.identity, new Vector3(range, range, range));
                    cmd.DrawMesh(m_PointLightMesh, matrix, m_DeferredMaterial, 0, 0, props);
                }
            }
            loop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }
         */

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

                InitAndClearBuffer(camera, renderLoop);

                RenderGBuffer(cullResults, camera, renderLoop);

                RenderForward(cullResults, camera, renderLoop);

                FinalPass(renderLoop);

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