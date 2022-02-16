using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Implement this interface to create a filter of <typeparamref name="T"/>.
    /// </summary>
    public interface IFilter<T> : IEquatable<IFilter<T>>
    {
        bool IsAccepted(T subjectType);
    }
}
