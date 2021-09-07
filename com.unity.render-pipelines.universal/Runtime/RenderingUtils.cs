using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Contains properties and helper functions that you can use when rendering.
    /// </summary>
    public static class RenderingUtils
    {
        static List<ShaderTagId> m_LegacyShaderPassNames = new List<ShaderTagId>
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM"),
        };

        static AttachmentDescriptor s_EmptyAttachment = new AttachmentDescriptor(GraphicsFormat.None);
        internal static AttachmentDescriptor emptyAttachment
        {
            get
            {
                return s_EmptyAttachment;
            }
        }


        static Mesh s_FullscreenMesh = null;

        /// <summary>
        /// Returns a mesh that you can use with <see cref="CommandBuffer.DrawMesh(Mesh, Matrix4x4, Material)"/> to render full-screen effects.
        /// </summary>
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

        internal static bool useStructuredBuffer
        {
            // There are some performance issues with StructuredBuffers in some platforms.
            // We fallback to UBO in those cases.
            get
            {
                // TODO: For now disabling SSBO until figure out Vulkan binding issues.
                // When enabling this also enable USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA in shader side in Input.hlsl
                return false;

                // We don't use SSBO in D3D because we can't figure out without adding shader variants if platforms is D3D10.
                //GraphicsDeviceType deviceType = SystemInfo.graphicsDeviceType;
                //return !Application.isMobilePlatform &&
                //    (deviceType == GraphicsDeviceType.Metal || deviceType == GraphicsDeviceType.Vulkan ||
                //     deviceType == GraphicsDeviceType.PlayStation4 || deviceType == GraphicsDeviceType.PlayStation5 || deviceType == GraphicsDeviceType.XboxOne);
            }
        }

        internal static bool SupportsLightLayers(GraphicsDeviceType type)
        {
            // GLES2 does not support bitwise operations.
            return type != GraphicsDeviceType.OpenGLES2;
        }

        static Material s_ErrorMaterial;
        static Material errorMaterial
        {
            get
            {
                if (s_ErrorMaterial == null)
                {
                    // TODO: When importing project, AssetPreviewUpdater::CreatePreviewForAsset will be called multiple times.
                    // This might be in a point that some resources required for the pipeline are not finished importing yet.
                    // Proper fix is to add a fence on asset import.
                    try
                    {
                        s_ErrorMaterial = new Material(Shader.Find("Hidden/Universal Render Pipeline/FallbackError"));
                    }
                    catch { }
                }

                return s_ErrorMaterial;
            }
        }

        /// <summary>
        /// Set view and projection matrices.
        /// This function will set <c>UNITY_MATRIX_V</c>, <c>UNITY_MATRIX_P</c>, <c>UNITY_MATRIX_VP</c> to given view and projection matrices.
        /// If <c>setInverseMatrices</c> is set to true this function will also set <c>UNITY_MATRIX_I_V</c> and <c>UNITY_MATRIX_I_VP</c>.
        /// </summary>
        /// <param name="cmd">CommandBuffer to submit data to GPU.</param>
        /// <param name="viewMatrix">View matrix to be set.</param>
        /// <param name="projectionMatrix">Projection matrix to be set.</param>
        /// <param name="setInverseMatrices">Set this to true if you also need to set inverse camera matrices.</param>
        public static void SetViewAndProjectionMatrices(CommandBuffer cmd, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, bool setInverseMatrices)
        {
            Matrix4x4 viewAndProjectionMatrix = projectionMatrix * viewMatrix;
            cmd.SetGlobalMatrix(ShaderPropertyId.viewMatrix, viewMatrix);
            cmd.SetGlobalMatrix(ShaderPropertyId.projectionMatrix, projectionMatrix);
            cmd.SetGlobalMatrix(ShaderPropertyId.viewAndProjectionMatrix, viewAndProjectionMatrix);

            if (setInverseMatrices)
            {
                Matrix4x4 inverseViewMatrix = Matrix4x4.Inverse(viewMatrix);
                Matrix4x4 inverseProjectionMatrix = Matrix4x4.Inverse(projectionMatrix);
                Matrix4x4 inverseViewProjection = inverseViewMatrix * inverseProjectionMatrix;
                cmd.SetGlobalMatrix(ShaderPropertyId.inverseViewMatrix, inverseViewMatrix);
                cmd.SetGlobalMatrix(ShaderPropertyId.inverseProjectionMatrix, inverseProjectionMatrix);
                cmd.SetGlobalMatrix(ShaderPropertyId.inverseViewAndProjectionMatrix, inverseViewProjection);
            }
        }

#if ENABLE_VR && ENABLE_XR_MODULE
        internal static readonly int UNITY_STEREO_MATRIX_V = Shader.PropertyToID("unity_StereoMatrixV");
        internal static readonly int UNITY_STEREO_MATRIX_IV = Shader.PropertyToID("unity_StereoMatrixInvV");
        internal static readonly int UNITY_STEREO_MATRIX_P = Shader.PropertyToID("unity_StereoMatrixP");
        internal static readonly int UNITY_STEREO_MATRIX_IP = Shader.PropertyToID("unity_StereoMatrixInvP");
        internal static readonly int UNITY_STEREO_MATRIX_VP = Shader.PropertyToID("unity_StereoMatrixVP");
        internal static readonly int UNITY_STEREO_MATRIX_IVP = Shader.PropertyToID("unity_StereoMatrixInvVP");
        internal static readonly int UNITY_STEREO_CAMERA_PROJECTION = Shader.PropertyToID("unity_StereoCameraProjection");
        internal static readonly int UNITY_STEREO_CAMERA_INV_PROJECTION = Shader.PropertyToID("unity_StereoCameraInvProjection");
        internal static readonly int UNITY_STEREO_VECTOR_CAMPOS = Shader.PropertyToID("unity_StereoWorldSpaceCameraPos");

        // Hold the stereo matrices in this class to avoid allocating arrays every frame
        internal class StereoConstants
        {
            public Matrix4x4[] viewProjMatrix = new Matrix4x4[2];
            public Matrix4x4[] invViewMatrix = new Matrix4x4[2];
            public Matrix4x4[] invProjMatrix = new Matrix4x4[2];
            public Matrix4x4[] invViewProjMatrix = new Matrix4x4[2];
            public Matrix4x4[] invCameraProjMatrix = new Matrix4x4[2];
            public Vector4[] worldSpaceCameraPos = new Vector4[2];
        };

        static readonly StereoConstants stereoConstants = new StereoConstants();

        /// <summary>
        /// Helper function to set all view and projection related matrices
        /// Should be called before draw call and after cmd.SetRenderTarget
        /// Internal usage only, function name and signature may be subject to change
        /// </summary>
        /// <param name="cmd">CommandBuffer to submit data to GPU.</param>
        /// <param name="viewMatrix">View matrix to be set. Array size is 2.</param>
        /// <param name="projectionMatrix">Projection matrix to be set.Array size is 2.</param>
        /// <param name="cameraProjectionMatrix">Camera projection matrix to be set.Array size is 2. Does not include platform specific transformations such as depth-reverse, depth range in post-projective space and y-flip. </param>
        /// <param name="setInverseMatrices">Set this to true if you also need to set inverse camera matrices.</param>
        /// <returns>Void</c></returns>
        internal static void SetStereoViewAndProjectionMatrices(CommandBuffer cmd, Matrix4x4[] viewMatrix, Matrix4x4[] projMatrix, Matrix4x4[] cameraProjMatrix, bool setInverseMatrices)
        {
            for (int i = 0; i < 2; i++)
            {
                stereoConstants.viewProjMatrix[i] = projMatrix[i] * viewMatrix[i];
                stereoConstants.invViewMatrix[i] = Matrix4x4.Inverse(viewMatrix[i]);
                stereoConstants.invProjMatrix[i] = Matrix4x4.Inverse(projMatrix[i]);
                stereoConstants.invViewProjMatrix[i] = Matrix4x4.Inverse(stereoConstants.viewProjMatrix[i]);
                stereoConstants.invCameraProjMatrix[i] = Matrix4x4.Inverse(cameraProjMatrix[i]);
                stereoConstants.worldSpaceCameraPos[i] = stereoConstants.invViewMatrix[i].GetColumn(3);
            }

            cmd.SetGlobalMatrixArray(UNITY_STEREO_MATRIX_V, viewMatrix);
            cmd.SetGlobalMatrixArray(UNITY_STEREO_MATRIX_P, projMatrix);
            cmd.SetGlobalMatrixArray(UNITY_STEREO_MATRIX_VP, stereoConstants.viewProjMatrix);

            cmd.SetGlobalMatrixArray(UNITY_STEREO_CAMERA_PROJECTION, cameraProjMatrix);

            if (setInverseMatrices)
            {
                cmd.SetGlobalMatrixArray(UNITY_STEREO_MATRIX_IV, stereoConstants.invViewMatrix);
                cmd.SetGlobalMatrixArray(UNITY_STEREO_MATRIX_IP, stereoConstants.invProjMatrix);
                cmd.SetGlobalMatrixArray(UNITY_STEREO_MATRIX_IVP, stereoConstants.invViewProjMatrix);

                cmd.SetGlobalMatrixArray(UNITY_STEREO_CAMERA_INV_PROJECTION, stereoConstants.invCameraProjMatrix);
            }
            cmd.SetGlobalVectorArray(UNITY_STEREO_VECTOR_CAMPOS, stereoConstants.worldSpaceCameraPos);
        }

#endif

        internal static void Blit(CommandBuffer cmd,
            RenderTargetIdentifier source,
            RenderTargetIdentifier destination,
            Material material,
            int passIndex = 0,
            bool useDrawProcedural = false,
            RenderBufferLoadAction colorLoadAction = RenderBufferLoadAction.Load,
            RenderBufferStoreAction colorStoreAction = RenderBufferStoreAction.Store,
            RenderBufferLoadAction depthLoadAction = RenderBufferLoadAction.Load,
            RenderBufferStoreAction depthStoreAction = RenderBufferStoreAction.Store)
        {
            cmd.SetGlobalTexture(ShaderPropertyId.sourceTex, source);
            if (useDrawProcedural)
            {
                Vector4 scaleBias = new Vector4(1, 1, 0, 0);
                Vector4 scaleBiasRt = new Vector4(1, 1, 0, 0);
                cmd.SetGlobalVector(ShaderPropertyId.scaleBias, scaleBias);
                cmd.SetGlobalVector(ShaderPropertyId.scaleBiasRt, scaleBiasRt);
                cmd.SetRenderTarget(new RenderTargetIdentifier(destination, 0, CubemapFace.Unknown, -1),
                    colorLoadAction, colorStoreAction, depthLoadAction, depthStoreAction);
                cmd.DrawProcedural(Matrix4x4.identity, material, passIndex, MeshTopology.Quads, 4, 1, null);
            }
            else
            {
                cmd.SetRenderTarget(destination, colorLoadAction, colorStoreAction, depthLoadAction, depthStoreAction);
                cmd.Blit(source, BuiltinRenderTextureType.CurrentActive, material, passIndex);
            }
        }

        // This is used to render materials that contain built-in shader passes not compatible with URP.
        // It will render those legacy passes with error/pink shader.
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        internal static void RenderObjectsWithError(ScriptableRenderContext context, ref CullingResults cullResults, Camera camera, FilteringSettings filterSettings, SortingCriteria sortFlags)
        {
            // TODO: When importing project, AssetPreviewUpdater::CreatePreviewForAsset will be called multiple times.
            // This might be in a point that some resources required for the pipeline are not finished importing yet.
            // Proper fix is to add a fence on asset import.
            if (errorMaterial == null)
                return;

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

        // Caches render texture format support. SystemInfo.SupportsRenderTextureFormat and IsFormatSupported allocate memory due to boxing.
        static Dictionary<RenderTextureFormat, bool> m_RenderTextureFormatSupport = new Dictionary<RenderTextureFormat, bool>();
        static Dictionary<GraphicsFormat, Dictionary<FormatUsage, bool>> m_GraphicsFormatSupport = new Dictionary<GraphicsFormat, Dictionary<FormatUsage, bool>>();

        internal static void ClearSystemInfoCache()
        {
            m_RenderTextureFormatSupport.Clear();
            m_GraphicsFormatSupport.Clear();
        }

        /// <summary>
        /// Checks if a render texture format is supported by the run-time system.
        /// Similar to <see cref="SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat)"/>, but doesn't allocate memory.
        /// </summary>
        /// <param name="format">The format to look up.</param>
        /// <returns>Returns true if the graphics card supports the given <c>RenderTextureFormat</c></returns>
        public static bool SupportsRenderTextureFormat(RenderTextureFormat format)
        {
            if (!m_RenderTextureFormatSupport.TryGetValue(format, out var support))
            {
                support = SystemInfo.SupportsRenderTextureFormat(format);
                m_RenderTextureFormatSupport.Add(format, support);
            }

            return support;
        }

        /// <summary>
        /// Checks if a texture format is supported by the run-time system.
        /// Similar to <see cref="SystemInfo.IsFormatSupported"/>, but doesn't allocate memory.
        /// </summary>
        /// <param name="format">The format to look up.</param>
        /// <param name="usage">The format usage to look up.</param>
        /// <returns>Returns true if the graphics card supports the given <c>GraphicsFormat</c></returns>
        public static bool SupportsGraphicsFormat(GraphicsFormat format, FormatUsage usage)
        {
            bool support = false;
            if (!m_GraphicsFormatSupport.TryGetValue(format, out var uses))
            {
                uses = new Dictionary<FormatUsage, bool>();
                support = SystemInfo.IsFormatSupported(format, usage);
                uses.Add(usage, support);
                m_GraphicsFormatSupport.Add(format, uses);
            }
            else
            {
                if (!uses.TryGetValue(usage, out support))
                {
                    support = SystemInfo.IsFormatSupported(format, usage);
                    uses.Add(usage, support);
                }
            }

            return support;
        }

        /// <summary>
        /// Return the last colorBuffer index actually referring to an existing RenderTarget
        /// </summary>
        /// <param name="colorBuffers"></param>
        /// <returns></returns>
        internal static int GetLastValidColorBufferIndex(RenderTargetIdentifier[] colorBuffers)
        {
            int i = colorBuffers.Length - 1;
            for (; i >= 0; --i)
            {
                if (colorBuffers[i] != 0)
                    break;
            }
            return i;
        }

        /// <summary>
        /// Return the number of items in colorBuffers actually referring to an existing RenderTarget
        /// </summary>
        /// <param name="colorBuffers"></param>
        /// <returns></returns>
        internal static uint GetValidColorBufferCount(RenderTargetIdentifier[] colorBuffers)
        {
            uint nonNullColorBuffers = 0;
            if (colorBuffers != null)
            {
                foreach (var identifier in colorBuffers)
                {
                    if (identifier != 0)
                        ++nonNullColorBuffers;
                }
            }
            return nonNullColorBuffers;
        }

        /// <summary>
        /// Return true if colorBuffers is an actual MRT setup
        /// </summary>
        /// <param name="colorBuffers"></param>
        /// <returns></returns>
        internal static bool IsMRT(RenderTargetIdentifier[] colorBuffers)
        {
            return GetValidColorBufferCount(colorBuffers) > 1;
        }

        /// <summary>
        /// Return true if value can be found in source (without recurring to Linq)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static bool Contains(RenderTargetIdentifier[] source, RenderTargetIdentifier value)
        {
            foreach (var identifier in source)
            {
                if (identifier == value)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Return the index where value was found source. Otherwise, return -1. (without recurring to Linq)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static int IndexOf(RenderTargetIdentifier[] source, RenderTargetIdentifier value)
        {
            for (int i = 0; i < source.Length; ++i)
            {
                if (source[i] == value)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Return the number of RenderTargetIdentifiers in "source" that are valid (not 0) and different from "value" (without recurring to Linq)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static uint CountDistinct(RenderTargetIdentifier[] source, RenderTargetIdentifier value)
        {
            uint count = 0;
            for (int i = 0; i < source.Length; ++i)
            {
                if (source[i] != value && source[i] != 0)
                    ++count;
            }
            return count;
        }

        /// <summary>
        /// Return the index of last valid (i.e different from 0) RenderTargetIdentifiers in "source" (without recurring to Linq)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        internal static int LastValid(RenderTargetIdentifier[] source)
        {
            for (int i = source.Length - 1; i >= 0; --i)
            {
                if (source[i] != 0)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Return true if ClearFlag a contains ClearFlag b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        internal static bool Contains(ClearFlag a, ClearFlag b)
        {
            return (a & b) == b;
        }

        /// <summary>
        /// Return true if "left" and "right" are the same (without recurring to Linq)
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        internal static bool SequenceEqual(RenderTargetIdentifier[] left, RenderTargetIdentifier[] right)
        {
            if (left.Length != right.Length)
                return false;

            for (int i = 0; i < left.Length; ++i)
                if (left[i] != right[i])
                    return false;

            return true;
        }
    }
}
