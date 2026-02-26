using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Represents a GPU kernel task that utilizes a specific kernel within a compute shader.
    /// </summary>
    /*public*/ class GpuKernelTask : ITask
    {
        /// <summary>
        /// Gets the compute shader associated with this GPU kernel task.
        /// </summary>
        public ComputeShader Shader { get; }

        /// <summary>
        /// Gets the index of the kernel within the associated compute shader.
        /// </summary>
        public int KernelIndex { get; }

        /// <summary>
        /// UniqueDataId to bind the count data.
        /// </summary>
        public static UniqueDataKey Count { get; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="GPUKernelTask"/> class.
        /// </summary>
        /// <param name="computeShader">The compute shader associated with the task.</param>
        /// <param name="kernelIndex">The index of the kernel in the compute shader.</param>
        public GpuKernelTask(ComputeShader computeShader, int kernelIndex)
        {
            Shader = computeShader;
            KernelIndex = kernelIndex;

            // Read the binders or perform additional initializations
        }
    }

}
