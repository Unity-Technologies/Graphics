using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// An unmanaged, untyped, resizable list, without any thread safety check features.
    /// </summary>
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}, IsEmpty = {IsEmpty}")]
    [StructLayout(LayoutKind.Sequential)]
    [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
    public unsafe struct UnsafeList
        : INativeDisposable
    {
        /// <summary>
        /// </summary>
        [NativeDisableUnsafePtrRestriction]
        public void* Ptr;

        /// <summary>
        /// </summary>
        public int Length;

        /// <summary>
        /// </summary>
        public int Capacity;

        /// <summary>
        /// </summary>
        public AllocatorManager.AllocatorHandle Allocator;

        /// <summary>
        /// Constructs a new container with type of memory allocation.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <remarks>The list initially has a capacity of one. To avoid reallocating memory for the list, specify
        /// sufficient capacity up front.</remarks>
        public UnsafeList(Allocator allocator)
        {
            Ptr = null;
            Length = 0;
            Capacity = 0;
            Allocator = allocator;
        }

        /// <summary>
        /// Constructs container as view into memory.
        /// </summary>
        /// <param name="ptr">Pointer to data.</param>
        /// <param name="length">Lenght of data in bytes.</param>
        public UnsafeList(void* ptr, int length)
        {
            Ptr = ptr;
            Length = length;
            Capacity = length;
            Allocator = Collections.Allocator.None;
        }

        /// <summary>
        /// Constructs a new container with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="sizeOf">Size of element.</param>
        /// <param name="alignOf">Alignment of element.</param>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        public UnsafeList(int sizeOf, int alignOf, int initialCapacity, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            Allocator = allocator;
            Ptr = null;
            Length = 0;
            Capacity = 0;

            if (initialCapacity != 0)
            {
                SetCapacity(sizeOf, alignOf, initialCapacity);
            }

            if (options == NativeArrayOptions.ClearMemory
                && Ptr != null)
            {
                UnsafeUtility.MemClear(Ptr, Capacity * sizeOf);
            }
        }

        /// <summary>
        /// Constructs a new container with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="sizeOf">Size of element.</param>
        /// <param name="alignOf">Alignment of element.</param>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        public UnsafeList(int sizeOf, int alignOf, int initialCapacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            Allocator = allocator;
            Ptr = null;
            Length = 0;
            Capacity = 0;

            if (initialCapacity != 0)
            {
                SetCapacity(sizeOf, alignOf, initialCapacity);
            }

            if (options == NativeArrayOptions.ClearMemory
                && Ptr != null)
            {
                UnsafeUtility.MemClear(Ptr, Capacity * sizeOf);
            }
        }

        /// <summary>
        /// Creates a new container with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="sizeOf">Size of element.</param>
        /// <param name="alignOf">Alignment of element.</param>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        /// <returns>New initialized container.</returns>
        public static UnsafeList* Create(int sizeOf, int alignOf, int initialCapacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            var handle = new AllocatorManager.AllocatorHandle { Value = (int)allocator };
            UnsafeList* listData = AllocatorManager.Allocate<UnsafeList>(handle);
            UnsafeUtility.MemClear(listData, UnsafeUtility.SizeOf<UnsafeList>());

            listData->Allocator = allocator;

            if (initialCapacity != 0)
            {
                listData->SetCapacity(sizeOf, alignOf, initialCapacity);
            }

            if (options == NativeArrayOptions.ClearMemory
                && listData->Ptr != null)
            {
                UnsafeUtility.MemClear(listData->Ptr, listData->Capacity * sizeOf);
            }

            return listData;
        }

        /// <summary>
        /// Destroys container.
        /// </summary>
        /// <param name="listData">Container to destroy.</param>
        public static void Destroy(UnsafeList* listData)
        {
            CheckNull(listData);
            var allocator = listData->Allocator;
            listData->Dispose();
            AllocatorManager.Free(allocator, listData);
        }

        /// <summary>
        /// Reports whether container is empty.
        /// </summary>
        /// <value>True if this container empty.</value>
        public bool IsEmpty => !IsCreated || Length == 0;

        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// </summary>
        /// <value>True if this container object's internal storage has been allocated.</value>
        /// <remarks>
        /// Note that the container storage is not created if you use the default constructor. You must specify
        /// at least an allocation type to construct a usable container.
        ///
        /// *Warning:* the `IsCreated` property can't be used to determine whether a copy of a container is still valid.
        /// If you dispose any copy of the container, the container storage is deallocated. However, the properties of
        /// the other copies of the container (including the original) are not updated. As a result the `IsCreated` property
        /// of the copies still return `true` even though the container storage has been deallocated.
        /// </remarks>
        public bool IsCreated => Ptr != null;

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
            if (CollectionHelper.ShouldDeallocate(Allocator))
            {
                AllocatorManager.Free(Allocator, Ptr);
                Allocator = AllocatorManager.Invalid;
            }

            Ptr = null;
            Length = 0;
            Capacity = 0;
        }

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
        [BurstCompatible(RequiredUnityDefine = "UNITY_2020_2_OR_NEWER") /* Due to job scheduling on 2020.1 using statics */]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (CollectionHelper.ShouldDeallocate(Allocator))
            {
                var jobHandle = new UnsafeDisposeJob { Ptr = Ptr, Allocator = (Allocator)Allocator.Value }.Schedule(inputDeps);

                Ptr = null;
                Allocator = AllocatorManager.Invalid;

                return jobHandle;
            }

            Ptr = null;

            return inputDeps;
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        /// <remarks>The container capacity remains unchanged.</remarks>
        public void Clear()
        {
            Length = 0;
        }

        /// <summary>
        /// Changes the list length, resizing if necessary.
        /// </summary>
        /// <param name="sizeOf">Size of element.</param>
        /// <param name="alignOf">Alignment of element.</param>
        /// <param name="length">The new length of the list.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        public void Resize(int sizeOf, int alignOf, int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            var oldLength = Length;

            if (length > Capacity)
            {
                SetCapacity(sizeOf, alignOf, length);
            }

            Length = length;

            if (options == NativeArrayOptions.ClearMemory
                && oldLength < length)
            {
                var num = length - oldLength;
                byte* ptr = (byte*)Ptr;
                UnsafeUtility.MemClear(ptr + oldLength * sizeOf, num * sizeOf);
            }
        }

        /// <summary>
        /// Changes the list length, resizing if necessary.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="length">The new length of the list.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public void Resize<T>(int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) where T : struct
        {
            Resize(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), length, options);
        }

        void Realloc(int sizeOf, int alignOf, int capacity)
        {
            CheckAllocator(Allocator);
            void* newPointer = null;

            if (capacity > 0)
            {
                newPointer = AllocatorManager.Allocate(Allocator, sizeOf, alignOf, capacity);

                if (Capacity > 0)
                {
                    var itemsToCopy = math.min(capacity, Capacity);
                    var bytesToCopy = itemsToCopy * sizeOf;
                    UnsafeUtility.MemCpy(newPointer, Ptr, bytesToCopy);
                }
            }

            AllocatorManager.Free(Allocator, Ptr);

            Ptr = newPointer;
            Capacity = capacity;
            Length = math.min(Length, capacity);
        }

        void SetCapacity(int sizeOf, int alignOf, int capacity)
        {
            var newCapacity = math.max(capacity, 64 / sizeOf);
            newCapacity = math.ceilpow2(newCapacity);

            if (newCapacity == Capacity)
            {
                return;
            }

            Realloc(sizeOf, alignOf, newCapacity);
        }

        /// <summary>
        /// Set the number of items that can fit in the container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="capacity">The number of items that the container can hold before it resizes its internal storage.</param>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public void SetCapacity<T>(int capacity) where T : struct
        {
            SetCapacity(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), capacity);
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public void TrimExcess<T>() where T : struct
        {
            if (Capacity != Length)
            {
                Realloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), Length);
            }
        }

        /// <summary>
        /// Searches for the specified element in list.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="value"></param>
        /// <returns>The zero-based index of the first occurrence element if found, otherwise returns -1.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public int IndexOf<T>(T value) where T : struct, IEquatable<T>
        {
            return NativeArrayExtensions.IndexOf<T, T>(Ptr, Length, value);
        }

        /// <summary>
        /// Determines whether an element is in the list.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="value"></param>
        /// <returns>True, if element is found.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public bool Contains<T>(T value) where T : struct, IEquatable<T>
        {
            return IndexOf(value) != -1;
        }

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="value">The value to be added at the end of the list.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public void AddNoResize<T>(T value) where T : struct
        {
            CheckNoResizeHasEnoughCapacity(1);
            UnsafeUtility.WriteArrayElement(Ptr, Length, value);
            Length += 1;
        }

        void AddRangeNoResize(int sizeOf, void* ptr, int length)
        {
            CheckNoResizeHasEnoughCapacity(length);
            void* dst = (byte*)Ptr + Length * sizeOf;
            UnsafeUtility.MemCpy(dst, ptr, length * sizeOf);
            Length += length;
        }

        /// <summary>
        /// Adds elements from a buffer to this list.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="ptr">A pointer to the buffer.</param>
        /// <param name="length">The number of elements to add to the list.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public void AddRangeNoResize<T>(void* ptr, int length) where T : struct
        {
            AddRangeNoResize(UnsafeUtility.SizeOf<T>(), ptr, length);
        }

        /// <summary>
        /// Adds elements from a list to this list.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="list">Other container to copy elements from.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public void AddRangeNoResize<T>(UnsafeList list) where T : struct
        {
            AddRangeNoResize(UnsafeUtility.SizeOf<T>(), list.Ptr, CollectionHelper.AssumePositive(list.Length));
        }

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="value">The value to be added at the end of the list.</param>
        /// <remarks>
        /// If the list has reached its current capacity, it copies the original, internal array to
        /// a new, larger array, and then deallocates the original.
        /// </remarks>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public void Add<T>(T value) where T : struct
        {
            var idx = Length;

            if (Length + 1 > Capacity)
            {
                Resize<T>(idx + 1);
            }
            else
            {
                Length += 1;
            }

            UnsafeUtility.WriteArrayElement(Ptr, idx, value);
        }

        void AddRange(int sizeOf, int alignOf, void* ptr, int length)
        {
            var idx = Length;

            if (Length + length > Capacity)
            {
                Resize(sizeOf, alignOf, Length + length);
            }
            else
            {
                Length += length;
            }

            void* dst = (byte*)Ptr + idx * sizeOf;
            UnsafeUtility.MemCpy(dst, ptr, length * sizeOf);
        }

        /// <summary>
        /// Adds elements from a buffer to this list.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="ptr">A pointer to the buffer.</param>
        /// <param name="length">The number of elements to add to the list.</param>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public void AddRange<T>(void* ptr, int length) where T : struct
        {
            AddRange(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), ptr, length);
        }

        /// <summary>
        /// Adds elements from a list to this list.
        /// </summary>
        /// <remarks>
        /// If the list has reached its current capacity, it copies the original, internal array to
        /// a new, larger array, and then deallocates the original.
        /// </remarks>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="list">Other container to copy elements from.</param>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public void AddRange<T>(UnsafeList list) where T : struct
        {
            AddRange(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), list.Ptr, list.Length);
        }

        void InsertRangeWithBeginEnd(int sizeOf, int alignOf, int begin, int end)
        {
            CheckBeginEnd(begin, end);

            int items = end - begin;
            if (items < 1)
            {
                return;
            }

            var oldLength = Length;

            if (Length + items > Capacity)
            {
                Resize(sizeOf, alignOf, Length + items);
            }
            else
            {
                Length += items;
            }

            var itemsToCopy = oldLength - begin;

            if (itemsToCopy < 1)
            {
                return;
            }

            var bytesToCopy = itemsToCopy * sizeOf;
            unsafe
            {
                byte* ptr  = (byte*)Ptr;
                byte* dest = ptr + end * sizeOf;
                byte* src  = ptr + begin * sizeOf;
                UnsafeUtility.MemMove(dest, src, bytesToCopy);
            }
        }

        /// <summary>
        /// Inserts a number of items into a container at a specified zero-based index.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="begin">The zero-based index at which the new elements should be inserted.</param>
        /// <param name="end">The zero-based index just after where the elements should be removed.</param>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public void InsertRangeWithBeginEnd<T>(int begin, int end) where T : struct
        {
            InsertRangeWithBeginEnd(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), begin, end);
        }

        void RemoveRangeSwapBackWithBeginEnd(int sizeOf, int begin, int end)
        {
            CheckBeginEnd(begin, end);

            int itemsToRemove = end - begin;
            if (itemsToRemove > 0)
            {
                int copyFrom = math.max(Length - itemsToRemove, end);
                void* dst = (byte*)Ptr + begin * sizeOf;
                void* src = (byte*)Ptr + copyFrom * sizeOf;
                UnsafeUtility.MemCpy(dst, src, (Length - copyFrom) * sizeOf);
                Length -= itemsToRemove;
            }
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index with the last item in the list. The list
        /// is shortened by one.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="index">The index of the item to delete.</param>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public void RemoveAtSwapBack<T>(int index) where T : struct
        {
            RemoveRangeSwapBackWithBeginEnd<T>(index, index + 1);
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index range with the items from the end the list. The list
        /// is shortened by number of elements in range.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="begin">The first index of the item to remove.</param>
        /// <param name="end">The index past-the-last item to remove.</param>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public void RemoveRangeSwapBackWithBeginEnd<T>(int begin, int end) where T : struct
        {
            RemoveRangeSwapBackWithBeginEnd(UnsafeUtility.SizeOf<T>(), begin, end);
        }

        void RemoveRangeWithBeginEnd(int sizeOf, int begin, int end)
        {
            CheckBeginEnd(begin, end);

            int itemsToRemove = end - begin;
            if (itemsToRemove > 0)
            {
                int copyFrom = math.min(begin + itemsToRemove, Length);
                void* dst = (byte*)Ptr + begin * sizeOf;
                void* src = (byte*)Ptr + copyFrom * sizeOf;
                UnsafeUtility.MemCpy(dst, src, (Length - copyFrom) * sizeOf);
                Length -= itemsToRemove;
            }
        }

        /// <summary>
        /// Truncates the list by removing the item at the specified index, and shifting all remaining items to replace removed item. The list
        /// is shortened by one.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="index">The index of the item to delete.</param>
        /// <remarks>
        /// This method of removing item is useful only in case when list is ordered and user wants to preserve order
        /// in list after removal In majority of cases is not important and user should use more performant `RemoveAtSwapBack`.
        /// </remarks>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public void RemoveAt<T>(int index) where T : struct
        {
            RemoveRangeWithBeginEnd<T>(index, index + 1);
        }

        /// <summary>
        /// Truncates the list by removing the items at the specified index range, and shifting all remaining items to replace removed items. The list
        /// is shortened by number of elements in range.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="begin">The first index of the item to remove.</param>
        /// <param name="end">The index past-the-last item to remove.</param>
        /// <remarks>
        /// This method of removing item(s) is useful only in case when list is ordered and user wants to preserve order
        /// in list after removal In majority of cases is not important and user should use more performant `RemoveRangeSwapBackWithBeginEnd`.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public void RemoveRangeWithBeginEnd<T>(int begin, int end) where T : struct
        {
            RemoveRangeWithBeginEnd(UnsafeUtility.SizeOf<T>(), begin, end);
        }

        /// <summary>
        /// Returns parallel reader instance.
        /// </summary>
        /// <returns>Parallel reader instance.</returns>
        public ParallelReader AsParallelReader()
        {
            return new ParallelReader(Ptr, Length);
        }

        /// <summary>
        /// Implements parallel reader. Use AsParallelReader to obtain it from container.
        /// </summary>
        public unsafe struct ParallelReader
        {
            /// <summary>
            ///
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public readonly void* Ptr;

            /// <summary>
            ///
            /// </summary>
            public readonly int Length;

            internal ParallelReader(void* ptr, int length)
            {
                Ptr = ptr;
                Length = length;
            }

            /// <summary>
            ///
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="value"></param>
            /// <returns></returns>
            [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
            public int IndexOf<T>(T value) where T : struct, IEquatable<T>
            {
                return NativeArrayExtensions.IndexOf<T, T>(Ptr, Length, value);
            }

            /// <summary>
            ///
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="value"></param>
            /// <returns></returns>
            [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
            public bool Contains<T>(T value) where T : struct, IEquatable<T>
            {
                return IndexOf(value) != -1;
            }
        }

        /// <summary>
        /// Returns parallel writer instance.
        /// </summary>
        /// <returns>Parallel writer instance.</returns>
        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter(Ptr, (UnsafeList*)UnsafeUtility.AddressOf(ref this));
        }

        /// <summary>
        ///
        /// </summary>
        public unsafe struct ParallelWriter
        {
            /// <summary>
            ///
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public readonly void* Ptr;

            /// <summary>
            ///
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public UnsafeList* ListData;

            internal unsafe ParallelWriter(void* ptr, UnsafeList* listData)
            {
                Ptr = ptr;
                ListData = listData;
            }

            /// <summary>
            /// Adds an element to the list.
            /// </summary>
            /// <typeparam name="T">Source type of elements</typeparam>
            /// <param name="value">The value to be added at the end of the list.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
            public void AddNoResize<T>(T value) where T : struct
            {
                var idx = Interlocked.Increment(ref ListData->Length) - 1;
                ListData->CheckNoResizeHasEnoughCapacity(idx, 1);
                UnsafeUtility.WriteArrayElement(Ptr, idx, value);
            }

            void AddRangeNoResize(int sizeOf, int alignOf, void* ptr, int length)
            {
                var idx = Interlocked.Add(ref ListData->Length, length) - length;
                ListData->CheckNoResizeHasEnoughCapacity(idx, length);
                void* dst = (byte*)Ptr + idx * sizeOf;
                UnsafeUtility.MemCpy(dst, ptr, length * sizeOf);
            }

            /// <summary>
            /// Adds elements from a buffer to this list.
            /// </summary>
            /// <typeparam name="T">Source type of elements</typeparam>
            /// <param name="ptr">A pointer to the buffer.</param>
            /// <param name="length">The number of elements to add to the list.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
            public void AddRangeNoResize<T>(void* ptr, int length) where T : struct
            {
                AddRangeNoResize(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), ptr, length);
            }

            /// <summary>
            /// Adds elements from a list to this list.
            /// </summary>
            /// <typeparam name="T">Source type of elements</typeparam>
            /// <param name="list">Other container to copy elements from.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
            public void AddRangeNoResize<T>(UnsafeList list) where T : struct
            {
                AddRangeNoResize(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), list.Ptr, list.Length);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckNull(void* listData)
        {
            if (listData == null)
            {
                throw new Exception("UnsafeList has yet to be created or has been destroyed!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckAllocator(Allocator a)
        {
            if (!CollectionHelper.ShouldDeallocate(a))
            {
                throw new Exception("UnsafeList is not initialized, it must be initialized with allocator before use.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckAllocator(AllocatorManager.AllocatorHandle a)
        {
            if (!CollectionHelper.ShouldDeallocate(a))
            {
                throw new Exception("UnsafeList is not initialized, it must be initialized with allocator before use.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckBeginEnd(int begin, int end)
        {
            if (begin > end)
            {
                throw new ArgumentException($"Value for begin {begin} index must less or equal to end {end}.");
            }

            if (begin < 0)
            {
                throw new ArgumentOutOfRangeException($"Value for begin {begin} must be positive.");
            }

            if (begin > Length)
            {
                throw new ArgumentOutOfRangeException($"Value for begin {begin} is out of bounds.");
            }

            if (end > Length)
            {
                throw new ArgumentOutOfRangeException($"Value for end {end} is out of bounds.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckNoResizeHasEnoughCapacity(int length)
        {
            CheckNoResizeHasEnoughCapacity(length, Length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckNoResizeHasEnoughCapacity(int length, int index)
        {
            if (Capacity < index + length)
            {
                throw new Exception($"AddNoResize assumes that list capacity is sufficient (Capacity {Capacity}, Length {Length}), requested length {length}!");
            }
        }
    }

    [BurstCompile]
    internal unsafe struct UnsafeDisposeJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public void* Ptr;
        public Allocator Allocator;

        public void Execute()
        {
            var handle = new AllocatorManager.AllocatorHandle { Value = (int)Allocator };
            AllocatorManager.Free(handle, Ptr);
        }
    }

    /// <summary>
    /// An managed, resizable list, without any thread safety check features.
    /// </summary>
    /// <typeparam name="T">Source type of elements</typeparam>
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}, IsEmpty = {IsEmpty}")]
    [DebuggerTypeProxy(typeof(UnsafeListTDebugView<>))]
    [StructLayout(LayoutKind.Sequential)]
    [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
    public unsafe struct UnsafeList<T>
        : INativeDisposable
        , INativeList<T>
        , IEnumerable<T> // Used by collection initializers.
        where T : unmanaged
    {
        /// <summary>
        ///
        /// </summary>
        [NativeDisableUnsafePtrRestriction]
        public T* Ptr;

        /// <summary>
        ///
        /// </summary>
        public int length;

        /// <summary>
        ///
        /// </summary>
        public int capacity;

        /// <summary>
        ///
        /// </summary>
        public AllocatorManager.AllocatorHandle Allocator;

        /// <summary>
        ///
        /// </summary>
        public int Length
        {
            get { return length; }
            set { length = value; }
        }

        /// <summary>
        ///
        /// </summary>
        public int Capacity
        {
            get { return capacity; }
            set { capacity = value; }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get { return Ptr[index]; }
            set { Ptr[index] = value; }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ref T ElementAt(int index)
        {
            return ref Ptr[index];
        }

        /// <summary>
        /// Constructs list as view into memory.
        /// </summary>
        /// <param name="ptr">Pointer to data.</param>
        /// <param name="length">Lenght of data in bytes.</param>
        public UnsafeList(T* ptr, int length)
        {
            Ptr = ptr;
            this.length = length;
            capacity = 0;
            Allocator = AllocatorManager.None;
        }

        /// <summary>
        /// Constructs a new list using the specified type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        /// <remarks>The list initially has a capacity of one. To avoid reallocating memory for the list, specify
        /// sufficient capacity up front.</remarks>
        public UnsafeList(int initialCapacity, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            Ptr = null;
            length = 0;
            capacity = 0;
            Allocator = AllocatorManager.None;
            var sizeOf = UnsafeUtility.SizeOf<T>();
            var alignOf = UnsafeUtility.AlignOf<T>();
            this.ListData() = new UnsafeList(sizeOf, alignOf, initialCapacity, allocator, options);
        }

        /// <summary>
        /// Constructs a new list using the specified type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        /// <remarks>The list initially has a capacity of one. To avoid reallocating memory for the list, specify
        /// sufficient capacity up front.</remarks>
        public UnsafeList(int initialCapacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            Ptr = null;
            length = 0;
            capacity = 0;
            Allocator = AllocatorManager.None;
            var sizeOf = UnsafeUtility.SizeOf<T>();
            var alignOf = UnsafeUtility.AlignOf<T>();
            this.ListData() = new UnsafeList(sizeOf, alignOf, initialCapacity, allocator, options);
        }

        /// <summary>
        /// Reports whether container is empty.
        /// </summary>
        /// <value>True if this container empty.</value>
        public bool IsEmpty => !IsCreated || Length == 0;

        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// </summary>
        /// <value>True if this container object's internal storage has been allocated.</value>
        /// <remarks>
        /// Note that the container storage is not created if you use the default constructor. You must specify
        /// at least an allocation type to construct a usable container.
        ///
        /// *Warning:* the `IsCreated` property can't be used to determine whether a copy of a container is still valid.
        /// If you dispose any copy of the container, the container storage is deallocated. However, the properties of
        /// the other copies of the container (including the original) are not updated. As a result the `IsCreated` property
        /// of the copies still return `true` even though the container storage has been deallocated.
        /// </remarks>
        public bool IsCreated => Ptr != null;

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
            this.ListData().Dispose();
        }

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
        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) }, RequiredUnityDefine = "UNITY_2020_2_OR_NEWER") /* Due to job scheduling on 2020.1 using statics */]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            return this.ListData().Dispose(inputDeps);
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        /// <remarks>List Capacity remains unchanged.</remarks>
        public void Clear()
        {
            this.ListData().Clear();
        }

        /// <summary>
        /// Changes the list length, resizing if necessary.
        /// </summary>
        /// <param name="length">The new length of the list.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        public void Resize(int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            this.ListData().Resize<T>(length, options);
        }

        /// <summary>
        /// Set the number of items that can fit in the list.
        /// </summary>
        /// <param name="capacity">The number of items that the list can hold before it resizes its internal storage.</param>
        public void SetCapacity(int capacity)
        {
            this.ListData().SetCapacity<T>(capacity);
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the container.
        /// </summary>
        public void TrimExcess()
        {
            this.ListData().TrimExcess<T>();
        }

        /// <summary>
        /// Adds an element to the container.
        /// </summary>
        /// <param name="value">The value to be added at the end of the container.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddNoResize(T value)
        {
            this.ListData().AddNoResize(value);
        }

        /// <summary>
        /// Adds the elements to this container.
        /// </summary>
        /// <param name="ptr">A pointer to the buffer.</param>
        /// <param name="length">The number of elements to add to the container.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddRangeNoResize(void* ptr, int length)
        {
            this.ListData().AddRangeNoResize<T>(ptr, length);
        }

        /// <summary>
        /// Adds elements from a list to this container.
        /// </summary>
        /// <param name="list">Other container to copy elements from.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        public void AddRangeNoResize(UnsafeList<T> list)
        {
            this.ListData().AddRangeNoResize<T>(list.ListData());
        }

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <param name="value">The struct to be added at the end of the list.</param>
        public void Add(in T value)
        {
            this.ListData().Add(value);
        }

        /// <summary>
        /// Adds elements from a buffer to this list.
        /// </summary>
        /// <param name="ptr">A pointer to the buffer.</param>
        /// <param name="length">The number of elements to add to the list.</param>
        public void AddRange(void* ptr, int length)
        {
            this.ListData().AddRange<T>(ptr, length);
        }

        /// <summary>
        /// Adds the elements of a UnsafePtrList to this list.
        /// </summary>
        /// <param name="list">Other container to copy elements from.</param>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        public void AddRange(UnsafeList<T> list)
        {
            this.ListData().AddRange<T>(list.ListData());
        }

        /// <summary>
        /// Inserts a number of items into a container at a specified zero-based index.
        /// </summary>
        /// <param name="begin">The zero-based index at which the new elements should be inserted.</param>
        /// <param name="end">The zero-based index just after where the elements should be removed.</param>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        public void InsertRangeWithBeginEnd(int begin, int end)
        {
            this.ListData().InsertRangeWithBeginEnd<T>(begin, end);
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index with the last item in the list. The list
        /// is shortened by one.
        /// </summary>
        /// <param name="index">The index of the item to delete.</param>
        public void RemoveAtSwapBack(int index)
        {
            this.ListData().RemoveAtSwapBack<T>(index);
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index range with the items from the end the list. The list
        /// is shortened by number of elements in range.
        /// </summary>
        /// <param name="begin">The first index of the item to remove.</param>
        /// <param name="end">The index past-the-last item to remove.</param>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        public void RemoveRangeSwapBackWithBeginEnd(int begin, int end)
        {
            this.ListData().RemoveRangeSwapBackWithBeginEnd<T>(begin, end);
        }

        /// <summary>
        /// Truncates the list by removing the item at the specified index, and shifting all remaining items to replace removed item. The list
        /// is shortened by one.
        /// </summary>
        /// <param name="index">The index of the item to delete.</param>
        /// <remarks>
        /// This method of removing item is useful only in case when list is ordered and user wants to preserve order
        /// in list after removal In majority of cases is not important and user should use more performant `RemoveAtSwapBack`.
        /// </remarks>
        public void RemoveAt(int index)
        {
            this.ListData().RemoveAt<T>(index);
        }

        /// <summary>
        /// Truncates the list by removing the items at the specified index range, and shifting all remaining items to replace removed items. The list
        /// is shortened by number of elements in range.
        /// </summary>
        /// <param name="begin">The first intex of the item to remove.</param>
        /// <param name="end">The index past-the-last item to remove.</param>
        /// <remarks>
        /// This method of removing item(s) is useful only in case when list is ordered and user wants to preserve order
        /// in list after removal In majority of cases is not important and user should use more performant `RemoveRangeSwapBackWithBeginEnd`.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        public void RemoveRangeWithBeginEnd(int begin, int end)
        {
            this.ListData().RemoveRangeWithBeginEnd<T>(begin, end);
        }

        /// <summary>
        /// Returns parallel reader instance.
        /// </summary>
        /// <returns>Parallel reader instance.</returns>
        public ParallelReader AsParallelReader()
        {
            return new ParallelReader(Ptr, Length);
        }

        /// <summary>
        /// Implements parallel reader. Use AsParallelReader to obtain it from container.
        /// </summary>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public unsafe struct ParallelReader
        {
            /// <summary>
            ///
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public readonly T* Ptr;

            /// <summary>
            ///
            /// </summary>
            public readonly int Length;

            internal ParallelReader(T* ptr, int length)
            {
                Ptr = ptr;
                Length = length;
            }
        }

        /// <summary>
        /// Returns parallel writer instance.
        /// </summary>
        /// <returns>Parallel writer instance.</returns>
        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter((UnsafeList*)UnsafeUtility.AddressOf(ref this));
        }

        /// <summary>
        ///
        /// </summary>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public unsafe struct ParallelWriter
        {
            /// <summary>
            ///
            /// </summary>
            public UnsafeList.ParallelWriter Writer;

            internal unsafe ParallelWriter(UnsafeList* listData)
            {
                Writer = listData->AsParallelWriter();
            }

            /// <summary>
            /// Adds an element to the list.
            /// </summary>
            /// <param name="value">The value to be added at the end of the list.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            public void AddNoResize(T value)
            {
                Writer.AddNoResize(value);
            }

            /// <summary>
            /// </summary>
            /// <param name="ptr"></param>
            /// <param name="length"></param>
            public void AddRangeNoResize(void* ptr, int length)
            {
                Writer.AddRangeNoResize<T>(ptr, length);
            }

            /// <summary>
            /// </summary>
            /// <param name="list">Other container to copy elements from.</param>
            public void AddRangeNoResize(UnsafeList<T> list)
            {
                Writer.AddRangeNoResize<T>(list.ListData());
            }
        }

        /// <summary>
        /// Returns an IEnumerator interface for the container.
        /// </summary>
        /// <returns>An IEnumerator interface for the container.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator { m_Ptr = Ptr, m_Length = Length, m_Index = -1 };
        }

        /// <summary>
        /// This method is not implemented. It will throw NotImplementedException if it is used.
        /// </summary>
        /// <remarks>Use Enumerator GetEnumerator() instead.</remarks>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is not implemented. It will throw NotImplementedException if it is used.
        /// </summary>
        /// <remarks>Use Enumerator GetEnumerator() instead.</remarks>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implements iterator over the container.
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            internal T* m_Ptr;
            internal int m_Length;
            internal int m_Index;

            /// <summary>
            /// Disposes enumerator.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element of the container.
            /// </summary>
            /// <returns>Returns true if the iterator is successfully moved to the next element, otherwise it returns false.</returns>
            public bool MoveNext() => ++m_Index < m_Length;

            /// <summary>
            /// Resets the enumerator to the first element of the container.
            /// </summary>
            public void Reset() => m_Index = -1;

            /// <summary>
            /// Gets the element at the current position of the enumerator in the container.
            /// </summary>
            public T Current => m_Ptr[m_Index];

            object IEnumerator.Current => Current;
        }
    }

    /// <summary>
    /// UnsafeList extension methods.
    /// </summary>
    [BurstCompatible]
    public unsafe static class UnsafeListExtensions
    {
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        internal static ref UnsafeList ListData<T>(ref this UnsafeList<T> from) where T : unmanaged => ref UnsafeUtility.As<UnsafeList<T>, UnsafeList>(ref from);

        /// Type parameter has the same name as the type parameter from outer type
        /// <summary>
        /// Searches for the specified element in the container.
        /// </summary>
        /// <typeparam name="T">The type of values in the container.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="list">The container to locate value.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The zero-based index of the first occurrence element if found, otherwise returns -1.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static int IndexOf<T, U>(this UnsafeList<T> list, U value) where T : unmanaged, IEquatable<U>
        {
            return NativeArrayExtensions.IndexOf<T, U>(list.Ptr, list.Length, value);
        }

        /// <summary>
        /// Determines whether an element is in the container.
        /// </summary>
        /// <typeparam name="T">The type of values in the container.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="list">The container to locate value.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True, if element is found.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static bool Contains<T, U>(this UnsafeList<T> list, U value) where T : unmanaged, IEquatable<U>
        {
            return list.IndexOf(value) != -1;
        }

        /// Type parameter has the same name as the type parameter from outer type
        /// <summary>
        /// Searches for the specified element in the container.
        /// </summary>
        /// <typeparam name="T">The type of values in the container.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="list">The container to locate value.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The zero-based index of the first occurrence element if found, otherwise returns -1.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static int IndexOf<T, U>(this UnsafeList<T>.ParallelReader list, U value) where T : unmanaged, IEquatable<U>
        {
            return NativeArrayExtensions.IndexOf<T, U>(list.Ptr, list.Length, value);
        }

        /// <summary>
        /// Determines whether an element is in the container.
        /// </summary>
        /// <typeparam name="T">The type of values in the container.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="list">The container to locate value.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True, if element is found.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static bool Contains<T, U>(this UnsafeList<T>.ParallelReader list, U value) where T : unmanaged, IEquatable<U>
        {
            return list.IndexOf(value) != -1;
        }
    }

    internal sealed class UnsafeListTDebugView<T>
        where T : unmanaged
    {
        UnsafeList<T> Data;

        public UnsafeListTDebugView(UnsafeList<T> data)
        {
            Data = data;
        }

        public unsafe T[] Items
        {
            get
            {
                T[] result = new T[Data.Length];

                for (var i = 0; i < result.Length; ++i)
                {
                    result[i] = Data.Ptr[i];
                }

                return result;
            }
        }
    }

    /// <summary>
    /// An unmanaged, resizable list, without any thread safety check features.
    /// </summary>
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}, IsEmpty = {IsEmpty}")]
    [DebuggerTypeProxy(typeof(UnsafePtrListDebugView))]
    [BurstCompatible]
    public unsafe struct UnsafePtrList
        : INativeDisposable
        , INativeList<IntPtr>
        , IEnumerable<IntPtr> // Used by collection initializers.
    {
        /// <summary>
        ///
        /// </summary>
        [NativeDisableUnsafePtrRestriction]
        public readonly void** Ptr;

        /// <summary>
        ///
        /// </summary>
        public readonly int length;

        /// <summary>
        ///
        /// </summary>
        public readonly int capacity;

        /// <summary>
        ///
        /// </summary>
        public readonly AllocatorManager.AllocatorHandle Allocator;

        /// <summary>
        ///
        /// </summary>
        public int Length { get { return length; } set { } }

        /// <summary>
        ///
        /// </summary>
        public int Capacity { get { return capacity; } set { } }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IntPtr this[int index]
        {
            get { return new IntPtr(Ptr[index]); }
            set { Ptr[index] = (void*)value; }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ref IntPtr ElementAt(int index)
        {
            return ref ((IntPtr*)Ptr)[index];
        }

        /// <summary>
        /// Constructs list as view into memory.
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="length"></param>
        public unsafe UnsafePtrList(void** ptr, int length)
        {
            Ptr = ptr;
            this.length = length;
            this.capacity = length;
            Allocator = AllocatorManager.None;
        }

        /// <summary>
        /// Constructs a new list using the specified type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        /// <remarks>The list initially has a capacity of one. To avoid reallocating memory for the list, specify
        /// sufficient capacity up front.</remarks>
        public unsafe UnsafePtrList(int initialCapacity, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            Ptr = null;
            length = 0;
            capacity = 0;
            Allocator = AllocatorManager.None;

            var sizeOf = IntPtr.Size;
            this.ListData() = new UnsafeList(sizeOf, sizeOf, initialCapacity, allocator, options);
        }

        /// <summary>
        /// Constructs a new list using the specified type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        /// <remarks>The list initially has a capacity of one. To avoid reallocating memory for the list, specify
        /// sufficient capacity up front.</remarks>
        public unsafe UnsafePtrList(int initialCapacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            Ptr = null;
            length = 0;
            capacity = 0;
            Allocator = AllocatorManager.None;

            var sizeOf = IntPtr.Size;
            this.ListData() = new UnsafeList(sizeOf, sizeOf, initialCapacity, allocator, options);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="length"></param>
        /// <returns>New initialized container.</returns>
        public static UnsafePtrList* Create(void** ptr, int length)
        {
            UnsafePtrList* listData = AllocatorManager.Allocate<UnsafePtrList>(AllocatorManager.Persistent);
            *listData = new UnsafePtrList(ptr, length);
            return listData;
        }

        /// <summary>
        /// Creates a new list with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        /// <returns>New initialized container.</returns>
        public static UnsafePtrList* Create(int initialCapacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            UnsafePtrList* listData = AllocatorManager.Allocate<UnsafePtrList>(allocator);
            *listData = new UnsafePtrList(initialCapacity, allocator, options);
            return listData;
        }

        /// <summary>
        /// Destroys list.
        /// </summary>
        /// <param name="listData">Container to destroy.</param>
        public static void Destroy(UnsafePtrList* listData)
        {
            UnsafeList.CheckNull(listData);
            var allocator = listData->ListData().Allocator.Value == AllocatorManager.Invalid.Value
                ? AllocatorManager.Persistent
                : listData->ListData().Allocator
            ;
            listData->Dispose();
            AllocatorManager.Free(allocator, listData);
        }

        /// <summary>
        /// Reports whether container is empty.
        /// </summary>
        /// <value>True if this container empty.</value>
        public bool IsEmpty => !IsCreated || Length == 0;

        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// </summary>
        /// <value>True if this container object's internal storage has been allocated.</value>
        /// <remarks>
        /// Note that the container storage is not created if you use the default constructor. You must specify
        /// at least an allocation type to construct a usable container.
        ///
        /// *Warning:* the `IsCreated` property can't be used to determine whether a copy of a container is still valid.
        /// If you dispose any copy of the container, the container storage is deallocated. However, the properties of
        /// the other copies of the container (including the original) are not updated. As a result the `IsCreated` property
        /// of the copies still return `true` even though the container storage has been deallocated.
        /// </remarks>
        public bool IsCreated => Ptr != null;

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
            this.ListData().Dispose();
        }

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
        [BurstCompatible(RequiredUnityDefine = "UNITY_2020_2_OR_NEWER") /* Due to job scheduling on 2020.1 using statics */]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            return this.ListData().Dispose(inputDeps);
        }

        /// <summary>
        /// Clears the list.
        /// </summary>
        /// <remarks>List Capacity remains unchanged.</remarks>
        public void Clear()
        {
            this.ListData().Clear();
        }

        /// <summary>
        /// Changes the list length, resizing if necessary.
        /// </summary>
        /// <param name="length">The new length of the list.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        public void Resize(int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            this.ListData().Resize<IntPtr>(length, options);
        }

        /// <summary>
        /// Set the number of items that can fit in the list.
        /// </summary>
        /// <param name="capacity">The number of items that the list can hold before it resizes its internal storage.</param>
        public void SetCapacity(int capacity)
        {
            this.ListData().SetCapacity<IntPtr>(capacity);
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the container.
        /// </summary>
        public void TrimExcess()
        {
            this.ListData().TrimExcess<IntPtr>();
        }

        /// <summary>
        /// Searches for the specified element in list.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>The zero-based index of the first occurrence element if found, otherwise returns -1.</returns>
        public int IndexOf(void* value)
        {
            for (int i = 0; i < Length; ++i)
            {
                if (Ptr[i] == value) return i;
            }

            return -1;
        }

        /// <summary>
        /// Determines whether an element is in the list.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>True, if element is found.</returns>
        public bool Contains(void* value)
        {
            return IndexOf(value) != -1;
        }

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <param name="value">The value to be added at the end of the list.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddNoResize(void* value)
        {
            this.ListData().AddNoResize((IntPtr)value);
        }

        /// <summary>
        /// Adds elements from a buffer to this list.
        /// </summary>
        /// <param name="ptr">A pointer to the buffer.</param>
        /// <param name="length">The number of elements to add to the list.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddRangeNoResize(void** ptr, int length)
        {
            this.ListData().AddRangeNoResize<IntPtr>(ptr, length);
        }

        /// <summary>
        /// Adds elements from a list to this list.
        /// </summary>
        /// <param name="list">Other container to copy elements from.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddRangeNoResize(UnsafePtrList list)
        {
            this.ListData().AddRangeNoResize<IntPtr>(list.Ptr, list.Length);
        }

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <param name="value">The struct to be added at the end of the list.</param>
        public void Add(in IntPtr value)
        {
            this.ListData().Add(value);
        }

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <param name="value">The struct to be added at the end of the list.</param>
        public void Add(void* value)
        {
            this.ListData().Add((IntPtr)value);
        }

        /// <summary>
        /// Adds elements from a buffer to this list.
        /// </summary>
        /// <param name="ptr">A pointer to the buffer.</param>
        /// <param name="length">The number of elements to add to the list.</param>
        public void AddRange(void* ptr, int length)
        {
            this.ListData().AddRange<IntPtr>(ptr, length);
        }

        /// <summary>
        /// Adds the elements of a UnsafePtrList to this list.
        /// </summary>
        /// <param name="list">Other container to copy elements from.</param>
        public void AddRange(UnsafePtrList list)
        {
            this.ListData().AddRange<IntPtr>(list.ListData());
        }

        /// <summary>
        /// Inserts a number of items into a container at a specified zero-based index.
        /// </summary>
        /// <param name="begin">The zero-based index at which the new elements should be inserted.</param>
        /// <param name="end">The zero-based index just after where the elements should be removed.</param>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        public void InsertRangeWithBeginEnd(int begin, int end)
        {
            this.ListData().InsertRangeWithBeginEnd<IntPtr>(begin, end);
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index with the last item in the list. The list
        /// is shortened by one.
        /// </summary>
        /// <param name="index">The index of the item to delete.</param>
        public void RemoveAtSwapBack(int index)
        {
            this.ListData().RemoveAtSwapBack<IntPtr>(index);
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index range with the items from the end the list. The list
        /// is shortened by number of elements in range.
        /// </summary>
        /// <param name="begin">The first index of the item to remove.</param>
        /// <param name="end">The index past-the-last item to remove.</param>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        public void RemoveRangeSwapBackWithBeginEnd(int begin, int end)
        {
            this.ListData().RemoveRangeSwapBackWithBeginEnd<IntPtr>(begin, end);
        }

        /// <summary>
        /// Truncates the list by removing the item at the specified index, and shifting all remaining items to replace removed item. The list
        /// is shortened by one.
        /// </summary>
        /// <param name="index">The index of the item to delete.</param>
        /// <remarks>
        /// This method of removing item is useful only in case when list is ordered and user wants to preserve order
        /// in list after removal In majority of cases is not important and user should use more performant `RemoveAtSwapBack`.
        /// </remarks>
        public void RemoveAt(int index)
        {
            this.ListData().RemoveAt<IntPtr>(index);
        }

        /// <summary>
        /// Truncates the list by removing the items at the specified index range, and shifting all remaining items to replace removed items. The list
        /// is shortened by number of elements in range.
        /// </summary>
        /// <param name="begin">The first index of the item to remove.</param>
        /// <param name="end">The index past-the-last item to remove.</param>
        /// <remarks>
        /// This method of removing item(s) is useful only in case when list is ordered and user wants to preserve order
        /// in list after removal In majority of cases is not important and user should use more performant `RemoveRangeSwapBackWithBeginEnd`.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if end argument is less than begin argument.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if begin or end arguments are not positive or out of bounds.</exception>
        public void RemoveRangeWithBeginEnd(int begin, int end)
        {
            this.ListData().RemoveRangeWithBeginEnd<IntPtr>(begin, end);
        }

        /// <summary>
        /// This method is not implemented. It will throw NotImplementedException if it is used.
        /// </summary>
        /// <remarks>Use Enumerator GetEnumerator() instead.</remarks>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is not implemented. It will throw NotImplementedException if it is used.
        /// </summary>
        /// <remarks>Use Enumerator GetEnumerator() instead.</remarks>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<IntPtr> IEnumerable<IntPtr>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns parallel reader instance.
        /// </summary>
        /// <returns>Parallel reader instance.</returns>
        public ParallelReader AsParallelReader()
        {
            return new ParallelReader(Ptr, Length);
        }

        /// <summary>
        /// Implements parallel reader. Use AsParallelReader to obtain it from container.
        /// </summary>
        [BurstCompatible]
        public unsafe struct ParallelReader
        {
            /// <summary>
            ///
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public readonly void** Ptr;

            /// <summary>
            ///
            /// </summary>
            public readonly int Length;

            internal ParallelReader(void** ptr, int length)
            {
                Ptr = ptr;
                Length = length;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public int IndexOf(void* value)
            {
                for (int i = 0; i < Length; ++i)
                {
                    if (Ptr[i] == value) return i;
                }

                return -1;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public bool Contains(void* value)
            {
                return IndexOf(value) != -1;
            }
        }

        /// <summary>
        /// Returns parallel writer instance.
        /// </summary>
        /// <returns>Parallel writer instance.</returns>
        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter(Ptr, (UnsafeList*)UnsafeUtility.AddressOf(ref this));
        }

        /// <summary>
        ///
        /// </summary>
        [BurstCompatible]
        public unsafe struct ParallelWriter
        {
            /// <summary>
            ///
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public readonly void* Ptr;

            /// <summary>
            ///
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public UnsafeList* ListData;

            internal unsafe ParallelWriter(void* ptr, UnsafeList* listData)
            {
                Ptr = ptr;
                ListData = listData;
            }

            /// <summary>
            /// Adds an element to the list.
            /// </summary>
            /// <param name="value">The value to be added at the end of the list.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            public void AddNoResize(void* value)
            {
                ListData->AddNoResize((IntPtr)value);
            }

            /// <summary>
            /// Adds elements from a buffer to this list.
            /// </summary>
            /// <param name="ptr">A pointer to the buffer.</param>
            /// <param name="length">The number of elements to add to the list.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            public void AddRangeNoResize(void** ptr, int length)
            {
                ListData->AddRangeNoResize<IntPtr>(ptr, length);
            }

            /// <summary>
            /// Adds elements from a list to this list.
            /// </summary>
            /// <param name="list">Other container to copy elements from.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            public void AddRangeNoResize(UnsafePtrList list)
            {
                ListData->AddRangeNoResize<IntPtr>(list.Ptr, list.Length);
            }
        }
    }

    [BurstCompatible]
    internal static class UnsafePtrListExtensions
    {
        public static ref UnsafeList ListData(ref this UnsafePtrList from) => ref UnsafeUtility.As<UnsafePtrList, UnsafeList>(ref from);
    }

    internal sealed class UnsafePtrListDebugView
    {
        UnsafePtrList Data;

        public UnsafePtrListDebugView(UnsafePtrList data)
        {
            Data = data;
        }

        public unsafe IntPtr[] Items
        {
            get
            {
                IntPtr[] result = new IntPtr[Data.Length];

                for (var i = 0; i < result.Length; ++i)
                {
                    result[i] = (IntPtr)Data.Ptr[i];
                }

                return result;
            }
        }
    }
}
