using Unity.Mathematics;

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    /// <summary>
    /// Shader abstraction that is used to bind resources and execute a unified ray tracing shader (.urtshader) on the GPU.
    /// </summary>
    /// <remarks>
    /// It can be created by calling <see cref="RayTracingContext.CreateRayTracingShader"/>, <see cref="RayTracingContext.LoadRayTracingShader"/> or <see cref="RayTracingContext.LoadRayTracingShaderFromAssetBundle"/>.
    /// Depending on the backend that was selected when creating the <see cref="RayTracingContext"/>, this class either wraps
    /// a RayTracing or a Compute shader.
    /// </remarks>
    public interface IRayTracingShader
    {
        /// <summary>
        /// Adds a command in cmd to set an IRayTracingAccelStruct on this shader.
        /// </summary>
        /// <param name="cmd">CommandBuffer to register the command to. </param>
        /// <param name="name">Name of the variable in shader code.</param>
        /// <param name="accelStruct">The IRayTracingAccelStruct to be used.</param>
        void SetAccelerationStructure(CommandBuffer cmd, string name, IRayTracingAccelStruct accelStruct);

        /// <summary>
        /// Adds a command in cmd to set an integer parameter on this shader.
        /// </summary>
        /// <param name="cmd">CommandBuffer to register the command to.</param>
        /// <param name="nameID">Property name ID. Use Shader.PropertyToID to get this ID.</param>
        /// <param name="val">Value to set.</param>
        void SetIntParam(CommandBuffer cmd, int nameID, int val);

        /// <summary>
        /// Adds a command in cmd to set a float parameter.
        /// </summary>
        /// <param name="cmd">CommandBuffer to register the command to.</param>
        /// <param name="nameID">Property name ID. Use Shader.PropertyToID to get this ID.</param>
        /// <param name="val">Value to set.</param>
        void SetFloatParam(CommandBuffer cmd, int nameID, float val);

        /// <summary>
        /// Adds a command in cmd to set a vector parameter.
        /// </summary>
        /// <param name="cmd">CommandBuffer to register the command to.</param>
        /// <param name="nameID">Property name ID. Use Shader.PropertyToID to get this ID.</param>
        /// <param name="val">Value to set.</param>
        void SetVectorParam(CommandBuffer cmd, int nameID, Vector4 val);

        /// <summary>
        /// Adds a command in cmd to set a matrix parameter.
        /// </summary>
        /// <param name="cmd">CommandBuffer to register the command to.</param>
        /// <param name="nameID">Property name ID. Use Shader.PropertyToID to get this ID.</param>
        /// <param name="val">Value to set.</param>
        void SetMatrixParam(CommandBuffer cmd, int nameID, Matrix4x4 val);

        /// <summary>
        /// Adds a command in cmd to set a texture parameter.
        /// </summary>
        /// <param name="cmd">CommandBuffer to register the command to.</param>
        /// <param name="nameID">Property name ID. Use Shader.PropertyToID to get this ID.</param>
        /// <param name="rt">Texture to set.</param>
        void SetTextureParam(CommandBuffer cmd, int nameID, RenderTargetIdentifier rt);

        /// <summary>
        /// Adds a command in cmd to set a buffer parameter.
        /// </summary>
        /// <param name="cmd">CommandBuffer to register the command to.</param>
        /// <param name="nameID">Property name ID. Use Shader.PropertyToID to get this ID.</param>
        /// <param name="buffer">Buffer to set.</param>
        void SetBufferParam(CommandBuffer cmd, int nameID, GraphicsBuffer buffer);

        /// <summary>
        /// Adds a command in cmd to set a buffer parameter.
        /// </summary>
        /// <param name="cmd">CommandBuffer to register the command to.</param>
        /// <param name="nameID">Property name ID. Use Shader.PropertyToID to get this ID.</param>
        /// <param name="buffer">Buffer to set.</param>
        void SetBufferParam(CommandBuffer cmd, int nameID, ComputeBuffer buffer);

        /// <summary>
        /// Adds a command in cmd to dispatch this IRayTracingShader.
        /// </summary>
        /// <remarks>
        /// Dispatches to the GPU this shader to be executed on a grid of width*height*depth threads.
        /// Depending on the backend, the GPU ray traversal algorithm can require additional GPU storage that is supplied through the scratchBuffer parameter.
        /// Its required size can be queried by calling <see cref="GetTraceScratchBufferRequiredSizeInBytes"/>.
        /// </remarks>
        /// <param name="cmd">CommandBuffer to register the command to.</param>
        /// <param name="scratchBuffer">Temporary buffer used during the shader's ray tracing calls.</param>
        /// <param name="width">Number of threads in the X dimension.</param>
        /// <param name="height">Number of threads in the Y dimension.</param>
        /// <param name="depth">Number of threads in the Z dimension.</param>
        void Dispatch(CommandBuffer cmd, GraphicsBuffer scratchBuffer, uint width, uint height, uint depth);

        /// <summary>
        /// Adds a command in cmd to dispatch this IRayTracingShader.
        /// </summary>
        /// <remarks>
        /// Dispatches to the GPU this shader to be executed on a grid of width*height*depth threads. The grid dimensions are read directly from the argsBuffer parameter. It needs
        /// to contain 3 integers: number of threads in X dimension, number of threads in Y dimension, number of threads in Z dimension.
        /// Typical use case is writing to argsBuffer from another shader and then dispatching this shader, without requiring a readback to the CPU.
        /// Depending on the backend, the GPU ray traversal algorithm can require additional GPU storage that is supplied through the scratchBuffer parameter.
        /// Its required size can be queried by calling <see cref="GetTraceScratchBufferRequiredSizeInBytes"/>.
        /// </remarks>
        /// <param name="cmd">CommandBuffer to register the command to.</param>
        /// <param name="scratchBuffer">Temporary buffer used during the shader's ray tracing calls.</param>
        /// <param name="argsBuffer">Buffer with work grid dimensions.</param>
        void Dispatch(CommandBuffer cmd, GraphicsBuffer scratchBuffer, GraphicsBuffer argsBuffer);

        /// <summary>
        /// Adds a command in cmd to set a constant buffer parameter.
        /// </summary>
        /// <param name="cmd">CommandBuffer to register the command to.</param>
        /// <param name="nameID">Property name ID. Use Shader.PropertyToID to get this ID.</param>
        /// <param name="buffer">The buffer to bind as constant buffer.</param>
        /// <param name="offset">The offset in bytes from the beginning of the buffer to bind. Must be a multiple of SystemInfo.constantBufferOffsetAlignment, or 0 if that value is 0.</param>
        /// <param name="size">The number of bytes to bind.</param>
        void SetConstantBufferParam(CommandBuffer cmd, int nameID, GraphicsBuffer buffer, int offset, int size);

        /// <summary>
        /// Adds a command in cmd to set a constant buffer parameter.
        /// </summary>
        /// <param name="cmd">CommandBuffer to register the command to.</param>
        /// <param name="nameID">Property name ID. Use Shader.PropertyToID to get this ID.</param>
        /// <param name="buffer">The buffer to bind as constant buffer.</param>
        /// <param name="offset">The offset in bytes from the beginning of the buffer to bind. Must be a multiple of SystemInfo.constantBufferOffsetAlignment, or 0 if that value is 0.</param>
        /// <param name="size">The number of bytes to bind.</param>
        void SetConstantBufferParam(CommandBuffer cmd, int nameID, ComputeBuffer buffer, int offset, int size);

        /// <summary>
        /// Returns the minimum buffer size that is required by the scratchBuffer parameter of <see cref="Dispatch"/>.
        /// This size depends on the specific values for width,height and depth that will be passed to Dispatch().
        /// </summary>
        /// <param name="width">Number of threads in the X dimension.</param>
        /// <param name="height">Number of threads in the Y dimension.</param>
        /// <param name="depth">Number of threads in the Z dimension.</param>
        /// <returns>The minimum size in bytes.</returns>
        ulong GetTraceScratchBufferRequiredSizeInBytes(uint width, uint height, uint depth);

        /// <summary>
        /// Get the thread group sizes of this shader.
        /// </summary>
        /// <returns>Thread group size in the X,Y and Z directions.</returns>
        uint3 GetThreadGroupSizes();
    }
}


