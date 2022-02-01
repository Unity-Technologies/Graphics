using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Implement this interface to create a volume component filter.
    ///
    /// Use it with <see cref="VolumeComponentArchetype.FromFilterCached{TFilter}"/> to create a <see cref="VolumeComponentArchetype"/>.
    /// </summary>
    public interface IFilter<T> : IEquatable<IFilter<T>>
    {
        bool IsAccepted(T subjectType);
    }
}
