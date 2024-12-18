using System;
using System.Collections.Generic;
using UnityEditor;
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
        internal RenderTargetIdentifier motionVectorRenderTarget;
        internal RenderTextureDescriptor motionVectorRenderTargetDesc;
        internal ScriptableCullingParameters cullingParameters;
        internal Material occlusionMeshMaterial;
        internal float occlusionMeshScale;
        internal int renderTargetScaledWidth;
        internal int renderTargetScaledHeight;
        internal IntPtr foveatedRenderingInfo;
        internal int multipassId;
        internal int cullingPassId;
        internal bool copyDepth;
        internal bool hasMotionVectorPass;

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
        /// <param name="createInfo"> A descriptor used to create and initialize the XRPass. </param>
        /// <returns> Default XRPass created from createInfo descriptor. </returns>
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
        ///  If true, the render pipeline is expected to generate motion data and output to the motionVectorRenderTarget.
        /// </summary>
        public bool hasMotionVectorPass { get; private set; }

        /// <summary>
        /// If true, is the first pass of a xr camera
        /// </summary>
        public bool isFirstCameraPass => multipassId == 0;

        /// <summary>
        /// If true, is the last pass of a xr camera
        /// Multipass last pass: pass ID == 1, viewCount == 1
        /// Singlepass last pass: pass ID == 0, viewCount ==2
        /// Emptypass(non-XR) last pass: pass ID == 0, viewCount == 0
        /// </summary>
        public bool isLastCameraPass => (multipassId == 1 && viewCount <= 1) || (multipassId == 0 && viewCount > 1) || (multipassId == 0 && viewCount == 0) /* ViewCount 0 handles the empty pass*/;

        /// <summary>
        /// Index of the pass inside the frame.
        /// </summary>
        public int multipassId { get; private set; }

        /// <summary>
        /// Index used for culling. It can be shared between multiple passes.
        /// </summary>
        public int cullingPassId { get; private set; }

        /// <summary>
        /// Destination render target scaled width if XR dynamic resolution is enabled
        /// </summary>
        public int renderTargetScaledWidth { get; private set; }

        /// <summary>
        /// Destination render target scaled height if XR dynamic resolution is enabled
        /// </summary>
        public int renderTargetScaledHeight { get; private set; }

        /// <summary>
        /// Destination render target.
        /// </summary>
        public RenderTargetIdentifier renderTarget { get; private set; }

        /// <summary>
        /// Destination render target descriptor.
        /// </summary>
        public RenderTextureDescriptor renderTargetDesc { get; private set; }

        /// <summary>
        ///  Destination render target for motion vectors
        /// </summary>
        public RenderTargetIdentifier motionVectorRenderTarget { get; private set; }

        /// <summary>
        /// Destination render target descriptor for motion vectors.
        /// </summary>
        public RenderTextureDescriptor motionVectorRenderTargetDesc { get; private set; }

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
        /// Returns true when the active display has HDR enabled.
        /// </summary>
        public bool isHDRDisplayOutputActive
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            get => XRSystem.GetActiveDisplay().hdrOutputSettings?.active ?? false;
#else
            get => false;
#endif
        }

        /// <summary>
        /// Returns color gamut of the active HDR display.
        /// </summary>
        public ColorGamut hdrDisplayOutputColorGamut
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            get => XRSystem.GetActiveDisplay().hdrOutputSettings?.displayColorGamut ?? ColorGamut.sRGB;
#else
            get => ColorGamut.sRGB;
#endif
        }

        /// <summary>
        /// Returns HDR display information of the active HDR display.
        /// </summary>
        public HDROutputUtils.HDRDisplayInformation hdrDisplayOutputInformation
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            get => new HDROutputUtils.HDRDisplayInformation(
                XRSystem.GetActiveDisplay().hdrOutputSettings?.maxFullFrameToneMapLuminance ?? -1,
                XRSystem.GetActiveDisplay().hdrOutputSettings?.maxToneMapLuminance ?? -1,
                XRSystem.GetActiveDisplay().hdrOutputSettings?.minToneMapLuminance ?? -1,
                XRSystem.GetActiveDisplay().hdrOutputSettings?.paperWhiteNits ?? 160.0f
                );
#else
            get => new HDROutputUtils.HDRDisplayInformation(-1, -1, -1, 160.0f);
#endif
        }

        /// <summary>
        /// Scaling factor used when drawing the occlusion mesh.
        /// </summary>
        public float occlusionMeshScale { get; private set; }

        /// <summary>
        /// Returns the projection matrix for a given view.
        /// </summary>
        /// <param name="viewIndex"> Index of XRView to retrieve the data from. </param>
        /// <returns> XR projection matrix for the specified XRView. </returns>
        public Matrix4x4 GetProjMatrix(int viewIndex = 0)
        {
            return m_Views[viewIndex].projMatrix;
        }

        /// <summary>
        /// Returns the view matrix for a given view.
        /// </summary>
        /// <param name="viewIndex"> Index of XRView to retrieve the data from. </param>
        /// <returns> XR view matrix for the specified XRView. </returns>
        public Matrix4x4 GetViewMatrix(int viewIndex = 0)
        {
            return m_Views[viewIndex].viewMatrix;
        }

        /// <summary>
        /// Returns true if the previous frame view matrix for a given view is valid.
        /// </summary>
        /// <param name="viewIndex"> Index of XRView to retrieve the data from. </param>
        /// <returns> Boolean describing if previous frame view matrix for a given view is valid. </returns>
        public bool GetPrevViewValid(int viewIndex = 0)
        {
            return m_Views[viewIndex].isPrevViewMatrixValid;
        }

        /// <summary>
        /// Returns the previous frame view matrix for a given view.
        /// </summary>
        /// <param name="viewIndex"> Index of XRView to retrieve the data from. </param>
        /// <returns> Previous frame XR view matrix for the specified XRView. </returns>
        public Matrix4x4 GetPrevViewMatrix(int viewIndex = 0)
        {
            return m_Views[viewIndex].prevViewMatrix;
        }

        /// <summary>
        /// Returns the viewport for a given view.
        /// </summary>
        /// <param name="viewIndex"> Index of XRView to retrieve the data from. </param>
        /// <returns> XR viewport rect for the specified XRView. </returns>
        public Rect GetViewport(int viewIndex = 0)
        {
            return m_Views[viewIndex].viewport;
        }

        /// <summary>
        /// Returns the occlusion mesh for a given view.
        /// </summary>
        /// <param name="viewIndex"> Index of XRView to retrieve the data from. </param>
        /// <returns> XR occlusion mesh for the specified XRView. </returns>
        public Mesh GetOcclusionMesh(int viewIndex = 0)
        {
            return m_Views[viewIndex].occlusionMesh;
        }

        /// <summary>
        /// Returns the destination slice index (for texture array) for a given view.
        /// </summary>
        /// <param name="viewIndex"> Index of XRView to retrieve the data from. </param>
        /// <returns> XR target slice index for the specified XRView.  </returns>
        public int GetTextureArraySlice(int viewIndex = 0)
        {
            return m_Views[viewIndex].textureArraySlice;
        }

        /// <summary>
        /// Queue up render commands to enable single-pass techniques.
        /// Note: depending on the platform and settings, either single-pass instancing or the multiview extension will be used.
        /// </summary>
        /// <param name="cmd">CommandBuffer to modify</param>
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
                            cmd.EnableKeyword(SinglepassKeywords.STEREO_MULTIVIEW_ON);
                        }
                        else
                        {
                            cmd.EnableKeyword(SinglepassKeywords.STEREO_INSTANCING_ON);
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

        /// <summary>
        /// Queue up render commands to disable single-pass techniques.
        /// </summary>
        /// <param name="cmd">IRasterCommandBuffer compatible command buffer to modify (This can be a RasterCommandBuffer or an UnsafeCommandBuffer)</param>
        public void StartSinglePass(IRasterCommandBuffer cmd)
        {
            StartSinglePass((cmd as BaseCommandBuffer).m_WrappedCommandBuffer);
        }

        /// <summary>
        /// Queue up render commands to disable single-pass techniques.
        /// </summary>
        /// <param name="cmd">CommandBuffer to modify.</param>
        public void StopSinglePass(CommandBuffer cmd)
        {
            if (enabled)
            {
                if (singlePassEnabled)
                {
                    if (SystemInfo.supportsMultiview)
                    {
                        cmd.DisableKeyword(SinglepassKeywords.STEREO_MULTIVIEW_ON);
                    }
                    else
                    {
                        cmd.DisableKeyword(SinglepassKeywords.STEREO_INSTANCING_ON);
                        cmd.SetInstanceMultiplier(1);
                    }
                }
            }
        }


        /// <summary>
        /// Queue up render commands to disable single-pass techniques.
        /// </summary>
        /// <param name="cmd">BaseCommandBuffer to modify</param>
        public void StopSinglePass(BaseCommandBuffer cmd)
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
        /// <param name="cmd">CommandBuffer to modify</param>
        /// <param name="renderIntoTexture">Set to true when rendering into a render texture. Used for handling Unity yflip.</param>
        public void RenderOcclusionMesh(CommandBuffer cmd, bool renderIntoTexture = false)
        {
            if(occlusionMeshScale > 0)
                m_OcclusionMesh.RenderOcclusionMesh(cmd, occlusionMeshScale, renderIntoTexture);
        }

        /// <summary>
        /// Generate commands to render the occlusion mesh for this pass.
        /// In single-pass mode : the meshes for all views are combined into one mesh,
        /// where the corresponding view index is encoded into each vertex. The keyword
        /// "XR_OCCLUSION_MESH_COMBINED" is also enabled when rendering the combined mesh.
        /// </summary>
        /// <param name="cmd">RasterCommandBuffer to modify</param>
        /// <param name="renderIntoTexture">Set to true when rendering into a render texture. Used for handling Unity yflip.</param>
        public void RenderOcclusionMesh(RasterCommandBuffer cmd, bool renderIntoTexture = false)
        {
            if (occlusionMeshScale > 0)
                m_OcclusionMesh.RenderOcclusionMesh(cmd.m_WrappedCommandBuffer, occlusionMeshScale, renderIntoTexture);
        }

        /// <summary>
        /// Draw debug line for all XR views.
        /// </summary>
        public void RenderDebugXRViewsFrustum()
        {
            for(int i = 0; i < m_Views.Count; i++)
            {
                const float k_DebugVeiwsFrustumDepthZ = 10.0f;
                var view = m_Views[i];
                var corners = CoreUtils.CalculateViewSpaceCorners(view.projMatrix, k_DebugVeiwsFrustumDepthZ);

                // Get world space camera pos
                Vector3 worldSpaceCameraPos = -(view.viewMatrix).GetColumn(3);
                for(int j = 0; j < 4; j++)
                    Debug.DrawLine(worldSpaceCameraPos, view.viewMatrix.MultiplyPoint(corners[j]), i == 0 ? Color.green : Color.red);
            }
        }

        /// <summary>
        /// Take a point that is center-relative (0.5, 0.5) and modify it to be placed relative to the view's center instead, respecting the asymmetric FOV (if it is used)
        /// </summary>
        /// <param name="center"> Center relative point for symmetric FOV. </param>
        /// <returns> View center relative points. First view center is stored in x,y components and second view center is stored in z,w components. </returns>
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
        /// <param name="createInfo"> A descriptor used to create and initialize the XRPass. </param>
        public void InitBase(XRPassCreateInfo createInfo)
        {
            m_Views.Clear();
            copyDepth = createInfo.copyDepth;
            multipassId = createInfo.multipassId;
            AssignCullingParams(createInfo.cullingPassId, createInfo.cullingParameters);
            renderTarget = new RenderTargetIdentifier(createInfo.renderTarget, 0, CubemapFace.Unknown, -1);
            renderTargetDesc = createInfo.renderTargetDesc;
            renderTargetScaledWidth = createInfo.renderTargetScaledWidth;
            renderTargetScaledHeight = createInfo.renderTargetScaledHeight;
            motionVectorRenderTarget = new RenderTargetIdentifier(createInfo.motionVectorRenderTarget, 0, CubemapFace.Unknown, -1);
            motionVectorRenderTargetDesc = createInfo.motionVectorRenderTargetDesc;
            hasMotionVectorPass = createInfo.hasMotionVectorPass;
            m_OcclusionMesh.SetMaterial(createInfo.occlusionMeshMaterial);
            occlusionMeshScale = createInfo.occlusionMeshScale;
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
