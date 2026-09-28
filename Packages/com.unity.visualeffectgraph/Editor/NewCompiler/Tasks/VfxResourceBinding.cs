using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    /// <summary>
    /// A binding for a GPU resource (a texture or a buffer). Unlike a value binding, the resource is declared
    /// in the generated shader directly under the binding key, so there is nothing to remap at runtime. What
    /// gets declared is the business of the writer registered for the bound view's description.
    /// </summary>
    class VfxResourceBinding : TemplatedTaskBinding<ResourceValueData>
    {
        /// <summary>
        /// The element type a block declared for this buffer, or an invalid <see cref="BufferType"/> when the
        /// resource has no declared usage.
        /// </summary>
        public BufferType BufferType { get; }

        public VfxResourceBinding(IDataKey key, BufferType bufferType = default)
            : base(key)
        {
            BufferType = bufferType;
            ReadPathSet.Add(DataPath.Root);

            if (VfxBufferTypeHlsl.IsReadWrite(bufferType))
                WritePathSet.Add(DataPath.Root);
        }
    }
}
