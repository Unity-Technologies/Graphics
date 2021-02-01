using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Collections
{
    [BurstCompatible]
    unsafe internal struct Memory
    {
        internal const long k_MaximumRamSizeInBytes = 1L << 40; // a terabyte

        [BurstCompatible]
        internal struct Unmanaged
        {
            internal static void* Allocate(long size, int align, Allocator allocator)
            {
                return Array.Resize(null, 0, 1, allocator, size, align);
            }

            internal static void Free(void* pointer, Allocator allocator)
            {
                if (pointer == null)
                    return;
                Array.Resize(pointer, 1, 0, allocator, 1, 1);
            }

            [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
            internal static T* Allocate<T>(Allocator allocator) where T : unmanaged
            {
                return Array.Resize<T>(null, 0, 1, allocator);
            }

            [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
            internal static void Free<T>(T* pointer, Allocator allocator) where T : unmanaged
            {
                if (pointer == null)
                    return;
                Array.Resize(pointer, 1, 0, allocator);
            }

            [BurstCompatible]
            internal struct Array
            {
                static bool IsCustom(Allocator allocator)
                {
                    return (int) allocator >= AllocatorManager.FirstUserIndex;
                }

                static void* CustomResize(void* oldPointer, long oldCount, long newCount, Allocator allocator, long size, int align)
                {
                    AllocatorManager.Block block = default;
                    block.Range.Allocator = new AllocatorManager.AllocatorHandle{Value=(int)allocator};
                    block.Range.Items = (int)newCount;
                    block.Range.Pointer = (IntPtr)oldPointer;
                    block.BytesPerItem = (int)size;
                    block.Alignment = align;
                    block.AllocatedItems = (int)oldCount;
                    var error = AllocatorManager.Try(ref block);
                    AllocatorManager.CheckFailedToAllocate(error);
                    return (void*)block.Range.Pointer;
                }

                internal static void* Resize(void* oldPointer, long oldCount, long newCount, Allocator allocator,
                    long size, int align)
                {
                    if (IsCustom(allocator))
                        return CustomResize(oldPointer, oldCount, newCount, allocator, size, align);
                    void* newPointer = default;
                    if (newCount > 0)
                    {
                        long bytesToAllocate = newCount * size;
                        CheckByteCountIsReasonable(bytesToAllocate);
                        newPointer = UnsafeUtility.Malloc(bytesToAllocate, align, allocator);
                        if (oldCount > 0)
                        {
                            long count = math.min(oldCount, newCount);
                            long bytesToCopy = count * size;
                            CheckByteCountIsReasonable(bytesToCopy);
                            UnsafeUtility.MemCpy(newPointer, oldPointer, bytesToCopy);
                        }
                    }
                    if (oldCount > 0)
                        UnsafeUtility.Free(oldPointer, allocator);
                    return newPointer;
                }

                [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
                internal static T* Resize<T>(T* oldPointer, long oldCount, long newCount, Allocator allocator) where T : unmanaged
                {
                    return (T*)Resize((byte*)oldPointer, oldCount, newCount, allocator, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>());
                }

                [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
                internal static T* Allocate<T>(long count, Allocator allocator)
                    where T : unmanaged
                {
                    return Resize<T>(null, 0, count, allocator);
                }

                [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
                internal static void Free<T>(T* pointer, long count, Allocator allocator)
                    where T : unmanaged
                {
                    if (pointer == null)
                        return;
                    Resize(pointer, count, 0, allocator);
                }
            }
        }

        [BurstCompatible]
        internal struct Array
        {
            [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
            internal static void Set<T>(T* pointer, long count, T t = default) where T : unmanaged
            {
                long bytesToSet = count * UnsafeUtility.SizeOf<T>();
                CheckByteCountIsReasonable(bytesToSet);
                for (var i = 0; i < count; ++i)
                    pointer[i] = t;
            }

            [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
            internal static void Clear<T>(T* pointer, long count) where T : unmanaged
            {
                long bytesToClear = count * UnsafeUtility.SizeOf<T>();
                CheckByteCountIsReasonable(bytesToClear);
                UnsafeUtility.MemClear(pointer, bytesToClear);
            }

            [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
            internal static void Copy<T>(T* dest, T* src, long count) where T : unmanaged
            {
                long bytesToCopy = count * UnsafeUtility.SizeOf<T>();
                CheckByteCountIsReasonable(bytesToCopy);
                UnsafeUtility.MemCpy(dest, src, bytesToCopy);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTION_CHECKS")]
        internal static void CheckByteCountIsReasonable(long size)
        {
            if (size < 0)
                throw new InvalidOperationException("Attempted to operate on {size} bytes of memory: nonsensical");
            if (size > k_MaximumRamSizeInBytes)
                throw new InvalidOperationException("Attempted to operate on {size} bytes of memory: too big");
        }

    }
}
