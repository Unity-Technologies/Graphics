using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    /// <summary>
    /// Specifies what backend to use when creating a <see cref="RayTracingContext"/>.
    /// </summary>
    public enum RayTracingBackend
    {
        /// <summary>
        /// Requires a GPU supporting hardware accelerated ray tracing.
        /// </summary>
        Hardware = 0,

        /// <summary>
        /// Software implementation of ray tracing that requires the GPU to support compute shaders.
        /// </summary>
        Compute = 1
    }

    /// <summary>
    /// Entry point for the UnifiedRayTracing API.
    /// </summary>
    /// <remarks>
    /// It provides functionality to:
    /// <list type="bullet">
    /// <item><description>load shader code (<see cref="CreateRayTracingShader">CreateRayTracingShader</see>)</description></item>
    /// <item><description>create an acceleration structure (<see cref="CreateAccelerationStructure">CreateAccelerationStructure</see>) that represents the geometry to be ray traced against.</description></item>
    /// </list>
    /// Once these objects have been created, the shader code can be executed by calling <see cref="IRayTracingShader.Dispatch">IRayTracingShader.Dispatch</see>
    /// Before calling Dispose() on a RayTracingContext, all <see cref="IRayTracingAccelStruct"/> that have been created by a RayTracingContext must be disposed as well.
    /// </remarks>
    public sealed class RayTracingContext : IDisposable
    {
        /// <summary>
        /// Creates a RayTracingContext.
        /// </summary>
        /// <param name="backend">The chosen backend.</param>
        /// <param name="resources">The resources (provides the various shaders the context needs to operate).</param>
        /// <exception cref="System.InvalidOperationException">Thrown when the supplied backend is not supported.</exception>
        public RayTracingContext(RayTracingBackend backend, RayTracingResources resources)
        {
            Utils.CheckArgIsNotNull(resources, nameof(resources));

            if (!IsBackendSupported(backend))
                throw new System.InvalidOperationException("Unsupported backend: " + backend.ToString());

            BackendType = backend;
            if (backend == RayTracingBackend.Hardware)
                m_Backend = new HardwareRayTracingBackend(resources);
            else if (backend == RayTracingBackend.Compute)
                m_Backend = new ComputeRayTracingBackend(resources);

            Resources = resources;
            m_DispatchBuffer = RayTracingHelper.CreateDispatchIndirectBuffer();
        }

        /// <summary>
        /// Creates a RayTracingContext.
        /// </summary>
        /// <param name="resources">The resources (provides the various shaders the context needs to operate).</param>
        /// <exception cref="System.InvalidOperationException">Thrown when no supported backend is available.</exception>
        public RayTracingContext(RayTracingResources resources) : this(IsBackendSupported(RayTracingBackend.Hardware) ? RayTracingBackend.Hardware : RayTracingBackend.Compute, resources)
        {
        }

        /// <summary>
        /// Disposes the RaytracingContext.
        /// </summary>
        /// <remarks>
        /// Before calling this, all <see cref="IRayTracingAccelStruct"/> that have been created with this RayTracingContext must be disposed as well.
        /// </remarks>
        public void Dispose()
        {
            if (m_AccelStructCounter.value != 0)
            {
                Debug.LogError("Memory Leak. Please call .Dispose() on all the IAccelerationStructure resources "+
                               "that have been created with this context before calling RayTracingContext.Dispose()");
            }
            m_DispatchBuffer?.Release();
        }

        /// <summary>
        /// <see cref="RayTracingResources"/> object this context has been created with.
        /// </summary>
        public RayTracingResources Resources { get; private set; }

        /// <summary>
        /// Checks if the specified backend is supported on the current GPU.
        /// </summary>
        /// <param name="backend">The backend.</param>
        /// <returns>Whether the specified bakend is supported.</returns>
        static public bool IsBackendSupported(RayTracingBackend backend)
        {
            if (backend == RayTracingBackend.Hardware)
                return SystemInfo.supportsRayTracing;
            else if (backend == RayTracingBackend.Compute)
                return SystemInfo.supportsComputeShaders;

            return false;
        }

        /// <summary>
        /// Creates a IRayTracingShader.
        /// </summary>
        /// <remarks>
        /// Depending on the chosen backend, the shader parameter
        /// needs to be either a ComputeShader or RayTracingShader.
        /// </remarks>
        /// <param name="shader">The ComputeShader or RayTracingShader asset.</param>
        /// <returns>The unified ray tracing shader.</returns>
        public IRayTracingShader CreateRayTracingShader(Object shader) =>
            m_Backend.CreateRayTracingShader(shader, "MainRayGenShader", m_DispatchBuffer);

#if UNITY_EDITOR
        /// <summary>
        /// Creates a unified ray tracing shader from .urtshader asset file.
        /// </summary>
        /// <remarks>
        /// - This API works only in the Unity Editor, not at runtime.
        /// - The path must be relative to the project folder, for example: "Assets/Stuff/myshader.urtshader".
        /// - A .urtshader asset file is imported in the Editor as 2 shaders: a ComputeShader and a RayTracingShader. LoadRayTracingShader loads the one relevant one depending on the RayTracingContext's backend.
        /// </remarks>
        /// <param name="fileName">Path to the .urtshader shader asset file to load.</param>
        /// <returns>The unified ray tracing shader.</returns>
        public IRayTracingShader LoadRayTracingShader(string fileName)
        {
            Type shaderType = BackendHelpers.GetTypeOfShader(BackendType);
            Object asset = AssetDatabase.LoadAssetAtPath(fileName, shaderType);
            return CreateRayTracingShader(asset);
        }
#endif
#if ENABLE_ASSET_BUNDLE
        /// <summary>
        /// Creates a unified ray tracing shader from an AssetBundle.
        /// </summary>
        /// <param name="assetBundle">The AssetBundle.</param>
        /// <param name="name">The asset name with the .urtshader extension included.</param>
        /// <returns>The unified ray tracing shader.</returns>
        public IRayTracingShader LoadRayTracingShaderFromAssetBundle(AssetBundle assetBundle, string name)
        {
            Utils.CheckArgIsNotNull(assetBundle, nameof(assetBundle));

            Object asset = assetBundle.LoadAsset(name, BackendHelpers.GetTypeOfShader(BackendType));
            return CreateRayTracingShader(asset);
        }
#endif
        /// <summary>
        /// Creates a IRayTracingAccelStruct.
        /// </summary>
        /// <param name="options">Options for quality/performance trade-offs for the returned acceleration structure</param>
        /// <returns>The acceleration structure.</returns>
        public IRayTracingAccelStruct CreateAccelerationStructure(AccelerationStructureOptions options)
        {
            Utils.CheckArgIsNotNull(options, nameof(options));

            var accelStruct = m_Backend.CreateAccelerationStructure(options, m_AccelStructCounter);
            return accelStruct;
        }

        /// <summary>
        /// Returns the minimum size that is required by the scratchBuffer parameter of <see cref="IRayTracingShader.Dispatch"/>.
        /// </summary>
        /// <param name="width">Number of threads in the X dimension.</param>
        /// <param name="height">Number of threads in the Y dimension.</param>
        /// <param name="depth">Number of threads in the Z dimension.</param>
        /// <returns>The size in bytes.</returns>
        public ulong GetRequiredTraceScratchBufferSizeInBytes(uint width, uint height, uint depth)
        {
            return m_Backend.GetRequiredTraceScratchBufferSizeInBytes(width, height, depth);
        }

        /// <summary>
        /// Required stride for the creation of the scratchBuffers used by <see cref="IRayTracingShader.Dispatch"/> and <see cref="IRayTracingAccelStruct.Build"/>.
        /// </summary>
        /// <returns>The required stride.</returns>
        public static uint GetScratchBufferStrideInBytes() => 4;

        /// <summary>
        /// The <see cref="RayTracingBackend"/> this context was created with.
        /// </summary>
        public RayTracingBackend BackendType { get; private set; }

        readonly IRayTracingBackend m_Backend;
        readonly ReferenceCounter m_AccelStructCounter = new ReferenceCounter();
        readonly GraphicsBuffer m_DispatchBuffer;
    }

    /// <summary>
    /// Specifies how Unity builds the acceleration structure on the GPU.
    /// </summary>
    [System.Flags]
    public enum BuildFlags
    {
        /// <summary>
        /// Specify no options for the acceleration structure build. Provides a trade-off between good ray tracing performance and fast build times.
        /// </summary>
        None = 0,

        /// <summary>
        /// Build a high quality acceleration structure, increasing build time but maximizing ray tracing performance.
        /// </summary>
        PreferFastTrace = 1 << 0,

        /// <summary>
        /// Build a lower quality acceleration structure, minimizing build time but decreasing ray tracing performance.
        /// </summary>
        PreferFastBuild = 1 << 1,

        /// <summary>
        /// Minimize the amount of temporary memory Unity uses when building the acceleration structure, and minimize the size of the result.
        /// </summary>
        MinimizeMemory = 1 << 2
    }

    /// <summary>
    /// Options used to configure the creation of a <see cref="IRayTracingAccelStruct"/>.
    /// </summary>
    public class AccelerationStructureOptions
    {
        /// <summary>
        /// Option for the quality of the built <see cref="IRayTracingAccelStruct"/>.
        /// </summary>
        public BuildFlags buildFlags = 0;
#if UNITY_EDITOR
        /// <summary>
        /// Enables building the acceleration structure on the CPU instead of the GPU.
        /// Enabling this option combined with the use of the PreferFastBuild flag provides the best possible ray tracing performance.
        /// </summary>
        /// <remarks>
        /// This field works only in the Unity Editor, not at runtime.
        /// </remarks>
        public bool useCPUBuild = false;
#endif
    }

    internal class ReferenceCounter
    {
        public ulong value = 0;

        public void Inc() { value++; }
        public void Dec() { value--; }
    }

    /// <summary>
    /// Helper functions that can be used to create a scratch buffer.
    /// </summary>
    /// <remarks>
    /// A scratch buffer is a GraphicsBuffer that Unity uses during the acceleration structure build or the ray tracing dispatch to store temporary data.
    /// </remarks>
    public static class RayTracingHelper
    {
        /// <summary>
        /// <see cref="GraphicsBuffer.Target"/> suitable for scratch buffers used in for both <see cref="IRayTracingShader.Dispatch"/> and <see cref="IRayTracingAccelStruct.Build"/>.
        /// </summary>
        public const GraphicsBuffer.Target ScratchBufferTarget = GraphicsBuffer.Target.Structured;

        /// <summary>
        /// Creates an indirect args buffer suitable for <see cref="IRayTracingShader.Dispatch"/>.
        /// </summary>
        /// <returns>The scratch buffer.</returns>
        static public GraphicsBuffer CreateDispatchIndirectBuffer()
        {
            return new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource, 3, sizeof(uint));
        }

        /// <summary>
        /// Creates a scratch buffer suitable for both <see cref="IRayTracingShader.Dispatch"/> and <see cref="IRayTracingAccelStruct.Build"/>.
        /// </summary>
        /// <param name="accelStruct">The acceleration structure that will be passed to <see cref="IRayTracingAccelStruct.Build"/>.</param>
        /// <param name="shader">The shader that will be passed to <see cref="IRayTracingShader.Dispatch"/>.</param>
        /// <param name="dispatchWidth">Number of threads in the X dimension that will be passed to <see cref="IRayTracingShader.Dispatch"/>.</param>
        /// <param name="dispatchHeight">Number of threads in the Y dimension that will be passed to <see cref="IRayTracingShader.Dispatch"/>.</param>
        /// <param name="dispatchDepth">Number of threads in the Z dimension that will be passed to <see cref="IRayTracingShader.Dispatch"/>.</param>
        /// <returns>The scratch buffer.</returns>
        static public GraphicsBuffer CreateScratchBufferForBuildAndDispatch(
            IRayTracingAccelStruct accelStruct,
            IRayTracingShader shader, uint dispatchWidth, uint dispatchHeight, uint dispatchDepth)
        {
            Utils.CheckArgIsNotNull(accelStruct, nameof(accelStruct));
            Utils.CheckArgIsNotNull(shader, nameof(shader));

            var sizeInBytes = System.Math.Max(accelStruct.GetBuildScratchBufferRequiredSizeInBytes(), shader.GetTraceScratchBufferRequiredSizeInBytes(dispatchWidth, dispatchHeight, dispatchDepth));
            if (sizeInBytes == 0)
                return null;

            return new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)(sizeInBytes / 4), 4);
        }

        /// <summary>
        /// Creates a scratch buffer suitable for <see cref="IRayTracingAccelStruct.Build"/>.
        /// </summary>
        /// <param name="accelStruct">The acceleration structure that will be passed to <see cref="IRayTracingAccelStruct.Build"/>.</param>
        /// <returns>The scratch buffer.</returns>
        static public GraphicsBuffer CreateScratchBufferForBuild(
            IRayTracingAccelStruct accelStruct)
        {
            Utils.CheckArgIsNotNull(accelStruct, nameof(accelStruct));

            var sizeInBytes = accelStruct.GetBuildScratchBufferRequiredSizeInBytes();
            if (sizeInBytes == 0)
                return null;

            return new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)(sizeInBytes / 4), 4);
        }

        /// <summary>
        /// Creates a scratch buffer suitable for <see cref="IRayTracingShader.Dispatch"/>.
        /// </summary>
        /// <param name="shader">The shader that will be passed to <see cref="IRayTracingShader.Dispatch"/>.</param>
        /// <param name="dispatchWidth">Number of threads in the X dimension that will be passed to <see cref="IRayTracingShader.Dispatch"/>.</param>
        /// <param name="dispatchHeight">Number of threads in the Y dimension that will be passed to <see cref="IRayTracingShader.Dispatch"/>.</param>
        /// <param name="dispatchDepth">Number of threads in the Z dimension that will be passed to <see cref="IRayTracingShader.Dispatch"/>.</param>
        /// <returns>The scratch buffer.</returns>
        static public GraphicsBuffer CreateScratchBufferForTrace(IRayTracingShader shader, uint dispatchWidth, uint dispatchHeight, uint dispatchDepth)
        {
            Utils.CheckArgIsNotNull(shader, nameof(shader));

            var sizeInBytes = shader.GetTraceScratchBufferRequiredSizeInBytes(dispatchWidth, dispatchHeight, dispatchDepth);
            if (sizeInBytes == 0)
                return null;

            return new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)(sizeInBytes / 4), 4);
        }

        /// <summary>
        /// Resizes a scratch buffer if its size doesn't fit the requirement of <see cref="IRayTracingShader.Dispatch"/>.
        /// </summary>
        /// <remarks>
        /// The resize is accomplished by disposing of the GraphicsBuffer and instanciating a new one at the proper size.
        /// </remarks>
        /// <param name="shader">The shader that will be passed to <see cref="IRayTracingShader.Dispatch"/>.</param>
        /// <param name="dispatchWidth">Number of threads in the X dimension that will be passed to <see cref="IRayTracingShader.Dispatch"/>.</param>
        /// <param name="dispatchHeight">Number of threads in the Y dimension that will be passed to <see cref="IRayTracingShader.Dispatch"/>.</param>
        /// <param name="dispatchDepth">Number of threads in the Z dimension that will be passed to <see cref="IRayTracingShader.Dispatch"/>.</param>
        /// <param name="scratchBuffer">The scratch buffer.</param>
        static public void ResizeScratchBufferForTrace(
            IRayTracingShader shader, uint dispatchWidth, uint dispatchHeight, uint dispatchDepth, ref GraphicsBuffer scratchBuffer)
        {
            Utils.CheckArgIsNotNull(shader, nameof(shader));

            var sizeInBytes = shader.GetTraceScratchBufferRequiredSizeInBytes(dispatchWidth, dispatchHeight, dispatchDepth);
            if (sizeInBytes == 0)
                return;

            if (scratchBuffer != null)
                Utils.CheckArg(scratchBuffer.target == ScratchBufferTarget, "scratchBuffer.target must have Target.Structured set");

            if (scratchBuffer == null || (ulong)(scratchBuffer.count*scratchBuffer.stride) < sizeInBytes)
            {
                scratchBuffer?.Dispose();
                scratchBuffer = new GraphicsBuffer(ScratchBufferTarget, (int)(sizeInBytes / 4), 4);
            }
        }

        /// <summary>
        /// Resizes a scratch buffer if its size doesn't fit the requirement of <see cref="IRayTracingAccelStruct.Build"/>.
        /// </summary>
        /// <remarks>
        /// The resize is accomplished by disposing of the GraphicsBuffer and instanciating a new one at the proper size.
        /// </remarks>
        /// <param name="accelStruct">The acceleration structure that will be passed to <see cref="IRayTracingAccelStruct.Build"/>.</param>
        /// <param name="scratchBuffer">The scratch buffer.</param>
        static public void ResizeScratchBufferForBuild(
            IRayTracingAccelStruct accelStruct, ref GraphicsBuffer scratchBuffer)
        {
            Utils.CheckArgIsNotNull(accelStruct, nameof(accelStruct));

            var sizeInBytes = accelStruct.GetBuildScratchBufferRequiredSizeInBytes();
            if (sizeInBytes == 0)
                return;

            if (scratchBuffer != null)
                Utils.CheckArg(scratchBuffer.target == ScratchBufferTarget, "scratchBuffer.target must have Target.Structured set");

            if (scratchBuffer == null || (ulong)(scratchBuffer.count * scratchBuffer.stride) < sizeInBytes)
            {
                scratchBuffer?.Dispose();
                scratchBuffer = new GraphicsBuffer(ScratchBufferTarget, (int)(sizeInBytes / 4), 4);
            }
        }
    }

}
