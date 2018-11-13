using System;

namespace UnityEditor.ShaderGraph
{
    public struct Identifier : IEquatable<Identifier>
    {
        uint m_Version;
        int m_Index;

        public Identifier(int index, uint version = 1)
        {
            m_Version = version;
            m_Index = index;
        }

        public void IncrementVersion()
        {
            if (m_Version == uint.MaxValue)
                m_Version = 1;
            else
                m_Version++;
        }

        public uint version
        {
            get { return m_Version; }
        }

        public int index
        {
            get { return m_Index; }
        }

        public bool valid
        {
            get { return m_Version != 0; }
        }

        public bool Equals(Identifier other)
        {
            return m_Version == other.m_Version && m_Index == other.m_Index;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Identifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)m_Version * 397) ^ m_Index;
            }
        }

        public static bool operator ==(Identifier left, Identifier right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Identifier left, Identifier right)
        {
            return !left.Equals(right);
        }
    }
}
