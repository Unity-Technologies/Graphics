// This file should be used as a container for things on its
// way to being deprecated and removed in future releases

namespace UnityEngine.Rendering.Universal
{
    public abstract partial class ScriptableRenderPass
    {
        // This callback method will be removed. Please use OnCameraCleanup() instead.
        public virtual void FrameCleanup(CommandBuffer cmd) => OnCameraCleanup(cmd);
    }
}
