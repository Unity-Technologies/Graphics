using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    /// <summary>
    /// NativeArray extension methods.
    /// </summary>
    [BurstCompatible]
    public unsafe static class NativeArrayExtensions
    {
        /// <summary>
        /// Determines whether an element is in the native array.
        /// </summary>
        /// <typeparam name="T">The type of values in the array.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="array">Array to perform search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True, if element is found.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static bool Contains<T, U>(this NativeArray<T> array, U value) where T : struct, IEquatable<U>
        {
            return IndexOf<T, U>(array.GetUnsafeReadOnlyPtr(), array.Length, value) != -1;
        }

        /// <summary>
        /// Searches for the specified element in native array.
        /// </summary>
        /// <typeparam name="T">The type of values in the array.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="array">Array to perform search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The zero-based index of the first occurrence element if found, otherwise returns -1.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static int IndexOf<T, U>(this NativeArray<T> array, U value) where T : struct, IEquatable<U>
        {
            return IndexOf<T, U>(array.GetUnsafeReadOnlyPtr(), array.Length, value);
        }

        /// <summary>
        /// Determines whether an element is in the native array.
        /// </summary>
        /// <typeparam name="T">The type of values in the array.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="array">Array to perform search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True, if element is found.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static bool Contains<T, U>(this NativeArray<T>.ReadOnly array, U value) where T : struct, IEquatable<U>
        {
            return IndexOf<T, U>(array.m_Buffer, array.m_Length, value) != -1;
        }

        /// <summary>
        /// Searches for the specified element in native array.
        /// </summary>
        /// <typeparam name="T">The type of values in the array.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="array">Array to perform search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The zero-based index of the first occurrence element if found, otherwise returns -1.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static int IndexOf<T, U>(this NativeArray<T>.ReadOnly array, U value) where T : struct, IEquatable<U>
        {
            return IndexOf<T, U>(array.m_Buffer, array.m_Length, value);
        }

        /// <summary>
        /// Determines whether an element is in the native list.
        /// </summary>
        /// <typeparam name="T">The type of values in the list.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="list">List to perform search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True, if element is found.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static bool Contains<T, U>(this NativeList<T> list, U value) where T : struct, IEquatable<U>
        {
            return IndexOf<T, U>(list.GetUnsafeReadOnlyPtr(), list.Length, value) != -1;
        }

        /// <summary>
        /// Searches for the specified element in native list.
        /// </summary>
        /// <typeparam name="T">The type of values in the list.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="list">List to perform search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The zero-based index of the first occurrence element if found, otherwise returns -1.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static int IndexOf<T, U>(this NativeList<T> list, U value) where T : struct, IEquatable<U>
        {
            return IndexOf<T, U>(list.GetUnsafeReadOnlyPtr(), list.Length, value);
        }

        /// <summary>
        /// Determines whether an element is in the array.
        /// </summary>
        /// <typeparam name="T">The type of values in the array.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="ptr">Pointer to first element to perform search.</param>
        /// <param name="length">Number of elements to perform search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True, if element is found.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static bool Contains<T, U>(void* ptr, int length, U value) where T : struct, IEquatable<U>
        {
            return IndexOf<T, U>(ptr, length, value) != -1;
        }

        /// <summary>
        /// Searches for the specified element in array.
        /// </summary>
        /// <typeparam name="T">The type of values in the array.</typeparam>
        /// <param name="array">Array to perform search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The zero-based index of the first occurrence element if found, otherwise returns -1.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public static int IndexOf<T>(this NativeArray<T> array, T value) where T : struct, IComparable<T>
        {
            for (int i = 0; i != array.Length; i++)
            {
                if (array[i].CompareTo(value) == 0)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Searches for the specified element in array.
        /// </summary>
        /// <typeparam name="T">The type of values in the array.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="ptr">Pointer to first element to perform search.</param>
        /// <param name="length">Number of elements to perform search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The zero-based index of the first occurrence element if found, otherwise returns -1.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static int IndexOf<T, U>(void* ptr, int length, U value) where T : struct, IEquatable<U>
        {
            for (int i = 0; i != length; i++)
            {
                if (UnsafeUtility.ReadArrayElement<T>(ptr, i).Equals(value))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Reinterpret a native array as being of another type, aliasing its contents via type punning.
        /// </summary>
        /// <param name="array">The array to alias</param>
        /// <typeparam name="T">Source type of array elements</typeparam>
        /// <typeparam name="U">Target type of array elements</typeparam>
        /// <returns>The same array, with a different type of element</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static NativeArray<U> Reinterpret<T, U>(this NativeArray<T> array) where U : struct where T : struct
        {
            var tSize = UnsafeUtility.SizeOf<T>();
            var uSize = UnsafeUtility.SizeOf<U>();

            var byteLen = ((long)array.Length) * tSize;
            var uLen = byteLen / uSize;

            CheckReinterpretSize<T, U>(ref array);

            var ptr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(array);
            var result = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<U>(ptr, (int)uLen, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var handle = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(array);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref result, handle);
#endif

            return result;
        }

        /// <summary>
        /// Returns true if the Length and the content of the two NativeArray's are the same.
        /// </summary>
        /// <typeparam name="T">Source type of array elements</typeparam>
        /// <param name="array"></param>
        /// <param name="other"></param>
        /// <returns>Returns true if both array are equal.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public static bool ArraysEqual<T>(this NativeArray<T> array, NativeArray<T> other) where T : struct, IEquatable<T>
        {
            if (array.Length != other.Length)
                return false;

            for (int i = 0; i != array.Length; i++)
            {
                if (!array[i].Equals(other[i]))
                    return false;
            }

            return true;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckReinterpretSize<T, U>(ref NativeArray<T> array) where U : struct where T : struct
        {
            var tSize = UnsafeUtility.SizeOf<T>();
            var uSize = UnsafeUtility.SizeOf<U>();

            var byteLen = ((long)array.Length) * tSize;
            var uLen = byteLen / uSize;

            if (uLen * uSize != byteLen)
            {
                throw new InvalidOperationException($"Types {typeof(T)} (array length {array.Length}) and {typeof(U)} cannot be aliased due to size constraints. The size of the types and lengths involved must line up.");
            }
        }
    }
}
