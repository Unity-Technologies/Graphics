using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    public interface IVolumeComponentFilter : IEquatable<IVolumeComponentFilter>
    {
        bool IsAccepted([DisallowNull] Type subjectType);
    }
}
