using System.Collections.Generic;
using System.Text;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Used by render pipelines to store information about the XR device layout.
    /// </summary>
    public class XRLayout
    {
        readonly List<(Camera, XRPass)> m_FramePasses = new List<(Camera, XRPass)>();

        /// <summary>
        /// Configure the layout to render from the specified camera by generating passes from the the connected XR device.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="enableXR"></param>
        public void AddCamera(Camera camera, bool enableXR)
        {
            if (camera == null)
                return;

            // Enable XR layout only for game camera
            bool isGameCamera = (camera.cameraType == CameraType.Game || camera.cameraType == CameraType.VR);
            bool xrSupported = isGameCamera && camera.targetTexture == null && enableXR;

            if (XRSystem.displayActive && xrSupported)
            {
                XRSystem.SetDisplayZRange(camera.nearClipPlane, camera.farClipPlane);
                XRSystem.SetDisplaySync();
                XRSystem.CreateDefaultLayout(camera);
            }
            else
            {
                AddPass(camera, XRSystem.emptyPass);
            }
        }

        /// <summary>
        /// Used by render pipelines to reconfigure a pass from a camera.
        /// </summary>
        /// <param name="xrPass"></param>
        /// <param name="camera"></param>
        public void ReconfigurePass(XRPass xrPass, Camera camera)
        {
            if (xrPass.enabled)
            {
                XRSystem.ReconfigurePass(xrPass, camera);
                xrPass.UpdateCombinedOcclusionMesh();
            }
        }

        /// <summary>
        /// Used by render pipelines to access all registered passes on this layout.
        /// </summary>
        public List<(Camera, XRPass)> GetFramePasses()
        {
            return m_FramePasses;
        }

        internal void AddPass(Camera camera, XRPass xrPass)
        {
            xrPass.UpdateCombinedOcclusionMesh();
            m_FramePasses.Add((camera, xrPass));
        }

        internal void Clear()
        {
            foreach ((Camera _, XRPass xrPass) in m_FramePasses)
            {
                if (xrPass != XRSystem.emptyPass)
                    XRPass.Release(xrPass);
            }

            m_FramePasses.Clear();
        }

        internal void LogDebugInfo()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("XRSystem setup for frame {0}, active: {1}", Time.frameCount, XRSystem.displayActive);
            sb.AppendLine();

            for (int passIndex = 0; passIndex < m_FramePasses.Count; passIndex++)
            {
                var pass = m_FramePasses[passIndex].Item2;
                for (int viewIndex = 0; viewIndex < pass.viewCount; viewIndex++)
                {
                    var viewport = pass.GetViewport(viewIndex);
                    sb.AppendFormat("XR Pass {0} Cull {1} View {2} Slice {3} : {4} x {5}",
                        pass.multipassId,
                        pass.cullingPassId,
                        viewIndex,
                        pass.GetTextureArraySlice(viewIndex),
                        viewport.width,
                        viewport.height);
                    sb.AppendLine();
                }
            }

            Debug.Log(sb);
        }
    }
}
