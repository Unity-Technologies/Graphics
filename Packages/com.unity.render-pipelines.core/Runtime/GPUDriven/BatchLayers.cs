namespace UnityEngine.Rendering
{
    /// <summary>
    /// Predefined batch layer values used by the GPU Resident Drawer.
    /// </summary>
    public class BatchLayer
    {
        /// <summary>
        /// Batch layer for BatchRendererGroup direct draw commands produced by the GPU Resident Drawer.
        /// </summary>
        public const byte InstanceCullingDirect = 29;

        /// <summary>
        /// Batch layer for BatchRendererGroup indirect draw commands produced by the GPU Resident Drawer.
        /// </summary>
        public const byte InstanceCullingIndirect = 28;

        /// <summary>
        /// A batch layer mask to include BatchRendererGroup direct draw commands produced by the GPU Resident Drawer.
        /// Batch layer masks can be used to filter the set of draw calls in a renderer list.
        /// </summary>
        public const uint InstanceCullingDirectMask = 1u << InstanceCullingDirect;

        /// <summary>
        /// A batch layer mask to include BatchRendererGroup indirect draw commands produced by the GPU Resident Drawer.
        /// Batch layer masks can be used to filter the set of draw calls in a renderer list.
        /// </summary>
        public const uint InstanceCullingIndirectMask = 1u << InstanceCullingIndirect;

        /// <summary>
        /// A batch layer mask to include BatchRendererGroup direct and indirect draw commands produced by the GPU Resident Drawer.
        /// Batch layer masks can be used to filter the set of draw calls in a renderer list.
        /// </summary>
        public const uint InstanceCullingMask = InstanceCullingDirectMask | InstanceCullingIndirectMask;
    }
}
