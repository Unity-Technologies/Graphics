using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.ScriptableRenderLoop
{
	//[ExecuteInEditMode]
	public class FptlLighting : ScriptableRenderLoop
	{
		[MenuItem("Renderloop/CreateRenderLoopFPTL")]
		static void CreateRenderLoopFPTL()
		{
			var instance = ScriptableObject.CreateInstance<FptlLighting>();
			AssetDatabase.CreateAsset(instance, "Assets/renderloopfptl.asset");
			//AssetDatabase.CreateAsset(instance, "Assets/ScriptableRenderLoop/fptl/renderloopfptl.asset");
		}

		[SerializeField]
		ShadowSettings m_ShadowSettings = ShadowSettings.Default;
		ShadowRenderPass m_ShadowPass;

		public Shader m_DeferredShader;
		public Shader m_DeferredReflectionShader;

		public ComputeShader m_BuildScreenAABBShader;
		public ComputeShader m_BuildPerTileLightListShader;

		private Material m_DeferredMaterial;
		private Material m_DeferredReflectionMaterial;
		static private int kGBufferAlbedo;
		static private int kGBufferSpecRough;
		static private int kGBufferNormal;
		static private int kGBufferEmission;
		static private int kGBufferZ;
		static private int kCameraTarget;
		static private int kCameraDepthTexture;

		static private int kGenAABBKernel;
		static private int kGenListPerTileKernel;
		static private ComputeBuffer m_lightDataBuffer;
		static private ComputeBuffer m_convexBoundsBuffer;
		static private ComputeBuffer m_aabbBoundsBuffer;
		static private ComputeBuffer lightList;
		static private ComputeBuffer m_dirLightList;

		Matrix4x4[] g_matWorldToShadow = new Matrix4x4[MAX_LIGHTS * MAX_SHADOWMAP_PER_LIGHTS];
		Vector4[] g_vDirShadowSplitSpheres = new Vector4[MAX_DIRECTIONAL_SPLIT];
		Vector4[] g_vShadow3x3PCFTerms = new Vector4[4];

		public const int gMaxNumLights = 1024;
		public const int gMaxNumDirLights = 2;
		public const float gFltMax = 3.402823466e+38F;

		const int MAX_LIGHTS = 10;
		const int MAX_SHADOWMAP_PER_LIGHTS = 6;
		const int MAX_DIRECTIONAL_SPLIT = 4;
		// Directional lights become spotlights at a far distance. This is the distance we pull back to set the spotlight origin.
		const float DIRECTIONAL_LIGHT_PULLBACK_DISTANCE = 10000.0f;

		[NonSerialized]
		private int m_nWarnedTooManyLights = 0;

		private TextureCache2D m_cookieTexArray;
		private TextureCacheCubemap m_cubeCookieTexArray;
		private TextureCacheCubemap m_cubeReflTexArray;

		private SkyboxHelper m_skyboxHelper;

		private Material m_blitMaterial;

		void OnEnable()
		{
			Rebuild();
		}

		void OnValidate()
		{
			Rebuild();
		}

		void ClearComputeBuffers()
		{
			if (m_aabbBoundsBuffer != null)
				m_aabbBoundsBuffer.Release();

			if (m_convexBoundsBuffer != null)
				m_convexBoundsBuffer.Release();

			if (m_lightDataBuffer != null)
				m_lightDataBuffer.Release();

			if (lightList != null)
				lightList.Release();

			if (m_dirLightList != null)
				m_dirLightList.Release();
		}

		void Rebuild()
		{
			ClearComputeBuffers();

			kGBufferAlbedo = Shader.PropertyToID("_CameraGBufferTexture0");
			kGBufferSpecRough = Shader.PropertyToID("_CameraGBufferTexture1");
			kGBufferNormal = Shader.PropertyToID("_CameraGBufferTexture2");
			kGBufferEmission = Shader.PropertyToID("_CameraGBufferTexture3");
			kGBufferZ = Shader.PropertyToID("_CameraGBufferZ"); // used while rendering into G-buffer+
			kCameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture"); // copy of that for later sampling in shaders
			kCameraTarget = Shader.PropertyToID("_CameraTarget");

			//   RenderLoop.renderLoopDelegate += ExecuteRenderLoop;
			//var deferredShader = GraphicsSettings.GetCustomShader (BuiltinShaderType.DeferredShading);
			var deferredShader = m_DeferredShader;
			var deferredReflectionShader = m_DeferredReflectionShader;

			m_DeferredMaterial = new Material(deferredShader);
			m_DeferredReflectionMaterial = new Material(deferredReflectionShader);
			m_DeferredMaterial.hideFlags = HideFlags.HideAndDontSave;
			m_DeferredReflectionMaterial.hideFlags = HideFlags.HideAndDontSave;

			kGenAABBKernel = m_BuildScreenAABBShader.FindKernel("ScreenBoundsAABB");
			kGenListPerTileKernel = m_BuildPerTileLightListShader.FindKernel("TileLightListGen");
			m_aabbBoundsBuffer = new ComputeBuffer(2 * gMaxNumLights, 3 * sizeof(float));
			m_convexBoundsBuffer = new ComputeBuffer(gMaxNumLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SFiniteLightBound)));
			m_lightDataBuffer = new ComputeBuffer(gMaxNumLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SFiniteLightData)));
			m_dirLightList = new ComputeBuffer(gMaxNumDirLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLight)));

			lightList = new ComputeBuffer(LightDefinitions.NR_LIGHT_MODELS * 1024 * 1024, sizeof(uint));       // enough list memory for a 4k x 4k display

			m_BuildScreenAABBShader.SetBuffer(kGenAABBKernel, "g_data", m_convexBoundsBuffer);
			//m_BuildScreenAABBShader.SetBuffer(kGenAABBKernel, "g_vBoundsBuffer", m_aabbBoundsBuffer);
			m_DeferredMaterial.SetBuffer("g_vLightData", m_lightDataBuffer);
			m_DeferredMaterial.SetBuffer("g_dirLightData", m_dirLightList);
			m_DeferredReflectionMaterial.SetBuffer("g_vLightData", m_lightDataBuffer);

			m_BuildPerTileLightListShader.SetBuffer(kGenListPerTileKernel, "g_vBoundsBuffer", m_aabbBoundsBuffer);
			m_BuildPerTileLightListShader.SetBuffer(kGenListPerTileKernel, "g_vLightData", m_lightDataBuffer);

			m_cookieTexArray = new TextureCache2D();
			m_cubeCookieTexArray = new TextureCacheCubemap();
			m_cubeReflTexArray = new TextureCacheCubemap();
			m_cookieTexArray.AllocTextureArray(8, 128, 128, TextureFormat.Alpha8, true);
			m_cubeCookieTexArray.AllocTextureArray(4, 512, TextureFormat.Alpha8, true);
            m_cubeReflTexArray.AllocTextureArray(64, 128, TextureFormat.BC6H, true);


			m_DeferredMaterial.SetTexture("_spotCookieTextures", m_cookieTexArray.GetTexCache());
			m_DeferredMaterial.SetTexture("_pointCookieTextures", m_cubeCookieTexArray.GetTexCache());
			m_DeferredReflectionMaterial.SetTexture("_reflCubeTextures", m_cubeReflTexArray.GetTexCache());

			g_matWorldToShadow = new Matrix4x4[MAX_LIGHTS * MAX_SHADOWMAP_PER_LIGHTS];
			g_vDirShadowSplitSpheres = new Vector4[MAX_DIRECTIONAL_SPLIT];
			g_vShadow3x3PCFTerms = new Vector4[4];
			m_ShadowPass = new ShadowRenderPass(m_ShadowSettings);

			m_skyboxHelper = new SkyboxHelper();
			m_skyboxHelper.CreateMesh();

			m_blitMaterial = new Material(Shader.Find("Hidden/FinalPass"));
			m_blitMaterial.hideFlags = HideFlags.HideAndDontSave;
		}

		void OnDisable()
		{
			// RenderLoop.renderLoopDelegate -= ExecuteRenderLoop;
			if (m_DeferredMaterial) DestroyImmediate(m_DeferredMaterial);
			if (m_DeferredReflectionMaterial) DestroyImmediate(m_DeferredReflectionMaterial);
			if (m_blitMaterial) DestroyImmediate(m_blitMaterial);

			m_cookieTexArray.Release();
			m_cubeCookieTexArray.Release();
			m_cubeReflTexArray.Release();

			m_aabbBoundsBuffer.Release();
			m_convexBoundsBuffer.Release();
			m_lightDataBuffer.Release();
			lightList.Release();
			m_dirLightList.Release();
		}

		static void SetupGBuffer(CommandBuffer cmd)
		{
			var format10 = RenderTextureFormat.ARGB32;
			if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB2101010))
				format10 = RenderTextureFormat.ARGB2101010;
			//@TODO: GetGraphicsCaps().buggyMRTSRGBWriteFlag
			cmd.GetTemporaryRT(kGBufferAlbedo, -1, -1, 0, FilterMode.Point, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Default);
			cmd.GetTemporaryRT(kGBufferSpecRough, -1, -1, 0, FilterMode.Point, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Default);
			cmd.GetTemporaryRT(kGBufferNormal, -1, -1, 0, FilterMode.Point, format10, RenderTextureReadWrite.Linear);
			cmd.GetTemporaryRT(kGBufferEmission, -1, -1, 0, FilterMode.Point, format10, RenderTextureReadWrite.Linear); //@TODO: HDR
			cmd.GetTemporaryRT(kGBufferZ, -1, -1, 24, FilterMode.Point, RenderTextureFormat.Depth);
			cmd.GetTemporaryRT(kCameraDepthTexture, -1, -1, 24, FilterMode.Point, RenderTextureFormat.Depth);

			cmd.GetTemporaryRT(kCameraTarget, -1, -1, 0, FilterMode.Point, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Default);

			var colorMRTs = new RenderTargetIdentifier[4] { kGBufferAlbedo, kGBufferSpecRough, kGBufferNormal, kGBufferEmission };
			cmd.SetRenderTarget(colorMRTs, new RenderTargetIdentifier(kGBufferZ));
			cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));

			//@TODO: render VR occlusion mesh
		}

		static void RenderGBuffer(CullResults cull, Camera camera, RenderLoop loop)
		{
			// setup GBuffer for rendering
			var cmd = new CommandBuffer();
			cmd.name = "Create G-Buffer";
			SetupGBuffer(cmd);
			loop.ExecuteCommandBuffer(cmd);
			cmd.Dispose();

			// render opaque objects using Deferred pass
			DrawRendererSettings settings = new DrawRendererSettings(cull, camera, new ShaderPassName("Deferred"));
			settings.sorting.sortOptions = SortOptions.SortByMaterialThenMesh;
			settings.inputCullingOptions.SetQueuesOpaque();
			loop.DrawRenderers(ref settings);

		}

		static void CopyDepthAfterGBuffer(RenderLoop loop)
		{
			var cmd = new CommandBuffer();
			cmd.CopyTexture(new RenderTargetIdentifier(kGBufferZ), new RenderTargetIdentifier(kCameraDepthTexture));
			loop.ExecuteCommandBuffer(cmd);
			cmd.Dispose();
		}

		void DoTiledDeferredLighting(Camera camera, RenderLoop loop, Matrix4x4 viewToWorld, Matrix4x4 scrProj, Matrix4x4 incScrProj, ComputeBuffer lightList)
		{
			m_DeferredMaterial.SetBuffer("g_vLightList", lightList);
			m_DeferredReflectionMaterial.SetBuffer("g_vLightList", lightList);

			m_DeferredMaterial.SetBuffer("g_vLightData", m_lightDataBuffer);
			m_DeferredReflectionMaterial.SetBuffer("g_vLightData", m_lightDataBuffer);

			m_DeferredMaterial.SetBuffer("g_dirLightData", m_dirLightList);
			var cmd = new CommandBuffer();
			cmd.name = "DoTiledDeferredLighting";

			//cmd.SetRenderTarget(new RenderTargetIdentifier(kGBufferEmission), new RenderTargetIdentifier(kGBufferZ));

			cmd.SetGlobalMatrix("g_mViewToWorld", viewToWorld);
			cmd.SetGlobalMatrix("g_mWorldToView", viewToWorld.inverse);
			cmd.SetGlobalMatrix("g_mScrProjection", scrProj);
			cmd.SetGlobalMatrix("g_mInvScrProjection", incScrProj);

			// Shadow constants
			cmd.SetGlobalMatrixArray("g_matWorldToShadow", g_matWorldToShadow);
			cmd.SetGlobalVectorArray("g_vDirShadowSplitSpheres", g_vDirShadowSplitSpheres);
			cmd.SetGlobalVector("g_vShadow3x3PCFTerms0", g_vShadow3x3PCFTerms[0]);
			cmd.SetGlobalVector("g_vShadow3x3PCFTerms1", g_vShadow3x3PCFTerms[1]);
			cmd.SetGlobalVector("g_vShadow3x3PCFTerms2", g_vShadow3x3PCFTerms[2]);
			cmd.SetGlobalVector("g_vShadow3x3PCFTerms3", g_vShadow3x3PCFTerms[3]);

			//cmd.Blit (kGBufferNormal, (RenderTexture)null); // debug: display normals

			cmd.Blit(kGBufferEmission, kCameraTarget, m_DeferredMaterial, 0);
			cmd.Blit(kGBufferEmission, kCameraTarget, m_DeferredReflectionMaterial, 0);

			// Set the intermediate target for compositing (skybox, etc)
			cmd.SetRenderTarget(new RenderTargetIdentifier(kCameraTarget), new RenderTargetIdentifier(kCameraDepthTexture));

			loop.ExecuteCommandBuffer(cmd);
			cmd.Dispose();
		}

		void SetMatrixCS(CommandBuffer cmd, ComputeShader shadercs, string name, Matrix4x4 mat)
		{
			float[] data = new float[16];

			for (int c = 0; c < 4; c++)
				for (int r = 0; r < 4; r++)
					data[4 * c + r] = mat[r, c];

			cmd.SetComputeFloatParams(shadercs, name, data);
		}

		void UpdateDirectionalLights(Camera camera, ActiveLight[] activeLights)
		{
			int dirLightCount = 0;
			List<DirectionalLight> lights = new List<DirectionalLight>();
			Matrix4x4 worldToView = camera.worldToCameraMatrix;

			for (int nLight = 0; nLight < activeLights.Length; nLight++)
			{
				ActiveLight light = activeLights[nLight];
				if (light.lightType == LightType.Directional)
				{
					Debug.Assert(dirLightCount < gMaxNumDirLights, "Too many directional lights.");

					DirectionalLight l = new DirectionalLight();

					Matrix4x4 lightToWorld = light.localToWorld;

					Vector3 lightDir = lightToWorld.GetColumn(2);   // Z axis in world space

					// represents a left hand coordinate system in world space
					Vector3 vx = lightToWorld.GetColumn(0);     // X axis in world space
					Vector3 vy = lightToWorld.GetColumn(1);     // Y axis in world space
					Vector3 vz = lightDir;                      // Z axis in world space

					vx = worldToView.MultiplyVector(vx);
					vy = worldToView.MultiplyVector(vy);
					vz = worldToView.MultiplyVector(vz);

					l.uShadowLightIndex = (uint)nLight;

					l.vLaxisX = vx;
					l.vLaxisY = vy;
					l.vLaxisZ = vz;
					
					l.vCol = new Vec3(light.finalColor.r, light.finalColor.g, light.finalColor.b);
					l.fLightIntensity = light.light.intensity;

					lights.Add(l);
					dirLightCount++;
				}
			}
			m_dirLightList.SetData(lights.ToArray());
			m_DeferredMaterial.SetInt("g_nDirLights", dirLightCount);
		}
		
		void UpdateShadowConstants(ActiveLight[] activeLights, ref ShadowOutput shadow)
		{
			int nNumLightsIncludingTooMany = 0;

			int g_nNumLights = 0;

			Vector4[] g_vLightShadowIndex_vLightParams = new Vector4[MAX_LIGHTS];
			Vector4[] g_vLightFalloffParams = new Vector4[MAX_LIGHTS];

			for (int nLight = 0; nLight < activeLights.Length; nLight++)
			{

				nNumLightsIncludingTooMany++;
				if (nNumLightsIncludingTooMany > MAX_LIGHTS)
					continue;

				ActiveLight light = activeLights[nLight];
				LightType lightType = light.lightType;
				Vector3 position = light.light.transform.position;
				Vector3 lightDir = light.light.transform.forward.normalized;

				// Setup shadow data arrays
				bool hasShadows = shadow.GetShadowSliceCountLightIndex(nLight) != 0;

				if (lightType == LightType.Directional)
				{
					g_vLightShadowIndex_vLightParams[g_nNumLights] = new Vector4(0, 0, 1, 1);
					g_vLightFalloffParams[g_nNumLights] = new Vector4(0.0f, 0.0f, float.MaxValue, (float)lightType);

					if (hasShadows)
					{
						for (int s = 0; s < MAX_DIRECTIONAL_SPLIT; ++s)
						{
							g_vDirShadowSplitSpheres[s] = shadow.directionalShadowSplitSphereSqr[s];
						}
					}
				}
				else if (lightType == LightType.Point)
				{
					g_vLightShadowIndex_vLightParams[g_nNumLights] = new Vector4(0, 0, 1, 1);
					g_vLightFalloffParams[g_nNumLights] = new Vector4(1.0f, 0.0f, light.range * light.range, (float)lightType);
				}
				else if (lightType == LightType.Spot)
				{
					g_vLightShadowIndex_vLightParams[g_nNumLights] = new Vector4(0, 0, 1, 1);
					g_vLightFalloffParams[g_nNumLights] = new Vector4(1.0f, 0.0f, light.range * light.range, (float)lightType);
				}

				if (hasShadows)
				{
					// Enable shadows
					g_vLightShadowIndex_vLightParams[g_nNumLights].x = 1;
					for (int s = 0; s < shadow.GetShadowSliceCountLightIndex(nLight); ++s)
					{
						int shadowSliceIndex = shadow.GetShadowSliceIndex(nLight, s);
						g_matWorldToShadow[g_nNumLights * MAX_SHADOWMAP_PER_LIGHTS + s] = shadow.shadowSlices[shadowSliceIndex].shadowTransform.transpose;
					}
				}

				g_nNumLights++;
			}

			// Warn if too many lights found
			if (nNumLightsIncludingTooMany > MAX_LIGHTS)
			{
				if (nNumLightsIncludingTooMany > m_nWarnedTooManyLights)
				{
					Debug.LogError("ERROR! Found " + nNumLightsIncludingTooMany + " runtime lights! Valve renderer supports up to " + MAX_LIGHTS +
						" active runtime lights at a time!\nDisabling " + (nNumLightsIncludingTooMany - MAX_LIGHTS) + " runtime light" +
						((nNumLightsIncludingTooMany - MAX_LIGHTS) > 1 ? "s" : "") + "!\n");
				}
				m_nWarnedTooManyLights = nNumLightsIncludingTooMany;
			}
			else
			{
				if (m_nWarnedTooManyLights > 0)
				{
					m_nWarnedTooManyLights = 0;
					Debug.Log("SUCCESS! Found " + nNumLightsIncludingTooMany + " runtime lights which is within the supported number of lights, " + MAX_LIGHTS + ".\n\n");
				}
			}
			
			// PCF 3x3 Shadows
			float flTexelEpsilonX = 1.0f / m_ShadowSettings.shadowAtlasWidth;
			float flTexelEpsilonY = 1.0f / m_ShadowSettings.shadowAtlasHeight;
			g_vShadow3x3PCFTerms[0] = new Vector4(20.0f / 267.0f, 33.0f / 267.0f, 55.0f / 267.0f, 0.0f);
			g_vShadow3x3PCFTerms[1] = new Vector4(flTexelEpsilonX, flTexelEpsilonY, -flTexelEpsilonX, -flTexelEpsilonY);
			g_vShadow3x3PCFTerms[2] = new Vector4(flTexelEpsilonX, flTexelEpsilonY, 0.0f, 0.0f);
			g_vShadow3x3PCFTerms[3] = new Vector4(-flTexelEpsilonX, -flTexelEpsilonY, 0.0f, 0.0f);
		}

		int GenerateSourceLightBuffers(Camera camera, CullResults inputs)
		{
			ReflectionProbe[] probes = Object.FindObjectsOfType<ReflectionProbe>();

			int numLights = inputs.culledLights.Length;
			int numProbes = probes.Length;
			int numVolumes = numLights + numProbes;


			SFiniteLightData[] lightData = new SFiniteLightData[numVolumes];
			SFiniteLightBound[] boundData = new SFiniteLightBound[numVolumes];
			Matrix4x4 worldToView = camera.worldToCameraMatrix;

			int i = 0;
			uint shadowLightIndex = 0;
			foreach (var cl in inputs.culledLights)
			{
				float range = cl.range;

				Matrix4x4 lightToWorld = cl.localToWorld;
				//Matrix4x4 worldToLight = l.worldToLocal;

				Vector3 lightPos = lightToWorld.GetColumn(3);

				boundData[i].vBoxAxisX = new Vec3(1, 0, 0);
				boundData[i].vBoxAxisY = new Vec3(0, 1, 0);
				boundData[i].vBoxAxisZ = new Vec3(0, 0, 1);
				boundData[i].vScaleXY = new Vec2(1.0f, 1.0f);
				boundData[i].fRadius = range;

				lightData[i].flags = 0;
				lightData[i].fRecipRange = 1.0f / range;
				lightData[i].vCol = new Vec3(cl.finalColor.r, cl.finalColor.g, cl.finalColor.b);
				lightData[i].iSliceIndex = 0;
				lightData[i].uLightModel = (uint)LightDefinitions.DIRECT_LIGHT;
				lightData[i].uShadowLightIndex = shadowLightIndex;
				shadowLightIndex++;

				bool bHasCookie = cl.light.cookie != null;
                bool bHasShadow = cl.light.shadows != LightShadows.None;

				if (cl.lightType == LightType.Spot)
				{
					bool bIsCircularSpot = !bHasCookie;
					if (!bIsCircularSpot)    // square spots always have cookie
					{
						lightData[i].iSliceIndex = m_cookieTexArray.FetchSlice(cl.light.cookie);
					}

					Vector3 lightDir = lightToWorld.GetColumn(2);   // Z axis in world space

					// represents a left hand coordinate system in world space
					Vector3 vx = lightToWorld.GetColumn(0);     // X axis in world space
					Vector3 vy = lightToWorld.GetColumn(1);     // Y axis in world space
					Vector3 vz = lightDir;                      // Z axis in world space

					// transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
					vx = worldToView.MultiplyVector(vx);
					vy = worldToView.MultiplyVector(vy);
					vz = worldToView.MultiplyVector(vz);


					const float pi = 3.1415926535897932384626433832795f;
					const float degToRad = (float)(pi / 180.0);
					const float radToDeg = (float)(180.0 / pi);


					//float sa = cl.GetSpotAngle();		// total field of view from left to right side
					float sa = radToDeg * (2 * Mathf.Acos(1.0f / cl.invCosHalfSpotAngle));       // spot angle doesn't exist in the structure so reversing it for now.


					float cs = Mathf.Cos(0.5f * sa * degToRad);
					float si = Mathf.Sin(0.5f * sa * degToRad);
					float ta = cs > 0.0f ? (si / cs) : gFltMax;

					float cota = si > 0.0f ? (cs / si) : gFltMax;

					//const float cotasa = l.GetCotanHalfSpotAngle();

					// apply nonuniform scale to OBB of spot light
					bool bSqueeze = sa < 0.7f * 90.0f;      // arb heuristic
					float fS = bSqueeze ? ta : si;
					boundData[i].vCen = worldToView.MultiplyPoint(lightPos + ((0.5f * range) * lightDir));    // use mid point of the spot as the center of the bounding volume for building screen-space AABB for tiled lighting.

					lightData[i].vLaxisX = vx;
					lightData[i].vLaxisY = vy;
					lightData[i].vLaxisZ = vz;

					// scale axis to match box or base of pyramid
					boundData[i].vBoxAxisX = (fS * range) * vx;
					boundData[i].vBoxAxisY = (fS * range) * vy;
					boundData[i].vBoxAxisZ = (0.5f * range) * vz;

					// generate bounding sphere radius
					float fAltDx = si;
					float fAltDy = cs;
					fAltDy = fAltDy - 0.5f;
					//if(fAltDy<0) fAltDy=-fAltDy;

					fAltDx *= range; fAltDy *= range;

					float fAltDist = Mathf.Sqrt(fAltDy * fAltDy + (bIsCircularSpot ? 1.0f : 2.0f) * fAltDx * fAltDx);
					boundData[i].fRadius = fAltDist > (0.5f * range) ? fAltDist : (0.5f * range);       // will always pick fAltDist
					boundData[i].vScaleXY = bSqueeze ? new Vec2(0.01f, 0.01f) : new Vec2(1.0f, 1.0f);

					// fill up ldata
					lightData[i].uLightType = (uint)LightDefinitions.SPOT_LIGHT;
					lightData[i].vLpos = worldToView.MultiplyPoint(lightPos);
					lightData[i].fSphRadiusSq = range * range;
					lightData[i].fPenumbra = cs;
					lightData[i].cotan = cota;
					lightData[i].flags |= (bIsCircularSpot ? LightDefinitions.IS_CIRCULAR_SPOT_SHAPE : 0);

					lightData[i].flags |= (bHasCookie ? LightDefinitions.HAS_COOKIE_TEXTURE : 0);
                    lightData[i].flags |= (bHasShadow ? LightDefinitions.HAS_SHADOW : 0);
				}
				else if (cl.lightType == LightType.Point)
				{
					if (bHasCookie)
					{
						lightData[i].iSliceIndex = m_cubeCookieTexArray.FetchSlice(cl.light.cookie);
					}

					boundData[i].vCen = worldToView.MultiplyPoint(lightPos);
					boundData[i].vBoxAxisX = new Vec3(range, 0, 0);
					boundData[i].vBoxAxisY = new Vec3(0, range, 0);
					boundData[i].vBoxAxisZ = new Vec3(0, 0, -range);    // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
					boundData[i].vScaleXY = new Vec2(1.0f, 1.0f);
					boundData[i].fRadius = range;

					// fill up ldata
					lightData[i].uLightType = (uint)LightDefinitions.SPHERE_LIGHT;
					lightData[i].vLpos = boundData[i].vCen;
					lightData[i].fSphRadiusSq = range * range;

					lightData[i].flags |= (bHasCookie ? LightDefinitions.HAS_COOKIE_TEXTURE : 0);
                    lightData[i].flags |= (bHasShadow ? LightDefinitions.HAS_SHADOW : 0);
				}
				else
				{
					//Assert(false);
				}

				// next light
				if (cl.lightType == LightType.Spot || cl.lightType == LightType.Point)
					++i;
			}


			// probe.m_BlendDistance
			// Vector3f extents = 0.5*Abs(probe.m_BoxSize);
			// C center of rendered refl box <-- GetComponent (Transform).GetPosition() + m_BoxOffset;
			// cube map capture point: GetComponent (Transform).GetPosition()
			// shader parameter min and max are C+/-(extents+blendDistance)

			int numProbesOut = 0;
			foreach (var rl in probes)
			{
				Texture cubemap = rl.mode == ReflectionProbeMode.Custom ? rl.customBakedTexture : rl.bakedTexture;
				if (cubemap != null)        // always a box for now
				{
					i = numProbesOut + numLights;

					lightData[i].flags = 0;

					Bounds bnds = rl.bounds;
					Vector3 boxOffset = rl.center;                  // reflection volume offset relative to cube map capture point
					float blendDistance = rl.blendDistance;
					float imp = rl.importance;

                    Matrix4x4 mat = rl.transform.localToWorldMatrix;
                    Vector3 cubeCapturePos = mat.GetColumn(3);      // cube map capture position in world space


					// implicit in CalculateHDRDecodeValues() --> float ints = rl.intensity;
					bool boxProj = rl.boxProjection;
					Vector4 decodeVals = rl.CalculateHDRDecodeValues();

                    // C is reflection volume center in world space (NOT same as cube map capture point)
					Vector3 e = bnds.extents;       // 0.5f * Vector3.Max(-boxSizes[p], boxSizes[p]);
					//Vector3 C = bnds.center;        // P + boxOffset;
                    Vector3 C = mat.MultiplyPoint(boxOffset);       // same as commented out line above when rot is identity
                    
					//Vector3 posForShaderParam = bnds.center - boxOffset;    // gives same as rl.GetComponent<Transform>().position;
                    Vector3 posForShaderParam = cubeCapturePos;        // same as commented out line above when rot is identity
					Vector3 combinedExtent = e + new Vector3(blendDistance, blendDistance, blendDistance);

                    Vector3 vx = mat.GetColumn(0);
                    Vector3 vy = mat.GetColumn(1);
                    Vector3 vz = mat.GetColumn(2);

					// transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
					vx = worldToView.MultiplyVector(vx);
					vy = worldToView.MultiplyVector(vy);
					vz = worldToView.MultiplyVector(vz);

					Vector3 Cw = worldToView.MultiplyPoint(C);

					if (boxProj) lightData[i].flags |= LightDefinitions.IS_BOX_PROJECTED;

					lightData[i].vLpos = Cw;
					lightData[i].vLaxisX = vx;
					lightData[i].vLaxisY = vy;
					lightData[i].vLaxisZ = vz;
					lightData[i].vLocalCubeCapturePoint = -boxOffset;
					lightData[i].fProbeBlendDistance = blendDistance;

					lightData[i].fLightIntensity = decodeVals.x;
					lightData[i].fDecodeExp = decodeVals.y;

					lightData[i].iSliceIndex = m_cubeReflTexArray.FetchSlice(cubemap);

					Vector3 delta = combinedExtent - e;
					lightData[i].vBoxInnerDist = e;
					lightData[i].vBoxInvRange = new Vec3(1.0f / delta.x, 1.0f / delta.y, 1.0f / delta.z);

					boundData[i].vCen = Cw;
					boundData[i].vBoxAxisX = combinedExtent.x * vx;
					boundData[i].vBoxAxisY = combinedExtent.y * vy;
					boundData[i].vBoxAxisZ = combinedExtent.z * vz;
					boundData[i].vScaleXY = new Vec2(1.0f, 1.0f);
					boundData[i].fRadius = combinedExtent.magnitude;

					// fill up ldata
					lightData[i].uLightType = (uint)LightDefinitions.BOX_LIGHT;
					lightData[i].uLightModel = (uint)LightDefinitions.REFLECTION_LIGHT;

					++numProbesOut;
				}
			}


			m_convexBoundsBuffer.SetData(boundData);
			m_lightDataBuffer.SetData(lightData);


			return numLights + numProbesOut;
		}

		/* public override void Render(Camera[] cameras, RenderLoop renderLoop)
		{
			foreach (var camera in cameras)
			{
				CullResults cullResults;
				CullingParameters cullingParams;
				if (!CullResults.GetCullingParameters(camera, out cullingParams))
					continue;

				m_ShadowPass.UpdateCullingParameters(ref cullingParams);

				cullResults = CullResults.Cull(ref cullingParams, renderLoop);

				ShadowOutput shadows;
				m_ShadowPass.Render(renderLoop, cullResults, out shadows);

				renderLoop.SetupCameraProperties(camera);

				UpdateLightConstants(cullResults.culledLights, ref shadows);

				DrawRendererSettings settings = new DrawRendererSettings(cullResults, camera, new ShaderPassName("ForwardBase"));
				settings.rendererConfiguration = RendererConfiguration.ConfigureOneLightProbePerRenderer | RendererConfiguration.ConfigureReflectionProbesProbePerRenderer;
				settings.sorting.sortOptions = SortOptions.SortByMaterialThenMesh;

				renderLoop.DrawRenderers(ref settings);
				renderLoop.Submit();
			}

			// Post effects
		}*/

		public override void Render(Camera[] cameras, RenderLoop renderLoop)
		{
			foreach (var camera in cameras)
			{
				CullResults cullResults;
				CullingParameters cullingParams;
				if (!CullResults.GetCullingParameters(camera, out cullingParams))
					continue;

				m_ShadowPass.UpdateCullingParameters(ref cullingParams);

				cullResults = CullResults.Cull(ref cullingParams, renderLoop);
				ExecuteRenderLoop(camera, cullResults, renderLoop);
			}
		}

		void FinalPass(RenderLoop loop)
		{
			CommandBuffer cmd = new CommandBuffer();
			cmd.name = "FinalPass";
			cmd.Blit(kCameraTarget, BuiltinRenderTextureType.CameraTarget, m_blitMaterial, 0);
			loop.ExecuteCommandBuffer(cmd);
			cmd.Dispose();
		}

		void ExecuteRenderLoop(Camera camera, CullResults cullResults, RenderLoop loop)
		{
			// do anything we need to do upon a new frame.
			NewFrame();

			ShadowOutput shadows;
			m_ShadowPass.Render(loop, cullResults, out shadows);

			//m_DeferredMaterial.SetInt("_SrcBlend", camera.hdr ? (int)BlendMode.One : (int)BlendMode.DstColor);
			//m_DeferredMaterial.SetInt("_DstBlend", camera.hdr ? (int)BlendMode.One : (int)BlendMode.Zero);
			//m_DeferredReflectionMaterial.SetInt("_SrcBlend", camera.hdr ? (int)BlendMode.One : (int)BlendMode.DstColor);
			//m_DeferredReflectionMaterial.SetInt("_DstBlend", camera.hdr ? (int)BlendMode.One : (int)BlendMode.Zero);
			loop.SetupCameraProperties(camera);

			UpdateShadowConstants(cullResults.culledLights, ref shadows);

			RenderGBuffer(cullResults, camera, loop);

			//@TODO: render forward-only objects into depth buffer
			CopyDepthAfterGBuffer(loop);
			//@TODO: render reflection probes

			//RenderLighting(camera, inputs, loop);

			//
			Matrix4x4 proj = camera.projectionMatrix;
			Matrix4x4 temp = new Matrix4x4();
			temp.SetRow(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
			temp.SetRow(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
			temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
			temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Matrix4x4 projh = temp * proj;
			Matrix4x4 invProjh = projh.inverse;

			int iW = camera.pixelWidth;
			int iH = camera.pixelHeight;

			temp.SetRow(0, new Vector4(0.5f * iW, 0.0f, 0.0f, 0.5f * iW));
			temp.SetRow(1, new Vector4(0.0f, 0.5f * iH, 0.0f, 0.5f * iH));
			temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
			temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Matrix4x4 projscr = temp * proj;
			Matrix4x4 invProjscr = projscr.inverse;


			int numLights = GenerateSourceLightBuffers(camera, cullResults);


			int nrTilesX = (iW + 15) / 16;
			int nrTilesY = (iH + 15) / 16;
			//ComputeBuffer lightList = new ComputeBuffer(nrTilesX * nrTilesY * (32 / 2), sizeof(uint));


			var cmd = new CommandBuffer();

			cmd.name = "Build light list";
			cmd.SetComputeIntParam(m_BuildScreenAABBShader, "g_iNrVisibLights", numLights);
			SetMatrixCS(cmd, m_BuildScreenAABBShader, "g_mProjection", projh);
			SetMatrixCS(cmd, m_BuildScreenAABBShader, "g_mInvProjection", invProjh);
			cmd.SetComputeBufferParam(m_BuildScreenAABBShader, kGenAABBKernel, "g_vBoundsBuffer", m_aabbBoundsBuffer);
			cmd.ComputeDispatch(m_BuildScreenAABBShader, kGenAABBKernel, (numLights + 7) / 8, 1, 1);

			cmd.SetComputeIntParam(m_BuildPerTileLightListShader, "g_iNrVisibLights", numLights);
			SetMatrixCS(cmd, m_BuildPerTileLightListShader, "g_mScrProjection", projscr);
			SetMatrixCS(cmd, m_BuildPerTileLightListShader, "g_mInvScrProjection", invProjscr);
			cmd.SetComputeTextureParam(m_BuildPerTileLightListShader, kGenListPerTileKernel, "g_depth_tex", new RenderTargetIdentifier(kCameraDepthTexture));
			cmd.SetComputeBufferParam(m_BuildPerTileLightListShader, kGenListPerTileKernel, "g_vLightList", lightList);
			cmd.ComputeDispatch(m_BuildPerTileLightListShader, kGenListPerTileKernel, nrTilesX, nrTilesY, 1);

			loop.ExecuteCommandBuffer(cmd);
			cmd.Dispose();

			UpdateDirectionalLights(camera, cullResults.culledLights);

			DoTiledDeferredLighting(camera, loop, camera.cameraToWorldMatrix, projscr, invProjscr, lightList);

			m_skyboxHelper.Draw(loop, camera);

			FinalPass(loop);

			loop.Submit();
		}

		void NewFrame()
		{
			// update texture caches
			m_cookieTexArray.NewFrame();
			m_cubeCookieTexArray.NewFrame();
			m_cubeReflTexArray.NewFrame();
		}
	}
}
