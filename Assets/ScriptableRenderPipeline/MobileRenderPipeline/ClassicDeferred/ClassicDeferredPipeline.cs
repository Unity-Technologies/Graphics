using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class ClassicDeferredPipelineInstance : RenderPipeline {

	private readonly ClassicDeferredPipeline m_Owner;

	public ClassicDeferredPipelineInstance(ClassicDeferredPipeline owner)
	{
		m_Owner = owner;

		if (m_Owner != null)
			m_Owner.Build();
	}

	public override void Dispose()
	{
		base.Dispose();
		if (m_Owner != null)
			m_Owner.Cleanup();
	}


	public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
	{
		base.Render(renderContext, cameras);
		m_Owner.Render(renderContext, cameras);
	}
}

[ExecuteInEditMode]
public class ClassicDeferredPipeline : RenderPipelineAsset {

#if UNITY_EDITOR
	[UnityEditor.MenuItem("RenderPipeline/Create ClassicDeferredPipeline")]
	static void CreateDeferredRenderPipeline()
	{
		var instance = ScriptableObject.CreateInstance<ClassicDeferredPipeline> ();
		UnityEditor.AssetDatabase.CreateAsset (instance, "Assets/ClassicDeferredPipeline.asset");
	}
#endif

	protected override IRenderPipeline InternalCreatePipeline()
	{
		return new ClassicDeferredPipelineInstance(this);
	}
		
	[SerializeField]
	ShadowSettings m_ShadowSettings = ShadowSettings.Default;
	ShadowRenderPass m_ShadowPass;

	const int k_MaxLights = 10;
	const int k_MaxShadowmapPerLights = 6;
	const int k_MaxDirectionalSplit = 4;

	Matrix4x4[] m_MatWorldToShadow = new Matrix4x4[k_MaxLights * k_MaxShadowmapPerLights];
	Vector4[] m_DirShadowSplitSpheres = new Vector4[k_MaxDirectionalSplit];
	Vector4[] m_Shadow3X3PCFTerms = new Vector4[4];

	[NonSerialized]
	private int m_WarnedTooManyLights = 0;

	private int m_shadowBufferID;

	public Mesh m_PointLightMesh;
	public Mesh m_SpotLightMesh;
	public Mesh m_QuadMesh;

	public Shader finalPassShader;
	public Shader deferredShader;
	public Shader deferredReflectionShader;

	private static int s_GBufferAlbedo;
	private static int s_GBufferSpecRough;
	private static int s_GBufferNormal;
	private static int s_GBufferEmission;
	private static int s_GBufferRedF32;

	private static int s_GBufferZ;
	private static int s_CameraTarget;
	private static int s_CameraDepthTexture;

	private static int m_quadLightingPassNdx;
	private static int m_FiniteLightingPassNdx;

	private Material m_DeferredMaterial;
	private Material m_DeferredReflectionMaterial;
	private Material m_BlitMaterial;

	public Texture m_DefaultSpotCookie;

	private void OnValidate()
	{
		Build();
	}

	public void Cleanup()
	{
		if (m_BlitMaterial) DestroyImmediate(m_BlitMaterial);
		if (m_DeferredMaterial) DestroyImmediate(m_DeferredMaterial);
		if (m_DeferredReflectionMaterial) DestroyImmediate (m_DeferredReflectionMaterial);

	}

	public void Build()
	{
		s_GBufferAlbedo = Shader.PropertyToID ("_CameraGBufferTexture0");
		s_GBufferSpecRough = Shader.PropertyToID ("_CameraGBufferTexture1");
		s_GBufferNormal = Shader.PropertyToID ("_CameraGBufferTexture2");
		s_GBufferEmission = Shader.PropertyToID ("_CameraGBufferTexture3");
		s_GBufferRedF32 = Shader.PropertyToID ("_CameraVPDepth"); 

		s_GBufferZ = Shader.PropertyToID ("_CameraGBufferZ"); // used while rendering into G-buffer+
		s_CameraDepthTexture = Shader.PropertyToID ("_CameraDepthTexture"); // copy of that for later sampling in shaders
		s_CameraTarget = Shader.PropertyToID ("_CameraTarget");

		m_BlitMaterial = new Material (finalPassShader) { hideFlags = HideFlags.HideAndDontSave };
		m_DeferredMaterial = new Material (deferredShader) { hideFlags = HideFlags.HideAndDontSave };
		m_DeferredReflectionMaterial = new Material (deferredReflectionShader) { hideFlags = HideFlags.HideAndDontSave };

		m_quadLightingPassNdx = m_DeferredMaterial.FindPass ("DIRECTIONALLIGHT");
		m_FiniteLightingPassNdx = m_DeferredMaterial.FindPass ("FINITELIGHT");

		//shadows
		m_MatWorldToShadow = new Matrix4x4[k_MaxLights * k_MaxShadowmapPerLights];
		m_DirShadowSplitSpheres = new Vector4[k_MaxDirectionalSplit];
		m_Shadow3X3PCFTerms = new Vector4[4];
		m_ShadowPass = new ShadowRenderPass(m_ShadowSettings);

		m_shadowBufferID = Shader.PropertyToID("g_tShadowBuffer");
	}

	public void Render(ScriptableRenderContext context, IEnumerable<Camera> cameras)
	{
		foreach (var camera in cameras) {
			// Culling
			CullingParameters cullingParams;
			if (!CullResults.GetCullingParameters (camera, out cullingParams))
				continue;

			m_ShadowPass.UpdateCullingParameters(ref cullingParams);

			var cullResults = CullResults.Cull (ref cullingParams, context);
			ExecuteRenderLoop(camera, cullResults, context);
		}

		context.Submit ();
	}

	void ExecuteRenderLoop(Camera camera, CullResults cullResults, ScriptableRenderContext loop)
	{
		RenderShadowMaps(cullResults, loop);

		loop.SetupCameraProperties(camera);
		RenderGBuffer(cullResults, camera, loop);
	
		//DepthOnlyForForwardOpaques(cullResults, camera, loop);

		// IF PLATFORM_MAC -- cannot use framebuffer fetch
		#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
		CopyDepthAfterGBuffer(loop);
		#endif

		PushGlobalShadowParams (loop);

		RenderLighting (camera, cullResults, loop);

		loop.DrawSkybox (camera);

		loop.SetupCameraProperties (camera);

		// present frame buffer.
		FinalPass(loop);
	}

	void RenderShadowMaps(CullResults cullResults, ScriptableRenderContext loop)
	{
		ShadowOutput shadows;
		m_ShadowPass.Render(loop, cullResults, out shadows);
		UpdateShadowConstants (cullResults.visibleLights, ref shadows);
	}

	void PushGlobalShadowParams(ScriptableRenderContext loop)
	{
		var cmd = new CommandBuffer { name = "Push Global Parameters" };

		// Shadow constants
		cmd.SetGlobalMatrixArray("g_matWorldToShadow", m_MatWorldToShadow);
		cmd.SetGlobalVectorArray("g_vDirShadowSplitSpheres", m_DirShadowSplitSpheres);
		cmd.SetGlobalVector("g_vShadow3x3PCFTerms0", m_Shadow3X3PCFTerms[0]);
		cmd.SetGlobalVector("g_vShadow3x3PCFTerms1", m_Shadow3X3PCFTerms[1]);
		cmd.SetGlobalVector("g_vShadow3x3PCFTerms2", m_Shadow3X3PCFTerms[2]);
		cmd.SetGlobalVector("g_vShadow3x3PCFTerms3", m_Shadow3X3PCFTerms[3]);

		loop.ExecuteCommandBuffer(cmd);
		cmd.Dispose();
	}

	void UpdateShadowConstants(IList<VisibleLight> visibleLights, ref ShadowOutput shadow)
	{
		var nNumLightsIncludingTooMany = 0;

		var numLights = 0;

		var lightShadowIndex_LightParams = new Vector4[k_MaxLights];
		var lightFalloffParams = new Vector4[k_MaxLights];

		for (int nLight = 0; nLight < visibleLights.Count; nLight++)
		{
			nNumLightsIncludingTooMany++;
			if (nNumLightsIncludingTooMany > k_MaxLights)
				continue;

			var light = visibleLights[nLight];
			var lightType = light.lightType;
			var position = light.light.transform.position;
			var lightDir = light.light.transform.forward.normalized;

			// Setup shadow data arrays
			var hasShadows = shadow.GetShadowSliceCountLightIndex(nLight) != 0;

			if (lightType == LightType.Directional)
			{
				lightShadowIndex_LightParams[numLights] = new Vector4(0, 0, 1, 1);
				lightFalloffParams[numLights] = new Vector4(0.0f, 0.0f, float.MaxValue, (float)lightType);

				if (hasShadows)
				{
					for (int s = 0; s < k_MaxDirectionalSplit; ++s)
					{
						m_DirShadowSplitSpheres[s] = shadow.directionalShadowSplitSphereSqr[s];
					}
				}
			}
			else if (lightType == LightType.Point)
			{
				lightShadowIndex_LightParams[numLights] = new Vector4(0, 0, 1, 1);
				lightFalloffParams[numLights] = new Vector4(1.0f, 0.0f, light.range * light.range, (float)lightType);
			}
			else if (lightType == LightType.Spot)
			{
				lightShadowIndex_LightParams[numLights] = new Vector4(0, 0, 1, 1);
				lightFalloffParams[numLights] = new Vector4(1.0f, 0.0f, light.range * light.range, (float)lightType);
			}

			if (hasShadows)
			{
				// Enable shadows
				lightShadowIndex_LightParams[numLights].x = 1;
				for (int s = 0; s < shadow.GetShadowSliceCountLightIndex(nLight); ++s)
				{
					var shadowSliceIndex = shadow.GetShadowSliceIndex(nLight, s);
					m_MatWorldToShadow[numLights * k_MaxShadowmapPerLights + s] = shadow.shadowSlices[shadowSliceIndex].shadowTransform.transpose;
				}
			}

			numLights++;
		}

		// Warn if too many lights found
		if (nNumLightsIncludingTooMany > k_MaxLights)
		{
			if (nNumLightsIncludingTooMany > m_WarnedTooManyLights)
			{
				Debug.LogError("ERROR! Found " + nNumLightsIncludingTooMany + " runtime lights! Renderer supports up to " + k_MaxLights +
					" active runtime lights at a time!\nDisabling " + (nNumLightsIncludingTooMany - k_MaxLights) + " runtime light" +
					((nNumLightsIncludingTooMany - k_MaxLights) > 1 ? "s" : "") + "!\n");
			}
			m_WarnedTooManyLights = nNumLightsIncludingTooMany;
		}
		else
		{
			if (m_WarnedTooManyLights > 0)
			{
				m_WarnedTooManyLights = 0;
				Debug.Log("SUCCESS! Found " + nNumLightsIncludingTooMany + " runtime lights which is within the supported number of lights, " + k_MaxLights + ".\n\n");
			}
		}

		// PCF 3x3 Shadows
		var flTexelEpsilonX = 1.0f / m_ShadowSettings.shadowAtlasWidth;
		var flTexelEpsilonY = 1.0f / m_ShadowSettings.shadowAtlasHeight;
		m_Shadow3X3PCFTerms[0] = new Vector4(20.0f / 267.0f, 33.0f / 267.0f, 55.0f / 267.0f, 0.0f);
		m_Shadow3X3PCFTerms[1] = new Vector4(flTexelEpsilonX, flTexelEpsilonY, -flTexelEpsilonX, -flTexelEpsilonY);
		m_Shadow3X3PCFTerms[2] = new Vector4(flTexelEpsilonX, flTexelEpsilonY, 0.0f, 0.0f);
		m_Shadow3X3PCFTerms[3] = new Vector4(-flTexelEpsilonX, -flTexelEpsilonY, 0.0f, 0.0f);
	}

	static void SetupGBuffer(int width, int height, CommandBuffer cmd)
	{
		var format10 = RenderTextureFormat.ARGB32;
		if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB2101010))
			format10 = RenderTextureFormat.ARGB2101010;
		var formatHDR = RenderTextureFormat.DefaultHDR;

		//@TODO: cleanup, right now only because we want to use unmodified Standard shader that encodes emission differently based on HDR or not,
		// so we make it think we always render in HDR
		cmd.EnableShaderKeyword ("UNITY_HDR_ON");

		//@TODO: GetGraphicsCaps().buggyMRTSRGBWriteFlag
		cmd.GetTemporaryRT(s_GBufferAlbedo, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
		cmd.GetTemporaryRT(s_GBufferSpecRough, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
		cmd.GetTemporaryRT(s_GBufferNormal, width, height, 0, FilterMode.Point, format10, RenderTextureReadWrite.Linear);
		cmd.GetTemporaryRT(s_GBufferEmission, width, height, 0, FilterMode.Point, formatHDR, RenderTextureReadWrite.Linear);
		cmd.GetTemporaryRT(s_CameraDepthTexture, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth);
		cmd.GetTemporaryRT(s_CameraTarget, width, height, 0, FilterMode.Point, formatHDR, RenderTextureReadWrite.Default, 1, true); // rtv/uav

		#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
		cmd.GetTemporaryRT(s_GBufferZ, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth);

		var colorMRTs = new RenderTargetIdentifier[4] { s_GBufferAlbedo, s_GBufferSpecRough, s_GBufferNormal, s_GBufferEmission };
		cmd.SetRenderTarget(colorMRTs, new RenderTargetIdentifier(s_GBufferZ));

		#else
		cmd.GetTemporaryRT(s_GBufferZ, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth);
		cmd.GetTemporaryRT(s_GBufferRedF32, width, height, 24, FilterMode.Point, RenderTextureFormat.RFloat);

		var colorMRTs = new RenderTargetIdentifier[5] { s_GBufferAlbedo, s_GBufferSpecRough, s_GBufferNormal, s_GBufferEmission, s_GBufferRedF32 };
		cmd.SetRenderTarget(colorMRTs, new RenderTargetIdentifier(s_GBufferZ));

		#endif

		cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));

	}

	static void RenderGBuffer(CullResults cull, Camera camera, ScriptableRenderContext loop)
	{
		// setup GBuffer for rendering
		var cmd = new CommandBuffer { name = "Create G-Buffer" };
		SetupGBuffer(camera.pixelWidth, camera.pixelHeight, cmd);
		loop.ExecuteCommandBuffer(cmd);
		cmd.Dispose();

		// render opaque objects using Deferred pass
		var settings = new DrawRendererSettings(cull, camera, new ShaderPassName("Deferred"))
		{
			sorting = {flags = SortFlags.CommonOpaque},
			rendererConfiguration = RendererConfiguration.PerObjectLightmaps
		};

		//@TODO: need to get light probes + LPPV too?
		settings.inputFilter.SetQueuesOpaque();
		settings.rendererConfiguration = RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe;
		loop.DrawRenderers(ref settings);
	}

	void RenderLighting (Camera camera, CullResults inputs, ScriptableRenderContext loop)
	{
		var cmd = new CommandBuffer { name = "Lighting" };

		// IF PLATFORM_MAC -- cannot use framebuffer fetch
		#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
			cmd.SetRenderTarget (new RenderTargetIdentifier(s_GBufferEmission), new RenderTargetIdentifier (s_GBufferZ));
		#endif

		foreach (var light in inputs.visibleLights)
		{
			RenderLightGeometry (camera, light, cmd, loop);
		}

		loop.ExecuteCommandBuffer (cmd);
		cmd.Dispose ();
	}

	void RenderLightGeometry (Camera camera, VisibleLight light, CommandBuffer cmd, ScriptableRenderContext loop)
	{
		bool renderAsQuad = (light.flags & VisibleLightFlags.IntersectsNearPlane)!=0 || (light.flags & VisibleLightFlags.IntersectsFarPlane)!=0 || (light.lightType == LightType.Directional);
		Vector3 lightPos = light.localToWorld.GetColumn (3); //position
		Vector3 lightDir = light.localToWorld.GetColumn (2); //z axis
		float range = light.range;
		var lightToWorld = light.localToWorld;
		var worldToLight = lightToWorld.inverse;

		var props = new MaterialPropertyBlock ();
		props.SetFloat ("_LightAsQuad", renderAsQuad ? 1 : 0);
		props.SetVector ("_LightPos", new Vector4(lightPos.x, lightPos.y, lightPos.z, 1.0f / (range * range)));
		props.SetVector ("_LightDir", new Vector4(lightDir.x, lightDir.y, lightDir.z, 0.0f));
		props.SetVector ("_LightColor", light.finalColor);
		props.SetMatrix ("_WorldToLight", lightToWorld.inverse);

		// TODO:OPTIMIZATION DeferredRenderLoop.cpp:660 -- split up into shader varients

		cmd.DisableShaderKeyword ("POINT");
		cmd.DisableShaderKeyword ("POINT_COOKIE");
		cmd.DisableShaderKeyword ("SPOT");
		cmd.DisableShaderKeyword ("DIRECTIONAL");
		cmd.DisableShaderKeyword ("DIRECTIONAL_COOKIE");
		switch (light.lightType)
		{
		case LightType.Point:
			cmd.EnableShaderKeyword ("POINT");
			break;
		case LightType.Spot:
			cmd.EnableShaderKeyword ("SPOT");
			break;
		case LightType.Directional:
			cmd.EnableShaderKeyword ("DIRECTIONAL");
			break;
		}
			
		Texture cookie = light.light.cookie;
		if (cookie != null)
			cmd.SetGlobalTexture ("_LightTexture0", cookie);

		if ((light.lightType == LightType.Point)) {
			
			var matrix = Matrix4x4.TRS (lightPos, Quaternion.identity, new Vector3 (range*2, range*2, range*2));

			if (cookie!=null)
				cmd.EnableShaderKeyword ("POINT_COOKIE");

			if (renderAsQuad) {
				cmd.DrawMesh (m_QuadMesh, Matrix4x4.identity, m_DeferredMaterial, 0, m_quadLightingPassNdx, props);
			} else {
				cmd.DrawMesh (m_PointLightMesh, matrix, m_DeferredMaterial, 0, m_FiniteLightingPassNdx, props);
			}

		} else if ((light.lightType == LightType.Spot)) {

			float chsa = GetCotanHalfSpotAngle (light.spotAngle);

			// Setup Light Matrix
			Matrix4x4 temp1 = Matrix4x4.Scale(new Vector3 (-.5f, -.5f, 1.0f));
			Matrix4x4 temp2 = Matrix4x4.Translate( new Vector3 (.5f, .5f, 0.0f));
			Matrix4x4 temp3 = PerspectiveCotanMatrix (chsa, 0.0f, range);
			var LightMatrix0 = temp2 * temp3 * temp1 * worldToLight;
			props.SetMatrix ("_LightMatrix0", LightMatrix0);

			// Setup Spot Rendering mesh matrix
			float sideLength = range / chsa;

			// builtin pyramid model range is -.1 to .1 so scale by 10
			lightToWorld = lightToWorld * Matrix4x4.Scale (new Vector3(sideLength*10, sideLength*10, range*10));

			//set default cookie for spot light if there wasnt one added to the light manually
			if (cookie == null)
				cmd.SetGlobalTexture ("_LightTexture0", m_DefaultSpotCookie);
			
			if (renderAsQuad) {
				cmd.DrawMesh (m_QuadMesh, Matrix4x4.identity, m_DeferredMaterial, 0, m_quadLightingPassNdx, props);
			} else {
				cmd.DrawMesh (m_SpotLightMesh, lightToWorld, m_DeferredMaterial, 0, m_FiniteLightingPassNdx, props);
			}
				
		} else {

			// Setup Light Matrix
			float scale = 1.0f;// / light.light.cookieSize;
			Matrix4x4 temp1 = Matrix4x4.Scale(new Vector3 (scale, scale, 0.0f));
			Matrix4x4 temp2 = Matrix4x4.Translate( new Vector3 (.5f, .5f, 0.0f));
			var LightMatrix0 = temp2 * temp1 * worldToLight;
			props.SetMatrix ("_LightMatrix0", LightMatrix0);
		
			if (cookie != null)
				cmd.EnableShaderKeyword ("DIRECTIONAL_COOKIE");
		

			cmd.DrawMesh (m_QuadMesh, Matrix4x4.identity, m_DeferredMaterial, 0, m_quadLightingPassNdx, props);
		}
	}

	Matrix4x4 PerspectiveCotanMatrix(float cotangent, float zNear, float zFar )
	{
		float deltaZ = zNear - zFar;
		var m = Matrix4x4.zero;

		m.m00 = cotangent;			m.m01 = 0.0f;      m.m02 = 0.0f;                    m.m03 = 0.0f;
		m.m10 = 0.0f;               m.m11 = cotangent; m.m12 = 0.0f;                    m.m13 = 0.0f;
		m.m20 = 0.0f;               m.m21 = 0.0f;      m.m22 = (zFar + zNear) / deltaZ; m.m23 = 2.0f * zNear * zFar / deltaZ;
		m.m30 = 0.0f;               m.m31 = 0.0f;      m.m32 = -1.0f;                   m.m33 = 0.0f;

		return m;
	}

	float GetCotanHalfSpotAngle (float spotAngle)
	{
		const float pi = 3.1415926535897932384626433832795f;
		const float degToRad = (float)(pi / 180.0);
		var cs = Mathf.Cos(0.5f * spotAngle * degToRad);
		var ss = Mathf.Sin(0.5f * spotAngle * degToRad);
		return cs / ss; //cothalfspotangle
		//m_InvCosHalfSpotAngle = 1.0f / cs;
	}

	static void DepthOnlyForForwardOpaques(CullResults cull, Camera camera, ScriptableRenderContext loop)
	{
		var cmd = new CommandBuffer { name = "Forward Opaques - Depth Only" };
		cmd.SetRenderTarget(new RenderTargetIdentifier(s_GBufferZ));
		loop.ExecuteCommandBuffer(cmd);
		cmd.Dispose();

		// render opaque objects using Deferred pass
		var settings = new DrawRendererSettings(cull, camera, new ShaderPassName("DepthOnly"))
		{
			sorting = { flags = SortFlags.CommonOpaque }
		};
		settings.inputFilter.SetQueuesOpaque();
		loop.DrawRenderers(ref settings);
	}

	static void CopyDepthAfterGBuffer(ScriptableRenderContext loop)
	{
		var cmd = new CommandBuffer { name = "Copy depth" };
		cmd.CopyTexture(new RenderTargetIdentifier(s_GBufferZ), new RenderTargetIdentifier(s_CameraDepthTexture));
		loop.ExecuteCommandBuffer(cmd);
		cmd.Dispose();
	}

	void FinalPass(ScriptableRenderContext loop)
	{
		var cmd = new CommandBuffer { name = "FinalPass" };
		cmd.Blit(s_GBufferEmission, BuiltinRenderTextureType.CameraTarget, m_BlitMaterial, 0);
		loop.ExecuteCommandBuffer(cmd);
		cmd.Dispose();
	}


}

