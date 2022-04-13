// Helper API to create custom XR layout

namespace UnityEngine.Rendering.HighDefinition
{
    internal struct XRLayout
    {
        internal Camera camera;
        internal XRSystem xrSystem;

        internal XRPass CreatePass(XRPassCreateInfo passCreateInfo)
        {
            XRPass pass = XRPass.Create(passCreateInfo);
            xrSystem.AddPassToFrame(camera, pass);
            return pass;
        }

        internal void AddViewToPass(XRViewCreateInfo viewCreateInfo, XRPass pass)
        {
            pass.AddView(viewCreateInfo.projMatrix, viewCreateInfo.viewMatrix, viewCreateInfo.viewport, viewCreateInfo.textureArraySlice);
        }
    }
}
