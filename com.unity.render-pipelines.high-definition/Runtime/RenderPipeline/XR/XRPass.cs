// This file contain the two main data structures controlled by the XRSystem.
// XRView contains the parameters required to render (proj and view matrices, viewport, etc)
// XRPass holds the render target information and a list of XRView.
// When a pass has 2+ views, single-pass will be active.
// To avoid allocating every frame, XRView is a struct and XRPass is pooled.

using System;
using System.Collections.Generic;

#if ENABLE_VR && ENABLE_XR_MODULE
using UnityEngine.XR;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    internal struct XRPassCreateInfo
    {
        public int multipassId;
        public int cullingPassId;
        public RenderTexture renderTarget;
        public ScriptableCullingParameters cullingParameters;
        public XRPass.CustomMirrorView customMirrorView;
    }

    internal struct XRViewCreateInfo
    {
        public Matrix4x4 projMatrix;
        public Matrix4x4 viewMatrix;
        public Rect viewport;
        public int textureArraySlice;
    }

    internal struct XRView
    {
        internal readonly Matrix4x4 projMatrix;
        internal readonly Matrix4x4 viewMatrix;
        internal readonly Rect viewport;
        internal readonly Mesh occlusionMesh;
        internal readonly int textureArraySlice;

        internal XRView(Matrix4x4 proj, Matrix4x4 view, Rect vp, int dstSlice)
        {
            projMatrix = proj;
            viewMatrix = view;
            viewport = vp;
            occlusionMesh = null;
            textureArraySlice = dstSlice;
        }

#if ENABLE_VR && ENABLE_XR_MODULE
        internal XRView(XRDisplaySubsystem.XRRenderPass renderPass, XRDisplaySubsystem.XRRenderParameter renderParameter)
        {
            projMatrix = renderParameter.projection;
            viewMatrix = renderParameter.view;
            viewport = renderParameter.viewport;
            occlusionMesh = renderParameter.occlusionMesh;
            textureArraySlice = renderParameter.textureArraySlice;

            // Convert viewport from normalized to screen space
            viewport.x      *= renderPass.renderTargetDesc.width;
            viewport.width  *= renderPass.renderTargetDesc.width;
            viewport.y      *= renderPass.renderTargetDesc.height;
            viewport.height *= renderPass.renderTargetDesc.height;
        }

#endif
    }

    class XRPass
    {
        readonly List<XRView> views = new List<XRView>(2);

        internal bool enabled { get => views.Count > 0; }
        internal bool xrSdkEnabled { get; private set; }
        internal bool copyDepth { get; private set; }

        internal int multipassId { get; private set; }
        internal int cullingPassId { get; private set; }

        // Ability to specify where to render the pass
        internal RenderTargetIdentifier renderTarget { get; private set; }
        internal RenderTextureDescriptor renderTargetDesc { get; private set; }
        static RenderTargetIdentifier invalidRT = -1;
        internal bool renderTargetValid { get => renderTarget != invalidRT; }

        // Access to view information
        internal Matrix4x4 GetProjMatrix(int viewIndex = 0) { return views[viewIndex].projMatrix; }
        internal Matrix4x4 GetViewMatrix(int viewIndex = 0) { return views[viewIndex].viewMatrix; }
        internal int GetTextureArraySlice(int viewIndex = 0) { return views[viewIndex].textureArraySlice; }
        internal Rect GetViewport(int viewIndex = 0) { return views[viewIndex].viewport; }

        // Combined projection and view matrices for culling
        internal ScriptableCullingParameters cullingParams { get; private set; }

        // Single-pass rendering support (instanced draw calls or multiview extension)
        internal int viewCount { get => views.Count; }
        internal bool singlePassEnabled { get => viewCount > 1; }

        // Occlusion mesh rendering
        Material occlusionMeshMaterial = null;
        Mesh occlusionMeshCombined = null;
        int occlusionMeshCombinedHashCode = 0;

        internal bool isOcclusionMeshSupported { get => enabled && xrSdkEnabled && occlusionMeshMaterial != null; }

        internal bool hasValidOcclusionMesh
        {
            get
            {
                if (isOcclusionMeshSupported)
                {
                    if (singlePassEnabled)
                        return occlusionMeshCombined != null;
                    else
                        return views[0].occlusionMesh != null;
                }

                return false;
            }
        }

        // Ability to override mirror view behavior for each pass
        internal delegate void CustomMirrorView(XRPass pass, CommandBuffer cmd, RenderTexture rt, Rect viewport);
        CustomMirrorView customMirrorView = null;
        internal void SetCustomMirrorView(CustomMirrorView callback) => customMirrorView = callback;

        internal static XRPass Create(XRPassCreateInfo createInfo)
        {
            XRPass passInfo = GenericPool<XRPass>.Get();

            passInfo.multipassId = createInfo.multipassId;
            passInfo.cullingPassId = createInfo.cullingPassId;
            passInfo.cullingParams = createInfo.cullingParameters;
            passInfo.customMirrorView = createInfo.customMirrorView;
            passInfo.views.Clear();

            if (createInfo.renderTarget != null)
            {
                passInfo.renderTarget = new RenderTargetIdentifier(createInfo.renderTarget);
                passInfo.renderTargetDesc = createInfo.renderTarget.descriptor;
            }
            else
            {
                passInfo.renderTarget = invalidRT;
                passInfo.renderTargetDesc = default;
            }

            passInfo.occlusionMeshMaterial = null;
            passInfo.xrSdkEnabled = false;
            passInfo.copyDepth = false;

            return passInfo;
        }

#if ENABLE_VR && ENABLE_XR_MODULE

        internal void UpdateView(int viewId, XRDisplaySubsystem.XRRenderPass xrSdkRenderPass, XRDisplaySubsystem.XRRenderParameter xrSdkRenderParameter)
        {
            if (viewId >= views.Count)
                throw new NotImplementedException($"Invalid XR setup to update, trying to update non-existing xr view.");

            views[viewId] = new XRView(xrSdkRenderPass, xrSdkRenderParameter);
        }

#endif

        internal void UpdateView(int viewId, Matrix4x4 proj, Matrix4x4 view, Rect vp, int textureArraySlice = -1)
        {
            if (viewId >= views.Count)
                throw new NotImplementedException($"Invalid XR setup to update, trying to update non-existing xr view.");

            views[viewId] = new XRView(proj, view, vp, textureArraySlice);
        }

        internal void UpdateCullingParams(int cullingPassId, ScriptableCullingParameters cullingParams)
        {
            this.cullingPassId = cullingPassId;
            this.cullingParams = cullingParams;
        }

        internal void AddView(Matrix4x4 proj, Matrix4x4 view, Rect vp, int textureArraySlice = -1)
        {
            AddViewInternal(new XRView(proj, view, vp, textureArraySlice));
        }

#if ENABLE_VR && ENABLE_XR_MODULE
        internal static XRPass Create(XRDisplaySubsystem.XRRenderPass xrRenderPass, int multipassId, ScriptableCullingParameters cullingParameters, Material occlusionMeshMaterial)
        {
            XRPass passInfo = GenericPool<XRPass>.Get();

            passInfo.multipassId = multipassId;
            passInfo.cullingPassId = xrRenderPass.cullingPassIndex;
            passInfo.cullingParams = cullingParameters;
            passInfo.views.Clear();
            passInfo.renderTarget = xrRenderPass.renderTarget;
            passInfo.renderTargetDesc = xrRenderPass.renderTargetDesc;
            passInfo.occlusionMeshMaterial = occlusionMeshMaterial;
            passInfo.xrSdkEnabled = true;
            passInfo.copyDepth = xrRenderPass.shouldFillOutDepth;
            passInfo.customMirrorView = null;

            Debug.Assert(passInfo.renderTargetValid, "Invalid render target from XRDisplaySubsystem!");

            return passInfo;
        }

        internal void AddView(XRDisplaySubsystem.XRRenderPass xrSdkRenderPass, XRDisplaySubsystem.XRRenderParameter xrSdkRenderParameter)
        {
            AddViewInternal(new XRView(xrSdkRenderPass, xrSdkRenderParameter));
        }

#endif

        internal static void Release(XRPass xrPass)
        {
            GenericPool<XRPass>.Release(xrPass);
        }

        internal void AddViewInternal(XRView xrView)
        {
            int maxSupportedViews = Math.Min(TextureXR.slices, ShaderConfig.s_XrMaxViews);

            if (views.Count < maxSupportedViews)
            {
                views.Add(xrView);
            }
            else
            {
                if (xrSdkEnabled)
                {
                    Debug.LogWarning("If you're trying to enable XR single-pass after the first frame, you need to set TextureXR.maxViews to 2 before the render pipeline is created (typically in a script with Awake()).");
                }

                throw new NotImplementedException($"Invalid XR setup for single-pass, trying to add too many views! Max supported: {maxSupportedViews}");
            }
        }

        // Must be called after all views have been added to the pass
        internal void UpdateOcclusionMesh()
        {
            if (isOcclusionMeshSupported && singlePassEnabled && TryGetOcclusionMeshCombinedHashCode(out var hashCode))
            {
                if (occlusionMeshCombined == null || hashCode != occlusionMeshCombinedHashCode)
                {
                    CreateOcclusionMeshCombined();
                    occlusionMeshCombinedHashCode = hashCode;
                }
            }
            else
            {
                occlusionMeshCombined = null;
                occlusionMeshCombinedHashCode = 0;
            }
        }

        private bool TryGetOcclusionMeshCombinedHashCode(out int hashCode)
        {
            hashCode = 17;

            for (int viewId = 0; viewId < viewCount; ++viewId)
            {
                if (views[viewId].occlusionMesh != null)
                {
                    hashCode = hashCode * 23 + views[viewId].occlusionMesh.GetHashCode();
                }
                else
                {
                    hashCode = 0;
                    return false;
                }
            }

            return true;
        }

        // Create a new mesh that contains the occlusion data from all views
        private void CreateOcclusionMeshCombined()
        {
            occlusionMeshCombined = new Mesh();
            occlusionMeshCombined.indexFormat = IndexFormat.UInt16;

            int combinedVertexCount = 0;
            uint combinedIndexCount = 0;

            for (int viewId = 0; viewId < viewCount; ++viewId)
            {
                Mesh mesh = views[viewId].occlusionMesh;

                Debug.Assert(mesh != null);
                Debug.Assert(mesh.subMeshCount == 1);
                Debug.Assert(mesh.indexFormat == IndexFormat.UInt16);

                combinedVertexCount += mesh.vertexCount;
                combinedIndexCount += mesh.GetIndexCount(0);
            }

            Vector3[] vertices = new Vector3[combinedVertexCount];
            ushort[] indices = new ushort[combinedIndexCount];
            int vertexStart = 0;
            int indexStart = 0;

            for (int viewId = 0; viewId < viewCount; ++viewId)
            {
                Mesh mesh = views[viewId].occlusionMesh;
                var meshIndices = mesh.GetIndices(0);

                // Encore the viewId into the z channel
                {
                    mesh.vertices.CopyTo(vertices, vertexStart);

                    for (int i = 0; i < mesh.vertices.Length; i++)
                        vertices[vertexStart + i].z = viewId;
                }

                // Combine indices into one buffer
                for (int i = 0; i < meshIndices.Length; i++)
                {
                    int newIndex = vertexStart + meshIndices[i];
                    Debug.Assert(meshIndices[i] < ushort.MaxValue);

                    indices[indexStart + i] = (ushort)newIndex;
                }

                vertexStart += mesh.vertexCount;
                indexStart += meshIndices.Length;
            }

            occlusionMeshCombined.vertices = vertices;
            occlusionMeshCombined.SetIndices(indices, MeshTopology.Triangles, 0);
        }

        /// <summary>
        /// Enable XR single-pass rendering.
        /// </summary>
        public void StartSinglePass(CommandBuffer cmd)
        {
            if (enabled)
            {
                // Required for some legacy shaders (text for example)
                cmd.SetViewProjectionMatrices(GetViewMatrix(), GetProjMatrix());

                if (singlePassEnabled)
                {
                    if (viewCount <= TextureXR.slices)
                    {
                        cmd.EnableShaderKeyword("STEREO_INSTANCING_ON");
                        cmd.SetInstanceMultiplier((uint)viewCount);
                    }
                    else
                    {
                        throw new NotImplementedException($"Invalid XR setup for single-pass, trying to render too many views! Max supported: {TextureXR.slices}");
                    }
                }
            }
        }

        /// <summary>
        /// Disable XR single-pass rendering.
        /// </summary>
        public void StopSinglePass(CommandBuffer cmd)
        {
            if (enabled)
            {
                cmd.DisableShaderKeyword("STEREO_INSTANCING_ON");
                cmd.SetInstanceMultiplier(1);
            }
        }

        /// <summary>Obsolete</summary>
        [Obsolete]
        public void StartSinglePass(CommandBuffer cmd, Camera camera, ScriptableRenderContext renderContext)
        {
            StartSinglePass(cmd);
        }

        /// <summary>Obsolete</summary>
        [Obsolete]
        public void StopSinglePass(CommandBuffer cmd, Camera camera, ScriptableRenderContext renderContext)
        {
            StopSinglePass(cmd);
        }

        internal void EndCamera(CommandBuffer cmd, HDCamera hdCamera)
        {
            if (!enabled)
                return;

            StopSinglePass(cmd);

            // Callback for custom mirror view
            if (customMirrorView != null)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.XRCustomMirrorView)))
                {
                    customMirrorView(this, cmd, hdCamera.camera.targetTexture, hdCamera.camera.pixelRect);
                }
            }
        }

        internal void RenderOcclusionMeshes(CommandBuffer cmd, Color clearColor, RTHandle colorBuffer, RTHandle depthBuffer)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (XRGraphicsAutomatedTests.enabled && XRGraphicsAutomatedTests.running)
                return;
#endif

            if (isOcclusionMeshSupported)
            {
                CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer, ClearFlag.None, clearColor, 0, CubemapFace.Unknown, -1);
                cmd.SetGlobalVector(HDShaderIDs._ClearColor, clearColor);

                if (singlePassEnabled)
                {
                    if (occlusionMeshCombined != null && SystemInfo.supportsRenderTargetArrayIndexFromVertexShader)
                    {
                        cmd.EnableShaderKeyword("XR_OCCLUSION_MESH_COMBINED");
                        cmd.DrawMesh(occlusionMeshCombined, Matrix4x4.identity, occlusionMeshMaterial);
                        cmd.DisableShaderKeyword("XR_OCCLUSION_MESH_COMBINED");
                    }
                }
                else if (views[0].occlusionMesh != null)
                {
                    cmd.DrawMesh(views[0].occlusionMesh, Matrix4x4.identity, occlusionMeshMaterial);
                }
            }
        }

        static readonly int _unity_StereoMatrixV = Shader.PropertyToID("unity_StereoMatrixV");
        static readonly int _unity_StereoMatrixP = Shader.PropertyToID("unity_StereoMatrixP");
        static readonly int _unity_StereoMatrixVP = Shader.PropertyToID("unity_StereoMatrixVP");
        static readonly int _unity_StereoWorldSpaceCameraPos = Shader.PropertyToID("unity_StereoWorldSpaceCameraPos");
        Matrix4x4[] builtinViewMatrix = new Matrix4x4[2];
        Matrix4x4[] builtinProjMatrix = new Matrix4x4[2];
        Matrix4x4[] builtinViewProjMatrix = new Matrix4x4[2];
        Vector4[] builtinWorldSpaceCameraPos = new Vector4[2];

        // Maintain compatibility with builtin renderer
        internal void UpdateBuiltinStereoMatrices(CommandBuffer cmd, HDCamera hdCamera)
        {
            if (singlePassEnabled)
            {
                for (int viewIndex = 0; viewIndex < 2; ++viewIndex)
                {
                    builtinWorldSpaceCameraPos[viewIndex] = hdCamera.m_XRViewConstants[viewIndex].worldSpaceCameraPos;
                    builtinViewMatrix[viewIndex] = GetViewMatrix(viewIndex);
                    builtinProjMatrix[viewIndex] = GL.GetGPUProjectionMatrix(GetProjMatrix(viewIndex), true);
                    builtinViewProjMatrix[viewIndex] = builtinProjMatrix[viewIndex] * builtinViewMatrix[viewIndex];
                }

                cmd.SetGlobalMatrixArray(_unity_StereoMatrixV, builtinViewMatrix);
                cmd.SetGlobalMatrixArray(_unity_StereoMatrixP, builtinProjMatrix);
                cmd.SetGlobalMatrixArray(_unity_StereoMatrixVP, builtinViewProjMatrix);
                cmd.SetGlobalVectorArray(_unity_StereoWorldSpaceCameraPos, builtinWorldSpaceCameraPos);
            }
        }
    }
}
