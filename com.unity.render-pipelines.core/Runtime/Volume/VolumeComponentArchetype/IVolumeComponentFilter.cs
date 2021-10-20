using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Implement this interface to create a volume component filter.
    ///
    /// Use it with <see cref="VolumeComponentArchetype.FromFilter{TFilter}"/> to create a <see cref="VolumeComponentArchetype"/>.
    /// </summary>
    public interface IVolumeComponentFilter : IEquatable<IVolumeComponentFilter>
    {
        bool IsAccepted([DisallowNull] Type subjectType);
    }
}
