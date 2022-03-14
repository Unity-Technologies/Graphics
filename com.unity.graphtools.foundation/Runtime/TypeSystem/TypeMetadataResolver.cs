using System.Collections.Concurrent;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Default implementation for a type metadata resolver.
    /// </summary>
    public class TypeMetadataResolver : ITypeMetadataResolver
    {
        readonly ConcurrentDictionary<TypeHandle, ITypeMetadata> m_MetadataCache
            = new ConcurrentDictionary<TypeHandle, ITypeMetadata>();

        /// <inheritdoc />
        public ITypeMetadata Resolve(TypeHandle th)
        {
            if (!m_MetadataCache.TryGetValue(th, out ITypeMetadata metadata))
            {
                metadata = m_MetadataCache.GetOrAdd(th, t => new TypeMetadata(t, th.Resolve()));
            }
            return metadata;
        }
    }
}
