using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    public partial class UIDefinition
    {
        public struct CategoryId : IEquatable<CategoryId>
        {
            public static readonly CategoryId Empty = default;
        
            [MustUseReturnValue]
            public static bool From(
                [DisallowNull] string name,
                out CategoryId CategoryId,
                [NotNullWhen(false)] out Exception error
            )
            {
                if (string.IsNullOrEmpty(name))
                {
                    error = new ArgumentException($"{nameof(CategoryId)} must not be empty");
                    CategoryId = Empty;
                    return false;
                }
        
                CategoryId = new CategoryId(name);
                error = default;
                return true;
            }
        
            public unsafe static CategoryId FromUnsafe([DisallowNull] string value)
                => new CategoryId(value);
        
            string m_Name;
        
            CategoryId(string name) {
                m_Name = name;
            }
        
            public bool Equals(CategoryId other)
            {
                return m_Name == other.m_Name;
            }
        
            public override bool Equals(object obj)
            {
                return obj is CategoryId other && Equals(other);
            }
        
            public override int GetHashCode()
            {
                return (m_Name != null ? m_Name.GetHashCode() : 0);
            }
        
            public static bool operator ==(in CategoryId l, in CategoryId r) => r.Equals(l);
            public static bool operator !=(in CategoryId l, in CategoryId r) => !r.Equals(l);
            public static explicit operator string(in CategoryId v) => v.m_Name;
        }
        public struct PropertyPath : IEquatable<PropertyPath>
        {
            public static readonly PropertyPath Empty = default;
        
            [MustUseReturnValue]
            public static bool From(
                [DisallowNull] string name,
                out PropertyPath PropertyPath,
                [NotNullWhen(false)] out Exception error
            )
            {
                if (string.IsNullOrEmpty(name))
                {
                    error = new ArgumentException($"{nameof(PropertyPath)} must not be empty");
                    PropertyPath = Empty;
                    return false;
                }
        
                PropertyPath = new PropertyPath(name);
                error = default;
                return true;
            }
        
            public unsafe static PropertyPath FromUnsafe([DisallowNull] string value)
                => new PropertyPath(value);
        
            string m_Name;
        
            PropertyPath(string name) {
                m_Name = name;
            }
        
            public bool Equals(PropertyPath other)
            {
                return m_Name == other.m_Name;
            }
        
            public override bool Equals(object obj)
            {
                return obj is PropertyPath other && Equals(other);
            }
        
            public override int GetHashCode()
            {
                return (m_Name != null ? m_Name.GetHashCode() : 0);
            }
        
            public static bool operator ==(in PropertyPath l, in PropertyPath r) => r.Equals(l);
            public static bool operator !=(in PropertyPath l, in PropertyPath r) => !r.Equals(l);
            public static explicit operator string(in PropertyPath v) => v.m_Name;
        }
        public struct PropertyName : IEquatable<PropertyName>
        {
            public static readonly PropertyName Empty = default;
        
            [MustUseReturnValue]
            public static bool From(
                [DisallowNull] string name,
                out PropertyName PropertyName,
                [NotNullWhen(false)] out Exception error
            )
            {
                if (string.IsNullOrEmpty(name))
                {
                    error = new ArgumentException($"{nameof(PropertyName)} must not be empty");
                    PropertyName = Empty;
                    return false;
                }
        
                PropertyName = new PropertyName(name);
                error = default;
                return true;
            }
        
            public unsafe static PropertyName FromUnsafe([DisallowNull] string value)
                => new PropertyName(value);
        
            string m_Name;
        
            PropertyName(string name) {
                m_Name = name;
            }
        
            public bool Equals(PropertyName other)
            {
                return m_Name == other.m_Name;
            }
        
            public override bool Equals(object obj)
            {
                return obj is PropertyName other && Equals(other);
            }
        
            public override int GetHashCode()
            {
                return (m_Name != null ? m_Name.GetHashCode() : 0);
            }
        
            public static bool operator ==(in PropertyName l, in PropertyName r) => r.Equals(l);
            public static bool operator !=(in PropertyName l, in PropertyName r) => !r.Equals(l);
            public static explicit operator string(in PropertyName v) => v.m_Name;
        }
        public struct PropertyTooltip : IEquatable<PropertyTooltip>
        {
            public static readonly PropertyTooltip Empty = default;
        
            [MustUseReturnValue]
            public static bool From(
                [DisallowNull] string name,
                out PropertyTooltip PropertyTooltip,
                [NotNullWhen(false)] out Exception error
            )
            {
                if (string.IsNullOrEmpty(name))
                {
                    error = new ArgumentException($"{nameof(PropertyTooltip)} must not be empty");
                    PropertyTooltip = Empty;
                    return false;
                }
        
                PropertyTooltip = new PropertyTooltip(name);
                error = default;
                return true;
            }
        
            public unsafe static PropertyTooltip FromUnsafe([DisallowNull] string value)
                => new PropertyTooltip(value);
        
            string m_Name;
        
            PropertyTooltip(string name) {
                m_Name = name;
            }
        
            public bool Equals(PropertyTooltip other)
            {
                return m_Name == other.m_Name;
            }
        
            public override bool Equals(object obj)
            {
                return obj is PropertyTooltip other && Equals(other);
            }
        
            public override int GetHashCode()
            {
                return (m_Name != null ? m_Name.GetHashCode() : 0);
            }
        
            public static bool operator ==(in PropertyTooltip l, in PropertyTooltip r) => r.Equals(l);
            public static bool operator !=(in PropertyTooltip l, in PropertyTooltip r) => !r.Equals(l);
            public static explicit operator string(in PropertyTooltip v) => v.m_Name;
        }
    }
}
