using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class ShadowSetup : IDisposable
{
	// shadow related stuff
	const int k_MaxShadowDataSlots              = 64;
	const int k_MaxPayloadSlotsPerShadowData    =  4;
	ShadowmapBase[]         m_Shadowmaps;
	ShadowManager           m_ShadowMgr;
	static ComputeBuffer    s_ShadowDataBuffer;
	static ComputeBuffer    s_ShadowPayloadBuffer;

	public ShadowSetup(ShadowSettings shadowSettings, out IShadowManager shadowManager)
	{
		s_ShadowDataBuffer = new ComputeBuffer(k_MaxShadowDataSlots, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShadowData)));
		s_ShadowPayloadBuffer = new ComputeBuffer(k_MaxShadowDataSlots * k_MaxPayloadSlotsPerShadowData, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShadowPayload)));
		ShadowAtlas.AtlasInit atlasInit;
		atlasInit.baseInit.width                  = (uint)shadowSettings.shadowAtlasWidth;
		atlasInit.baseInit.height                 = (uint)shadowSettings.shadowAtlasHeight;
		atlasInit.baseInit.slices                 = 1;
		atlasInit.baseInit.shadowmapBits          = 32;
		atlasInit.baseInit.shadowmapFormat        = RenderTextureFormat.Shadowmap;
		atlasInit.baseInit.samplerState           = SamplerState.Default();
		atlasInit.baseInit.comparisonSamplerState = ComparisonSamplerState.Default();
		atlasInit.baseInit.clearColor             = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
		atlasInit.baseInit.maxPayloadCount        = 0;
		atlasInit.baseInit.shadowSupport          = ShadowmapBase.ShadowSupport.Directional | ShadowmapBase.ShadowSupport.Point | ShadowmapBase.ShadowSupport.Spot;
		atlasInit.shaderKeyword                   = null;
		atlasInit.cascadeCount                    = shadowSettings.directionalLightCascadeCount;
		atlasInit.cascadeRatios                   = shadowSettings.directionalLightCascades;

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
		ShadowContext.BindDel binder = (ShadowContext sc, CommandBuffer cb) =>
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
		// set global overrides - these need to match the override specified in ShadowDispatch.hlsl
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

		if( s_ShadowDataBuffer != null )
			s_ShadowDataBuffer.Release();
		if( s_ShadowPayloadBuffer != null )
			s_ShadowPayloadBuffer.Release();
	}
}

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

	[UnityEditor.MenuItem("MobileRenderPipeline/Setup Materials")]
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
		return new ClassicDeferredPipelineInstance(this);
	}
		
	[SerializeField]
	ShadowSettings m_ShadowSettings = ShadowSettings.Default;
	ShadowSetup    m_ShadowSetup;
	IShadowManager m_ShadowMgr;
	FrameId        m_FrameId = new FrameId();

	List<int>               m_ShadowRequests = new List<int>();
	Dictionary<int, int>    m_ShadowIndices = new Dictionary<int,int>();

	void InitShadowSystem(ShadowSettings shadowSettings)
	{
		m_ShadowSetup = new ShadowSetup(shadowSettings, out m_ShadowMgr);
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

	// TODO: define this using LightDefintions.cs
	public static int SPOT_LIGHT = 0;
	public static int SPHERE_LIGHT = 1;
	public static int BOX_LIGHT = 2;
	public static int DIRECTIONAL_LIGHT = 3;

	const int k_MaxLights = 10;
	const int k_MaxShadowmapPerLights = 6;
	const int k_MaxDirectionalSplit = 4;

	Matrix4x4[] m_MatWorldToShadow = new Matrix4x4[k_MaxLights * k_MaxShadowmapPerLights];
	Vector4[] m_DirShadowSplitSpheres = new Vector4[k_MaxDirectionalSplit];
	Vector4[] m_Shadow3X3PCFTerms = new Vector4[4];

	// arrays for shader data
	private Vector4[] m_LightData = new Vector4[k_MaxLights]; // x:Light_type, y:ShadowIndex z:w:UNUSED
	private Vector4[] m_LightPositions = new Vector4[k_MaxLights];
	private Vector4[] m_LightColors = new Vector4[k_MaxLights];
	private Vector4[] m_LightDirections = new Vector4[k_MaxLights];
	private Matrix4x4[] m_LightMatrix = new Matrix4x4[k_MaxLights];
	private Matrix4x4[] m_WorldToLightMatrix = new Matrix4x4[k_MaxLights];

	[NonSerialized]
	private int m_shadowBufferID;

	public Mesh m_PointLightMesh;
	public float PointLightMeshScaleFactor = 2.0f;

	public Mesh m_SpotLightMesh;
	public float SpotLightMeshScaleFactor = 1.0f;

	public Mesh m_QuadMesh;
	public Mesh m_BoxMesh;

	public Texture m_DefaultSpotCookie;

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

		DeinitShadowSystem();
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
					
		//shadows
		m_MatWorldToShadow = new Matrix4x4[k_MaxLights * k_MaxShadowmapPerLights];
		m_DirShadowSplitSpheres = new Vector4[k_MaxDirectionalSplit];
		m_Shadow3X3PCFTerms = new Vector4[4];
		InitShadowSystem(m_ShadowSettings);

		m_shadowBufferID = Shader.PropertyToID("g_tShadowBuffer");
	}

	public void Render(ScriptableRenderContext context, IEnumerable<Camera> cameras)
	{
		foreach (var camera in cameras) {
			// Culling
			CullingParameters cullingParams;
			if (!CullResults.GetCullingParameters (camera, out cullingParams))
				continue;

			m_ShadowMgr.UpdateCullingParameters(ref cullingParams);

			var cullResults = CullResults.Cull (ref cullingParams, context);
			ExecuteRenderLoop(camera, cullResults, context);
		}

		context.Submit ();
	}

	void ExecuteRenderLoop(Camera camera, CullResults cullResults, ScriptableRenderContext loop)
	{
		UpdateShadowConstants (camera, cullResults);

		m_ShadowMgr.RenderShadows( m_FrameId, loop, cullResults, cullResults.visibleLights );
		m_ShadowMgr.SyncData();
		m_ShadowMgr.BindResources( loop );

		loop.SetupCameraProperties(camera);
		RenderGBuffer(cullResults, camera, loop);

		// IF PLATFORM_MAC -- cannot use framebuffer fetch
		#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
		CopyDepthAfterGBuffer(loop);
		#endif
		
		RenderLighting (camera, cullResults, loop);

		loop.DrawSkybox (camera);

		RenderForward (cullResults, camera, loop, false);

		loop.SetupCameraProperties (camera);

		// present frame buffer.
		FinalPass(loop);
	}

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

	void RenderReflections(Camera camera, CommandBuffer cmd, CullResults cullResults, ScriptableRenderContext loop)
	{
		var probes = cullResults.visibleReflectionProbes;
		var worldToView = camera.worldToCameraMatrix; //WorldToCamera(camera);

		float nearDistanceFudged = camera.nearClipPlane * 1.001f;
		float farDistanceFudged = camera.farClipPlane * 0.999f;
		var viewDir = camera.cameraToWorldMatrix.GetColumn(2);
		var viewDirNormalized = -1 * Vector3.Normalize(new Vector3 (viewDir.x, viewDir.y, viewDir.z));

		Plane eyePlane = new Plane ();
		eyePlane.SetNormalAndPosition(viewDirNormalized, camera.transform.position);

		// TODO: need this? --> Set the ambient probe into the SH constants otherwise
		// SetSHConstants(builtins, m_LightprobeContext.ambientProbe);

		// render all probes in reverse order so they are blended into the existing emission buffer with the correct blend settings as follows:
		// emisNew = emis + Lerp( Lerp( Lerp(base,probe0,1-t0), probe1, 1-t1 ), probe2, 1-t2)....
		// DST_COL = DST_COL + DST_ALPHA * SRC_COLOR
		// DST_ALPHA = DST_ALPHA * SRC_ALPHA

		int numProbes = probes.Length;
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

	void UpdateShadowConstants(Camera camera, CullResults inputs)
	{
		m_FrameId.frameCount++;
		// get the indices for all lights that want to have shadows
		m_ShadowRequests.Clear();
		m_ShadowRequests.Capacity = inputs.visibleLights.Length;
		int lcnt = inputs.visibleLights.Length;
		for (int i = 0; i < lcnt; ++i)
		{
			VisibleLight vl = inputs.visibleLights[i];
			if (vl.light.shadows != LightShadows.None) //&& vl.light.GetComponent<AdditionalLightData>().shadowDimmer > 0.0f)
				m_ShadowRequests.Add(i);
		}
		// pass this list to a routine that assigns shadows based on some heuristic
		uint shadowRequestCount = (uint)m_ShadowRequests.Count;
		int[] shadowRequests = m_ShadowRequests.ToArray();
		int[] shadowDataIndices;
		m_ShadowMgr.ProcessShadowRequests(m_FrameId, inputs, camera, inputs.visibleLights,
			ref shadowRequestCount, shadowRequests, out shadowDataIndices);

		// update the visibleLights with the shadow information
		m_ShadowIndices.Clear();
		for (uint i = 0; i < shadowRequestCount; i++)
		{
			m_ShadowIndices.Add(shadowRequests[i], shadowDataIndices[i]);
		}
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
		cmd.SetRenderTarget (new RenderTargetIdentifier (s_GBufferEmission), new RenderTargetIdentifier (s_GBufferZ));
		#endif

		RenderLightsDeferred (camera, inputs, cmd, loop);

		// TODO: UNITY_BRDF_PBS1 writes out alpha 1 to our emission alpha. Should preclear emission alpha after gbuffer pass in case this ever changes
		RenderReflections (camera, cmd, inputs, loop);

		loop.ExecuteCommandBuffer (cmd);
		cmd.Dispose ();
	}

	void RenderSpotlight(VisibleLight light, CommandBuffer cmd, MaterialPropertyBlock properties, bool renderAsQuad, bool intersectsNear, bool deferred)
	{
		float range = light.range;
		var lightToWorld = light.localToWorld;
		var worldToLight = lightToWorld.inverse;

		float chsa = GetCotanHalfSpotAngle (light.spotAngle);

		// Setup Light Matrix
		Matrix4x4 temp1 = Matrix4x4.Scale(new Vector3 (-.5f, -.5f, 1.0f));
		Matrix4x4 temp2 = Matrix4x4.Translate( new Vector3 (.5f, .5f, 0.0f));
		Matrix4x4 temp3 = PerspectiveCotanMatrix (chsa, 0.0f, range);
		var LightMatrix0 = temp2 * temp1 * temp3 * worldToLight;
		properties.SetMatrix ("_LightMatrix0", LightMatrix0);

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
	
	void RenderDirectionalLight(VisibleLight light, CommandBuffer cmd, MaterialPropertyBlock properties, bool renderAsQuad, bool intersectsNear, bool deferred)
	{
		float range = light.range;
		var lightToWorld = light.localToWorld;
		var worldToLight = lightToWorld.inverse;

		// Setup Light Matrix
		float scale = 1.0f / light.light.cookieSize;
		Matrix4x4 temp1 = Matrix4x4.Scale(new Vector3 (scale, scale, 0.0f));
		Matrix4x4 temp2 = Matrix4x4.Translate( new Vector3 (.5f, .5f, 0.0f));
		var LightMatrix0 = temp2 * temp1 * worldToLight;
		properties.SetMatrix ("_LightMatrix0", LightMatrix0);

		Texture cookie = light.light.cookie;
		if (cookie != null) {
			cmd.EnableShaderKeyword ("DIRECTIONAL_COOKIE");
		} else
			cmd.EnableShaderKeyword ("DIRECTIONAL");

		cmd.DrawMesh (m_QuadMesh, Matrix4x4.identity, m_DirectionalDeferredLightingMaterial, 0, 0, properties);
	}

	void RenderLightsDeferred (Camera camera, CullResults inputs, CommandBuffer cmd, ScriptableRenderContext loop)
	{
		int lightCount = inputs.visibleLights.Length;
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
				RenderDirectionalLight(light, cmd, props, renderAsQuad, intersectsNear, true);
				break;
			}
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
		const float degToRad = (float)(Mathf.PI / 180.0);
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
		
	void RenderForward(CullResults cull, Camera camera, ScriptableRenderContext loop, bool opaquesOnly)
	{

		SetupLightShaderVariables (cull, camera, loop);

		var settings = new DrawRendererSettings(cull, camera, new ShaderPassName("ForwardSinglePass"))
		{
			sorting = { flags = SortFlags.CommonOpaque }
		};
		settings.rendererConfiguration = RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe;
		if (opaquesOnly) settings.inputFilter.SetQueuesOpaque();
		else settings.inputFilter.SetQueuesTransparent();

		loop.DrawRenderers(ref settings);
	}

	static Matrix4x4 CameraToWorld(Camera camera)
	{
		return camera.cameraToWorldMatrix * GetFlipMatrix();
	}

	static Matrix4x4 CameraProjection(Camera camera)
	{
		return camera.projectionMatrix * GetFlipMatrix();
	}

	private void InitializeLightData()
	{
		for (int i = 0; i < k_MaxLights; ++i)
		{
			m_LightData [i] = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
			m_LightColors[i] = Vector4.zero;
			m_LightDirections[i] = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
			m_LightPositions[i] = Vector4.zero;
			m_LightMatrix[i] = Matrix4x4.identity;
			m_WorldToLightMatrix[i] = Matrix4x4.identity;
		}
	}

	private void SetupLightShaderVariables(CullResults cull, Camera camera, ScriptableRenderContext context)
	{
		int totalLightCount = cull.visibleLights.Length;
		InitializeLightData();

		var w = camera.pixelWidth;
		var h = camera.pixelHeight;
		Matrix4x4 viewToWorld = CameraToWorld (camera);

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
				m_LightData[i].x = SPHERE_LIGHT;
				//RenderPointLight (light, cmd, props, renderAsQuad, intersectsNear, true);

			} else if (light.lightType == LightType.Spot) {
				m_LightData[i].x = SPOT_LIGHT;

				float chsa = GetCotanHalfSpotAngle (light.spotAngle);

				// Setup Light Matrix
				Matrix4x4 temp1 = Matrix4x4.Scale(new Vector3 (-.5f, -.5f, 1.0f));
				Matrix4x4 temp2 = Matrix4x4.Translate( new Vector3 (.5f, .5f, 0.0f));
				Matrix4x4 temp3 = PerspectiveCotanMatrix (chsa, 0.0f, range);
				m_LightMatrix[i] = temp2 * temp1 * temp3 * worldToLight;

			} else if (light.lightType == LightType.Directional) {
				m_LightData[i].x = DIRECTIONAL_LIGHT;

				// Setup Light Matrix
				float scale = 1.0f / light.light.cookieSize;
				Matrix4x4 temp1 = Matrix4x4.Scale(new Vector3 (scale, scale, 0.0f));
				Matrix4x4 temp2 = Matrix4x4.Translate( new Vector3 (.5f, .5f, 0.0f));
				m_LightMatrix[i] = temp2 * temp1 * worldToLight;

			}
		}

		CommandBuffer cmd = new CommandBuffer() {name = "SetupShaderConstants"};

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
		cmd.SetGlobalVector("gLightData", new Vector4(totalLightCount, 0, 0, 0));

		context.ExecuteCommandBuffer(cmd);
		cmd.Dispose();
	}
}


