using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Rendering.LWRP
{
    public static class RenderingUtils
    {
        static int m_PostProcessingTemporaryTargetId = Shader.PropertyToID("_TemporaryColorTexture");

        static List<ShaderTagId> m_LegacyShaderPassNames = new List<ShaderTagId>()
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM"),
        };

        static Mesh s_FullscreenMesh = null;
        public static Mesh fullscreenMesh
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
        public static PostProcessRenderContext postProcessRenderContext
        {
            get
            {
                if (m_PostProcessRenderContext == null)
                    m_PostProcessRenderContext = new PostProcessRenderContext();

                return m_PostProcessRenderContext;
            }
        }

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

        internal static void RenderPostProcessing(CommandBuffer cmd, ref CameraData cameraData, RenderTextureDescriptor sourceDescriptor,
            RenderTargetIdentifier source, RenderTargetIdentifier destination, bool opaqueOnly, bool flip)
        {
            var layer = cameraData.postProcessLayer;
            int effectsCount;
            if (opaqueOnly)
            {
                effectsCount = layer.sortedBundles[PostProcessEvent.BeforeTransparent].Count;
            }
            else
            {
                effectsCount = layer.sortedBundles[PostProcessEvent.BeforeStack].Count +
                               layer.sortedBundles[PostProcessEvent.AfterStack].Count;
            }

            Camera camera = cameraData.camera;
            var postProcessRenderContext = RenderingUtils.postProcessRenderContext;
            postProcessRenderContext.Reset();
            postProcessRenderContext.camera = camera;
            postProcessRenderContext.source = source;
            postProcessRenderContext.sourceFormat = sourceDescriptor.colorFormat;
            postProcessRenderContext.destination = destination;
            postProcessRenderContext.command = cmd;
            postProcessRenderContext.flip = flip;

            // If there's only one effect in the stack and soure is same as dest we
            // create an intermediate blit rendertarget to handle it.
            // Otherwise, PostProcessing system will create the intermediate blit targets itself.
            if (effectsCount == 1 && source == destination)
            {
                RenderTargetIdentifier rtId = new RenderTargetIdentifier(m_PostProcessingTemporaryTargetId);
                RenderTextureDescriptor descriptor = sourceDescriptor;
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = 0;

                postProcessRenderContext.destination = rtId;
                cmd.GetTemporaryRT(m_PostProcessingTemporaryTargetId, descriptor, FilterMode.Point);

                if (opaqueOnly)
                    cameraData.postProcessLayer.RenderOpaqueOnly(postProcessRenderContext);
                else
                    cameraData.postProcessLayer.Render(postProcessRenderContext);

                cmd.Blit(rtId, destination);
                cmd.ReleaseTemporaryRT(m_PostProcessingTemporaryTargetId);
            }
            else
            {
                if (opaqueOnly)
                    cameraData.postProcessLayer.RenderOpaqueOnly(postProcessRenderContext);
                else
                    cameraData.postProcessLayer.Render(postProcessRenderContext);
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        internal static void RenderObjectsWithError(ScriptableRenderContext context, ref CullingResults cullResults, Camera camera, FilteringSettings filterSettings, SortingCriteria sortFlags)
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

        // Caches render texture format support. SystemInfo.SupportsRenderTextureFormat allocates memory due to boxing.
        static Dictionary<RenderTextureFormat, bool> m_RenderTextureFormatSupport = new Dictionary<RenderTextureFormat, bool>();

        internal static void ClearSystemInfoCache()
        {
            m_RenderTextureFormatSupport.Clear();
        }

        internal static bool SupportsRenderTextureFormat(RenderTextureFormat format)
        {
            if (!m_RenderTextureFormatSupport.TryGetValue(format, out var support))
            {
                support = SystemInfo.SupportsRenderTextureFormat(format);
                m_RenderTextureFormatSupport.Add(format, support);
            }

            return support;
        }
    }
}
