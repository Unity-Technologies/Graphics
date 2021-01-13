// This file contain the two main data structures controlled by the XRSystem.
// XRView contains the parameters required to render (proj and view matrices, viewport, etc)
// XRPass holds the render target information and a list of XRView.
// When a pass has 2+ views, single-pass will be active.
// To avoid allocating every frame, XRView is a struct and XRPass is pooled.

#if ENABLE_VR && ENABLE_XR_MODULE

using System;
using System.Collections.Generic;
using UnityEngine.XR;

namespace UnityEngine.Rendering.Universal
{
    internal struct XRPassCreateInfo
    {
        public int multipassId;
        public int cullingPassId;
        public RenderTexture renderTarget;
        public RenderTextureDescriptor renderTargetDesc;
        public bool renderTargetIsRenderTexture;
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
    }

    class XRPass
    {
        internal List<XRView> views = new List<XRView>(2);

        internal bool enabled      { get => views.Count > 0; }
        internal bool xrSdkEnabled { get; private set; }
        internal bool copyDepth    { get; private set; }

        internal int multipassId    { get; private set; }
        internal int cullingPassId  { get; private set; }

        // Ability to specify where to render the pass
        internal RenderTargetIdentifier  renderTarget     { get; private set; }
        internal RenderTextureDescriptor renderTargetDesc { get; private set; }
        static   RenderTargetIdentifier  invalidRT = -1;
        internal bool                    renderTargetValid { get => renderTarget != invalidRT; }
        internal bool                    renderTargetIsRenderTexture { get; private set; }

        // Access to view information
        internal Matrix4x4 GetProjMatrix(int viewIndex = 0)  { return views[viewIndex].projMatrix; }
        internal Matrix4x4 GetViewMatrix(int viewIndex = 0)  { return views[viewIndex].viewMatrix; }
        internal int GetTextureArraySlice(int viewIndex = 0) { return views[viewIndex].textureArraySlice; }
        internal Rect GetViewport(int viewIndex = 0)         { return views[viewIndex].viewport; }

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

        const string k_XRCustomMirrorTag = "XR Custom Mirror View";
        static ProfilingSampler _XRCustomMirrorProfilingSampler = new ProfilingSampler(k_XRCustomMirrorTag);
        const string k_XROcclusionTag = "XR Occlusion Mesh";
        static ProfilingSampler _XROcclusionProfilingSampler = new ProfilingSampler(k_XROcclusionTag);

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
                passInfo.renderTarget = new RenderTargetIdentifier(createInfo.renderTarget, 0, CubemapFace.Unknown, -1);
                passInfo.renderTargetDesc = createInfo.renderTarget.descriptor;
                passInfo.renderTargetIsRenderTexture = createInfo.renderTargetIsRenderTexture;
            }
            else
            {
                passInfo.renderTarget = invalidRT;
                passInfo.renderTargetDesc = createInfo.renderTargetDesc;
                passInfo.renderTargetIsRenderTexture = createInfo.renderTargetIsRenderTexture;
            }

            passInfo.occlusionMeshMaterial = null;
            passInfo.xrSdkEnabled = false;
            passInfo.copyDepth = false;

            return passInfo;
        }

        internal void UpdateView(int viewId, XRDisplaySubsystem.XRRenderPass xrSdkRenderPass, XRDisplaySubsystem.XRRenderParameter xrSdkRenderParameter)
        {
            if (viewId >= views.Count)
                throw new NotImplementedException($"Invalid XR setup to update, trying to update non-existing xr view.");

            views[viewId] = new XRView(xrSdkRenderPass, xrSdkRenderParameter);
        }

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

        internal static XRPass Create(XRDisplaySubsystem.XRRenderPass xrRenderPass, int multipassId, ScriptableCullingParameters cullingParameters, Material occlusionMeshMaterial)
        {
            XRPass passInfo = GenericPool<XRPass>.Get();

            passInfo.multipassId = multipassId;
            passInfo.cullingPassId = xrRenderPass.cullingPassIndex;
            passInfo.cullingParams = cullingParameters;
            passInfo.views.Clear();

            // URP ScriptableRenderer does not track current active depth slice state. We make sure to set all texture slices(-1) across the pipeline to ensure consistency.
            passInfo.renderTarget = new RenderTargetIdentifier(xrRenderPass.renderTarget, 0, CubemapFace.Unknown, -1);

            RenderTextureDescriptor xrDesc = xrRenderPass.renderTargetDesc;
            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(xrDesc.width, xrDesc.height, xrDesc.colorFormat, xrDesc.depthBufferBits, xrDesc.mipCount);
            rtDesc.dimension   = xrRenderPass.renderTargetDesc.dimension;
            rtDesc.volumeDepth = xrRenderPass.renderTargetDesc.volumeDepth;
            rtDesc.vrUsage     = xrRenderPass.renderTargetDesc.vrUsage;
            rtDesc.sRGB        = xrRenderPass.renderTargetDesc.sRGB;

            // Can't use xr descriptor directly as its descriptor force off y-flip cap
            //passInfo.renderTargetDesc = xrDesc;
            passInfo.renderTargetDesc = rtDesc;

            // Eye textures are back buffer type internally (See c++ core XRTextureManager)
            passInfo.renderTargetIsRenderTexture = false;
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

        internal static void Release(XRPass xrPass)
        {
            GenericPool<XRPass>.Release(xrPass);
        }

        internal void AddViewInternal(XRView xrView)
        {
            // XRTODO: Fix hard coded max views
            int maxSupportedViews = Math.Min(TextureXR.slices, 2 /*ShaderConfig.s_XrMaxViews*/);

            if (views.Count < maxSupportedViews)
            {
                views.Add(xrView);
            }
            else
            {
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

        Vector4[] stereoEyeIndices = new Vector4[2] { Vector4.zero , Vector4.one };

        internal void StartSinglePass(CommandBuffer cmd)
        {
            if (enabled)
            {
                if (singlePassEnabled)
                {
                    if (viewCount <= TextureXR.slices)
                    {
                        if (SystemInfo.supportsMultiview)
                        {
                            cmd.EnableShaderKeyword("STEREO_MULTIVIEW_ON");
                            cmd.SetGlobalVectorArray("unity_StereoEyeIndices", stereoEyeIndices);
                        }
                        else
                        {
                            cmd.EnableShaderKeyword("STEREO_INSTANCING_ON");
                            cmd.SetInstanceMultiplier((uint)viewCount);
                        }
                    }
                    else
                    {
                        throw new NotImplementedException($"Invalid XR setup for single-pass, trying to render too many views! Max supported: {TextureXR.slices}");
                    }
                }
            }
        }

        internal void StopSinglePass(CommandBuffer cmd)
        {
            if (enabled)
            {
                if (singlePassEnabled)
                {
                    if (SystemInfo.supportsMultiview)
                    {
                        cmd.DisableShaderKeyword("STEREO_MULTIVIEW_ON");
                    }
                    else
                    {
                        cmd.DisableShaderKeyword("STEREO_INSTANCING_ON");
                        cmd.SetInstanceMultiplier(1);
                    }
                }
            }
        }

        internal void EndCamera(CommandBuffer cmd, CameraData cameraData)
        {
            if (!enabled)
                return;

            StopSinglePass(cmd);

            // Callback for custom mirror view
            if (customMirrorView != null)
            {
                using (new ProfilingScope(cmd, _XRCustomMirrorProfilingSampler))
                {
                    customMirrorView(this, cmd, cameraData.targetTexture, cameraData.pixelRect);
                }
            }
        }

        internal void RenderOcclusionMesh(CommandBuffer cmd)
        {
            if (isOcclusionMeshSupported)
            {
                using (new ProfilingScope(cmd, _XROcclusionProfilingSampler))
                {
                    if (singlePassEnabled)
                    {
                        if (occlusionMeshCombined != null && SystemInfo.supportsRenderTargetArrayIndexFromVertexShader)
                        {
                            StopSinglePass(cmd);

                            cmd.EnableShaderKeyword("XR_OCCLUSION_MESH_COMBINED");
                            cmd.DrawMesh(occlusionMeshCombined, Matrix4x4.identity, occlusionMeshMaterial);
                            cmd.DisableShaderKeyword("XR_OCCLUSION_MESH_COMBINED");

                            StartSinglePass(cmd);
                        }
                    }
                    else if (views[0].occlusionMesh != null)
                    {
                        cmd.DrawMesh(views[0].occlusionMesh, Matrix4x4.identity, occlusionMeshMaterial);
                    }
                }
            }
        }

        // Store array to avoid allocating every frame
        private Matrix4x4[] stereoProjectionMatrix = new Matrix4x4[2];
        private Matrix4x4[] stereoViewMatrix = new Matrix4x4[2];
        private Matrix4x4[] stereoCameraProjectionMatrix = new Matrix4x4[2];

        internal void UpdateGPUViewAndProjectionMatrices(CommandBuffer cmd, ref CameraData cameraData, bool isRenderToTexture)
        {
            Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(cameraData.xr.GetProjMatrix(0), isRenderToTexture);
            RenderingUtils.SetViewAndProjectionMatrices(cmd, cameraData.xr.GetViewMatrix(0), projectionMatrix, true);

            if (cameraData.xr.singlePassEnabled)
            {
                for (int i = 0; i < 2; i++)
                {
                    stereoCameraProjectionMatrix[i] = cameraData.xr.GetProjMatrix(i);
                    stereoViewMatrix[i] = cameraData.xr.GetViewMatrix(i);
                    stereoProjectionMatrix[i] = GL.GetGPUProjectionMatrix(stereoCameraProjectionMatrix[i], isRenderToTexture);
                }
                RenderingUtils.SetStereoViewAndProjectionMatrices(cmd, stereoViewMatrix, stereoProjectionMatrix, stereoCameraProjectionMatrix, true);
            }
        }
    }
}

#else
namespace UnityEngine.Rendering.Universal
{
    internal class XRPass
    {
        internal static readonly XRPass emptyPass = new XRPass();

        internal bool enabled { get => false; }
        internal void StartSinglePass(CommandBuffer cmd) {}
        internal void StopSinglePass(CommandBuffer cmd) {}
        internal void EndCamera(CommandBuffer cmd, CameraData camera) {}
        internal void RenderOcclusionMesh(CommandBuffer cmd) {}
    }
}
#endif
