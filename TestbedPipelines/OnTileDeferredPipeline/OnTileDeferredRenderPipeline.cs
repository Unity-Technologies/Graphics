using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.OnTileDeferredRenderPipeline
{
	class ShadowSetup : IDisposable
	{
		// shadow related stuff
		const int k_MaxShadowDataSlots              = 64;
		const int k_MaxPayloadSlotsPerShadowData    =  4;
		ShadowmapBase[]         m_Shadowmaps;
		ShadowManager           m_ShadowMgr;
		ComputeBuffer    s_ShadowDataBuffer;
		ComputeBuffer    s_ShadowPayloadBuffer;

		public ShadowSetup(ShadowInitParameters shadowInit, ShadowSettings shadowSettings, out IShadowManager shadowManager)
		{
			s_ShadowDataBuffer = new ComputeBuffer(k_MaxShadowDataSlots, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShadowData)));
			s_ShadowPayloadBuffer = new ComputeBuffer(k_MaxShadowDataSlots * k_MaxPayloadSlotsPerShadowData, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShadowPayload)));
			ShadowAtlas.AtlasInit atlasInit;
			atlasInit.baseInit.width                  = (uint)shadowInit.shadowAtlasWidth;
			atlasInit.baseInit.height                 = (uint)shadowInit.shadowAtlasHeight;
			atlasInit.baseInit.slices                 = 1;
			atlasInit.baseInit.shadowmapBits          = 32;
			atlasInit.baseInit.shadowmapFormat        = RenderTextureFormat.Shadowmap;
			atlasInit.baseInit.samplerState           = SamplerState.Default();
			atlasInit.baseInit.comparisonSamplerState = ComparisonSamplerState.Default();
			atlasInit.baseInit.clearColor             = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
			atlasInit.baseInit.maxPayloadCount        = 0;
			atlasInit.baseInit.shadowSupport          = ShadowmapBase.ShadowSupport.Directional | ShadowmapBase.ShadowSupport.Point | ShadowmapBase.ShadowSupport.Spot;
			atlasInit.shaderKeyword                   = null;

			m_Shadowmaps = new ShadowmapBase[] { new ShadowAtlas(ref atlasInit) };

			ShadowContext.SyncDel syncer = (ShadowContext sc) =>
			{
				// update buffers
				uint offset, count;
				ShadowData[] sds;
				sc.GetShadowDatas(out sds, out offset, out count);
				Debug.Assert(offset == 0);
				s_ShadowDataBuffer.SetData(sds); // unfortunately we can't pass an offset or count to this function
				ShadowPayload[] payloads;
				sc.GetPayloads(out payloads, out offset, out count);
				Debug.Assert(offset == 0);
				s_ShadowPayloadBuffer.SetData(payloads);
			};

			// binding code. This needs to be in sync with ShadowContext.hlsl
			ShadowContext.BindDel binder = (ShadowContext sc, CommandBuffer cb, ComputeShader computeShader, int computeKernel) =>
			{
				// bind buffers
				cb.SetGlobalBuffer("_ShadowDatasExp", s_ShadowDataBuffer);
				cb.SetGlobalBuffer("_ShadowPayloads", s_ShadowPayloadBuffer);
				// bind textures
				uint offset, count;
				RenderTargetIdentifier[] tex;
				sc.GetTex2DArrays(out tex, out offset, out count);
				cb.SetGlobalTexture("_ShadowmapExp_PCF", tex[0]);
				// TODO: Currently samplers are hard coded in ShadowContext.hlsl, so we can't really set them here
			};

			ShadowContext.CtxtInit scInit;
			scInit.storage.maxShadowDataSlots        = k_MaxShadowDataSlots;
			scInit.storage.maxPayloadSlots           = k_MaxShadowDataSlots * k_MaxPayloadSlotsPerShadowData;
			scInit.storage.maxTex2DArraySlots        = 1;
			scInit.storage.maxTexCubeArraySlots      = 0;
			scInit.storage.maxComparisonSamplerSlots = 1;
			scInit.storage.maxSamplerSlots           = 0;
			scInit.dataSyncer                        = syncer;
			scInit.resourceBinder                    = binder;

			m_ShadowMgr = new ShadowManager(shadowSettings, ref scInit, m_Shadowmaps);
			// set global overrides - these need to match the override specified in Fptl/Shadow.hlsl
			m_ShadowMgr.SetGlobalShadowOverride( GPUShadowType.Point        , ShadowAlgorithm.PCF, ShadowVariant.V1, ShadowPrecision.High, true );
			m_ShadowMgr.SetGlobalShadowOverride( GPUShadowType.Spot         , ShadowAlgorithm.PCF, ShadowVariant.V1, ShadowPrecision.High, true );
			m_ShadowMgr.SetGlobalShadowOverride( GPUShadowType.Directional  , ShadowAlgorithm.PCF, ShadowVariant.V1, ShadowPrecision.High, true );
			shadowManager = m_ShadowMgr;
		}
		public void Dispose()
		{
			if (m_Shadowmaps != null)
			{
				(m_Shadowmaps[0] as ShadowAtlas).Dispose();
				m_Shadowmaps = null;
			}
			m_ShadowMgr = null;

			if (s_ShadowDataBuffer != null) {
				s_ShadowDataBuffer.Release ();
				s_ShadowDataBuffer = null;
			}
			if (s_ShadowPayloadBuffer != null) {
				s_ShadowPayloadBuffer.Release ();
				s_ShadowPayloadBuffer = null;
			}
		}
	}

	public class OnTileDeferredRenderPipelineInstance : RenderPipeline {

		private readonly OnTileDeferredRenderPipeline m_Owner;

		public OnTileDeferredRenderPipelineInstance(OnTileDeferredRenderPipeline owner)
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
	public class OnTileDeferredRenderPipeline : RenderPipelineAsset {

#if UNITY_EDITOR
        // Hide as not an official pipeline
        // [UnityEditor.MenuItem("Assets/Create/Render Pipeline/On Tile Deferred/Render Pipeline", priority = CoreUtils.assetCreateMenuPriority1)]
		static void CreateDeferredRenderPipeline()
		{
			var instance = ScriptableObject.CreateInstance<OnTileDeferredRenderPipeline> ();
			UnityEditor.AssetDatabase.CreateAsset (instance, "Assets/OnTileDeferredPipeline.asset");
		}

        // Hide as not an official pipeline
		//[UnityEditor.MenuItem("Edit/Render Pipeline/Upgrade/On Tile Deferred/Upgrade Standard Shader Materials", priority = CoreUtils.editMenuPriority2)]
		static void SetupDeferredRenderPipelineMaterials()
		{
			Renderer[] _renderers = Component.FindObjectsOfType<Renderer> ();
			foreach (Renderer _renderer in _renderers) {
				Material[] _materials = _renderer.sharedMaterials;
				foreach (Material _material in _materials) {
					if (_material == null)
						continue;

					if (_material.shader.name.Contains ("Standard (Specular setup)")) {
						_material.shader = Shader.Find("Standard-SRP (Specular setup)");
					} else if (_material.shader.name.Contains ("Standard")) {
						_material.shader = Shader.Find("Standard-SRP");
					}

				}
			}
		}
	#endif

		protected override IRenderPipeline InternalCreatePipeline()
		{
			return new OnTileDeferredRenderPipelineInstance(this);
		}

		[SerializeField] ShadowSettings m_ShadowSettings = new ShadowSettings();

		ShadowSetup    m_ShadowSetup;
		IShadowManager m_ShadowMgr;
		FrameId        m_FrameId = new FrameId();

		List<int>               m_ShadowRequests = new List<int>();
		Dictionary<int, int>    m_ShadowIndices = new Dictionary<int,int>();

		void InitShadowSystem(ShadowSettings shadowSettings)
		{
			m_ShadowSetup = new ShadowSetup(new ShadowInitParameters(), shadowSettings, out m_ShadowMgr);
		}

		void DeinitShadowSystem()
		{
			if (m_ShadowSetup != null)
			{
				m_ShadowSetup.Dispose();
				m_ShadowSetup = null;
				m_ShadowMgr = null;
			}
		}

		// This must match MAX_LIGHTS in UnityStandardForwardMobile
		const int k_MaxLights = 100;

		// arrays for shader data
		private Vector4[] m_LightData = new Vector4[k_MaxLights]; // x:Light_type, y:ShadowIndex z:w:UNUSED
		private Vector4[] m_LightPositions = new Vector4[k_MaxLights];
		private Vector4[] m_LightColors = new Vector4[k_MaxLights];
		private Vector4[] m_LightDirections = new Vector4[k_MaxLights];
		private Matrix4x4[] m_LightMatrix = new Matrix4x4[k_MaxLights];
		private Matrix4x4[] m_WorldToLightMatrix = new Matrix4x4[k_MaxLights];

		[SerializeField] TextureSettings m_TextureSettings = new TextureSettings();
		[SerializeField] public bool UseLegacyCookies;
		[SerializeField] public bool TransparencyShadows;
		[SerializeField] public Mesh m_PointLightMesh;
		[SerializeField] public float PointLightMeshScaleFactor = 2.0f;
		[SerializeField] public Mesh m_SpotLightMesh;
		[SerializeField] public float SpotLightMeshScaleFactor = 1.0f;
		[SerializeField] public Mesh m_QuadMesh;
		[SerializeField] public Mesh m_BoxMesh;
		[SerializeField] public Texture m_DefaultSpotCookie;
		[SerializeField] public Shader finalPassShader;
		[SerializeField] public Shader deferredShader;
		[SerializeField] public Shader deferredReflectionShader;

		private TextureCache2D m_CookieTexArray;
		private TextureCacheCubemap m_CubeCookieTexArray;
		private TextureCacheCubemap m_CubeReflTexArray;

		private ComputeBuffer s_LightDataBuffer;

		private RenderPassAttachment s_GBufferAlbedo;
		private RenderPassAttachment s_GBufferSpecRough;
		private RenderPassAttachment s_GBufferNormal;
		private RenderPassAttachment s_GBufferEmission;
		private RenderPassAttachment s_CameraTarget;
		private RenderPassAttachment s_Depth;

		// write depth to red color buffer if on mobile so we can read it back
		// cannot read depth buffer directly in shader on iOS
		private RenderPassAttachment s_GBufferRedF32;

		// TODO: When graphics/renderpass lands, replace code that uses boolean below with SystemInfo.supportsReadOnlyDepth
		#if UNITY_EDITOR || UNITY_STANDALONE
		static bool s_SupportsReadOnlyDepth = true;
		#else
		static bool s_SupportsReadOnlyDepth = false;
		#endif

		private static int _sceneViewBlitId;
		private static int _sceneViewDepthId;
		private static Material _blitDepthMaterial;

		private Material m_DirectionalDeferredLightingMaterial;
		private Material m_FiniteDeferredLightingMaterial;
		private Material m_FiniteNearDeferredLightingMaterial;

		private Material m_ReflectionMaterial;
		private Material m_ReflectionNearClipMaterial;
		private Material m_ReflectionNearAndFarClipMaterial;

		private Material m_BlitMaterial;

		private void OnValidate()
		{
			Build();
		}

		public void Cleanup()
		{
			if (m_BlitMaterial) DestroyImmediate(m_BlitMaterial);
			if (m_DirectionalDeferredLightingMaterial) DestroyImmediate(m_DirectionalDeferredLightingMaterial);
			if (m_FiniteDeferredLightingMaterial) DestroyImmediate(m_FiniteDeferredLightingMaterial);
			if (m_FiniteNearDeferredLightingMaterial) DestroyImmediate(m_FiniteNearDeferredLightingMaterial);
			if (m_ReflectionMaterial) DestroyImmediate (m_ReflectionMaterial);
			if (m_ReflectionNearClipMaterial) DestroyImmediate (m_ReflectionNearClipMaterial);
			if (m_ReflectionNearAndFarClipMaterial) DestroyImmediate (m_ReflectionNearAndFarClipMaterial);

			if (s_LightDataBuffer != null) {
				s_LightDataBuffer.Release ();
				s_LightDataBuffer = null;
			}

			m_CookieTexArray.Release();
			m_CubeCookieTexArray.Release();
			m_CubeReflTexArray.Release();

			DeinitShadowSystem();
		}

		public void Build()
		{
			s_GBufferAlbedo = new RenderPassAttachment(RenderTextureFormat.ARGB32) { hideFlags = HideFlags.HideAndDontSave };
			s_GBufferSpecRough = new RenderPassAttachment(RenderTextureFormat.ARGB32) { hideFlags = HideFlags.HideAndDontSave };
			s_GBufferNormal = new RenderPassAttachment(RenderTextureFormat.ARGB2101010) { hideFlags = HideFlags.HideAndDontSave };
			s_GBufferEmission = new RenderPassAttachment(RenderTextureFormat.ARGBHalf) { hideFlags = HideFlags.HideAndDontSave };
			s_Depth = new RenderPassAttachment(RenderTextureFormat.Depth) { hideFlags = HideFlags.HideAndDontSave };
			s_CameraTarget = s_GBufferAlbedo;

			s_GBufferEmission.Clear(new Color(0.0f, 0.0f, 0.0f, 0.0f), 1.0f, 0);
			s_Depth.Clear(new Color(), 1.0f, 0);

			if (s_SupportsReadOnlyDepth)
			{
				s_GBufferRedF32 = null;
			}
			else
			{
				s_GBufferRedF32 = new RenderPassAttachment(RenderTextureFormat.RFloat) { hideFlags = HideFlags.HideAndDontSave };
				s_GBufferRedF32.Clear(new Color(), 1.0f, 0);
			}

			m_BlitMaterial = new Material (finalPassShader) { hideFlags = HideFlags.HideAndDontSave };

			_blitDepthMaterial = new Material(Shader.Find("Hidden/BlitCopyWithDepth")) { hideFlags = HideFlags.HideAndDontSave };
			_sceneViewBlitId = Shader.PropertyToID("_TempCameraRT");
			_sceneViewDepthId = Shader.PropertyToID("_TempCameraDepth");

			m_DirectionalDeferredLightingMaterial = new Material (deferredShader) { hideFlags = HideFlags.HideAndDontSave };
			m_DirectionalDeferredLightingMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
			m_DirectionalDeferredLightingMaterial.SetInt("_DstBlend", (int)BlendMode.One);
			m_DirectionalDeferredLightingMaterial.SetInt("_SrcABlend", (int)BlendMode.One);
			m_DirectionalDeferredLightingMaterial.SetInt("_DstABlend", (int)BlendMode.Zero);
			m_DirectionalDeferredLightingMaterial.SetInt("_CullMode", (int)CullMode.Off);
			m_DirectionalDeferredLightingMaterial.SetInt("_CompareFunc", (int)CompareFunction.Always);

			m_FiniteDeferredLightingMaterial = new Material (deferredShader) { hideFlags = HideFlags.HideAndDontSave };
			m_FiniteDeferredLightingMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
			m_FiniteDeferredLightingMaterial.SetInt("_DstBlend", (int)BlendMode.One);
			m_FiniteDeferredLightingMaterial.SetInt("_SrcABlend", (int)BlendMode.One);
			m_FiniteDeferredLightingMaterial.SetInt("_DstABlend", (int)BlendMode.Zero);
			m_FiniteDeferredLightingMaterial.SetInt("_CullMode", (int)CullMode.Back);
			m_FiniteDeferredLightingMaterial.SetInt("_CompareFunc", (int)CompareFunction.LessEqual);

			m_FiniteNearDeferredLightingMaterial = new Material (deferredShader) { hideFlags = HideFlags.HideAndDontSave };
			m_FiniteNearDeferredLightingMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
			m_FiniteNearDeferredLightingMaterial.SetInt("_DstBlend", (int)BlendMode.One);
			m_FiniteNearDeferredLightingMaterial.SetInt("_SrcABlend", (int)BlendMode.One);
			m_FiniteNearDeferredLightingMaterial.SetInt("_DstABlend", (int)BlendMode.Zero);
			m_FiniteNearDeferredLightingMaterial.SetInt("_CullMode", (int)CullMode.Front);
			m_FiniteNearDeferredLightingMaterial.SetInt("_CompareFunc", (int)CompareFunction.Greater);

			m_ReflectionMaterial = new Material (deferredReflectionShader) { hideFlags = HideFlags.HideAndDontSave };
			m_ReflectionMaterial.SetInt("_SrcBlend", (int)BlendMode.DstAlpha);
			m_ReflectionMaterial.SetInt("_DstBlend", (int)BlendMode.One);
			m_ReflectionMaterial.SetInt("_SrcABlend", (int)BlendMode.DstAlpha);
			m_ReflectionMaterial.SetInt("_DstABlend", (int)BlendMode.Zero);
			m_ReflectionMaterial.SetInt("_CullMode", (int)CullMode.Back);
			m_ReflectionMaterial.SetInt("_CompareFunc", (int)CompareFunction.LessEqual);

			m_ReflectionNearClipMaterial = new Material (deferredReflectionShader) { hideFlags = HideFlags.HideAndDontSave };
			m_ReflectionNearClipMaterial.SetInt("_SrcBlend", (int)BlendMode.DstAlpha);
			m_ReflectionNearClipMaterial.SetInt("_DstBlend", (int)BlendMode.One);
			m_ReflectionNearClipMaterial.SetInt("_SrcABlend", (int)BlendMode.DstAlpha);
			m_ReflectionNearClipMaterial.SetInt("_DstABlend", (int)BlendMode.Zero);
			m_ReflectionNearClipMaterial.SetInt("_CullMode", (int)CullMode.Front);
			m_ReflectionNearClipMaterial.SetInt("_CompareFunc", (int)CompareFunction.GreaterEqual);

			m_ReflectionNearAndFarClipMaterial = new Material (deferredReflectionShader) { hideFlags = HideFlags.HideAndDontSave };
			m_ReflectionNearAndFarClipMaterial.SetInt("_SrcBlend", (int)BlendMode.DstAlpha);
			m_ReflectionNearAndFarClipMaterial.SetInt("_DstBlend", (int)BlendMode.One);
			m_ReflectionNearAndFarClipMaterial.SetInt("_SrcABlend", (int)BlendMode.DstAlpha);
			m_ReflectionNearAndFarClipMaterial.SetInt("_DstABlend", (int)BlendMode.Zero);
			m_ReflectionNearAndFarClipMaterial.SetInt("_CullMode", (int)CullMode.Off);
			m_ReflectionNearAndFarClipMaterial.SetInt("_CompareFunc", (int)CompareFunction.Always);

			m_CookieTexArray = new TextureCache2D();
			m_CubeCookieTexArray = new TextureCacheCubemap();
			m_CubeReflTexArray = new TextureCacheCubemap();
			m_CookieTexArray.AllocTextureArray(8, m_TextureSettings.spotCookieSize, m_TextureSettings.spotCookieSize, TextureFormat.RGBA32, true);
			m_CubeCookieTexArray.AllocTextureArray(4, m_TextureSettings.pointCookieSize, TextureFormat.RGBA32, true);
			m_CubeReflTexArray.AllocTextureArray(64, m_TextureSettings.reflectionCubemapSize, TextureCache.GetPreferredHDRCompressedTextureFormat, true);

			s_LightDataBuffer = new ComputeBuffer(k_MaxLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SFiniteLightData)));

			//shadows
			InitShadowSystem(m_ShadowSettings);
		}

		void NewFrame()
		{
			// update texture caches
			m_CookieTexArray.NewFrame();
			m_CubeCookieTexArray.NewFrame();
			m_CubeReflTexArray.NewFrame();
		}


		public void Render(ScriptableRenderContext context, IEnumerable<Camera> cameras)
		{
			foreach (var camera in cameras) {
				// Culling
				ScriptableCullingParameters cullingParams;
				if (!CullResults.GetCullingParameters (camera, out cullingParams))
					continue;

				m_ShadowMgr.UpdateCullingParameters(ref cullingParams);

				var cullResults = CullResults.Cull (ref cullingParams, context);

				NewFrame ();

				UpdateShadowConstants (camera, cullResults);

				CommandBuffer cmdShadow = CommandBufferPool.Get();
				m_ShadowMgr.RenderShadows( m_FrameId, context, cmdShadow, cullResults, cullResults.visibleLights );
				m_ShadowMgr.SyncData();
				m_ShadowMgr.BindResources( cmdShadow, null, 0 );
				context.ExecuteCommandBuffer(cmdShadow);
				CommandBufferPool.Release(cmdShadow);

				context.SetupCameraProperties(camera);

				// The scene view needs extra blit because it'll be using a non-fullscreen viewport
				if (camera.cameraType == CameraType.SceneView)
				{
					using (var cmd = new CommandBuffer())
					{
						cmd.GetTemporaryRT(_sceneViewBlitId, camera.pixelWidth, camera.pixelHeight, 0,
							FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
						cmd.GetTemporaryRT(_sceneViewDepthId, camera.pixelWidth, camera.pixelHeight, 24,
							FilterMode.Point, RenderTextureFormat.Depth, RenderTextureReadWrite.Default);
						context.ExecuteCommandBuffer(cmd);
					}
					s_CameraTarget.BindSurface(new RenderTargetIdentifier(_sceneViewBlitId), false, true);
					s_Depth.BindSurface(new RenderTargetIdentifier(_sceneViewDepthId), false, false);
				}
				else
				{
					s_CameraTarget.BindSurface(BuiltinRenderTextureType.CameraTarget, false, true);
					s_Depth.BindSurface(BuiltinRenderTextureType.Depth, false, false);
				}

				// set load store actions
				s_GBufferSpecRough.BindSurface (BuiltinRenderTextureType.None, false, false);
				s_GBufferNormal.BindSurface (BuiltinRenderTextureType.None, false, false);
				s_GBufferEmission.BindSurface (BuiltinRenderTextureType.None, false, false);
				if (s_GBufferRedF32 != null)
					s_GBufferRedF32.BindSurface(BuiltinRenderTextureType.None, false, false);

				ExecuteRenderLoop(camera, cullResults, context);

				if (camera.cameraType == CameraType.SceneView)
				{
					using (var cmd = new CommandBuffer())
					{
						cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
						cmd.SetGlobalTexture("_DepthTex", _sceneViewDepthId);
						cmd.Blit(_sceneViewBlitId, BuiltinRenderTextureType.CameraTarget, _blitDepthMaterial);
						context.ExecuteCommandBuffer(cmd);
					}
				}

				context.Submit ();
			}

		}

		void ExecuteRenderLoop(Camera camera, CullResults cullResults, ScriptableRenderContext loop)
		{
			using (RenderPass rp = new RenderPass (loop, camera.pixelWidth, camera.pixelHeight, 1, s_SupportsReadOnlyDepth ?
				new[] { s_GBufferAlbedo, s_GBufferSpecRough, s_GBufferNormal, s_GBufferEmission } :
				new[] { s_GBufferAlbedo, s_GBufferSpecRough, s_GBufferNormal, s_GBufferEmission, s_GBufferRedF32 }, s_Depth)) {

				// GBuffer pass
				using (new RenderPass.SubPass (rp, s_SupportsReadOnlyDepth ?
					new[] { s_GBufferAlbedo, s_GBufferSpecRough, s_GBufferNormal, s_GBufferEmission } :
					new[] { s_GBufferAlbedo, s_GBufferSpecRough, s_GBufferNormal, s_GBufferEmission, s_GBufferRedF32 }, null)) {
					using (var cmd = new CommandBuffer { name = "Create G-Buffer" }) {

						cmd.EnableShaderKeyword ("UNITY_HDR_ON");
						cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
						loop.ExecuteCommandBuffer (cmd);

						// render opaque objects using Deferred pass
						var drawSettings = new DrawRendererSettings (camera, new ShaderPassName ("Deferred")) {
							sorting = { flags = SortFlags.CommonOpaque },
							rendererConfiguration = RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe
						};
                        var filterSettings = new FilterRenderersSettings(true) {renderQueueRange = RenderQueueRange.opaque};
						loop.DrawRenderers (cullResults.visibleRenderers, ref drawSettings, filterSettings);

					}
				}

				//Lighting Pass
				using (new RenderPass.SubPass(rp, new[] { s_GBufferEmission },
					new[] { s_GBufferAlbedo, s_GBufferSpecRough, s_GBufferNormal, s_SupportsReadOnlyDepth ? s_Depth : s_GBufferRedF32 }, true))
				{
					using (var cmd = new CommandBuffer { name = "Deferred Lighting and Reflections Pass"} )
					{
						RenderLightsDeferred (camera, cullResults, cmd, loop);
						RenderReflections (camera, cmd, cullResults, loop);

						loop.ExecuteCommandBuffer(cmd);
					}
				}

				//skybox
				using (new RenderPass.SubPass (rp, new[] { s_GBufferEmission }, null)) {
					loop.DrawSkybox (camera);
				}

				//Single Pass Forward Transparencies
				using (new RenderPass.SubPass(rp, new[] { s_GBufferEmission }, null))
				{
					using (var cmd = new CommandBuffer { name = "Forwward Lighting Setup"} )
					{

						SetupLightShaderVariables (cullResults, camera, loop, cmd);
						loop.ExecuteCommandBuffer(cmd);

						var settings = new DrawRendererSettings(camera, new ShaderPassName("ForwardSinglePass"))
						{
							sorting = { flags = SortFlags.CommonTransparent },
							rendererConfiguration = RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe,
						};
					    var filterSettings = new FilterRenderersSettings(true) {renderQueueRange = RenderQueueRange.transparent};
						loop.DrawRenderers (cullResults.visibleRenderers, ref settings, filterSettings);
					}
				}

				//Final pass
				using (new RenderPass.SubPass(rp, new[] { s_CameraTarget }, new[] { s_GBufferEmission }))
				{
					var cmd = new CommandBuffer { name = "FinalPass" };

					cmd.DrawProcedural(new Matrix4x4(), m_BlitMaterial, 0, MeshTopology.Triangles, 3);

					loop.ExecuteCommandBuffer(cmd);
					cmd.Dispose();

				}
			}
		}

		// Utilites
		static Matrix4x4 GetFlipMatrix()
		{
			Matrix4x4 flip = Matrix4x4.identity;
			bool isLeftHand = ((int)LightDefinitions.USE_LEFTHAND_CAMERASPACE) != 0;
			if (isLeftHand) flip.SetColumn(2, new Vector4(0.0f, 0.0f, -1.0f, 0.0f));
			return flip;
		}

		static Matrix4x4 WorldToCamera(Camera camera)
		{
			return GetFlipMatrix() * camera.worldToCameraMatrix;
		}

		static Matrix4x4 CameraToWorld(Camera camera)
		{
			return camera.cameraToWorldMatrix * GetFlipMatrix();
		}

		static Matrix4x4 CameraProjection(Camera camera)
		{
			return camera.projectionMatrix * GetFlipMatrix();
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
			const float degToRad = (float)(Mathf.PI / 180.0);
			var cs = Mathf.Cos(0.5f * spotAngle * degToRad);
			var ss = Mathf.Sin(0.5f * spotAngle * degToRad);
			return cs / ss; //cothalfspotangle
		}

		// Shadows
		void UpdateShadowConstants(Camera camera, CullResults inputs)
		{
			m_FrameId.frameCount++;
			// get the indices for all lights that want to have shadows
			m_ShadowRequests.Clear();
			m_ShadowRequests.Capacity = inputs.visibleLights.Count;
			int lcnt = inputs.visibleLights.Count;
			for (int i = 0; i < lcnt; ++i)
			{
				VisibleLight vl = inputs.visibleLights[i];

				AdditionalShadowData asd = vl.light.GetComponent<AdditionalShadowData>();

				if (vl.light.shadows != LightShadows.None && asd != null && asd.shadowDimmer > 0.0f)
					m_ShadowRequests.Add(i);
			}
			// pass this list to a routine that assigns shadows based on some heuristic
			uint shadowRequestCount = (uint)m_ShadowRequests.Count;
			int[] shadowRequests = m_ShadowRequests.ToArray();
			int[] shadowDataIndices;
			m_ShadowMgr.ProcessShadowRequests(m_FrameId, inputs, camera, false, inputs.visibleLights,
				ref shadowRequestCount, shadowRequests, out shadowDataIndices);

			// update the visibleLights with the shadow information
			m_ShadowIndices.Clear();
			for (uint i = 0; i < shadowRequestCount; i++)
			{
				m_ShadowIndices.Add(shadowRequests[i], shadowDataIndices[i]);
			}
		}

		// Reflections
		void RenderReflections(Camera camera, CommandBuffer cmd, CullResults cullResults, ScriptableRenderContext loop)
		{
			var probes = cullResults.visibleReflectionProbes;
			var worldToView = camera.worldToCameraMatrix; //WorldToCamera(camera);

			// matches builtin deferred
			float nearDistanceFudged = camera.nearClipPlane * 1.001f;
			float farDistanceFudged = camera.farClipPlane * 0.999f;
			var viewDir = camera.cameraToWorldMatrix.GetColumn(2);
			var viewDirNormalized = -1 * Vector3.Normalize(new Vector3 (viewDir.x, viewDir.y, viewDir.z));

			Plane eyePlane = new Plane ();
			eyePlane.SetNormalAndPosition(viewDirNormalized, camera.transform.position);

			// Note: Optimization for tiled GPUs: render all probes in reverse order so they are blended into the existing emission buffer with the correct blend settings as follows:
			// emisNew = emis + Lerp( Lerp( Lerp(base,probe0,1-t0), probe1, 1-t1 ), probe2, 1-t2)....
			// DST_COL = DST_COL + DST_ALPHA * SRC_COLOR
			// DST_ALPHA = DST_ALPHA * SRC_ALPHA

			int numProbes = probes.Count;
			for (int i = numProbes-1; i >= 0; i--)
			{
				var rl = probes [i];
				var cubemap = rl.texture;

				// always a box for now
				if (cubemap == null)
					continue;

				var bnds = rl.bounds;
				var boxOffset = rl.center;                  // reflection volume offset relative to cube map capture point
				var blendDistance = rl.blendDistance;

				// TODO: fix for rotations on probes... Builtin Unity also does not take these into account, for now just grab position for mat
				//var mat = rl.localToWorld;
				Matrix4x4 mat = Matrix4x4.identity;
				mat.SetColumn (3, rl.localToWorld.GetColumn (3));

				var boxProj = (rl.boxProjection != 0);
				var probePosition = mat.GetColumn (3); // translation vector
				var probePosition1 = new Vector4 (probePosition [0], probePosition [1], probePosition [2], boxProj ? 1f : 0f);

				// C is reflection volume center in world space (NOT same as cube map capture point)
				var e = bnds.extents;       // 0.5f * Vector3.Max(-boxSizes[p], boxSizes[p]);
				var combinedExtent = e + new Vector3(blendDistance, blendDistance, blendDistance);

				Matrix4x4 scaled = Matrix4x4.Scale (combinedExtent * 2.0f);
				mat = mat * Matrix4x4.Translate (boxOffset) * scaled;

				var probeRadius = combinedExtent.magnitude;
				var viewDistance = eyePlane.GetDistanceToPoint(boxOffset);
				bool intersectsNear = viewDistance - probeRadius <= nearDistanceFudged;
				bool intersectsFar = viewDistance + probeRadius >= farDistanceFudged;
				bool renderAsQuad = (intersectsNear && intersectsFar);

				var props = new MaterialPropertyBlock ();
				props.SetFloat ("_LightAsQuad", renderAsQuad ? 1 : 0);

				var min = rl.bounds.min;
				var max = rl.bounds.max;

				// TODO: (cleanup) dont use builtins like unity_SpecCube0
				cmd.SetGlobalTexture("unity_SpecCube0", cubemap);
				cmd.SetGlobalVector("unity_SpecCube0_HDR", rl.probe.textureHDRDecodeValues);
				cmd.SetGlobalVector ("unity_SpecCube0_BoxMin", min);
				cmd.SetGlobalVector ("unity_SpecCube0_BoxMax", max);
				cmd.SetGlobalVector ("unity_SpecCube0_ProbePosition", probePosition1);
				cmd.SetGlobalVector ("unity_SpecCube1_ProbePosition", new Vector4(0, 0, 0, blendDistance));

				if (renderAsQuad) {
					cmd.DrawMesh (m_QuadMesh, Matrix4x4.identity, m_ReflectionNearAndFarClipMaterial, 0, 0, props);
				} else if (intersectsNear) {
					cmd.DrawMesh (m_BoxMesh, mat, m_ReflectionNearClipMaterial, 0, 0, props);
				} else{
					cmd.DrawMesh (m_BoxMesh, mat, m_ReflectionMaterial, 0, 0, props);
				}
			}

			// draw the base probe
			// TODO: (cleanup) dont use builtins like unity_SpecCube0
			{
				var props = new MaterialPropertyBlock ();
				props.SetFloat ("_LightAsQuad", 1.0f);

				// base reflection probe
				var topCube = ReflectionProbe.defaultTexture;
				var defdecode = ReflectionProbe.defaultTextureHDRDecodeValues;
				cmd.SetGlobalTexture ("unity_SpecCube0", topCube);
				cmd.SetGlobalVector ("unity_SpecCube0_HDR", defdecode);

				float max = float.PositiveInfinity;
				float min = float.NegativeInfinity;
				cmd.SetGlobalVector("unity_SpecCube0_BoxMin", new Vector4(min, min, min, 1));
				cmd.SetGlobalVector("unity_SpecCube0_BoxMax", new Vector4(max, max, max, 1));

				cmd.SetGlobalVector ("unity_SpecCube0_ProbePosition", new Vector4 (0.0f, 0.0f, 0.0f, 0.0f));
				cmd.SetGlobalVector ("unity_SpecCube1_ProbePosition", new Vector4 (0.0f, 0.0f, 0.0f, 1.0f));

				cmd.DrawMesh (m_QuadMesh, Matrix4x4.identity, m_ReflectionNearAndFarClipMaterial, 0, 0, props);
			}
		}

		Matrix4x4 SpotlightMatrix (VisibleLight light, Matrix4x4 worldToLight, float range, float chsa)
		{
			Matrix4x4 temp1 = Matrix4x4.Scale(new Vector3 (-.5f, -.5f, 1.0f));
			Matrix4x4 temp2 = Matrix4x4.Translate( new Vector3 (.5f, .5f, 0.0f));
			Matrix4x4 temp3 = PerspectiveCotanMatrix (chsa, 0.0f, range);
			return temp2 * temp1 * temp3 * worldToLight;
		}

		void RenderSpotlight(VisibleLight light, CommandBuffer cmd, MaterialPropertyBlock properties, bool renderAsQuad, bool intersectsNear, bool deferred)
		{
			float range = light.range;
			var lightToWorld = light.localToWorld;
			var worldToLight = lightToWorld.inverse;
			float chsa = GetCotanHalfSpotAngle (light.spotAngle);

			// Setup Light Matrix
			properties.SetMatrix ("_LightMatrix0", SpotlightMatrix(light, worldToLight, range, chsa));

			// Setup Spot Rendering mesh matrix
			float sideLength = range / chsa;

			// scalingFactor corrosoponds to the scale factor setting (and wether file scale is used) of mesh in Unity mesh inspector.
			// A scale factor setting in Unity of 0.01 would require this to be set to 100. A scale factor setting of 1, is just 1 here.
			lightToWorld = lightToWorld * Matrix4x4.Scale (new Vector3(sideLength*SpotLightMeshScaleFactor, sideLength*SpotLightMeshScaleFactor, range*SpotLightMeshScaleFactor));

			//set default cookie for spot light if there wasnt one added to the light manually
			Texture cookie = light.light.cookie;
			if (cookie == null) {
				cmd.SetGlobalTexture ("_LightTexture0", m_DefaultSpotCookie);
			}

			// turn on spotlights in shader, there is no spot cookie varient so no need for that
			cmd.EnableShaderKeyword ("SPOT");

			if (renderAsQuad) {
				cmd.DrawMesh (m_QuadMesh, Matrix4x4.identity, m_DirectionalDeferredLightingMaterial, 0, 0, properties);
			} else if (intersectsNear) {
				cmd.DrawMesh (m_SpotLightMesh, lightToWorld, m_FiniteNearDeferredLightingMaterial, 0, 0, properties);
			} else {
				cmd.DrawMesh (m_SpotLightMesh, lightToWorld, m_FiniteDeferredLightingMaterial, 0, 0, properties);
			}
		}

		void RenderPointLight(VisibleLight light, CommandBuffer cmd, MaterialPropertyBlock properties, bool renderAsQuad, bool intersectsNear, bool deferred)
		{
			Vector3 lightPos = light.localToWorld.GetColumn (3); //position
			float range = light.range;

			// scalingFactor corrosoponds to the scale factor setting (and wether file scale is used) of mesh in Unity mesh inspector.
			// A scale factor setting in Unity of 0.01 would require this to be set to 100. A scale factor setting of 1, is just 1 here.
			var matrix = Matrix4x4.TRS (lightPos, Quaternion.identity, new Vector3 (range*PointLightMeshScaleFactor, range*PointLightMeshScaleFactor, range*PointLightMeshScaleFactor));

			Texture cookie = light.light.cookie;
			if (cookie != null)
				cmd.EnableShaderKeyword ("POINT_COOKIE");
			else
				cmd.EnableShaderKeyword ("POINT");

			if (renderAsQuad)
				cmd.DrawMesh (m_QuadMesh, Matrix4x4.identity, m_DirectionalDeferredLightingMaterial, 0, 0, properties);
			else if (intersectsNear)
				cmd.DrawMesh (m_PointLightMesh, matrix, m_FiniteNearDeferredLightingMaterial, 0, 0, properties);
			else
				cmd.DrawMesh (m_PointLightMesh, matrix, m_FiniteDeferredLightingMaterial, 0, 0, properties);

		}

		Matrix4x4 DirectionalLightmatrix(VisibleLight light, Matrix4x4 worldToLight)
		{
			// Setup Light Matrix
			float scale = 1.0f / light.light.cookieSize;
			Matrix4x4 temp1 = Matrix4x4.Scale(new Vector3 (scale, scale, 0.0f));
			Matrix4x4 temp2 = Matrix4x4.Translate( new Vector3 (.5f, .5f, 0.0f));
			return temp2 * temp1 * worldToLight;
		}

		void RenderDirectionalLight(VisibleLight light, CommandBuffer cmd, MaterialPropertyBlock properties, bool intersectsNear)
		{
			var lightToWorld = light.localToWorld;
			var worldToLight = lightToWorld.inverse;

			// Setup Light Matrix
			properties.SetMatrix ("_LightMatrix0", DirectionalLightmatrix (light, worldToLight));

			Texture cookie = light.light.cookie;
			if (cookie != null) {
				cmd.EnableShaderKeyword ("DIRECTIONAL_COOKIE");
			} else
				cmd.EnableShaderKeyword ("DIRECTIONAL");

			cmd.DrawMesh (m_QuadMesh, Matrix4x4.identity, m_DirectionalDeferredLightingMaterial, 0, 0, properties);
		}

		void RenderLightsDeferred (Camera camera, CullResults inputs, CommandBuffer cmd, ScriptableRenderContext loop)
		{
			int lightCount = inputs.visibleLights.Count;
			for (int lightNum = 0; lightNum < lightCount; lightNum++)
			{
				VisibleLight light = inputs.visibleLights[lightNum];

				bool intersectsNear = (light.flags & VisibleLightFlags.IntersectsNearPlane) != 0;
				bool intersectsFar = (light.flags & VisibleLightFlags.IntersectsFarPlane) != 0;
				bool renderAsQuad =  (intersectsNear && intersectsFar) || (light.lightType == LightType.Directional);

				Vector3 lightPos = light.localToWorld.GetColumn (3); //position
				Vector3 lightDir = light.localToWorld.GetColumn (2); //z axis
				float range = light.range;
				var lightToWorld = light.localToWorld;
				var worldToLight = lightToWorld.inverse;

				cmd.SetGlobalMatrix ("unity_WorldToLight", lightToWorld.inverse);

				var props = new MaterialPropertyBlock ();
				props.SetFloat ("_LightAsQuad", renderAsQuad ? 1 : 0);
				props.SetVector ("_LightPos", new Vector4(lightPos.x, lightPos.y, lightPos.z, 1.0f / (range * range)));
				props.SetVector ("_LightDir", new Vector4(lightDir.x, lightDir.y, lightDir.z, 0.0f));
				props.SetVector ("_LightColor", light.finalColor);

				int shadowIdx;
				float lightShadowNDXOrNot = m_ShadowIndices.TryGetValue( (int) lightNum, out shadowIdx ) ? (float) shadowIdx : -1.0f;
				props.SetFloat ("_LightIndexForShadowMatrixArray", lightShadowNDXOrNot);
				props.SetFloat ("_useLegacyCookies", UseLegacyCookies?1.0f:0.0f);

				Texture cookie = light.light.cookie;
				if (cookie != null)
					cmd.SetGlobalTexture ("_LightTexture0", cookie);

				cmd.DisableShaderKeyword ("POINT");
				cmd.DisableShaderKeyword ("POINT_COOKIE");
				cmd.DisableShaderKeyword ("SPOT");
				cmd.DisableShaderKeyword ("DIRECTIONAL");
				cmd.DisableShaderKeyword ("DIRECTIONAL_COOKIE");
				switch (light.lightType)
				{
				case LightType.Point:
					RenderPointLight (light, cmd, props, renderAsQuad, intersectsNear, true);
					break;
				case LightType.Spot:
					RenderSpotlight (light, cmd, props, renderAsQuad, intersectsNear, true);
					break;
				case LightType.Directional:
					RenderDirectionalLight(light, cmd, props, intersectsNear);
					break;
				}
			}
		}

		private void InitializeLightData()
		{
			for (int i = 0; i < k_MaxLights; i++)
			{
				m_LightData [i] = new Vector4(0.0f, -1.0f, -1.0f, 0.0f);
				m_LightColors[i] = Vector4.zero;
				m_LightDirections[i] = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
				m_LightPositions[i] = Vector4.zero;
				m_LightMatrix[i] = Matrix4x4.identity;
				m_WorldToLightMatrix[i] = Matrix4x4.identity;
			}
		}

		private void SetupLightShaderVariables(CullResults cull, Camera camera, ScriptableRenderContext context, CommandBuffer cmd)
		{
			int totalLightCount = cull.visibleLights.Count;
			InitializeLightData();

			var w = camera.pixelWidth;
			var h = camera.pixelHeight;
			var viewToWorld = CameraToWorld (camera);
			var worldToView = WorldToCamera(camera);

			// camera to screen matrix (and it's inverse)
			var proj = CameraProjection(camera);
			var temp = new Matrix4x4();
			temp.SetRow(0, new Vector4(0.5f * w, 0.0f, 0.0f, 0.5f * w));
			temp.SetRow(1, new Vector4(0.0f, 0.5f * h, 0.0f, 0.5f * h));
			temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
			temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			var projscr = temp * proj;
			var invProjscr = projscr.inverse;

			for (int i = 0; i < totalLightCount; ++i)
			{
				VisibleLight light = cull.visibleLights [i];

				Vector3 lightPos = light.localToWorld.GetColumn (3); //position
				Vector3 lightDir = light.localToWorld.GetColumn (2); //z axis
				float range = light.range;
				float rangeSq = light.range * light.range;
				var lightToWorld = light.localToWorld;
				var worldToLight = lightToWorld.inverse;

				m_WorldToLightMatrix[i] = worldToLight;

				//postiions and directions
				m_LightPositions [i] = new Vector4(lightPos.x, lightPos.y, lightPos.z, 1.0f / rangeSq);
				m_LightDirections [i] = new Vector4(lightDir.x, lightDir.y, lightDir.z, 0.0f);

				//shadow index
				int shadowIdx;
				float lightShadowNDXOrNot = m_ShadowIndices.TryGetValue(i, out shadowIdx ) ? (float) shadowIdx : -1.0f;
				m_LightData[i].y = lightShadowNDXOrNot;

				// color
				m_LightColors [i] = light.finalColor;

				if (light.lightType == LightType.Point) {
					m_LightData[i].x = LightDefinitions.SPHERE_LIGHT;

					if (light.light.cookie != null)
						m_LightData[i].z = m_CubeCookieTexArray.FetchSlice(cmd, light.light.cookie);

				} else if (light.lightType == LightType.Spot) {
					m_LightData[i].x = LightDefinitions.SPOT_LIGHT;

					float chsa = GetCotanHalfSpotAngle (light.spotAngle);

					// Setup Light Matrix
					m_LightMatrix[i] = SpotlightMatrix (light, worldToLight, range, chsa);

					if (light.light.cookie != null)
						m_LightData[i].z = m_CookieTexArray.FetchSlice (cmd, light.light.cookie);
					else
						m_LightData [i].z = m_CookieTexArray.FetchSlice (cmd, m_DefaultSpotCookie);

				} else if (light.lightType == LightType.Directional) {
					m_LightData[i].x = LightDefinitions.DIRECTIONAL_LIGHT;

					// Setup Light Matrix
					m_LightMatrix[i] = DirectionalLightmatrix (light, worldToLight);

					if (light.light.cookie != null)
						m_LightData[i].z = m_CookieTexArray.FetchSlice (cmd, light.light.cookie);

				}
			}

			int probeCount = cull.visibleReflectionProbes.Count;
			int finalProbeCount = probeCount;
			var lightData = new SFiniteLightData[probeCount];
			int idx = 0;

			// TODO: (cleanup) unify reflection probe setup with deferred
			for (int i = 0; i < probeCount; ++i) {
				var rl = cull.visibleReflectionProbes [i];

				// always a box for now
				var cubemap = rl.texture;
				if (cubemap == null) {
					finalProbeCount--;
					continue;
				}
				var lgtData = new SFiniteLightData();
				lgtData.flags = 0;

				var bnds = rl.bounds;
				var boxOffset = rl.center;                  // reflection volume offset relative to cube map capture point
				var blendDistance = rl.blendDistance;
				var mat = rl.localToWorld;

				var boxProj = (rl.boxProjection != 0);
				var decodeVals = rl.hdr;

				// C is reflection volume center in world space (NOT same as cube map capture point)
				var e = bnds.extents;
				var C = mat.MultiplyPoint(boxOffset);
				var combinedExtent = e + new Vector3(blendDistance, blendDistance, blendDistance);

				Vector3 vx = mat.GetColumn(0);
				Vector3 vy = mat.GetColumn(1);
				Vector3 vz = mat.GetColumn(2);

				// transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
				vx = worldToView.MultiplyVector(vx);
				vy = worldToView.MultiplyVector(vy);
				vz = worldToView.MultiplyVector(vz);

				var Cw = worldToView.MultiplyPoint(C);

				if (boxProj) lgtData.flags |= LightDefinitions.IS_BOX_PROJECTED;

				lgtData.lightPos = Cw;
				lgtData.lightAxisX = vx;
				lgtData.lightAxisY = vy;
				lgtData.lightAxisZ = vz;
				lgtData.localCubeCapturePoint = -boxOffset;
				lgtData.probeBlendDistance = blendDistance;

				lgtData.lightIntensity = decodeVals.x;
				lgtData.decodeExp = decodeVals.y;

				lgtData.sliceIndex = m_CubeReflTexArray.FetchSlice(cmd, cubemap);

				var delta = combinedExtent - e;
				lgtData.boxInnerDist = e;
				lgtData.boxInvRange.Set(1.0f / delta.x, 1.0f / delta.y, 1.0f / delta.z);

				lgtData.lightType = (uint)LightDefinitions.BOX_LIGHT;
				lgtData.lightModel = (uint)LightDefinitions.REFLECTION_LIGHT;

				lightData [idx++] = lgtData;
			}

			s_LightDataBuffer.SetData(lightData);

			cmd.SetGlobalMatrix("g_mViewToWorld", viewToWorld);
			cmd.SetGlobalMatrix("g_mWorldToView", viewToWorld.inverse);
			cmd.SetGlobalMatrix("g_mScrProjection", projscr);
			cmd.SetGlobalMatrix("g_mInvScrProjection", invProjscr);

			cmd.SetGlobalVectorArray("gPerLightData", m_LightData);
			cmd.SetGlobalVectorArray("gLightColor", m_LightColors);
			cmd.SetGlobalVectorArray("gLightDirection", m_LightDirections);
			cmd.SetGlobalVectorArray("gLightPos", m_LightPositions);
			cmd.SetGlobalMatrixArray("gLightMatrix", m_LightMatrix);
			cmd.SetGlobalMatrixArray("gWorldToLightMatrix", m_WorldToLightMatrix);
			cmd.SetGlobalVector("gLightData", new Vector4(totalLightCount, finalProbeCount, 0, 0));

			cmd.SetGlobalTexture("_spotCookieTextures", m_CookieTexArray.GetTexCache());
			cmd.SetGlobalTexture("_pointCookieTextures", m_CubeCookieTexArray.GetTexCache());
			cmd.SetGlobalTexture("_reflCubeTextures", m_CubeReflTexArray.GetTexCache());

			cmd.SetGlobalBuffer("g_vProbeData", s_LightDataBuffer);
			var topCube = ReflectionProbe.defaultTexture;
			var defdecode = ReflectionProbe.defaultTextureHDRDecodeValues;
			cmd.SetGlobalTexture("_reflRootCubeTexture", topCube);
			cmd.SetGlobalFloat("_reflRootHdrDecodeMult", defdecode.x);
			cmd.SetGlobalFloat("_reflRootHdrDecodeExp", defdecode.y);

			cmd.SetGlobalFloat ("_useLegacyCookies", UseLegacyCookies?1.0f:0.0f);
			cmd.SetGlobalFloat ("_transparencyShadows", TransparencyShadows ? 1.0f : 0.0f);

		}
	}
}

