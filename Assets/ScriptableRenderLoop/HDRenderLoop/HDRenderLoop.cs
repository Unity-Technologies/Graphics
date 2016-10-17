using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System;

using UnityEditor;

namespace UnityEngine.ScriptableRenderLoop
{
    [ExecuteInEditMode]
    // This HDRenderLoop assume linear lighting. Don't work with gamma.
    public class HDRenderLoop : ScriptableRenderLoop
    {
        private static string m_HDRenderLoopPath = "Assets/ScriptableRenderLoop/HDRenderLoop/HDRenderLoop.asset";

        // Must be in sync with DebugViewMaterial.hlsl
        public enum DebugViewVaryingMode
        {
            Depth = 1,
            TexCoord0 = 2,
            VertexNormalWS = 3,
            VertexTangentWS = 4,
            VertexBitangentWS = 5,
            VertexColor = 6,
        }

        // Must be in sync with DebugViewMaterial.hlsl
        public enum DebugViewGbufferMode
        {
            Depth = 7,
            BakeDiffuseLighting = 8,
        }

        public class DebugParameters
        {
            // Material Debugging
            public int debugViewMaterial = 0;

            // Rendering debugging
            public bool displayOpaqueObjects = true;
            public bool displayTransparentObjects = true;

            public bool useForwardRenderingOnly = false;

            public bool enableTonemap = true;
            public float exposure = 0;
        }

        private DebugParameters m_DebugParameters = new DebugParameters();
        public DebugParameters debugParameters
        {
            get { return m_DebugParameters; }
        }

        #if UNITY_EDITOR
        [MenuItem("Renderloop/CreateHDRenderLoop")]
        static void CreateHDRenderLoop()
        {
            var instance = ScriptableObject.CreateInstance<HDRenderLoop>();
            UnityEditor.AssetDatabase.CreateAsset(instance, m_HDRenderLoopPath);
        }

        #endif

        public class GBufferManager
        {
            public const int MaxGbuffer = 8;

            public void SetBufferDescription(int index, string stringID, RenderTextureFormat inFormat, RenderTextureReadWrite inSRGBWrite)
            {
                IDs[index] = Shader.PropertyToID(stringID);
                RTIDs[index] = new RenderTargetIdentifier(IDs[index]);
                formats[index] = inFormat;
                sRGBWrites[index] = inSRGBWrite;
            }

            public void InitGBuffers(int width, int height, CommandBuffer cmd)
            {
                for (int index = 0; index < gbufferCount; index++)
                {
                    /* RTs[index] = */ cmd.GetTemporaryRT(IDs[index], width, height, 0, FilterMode.Point, formats[index], sRGBWrites[index]);
                }
            }

            public RenderTargetIdentifier[] GetGBuffers(CommandBuffer cmd)
            {
                var colorMRTs = new RenderTargetIdentifier[gbufferCount];
                for (int index = 0; index < gbufferCount; index++)
                {
                    colorMRTs[index] = RTIDs[index];
                }

                return colorMRTs;
            }

            /*
            public void BindBuffers(Material mat)
            {
                for (int index = 0; index < gbufferCount; index++)
                {
                    mat.SetTexture(IDs[index], RTs[index]);
                }
            }
            */


            public int gbufferCount { get; set; }
            int[] IDs = new int[MaxGbuffer];
            RenderTargetIdentifier[] RTIDs = new RenderTargetIdentifier[MaxGbuffer];
            RenderTextureFormat[] formats = new RenderTextureFormat[MaxGbuffer];
            RenderTextureReadWrite[] sRGBWrites = new RenderTextureReadWrite[MaxGbuffer];
        }

        public const int MaxLights = 32;
        public const int MaxProbes = 32;

        //[SerializeField]
        //ShadowSettings m_ShadowSettings = ShadowSettings.Default;
        //ShadowRenderPass m_ShadowPass;

        [SerializeField]
        TextureSettings m_TextureSettings = TextureSettings.Default;

        Material m_DeferredMaterial;
        Material m_FinalPassMaterial;

        // TODO: Find a way to automatically create/iterate through these kind of class
        Lit.RenderLoop m_litRenderLoop;

        // Debug
        Material m_DebugViewMaterialGBuffer;

        GBufferManager m_gbufferManager = new GBufferManager();

        static private int s_CameraColorBuffer;
        static private int s_CameraDepthBuffer;

        static private ComputeBuffer s_punctualLightList;
        static private ComputeBuffer s_envLightList;

        private TextureCacheCubemap m_cubeReflTexArray;

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
            if (s_punctualLightList != null)
                s_punctualLightList.Release();

            if (s_envLightList != null)
                s_envLightList.Release();
        }

        Material CreateEngineMaterial(string shaderPath)
        {
            Material mat = new Material(Shader.Find(shaderPath) as Shader);
            mat.hideFlags = HideFlags.HideAndDontSave;
            return mat;
        }

        public override void Rebuild()
        {
            ClearComputeBuffers();

            s_CameraColorBuffer = Shader.PropertyToID("_CameraColorTexture");
            s_CameraDepthBuffer = Shader.PropertyToID("_CameraDepthTexture");

            s_punctualLightList = new ComputeBuffer(MaxLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PunctualLightData)));
            s_envLightList = new ComputeBuffer(MaxLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EnvLightData)));

            m_DeferredMaterial = CreateEngineMaterial("Hidden/Unity/LightingDeferred");
            m_FinalPassMaterial = CreateEngineMaterial("Hidden/Unity/FinalPass");

            // Debug
            m_DebugViewMaterialGBuffer = CreateEngineMaterial("Hidden/Unity/DebugViewMaterialGBuffer");

            // m_ShadowPass = new ShadowRenderPass (m_ShadowSettings);

            m_cubeReflTexArray = new TextureCacheCubemap();
            m_cubeReflTexArray.AllocTextureArray(32, (int)m_TextureSettings.reflectionCubemapSize, TextureFormat.BC6H, true);

            // Init Lit material buffer - GBuffer and init
            m_litRenderLoop = new Lit.RenderLoop(); // Our object can be garbacge collected, so need to be allocate here

            m_gbufferManager.gbufferCount = m_litRenderLoop.GetGBufferCount();
            for (int gbufferIndex = 0; gbufferIndex < m_gbufferManager.gbufferCount; ++gbufferIndex)
            {
                m_gbufferManager.SetBufferDescription(gbufferIndex, "_CameraGBufferTexture" + gbufferIndex, m_litRenderLoop.RTFormat[gbufferIndex], m_litRenderLoop.RTReadWrite[gbufferIndex]);
            }

            m_litRenderLoop.Rebuild();
        }

        void OnDisable()
        {
            m_litRenderLoop.OnDisable();

            s_punctualLightList.Release();
            s_envLightList.Release();

            if (m_DeferredMaterial) DestroyImmediate(m_DeferredMaterial);
            if (m_FinalPassMaterial) DestroyImmediate(m_FinalPassMaterial);

            m_cubeReflTexArray.Release();
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
                int w = camera.pixelWidth;
                int h = camera.pixelHeight;

                cmd.GetTemporaryRT(s_CameraColorBuffer, w, h, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default);
                cmd.GetTemporaryRT(s_CameraDepthBuffer, w, h, 24, FilterMode.Point, RenderTextureFormat.Depth);
                m_gbufferManager.InitGBuffers(w, h, cmd);

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
                cmd.SetRenderTarget(m_gbufferManager.GetGBuffers(cmd), new RenderTargetIdentifier(s_CameraDepthBuffer));
                // Clear everything
                // TODO: Clear is not required for color as we rewrite everything, will save performance.
                cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 0));
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }

            // END TEMP
        }

        void RenderOpaqueRenderList(CullResults cull, Camera camera, RenderLoop renderLoop, string passName)
        {
            if (!debugParameters.displayOpaqueObjects)
                return;

            DrawRendererSettings settings = new DrawRendererSettings(cull, camera, new ShaderPassName(passName));
            settings.sorting.sortOptions = SortOptions.SortByMaterialThenMesh;
            settings.inputCullingOptions.SetQueuesOpaque();
            renderLoop.DrawRenderers(ref settings);
        }

        void RenderTransparentRenderList(CullResults cull, Camera camera, RenderLoop renderLoop, string passName)
        {
            if (!debugParameters.displayTransparentObjects)
                return;

            DrawRendererSettings settings = new DrawRendererSettings(cull, camera, new ShaderPassName(passName));
            settings.rendererConfiguration = RendererConfiguration.PerObjectLightProbe | RendererConfiguration.PerObjectReflectionProbes;
            settings.sorting.sortOptions = SortOptions.SortByMaterialThenMesh;
            settings.inputCullingOptions.SetQueuesTransparent();
            renderLoop.DrawRenderers(ref settings);
        }

        void RenderGBuffer(CullResults cull, Camera camera, RenderLoop renderLoop)
        {
            if (debugParameters.useForwardRenderingOnly)
            {
                return ;
            }

            // setup GBuffer for rendering
            var cmd = new CommandBuffer();
            cmd.name = "GBuffer Pass";
            cmd.SetRenderTarget(m_gbufferManager.GetGBuffers(cmd), new RenderTargetIdentifier(s_CameraDepthBuffer));
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            // render opaque objects into GBuffer
            RenderOpaqueRenderList(cull, camera, renderLoop, "GBuffer");
        }

        void RenderDebugViewMaterial(CullResults cull, Camera camera, RenderLoop renderLoop)
        {
            // Render Opaque forward
            {
                var cmd = new CommandBuffer();
                cmd.name = "DebugView Material Mode Pass";
                cmd.SetRenderTarget(new RenderTargetIdentifier(s_CameraColorBuffer), new RenderTargetIdentifier(s_CameraDepthBuffer));
                cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();

                Shader.SetGlobalInt("_DebugViewMaterial", (int)debugParameters.debugViewMaterial);

                RenderOpaqueRenderList(cull, camera, renderLoop, "DebugView");
            }

            // Render GBUffer opaque
            {
                Vector4 screenSize = ComputeScreenSize(camera);
                m_DebugViewMaterialGBuffer.SetVector("_ScreenSize", screenSize);
                m_DebugViewMaterialGBuffer.SetFloat("_DebugViewMaterial", (float)debugParameters.debugViewMaterial);

                // m_gbufferManager.BindBuffers(m_DeferredMaterial);
                // TODO: Bind depth textures
                var cmd = new CommandBuffer();
                cmd.name = "GBuffer Debug Pass";
                cmd.Blit(null, new RenderTargetIdentifier(s_CameraColorBuffer), m_DebugViewMaterialGBuffer, 0);
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }

            // Render forward transparent
            {
                RenderTransparentRenderList(cull, camera, renderLoop, "DebugView");
            }

            // Last blit
            {
                var cmd = new CommandBuffer();
                cmd.name = "Blit DebugView Material Debug";
                cmd.Blit(s_CameraColorBuffer, BuiltinRenderTextureType.CameraTarget);
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }
        }

        Matrix4x4 GetViewProjectionMatrix(Camera camera)
        {
            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            Matrix4x4 gpuVP = gpuProj * camera.worldToCameraMatrix;

            return gpuVP;
        }

        Vector4 ComputeScreenSize(Camera camera)
        {
            Vector4 screenSize = new Vector4();
            screenSize.x = camera.pixelWidth;
            screenSize.y = camera.pixelHeight;
            screenSize.z = 1.0f / camera.pixelWidth;
            screenSize.w = 1.0f / camera.pixelHeight;
            return screenSize;
        }

        void RenderDeferredLighting(Camera camera, RenderLoop renderLoop)
        {
            if (debugParameters.useForwardRenderingOnly)
            {
                return;
            }

            // Bind material data
            m_litRenderLoop.Bind();

            Matrix4x4 invViewProj = GetViewProjectionMatrix(camera).inverse;
            m_DeferredMaterial.SetMatrix("_InvViewProjMatrix", invViewProj);

            Vector4 screenSize = ComputeScreenSize(camera);
            m_DeferredMaterial.SetVector("_ScreenSize", screenSize);

            // m_gbufferManager.BindBuffers(m_DeferredMaterial);
            // TODO: Bind depth textures
            var cmd = new CommandBuffer();
            cmd.name = "Deferred Ligthing Pass";
            cmd.Blit(null, new RenderTargetIdentifier(s_CameraColorBuffer), m_DeferredMaterial, 0);
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        void RenderForward(CullResults cullResults, Camera camera, RenderLoop renderLoop)
        {
            // Bind material data
            m_litRenderLoop.Bind();

            var cmd = new CommandBuffer();
            cmd.name = "Forward Pass";
            cmd.SetRenderTarget(new RenderTargetIdentifier(s_CameraColorBuffer), new RenderTargetIdentifier(s_CameraDepthBuffer));
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            if (debugParameters.useForwardRenderingOnly)
            {
                RenderOpaqueRenderList(cullResults, camera, renderLoop, "Forward");
            }

            RenderTransparentRenderList(cullResults, camera, renderLoop, "Forward");
        }

        void FinalPass(RenderLoop renderLoop)
        {
            // Those could be tweakable for the neutral tonemapper, but in the case of the LookDev we don't need that
            const float BlackIn = 0.02f;
            const float WhiteIn = 10.0f;
            const float BlackOut = 0.0f;
            const float WhiteOut = 10.0f;
            const float WhiteLevel = 5.3f;
            const float WhiteClip = 10.0f;
            const float DialUnits = 20.0f;
            const float HalfDialUnits = DialUnits * 0.5f;

            // converting from artist dial units to easy shader-lerps (0-1)
            Vector4 tonemapCoeff1 = new Vector4((BlackIn * DialUnits) + 1.0f, (BlackOut * HalfDialUnits) + 1.0f, (WhiteIn / DialUnits), (1.0f - (WhiteOut / DialUnits)));
            Vector4 tonemapCoeff2 = new Vector4(0.0f, 0.0f, WhiteLevel, WhiteClip / HalfDialUnits);

            m_FinalPassMaterial.SetVector("_ToneMapCoeffs1", tonemapCoeff1);
            m_FinalPassMaterial.SetVector("_ToneMapCoeffs2", tonemapCoeff2);

            m_FinalPassMaterial.SetFloat("_EnableToneMap", debugParameters.enableTonemap ? 1.0f : 0.0f);
            m_FinalPassMaterial.SetFloat("_Exposure", debugParameters.exposure);

            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "FinalPass";
            // Resolve our HDR texture to CameraTarget.
            cmd.Blit(s_CameraColorBuffer, BuiltinRenderTextureType.CameraTarget, m_FinalPassMaterial, 0);
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        void NewFrame()
        {
            // update texture caches
            m_cubeReflTexArray.NewFrame();
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------

        void UpdatePunctualLights(VisibleLight[] visibleLights)
        {
            List<PunctualLightData> lights = new List<PunctualLightData>();

            for (int lightIndex = 0; lightIndex < Math.Min(visibleLights.Length, MaxLights); lightIndex++)
            {
                VisibleLight light = visibleLights[lightIndex];
                if (light.lightType == LightType.Spot || light.lightType == LightType.Point || light.lightType == LightType.Directional)
                {
                    PunctualLightData l = new PunctualLightData();

                    if (light.lightType == LightType.Directional)
                    {
                        l.useDistanceAttenuation = 0.0f;
                        // positionWS store Light direction for directional and is opposite to the forward direction
                        l.positionWS = -light.light.transform.forward;
                        l.invSqrAttenuationRadius = 0.0f;
                    }
                    else
                    {
                        l.useDistanceAttenuation = 1.0f;
                        l.positionWS = light.light.transform.position;
                        l.invSqrAttenuationRadius = 1.0f / (light.range * light.range);
                    }

                    // Correct intensity calculation (Different from Unity)
                    float lightColorR = light.light.intensity * Mathf.GammaToLinearSpace(light.light.color.r);
                    float lightColorG = light.light.intensity * Mathf.GammaToLinearSpace(light.light.color.g);
                    float lightColorB = light.light.intensity * Mathf.GammaToLinearSpace(light.light.color.b);

                    l.color.Set(lightColorR, lightColorG, lightColorB);

                    // Light direction is opposite to the forward direction
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
                        l.angleScale    = 1.0f / val;
                        l.angleOffset   = -cosSpotOuterHalfAngle * l.angleScale;
                    }
                    else
                    {
                        // 1.0f, 2.0f are neutral value allowing GetAngleAnttenuation in shader code to return 1.0
                        l.angleScale = 1.0f;
                        l.angleOffset = 2.0f;
                    }

                    lights.Add(l);
                }
            }
            s_punctualLightList.SetData(lights.ToArray());

            Shader.SetGlobalBuffer("_PunctualLightList", s_punctualLightList);
            Shader.SetGlobalInt("_PunctualLightCount", lights.Count);
        }

        void UpdateReflectionProbes(VisibleReflectionProbe[] activeReflectionProbes)
        {
            List<EnvLightData> lights = new List<EnvLightData>();

            for (int lightIndex = 0; lightIndex < Math.Min(activeReflectionProbes.Length, MaxProbes); lightIndex++)
            {
                VisibleReflectionProbe probe = activeReflectionProbes[lightIndex];

                if (probe.texture == null)
                    continue;

                EnvLightData l = new EnvLightData();

                l.positionWS = probe.localToWorld.GetColumn(3);
                l.shapeType = EnvShapeType.None;
                if (probe.boxProjection != 0)
                {
                    l.shapeType = EnvShapeType.Box;
                }

                // Caution should only be rigid transform, no scale
                l.worldToLocal = probe.localToWorld.transpose;
                l.innerDistance = probe.bounds.extents;

                l.sliceIndex = m_cubeReflTexArray.FetchSlice(probe.texture);

                // center is local capture point. By multiply by localToWorld we get world position
                l.capturePointWS = probe.localToWorld.MultiplyPoint(probe.center);
                l.blendDistance = probe.blendDistance;

                lights.Add(l);
            }

            s_envLightList.SetData(lights.ToArray());

            Shader.SetGlobalBuffer("_EnvLightList", s_envLightList);
            Shader.SetGlobalInt("_EnvLightCount", lights.Count);
            Shader.SetGlobalTexture("_EnvTextures", m_cubeReflTexArray.GetTexCache());
        }

        public override void Render(Camera[] cameras, RenderLoop renderLoop)
        {
            if (!m_litRenderLoop.isInit)
            {
                m_litRenderLoop.RenderInit(renderLoop);
            }

            // Do anything we need to do upon a new frame.
            NewFrame();

            // Set Frame constant buffer
            // TODO...

            foreach (var camera in cameras)
            {
                // Set camera constant buffer
                // TODO...

                CullResults cullResults;
                CullingParameters cullingParams;
                if (!CullResults.GetCullingParameters(camera, out cullingParams))
                    continue;

                //m_ShadowPass.UpdateCullingParameters (ref cullingParams);

                cullResults = CullResults.Cull(ref cullingParams, renderLoop);

                //ShadowOutput shadows;
                //m_ShadowPass.Render (renderLoop, cullResults, out shadows);

                renderLoop.SetupCameraProperties(camera);

                //UpdateLightConstants(cullResults.visibleLights /*, ref shadows */);

                UpdatePunctualLights(cullResults.visibleLights);
                UpdateReflectionProbes(cullResults.visibleReflectionProbes);

                InitAndClearBuffer(camera, renderLoop);

                RenderGBuffer(cullResults, camera, renderLoop);

                if (debugParameters.debugViewMaterial != 0)
                {
                    RenderDebugViewMaterial(cullResults, camera, renderLoop);
                }
                else
                {
                    RenderDeferredLighting(camera, renderLoop);

                    RenderForward(cullResults, camera, renderLoop);

                    FinalPass(renderLoop);
                }

                renderLoop.Submit();
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
