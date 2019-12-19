// Helper API to create custom XR layout

namespace UnityEngine.Rendering.HighDefinition
{
    public struct XRLayout
    {
        public Camera camera;
        public XRSystem xrSystem;

        public XRPass CreatePass(XRPassCreateInfo passCreateInfo)
        {
            XRPass pass = XRPass.Create(passCreateInfo);
            xrSystem.AddPassToFrame(camera, pass);
            return pass;
        }

        public void AddViewToPass(XRViewCreateInfo viewCreateInfo, XRPass pass)
        {
            pass.AddView(viewCreateInfo.projMatrix, viewCreateInfo.viewMatrix, viewCreateInfo.viewport, viewCreateInfo.globalScreenSpaceMatrix, viewCreateInfo.textureArraySlice);
        }
    }
}
