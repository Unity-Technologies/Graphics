using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    /// <summary>
    /// Inherit from this class to perform custom rendering in the Lightweight Render Pipeline. 
    /// </summary>
    public abstract class ScriptableRenderPass
    {
        List<ShaderTagId> m_ShaderTagIDs = new List<ShaderTagId>();

        List<ShaderTagId> m_LegacyShaderPassNames = new List<ShaderTagId>()
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM"),
        };

        static Material s_ErrorMaterial;
        static Material errorMaterial
        {
            get
            {
                if (s_ErrorMaterial == null)
                    s_ErrorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));

                return s_ErrorMaterial;
            }
        }

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

        static PostProcessRenderContext m_PostProcessRenderContext;
        internal static PostProcessRenderContext postProcessRenderContext
        {
            get
            {
                if (m_PostProcessRenderContext == null)
                    m_PostProcessRenderContext = new PostProcessRenderContext();

                return m_PostProcessRenderContext;
            }
        }

        public FilteringSettings filteringSettings;

        public DrawGroups drawGroupsMask
        {
            get => m_DrawGroupsMask;
            set => m_DrawGroupsMask = value;
        }

        DrawGroups m_DrawGroupsMask = DrawGroups.Background;
        
        public ScriptableRenderPass()
        {
            filteringSettings = new FilteringSettings(RenderQueueRange.all);
        }
        
        /// <summary>
        /// Cleanup any allocated data that was created during the execution of the pass.
        /// </summary>
        /// <param name="cmd">Use this CommandBuffer to cleanup any generated data</param>
        public virtual void FrameCleanup(CommandBuffer cmd)
        {}

        /// <summary>
        /// Execute the pass. This is where custom rendering occurs. Specific details are left to the implementation
        /// </summary>
        /// <param name="renderer">The currently executing renderer. Contains configuration for the current execute call.</param>
        /// <param name="context">Use this render context to issue any draw commands during execution</param>
        /// <param name="renderingData">Current rendering state information</param>
        public abstract void Execute(ScriptableRenderContext context, ref RenderingData renderingData);

        protected void RegisterShaderPassName(string passName)
        {
            m_ShaderTagIDs.Add(new ShaderTagId(passName));
        }

        protected void RenderFullscreenQuad(CommandBuffer cmd, Material material, MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            cmd.DrawMesh(fullscreenMesh, Matrix4x4.identity, material, 0, shaderPassId, properties);
        }

        protected void RenderObjects(ScriptableRenderContext context, ref RenderingData renderingData, ref FilteringSettings filterSettings,
            ShaderTagId[] passNames,  SortingCriteria sortingCriteria)
        {
            if (passNames.Length == 0)
                return;

            SortingSettings sortingSettings = new SortingSettings(renderingData.cameraData.camera) { criteria = sortingCriteria };
            DrawingSettings drawingSettings = new DrawingSettings(passNames[0], sortingSettings)
            {
                perObjectData = renderingData.perObjectData,
                enableInstancing = true,
                mainLightIndex = renderingData.lightData.mainLightIndex,
                enableDynamicBatching = renderingData.supportsDynamicBatching,
            };
            for (int i = 1; i < passNames.Length; ++i)
                drawingSettings.SetShaderPassName(i, passNames[i]);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filterSettings);
        }
        
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        protected void RenderObjectsWithError(ScriptableRenderContext context, ref CullingResults cullResults, Camera camera, FilteringSettings filterSettings, SortingCriteria sortFlags)
        {
            SortingSettings sortingSettings = new SortingSettings(camera) { criteria = sortFlags };
            DrawingSettings errorSettings = new DrawingSettings(m_LegacyShaderPassNames[0], sortingSettings)
            {
                perObjectData = PerObjectData.None,
                overrideMaterial = errorMaterial,
                overrideMaterialPassIndex = 0
            };
            for (int i = 1; i < m_LegacyShaderPassNames.Count; ++i)
                errorSettings.SetShaderPassName(i, m_LegacyShaderPassNames[i]);

            context.DrawRenderers(cullResults, ref errorSettings, ref filterSettings);
        }

        protected void RenderPostProcess(CommandBuffer cmd, ref CameraData cameraData, RenderTextureFormat colorFormat, RenderTargetIdentifier source, RenderTargetIdentifier dest, bool opaqueOnly)
        {
            RenderPostProcess(cmd, ref cameraData, colorFormat, source, dest, opaqueOnly, !cameraData.isStereoEnabled && cameraData.camera.targetTexture == null);
        }

        protected void RenderPostProcess(CommandBuffer cmd, ref CameraData cameraData, RenderTextureFormat colorFormat, RenderTargetIdentifier source, RenderTargetIdentifier dest, bool opaqueOnly, bool flip)
        {
            Camera camera = cameraData.camera;
            postProcessRenderContext.Reset();
            postProcessRenderContext.camera = camera;
            postProcessRenderContext.source = source;
            postProcessRenderContext.sourceFormat = colorFormat;
            postProcessRenderContext.destination = dest;
            postProcessRenderContext.command = cmd;
            postProcessRenderContext.flip = flip;

            if (opaqueOnly)
                cameraData.postProcessLayer.RenderOpaqueOnly(postProcessRenderContext);
            else
                cameraData.postProcessLayer.Render(postProcessRenderContext);
        }

        protected static void CopyTexture(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, Material material)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            // TODO: In order to issue a copyTexture we need to also check if source and dest have same size
            //if (SystemInfo.copyTextureSupport != CopyTextureSupport.None)
            //    cmd.CopyTexture(source, dest);
            //else
            cmd.Blit(source, dest, material);
        }

        protected DrawingSettings CreateDrawingSettings(Camera camera, SortingCriteria sortingCriteria, PerObjectData perObjectData, bool supportsDynamicBatching, int mainLightIndex = -1)
        {
            SortingSettings sortingSettings = new SortingSettings(camera) { criteria = sortingCriteria };
            DrawingSettings settings = new DrawingSettings(m_ShaderTagIDs[0], sortingSettings)
            {
                perObjectData = perObjectData,
                enableInstancing = true,
                mainLightIndex = mainLightIndex,
                enableDynamicBatching = supportsDynamicBatching
            };
            for (int i = 1; i < m_ShaderTagIDs.Count; ++i)
                settings.SetShaderPassName(i, m_ShaderTagIDs[i]);
            return settings;
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

        protected static void SetRenderTarget(
            CommandBuffer cmd,
            RenderTargetIdentifier colorAttachment,
            RenderBufferLoadAction colorLoadAction,
            RenderBufferStoreAction colorStoreAction,
            ClearFlag clearFlags,
            Color clearColor,
            TextureDimension dimension)
        {
            if (dimension == TextureDimension.Tex2DArray)
                CoreUtils.SetRenderTarget(cmd, colorAttachment, clearFlags, clearColor, 0, CubemapFace.Unknown, -1);
            else
                CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction, clearFlags, clearColor);
        }

        protected static void SetRenderTarget(
            CommandBuffer cmd,
            RenderTargetIdentifier colorAttachment,
            RenderBufferLoadAction colorLoadAction,
            RenderBufferStoreAction colorStoreAction,
            RenderTargetIdentifier depthAttachment,
            RenderBufferLoadAction depthLoadAction,
            RenderBufferStoreAction depthStoreAction,
            ClearFlag clearFlags,
            Color clearColor,
            TextureDimension dimension)
        {
            if (depthAttachment == BuiltinRenderTextureType.CameraTarget)
            {
                SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction, clearFlags, clearColor,
                    dimension);
            }
            else
            {
                if (dimension == TextureDimension.Tex2DArray)
                    CoreUtils.SetRenderTarget(cmd, colorAttachment, depthAttachment,
                        clearFlags, clearColor, 0, CubemapFace.Unknown, -1);
                else
                    CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction,
                        depthAttachment, depthLoadAction, depthStoreAction, clearFlags, clearColor);
            }
        }
    }
}
