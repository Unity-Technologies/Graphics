namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// EXPERIMENTAL: Iterate by reference over a collection.
    ///
    /// similar to <see cref="System.Collections.Generic.IEnumerator{T}"/> but with a reference to the strong type.
    /// </summary>
    /// <typeparam name="T">The type of the iterator.</typeparam>
    public interface IRefEnumerator<T>
    {
        /// <summary>A reference to the current value.</summary>
        ref readonly T current { get; }

        /// <summary>Move to the next value.</summary>
        /// <returns><c>true</c> when a value was found, <c>false</c> when the enumerator has completed.</returns>
        bool MoveNext();

        /// <summary>Reset the enumerator to its initial state.</summary>
        void Reset();
    }
}
