namespace UnityEngine.Rendering
{
    // Bridge exposing the engine's RendererUpdateManager.GetMotionVectorFrameIndex() to SRP packages.
    internal static class RendererUpdateManagerBridge
    {
        // Advances once per logical frame / editor repaint; constant across multiple renders of the same camera within one frame.
        public static int motionVectorFrameIndex => RendererUpdateManagerBindings.GetMotionVectorFrameIndex();
    }
}
