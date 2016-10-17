using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.ScriptableRenderLoop
{
    [ExecuteInEditMode]
    public class FptlLighting : ScriptableRenderLoop
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Renderloop/CreateRenderLoopFPTL")]
        static void CreateRenderLoopFPTL()
        {
            var instance = ScriptableObject.CreateInstance<FptlLighting>();
            UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/renderloopfptl.asset");
            //AssetDatabase.CreateAsset(instance, "Assets/ScriptableRenderLoop/fptl/renderloopfptl.asset");
        }

#endif

        [SerializeField]
        ShadowSettings m_ShadowSettings = ShadowSettings.Default;
        ShadowRenderPass m_ShadowPass;

        [SerializeField]
        TextureSettings m_TextureSettings = TextureSettings.Default;

        public Shader m_DeferredShader;
        public Shader m_DeferredReflectionShader;
        public Shader m_FinalPassShader;

        public ComputeShader m_BuildScreenAABBShader;
        public ComputeShader m_BuildPerTileLightListShader;     // FPTL

        public ComputeShader m_BuildPerVoxelLightListShader;    // clustered

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
        static private int kGenListPerVoxelKernel;
        static private int kClearVoxelAtomicKernel;
        static private ComputeBuffer m_lightDataBuffer;
        static private ComputeBuffer m_convexBoundsBuffer;
        static private ComputeBuffer m_aabbBoundsBuffer;
        static private ComputeBuffer lightList;
        static private ComputeBuffer m_dirLightList;

        // clustered light list specific buffers and data begin
        public bool EnableClustered = false;
        const bool gUseDepthBuffer = true;//      // only has an impact when EnableClustered is true (requires a depth-prepass)
        const int g_iLog2NumClusters = 6;     // accepted range is from 0 to 6. NumClusters is 1<<g_iLog2NumClusters
        const float m_clustLogBase = 1.02f;     // each slice 2% bigger than the previous
        float m_clustScale;
        static private ComputeBuffer m_perVoxelLightLists;
        static private ComputeBuffer m_perVoxelOffset;
        static private ComputeBuffer m_perTileLogBaseTweak;
        static private ComputeBuffer m_globalLightListAtomic;
        // clustered light list specific buffers and data end

        static private int m_WidthOnRecord;
        static private int m_HeightOnRecord;

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

            ReleaseResolutionDependentBuffers();

            if (m_dirLightList != null)
                m_dirLightList.Release();

            if (EnableClustered)
            {
                if (m_globalLightListAtomic != null)
                    m_globalLightListAtomic.Release();
            }
        }

        public override void Rebuild()
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

            m_BuildScreenAABBShader.SetBuffer(kGenAABBKernel, "g_data", m_convexBoundsBuffer);
            //m_BuildScreenAABBShader.SetBuffer(kGenAABBKernel, "g_vBoundsBuffer", m_aabbBoundsBuffer);
            m_DeferredMaterial.SetBuffer("g_vLightData", m_lightDataBuffer);
            m_DeferredMaterial.SetBuffer("g_dirLightData", m_dirLightList);
            m_DeferredReflectionMaterial.SetBuffer("g_vLightData", m_lightDataBuffer);

            m_BuildPerTileLightListShader.SetBuffer(kGenListPerTileKernel, "g_vBoundsBuffer", m_aabbBoundsBuffer);
            m_BuildPerTileLightListShader.SetBuffer(kGenListPerTileKernel, "g_vLightData", m_lightDataBuffer);
            m_BuildPerTileLightListShader.SetBuffer(kGenListPerTileKernel, "g_data", m_convexBoundsBuffer);

            if (EnableClustered)
            {
                kGenListPerVoxelKernel = m_BuildPerVoxelLightListShader.FindKernel(gUseDepthBuffer ? "TileLightListGen_DepthRT" : "TileLightListGen_NoDepthRT");
                kClearVoxelAtomicKernel = m_BuildPerVoxelLightListShader.FindKernel("ClearAtomic");
                m_BuildPerVoxelLightListShader.SetBuffer(kGenListPerVoxelKernel, "g_vBoundsBuffer", m_aabbBoundsBuffer);
                m_BuildPerVoxelLightListShader.SetBuffer(kGenListPerVoxelKernel, "g_vLightData", m_lightDataBuffer);
                m_BuildPerVoxelLightListShader.SetBuffer(kGenListPerVoxelKernel, "g_data", m_convexBoundsBuffer);

                m_globalLightListAtomic = new ComputeBuffer(1, sizeof(uint));
            }

            m_cookieTexArray = new TextureCache2D();
            m_cubeCookieTexArray = new TextureCacheCubemap();
            m_cubeReflTexArray = new TextureCacheCubemap();
            m_cookieTexArray.AllocTextureArray(8, (int)m_TextureSettings.spotCookieSize, (int)m_TextureSettings.spotCookieSize, TextureFormat.RGBA32, true);
            m_cubeCookieTexArray.AllocTextureArray(4, (int)m_TextureSettings.pointCookieSize, TextureFormat.RGBA32, true);
            m_cubeReflTexArray.AllocTextureArray(64, (int)m_TextureSettings.reflectionCubemapSize, TextureFormat.BC6H, true);

            //m_DeferredMaterial.SetTexture("_spotCookieTextures", m_cookieTexArray.GetTexCache());
            //m_DeferredMaterial.SetTexture("_pointCookieTextures", m_cubeCookieTexArray.GetTexCache());
            //m_DeferredReflectionMaterial.SetTexture("_reflCubeTextures", m_cubeReflTexArray.GetTexCache());

            g_matWorldToShadow = new Matrix4x4[MAX_LIGHTS * MAX_SHADOWMAP_PER_LIGHTS];
            g_vDirShadowSplitSpheres = new Vector4[MAX_DIRECTIONAL_SPLIT];
            g_vShadow3x3PCFTerms = new Vector4[4];
            m_ShadowPass = new ShadowRenderPass(m_ShadowSettings);

            m_skyboxHelper = new SkyboxHelper();
            m_skyboxHelper.CreateMesh();

            m_blitMaterial = new Material(m_FinalPassShader);
            m_blitMaterial.hideFlags = HideFlags.HideAndDontSave;

            lightList = null;
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
            ReleaseResolutionDependentBuffers();
            m_dirLightList.Release();

            if (EnableClustered)
            {
                m_globalLightListAtomic.Release();
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
            cmd.GetTemporaryRT(kGBufferAlbedo, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            cmd.GetTemporaryRT(kGBufferSpecRough, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            cmd.GetTemporaryRT(kGBufferNormal, width, height, 0, FilterMode.Point, format10, RenderTextureReadWrite.Linear);
            cmd.GetTemporaryRT(kGBufferEmission, width, height, 0, FilterMode.Point, formatHDR, RenderTextureReadWrite.Linear);
            cmd.GetTemporaryRT(kGBufferZ, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth);
            cmd.GetTemporaryRT(kCameraDepthTexture, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth);

            cmd.GetTemporaryRT(kCameraTarget, width, height, 0, FilterMode.Point, formatHDR, RenderTextureReadWrite.Default);

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
            SetupGBuffer(camera.pixelWidth, camera.pixelHeight, cmd);
            loop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            // render opaque objects using Deferred pass
            DrawRendererSettings settings = new DrawRendererSettings(cull, camera, new ShaderPassName("Deferred"));
            settings.sorting.sortOptions = SortOptions.SortByMaterialThenMesh;
            settings.rendererConfiguration = RendererConfiguration.PerObjectLightmaps; //@TODO: need to get light probes + LPPV too?
            settings.inputCullingOptions.SetQueuesOpaque();
            loop.DrawRenderers(ref settings);
        }

        static void RenderForward(CullResults cull, Camera camera, RenderLoop loop, bool opaquesOnly)
        {
            var cmd = new CommandBuffer();
            cmd.name = opaquesOnly ? "Prep Opaques Only Forward Pass" : "Prep Forward Pass";

            // using these two lines will require a depth pre-pass for forward opaques which we don't have currently at least
            //cmd.SetGlobalFloat("g_isOpaquesOnlyEnabled", opaquesOnly ? 1 : 0);
            //cmd.SetGlobalBuffer("g_vLightListGlobal", opaquesOnly ? lightList : m_perVoxelLightLists);

            cmd.SetGlobalFloat("g_isOpaquesOnlyEnabled", 0);
            cmd.SetGlobalBuffer("g_vLightListGlobal", m_perVoxelLightLists);
            loop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            // render opaque objects using Deferred pass
            DrawRendererSettings settings = new DrawRendererSettings(cull, camera, new ShaderPassName("ForwardSinglePass"));
            //settings.rendererConfiguration = RendererConfiguration.PerObjectLightProbe | RendererConfiguration.PerObjectReflectionProbes;
            settings.sorting.sortOptions = SortOptions.SortByMaterialThenMesh;
            if (opaquesOnly) settings.inputCullingOptions.SetQueuesOpaque();
            loop.DrawRenderers(ref settings);
        }

        static void CopyDepthAfterGBuffer(RenderLoop loop)
        {
            var cmd = new CommandBuffer();
            cmd.name = "Copy depth";
            cmd.CopyTexture(new RenderTargetIdentifier(kGBufferZ), new RenderTargetIdentifier(kCameraDepthTexture));
            loop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        void DoTiledDeferredLighting(Camera camera, RenderLoop loop)
        {
            bool bUseClusteredForDeferred = false && EnableClustered;       // doesn't work on reflections yet but will soon
            var cmd = new CommandBuffer();

            m_DeferredMaterial.EnableKeyword(bUseClusteredForDeferred ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
            m_DeferredReflectionMaterial.EnableKeyword(bUseClusteredForDeferred ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");

            cmd.SetGlobalBuffer("g_vLightListGlobal", bUseClusteredForDeferred ? m_perVoxelLightLists : lightList);       // opaques list (unless MSAA possibly)

            // In case of bUseClusteredForDeferred disable toggle option since we're using m_perVoxelLightLists as opposed to lightList
            if (bUseClusteredForDeferred) cmd.SetGlobalFloat("g_isOpaquesOnlyEnabled", 0);

            cmd.name = "DoTiledDeferredLighting";

            //cmd.SetRenderTarget(new RenderTargetIdentifier(kGBufferEmission), new RenderTargetIdentifier(kGBufferZ));


            //cmd.Blit (kGBufferNormal, (RenderTexture)null); // debug: display normals

            cmd.Blit(0, kCameraTarget, m_DeferredMaterial, 0);
            cmd.Blit(0, kCameraTarget, m_DeferredReflectionMaterial, 0);

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

        int UpdateDirectionalLights(Camera camera, VisibleLight[] visibleLights)
        {
            int dirLightCount = 0;
            List<DirectionalLight> lights = new List<DirectionalLight>();
            Matrix4x4 worldToView = camera.worldToCameraMatrix;

            for (int nLight = 0; nLight < visibleLights.Length; nLight++)
            {
                VisibleLight light = visibleLights[nLight];
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

                    l.uShadowLightIndex = (light.light.shadows != LightShadows.None) ? (uint)nLight : 0xffffffff;

                    l.vLaxisX = vx;
                    l.vLaxisY = vy;
                    l.vLaxisZ = vz;

                    l.vCol.Set(light.finalColor.r, light.finalColor.g, light.finalColor.b);
                    l.fLightIntensity = light.light.intensity;

                    lights.Add(l);
                    dirLightCount++;
                }
            }
            m_dirLightList.SetData(lights.ToArray());

            return dirLightCount;
        }

        void UpdateShadowConstants(VisibleLight[] visibleLights, ref ShadowOutput shadow)
        {
            int nNumLightsIncludingTooMany = 0;

            int g_nNumLights = 0;

            Vector4[] g_vLightShadowIndex_vLightParams = new Vector4[MAX_LIGHTS];
            Vector4[] g_vLightFalloffParams = new Vector4[MAX_LIGHTS];

            for (int nLight = 0; nLight < visibleLights.Length; nLight++)
            {
                nNumLightsIncludingTooMany++;
                if (nNumLightsIncludingTooMany > MAX_LIGHTS)
                    continue;

                VisibleLight light = visibleLights[nLight];
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
            VisibleReflectionProbe[] probes = inputs.visibleReflectionProbes;
            //ReflectionProbe[] probes = Object.FindObjectsOfType<ReflectionProbe>();

            int nrModels = (int)LightDefinitions.NR_LIGHT_MODELS;
            int nrVolTypes = (int)LightDefinitions.MAX_TYPES;
            int[,] numEntries = new int[nrModels,nrVolTypes];
            int[,] offsets = new int[nrModels,nrVolTypes];
            int[,] numEntries2nd = new int[nrModels,nrVolTypes];

            // first pass. Figure out how much we have of each and establish offsets
            foreach (var cl in inputs.visibleLights)
            {
                int volType = cl.lightType==LightType.Spot ? LightDefinitions.SPOT_LIGHT : (cl.lightType==LightType.Point ? LightDefinitions.SPHERE_LIGHT : -1);
                if(volType>=0) ++numEntries[LightDefinitions.DIRECT_LIGHT,volType];
            }

            foreach (var rl in probes)
            {
                int volType = LightDefinitions.BOX_LIGHT;       // always a box for now
                if(rl.texture!=null) ++numEntries[LightDefinitions.REFLECTION_LIGHT,volType];
            }

            // add decals here too similar to the above

            // establish offsets
            for(int m=0; m<nrModels; m++)
            {
                offsets[m,0] = m==0 ? 0 : (numEntries[m-1,nrVolTypes-1] + offsets[m-1,nrVolTypes-1]);
                for(int v=1; v<nrVolTypes; v++) offsets[m,v] = numEntries[m,v-1]+offsets[m,v-1];
            }


            int numLights = inputs.visibleLights.Length;
            int numProbes = probes.Length;
            int numVolumes = numLights + numProbes;


            SFiniteLightData[] lightData = new SFiniteLightData[numVolumes];
            SFiniteLightBound[] boundData = new SFiniteLightBound[numVolumes];
            Matrix4x4 worldToView = camera.worldToCameraMatrix;

            uint shadowLightIndex = 0;
            foreach (var cl in inputs.visibleLights)
            {
                float range = cl.range;

                Matrix4x4 lightToWorld = cl.localToWorld;
                //Matrix4x4 worldToLight = l.worldToLocal;

                Vector3 lightPos = lightToWorld.GetColumn(3);

                SFiniteLightBound bndData = new SFiniteLightBound();
                SFiniteLightData lgtData = new SFiniteLightData();

                bndData.vBoxAxisX.Set(1, 0, 0);
                bndData.vBoxAxisY.Set(0, 1, 0);
                bndData.vBoxAxisZ.Set(0, 0, 1);
                bndData.vScaleXY.Set(1.0f, 1.0f);
                bndData.fRadius = range;

                lgtData.flags = 0;
                lgtData.fRecipRange = 1.0f / range;
                lgtData.vCol.Set(cl.finalColor.r, cl.finalColor.g, cl.finalColor.b);
                lgtData.iSliceIndex = 0;
                lgtData.uLightModel = (uint)LightDefinitions.DIRECT_LIGHT;
                lgtData.uShadowLightIndex = shadowLightIndex;
                shadowLightIndex++;

                bool bHasCookie = cl.light.cookie != null;
                bool bHasShadow = cl.light.shadows != LightShadows.None;

                int idxOut = 0;

                if (cl.lightType == LightType.Spot)
                {
                    bool bIsCircularSpot = !bHasCookie;
                    if (!bIsCircularSpot)    // square spots always have cookie
                    {
                        lgtData.iSliceIndex = m_cookieTexArray.FetchSlice(cl.light.cookie);
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


                    //float sa = cl.GetSpotAngle();     // total field of view from left to right side
                    float sa = radToDeg * (2 * Mathf.Acos(1.0f / cl.invCosHalfSpotAngle));       // spot angle doesn't exist in the structure so reversing it for now.


                    float cs = Mathf.Cos(0.5f * sa * degToRad);
                    float si = Mathf.Sin(0.5f * sa * degToRad);
                    float ta = cs > 0.0f ? (si / cs) : gFltMax;

                    float cota = si > 0.0f ? (cs / si) : gFltMax;

                    //const float cotasa = l.GetCotanHalfSpotAngle();

                    // apply nonuniform scale to OBB of spot light
                    bool bSqueeze = true;//sa < 0.7f * 90.0f;      // arb heuristic
                    float fS = bSqueeze ? ta : si;
                    bndData.vCen = worldToView.MultiplyPoint(lightPos + ((0.5f * range) * lightDir));    // use mid point of the spot as the center of the bounding volume for building screen-space AABB for tiled lighting.

                    lgtData.vLaxisX = vx;
                    lgtData.vLaxisY = vy;
                    lgtData.vLaxisZ = vz;

                    // scale axis to match box or base of pyramid
                    bndData.vBoxAxisX = (fS * range) * vx;
                    bndData.vBoxAxisY = (fS * range) * vy;
                    bndData.vBoxAxisZ = (0.5f * range) * vz;

                    // generate bounding sphere radius
                    float fAltDx = si;
                    float fAltDy = cs;
                    fAltDy = fAltDy - 0.5f;
                    //if(fAltDy<0) fAltDy=-fAltDy;

                    fAltDx *= range; fAltDy *= range;

                    float fAltDist = Mathf.Sqrt(fAltDy * fAltDy + (bIsCircularSpot ? 1.0f : 2.0f) * fAltDx * fAltDx);
                    bndData.fRadius = fAltDist > (0.5f * range) ? fAltDist : (0.5f * range);       // will always pick fAltDist
                    bndData.vScaleXY = bSqueeze ? new Vector2(0.01f, 0.01f) : new Vector2(1.0f, 1.0f);

                    // fill up ldata
                    lgtData.uLightType = (uint)LightDefinitions.SPOT_LIGHT;
                    lgtData.vLpos = worldToView.MultiplyPoint(lightPos);
                    lgtData.fSphRadiusSq = range * range;
                    lgtData.fPenumbra = cs;
                    lgtData.cotan = cota;
                    lgtData.flags |= (bIsCircularSpot ? LightDefinitions.IS_CIRCULAR_SPOT_SHAPE : 0);

                    lgtData.flags |= (bHasCookie ? LightDefinitions.HAS_COOKIE_TEXTURE : 0);
                    lgtData.flags |= (bHasShadow ? LightDefinitions.HAS_SHADOW : 0);

                    int i = LightDefinitions.DIRECT_LIGHT, j = LightDefinitions.SPOT_LIGHT;
                    idxOut = numEntries2nd[i,j] + offsets[i,j]; ++numEntries2nd[i,j];
                }
                else if (cl.lightType == LightType.Point)
                {
                    if (bHasCookie)
                    {
                        lgtData.iSliceIndex = m_cubeCookieTexArray.FetchSlice(cl.light.cookie);
                    }

                    bndData.vCen = worldToView.MultiplyPoint(lightPos);
                    bndData.vBoxAxisX.Set(range, 0, 0);
                    bndData.vBoxAxisY.Set(0, range, 0);
                    bndData.vBoxAxisZ.Set(0, 0, -range);    // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                    bndData.vScaleXY.Set(1.0f, 1.0f);
                    bndData.fRadius = range;

                    // represents a left hand coordinate system in world space since det(worldToView)<0
                    Matrix4x4 lightToView = worldToView * lightToWorld;
                    Vector3 vx = lightToView.GetColumn(0);
                    Vector3 vy = lightToView.GetColumn(1);
                    Vector3 vz = lightToView.GetColumn(2);

                    // fill up ldata
                    lgtData.uLightType = (uint)LightDefinitions.SPHERE_LIGHT;
                    lgtData.vLpos = bndData.vCen;
                    lgtData.fSphRadiusSq = range * range;

                    lgtData.vLaxisX = vx;
                    lgtData.vLaxisY = vy;
                    lgtData.vLaxisZ = vz;

                    lgtData.flags |= (bHasCookie ? LightDefinitions.HAS_COOKIE_TEXTURE : 0);
                    lgtData.flags |= (bHasShadow ? LightDefinitions.HAS_SHADOW : 0);

                    int i = LightDefinitions.DIRECT_LIGHT, j = LightDefinitions.SPHERE_LIGHT;
                    idxOut = numEntries2nd[i,j] + offsets[i,j]; ++numEntries2nd[i,j];
                }
                else
                {
                    //Assert(false);
                }

                // next light
                if (cl.lightType == LightType.Spot || cl.lightType == LightType.Point)
                {
                    boundData[idxOut] = bndData;
                    lightData[idxOut] = lgtData;
                }
            }
            int numLightsOut = offsets[LightDefinitions.DIRECT_LIGHT, nrVolTypes-1] + numEntries[LightDefinitions.DIRECT_LIGHT, nrVolTypes-1];
            
            // probe.m_BlendDistance
            // Vector3f extents = 0.5*Abs(probe.m_BoxSize);
            // C center of rendered refl box <-- GetComponent (Transform).GetPosition() + m_BoxOffset;
            // cube map capture point: GetComponent (Transform).GetPosition()
            // shader parameter min and max are C+/-(extents+blendDistance)
            foreach (var rl in probes)
            {
                Texture cubemap = rl.texture;
                if (cubemap != null)        // always a box for now
                {
                    SFiniteLightBound bndData = new SFiniteLightBound();
                    SFiniteLightData lgtData = new SFiniteLightData();

                    int idxOut = 0;
                    lgtData.flags = 0;

                    Bounds bnds = rl.bounds;
                    Vector3 boxOffset = rl.center;                  // reflection volume offset relative to cube map capture point
                    float blendDistance = rl.blendDistance;
                    float imp = rl.importance;

                    Matrix4x4 mat = rl.localToWorld;
                    //Matrix4x4 mat = rl.transform.localToWorldMatrix;
                    Vector3 cubeCapturePos = mat.GetColumn(3);      // cube map capture position in world space


                    // implicit in CalculateHDRDecodeValues() --> float ints = rl.intensity;
                    bool boxProj = (rl.boxProjection != 0);
                    Vector4 decodeVals = rl.hdr;
                    //Vector4 decodeVals = rl.CalculateHDRDecodeValues();

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

                    if (boxProj) lgtData.flags |= LightDefinitions.IS_BOX_PROJECTED;

                    lgtData.vLpos = Cw;
                    lgtData.vLaxisX = vx;
                    lgtData.vLaxisY = vy;
                    lgtData.vLaxisZ = vz;
                    lgtData.vLocalCubeCapturePoint = -boxOffset;
                    lgtData.fProbeBlendDistance = blendDistance;

                    lgtData.fLightIntensity = decodeVals.x;
                    lgtData.fDecodeExp = decodeVals.y;

                    lgtData.iSliceIndex = m_cubeReflTexArray.FetchSlice(cubemap);

                    Vector3 delta = combinedExtent - e;
                    lgtData.vBoxInnerDist = e;
                    lgtData.vBoxInvRange.Set(1.0f / delta.x, 1.0f / delta.y, 1.0f / delta.z);

                    bndData.vCen = Cw;
                    bndData.vBoxAxisX = combinedExtent.x * vx;
                    bndData.vBoxAxisY = combinedExtent.y * vy;
                    bndData.vBoxAxisZ = combinedExtent.z * vz;
                    bndData.vScaleXY.Set(1.0f, 1.0f);
                    bndData.fRadius = combinedExtent.magnitude;

                    // fill up ldata
                    lgtData.uLightType = (uint)LightDefinitions.BOX_LIGHT;
                    lgtData.uLightModel = (uint)LightDefinitions.REFLECTION_LIGHT;


                    int i = LightDefinitions.REFLECTION_LIGHT, j = LightDefinitions.BOX_LIGHT;
                    idxOut = numEntries2nd[i,j] + offsets[i,j]; ++numEntries2nd[i,j];
                    boundData[idxOut] = bndData;
                    lightData[idxOut] = lgtData;
                }
            }
            int numProbesOut = offsets[LightDefinitions.REFLECTION_LIGHT, nrVolTypes-1] + numEntries[LightDefinitions.REFLECTION_LIGHT, nrVolTypes-1];
            for(int m=0; m<nrModels; m++)
            {
                for(int v=0; v<nrVolTypes; v++)
                    Debug.Assert(numEntries[m,v]==numEntries2nd[m, v], "count mismatch on second pass!");
            }

            m_convexBoundsBuffer.SetData(boundData);
            m_lightDataBuffer.SetData(lightData);


            return numLightsOut + numProbesOut;
        }

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

            renderLoop.Submit();
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
            int iW = camera.pixelWidth;
            int iH = camera.pixelHeight;

            ResizeIfNecessary(iW, iH);

            // do anything we need to do upon a new frame.
            NewFrame ();

            ShadowOutput shadows;
            m_ShadowPass.Render(loop, cullResults, out shadows);

            //m_DeferredMaterial.SetInt("_SrcBlend", camera.hdr ? (int)BlendMode.One : (int)BlendMode.DstColor);
            //m_DeferredMaterial.SetInt("_DstBlend", camera.hdr ? (int)BlendMode.One : (int)BlendMode.Zero);
            //m_DeferredReflectionMaterial.SetInt("_SrcBlend", camera.hdr ? (int)BlendMode.One : (int)BlendMode.DstColor);
            //m_DeferredReflectionMaterial.SetInt("_DstBlend", camera.hdr ? (int)BlendMode.One : (int)BlendMode.Zero);
            loop.SetupCameraProperties(camera);

            UpdateShadowConstants (cullResults.visibleLights, ref shadows);

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
            cmd.DispatchCompute(m_BuildScreenAABBShader, kGenAABBKernel, (numLights + 7) / 8, 1, 1);

            cmd.SetComputeIntParams(m_BuildPerTileLightListShader, "g_viDimensions", new int[2] { iW, iH });
            cmd.SetComputeIntParam(m_BuildPerTileLightListShader, "g_iNrVisibLights", numLights);
            SetMatrixCS(cmd, m_BuildPerTileLightListShader, "g_mScrProjection", projscr);
            SetMatrixCS(cmd, m_BuildPerTileLightListShader, "g_mInvScrProjection", invProjscr);
            cmd.SetComputeTextureParam(m_BuildPerTileLightListShader, kGenListPerTileKernel, "g_depth_tex", new RenderTargetIdentifier(kCameraDepthTexture));
            cmd.SetComputeBufferParam(m_BuildPerTileLightListShader, kGenListPerTileKernel, "g_vLightList", lightList);
            cmd.DispatchCompute(m_BuildPerTileLightListShader, kGenListPerTileKernel, nrTilesX, nrTilesY, 1);

            if (EnableClustered) VoxelLightListGeneration(cmd, camera, numLights, projscr, invProjscr);


            loop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            int numDirLights = UpdateDirectionalLights(camera, cullResults.visibleLights);

            // Push all global params
            PushGlobalParams(camera, loop, camera.cameraToWorldMatrix, projscr, invProjscr, numDirLights);

            // do deferred lighting
            DoTiledDeferredLighting(camera, loop);

            // don't have a depth pre-pass for forward lit meshes so have to require clustered for now
            if (EnableClustered) RenderForward(cullResults, camera, loop, false);


            m_skyboxHelper.Draw(loop, camera);

            FinalPass(loop);
        }

        void NewFrame()
        {
            // update texture caches
            m_cookieTexArray.NewFrame();
            m_cubeCookieTexArray.NewFrame();
            m_cubeReflTexArray.NewFrame();

            //m_DeferredMaterial.SetTexture("_spotCookieTextures", m_cookieTexArray.GetTexCache());
            //m_DeferredMaterial.SetTexture("_pointCookieTextures", m_cubeCookieTexArray.GetTexCache());
            //m_DeferredReflectionMaterial.SetTexture("_reflCubeTextures", m_cubeReflTexArray.GetTexCache());
        }

        void ResizeIfNecessary(int curWidth, int curHeight)
        {
            if (curWidth != m_WidthOnRecord || curHeight != m_HeightOnRecord || lightList == null)
            {
                if (m_WidthOnRecord > 0 && m_HeightOnRecord > 0)
                    ReleaseResolutionDependentBuffers();

                AllocResolutionDependentBuffers(curWidth, curHeight);

                // update recorded window resolution
                m_WidthOnRecord = curWidth;
                m_HeightOnRecord = curHeight;
            }
        }

        void ReleaseResolutionDependentBuffers()
        {
            if (lightList != null)
                lightList.Release();

            if (EnableClustered)
            {
                if (m_perVoxelLightLists != null)
                    m_perVoxelLightLists.Release();

                if (m_perVoxelOffset != null)
                    m_perVoxelOffset.Release();

                if (gUseDepthBuffer && m_perTileLogBaseTweak != null)
                    m_perTileLogBaseTweak.Release();
            }
        }

        int NumLightIndicesPerClusteredTile()
        {
            return 4 * (1 << g_iLog2NumClusters);       // total footprint for all layers of the tile (measured in light index entries)
        }

        void AllocResolutionDependentBuffers(int width, int height)
        {
            int nrTilesX = (width + 15) / 16;
            int nrTilesY = (height + 15) / 16;
            int nrTiles = nrTilesX * nrTilesY;
            const int capacityUShortsPerTileFPTL = 32;
            const int nrDWordsPerTileFPTL = (capacityUShortsPerTileFPTL + 1) >> 1;        // room for 31 lights and a nrLights value.

            lightList = new ComputeBuffer(LightDefinitions.NR_LIGHT_MODELS * nrDWordsPerTileFPTL * nrTiles, sizeof(uint));       // enough list memory for a 4k x 4k display

            if (EnableClustered)
            {
                m_perVoxelOffset = new ComputeBuffer(LightDefinitions.NR_LIGHT_MODELS * (1 << g_iLog2NumClusters) * nrTiles, sizeof(uint));
                m_perVoxelLightLists = new ComputeBuffer(NumLightIndicesPerClusteredTile() * nrTiles, sizeof(uint));

                if (gUseDepthBuffer) m_perTileLogBaseTweak = new ComputeBuffer(nrTiles, sizeof(float));
            }
        }

        void VoxelLightListGeneration(CommandBuffer cmd, Camera camera, int numLights, Matrix4x4 projscr, Matrix4x4 invProjscr)
        {
            // clear atomic offset index
            cmd.SetComputeBufferParam(m_BuildPerVoxelLightListShader, kClearVoxelAtomicKernel, "g_LayeredSingleIdxBuffer", m_globalLightListAtomic);
            cmd.DispatchCompute(m_BuildPerVoxelLightListShader, kClearVoxelAtomicKernel, 1, 1, 1);

            cmd.SetComputeIntParam(m_BuildPerVoxelLightListShader, "g_iNrVisibLights", numLights);
            SetMatrixCS(cmd, m_BuildPerVoxelLightListShader, "g_mScrProjection", projscr);
            SetMatrixCS(cmd, m_BuildPerVoxelLightListShader, "g_mInvScrProjection", invProjscr);

            cmd.SetComputeIntParam(m_BuildPerVoxelLightListShader, "g_iLog2NumClusters", g_iLog2NumClusters);

            //Vector4 v2_near = invProjscr * new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            //Vector4 v2_far = invProjscr * new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
            //float nearPlane2 = -(v2_near.z/v2_near.w);
            //float farPlane2 = -(v2_far.z/v2_far.w);
            float nearPlane = camera.nearClipPlane;
            float farPlane = camera.farClipPlane;
            cmd.SetComputeFloatParam(m_BuildPerVoxelLightListShader, "g_fNearPlane", nearPlane);
            cmd.SetComputeFloatParam(m_BuildPerVoxelLightListShader, "g_fFarPlane", farPlane);

            float C = (float)(1 << g_iLog2NumClusters);
            double geomSeries = (1.0 - Mathf.Pow(m_clustLogBase, C)) / (1 - m_clustLogBase);        // geometric series: sum_k=0^{C-1} base^k
            m_clustScale = (float)(geomSeries / (farPlane - nearPlane));

            cmd.SetComputeFloatParam(m_BuildPerVoxelLightListShader, "g_fClustScale", m_clustScale);
            cmd.SetComputeFloatParam(m_BuildPerVoxelLightListShader, "g_fClustBase", m_clustLogBase);

            cmd.SetComputeTextureParam(m_BuildPerVoxelLightListShader, kGenListPerVoxelKernel, "g_depth_tex", new RenderTargetIdentifier(kCameraDepthTexture));
            cmd.SetComputeBufferParam(m_BuildPerVoxelLightListShader, kGenListPerVoxelKernel, "g_vLayeredLightList", m_perVoxelLightLists);
            cmd.SetComputeBufferParam(m_BuildPerVoxelLightListShader, kGenListPerVoxelKernel, "g_LayeredOffset", m_perVoxelOffset);
            cmd.SetComputeBufferParam(m_BuildPerVoxelLightListShader, kGenListPerVoxelKernel, "g_LayeredSingleIdxBuffer", m_globalLightListAtomic);

            if (gUseDepthBuffer) cmd.SetComputeBufferParam(m_BuildPerVoxelLightListShader, kGenListPerVoxelKernel, "g_logBaseBuffer", m_perTileLogBaseTweak);

            int nrTilesX = (camera.pixelWidth + 15) / 16;
            int nrTilesY = (camera.pixelHeight + 15) / 16;
            cmd.DispatchCompute(m_BuildPerVoxelLightListShader, kGenListPerVoxelKernel, nrTilesX, nrTilesY, 1);
        }

        void PushGlobalParams(Camera camera, RenderLoop loop, Matrix4x4 viewToWorld, Matrix4x4 scrProj, Matrix4x4 incScrProj, int numDirLights)
        {
            var cmd = new CommandBuffer();
            cmd.name = "Push Global Parameters";

            cmd.SetGlobalFloat("g_widthRT", (float)camera.pixelWidth);
            cmd.SetGlobalFloat("g_heightRT", (float)camera.pixelHeight);

            cmd.SetGlobalMatrix("g_mViewToWorld", viewToWorld);
            cmd.SetGlobalMatrix("g_mWorldToView", viewToWorld.inverse);
            cmd.SetGlobalMatrix("g_mScrProjection", scrProj);
            cmd.SetGlobalMatrix("g_mInvScrProjection", incScrProj);

            cmd.SetGlobalBuffer("g_vLightData", m_lightDataBuffer);

            cmd.SetGlobalTexture("_spotCookieTextures", m_cookieTexArray.GetTexCache());
            cmd.SetGlobalTexture("_pointCookieTextures", m_cubeCookieTexArray.GetTexCache());
            cmd.SetGlobalTexture("_reflCubeTextures", m_cubeReflTexArray.GetTexCache());

            if (EnableClustered)
            {
                cmd.SetGlobalFloat("g_fClustScale", m_clustScale);
                cmd.SetGlobalFloat("g_fClustBase", m_clustLogBase);
                cmd.SetGlobalFloat("g_fNearPlane", camera.nearClipPlane);
                cmd.SetGlobalFloat("g_fFarPlane", camera.farClipPlane);
                cmd.SetGlobalFloat("g_iLog2NumClusters", g_iLog2NumClusters);


                cmd.SetGlobalFloat("g_isLogBaseBufferEnabled", gUseDepthBuffer ? 1 : 0);

                cmd.SetGlobalBuffer("g_vLayeredOffsetsBuffer", m_perVoxelOffset);
                if (gUseDepthBuffer) cmd.SetGlobalBuffer("g_logBaseBuffer", m_perTileLogBaseTweak);
            }

            cmd.SetGlobalFloat("g_nNumDirLights", numDirLights);
            cmd.SetGlobalBuffer("g_dirLightData", m_dirLightList);

            // Shadow constants
            cmd.SetGlobalMatrixArray("g_matWorldToShadow", g_matWorldToShadow);
            cmd.SetGlobalVectorArray("g_vDirShadowSplitSpheres", g_vDirShadowSplitSpheres);
            cmd.SetGlobalVector("g_vShadow3x3PCFTerms0", g_vShadow3x3PCFTerms[0]);
            cmd.SetGlobalVector("g_vShadow3x3PCFTerms1", g_vShadow3x3PCFTerms[1]);
            cmd.SetGlobalVector("g_vShadow3x3PCFTerms2", g_vShadow3x3PCFTerms[2]);
            cmd.SetGlobalVector("g_vShadow3x3PCFTerms3", g_vShadow3x3PCFTerms[3]);

            loop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }
    }
}
