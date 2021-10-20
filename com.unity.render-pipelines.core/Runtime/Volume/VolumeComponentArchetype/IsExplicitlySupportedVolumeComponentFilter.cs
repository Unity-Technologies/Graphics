using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    public sealed class IsExplicitlySupportedVolumeComponentFilter : IVolumeComponentFilter
    {
        [NotNull]
        Type targetType { get; }

        public IsExplicitlySupportedVolumeComponentFilter([DisallowNull] Type targetType)
        {
            this.targetType = targetType;
        }

        public bool IsAccepted(Type subjectType)
        {
            return IsSupportedOn.IsExplicitlySupportedBy(subjectType, targetType);
        }

        bool Equals(IsExplicitlySupportedVolumeComponentFilter other)
        {
            return targetType == other.targetType;
        }

        public bool Equals(IVolumeComponentFilter other)
        {
            return other is IsExplicitlySupportedVolumeComponentFilter filter && Equals(filter);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((IsExplicitlySupportedVolumeComponentFilter)obj);
        }

        public override int GetHashCode()
        {
            return (targetType != null ? targetType.GetHashCode() : 0);
        }
    }
}
