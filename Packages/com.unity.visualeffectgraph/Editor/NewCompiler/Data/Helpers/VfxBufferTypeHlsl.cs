using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    /// <summary>
    /// How a <see cref="BufferType"/> — the element type a block declared for a resource it reads — is
    /// spelled in HLSL.
    /// </summary>
    /// <remarks>
    /// Despite the name, a <see cref="BufferType"/> also covers textures: <c>HLSLParser</c> builds one for
    /// any Custom HLSL parameter assignable to <c>Texture</c>, so this can return <c>RWTexture2D&lt;float4&gt;</c>
    /// as readily as <c>StructuredBuffer&lt;Foo&gt;</c>.
    /// </remarks>
    static class VfxBufferTypeHlsl
    {
        /// <summary>
        /// The HLSL declaration of a resource whose element type a block declared, or null when it declared
        /// none and the resource is bound as a raw buffer.
        /// </summary>
        public static string GetDeclaration(BufferType bufferType)
        {
            if (!bufferType.valid)
                return null;

            if (string.IsNullOrEmpty(bufferType.verbatimType))
                return bufferType.container.ToString();

            // A type HLSLParser could not resolve is spelled out as it was written, uint4 for instance.
            // Anything else is named the way VfxBufferElementTypeWriter declares it.
            var elementType = bufferType.actualType == null || bufferType.actualType == typeof(void)
                ? bufferType.verbatimType
                : HlslCodeHelper.GetTypeName(bufferType.actualType);

            return $"{bufferType.container}<{elementType}>";
        }

        /// <summary>
        /// Whether a container is a read-write (UAV) one, as opposed to read-only (SRV).
        /// </summary>
        public static bool IsReadWrite(BufferType bufferType) => bufferType.valid && k_ToReadOnly.ContainsKey(bufferType.container);

        /// <summary>
        /// The same buffer type, with its container switched to the read-write or read-only form asked for.
        /// A container with no counterpart of the other kind (<c>AppendStructuredBuffer</c>,
        /// <c>ConsumeStructuredBuffer</c>) is returned unchanged either way.
        /// </summary>
        public static BufferType WithReadWrite(BufferType bufferType, bool isReadWrite)
        {
            var table = isReadWrite ? k_ToReadWrite : k_ToReadOnly;
            var container = table.TryGetValue(bufferType.container, out var mapped) ? mapped : bufferType.container;
            return new BufferType(container, bufferType.verbatimType, bufferType.actualType);
        }

        static readonly Dictionary<BufferType.Container, BufferType.Container> k_ToReadWrite = new()
        {
            [BufferType.Container.StructuredBuffer] = BufferType.Container.RWStructuredBuffer,
            [BufferType.Container.ByteAddressBuffer] = BufferType.Container.RWByteAddressBuffer,
            [BufferType.Container.Buffer] = BufferType.Container.RWBuffer,
            [BufferType.Container.Texture1D] = BufferType.Container.RWTexture1D,
            [BufferType.Container.Texture2D] = BufferType.Container.RWTexture2D,
            [BufferType.Container.Texture3D] = BufferType.Container.RWTexture3D,
            [BufferType.Container.TextureCube] = BufferType.Container.RWTextureCube,
            [BufferType.Container.Texture1DArray] = BufferType.Container.RWTexture1DArray,
            [BufferType.Container.Texture2DArray] = BufferType.Container.RWTexture2DArray,
            [BufferType.Container.TextureCubeArray] = BufferType.Container.RWTextureCubeArray,
        };

        static readonly Dictionary<BufferType.Container, BufferType.Container> k_ToReadOnly = BuildToReadOnly();

        static Dictionary<BufferType.Container, BufferType.Container> BuildToReadOnly()
        {
            var toReadOnly = new Dictionary<BufferType.Container, BufferType.Container>();
            foreach (var pair in k_ToReadWrite)
                toReadOnly.Add(pair.Value, pair.Key);
            return toReadOnly;
        }
    }
}
