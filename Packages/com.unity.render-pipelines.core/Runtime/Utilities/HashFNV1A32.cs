using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Incremental 32-bit FNV-1a hash builder.
    /// Use this for change detection or other non-cryptographic hashing where
    /// equally-shaped inputs should produce well-distributed hashes.
    /// </summary>
    /// <remarks>
    /// As a <c>ref struct</c>, instances cannot be stored in fields, boxed, or
    /// captured by lambdas/async methods; create one, append inputs, then read
    /// <see cref="value"/>.
    /// </remarks>
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

        /// <summary>
        /// Creates a new hash builder seeded with the FNV offset basis.
        /// </summary>
        /// <returns>An empty <see cref="HashFNV1A32"/> ready to receive <see cref="Append(in int)"/> calls.</returns>
        public static HashFNV1A32 Create()
        {
            return new HashFNV1A32 { m_Hash = k_OffsetBasis };
        }

        /// <summary>Mixes an <see cref="int"/> into the running hash.</summary>
        /// <param name="input">Value to fold in.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(in int input)
        {
            unchecked
            {
                m_Hash = (m_Hash ^ (uint)input) * k_Prime;
            }
        }

        /// <summary>Mixes a <see cref="uint"/> into the running hash.</summary>
        /// <param name="input">Value to fold in.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(in uint input)
        {
            unchecked
            {
                m_Hash = (m_Hash ^ input) * k_Prime;
            }
        }

        /// <summary>Mixes a <see cref="bool"/> into the running hash.</summary>
        /// <param name="input">Value to fold in.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(in bool input)
        {
            m_Hash = (m_Hash ^ (input ? 1u : 0u)) * k_Prime;
        }

        /// <summary>Mixes a <see cref="float"/> into the running hash.</summary>
        /// <param name="input">Value to fold in.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(in float input)
        {
            unchecked
            {
                m_Hash = (m_Hash ^ (uint)input.GetHashCode()) * k_Prime;
            }
        }

        /// <summary>Mixes a <see cref="double"/> into the running hash.</summary>
        /// <param name="input">Value to fold in.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(in double input)
        {
            unchecked
            {
                m_Hash = (m_Hash ^ (uint)input.GetHashCode()) * k_Prime;
            }
        }

        /// <summary>Mixes a <see cref="Vector2"/> into the running hash.</summary>
        /// <param name="input">Value to fold in.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(in Vector2 input)
        {
            Append(input.x);
            Append(input.y);
        }

        /// <summary>Mixes a <see cref="Vector3"/> into the running hash.</summary>
        /// <param name="input">Value to fold in.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(in Vector3 input)
        {
            Append(input.x);
            Append(input.y);
            Append(input.z);
        }

        /// <summary>Mixes a <see cref="Vector4"/> into the running hash.</summary>
        /// <param name="input">Value to fold in.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(in Vector4 input)
        {
            Append(input.x);
            Append(input.y);
            Append(input.z);
            Append(input.w);
        }

        /// <summary>Mixes any value-type input into the running hash via its <see cref="object.GetHashCode"/>.</summary>
        /// <typeparam name="T">Any value type.</typeparam>
        /// <param name="input">Value to fold in.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append<T>(T input) where T : struct
        {
            unchecked
            {
                m_Hash = (m_Hash ^ (uint)input.GetHashCode()) * k_Prime;
            }
        }

        /// <summary>The current hash value as a signed 32-bit integer.</summary>
        public int value => (int)m_Hash;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return value;
        }
    }

    static class DelegateHashCodeUtils
    {
        //Cache to prevent CompilerGeneratedAttribute extraction for known delegate
        static readonly Lazy<Dictionary<int, bool>> s_MethodHashCodeToSkipTargetHashMap = new(() => new Dictionary<int, bool>(64));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFuncHashCode(Delegate del)
        {
            //Get MethodInfo hash code as the main one to be used
            var methodHashCode = RuntimeHelpers.GetHashCode(del.Method);

            //Check if we are dealing with lambda or static delegates and skip target if we are.
            //Static methods have a null Target.
            //Lambdas have a CompilerGeneratedAttribute as they are generated by a compiler.
            //If Lambda have any captured variable Target hashcode will be different each time we re-create lambda.
            if (!s_MethodHashCodeToSkipTargetHashMap.Value.TryGetValue(methodHashCode, out var skipTarget))
            {
                skipTarget = del.Target == null || (
                    del.Method.DeclaringType?.IsNestedPrivate == true &&
                    Attribute.IsDefined(del.Method.DeclaringType, typeof(CompilerGeneratedAttribute), false)
                );

                s_MethodHashCodeToSkipTargetHashMap.Value[methodHashCode] = skipTarget;
            }

            //Combine method info hashcode and target hashcode if needed
            return skipTarget ? methodHashCode : methodHashCode ^ RuntimeHelpers.GetHashCode(del.Target);
        }

        //used for testing
        internal static int GetTotalCacheCount() => s_MethodHashCodeToSkipTargetHashMap.Value.Count;

        internal static void ClearCache() => s_MethodHashCodeToSkipTargetHashMap.Value.Clear();
    }
}
