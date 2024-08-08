using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.Rendering
{
    internal ref struct HashFNV1A32
    {
        /// <summary>
        /// FNV prime.
        /// </summary>
        const uint k_Prime = 16777619;

        /// <summary>
        /// FNV offset basis.
        /// </summary>
        const uint k_OffsetBasis = 2166136261;

        uint m_Hash;

        public static HashFNV1A32 Create()
        {
            return new HashFNV1A32 { m_Hash = k_OffsetBasis };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(in int input)
        {
            unchecked
            {
                m_Hash = (m_Hash ^ (uint)input) * k_Prime;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(in uint input)
        {
            unchecked
            {
                m_Hash = (m_Hash ^ input) * k_Prime;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(in bool input)
        {
            m_Hash = (m_Hash ^ (input ? 1u : 0u)) * k_Prime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(in float input)
        {
            unchecked
            {
                m_Hash = (m_Hash ^ (uint)input.GetHashCode()) * k_Prime;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(in double input)
        {
            unchecked
            {
                m_Hash = (m_Hash ^ (uint)input.GetHashCode()) * k_Prime;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(in Vector2 input)
        {
            unchecked
            {
                m_Hash = (m_Hash ^ (uint)input.GetHashCode()) * k_Prime;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(in Vector3 input)
        {
            unchecked
            {
                m_Hash = (m_Hash ^ (uint)input.GetHashCode()) * k_Prime;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(in Vector4 input)
        {
            unchecked
            {
                m_Hash = (m_Hash ^ (uint)input.GetHashCode()) * k_Prime;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append<T>(T input) where T : struct
        {
            unchecked
            {
                m_Hash = (m_Hash ^ (uint)input.GetHashCode()) * k_Prime;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(Delegate del)
        {
            unchecked
            {
                m_Hash = (m_Hash ^ (uint)GetFuncHashCode(del)) * k_Prime;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetFuncHashCode(Delegate del)
        {
            return del.Method.GetHashCode() ^ RuntimeHelpers.GetHashCode(del.Target);
        }

        public int value => (int)m_Hash;

        public override int GetHashCode()
        {
            return value;
        }
    }
}
