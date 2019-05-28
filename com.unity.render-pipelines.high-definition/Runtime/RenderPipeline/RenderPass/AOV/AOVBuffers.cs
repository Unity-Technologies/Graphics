namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    /// <summary> Buffers available in HDRP </summary>
    public enum AOVBuffers
    {
        /// <summary>Color buffer that will be used at the end, include post processes.</summary>
        Output,
        /// <summary>Color buffer that will be used before post processes.</summary>
        Color,
        DepthStencil,
        Normals,
        MotionVectors
    }
}
