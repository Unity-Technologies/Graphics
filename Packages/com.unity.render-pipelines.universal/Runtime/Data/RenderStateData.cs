using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Container class for stencil rendering settings.
    /// </summary>
    [System.Serializable]
    public class StencilStateData
    {
        /// <summary>
        /// Used to mark whether the stencil values should be overridden or not.
        /// </summary>
        public bool overrideStencilState = false;

        /// <summary>
        /// The stencil reference value.
        /// </summary>
        public int stencilReference = 0;

        /// <summary>
        /// Bitmask applied to the reference value and the value already in the stencil buffer before the comparison.
        /// </summary>
        public int stencilReadMask = (int)StencilUsage.UserMask;

        /// <summary>
        /// Bitmask applied when writing to the stencil buffer.
        /// </summary>
        public int stencilWriteMask = (int)StencilUsage.UserMask;

        /// <summary>
        /// The comparison function to use.
        /// </summary>
        public CompareFunction stencilCompareFunction = CompareFunction.Always;

        /// <summary>
        /// The stencil operation to use when the stencil test passes.
        /// </summary>
        public StencilOp passOperation = StencilOp.Keep;

        /// <summary>
        /// The stencil operation to use when the stencil test fails.
        /// </summary>
        public StencilOp failOperation = StencilOp.Keep;

        /// <summary>
        /// The stencil operation to use when the stencil test fails because of depth.
        /// </summary>
        public StencilOp zFailOperation = StencilOp.Keep;
    }
}
