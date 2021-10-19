using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    public sealed class IsSupportedVolumeComponentFilter : IVolumeComponentFilter
    {
        [NotNull]
        Type targetType { get; }

        public IsSupportedVolumeComponentFilter([DisallowNull] Type targetType)
        {
            this.targetType = targetType;
        }

        public bool IsAccepted(Type subjectType)
        {
            return IsSupportedOn.IsSupportedBy(subjectType, targetType);
        }

        bool Equals(IsSupportedVolumeComponentFilter other)
        {
            return targetType == other.targetType;
        }

        public bool Equals(IVolumeComponentFilter other)
        {
            return other is IsSupportedVolumeComponentFilter filter && Equals(filter);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((IsSupportedVolumeComponentFilter)obj);
        }

        public override int GetHashCode()
        {
            return (targetType != null ? targetType.GetHashCode() : 0);
        }
    }
}
