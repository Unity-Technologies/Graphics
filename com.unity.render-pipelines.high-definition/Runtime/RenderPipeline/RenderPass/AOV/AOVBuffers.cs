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
        MotionVectors,
        /// <summary>Custom pass buffer after the custom pass at "BeforeRendering" injection point is executed.</summary> 
        CustomPassBufferBeforeRendering,
        /// <summary>Custom pass buffer after the custom pass at "AfterOpaqueDepthAndNormal" injection point is executed.</summary> 
        CustomPassBufferAfterOpaqueDepthAndNormal,
        /// <summary>Custom pass buffer after the custom pass at "BeforePreRefraction" injection point is executed.</summary> 
        CustomPassBufferBeforePreRefraction,
        /// <summary>Custom pass buffer after the custom pass at "BeforeTransparent" injection point is executed.</summary> 
        CustomPassBufferBeforeTransparent,
        /// <summary>Custom pass buffer after the custom pass at "BeforePostProcess" injection point is executed.</summary> 
        CustomPassBufferBeforePostProcess,
        /// <summary>Custom pass buffer after the custom pass at "AfterPostProcess" injection point is executed.</summary> 
        CustomPassBufferAfterPostProcess
    }
}
