using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// Set of data used to create a XRPass object.
    /// </summary>
    public struct XRPassCreateInfo
    {
        internal RenderTargetIdentifier renderTarget;
        internal RenderTextureDescriptor renderTargetDesc;
        internal ScriptableCullingParameters cullingParameters;
        internal Material occlusionMeshMaterial;
        internal IntPtr foveatedRenderingInfo;
        internal int multipassId;
        internal int cullingPassId;
        internal bool copyDepth;

#if ENABLE_VR && ENABLE_XR_MODULE
        internal UnityEngine.XR.XRDisplaySubsystem.XRRenderPass xrSdkRenderPass;
#endif
    }

    /// <summary>
    /// XRPass holds the render target information and a list of XRView.
    /// XRView contains the parameters required to render (projection and view matrices, viewport, etc)
    /// When a pass has 2 views or more, single-pass will be active if the platform supports it.
    /// To avoid allocating every frame, XRView is a struct and XRPass is pooled.
    /// </summary>
    public class XRPass
    {
        readonly List<XRView> m_Views;
        readonly XROcclusionMesh m_OcclusionMesh;

        /// <summary>
        /// Parameterless constructor.
        /// Note: in order to avoid GC, the render pipeline should use XRPass.Create instead of this method.
        /// </summary>
        public XRPass()
        {
            m_Views = new List<XRView>(2);
            m_OcclusionMesh = new XROcclusionMesh(this);
        }

        /// <summary>
        /// Default allocator method for XRPass.
        /// </summary>
        /// <param name="createInfo"></param>
        /// <returns></returns>
        public static XRPass CreateDefault(XRPassCreateInfo createInfo)
        {
            XRPass pass = GenericPool<XRPass>.Get();
            pass.InitBase(createInfo);
            return pass;
        }

        /// <summary>
        /// Default release method. Can be overridden by render pipelines.
        /// </summary>
        virtual public void Release()
        {
            GenericPool<XRPass>.Release(this);
        }

        /// <summary>
        /// Returns true if the pass contains at least one view.
        /// </summary>
        public bool enabled
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            get => viewCount > 0;
#else
            get => false;
#endif
        }

        /// <summary>
        /// Returns true if the pass can use foveated rendering commands.
        /// </summary>
        public bool supportsFoveatedRendering
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            get => enabled && foveatedRenderingInfo != IntPtr.Zero && XRSystem.foveatedRenderingCaps != FoveatedRenderingCaps.None;
#else
            get => false;
#endif
        }

        /// <summary>
        /// If true, the render pipeline is expected to output a valid depth buffer to the renderTarget.
        /// </summary>
        public bool copyDepth { get; private set; }

        /// <summary>
        /// Index of the pass inside the frame.
        /// </summary>
        public int multipassId { get; private set; }

        /// <summary>
        /// Index used for culling. It can be shared between multiple passes.
        /// </summary>
        public int cullingPassId { get; private set; }

        /// <summary>
        /// Destination render target.
        /// </summary>
        public RenderTargetIdentifier renderTarget { get; private set; }

        /// <summary>
        /// Destination render target descriptor.
        /// </summary>
        public RenderTextureDescriptor renderTargetDesc { get; private set; }

        /// <summary>
        /// Parameters used for culling.
        /// </summary>
        public ScriptableCullingParameters cullingParams { get; private set; }

        /// <summary>
        /// Returns the number of views inside this pass.
        /// </summary>
        public int viewCount { get => m_Views.Count; }

        /// <summary>
        /// If true, the render pipeline is expected to use single-pass techniques to save CPU time.
        /// </summary>
        public bool singlePassEnabled { get => viewCount > 1; }

        /// <summary>
        /// Native pointer from the XR plugin to be consumed by ConfigureFoveatedRendering.
        /// </summary>
        public IntPtr foveatedRenderingInfo { get; private set; }

        /// <summary>
        /// Returns the projection matrix for a given view.
        /// </summary>
        /// <param name="viewIndex"></param>
        /// <returns></returns>
        public Matrix4x4 GetProjMatrix(int viewIndex = 0)
        {
            return m_Views[viewIndex].projMatrix;
        }

        /// <summary>
        /// Returns the view matrix for a given view.
        /// </summary>
        /// <param name="viewIndex"></param>
        /// <returns></returns>
        public Matrix4x4 GetViewMatrix(int viewIndex = 0)
        {
            return m_Views[viewIndex].viewMatrix;
        }

        /// <summary>
        /// Returns the viewport for a given view.
        /// </summary>
        /// <param name="viewIndex"></param>
        /// <returns></returns>
        public Rect GetViewport(int viewIndex = 0)
        {
            return m_Views[viewIndex].viewport;
        }

        /// <summary>
        /// Returns the occlusion mesh for a given view.
        /// </summary>
        /// <param name="viewIndex"></param>
        /// <returns></returns>
        public Mesh GetOcclusionMesh(int viewIndex = 0)
        {
            return m_Views[viewIndex].occlusionMesh;
        }

        /// <summary>
        /// Returns the destination slice index (for texture array) for a given view.
        /// </summary>
        /// <param name="viewIndex"></param>
        /// <returns></returns>
        public int GetTextureArraySlice(int viewIndex = 0)
        {
            return m_Views[viewIndex].textureArraySlice;
        }

        /// <summary>
        /// Queue up render commands to enable single-pass techniques.
        /// Note: depending on the platform and settings, either single-pass instancing or the multiview extension will be used.
        /// </summary>
        /// <param name="cmd"></param>
        public void StartSinglePass(CommandBuffer cmd)
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

        public void StartSinglePass(RasterCommandBuffer cmd)
        {
            StartSinglePass(cmd.m_WrappedCommandBuffer);
        }

        /// <summary>
        /// Queue up render commands to disable single-pass techniques.
        /// </summary>
        /// <param name="cmd"></param>
        public void StopSinglePass(CommandBuffer cmd)
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


        /// <summary>
        /// Queue up render commands to disable single-pass techniques.
        /// </summary>
        /// <param name="cmd"></param>
        public void StopSinglePass(RasterCommandBuffer cmd)
        {
            StopSinglePass(cmd.m_WrappedCommandBuffer);
        }

        /// <summary>
        /// Returns true if the pass was setup with expected mesh and material.
        /// </summary>
        public bool hasValidOcclusionMesh { get => m_OcclusionMesh.hasValidOcclusionMesh; }

        /// <summary>
        /// Generate commands to render the occlusion mesh for this pass.
        /// In single-pass mode : the meshes for all views are combined into one mesh,
        /// where the corresponding view index is encoded into each vertex. The keyword
        /// "XR_OCCLUSION_MESH_COMBINED" is also enabled when rendering the combined mesh.
        /// </summary>
        /// <param name="cmd"></param>
        public void RenderOcclusionMesh(CommandBuffer cmd)
        {
            m_OcclusionMesh.RenderOcclusionMesh(cmd);
        }
        public void RenderOcclusionMesh(RasterCommandBuffer cmd)
        {
            m_OcclusionMesh.RenderOcclusionMesh(cmd.m_WrappedCommandBuffer);
        }

        /// <summary>
        /// Take a point that is center-relative (0.5, 0.5) and modify it to be placed relative to the view's center instead, respecting the asymmetric FOV (if it is used)
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        public Vector4 ApplyXRViewCenterOffset(Vector2 center)
        {
            Vector4 result = Vector4.zero;
            float centerDeltaX = 0.5f - center.x;
            float centerDeltaY = 0.5f - center.y;

            result.x = m_Views[0].eyeCenterUV.x - centerDeltaX;
            result.y = m_Views[0].eyeCenterUV.y - centerDeltaY;
            if (singlePassEnabled)
            {
                // With single-pass XR, we need to add the data for the 2nd view
                result.z = m_Views[1].eyeCenterUV.x - centerDeltaX;
                result.w = m_Views[1].eyeCenterUV.y - centerDeltaY;
            }

            return result;
        }

        internal void AssignView(int viewId, XRView xrView)
        {
            if (viewId < 0 || viewId >= m_Views.Count)
                throw new ArgumentOutOfRangeException(nameof(viewId));

            m_Views[viewId] = xrView;
        }

        internal void AssignCullingParams(int cullingPassId, ScriptableCullingParameters cullingParams)
        {
            // Disable legacy stereo culling path
            cullingParams.cullingOptions &= ~CullingOptions.Stereo;

            this.cullingPassId = cullingPassId;
            this.cullingParams = cullingParams;
        }

        internal void UpdateCombinedOcclusionMesh()
        {
            m_OcclusionMesh.UpdateCombinedMesh();
        }

        /// <summary>
        /// Initialize the base class fields.
        /// </summary>
        /// <param name="createInfo"></param>
        public void InitBase(XRPassCreateInfo createInfo)
        {
            m_Views.Clear();
            copyDepth = createInfo.copyDepth;
            multipassId = createInfo.multipassId;
            AssignCullingParams(createInfo.cullingPassId, createInfo.cullingParameters);
            renderTarget = new RenderTargetIdentifier(createInfo.renderTarget, 0, CubemapFace.Unknown, -1);
            renderTargetDesc = createInfo.renderTargetDesc;
            m_OcclusionMesh.SetMaterial(createInfo.occlusionMeshMaterial);
            foveatedRenderingInfo = createInfo.foveatedRenderingInfo;
        }

        internal void AddView(XRView xrView)
        {
            if (m_Views.Count < TextureXR.slices)
            {
                m_Views.Add(xrView);
            }
            else
            {
                throw new NotImplementedException($"Invalid XR setup for single-pass, trying to add too many views! Max supported: {TextureXR.slices}");
            }
        }
    }
}
