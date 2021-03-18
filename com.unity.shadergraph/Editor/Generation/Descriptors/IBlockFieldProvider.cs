using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    internal interface IBlockFieldProviderInfo
    {
        string uniqueNamespace { get; }
    }

    internal interface IBlockFieldProvider : IBlockFieldProviderInfo
    {
        IEnumerable<(BlockFieldSignature blockFieldSignature, BlockFieldDescriptor blockFieldDescriptor)> recognizedBlockFieldSignatures { get; }
    }
}
