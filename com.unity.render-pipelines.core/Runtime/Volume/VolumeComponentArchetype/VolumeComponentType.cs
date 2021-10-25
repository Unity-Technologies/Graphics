using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    public readonly struct VolumeComponentType : IEquatable<VolumeComponentType>
    {
        readonly Type m_Type;
        VolumeComponentType(Type type) => m_Type = type;

        public static bool FromType([AllowNull] Type type, out VolumeComponentType componentType)
        {
            if (type?.IsSubclassOf(typeof(VolumeComponent)) ?? false)
            {
                componentType = new VolumeComponentType(type);
                return true;
            }

            componentType = default;
            return false;
        }

        [return: NotNull]
        public Type AsType() => m_Type;

        /// <summary>
        /// Safety: type must be non null and a subclass of <see cref="VolumeComponent"/>.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static unsafe VolumeComponentType FromTypeUnsafe([DisallowNull] Type type) => new VolumeComponentType(type);

        public static explicit operator Type(in VolumeComponentType type) => type.m_Type;

        public bool Equals(VolumeComponentType other)
        {
            return Equals(m_Type, other.m_Type);
        }

        public override bool Equals(object obj)
        {
            return obj is VolumeComponentType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (m_Type != null ? m_Type.GetHashCode() : 0);
        }

        public static bool operator ==(in VolumeComponentType l, in VolumeComponentType r) => l.Equals(r);
        public static bool operator !=(in VolumeComponentType l, in VolumeComponentType r) => !l.Equals(r);
    }
}
