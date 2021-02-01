#if !UNITY_DOTSRUNTIME // can't use Burst function pointers from DOTS runtime (yet)
#define CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
#endif

#pragma warning disable 0649

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Unity.Collections
{
    /// <summary>
    ///
    /// </summary>
    public static class AllocatorManager
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="itemSizeInBytes"></param>
        /// <param name="alignmentInBytes"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public unsafe static void* Allocate(AllocatorHandle handle, int itemSizeInBytes, int alignmentInBytes, int items = 1)
        {
            Block block = default;
            block.Range.Allocator = handle;
            block.Range.Items = items;
            block.Range.Pointer = IntPtr.Zero;
            block.BytesPerItem = itemSizeInBytes;
            block.Alignment = alignmentInBytes;
            var error = Try(ref block);
            CheckFailedToAllocate(error);
            return (void*)block.Range.Pointer;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handle"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public unsafe static T* Allocate<T>(AllocatorHandle handle, int items = 1) where T : unmanaged
        {
            return (T*)Allocate(handle, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), items);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="pointer"></param>
        /// <param name="itemSizeInBytes"></param>
        /// <param name="alignmentInBytes"></param>
        /// <param name="items"></param>
        public unsafe static void Free(AllocatorHandle handle, void* pointer, int itemSizeInBytes, int alignmentInBytes,
            int items = 1)
        {
            if (pointer == null)
                return;
            Block block = default;
            block.Range.Allocator = handle;
            block.Range.Items = 0;
            block.Range.Pointer = (IntPtr)pointer;
            block.BytesPerItem = itemSizeInBytes;
            block.Alignment = alignmentInBytes;
            var error = Try(ref block);
            CheckFailedToFree(error);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="pointer"></param>
        public unsafe static void Free(AllocatorHandle handle, void* pointer)
        {
            Free(handle, pointer, 1, 1, 1);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handle"></param>
        /// <param name="pointer"></param>
        /// <param name="items"></param>
        public unsafe static void Free<T>(AllocatorHandle handle, T* pointer, int items = 1) where T : unmanaged
        {
            Free(handle, pointer, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), items);
        }

        /// <summary>
        /// Corresponds to Allocator.Invalid.
        /// </summary>
        public static readonly AllocatorHandle Invalid = new AllocatorHandle { Value = 0 };

        /// <summary>
        /// Corresponds to Allocator.None.
        /// </summary>
        public static readonly AllocatorHandle None = new AllocatorHandle { Value = 1 };

        /// <summary>
        /// Corresponds to Allocator.Temp.
        /// </summary>
        public static readonly AllocatorHandle Temp = new AllocatorHandle { Value = 2 };

        /// <summary>
        /// Corresponds to Allocator.TempJob.
        /// </summary>
        public static readonly AllocatorHandle TempJob = new AllocatorHandle { Value = 3 };

        /// <summary>
        /// Corresponds to Allocator.Persistent.
        /// </summary>
        public static readonly AllocatorHandle Persistent = new AllocatorHandle { Value = 4 };

        /// <summary>
        /// Corresponds to Allocator.AudioKernel.
        /// </summary>
        public static readonly AllocatorHandle AudioKernel = new AllocatorHandle { Value = 5 };

        #region Allocator Parts
        /// <summary>
        /// Delegate used for calling an allocator's allocation function.
        /// </summary>
        public delegate int TryFunction(IntPtr allocatorState, ref Block block);

        /// <summary>
        ///
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SmallAllocatorHandle
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name="a"></param>
            /// <returns></returns>
            public static implicit operator SmallAllocatorHandle(Allocator a) => new SmallAllocatorHandle { Value = (ushort)a };

            /// <summary>
            ///
            /// </summary>
            /// <param name="a"></param>
            /// <returns></returns>
            public static implicit operator SmallAllocatorHandle(AllocatorHandle a) => new SmallAllocatorHandle { Value = (ushort)a.Value };

            /// <summary>
            ///
            /// </summary>
            /// <param name="a"></param>
            /// <returns></returns>
            public static implicit operator AllocatorHandle(SmallAllocatorHandle a) => new AllocatorHandle { Value = a.Value };

            /// <summary>
            /// Index into a function table of allocation functions.
            /// </summary>
            public ushort Value;
        }

        /// <summary>
        /// Which allocator a Block's Range allocates from.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AllocatorHandle
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name="a"></param>
            /// <returns></returns>
            public static implicit operator AllocatorHandle(Allocator a) => new AllocatorHandle { Value = (int)a };

            /// <summary>
            /// Index into a function table of allocation functions.
            /// </summary>
            public int Value;

            /// <summary>
            /// Allocates a Block of memory from this allocator with requested number of items of a given type.
            /// </summary>
            /// <typeparam name="T">Type of item to allocate.</typeparam>
            /// <param name="block">Block of memory to allocate within.</param>
            /// <param name="Items">Number of items to allocate.</param>
            /// <returns>Error code from the given Block's allocate function.</returns>
            public int TryAllocate<T>(out Block block, int Items) where T : struct
            {
                block = new Block
                {
                    Range = new Range { Items = Items, Allocator = new AllocatorHandle { Value = Value } },
                    BytesPerItem = UnsafeUtility.SizeOf<T>(),
                    Alignment = 1 << math.min(3, math.tzcnt(UnsafeUtility.SizeOf<T>()))
                };
                var returnCode = Try(ref block);
                return returnCode;
            }

            /// <summary>
            /// Allocates a Block of memory from this allocator with requested number of items of a given type.
            /// </summary>
            /// <typeparam name="T">Type of item to allocate.</typeparam>
            /// <param name="Items">Number of items to allocate.</param>
            /// <returns>A Block of memory.</returns>
            public Block Allocate<T>(int Items) where T : struct
            {
                var error = TryAllocate<T>(out Block block, Items);
                CheckAllocatedSuccessfully(error);
                return block;
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            static void CheckAllocatedSuccessfully(int error)
            {
                if (error != 0)
                    throw new ArgumentException($"Error {error}: Failed to Allocate");
            }
        }

        /// <summary>
        ///
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BlockHandle
        {
            /// <summary>
            ///
            /// </summary>
            public ushort Value;
        }

        /// <summary>
        /// Pointer for the beginning of a block of memory, number of items in it, which allocator it belongs to, and which block this is.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Range : IDisposable
        {
            /// <summary>
            ///
            /// </summary>
            public IntPtr Pointer; //  0

            /// <summary>
            ///
            /// </summary>
            public int Items; //  8

            /// <summary>
            ///
            /// </summary>
            public SmallAllocatorHandle Allocator; // 12

            /// <summary>
            ///
            /// </summary>
            public BlockHandle Block; // 14

            /// <summary>
            ///
            /// </summary>
            public void Dispose()
            {
                Block block = new Block { Range = this };
                block.Dispose();
                this = block.Range;
            }
        }

        /// <summary>
        /// A block of memory with a Range and metadata for size in bytes of each item in the block, number of allocated items, and alignment.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Block : IDisposable
        {
            /// <summary>
            ///
            /// </summary>
            public Range Range;

            /// <summary>
            /// Number of bytes in each item requested.
            /// </summary>
            public int BytesPerItem;

            /// <summary>
            /// How many items were actually allocated.
            /// </summary>
            public int AllocatedItems;

            /// <summary>
            /// (1 &lt;&lt; this) is the byte alignment.
            /// </summary>
            public byte Log2Alignment;

            /// <summary>
            ///
            /// </summary>
            public byte Padding0;

            /// <summary>
            ///
            /// </summary>
            public ushort Padding1;

            /// <summary>
            ///
            /// </summary>
            public uint Padding2;

            /// <summary>
            ///
            /// </summary>
            public long Bytes => BytesPerItem * Range.Items;

            /// <summary>
            ///
            /// </summary>
            public int Alignment
            {
                get => 1 << Log2Alignment;
                set => Log2Alignment = (byte)(32 - math.lzcnt(math.max(1, value) - 1));
            }

            /// <summary>
            ///
            /// </summary>
            public void Dispose()
            {
                TryFree();
            }

            /// <summary>
            ///
            /// </summary>
            /// <returns></returns>
            public int TryAllocate()
            {
                Range.Pointer = IntPtr.Zero;
                return Try(ref this);
            }

            /// <summary>
            ///
            /// </summary>
            /// <returns></returns>
            public int TryFree()
            {
                Range.Items = 0;
                return Try(ref this);
            }

            /// <summary>
            ///
            /// </summary>
            public void Allocate()
            {
                var error = TryAllocate();
                CheckFailedToAllocate(error);
            }

            /// <summary>
            ///
            /// </summary>
            public void Free()
            {
                var error = TryFree();
                CheckFailedToFree(error);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckFailedToAllocate(int error)
            {
                if (error != 0)
                    throw new ArgumentException($"Error {error}: Failed to Allocate {this}");
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckFailedToFree(int error)
            {
                if (error != 0)
                    throw new ArgumentException($"Error {error}: Failed to Free {this}");
            }
        }

        /// <summary>
        /// An allocator with a tryable allocate/free/realloc function pointer.
        /// </summary>
        public interface IAllocator
        {
            /// <summary>
            ///
            /// </summary>
            TryFunction Function { get; }

            /// <summary>
            ///
            /// </summary>
            /// <param name="block"></param>
            /// <returns></returns>
            int Try(ref Block block);

            /// <summary>
            /// Upper limit on how many bytes this allocator is allowed to allocate.
            /// </summary>
            long BudgetInBytes { get; }

            /// <summary>
            /// Number of currently allocated bytes for this allocator.
            /// </summary>
            long AllocatedBytes { get; }
        }

        static Allocator LegacyOf(AllocatorHandle handle)
        {
            if (handle.Value >= FirstUserIndex)
                return Allocator.Persistent;
            return (Allocator) handle.Value;
        }

        static unsafe int TryLegacy(ref Block block)
        {
            if (block.Range.Pointer == IntPtr.Zero) // Allocate
            {
                block.Range.Pointer = (IntPtr)Memory.Unmanaged.Allocate(block.Bytes, block.Alignment, LegacyOf(block.Range.Allocator));
                block.AllocatedItems = block.Range.Items;
                return (block.Range.Pointer == IntPtr.Zero) ? -1 : 0;
            }
            if (block.Bytes == 0) // Free
            {
                Memory.Unmanaged.Free((void*) block.Range.Pointer, LegacyOf(block.Range.Allocator));
                block.Range.Pointer = IntPtr.Zero;
                block.AllocatedItems = 0;
                return 0;
            }
            // Reallocate (keep existing pointer and change size if possible. otherwise, allocate new thing and copy)
            return -1;
        }

        /// <summary>
        /// Looks up an allocator's allocate, free, or realloc function pointer from a table and invokes the function.
        /// </summary>
        /// <param name="block">Block to allocate memory for.</param>
        /// <returns>Error code of invoked function.</returns>
        public static unsafe int Try(ref Block block)
        {
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
            if (block.Range.Allocator.Value <= AudioKernel.Value)
#endif
                return TryLegacy(ref block);
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
            TableEntry tableEntry = default;
            fixed (TableEntry65536* tableEntry65536 = &StaticFunctionTable.Ref.Data)
                tableEntry = ((TableEntry*)tableEntry65536)[block.Range.Allocator.Value];
            var function = new FunctionPointer<TryFunction>(tableEntry.function);
            // this is really bad in non-Burst C#, it generates garbage each time we call Invoke
            return function.Invoke(tableEntry.state, ref block);
#endif
        }

        #endregion
        #region Allocators

        /// <summary>
        /// Stack allocator with no backing storage.
        /// </summary>
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
        [BurstCompile(CompileSynchronously = true)]
#endif
        internal struct StackAllocator : IAllocator, IDisposable
        {
            internal Block m_storage;
            internal long m_top;

            internal long budgetInBytes;
            public long BudgetInBytes => budgetInBytes;

            internal long allocatedBytes;
            public long AllocatedBytes => allocatedBytes;

            public unsafe int Try(ref Block block)
            {
                if (block.Range.Pointer == IntPtr.Zero) // Allocate
                {
                    if (m_top + block.Bytes > m_storage.Bytes)
                    {
                        return -1;
                    }

                    block.Range.Pointer = (IntPtr)((byte*)m_storage.Range.Pointer + m_top);
                    block.AllocatedItems = block.Range.Items;
                    allocatedBytes += block.Bytes;
                    m_top += block.Bytes;
                    return 0;
                }

                if (block.Bytes == 0) // Free
                {
                    if ((byte*)block.Range.Pointer - (byte*)m_storage.Range.Pointer == (long)(m_top - block.Bytes))
                    {
                        m_top -= block.Bytes;
                        var blockSizeInBytes = block.AllocatedItems * block.BytesPerItem;
                        allocatedBytes -= blockSizeInBytes;
                        block.Range.Pointer = IntPtr.Zero;
                        block.AllocatedItems = 0;
                        return 0;
                    }

                    return -1;
                }

                // Reallocate (keep existing pointer and change size if possible. otherwise, allocate new thing and copy)
                return -1;
            }

#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
            [BurstCompile(CompileSynchronously = true)]
#endif
            public static unsafe int Try(IntPtr allocatorState, ref Block block)
            {
                return ((StackAllocator*)allocatorState)->Try(ref block);
            }

            public TryFunction Function => Try;

            public void Dispose()
            {
            }
        }

        /// <summary>
        /// Slab allocator with no backing storage.
        /// </summary>
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
        [BurstCompile(CompileSynchronously = true)]
#endif
        internal struct SlabAllocator : IAllocator, IDisposable
        {
            internal Block Storage;
            internal int Log2SlabSizeInBytes;
            internal FixedListInt4096 Occupied;
            internal long budgetInBytes;
            internal long allocatedBytes;

            public long BudgetInBytes => budgetInBytes;

            public long AllocatedBytes => allocatedBytes;

            internal int SlabSizeInBytes
            {
                get => 1 << Log2SlabSizeInBytes;
                set => Log2SlabSizeInBytes = (byte)(32 - math.lzcnt(math.max(1, value) - 1));
            }

            internal int Slabs => (int)(Storage.Bytes >> Log2SlabSizeInBytes);

            internal SlabAllocator(Block storage, int slabSizeInBytes, long budget)
            {
                Assert.IsTrue((slabSizeInBytes & (slabSizeInBytes - 1)) == 0);
                Storage = storage;
                Log2SlabSizeInBytes = 0;
                Occupied = default;
                budgetInBytes = budget;
                allocatedBytes = 0;
                SlabSizeInBytes = slabSizeInBytes;
                Occupied.Length = (Slabs + 31) / 32;
            }

            public int Try(ref Block block)
            {
                if (block.Range.Pointer == IntPtr.Zero) // Allocate
                {
                    if (block.Bytes + allocatedBytes > budgetInBytes)
                        return -2; //over allocator budget
                    if (block.Bytes > SlabSizeInBytes)
                        return -1;
                    for (var wordIndex = 0; wordIndex < Occupied.Length; ++wordIndex)
                    {
                        var word = Occupied[wordIndex];
                        if (word == -1)
                            continue;
                        for (var bitIndex = 0; bitIndex < 32; ++bitIndex)
                            if ((word & (1 << bitIndex)) == 0)
                            {
                                Occupied[wordIndex] |= 1 << bitIndex;
                                block.Range.Pointer = Storage.Range.Pointer +
                                    (int)(SlabSizeInBytes * (wordIndex * 32U + bitIndex));
                                block.AllocatedItems = SlabSizeInBytes / block.BytesPerItem;
                                allocatedBytes += block.Bytes;
                                return 0;
                            }
                    }

                    return -1;
                }

                if (block.Bytes == 0) // Free
                {
                    var slabIndex = ((ulong)block.Range.Pointer - (ulong)Storage.Range.Pointer) >>
                        Log2SlabSizeInBytes;
                    int wordIndex = (int)(slabIndex >> 5);
                    int bitIndex = (int)(slabIndex & 31);
                    Occupied[wordIndex] &= ~(1 << bitIndex);
                    block.Range.Pointer = IntPtr.Zero;
                    var blockSizeInBytes = block.AllocatedItems * block.BytesPerItem;
                    allocatedBytes -= blockSizeInBytes;
                    block.AllocatedItems = 0;
                    return 0;
                }

                // Reallocate (keep existing pointer and change size if possible. otherwise, allocate new thing and copy)
                return -1;
            }

#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
            [BurstCompile(CompileSynchronously = true)]
#endif
            public static unsafe int Try(IntPtr allocatorState, ref Block block)
            {
                return ((SlabAllocator*)allocatorState)->Try(ref block);
            }

            public TryFunction Function => Try;

            public void Dispose()
            {
            }
        }
        #endregion
        #region AllocatorManager state and state functions
        /// <summary>
        /// Mapping between a Block, AllocatorHandle, and an IAllocator.
        /// </summary>
        /// <typeparam name="T">Type of allocator to install functions for.</typeparam>
        public struct AllocatorInstallation<T> : IDisposable
            where T : unmanaged, IAllocator, IDisposable
        {
            /// <summary>
            ///
            /// </summary>
            public Block MBlock;

            /// <summary>
            ///
            /// </summary>
            public AllocatorHandle m_handle;

            unsafe T* t => (T*)MBlock.Range.Pointer;

            /// <summary>
            ///
            /// </summary>
            public ref T Allocator
            {
                get
                {
                    unsafe
                    {
                        return ref UnsafeUtility.AsRef<T>(t);
                    }
                }
            }

            /// <summary>
            /// Creates a Block for an allocator, associates that allocator with an AllocatorHandle, then installs the allocator's function into the function table.
            /// </summary>
            /// <param name="Handle">Index into function table at which to install this allocator's function pointer.</param>
            public AllocatorInstallation(AllocatorHandle Handle)
            {
                // Allocate an allocator of type T using UnsafeUtility.Malloc with Allocator.Persistent.
                MBlock = Persistent.Allocate<T>(1);
                m_handle = Handle;
                unsafe
                {
                    UnsafeUtility.MemSet(t, 0, UnsafeUtility.SizeOf<T>());
                }

                unsafe
                {
                    Install(m_handle, (IntPtr)t, t->Function);
                }
            }

            /// <summary>
            ///
            /// </summary>
            public void Dispose()
            {
                Install(m_handle, IntPtr.Zero, null);
                unsafe
                {
                    t->Dispose();
                }

                MBlock.Dispose();
            }
        }

        struct TableEntry
        {
            internal IntPtr function;
            internal IntPtr state;
        }

        struct TableEntry16
        {
            internal TableEntry f0;
            internal TableEntry f1;
            internal TableEntry f2;
            internal TableEntry f3;
            internal TableEntry f4;
            internal TableEntry f5;
            internal TableEntry f6;
            internal TableEntry f7;
            internal TableEntry f8;
            internal TableEntry f9;
            internal TableEntry f10;
            internal TableEntry f11;
            internal TableEntry f12;
            internal TableEntry f13;
            internal TableEntry f14;
            internal TableEntry f15;
        }

        struct TableEntry256
        {
            internal TableEntry16 f0;
            internal TableEntry16 f1;
            internal TableEntry16 f2;
            internal TableEntry16 f3;
            internal TableEntry16 f4;
            internal TableEntry16 f5;
            internal TableEntry16 f6;
            internal TableEntry16 f7;
            internal TableEntry16 f8;
            internal TableEntry16 f9;
            internal TableEntry16 f10;
            internal TableEntry16 f11;
            internal TableEntry16 f12;
            internal TableEntry16 f13;
            internal TableEntry16 f14;
            internal TableEntry16 f15;
        }

        struct TableEntry4096
        {
            internal TableEntry256 f0;
            internal TableEntry256 f1;
            internal TableEntry256 f2;
            internal TableEntry256 f3;
            internal TableEntry256 f4;
            internal TableEntry256 f5;
            internal TableEntry256 f6;
            internal TableEntry256 f7;
            internal TableEntry256 f8;
            internal TableEntry256 f9;
            internal TableEntry256 f10;
            internal TableEntry256 f11;
            internal TableEntry256 f12;
            internal TableEntry256 f13;
            internal TableEntry256 f14;
            internal TableEntry256 f15;
        }

        struct TableEntry65536
        {
            internal TableEntry4096 f0;
            internal TableEntry4096 f1;
            internal TableEntry4096 f2;
            internal TableEntry4096 f3;
            internal TableEntry4096 f4;
            internal TableEntry4096 f5;
            internal TableEntry4096 f6;
            internal TableEntry4096 f7;
            internal TableEntry4096 f8;
            internal TableEntry4096 f9;
            internal TableEntry4096 f10;
            internal TableEntry4096 f11;
            internal TableEntry4096 f12;
            internal TableEntry4096 f13;
            internal TableEntry4096 f14;
            internal TableEntry4096 f15;
        }

        /// <summary>
        /// SharedStatic that holds array of allocation function pointers for each allocator.
        /// </summary>
        sealed class StaticFunctionTable
        {
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
            public static readonly SharedStatic<TableEntry65536> Ref =
                SharedStatic<TableEntry65536>.GetOrCreate<StaticFunctionTable>();
#endif
        }

        /// <summary>
        /// Initializes SharedStatic allocator function table and allocator table, and installs default allocators.
        /// </summary>
        public static void Initialize()
        {
        }

        /// <summary>
        /// Creates and saves allocators' function pointers into function table.
        /// </summary>
        /// <param name="handle">AllocatorHandle to allocator to install function for.</param>
        /// <param name="allocatorState">IntPtr to allocator's custom state.</param>
        /// <param name="function">Function pointer to create or save in function table.</param>
        public static unsafe void Install(AllocatorHandle handle, IntPtr allocatorState, TryFunction function)
        {
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
            var functionPointer = (function == null)
                ? new FunctionPointer<TryFunction>(IntPtr.Zero)
                : BurstCompiler.CompileFunctionPointer(function);
            var tableEntry = new TableEntry { state = allocatorState, function = functionPointer.Value };
            fixed (TableEntry65536* tableEntry65536 = &StaticFunctionTable.Ref.Data)
                ((TableEntry*)tableEntry65536)[handle.Value] = tableEntry;
#endif
        }

        /// <summary>
        ///
        /// </summary>
        public static void Shutdown()
        {
        }

        #endregion
        /// <summary>
        /// User-defined allocator index.
        /// </summary>
        public const ushort FirstUserIndex = 32;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckFailedToAllocate(int error)
        {
            if (error != 0)
                throw new ArgumentException("failed to allocate");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckFailedToFree(int error)
        {
            if (error != 0)
                throw new ArgumentException("failed to free");
        }
    }
}

#pragma warning restore 0649
