using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
        [Obsolete("Use Blitter.BlitCameraTexture instead of CommandBuffer.DrawMesh(fullscreenMesh, ...)")]  // TODO OBSOLETE: need to fix the URP test failures when bumping
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
            return true;
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
        public static void SetViewAndProjectionMatrices(CommandBuffer cmd, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, bool setInverseMatrices) { SetViewAndProjectionMatrices(CommandBufferHelpers.GetRasterCommandBuffer(cmd), viewMatrix, projectionMatrix, setInverseMatrices); }
        
        /// <summary>
        /// Set view and projection matrices.
        /// This function will set <c>UNITY_MATRIX_V</c>, <c>UNITY_MATRIX_P</c>, <c>UNITY_MATRIX_VP</c> to given view and projection matrices.
        /// If <c>setInverseMatrices</c> is set to true this function will also set <c>UNITY_MATRIX_I_V</c> and <c>UNITY_MATRIX_I_VP</c>.
        /// </summary>
        /// <param name="cmd">RasterCommandBuffer to submit data to GPU.</param>
        /// <param name="viewMatrix">View matrix to be set.</param>
        /// <param name="projectionMatrix">Projection matrix to be set.</param>
        /// <param name="setInverseMatrices">Set this to true if you also need to set inverse camera matrices.</param>        
        public static void SetViewAndProjectionMatrices(RasterCommandBuffer cmd, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, bool setInverseMatrices)
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

        //TODO FrameData: Merge these two SetScaleBiasRt() functions
        internal static void SetScaleBiasRt(RasterCommandBuffer cmd, in UniversalCameraData cameraData, RTHandle rTHandle)
        {
            // SetRenderTarget has logic to flip projection matrix when rendering to render texture. Flip the uv to account for that case.
            bool isCameraColorFinalTarget = (cameraData.cameraType == CameraType.Game && rTHandle.nameID == BuiltinRenderTextureType.CameraTarget && cameraData.camera.targetTexture == null);
            bool yflip = !isCameraColorFinalTarget;
            float flipSign = yflip ? -1.0f : 1.0f;
            Vector4 scaleBiasRt = (flipSign < 0.0f)
                ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
            cmd.SetGlobalVector(Shader.PropertyToID("_ScaleBiasRt"), scaleBiasRt);
        }

        internal static void SetScaleBiasRt(RasterCommandBuffer cmd, in RenderingData renderingData)
        {
            var renderer = renderingData.cameraData.renderer;

            // SetRenderTarget has logic to flip projection matrix when rendering to render texture. Flip the uv to account for that case.
            CameraData cameraData = renderingData.cameraData;

            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            bool isCameraColorFinalTarget = (cameraData.cameraType == CameraType.Game && renderer.cameraColorTargetHandle.nameID == BuiltinRenderTextureType.CameraTarget && cameraData.camera.targetTexture == null);
            #pragma warning restore CS0618

            bool yflip = !isCameraColorFinalTarget;
            float flipSign = yflip ? -1.0f : 1.0f;

            Vector4 scaleBiasRt = (flipSign < 0.0f)
                ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);

            cmd.SetGlobalVector(Shader.PropertyToID("_ScaleBiasRt"), scaleBiasRt);
        }

        internal static void Blit(CommandBuffer cmd,
            RTHandle source,
            Rect viewport,
            RTHandle destination,
            RenderBufferLoadAction loadAction,
            RenderBufferStoreAction storeAction,
            ClearFlag clearFlag,
            Color clearColor,
            Material material,
            int passIndex = 0)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            CoreUtils.SetRenderTarget(cmd, destination, loadAction, storeAction, ClearFlag.None, Color.clear);
            cmd.SetViewport(viewport);
            Blitter.BlitTexture(cmd, source, viewportScale, material, passIndex);
        }

        internal static void Blit(CommandBuffer cmd,
            RTHandle source,
            Rect viewport,
            RTHandle destinationColor,
            RenderBufferLoadAction colorLoadAction,
            RenderBufferStoreAction colorStoreAction,
            RTHandle destinationDepthStencil,
            RenderBufferLoadAction depthStencilLoadAction,
            RenderBufferStoreAction depthStencilStoreAction,
            ClearFlag clearFlag,
            Color clearColor,
            Material material,
            int passIndex = 0)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            CoreUtils.SetRenderTarget(cmd,
                destinationColor, colorLoadAction, colorStoreAction,
                destinationDepthStencil, depthStencilLoadAction, depthStencilStoreAction,
                clearFlag, clearColor); // implicit depth=1.0f stencil=0x0
            cmd.SetViewport(viewport);
            Blitter.BlitTexture(cmd, source, viewportScale, material, passIndex);
        }

        internal static void FinalBlit(
            CommandBuffer cmd,
            UniversalCameraData cameraData,
            RTHandle source,
            RTHandle destination,
            RenderBufferLoadAction loadAction,
            RenderBufferStoreAction storeAction,
            Material material, int passIndex)
        {
            bool isRenderToBackBufferTarget = !cameraData.isSceneViewCamera;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                    isRenderToBackBufferTarget = new RenderTargetIdentifier(destination.nameID, 0, CubemapFace.Unknown, -1) == new RenderTargetIdentifier(cameraData.xr.renderTarget, 0, CubemapFace.Unknown, -1);
#endif

            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;

            // We y-flip if
            // 1) we are blitting from render texture to back buffer(UV starts at bottom) and
            // 2) renderTexture starts UV at top
            bool yflip = isRenderToBackBufferTarget && cameraData.targetTexture == null && SystemInfo.graphicsUVStartsAtTop;
            Vector4 scaleBias = yflip ? new Vector4(viewportScale.x, -viewportScale.y, 0, viewportScale.y) : new Vector4(viewportScale.x, viewportScale.y, 0, 0);
            CoreUtils.SetRenderTarget(cmd, destination, loadAction, storeAction, ClearFlag.None, Color.clear);
            if (isRenderToBackBufferTarget)
                cmd.SetViewport(cameraData.pixelRect);

            // cmd.Blit must be used in Scene View for wireframe mode to make the full screen draw with fill mode
            // This branch of the if statement must be removed for render graph and the new command list with a novel way of using Blitter with fill mode
            if (GL.wireframe && cameraData.isSceneViewCamera)
            {
                // This set render target is necessary so we change the LOAD state to DontCare.
                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,
                    loadAction, storeAction, // color
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare); // depth

                // Necessary to disable the wireframe here, since Vulkan is handling the wireframe differently
                // to handle the Terrain "Draw Instanced" scenario (Ono: case-1205332).
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
                {
                    cmd.SetWireframe(false);
                    cmd.Blit(source, destination);
                    cmd.SetWireframe(true);
                }
                else
                {
                    cmd.Blit(source, destination);
                }
            }
            else if (source.rt == null)
                Blitter.BlitTexture(cmd, source.nameID, scaleBias, material, passIndex);  // Obsolete usage of RTHandle aliasing a RenderTargetIdentifier
            else
                Blitter.BlitTexture(cmd, source, scaleBias, material, passIndex);
        }

        // This is used to render materials that contain built-in shader passes not compatible with URP.
        // It will render those legacy passes with error/pink shader.
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        internal static void CreateRendererParamsObjectsWithError(ref CullingResults cullResults, Camera camera, FilteringSettings filterSettings, SortingCriteria sortFlags, ref RendererListParams param)
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

            param = new RendererListParams(cullResults, errorSettings, filterSettings);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        internal static void CreateRendererListObjectsWithError(ScriptableRenderContext context, ref CullingResults cullResults, Camera camera, FilteringSettings filterSettings, SortingCriteria sortFlags, ref RendererList rl)
        {
            // TODO: When importing project, AssetPreviewUpdater::CreatePreviewForAsset will be called multiple times.
            // This might be in a point that some resources required for the pipeline are not finished importing yet.
            // Proper fix is to add a fence on asset import.
            if (errorMaterial == null)
            {
                rl = RendererList.nullRendererList;
                return;
            }

            RendererListParams param = new RendererListParams();
            CreateRendererParamsObjectsWithError(ref cullResults, camera, filterSettings, sortFlags, ref param);
            rl = context.CreateRendererList(ref param);
        }

        // This is used to render materials that contain built-in shader passes not compatible with URP.
        // It will render those legacy passes with error/pink shader.
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        internal static void CreateRendererListObjectsWithError(RenderGraph renderGraph, ref CullingResults cullResults, Camera camera, FilteringSettings filterSettings, SortingCriteria sortFlags, ref RendererListHandle rl)
        {
            // TODO: When importing project, AssetPreviewUpdater::CreatePreviewForAsset will be called multiple times.
            // This might be in a point that some resources required for the pipeline are not finished importing yet.
            // Proper fix is to add a fence on asset import.
            if (errorMaterial == null)
            {
                rl = new RendererListHandle();
                return;
            }

            RendererListParams param = new RendererListParams();
            CreateRendererParamsObjectsWithError(ref cullResults, camera, filterSettings, sortFlags, ref param);
            rl = renderGraph.CreateRendererList(param);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        internal static void DrawRendererListObjectsWithError(RasterCommandBuffer cmd, ref RendererList rl)
        {
            cmd.DrawRendererList(rl);
        }

        // Create a RendererList using a RenderStateBlock override is quite common so we have this optimized utility function for it
        internal static void CreateRendererListWithRenderStateBlock(ScriptableRenderContext context, ref CullingResults cullResults, DrawingSettings ds, FilteringSettings fs, RenderStateBlock rsb, ref RendererList rl)
        {
            RendererListParams param = new RendererListParams();
            unsafe
            {
                // Taking references to stack variables in the current function does not require any pinning (as long as you stay within the scope)
                // so we can safely alias it as a native array
                RenderStateBlock* rsbPtr = &rsb;
                var stateBlocks = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<RenderStateBlock>(rsbPtr, 1, Allocator.None);

                var shaderTag = ShaderTagId.none;
                var tagValues = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ShaderTagId>(&shaderTag, 1, Allocator.None);

                // Inside CreateRendererList (below), we pass the NativeArrays to C++ by calling GetUnsafeReadOnlyPtr
                // This will check read access but NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray does not set up the SafetyHandle (by design) so create/add it here
                // NOTE: we explicitly share the handle
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var safetyHandle = AtomicSafetyHandle.Create();
                AtomicSafetyHandle.SetAllowReadOrWriteAccess(safetyHandle, true);

                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref stateBlocks, safetyHandle);
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref tagValues, safetyHandle);
#endif

                // Create & schedule the RL
                param = new RendererListParams(cullResults, ds, fs)
                {
                    tagValues = tagValues,
                    stateBlocks = stateBlocks

                };

                rl = context.CreateRendererList(ref param);

                // we need to explicitly release the SafetyHandle
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.Release(safetyHandle);
#endif
            }
        }

        static ShaderTagId[] s_ShaderTagValues = new ShaderTagId[1];
        static RenderStateBlock[] s_RenderStateBlocks = new RenderStateBlock[1];
        // Create a RendererList using a RenderStateBlock override is quite common so we have this optimized utility function for it
        internal static void CreateRendererListWithRenderStateBlock(RenderGraph renderGraph, ref CullingResults cullResults, DrawingSettings ds, FilteringSettings fs, RenderStateBlock rsb, ref RendererListHandle rl)
        {
            s_ShaderTagValues[0] = ShaderTagId.none;
            s_RenderStateBlocks[0] = rsb;
            NativeArray<ShaderTagId> tagValues = new NativeArray<ShaderTagId>(s_ShaderTagValues, Allocator.Temp);
            NativeArray<RenderStateBlock> stateBlocks = new NativeArray<RenderStateBlock>(s_RenderStateBlocks, Allocator.Temp);
            var param = new RendererListParams(cullResults, ds, fs)
            {
                tagValues = tagValues,
                stateBlocks = stateBlocks,
                isPassTagName = false
            };
            rl = renderGraph.CreateRendererList(param);
        }

        // Caches render texture format support. SystemInfo.SupportsRenderTextureFormat allocates memory due to boxing.
        static Dictionary<RenderTextureFormat, bool> m_RenderTextureFormatSupport = new Dictionary<RenderTextureFormat, bool>();

        internal static void ClearSystemInfoCache()
        {
            m_RenderTextureFormatSupport.Clear();
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
        /// Obsolete. Use <see cref="SystemInfo.IsFormatSupported"/> instead.
        /// </summary>
        /// <param name="format">The format to look up.</param>
        /// <param name="usage">The format usage to look up.</param>
        /// <returns>Returns true if the graphics card supports the given <c>GraphicsFormat</c></returns>
        [Obsolete("Use SystemInfo.IsFormatSupported instead.", false)]
        public static bool SupportsGraphicsFormat(GraphicsFormat format, FormatUsage usage)
        {
            GraphicsFormatUsage graphicsFormatUsage = (GraphicsFormatUsage)(1 << (int)usage);
            return SystemInfo.IsFormatSupported(format, graphicsFormatUsage);
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
        internal static uint GetValidColorBufferCount(RTHandle[] colorBuffers)
        {
            uint nonNullColorBuffers = 0;
            if (colorBuffers != null)
            {
                foreach (var identifier in colorBuffers)
                {
                    if (identifier != null && identifier.nameID != 0)
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
        internal static bool IsMRT(RTHandle[] colorBuffers)
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
        internal static int IndexOf(RTHandle[] source, RenderTargetIdentifier value)
        {
            for (int i = 0; i < source.Length; ++i)
            {
                if (source[i] == value)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Return the index where value was found source. Otherwise, return -1. (without recurring to Linq)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static int IndexOf(RTHandle[] source, RTHandle value) => IndexOf(source, value.nameID);

        /// <summary>
        /// Return the number of RenderTargetIdentifiers in "source" that are valid (not 0) and different from "value" (without recurring to Linq)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static uint CountDistinct(RTHandle[] source, RTHandle value)
        {
            uint count = 0;
            for (int i = 0; i < source.Length; ++i)
            {
                if (source[i] != null && source[i].nameID != 0 && source[i].nameID != value.nameID)
                    ++count;
            }
            return count;
        }

        /// <summary>
        /// Return the index of last valid (i.e different from 0) RenderTargetIdentifiers in "source" (without recurring to Linq)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        internal static int LastValid(RTHandle[] source)
        {
            for (int i = source.Length - 1; i >= 0; --i)
            {
                if (source[i] != null && source[i].nameID != 0)
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
        internal static bool SequenceEqual(RTHandle[] left, RTHandle[] right)
        {
            if (left.Length != right.Length)
                return false;

            for (int i = 0; i < left.Length; ++i)
                if (left[i]?.nameID != right[i]?.nameID)
                    return false;

            return true;
        }

        internal static bool MultisampleDepthResolveSupported()
        {
            // Temporarily disabling depth resolve a driver bug on OSX when using some AMD graphics cards. Temporarily disabling depth resolve on that platform
            // TODO: re-enable once the issue is investigated/fixed
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
                return false;

            // Should we also check if the format has stencil and check stencil resolve capability only in that case?
            return SystemInfo.supportsMultisampleResolveDepth && SystemInfo.supportsMultisampleResolveStencil;
        }

        /// <summary>
        /// Return true if handle does not match descriptor
        /// </summary>
        /// <param name="handle">RTHandle to check (can be null)</param>
        /// <param name="descriptor">Descriptor for the RTHandle to match</param>
        /// <param name="scaled">Check if the RTHandle has auto scaling enabled if not, check the widths and heights</param>
        /// <returns></returns>
        internal static bool RTHandleNeedsReAlloc(
            RTHandle handle,
            in TextureDesc descriptor,
            bool scaled)
        {
            if (handle == null || handle.rt == null)
                return true;
            if (handle.useScaling != scaled)
                return true;
            if (!scaled && (handle.rt.width != descriptor.width || handle.rt.height != descriptor.height))
                return true;
            if (handle.rt.enableShadingRate && handle.rt.graphicsFormat != descriptor.colorFormat)
                return true;

            //We should always prefer to cache data from Native to prevent duplicate copy operations when re-fetching
            var rtDescriptor = handle.rt.descriptor;
            var rtHandleFormat = (rtDescriptor.depthStencilFormat != GraphicsFormat.None) ? rtDescriptor.depthStencilFormat : rtDescriptor.graphicsFormat;
            var isShadowMap = rtDescriptor.shadowSamplingMode != ShadowSamplingMode.None;

            return
                rtHandleFormat != descriptor.format ||
                rtDescriptor.dimension != descriptor.dimension ||
                rtDescriptor.volumeDepth != descriptor.slices ||
                rtDescriptor.enableRandomWrite != descriptor.enableRandomWrite ||
                rtDescriptor.enableShadingRate != descriptor.enableShadingRate ||
                rtDescriptor.useMipMap != descriptor.useMipMap ||
                rtDescriptor.autoGenerateMips != descriptor.autoGenerateMips ||
                isShadowMap != descriptor.isShadowMap ||
                (MSAASamples)rtDescriptor.msaaSamples != descriptor.msaaSamples ||
                rtDescriptor.bindMS != descriptor.bindTextureMS ||
                rtDescriptor.useDynamicScale != descriptor.useDynamicScale ||
                rtDescriptor.useDynamicScaleExplicit != descriptor.useDynamicScaleExplicit ||
                rtDescriptor.memoryless != descriptor.memoryless ||
                handle.rt.filterMode != descriptor.filterMode ||
                handle.rt.wrapMode != descriptor.wrapMode ||
                handle.rt.anisoLevel != descriptor.anisoLevel ||
                Mathf.Abs(handle.rt.mipMapBias - descriptor.mipMapBias) > Mathf.Epsilon ||
                handle.name != descriptor.name;
        }

        /// <summary>
        /// Returns the RenderTargetIdentifier of the current camera target.
        /// </summary>
        /// <param name="renderingData"></param>
        /// <returns></returns>
        internal static RenderTargetIdentifier GetCameraTargetIdentifier(ref RenderingData renderingData)
        {
            // Note: We need to get the cameraData.targetTexture as this will get the targetTexture of the camera stack.
            // Overlay cameras need to output to the target described in the base camera while doing camera stack.
            ref CameraData cameraData = ref renderingData.cameraData;

            RenderTargetIdentifier cameraTarget = (cameraData.targetTexture != null) ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.CameraTarget;
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                if (cameraData.xr.singlePassEnabled)
                {
                    cameraTarget = cameraData.xr.renderTarget;
                }
                else
                {
                    int depthSlice = cameraData.xr.GetTextureArraySlice();
                    cameraTarget = new RenderTargetIdentifier(cameraData.xr.renderTarget, 0, CubemapFace.Unknown, depthSlice);
                }
            }
#endif

            return cameraTarget;
        }

        /// <summary>
        /// Re-allocate fixed-size RTHandle if it is not allocated or doesn't match the descriptor
        /// </summary>
        /// <param name="handle">RTHandle to check (can be null)</param>
        /// <param name="descriptor">Descriptor for the RTHandle to match</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>If an allocation was done.</returns>
        [Obsolete("This method will be removed in a future release. Please use ReAllocateHandleIfNeeded instead. #from(2023.3)")]
        public static bool ReAllocateIfNeeded(
            ref RTHandle handle,
            in RenderTextureDescriptor descriptor,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0,
            string name = "")
        {
            TextureDesc requestRTDesc = RTHandleResourcePool.CreateTextureDesc(descriptor, TextureSizeMode.Explicit, anisoLevel, 0, filterMode, wrapMode, name);
            if (RTHandleNeedsReAlloc(handle, requestRTDesc, false))
            {
                if (handle != null && handle.rt != null)
                {
                    TextureDesc currentRTDesc = RTHandleResourcePool.CreateTextureDesc(handle.rt.descriptor, TextureSizeMode.Explicit, handle.rt.anisoLevel, handle.rt.mipMapBias, handle.rt.filterMode, handle.rt.wrapMode, handle.name);
                    AddStaleResourceToPoolOrRelease(currentRTDesc, handle);
                }

                if (UniversalRenderPipeline.s_RTHandlePool.TryGetResource(requestRTDesc, out handle))
                {
                    return true;
                }
                else
                {
                    handle = RTHandles.Alloc(descriptor, filterMode, wrapMode, isShadowMap, anisoLevel, mipMapBias, name);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Re-allocate dynamically resized RTHandle if it is not allocated or doesn't match the descriptor
        /// </summary>
        /// <param name="handle">RTHandle to check (can be null)</param>
        /// <param name="scaleFactor">Constant scale for the RTHandle size computation.</param>
        /// <param name="descriptor">Descriptor for the RTHandle to match</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>If the RTHandle should be re-allocated</returns>
        [Obsolete("This method will be removed in a future release. Please use ReAllocateHandleIfNeeded instead. #from(2023.3)")]
        public static bool ReAllocateIfNeeded(
            ref RTHandle handle,
            Vector2 scaleFactor,
            in RenderTextureDescriptor descriptor,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0,
            string name = "")
        {
            var usingConstantScale = handle != null && handle.useScaling && handle.scaleFactor == scaleFactor;
            TextureDesc requestRTDesc = RTHandleResourcePool.CreateTextureDesc(descriptor, TextureSizeMode.Scale, anisoLevel, 0, filterMode, wrapMode);
            if (!usingConstantScale || RTHandleNeedsReAlloc(handle, requestRTDesc, true))
            {
                if (handle != null && handle.rt != null)
                {
                    TextureDesc currentRTDesc = RTHandleResourcePool.CreateTextureDesc(handle.rt.descriptor, TextureSizeMode.Scale, handle.rt.anisoLevel, handle.rt.mipMapBias, handle.rt.filterMode, handle.rt.wrapMode);
                    AddStaleResourceToPoolOrRelease(currentRTDesc, handle);
                }

                if (UniversalRenderPipeline.s_RTHandlePool.TryGetResource(requestRTDesc, out handle))
                {
                    return true;
                }
                else
                {
                    handle = RTHandles.Alloc(scaleFactor, descriptor, filterMode, wrapMode, isShadowMap, anisoLevel, mipMapBias, name);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Re-allocate dynamically resized RTHandle if it is not allocated or doesn't match the descriptor
        /// </summary>
        /// <param name="handle">RTHandle to check (can be null)</param>
        /// <param name="scaleFunc">Function used for the RTHandle size computation.</param>
        /// <param name="descriptor">Descriptor for the RTHandle to match</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>If an allocation was done</returns>
        [Obsolete("This method will be removed in a future release. Please use ReAllocateHandleIfNeeded instead. #from(2023.3)")]
        public static bool ReAllocateIfNeeded(
            ref RTHandle handle,
            ScaleFunc scaleFunc,
            in RenderTextureDescriptor descriptor,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            bool isShadowMap = false,
            int anisoLevel = 1,
            float mipMapBias = 0,
            string name = "")
        {
            var usingScaleFunction = handle != null && handle.useScaling && handle.scaleFactor == Vector2.zero;
            TextureDesc requestRTDesc = RTHandleResourcePool.CreateTextureDesc(descriptor, TextureSizeMode.Functor, anisoLevel, 0, filterMode, wrapMode);
            if (!usingScaleFunction || RTHandleNeedsReAlloc(handle, requestRTDesc, true))
            {
                if (handle != null && handle.rt != null)
                {
                    TextureDesc currentRTDesc = RTHandleResourcePool.CreateTextureDesc(handle.rt.descriptor, TextureSizeMode.Functor, handle.rt.anisoLevel, handle.rt.mipMapBias, handle.rt.filterMode, handle.rt.wrapMode);
                    AddStaleResourceToPoolOrRelease(currentRTDesc, handle);
                }

                if (UniversalRenderPipeline.s_RTHandlePool.TryGetResource(requestRTDesc, out handle))
                {
                    return true;
                }
                else
                {
                    handle = RTHandles.Alloc(scaleFunc, descriptor, filterMode, wrapMode, isShadowMap, anisoLevel, mipMapBias, name);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Re-allocate fixed-size RTHandle if it is not allocated or doesn't match the descriptor
        /// </summary>
        /// <param name="handle">RTHandle to check (can be null)</param>
        /// <param name="descriptor">Descriptor for the RTHandle to match</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>If an allocation was done.</returns>
        public static bool ReAllocateHandleIfNeeded(
            ref RTHandle handle,
            in RenderTextureDescriptor descriptor,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            int anisoLevel = 1,
            float mipMapBias = 0,
            string name = "")
        {
            Assertions.Assert.IsTrue(descriptor.graphicsFormat == GraphicsFormat.None ^ descriptor.depthStencilFormat == GraphicsFormat.None);

            TextureDesc requestRTDesc = RTHandleResourcePool.CreateTextureDesc(descriptor, TextureSizeMode.Explicit, anisoLevel, 0, filterMode, wrapMode, name);
            if (RTHandleNeedsReAlloc(handle, requestRTDesc, false))
            {
                if (handle != null && handle.rt != null)
                {
                    TextureDesc currentRTDesc = RTHandleResourcePool.CreateTextureDesc(handle.rt.descriptor, TextureSizeMode.Explicit, handle.rt.anisoLevel, handle.rt.mipMapBias, handle.rt.filterMode, handle.rt.wrapMode, handle.name);
                    AddStaleResourceToPoolOrRelease(currentRTDesc, handle);
                }

                if (UniversalRenderPipeline.s_RTHandlePool.TryGetResource(requestRTDesc, out handle))
                {
                    return true;
                }

                var allocInfo = CreateRTHandleAllocInfo(descriptor, filterMode, wrapMode, anisoLevel, mipMapBias, name);
                handle = RTHandles.Alloc(descriptor.width, descriptor.height, allocInfo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Re-allocate dynamically resized RTHandle if it is not allocated or doesn't match the descriptor
        /// </summary>
        /// <param name="handle">RTHandle to check (can be null)</param>
        /// <param name="scaleFactor">Constant scale for the RTHandle size computation.</param>
        /// <param name="descriptor">Descriptor for the RTHandle to match</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>If an allocation was done.</returns>
        public static bool ReAllocateHandleIfNeeded(
            ref RTHandle handle,
            Vector2 scaleFactor,
            in RenderTextureDescriptor descriptor,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            int anisoLevel = 1,
            float mipMapBias = 0,
            string name = "")
        {
            var usingConstantScale = handle != null && handle.useScaling && handle.scaleFactor == scaleFactor;
            TextureDesc requestRTDesc = RTHandleResourcePool.CreateTextureDesc(descriptor, TextureSizeMode.Scale, anisoLevel, 0, filterMode, wrapMode);
            if (!usingConstantScale || RTHandleNeedsReAlloc(handle, requestRTDesc, true))
            {
                if (handle != null && handle.rt != null)
                {
                    TextureDesc currentRTDesc = RTHandleResourcePool.CreateTextureDesc(handle.rt.descriptor, TextureSizeMode.Scale, handle.rt.anisoLevel, handle.rt.mipMapBias, handle.rt.filterMode, handle.rt.wrapMode);
                    AddStaleResourceToPoolOrRelease(currentRTDesc, handle);
                }

                if (UniversalRenderPipeline.s_RTHandlePool.TryGetResource(requestRTDesc, out handle))
                {
                    return true;
                }

                var allocInfo = CreateRTHandleAllocInfo(descriptor, filterMode, wrapMode, anisoLevel, mipMapBias, name);
                handle = RTHandles.Alloc(scaleFactor, allocInfo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Re-allocate dynamically resized RTHandle if it is not allocated or doesn't match the descriptor
        /// </summary>
        /// <param name="handle">RTHandle to check (can be null)</param>
        /// <param name="scaleFunc">Function used for the RTHandle size computation.</param>
        /// <param name="descriptor">Descriptor for the RTHandle to match</param>
        /// <param name="filterMode">Filtering mode of the RTHandle.</param>
        /// <param name="wrapMode">Addressing mode of the RTHandle.</param>
        /// <param name="anisoLevel">Anisotropic filtering level.</param>
        /// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
        /// <param name="name">Name of the RTHandle.</param>
        /// <returns>If an allocation was done.</returns>
        public static bool ReAllocateHandleIfNeeded(
            ref RTHandle handle,
            ScaleFunc scaleFunc,
            in RenderTextureDescriptor descriptor,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            int anisoLevel = 1,
            float mipMapBias = 0,
            string name = "")
        {
            var usingScaleFunction = handle != null && handle.useScaling && handle.scaleFactor == Vector2.zero;
            TextureDesc requestRTDesc = RTHandleResourcePool.CreateTextureDesc(descriptor, TextureSizeMode.Functor, anisoLevel, 0, filterMode, wrapMode);
            if (!usingScaleFunction || RTHandleNeedsReAlloc(handle, requestRTDesc, true))
            {
                if (handle != null && handle.rt != null)
                {
                    TextureDesc currentRTDesc = RTHandleResourcePool.CreateTextureDesc(handle.rt.descriptor, TextureSizeMode.Functor, handle.rt.anisoLevel, handle.rt.mipMapBias, handle.rt.filterMode, handle.rt.wrapMode);
                    AddStaleResourceToPoolOrRelease(currentRTDesc, handle);
                }

                if (UniversalRenderPipeline.s_RTHandlePool.TryGetResource(requestRTDesc, out handle))
                {
                    return true;
                }

                var allocInfo = CreateRTHandleAllocInfo(descriptor, filterMode, wrapMode, anisoLevel, mipMapBias, name);
                handle = RTHandles.Alloc(scaleFunc, allocInfo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resize the rthandle pool's max stale resource capacity. The default value is 32.
        /// Increasing the capacity may have a negative impact on the memory usage(dued to staled resources in pool).
        /// Increasing the capacity may improve runtime performance (by reducing the runtime RTHandle realloc count in multi view/multi camera setup).
        /// Setting capacity will purge the current pool. It is recommended to setup the capacity upfront and not changing it during the runtime.
        /// </summary>
        /// <param name="capacity">Max capacity to set</param>
        /// <returns> Return true if set successfully. Return false if URP is not initialized and pool does not exist yet. </returns>
        public static bool SetMaxRTHandlePoolCapacity(int capacity)
        {
            if (UniversalRenderPipeline.s_RTHandlePool == null)
                return false;

            UniversalRenderPipeline.s_RTHandlePool.staleResourceCapacity = capacity;
            return true;
        }

        /// <summary>
        /// Add stale rtHandle to pool so that it could be reused in the future.
        /// For stale rtHandle failed to add to pool(could happen when pool is reaching its max stale resource capacity), the stale resource will be released.
        /// </summary>
        internal static void AddStaleResourceToPoolOrRelease(TextureDesc desc, RTHandle handle)
        {
            if (!UniversalRenderPipeline.s_RTHandlePool.AddResourceToPool(desc, handle, Time.frameCount))
                RTHandles.Release(handle);
        }

        /// <summary>
        /// Creates <c>DrawingSettings</c> based on current the rendering state.
        /// </summary>
        /// <param name="shaderTagId">Shader pass tag to render.</param>
        /// <param name="renderingData">Current rendering state.</param>
        /// <param name="sortingCriteria">Criteria to sort objects being rendered.</param>
        /// <returns></returns>
        /// <seealso cref="DrawingSettings"/>
        static public DrawingSettings CreateDrawingSettings(ShaderTagId shaderTagId, ref RenderingData renderingData, SortingCriteria sortingCriteria)
        {
            UniversalRenderingData universalRenderingData = renderingData.frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = renderingData.frameData.Get<UniversalLightData>();

            return CreateDrawingSettings(shaderTagId, universalRenderingData, cameraData, lightData, sortingCriteria);
        }

        /// <summary>
        /// Creates <c>DrawingSettings</c> based on current the rendering state.
        /// </summary>
        /// <param name="shaderTagId">Shader pass tag to render.</param>
        /// <param name="renderingData">Current rendering state.</param>
        /// <param name="cameraData">Current camera state.</param>
        /// <param name="lightData">Current light state.</param>
        /// <param name="sortingCriteria">Criteria to sort objects being rendered.</param>
        /// <returns></returns>
        /// <seealso cref="DrawingSettings"/>
        static public DrawingSettings CreateDrawingSettings(ShaderTagId shaderTagId, UniversalRenderingData renderingData,
            UniversalCameraData cameraData, UniversalLightData lightData, SortingCriteria sortingCriteria)
        {
            bool renderGraphOn = !(GraphicsSettings.TryGetRenderPipelineSettings<RenderGraphSettings>(out var renderGraphSettings) && renderGraphSettings.enableRenderCompatibilityMode);

            Camera camera = cameraData.camera;
            SortingSettings sortingSettings = new SortingSettings(camera) { criteria = sortingCriteria };
            DrawingSettings settings = new DrawingSettings(shaderTagId, sortingSettings)
            {
                perObjectData = renderingData.perObjectData,
                mainLightIndex = lightData.mainLightIndex,
                enableDynamicBatching = renderingData.supportsDynamicBatching,

                // Disable instancing for preview cameras. This is consistent with the built-in forward renderer. Also fixes case 1127324.
                enableInstancing = camera.cameraType == CameraType.Preview ? false : true,
                // stencil-based LOD doesn't support native render pass for now.
                lodCrossFadeStencilMask = renderGraphOn && renderingData.stencilLodCrossFadeEnabled ? (int)UniversalRendererStencilRef.CrossFadeStencilRef_All : 0,
            };
            return settings;
        }

        /// <summary>
        /// Creates <c>DrawingSettings</c> based on current rendering state.
        /// </summary>
        /// <param name="shaderTagIdList">List of shader pass tag to render.</param>
        /// <param name="renderingData">Current rendering state.</param>
        /// <param name="sortingCriteria">Criteria to sort objects being rendered.</param>
        /// <returns></returns>
        /// <seealso cref="DrawingSettings"/>
        static public DrawingSettings CreateDrawingSettings(List<ShaderTagId> shaderTagIdList,
            ref RenderingData renderingData, SortingCriteria sortingCriteria)
        {
            UniversalRenderingData universalRenderingData = renderingData.frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = renderingData.frameData.Get<UniversalLightData>();
            return CreateDrawingSettings(shaderTagIdList, universalRenderingData, cameraData, lightData, sortingCriteria);
        }

        /// <summary>
        /// Creates <c>DrawingSettings</c> based on current rendering state.
        /// </summary>
        /// <param name="shaderTagIdList">List of shader pass tag to render.</param>
        /// <param name="renderingData">Current rendering state.</param>
        /// <param name="cameraData">Current camera state.</param>
        /// <param name="lightData">Current light state.</param>
        /// <param name="sortingCriteria">Criteria to sort objects being rendered.</param>
        /// <returns></returns>
        /// <seealso cref="DrawingSettings"/>
        static public DrawingSettings CreateDrawingSettings(List<ShaderTagId> shaderTagIdList,
            UniversalRenderingData renderingData, UniversalCameraData cameraData,
            UniversalLightData lightData, SortingCriteria sortingCriteria)
        {
            if (shaderTagIdList == null || shaderTagIdList.Count == 0)
            {
                Debug.LogWarning("ShaderTagId list is invalid. DrawingSettings is created with default pipeline ShaderTagId");
                return CreateDrawingSettings(new ShaderTagId("UniversalPipeline"), renderingData, cameraData, lightData, sortingCriteria);
            }

            DrawingSettings settings = CreateDrawingSettings(shaderTagIdList[0], renderingData, cameraData, lightData, sortingCriteria);
            for (int i = 1; i < shaderTagIdList.Count; ++i)
                settings.SetShaderPassName(i, shaderTagIdList[i]);
            return settings;
        }

        /// <summary>
        /// Returns the scale bias vector to use for final blits to the backbuffer, based on scaling mode and y-flip platform requirements.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="cameraData"></param>
        /// <returns></returns>
        internal static Vector4 GetFinalBlitScaleBias(RTHandle source, RTHandle destination, UniversalCameraData cameraData)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            var yflip = cameraData.IsRenderTargetProjectionMatrixFlipped(destination);
            Vector4 scaleBias = !yflip ? new Vector4(viewportScale.x, -viewportScale.y, 0, viewportScale.y) : new Vector4(viewportScale.x, viewportScale.y, 0, 0);

            return scaleBias;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RTHandleAllocInfo CreateRTHandleAllocInfo(in RenderTextureDescriptor descriptor, FilterMode filterMode, TextureWrapMode wrapMode, int anisoLevel, float mipMapBias, string name)
        {
            var actualFormat = descriptor.graphicsFormat != GraphicsFormat.None ? descriptor.graphicsFormat : descriptor.depthStencilFormat;

            // NOTE: this calls default(RTHandleAllocInfo) not RTHandleAllocInfo(string = "")
            RTHandleAllocInfo allocInfo = new RTHandleAllocInfo();
            allocInfo.slices = descriptor.volumeDepth;
            allocInfo.format = actualFormat;
            allocInfo.filterMode = filterMode;
            allocInfo.wrapModeU = wrapMode;
            allocInfo.wrapModeV = wrapMode;
            allocInfo.wrapModeW = wrapMode;
            allocInfo.dimension = descriptor.dimension;
            allocInfo.enableRandomWrite = descriptor.enableRandomWrite;
            allocInfo.enableShadingRate = descriptor.enableShadingRate;
            allocInfo.useMipMap = descriptor.useMipMap;
            allocInfo.autoGenerateMips = descriptor.autoGenerateMips;
            allocInfo.anisoLevel = anisoLevel;
            allocInfo.mipMapBias = mipMapBias;
            allocInfo.isShadowMap = descriptor.shadowSamplingMode != ShadowSamplingMode.None;
            allocInfo.msaaSamples = (MSAASamples)descriptor.msaaSamples;
            allocInfo.bindTextureMS = descriptor.bindMS;
            allocInfo.useDynamicScale = descriptor.useDynamicScale;
            allocInfo.useDynamicScaleExplicit = descriptor.useDynamicScaleExplicit;
            allocInfo.memoryless = descriptor.memoryless;
            allocInfo.vrUsage = descriptor.vrUsage;
            allocInfo.enableShadingRate = descriptor.enableShadingRate;
            allocInfo.name = name;

            return allocInfo;
        }
    }
}
