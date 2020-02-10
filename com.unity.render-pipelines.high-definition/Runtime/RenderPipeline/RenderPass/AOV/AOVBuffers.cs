namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary> Buffers available in HDRP </summary>
    public enum AOVBuffers
    {
        /// <summary>Color buffer that will be used at the end, include post processes.</summary>
        Output,
        /// <summary>Color buffer that will be used before post processes.</summary>
        Color,
        /// <summary>DepthStencil buffer at the end of the frame.</summary>
        DepthStencil,
        /// <summary>Normals buffer at the end of the frame.</summary>
        Normals,
        /// <summary>Motion vectors buffer at the end of the frame.</summary>
        MotionVectors
    }
}
