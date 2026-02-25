using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// A description of an actual resource, necessary for its creation an access.
    /// </summary>
    interface IResourceDescription
    {
        /// <summary>
        /// The invalid ResourceDescription.
        /// </summary>
        public static IResourceDescription Invalid;

        /// <summary>
        /// Checks if the ResouceDescription reprensents a valid resource.
        /// </summary>
        public bool IsValid { get; }

    }

    struct BufferDescription : IResourceDescription
    {
        /// <summary>
        /// The capacity of the resource.
        /// </summary>
        public uint Capacity { get; }

        /// <summary>
        /// The stride of the resource.
        /// </summary>
        public uint Stride { get; }

        public GraphicsBuffer.Target Target { get; }
        /// <summary>
        /// Checks if the ResouceDescription reprensents a valid resource.
        /// </summary>
        public bool IsValid => Stride != 0;

        /// <summary>
        /// Constructs a BufferDescription
        /// </summary>
        /// <param name="capacity">The capacity of the buffer.</param>
        /// <param name="stride">The stride of the buffer.</param>
        /// <param name="target"> The GraphicsBuffer target of the buffer.</param>
        public BufferDescription(uint capacity, uint stride, GraphicsBuffer.Target target)
        {
            Capacity = capacity;
            Stride = stride;
            Target = target;
        }

        /// <summary>
        /// Generates a string with the description of the resource.
        /// </summary>
        /// <returns>A string with the description of the resource.</returns>
        public override string ToString()
        {
            if (IsValid)
                return $"Capacity : {Capacity} - Stride : {Stride}";
            return "Invalid Resource Description (will not lead to a real resource created)";

        }
    }

    struct RenderTextureDescription : IResourceDescription
    {
        public RenderTextureDescriptor RtDescriptor { get; }
        public bool IsValid => RtDescriptor.height > 0;

        public RenderTextureDescription(RenderTextureDescriptor rtDesc)
        {
            RtDescriptor = rtDesc;
        }
    }

}
