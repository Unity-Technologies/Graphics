using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
#if !NET_DOTS
using System.Reflection;
#endif

namespace Unity.Collections
{
    /// <summary>
    /// INativeDisposable provides a mechanism for scheduling release of unmanaged resources.
    /// </summary>
    public interface INativeDisposable : IDisposable
    {
        /// <summary>
        /// Safely disposes of this container and deallocates its memory when the jobs that use it have completed.
        /// </summary>
        /// <remarks>You can call this function dispose of the container immediately after scheduling the job. Pass
        /// the [JobHandle](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html) returned by
        /// the [Job.Schedule](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJobExtensions.Schedule.html)
        /// method using the `jobHandle` parameter so the job scheduler can dispose the container after all jobs
        /// using it have run.</remarks>
        /// <param name="inputDeps">The job handle or handles for any scheduled jobs that use this container.</param>
        /// <returns>A new job handle containing the prior handles as well as the handle for the job that deletes
        /// the container.</returns>
        JobHandle Dispose(JobHandle inputDeps);
    }

    /// <summary>
    ///
    /// </summary>
    [BurstCompatible]
    public static class CollectionHelper
    {
        /// <summary>
        ///
        /// </summary>
        public const int CacheLineSize = JobsUtility.CacheLineSize;

        [StructLayout(LayoutKind.Explicit)]
        internal struct LongDoubleUnion
        {
            [FieldOffset(0)]
            internal long longValue;

            [FieldOffset(0)]
            internal double doubleValue;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int Log2Floor(int value)
        {
            return 31 - math.lzcnt((uint)value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int Log2Ceil(int value)
        {
            return 32 - math.lzcnt((uint)value - 1);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="size"></param>
        /// <param name="alignmentPowerOfTwo"></param>
        /// <returns></returns>
        public static int Align(int size, int alignmentPowerOfTwo)
        {
            if (alignmentPowerOfTwo == 0)
                return size;

            CheckIntPositivePowerOfTwo(alignmentPowerOfTwo);

            return (size + alignmentPowerOfTwo - 1) & ~(alignmentPowerOfTwo - 1);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="size"></param>
        /// <param name="alignmentPowerOfTwo"></param>
        /// <returns></returns>
        public static ulong Align(ulong size, ulong alignmentPowerOfTwo)
        {
            if (alignmentPowerOfTwo == 0)
                return size;

            CheckUlongPositivePowerOfTwo(alignmentPowerOfTwo);

            return (size + alignmentPowerOfTwo - 1) & ~(alignmentPowerOfTwo - 1);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="p"></param>
        /// <param name="alignmentPowerOfTwo"></param>
        /// <returns></returns>
        public static unsafe bool IsAligned(void* p, int alignmentPowerOfTwo)
        {
            CheckIntPositivePowerOfTwo(alignmentPowerOfTwo);
            return ((ulong)p & ((ulong)alignmentPowerOfTwo - 1)) == 0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="alignmentPowerOfTwo"></param>
        /// <returns></returns>
        public static bool IsAligned(ulong offset, int alignmentPowerOfTwo)
        {
            CheckIntPositivePowerOfTwo(alignmentPowerOfTwo);
            return (offset & ((ulong)alignmentPowerOfTwo - 1)) == 0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsPowerOfTwo(int value)
        {
            return (value & (value - 1)) == 0;
        }

        /// <summary>
        /// Returns hash value of memory block. Function is using djb2 (non-cryptographic hash).
        /// </summary>
        /// <param name="ptr">A pointer to the buffer.</param>
        /// <param name="bytes">Number of bytes to hash.</param>
        /// <returns></returns>
        public static unsafe uint Hash(void* ptr, int bytes)
        {
            // djb2 - Dan Bernstein hash function
            // http://web.archive.org/web/20190508211657/http://www.cse.yorku.ca/~oz/hash.html
            byte* str = (byte*)ptr;
            ulong hash = 5381;
            while (bytes > 0)
            {
                ulong c = str[--bytes];
                hash = ((hash << 5) + hash) + c;
            }
            return (uint)hash;
        }

        [NotBurstCompatible]
        internal static void WriteLayout(Type type)
        {
#if !NET_DOTS
            Console.WriteLine("   Offset | Bytes  | Name     Layout: {0}", type.Name);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                Console.WriteLine("   {0, 6} | {1, 6} | {2}"
                    , Marshal.OffsetOf(type, field.Name)
                    , Marshal.SizeOf(field.FieldType)
                    , field.Name
                );
            }
#else
            _ = type;
#endif
        }

        internal static bool ShouldDeallocate(Allocator allocator)
        {
            // Allocator.Invalid == container is not initialized.
            // Allocator.None    == container is initialized, but container doesn't own data.
            return allocator > Allocator.None;
        }

        internal static bool ShouldDeallocate(AllocatorManager.AllocatorHandle allocator)
        {
            // Allocator.Invalid == container is not initialized.
            // Allocator.None    == container is initialized, but container doesn't own data.
            return allocator.Value > (int)Allocator.None;
        }

        /// <summary>
        /// Tell Burst that an integer can be assumed to map to an always positive value.
        /// </summary>
        /// <param name="value">The integer that is always positive.</param>
        /// <returns>Returns `x`, but allows the compiler to assume it is always positive.</returns>
        [return: AssumeRange(0, int.MaxValue)]
        internal static int AssumePositive(int value)
        {
            return value;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard] // Must use BurstDiscard because UnsafeUtility.IsUnmanaged is not burstable.
        [NotBurstCompatible]
        internal static void CheckIsUnmanaged<T>()
        {
            if (!UnsafeUtility.IsValidNativeContainerElementType<T>())
            {
                throw new ArgumentException($"{typeof(T)} used in native collection is not blittable, not primitive, or contains a type tagged as NativeContainer");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckIntPositivePowerOfTwo(int value)
        {
            var valid = (value > 0) && ((value & (value - 1)) == 0);
            if (!valid)
            {
                throw new ArgumentException("Alignment requested: {value} is not a non-zero, positive power of two.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckUlongPositivePowerOfTwo(ulong value)
        {
            var valid = (value > 0) && ((value & (value - 1)) == 0);
            if (!valid)
            {
                throw new ArgumentException("Alignment requested: {value} is not a non-zero, positive power of two.");
            }
        }
    }
}
