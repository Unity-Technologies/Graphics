using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    public sealed class IsSupportedVolumeComponentFilter : IFilter<VolumeComponentType>
    {
        [NotNull]
        Type targetType { get; }

        IsSupportedVolumeComponentFilter([DisallowNull] Type targetType)
        {
            this.targetType = targetType;
        }

        public bool IsAccepted(VolumeComponentType subjectType)
        {
            return IsSupportedOn.IsSupportedBy((Type)subjectType, targetType);
        }

        bool Equals(IsSupportedVolumeComponentFilter other)
        {
            return targetType == other.targetType;
        }

        public bool Equals(IFilter<VolumeComponentType> other)
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

        public static IsSupportedVolumeComponentFilter FromType([DisallowNull] Type targetType)
            => new IsSupportedVolumeComponentFilter(targetType);

        public static bool operator ==(IsSupportedVolumeComponentFilter l, IsSupportedVolumeComponentFilter r)
            => !ReferenceEquals(null, l) && l.Equals(r) || ReferenceEquals(null, r);
        public static bool operator !=(IsSupportedVolumeComponentFilter l, IsSupportedVolumeComponentFilter r)
            => !(l == r);
    }

    public sealed class IsExplicitlySupportedVolumeComponentFilter : IFilter<VolumeComponentType>
    {
        [NotNull]
        Type targetType { get; }

        IsExplicitlySupportedVolumeComponentFilter([DisallowNull] Type targetType)
        {
            this.targetType = targetType;
        }

        public bool IsAccepted(VolumeComponentType subjectType)
        {
            return IsSupportedOn.IsExplicitlySupportedBy((Type)subjectType, targetType);
        }

        bool Equals(IsExplicitlySupportedVolumeComponentFilter other)
        {
            return targetType == other.targetType;
        }

        public bool Equals(IFilter<VolumeComponentType> other)
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

        public static IsExplicitlySupportedVolumeComponentFilter FromType([DisallowNull] Type targetType)
            => new IsExplicitlySupportedVolumeComponentFilter(targetType);

        public static bool operator ==(IsExplicitlySupportedVolumeComponentFilter l, IsExplicitlySupportedVolumeComponentFilter r)
            => !ReferenceEquals(null, l) && l.Equals(r) || ReferenceEquals(null, r);
        public static bool operator !=(IsExplicitlySupportedVolumeComponentFilter l, IsExplicitlySupportedVolumeComponentFilter r)
            => !(l == r);
    }
}
