namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for type metadata resolvers.
    /// </summary>
    public interface ITypeMetadataResolver
    {
        /// <summary>
        /// Gets the <see cref="ITypeMetadata"/> for a <see cref="TypeHandle"/>.
        /// </summary>
        /// <param name="th">The type handle for which to get the metadata.</param>
        /// <returns>The metadata for the type handle.</returns>
        ITypeMetadata Resolve(TypeHandle th);
    }
}
