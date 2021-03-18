using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    class BlockFieldProviderInfo : IBlockFieldProviderInfo
    {
        public string uniqueNamespace { get; private set;}
        public BlockFieldProviderInfo(string uniqueNamespace)
        {
            this.uniqueNamespace = uniqueNamespace;
        }
    }
    abstract class BlockFieldProvider : IBlockFieldProvider
    {
        IBlockFieldProviderInfo m_ProviderInfo;
        Func<IEnumerable<(BlockFieldSignature, BlockFieldDescriptor)>> m_GetSignatureMapFunc;
        // IBlockFieldProvider:
        public string uniqueNamespace { get => m_ProviderInfo.uniqueNamespace; }
        public IEnumerable<(BlockFieldSignature, BlockFieldDescriptor)> recognizedBlockFieldSignatures { get { return m_GetSignatureMapFunc(); } }
        public BlockFieldProvider(IBlockFieldProviderInfo info, Func<IEnumerable<(BlockFieldSignature, BlockFieldDescriptor)>> getSignatureMapFunc)
        {
            m_ProviderInfo = info;
            m_GetSignatureMapFunc = getSignatureMapFunc;
        }
    }
}
