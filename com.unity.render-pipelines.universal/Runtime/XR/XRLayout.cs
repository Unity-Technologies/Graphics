// Helper API to create custom XR layout

#if ENABLE_VR && ENABLE_XR_MODULE

namespace UnityEngine.Rendering.Universal
{
    internal struct XRLayout
    {
        internal Camera camera;
        internal XRSystem xrSystem;

        internal XRPass CreatePass(XRPassCreateInfo passCreateInfo)
        {
            XRPass pass = XRPass.Create(passCreateInfo);
            xrSystem.AddPassToFrame(pass);
            return pass;
        }

        internal void AddViewToPass(XRViewCreateInfo viewCreateInfo, XRPass pass)
        {
            pass.AddView(viewCreateInfo.projMatrix, viewCreateInfo.viewMatrix, viewCreateInfo.viewport, viewCreateInfo.textureArraySlice);
        }
    }
}

#endif
