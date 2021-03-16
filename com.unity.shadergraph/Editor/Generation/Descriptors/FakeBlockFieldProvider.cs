using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    class FakeBlockFieldProvider : IBlockFieldProvider
    {
        public string uniqueNamespace { get; private set; }
        public IEnumerable<(BlockFieldSignature, BlockFieldDescriptor)> recognizedBlockFieldSignatures { get; private set; }

        public FakeBlockFieldProvider(string providerNamespace)
        {
            this.uniqueNamespace = providerNamespace;
            recognizedBlockFieldSignatures = new List<(BlockFieldSignature, BlockFieldDescriptor)>();
        }
    }
}
