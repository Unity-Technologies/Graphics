
namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Abstract base class for managing cache data associated with a graph.
    /// Provides caching capabilities with on-demand rebuilding of the data.
    /// </summary>
    /// <typeparam name="T">The type of cache data being managed. Must be a reference type.</typeparam>
    /*public*/ abstract class GraphCacheData<T> where T : class
    {
        /// <summary>
        /// Weak reference to the graph owning the cached data.
        /// </summary>
        protected Handle<IReadOnlyGraph> m_Owner;

        /// <summary>
        /// Cached data, if it has been generated.
        /// </summary>
        protected T m_Cache;

        /// <summary>
        /// Gets whether the cache data is valid. Data is considered valid when both
        /// the owner graph reference is valid and the cache contains a non-null value.
        /// </summary>
        public bool Valid => m_Owner.Valid && m_Cache != null;

        /// <summary>
        /// Gets the cache data, automatically rebuilding it if it's not valid.
        /// </summary>
        public T Data
        {
            get
            {
                if (!Valid)
                {
                    Rebuild();
                }
                return m_Cache;
            }
        }

        internal GraphCacheData(TaskGraph owner)
        {
            m_Owner = owner;
        }

        /// <summary>
        /// Implicitly converts the cache data object to its underlying data type.
        /// </summary>
        /// <param name="cacheData">The cache data object to convert.</param>
        /// <returns>The underlying data of type T.</returns>
        public static implicit operator T(GraphCacheData<T> cacheData) => cacheData.Data;

        /// <summary>
        /// Manually rebuilds the cache data by updating the owner reference and
        /// invoking the <see cref="Build"/> method.
        /// </summary>
        public void Rebuild()
        {
            m_Owner.Update();
            m_Cache = Build();
        }

        /// <summary>
        /// Builds an updated version of the cache data.
        /// </summary>
        /// <returns>The update version of the cache data.</returns>
        protected abstract T Build();
    }

    /// <summary>
    /// A concrete implementation of <see cref="GraphCacheData{T}"/> that uses a delegate
    /// to build the cache data.
    /// </summary>
    /// <typeparam name="T">The type of cache data being managed. Must be a reference type.</typeparam>
    /*public*/ class DelegateGraphCacheData<T> : GraphCacheData<T> where T : class
    {
        System.Func<IReadOnlyGraph, T> m_Builder;

        internal DelegateGraphCacheData(TaskGraph owner, System.Func<IReadOnlyGraph, T> builder, bool build = false) : base(owner)
        {
            m_Builder = builder;
            if (build)
            {
                Rebuild();
            }
        }

        /// <summary>
        /// Updates the owner reference and returns the current instance.
        /// </summary>
        /// <returns>The current <see cref="DelegateGraphCacheData{T}"/> instance.</returns>
        public DelegateGraphCacheData<T> Refresh()
        {
            m_Owner.Update();
            return this;
        }

        /// <summary>
        /// Builds an updated version of the cache data. Uses the provided delegate to build the cache data.
        /// </summary>
        /// <returns>The update version of the cache data.</returns>
        protected override T Build()
        {
            return m_Builder(m_Owner.Ref);
        }
    }
}
