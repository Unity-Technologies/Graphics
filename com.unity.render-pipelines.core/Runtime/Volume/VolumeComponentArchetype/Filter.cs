using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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

    public sealed class IsVisibleVolumeComponentFilter : IFilter<VolumeComponentType>
    {
        static readonly IsVisibleVolumeComponentFilter k_True = new IsVisibleVolumeComponentFilter(true);
        static readonly IsVisibleVolumeComponentFilter k_False = new IsVisibleVolumeComponentFilter(false);

        readonly bool m_Visible;

        public static IsVisibleVolumeComponentFilter FromIsVisible(bool isVisible)
            => isVisible ? k_True : k_False;

        public static bool IsVisible(VolumeComponentType type)
            => !type.AsType().GetCustomAttributes(true)
                .Any(attr => attr is HideInInspector or ObsoleteAttribute);

        IsVisibleVolumeComponentFilter(bool visible)
        {
            m_Visible = visible;
        }

        public bool IsAccepted(VolumeComponentType subjectType)
        {
            var isVisible = IsVisible(subjectType);
            return m_Visible && isVisible || !m_Visible && !isVisible;
        }

        bool Equals(IsVisibleVolumeComponentFilter other) => true;

        public bool Equals(IFilter<VolumeComponentType> other)
        {
            return other is IsVisibleVolumeComponentFilter filter && Equals(filter);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((IsVisibleVolumeComponentFilter)obj);
        }

        public override int GetHashCode() => m_Visible.GetHashCode();
    }
}
