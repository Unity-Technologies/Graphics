using System;

namespace UnityEngine.Rendering.UIGen
{
    public partial class UIDefinition
    {
        public struct CategoryId : IEquatable<CategoryId>
        {
            string m_Name;

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
        }
    }
}
