using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering.LWRP;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public sealed class ScriptableRenderer
    {
        // When there is no support to StruturedBuffer lights data is setup in a constants data
        // we also limit the amount of lights that can be shaded per object to simplify shading
        // in these low end platforms (GLES 2.0 and GLES 3.0)

        // Amount of Lights that can be shaded per object (in the for loop in the shader)
        // This uses unity_4LightIndices0 to store 4 per-object light indices
        const int k_MaxPerObjectAdditionalLightsNoStructuredBuffer = 4;

        // Light data is stored in a constant buffer (uniform array)
        // This value has to match MAX_VISIBLE_LIGHTS in Input.hlsl
        const int k_MaxVisibleAdditionalLightsNoStructuredBuffer = 16;

        // Point and Spot Lights are stored in a StructuredBuffer.
        // We shade the amount of light per-object as requested in the pipeline asset and
        // we can store a great deal of lights in our global light buffer
        const int k_MaxVisibleAdditioanlLightsStructuredBuffer = 4096;

        public static bool useStructuredBufferForLights
        {
            get
            {
                // TODO: Graphics Emulation are breaking StructuredBuffers disabling it for now until
                // we have a fix for it
                return false;
                // return SystemInfo.supportsComputeShaders &&
                //        SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLCore &&
                //        !Application.isMobilePlatform &&
                //        Application.platform != RuntimePlatform.WebGLPlayer;
            }
        }

        public int maxPerObjectAdditionalLights
        {
            get
            {
                return useStructuredBufferForLights ?
                    8 : k_MaxPerObjectAdditionalLightsNoStructuredBuffer;
            }
        }

        public int maxVisibleAdditionalLights
        {
            get
            {
                return useStructuredBufferForLights ?
                    k_MaxVisibleAdditioanlLightsStructuredBuffer :
                    k_MaxVisibleAdditionalLightsNoStructuredBuffer;
            }
        }

        public PostProcessRenderContext postProcessingContext { get; private set; }

        public ComputeBuffer perObjectLightIndices { get; private set; }

        static Mesh s_FullscreenMesh = null;
        static Mesh fullscreenMesh
        {
            get
            {
                if (s_FullscreenMesh != null)
                    return s_FullscreenMesh;

                float topV = 1.0f;
                float bottomV = 0.0f;

                s_FullscreenMesh = new Mesh { name = "Fullscreen Quad" };
                s_FullscreenMesh.SetVertices(new List<Vector3>
                {
                    new Vector3(-1.0f, -1.0f, 0.0f),
                    new Vector3(-1.0f,  1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f,  1.0f, 0.0f)
                });

                s_FullscreenMesh.SetUVs(0, new List<Vector2>
                {
                    new Vector2(0.0f, bottomV),
                    new Vector2(0.0f, topV),
                    new Vector2(1.0f, bottomV),
                    new Vector2(1.0f, topV)
                });

                s_FullscreenMesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0, false);
                s_FullscreenMesh.UploadMeshData(true);
                return s_FullscreenMesh;
            }
        }

        List<ScriptableRenderPass> m_ActiveRenderPassQueue = new List<ScriptableRenderPass>();

        List<ShaderTagId> m_LegacyShaderPassNames = new List<ShaderTagId>()
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM"),
        };

        const string k_ReleaseResourcesTag = "Release Resources";
        Material m_ErrorMaterial = null;

        public ScriptableRenderer()
        {
            m_ErrorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            postProcessingContext = new PostProcessRenderContext();
        }

        public void Dispose()
        {
            if (perObjectLightIndices != null)
            {
                perObjectLightIndices.Release();
                perObjectLightIndices = null;
            }
        }

        public void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Keywords are enabled while executing passes.
            CommandBuffer cmd = CommandBufferPool.Get("Clear Pipeline Keywords");
            cmd.DisableShaderKeyword(ShaderKeywordStrings.MainLightShadows);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.MainLightShadowCascades);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightsVertex);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightsPixel);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightShadows);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.SoftShadows);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.MixedLightingSubtractive);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                m_ActiveRenderPassQueue[i].Execute(this, context, ref renderingData);

            DisposePasses(ref context);
        }

        public void Clear()
        {
            m_ActiveRenderPassQueue.Clear();
        }

        public void EnqueuePass(ScriptableRenderPass pass)
        {
            m_ActiveRenderPassQueue.Add(pass);
        }

        public void SetupPerObjectLightIndices(ref CullingResults cullResults, ref LightData lightData)
        {
            if (lightData.additionalLightsCount == 0)
                return;

            var visibleLights = lightData.visibleLights;
            var perObjectLightIndexMap = cullResults.GetLightIndexMap(Allocator.Temp);

            int directionalLightsCount = 0;
            int additionalLightsCount = 0;

            // Disable all directional lights from the perobject light indices
            // Pipeline handles them globally.
            for (int i = 0; i < visibleLights.Length; ++i)
            {
                if (additionalLightsCount >= maxVisibleAdditionalLights)
                    break;

                VisibleLight light = visibleLights[i];
                if (light.lightType == LightType.Directional)
                {
                    perObjectLightIndexMap[i] = -1;
                    ++directionalLightsCount;
                }
                else
                {
                    perObjectLightIndexMap[i] -= directionalLightsCount;
                    ++additionalLightsCount;
                }
            }

            // Disable all remaining lights we cannot fit into the global light buffer.
            for (int i = directionalLightsCount + additionalLightsCount; i < visibleLights.Length; ++i)
                perObjectLightIndexMap[i] = -1;

            cullResults.SetLightIndexMap(perObjectLightIndexMap);
            perObjectLightIndexMap.Dispose();

            // if not using a compute buffer, engine will set indices in 2 vec4 constants
            // unity_4LightIndices0 and unity_4LightIndices1
            if (useStructuredBufferForLights)
            {
                int lightIndicesCount = cullResults.lightAndReflectionProbeIndexCount;
                if (lightIndicesCount > 0)
                {
                    if (perObjectLightIndices == null)
                    {
                        perObjectLightIndices = new ComputeBuffer(lightIndicesCount, sizeof(int));
                    }
                    else if (perObjectLightIndices.count < lightIndicesCount)
                    {
                        perObjectLightIndices.Release();
                        perObjectLightIndices = new ComputeBuffer(lightIndicesCount, sizeof(int));
                    }

                    cullResults.FillLightAndReflectionProbeIndices(perObjectLightIndices);
                }
            }
        }

        public void RenderPostProcess(CommandBuffer cmd, ref CameraData cameraData, RenderTextureFormat colorFormat, RenderTargetIdentifier source, RenderTargetIdentifier dest, bool opaqueOnly)
        {
            RenderPostProcess(cmd, ref cameraData, colorFormat, source, dest, opaqueOnly, !cameraData.isStereoEnabled && cameraData.camera.targetTexture == null);
        }

        public void RenderPostProcess(CommandBuffer cmd, ref CameraData cameraData, RenderTextureFormat colorFormat, RenderTargetIdentifier source, RenderTargetIdentifier dest, bool opaqueOnly, bool flip)
        {
            Camera camera = cameraData.camera;
            postProcessingContext.Reset();
            postProcessingContext.camera = camera;
            postProcessingContext.source = source;
            postProcessingContext.sourceFormat = colorFormat;
            postProcessingContext.destination = dest;
            postProcessingContext.command = cmd;
            postProcessingContext.flip = flip;

            if (opaqueOnly)
                cameraData.postProcessLayer.RenderOpaqueOnly(postProcessingContext);
            else
                cameraData.postProcessLayer.Render(postProcessingContext);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void RenderObjectsWithError(ScriptableRenderContext context, ref CullingResults cullResults, Camera camera, FilteringSettings filterSettings, SortingCriteria sortFlags)
        {
            if (m_ErrorMaterial != null)
            {
                SortingSettings sortingSettings = new SortingSettings(camera) { criteria = sortFlags };
                DrawingSettings errorSettings = new DrawingSettings(m_LegacyShaderPassNames[0], sortingSettings)
                {
                    perObjectData = PerObjectData.None,
                    overrideMaterial = m_ErrorMaterial,
                    overrideMaterialPassIndex = 0
                };
                for (int i = 1; i < m_LegacyShaderPassNames.Count; ++i)
                    errorSettings.SetShaderPassName(i, m_LegacyShaderPassNames[i]);

                context.DrawRenderers(cullResults, ref errorSettings, ref filterSettings);
            }
        }

        public static RenderTextureDescriptor CreateRenderTextureDescriptor(ref CameraData cameraData, float scaler = 1.0f)
        {
            Camera camera = cameraData.camera;
            RenderTextureDescriptor desc;
            float renderScale = cameraData.renderScale;
            RenderTextureFormat renderTextureFormatDefault = RenderTextureFormat.Default;

            if (cameraData.isStereoEnabled)
            {
                desc = XRGraphics.eyeTextureDesc;
                renderTextureFormatDefault = desc.colorFormat;
            }
            else
            {
                desc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
                desc.width = (int)((float)desc.width * renderScale * scaler);
                desc.height = (int)((float)desc.height * renderScale * scaler);
                desc.depthBufferBits = 32;
            }

            // TODO: when preserve framebuffer alpha is enabled we can't use RGB111110Float format. 
            bool useRGB111110 = Application.isMobilePlatform &&
             SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float);
            RenderTextureFormat hdrFormat = (useRGB111110) ? RenderTextureFormat.RGB111110Float : RenderTextureFormat.DefaultHDR;
            desc.colorFormat = cameraData.isHdrEnabled ? hdrFormat : renderTextureFormatDefault;
            desc.enableRandomWrite = false;
            desc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            desc.msaaSamples = cameraData.msaaSamples;
            desc.bindMS = false;
            desc.useDynamicScale = cameraData.camera.allowDynamicResolution;
            return desc;
        }

        public static ClearFlag GetCameraClearFlag(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException("camera");

            CameraClearFlags cameraClearFlags = camera.clearFlags;

#if UNITY_EDITOR
            // We need public API to tell if FrameDebugger is active and enabled. In that case
            // we want to force a clear to see properly the drawcall stepping.
            // For now, to fix FrameDebugger in Editor, we force a clear. 
            cameraClearFlags = CameraClearFlags.SolidColor;
#endif

            // LWRP doesn't support CameraClearFlags.DepthOnly and CameraClearFlags.Nothing.
            // CameraClearFlags.DepthOnly has the same effect of CameraClearFlags.SolidColor
            // CameraClearFlags.Nothing clears Depth on PC/Desktop and in mobile it clears both
            // depth and color.
            // CameraClearFlags.Skybox clears depth only.

            // Implementation details:
            // Camera clear flags are used to initialize the attachments on the first render pass.
            // ClearFlag is used together with Tile Load action to figure out how to clear the camera render target.
            // In Tile Based GPUs ClearFlag.Depth + RenderBufferLoadAction.DontCare becomes DontCare load action.
            // While ClearFlag.All + RenderBufferLoadAction.DontCare become Clear load action.
            // In mobile we force ClearFlag.All as DontCare doesn't have noticeable perf. difference from Clear
            // and this avoid tile clearing issue when not rendering all pixels in some GPUs.
            // In desktop/consoles there's actually performance difference between DontCare and Clear.
            
            // RenderBufferLoadAction.DontCare in PC/Desktop behaves as not clearing screen
            // RenderBufferLoadAction.DontCare in Vulkan/Metal behaves as DontCare load action
            // RenderBufferLoadAction.DontCare in GLES behaves as glInvalidateBuffer

            // Always clear on first render pass in mobile as it's same perf of DontCare and avoid tile clearing issues.
            if (Application.isMobilePlatform)
                return ClearFlag.All;

            if ((cameraClearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null) ||
                cameraClearFlags == CameraClearFlags.Nothing)
                return ClearFlag.Depth;

            return ClearFlag.All;
        }

        public static PerObjectData GetPerObjectLightFlags(int mainLightIndex, int additionalLightsCount)
        {
            var configuration = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightData;
            if (additionalLightsCount > 0 && !useStructuredBufferForLights)
            {
                configuration |= PerObjectData.LightIndices;
            }

            return configuration;
        }

        public static void RenderFullscreenQuad(CommandBuffer cmd, Material material, MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            cmd.DrawMesh(fullscreenMesh, Matrix4x4.identity, material, 0, shaderPassId, properties);
        }

        public static void CopyTexture(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, Material material)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            // TODO: In order to issue a copyTexture we need to also check if source and dest have same size
            //if (SystemInfo.copyTextureSupport != CopyTextureSupport.None)
            //    cmd.CopyTexture(source, dest);
            //else
            cmd.Blit(source, dest, material);
        }

        void DisposePasses(ref ScriptableRenderContext context)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_ReleaseResourcesTag);

            for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                m_ActiveRenderPassQueue[i].FrameCleanup(cmd);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
