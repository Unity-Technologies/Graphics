using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Collections
{
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface INativeList<T> where T : struct
    {
        /// <summary>
        ///
        /// </summary>
        int Length { get; set; }

        /// <summary>
        ///
        /// </summary>
        int Capacity { get; set; }

        /// <summary>
        /// Reports whether container is empty.
        /// </summary>
        /// <value>True if this container empty.</value>
        bool IsEmpty { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        T this[int index] { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        ref T ElementAt(int index);

        /// <summary>
        /// Clears the list.
        /// </summary>
        /// <remarks>List <see cref="Capacity"/> remains unchanged.</remarks>
        void Clear();
    }

    /// <summary>
    /// An unmanaged, resizable list.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the container.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(NativeListDebugView<>))]
    [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
    public unsafe struct NativeList<T>
        : INativeDisposable
        , INativeList<T>
        , IEnumerable<T> // Used by collection initializers.
        where T : struct
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeList<T>>();

        [BurstDiscard]
        static void CreateStaticSafetyId()
        {
            s_staticSafetyId.Data = AtomicSafetyHandle.NewStaticSafetyId<NativeList<T>>();
        }

        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeList* m_ListData;

        //@TODO: Unity.Physics currently relies on the specific layout of NativeList in order to
        //       workaround a bug in 19.1 & 19.2 with atomic safety handle in jobified Dispose.
        internal Allocator m_DeprecatedAllocator;

        /// <summary>
        /// Constructs a new list using the specified type of memory allocation.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <remarks>The list initially has a capacity of one. To avoid reallocating memory for the list, specify
        /// sufficient capacity up front.</remarks>
        public NativeList(Allocator allocator)
            : this(1, allocator, 2)
        {
        }

        /// <summary>
        /// Constructs a new list with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        public NativeList(int initialCapacity, Allocator allocator)
            : this(initialCapacity, allocator, 2)
        {
        }

        NativeList(int initialCapacity, Allocator allocator, int disposeSentinelStackDepth)
        {
            var totalSize = UnsafeUtility.SizeOf<T>() * (long)initialCapacity;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckAllocator(allocator);
            CheckInitialCapacity(initialCapacity);
            CollectionHelper.CheckIsUnmanaged<T>();
            CheckTotalSize(initialCapacity, totalSize);

            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, disposeSentinelStackDepth, allocator);
            if (s_staticSafetyId.Data == 0)
            {
                CreateStaticSafetyId();
            }
            AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, s_staticSafetyId.Data);
#endif
            m_ListData = UnsafeList.Create(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), initialCapacity, allocator);
            m_DeprecatedAllocator = allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        /// <summary>
        /// Retrieve a member of the contaner by index.
        /// </summary>
        /// <param name="index">The zero-based index into the list.</param>
        /// <value>The list item at the specified index.</value>
        /// <exception cref="IndexOutOfRangeException">Thrown if index is negative or >= to <see cref="Length"/>.</exception>
        public T this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                CheckIndexInRange(index, m_ListData->Length);
                return UnsafeUtility.ReadArrayElement<T>(m_ListData->Ptr, CollectionHelper.AssumePositive(index));
            }
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                CheckIndexInRange(index, m_ListData->Length);
                UnsafeUtility.WriteArrayElement(m_ListData->Ptr, CollectionHelper.AssumePositive(index), value);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ref T ElementAt(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            CheckIndexInRange(index, m_ListData->Length);
            return ref UnsafeUtility.ArrayElementAsRef<T>(m_ListData->Ptr, index);
        }

        /// <summary>
        /// The current number of items in the list.
        /// </summary>
        /// <value>The item count.</value>
        public int Length
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return CollectionHelper.AssumePositive(m_ListData->Length);
            }

            set
            {
                m_ListData->Resize<T>(value, NativeArrayOptions.ClearMemory);
            }
        }

        /// <summary>
        /// The number of items that can fit in the list.
        /// </summary>
        /// <value>The number of items that the list can hold before it resizes its internal storage.</value>
        /// <remarks>Capacity specifies the number of items the list can currently hold. You can change Capacity
        /// to fit more or fewer items. Changing Capacity creates a new array of the specified size, copies the
        /// old array to the new one, and then deallocates the original array memory. You cannot change the Capacity
        /// to a size smaller than <see cref="Length"/> (remove unwanted elements from the list first).</remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if Capacity is set smaller than Length.</exception>
        public int Capacity
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return CollectionHelper.AssumePositive(m_ListData->Capacity);
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
                CheckCapacityInRange(value, m_ListData->Length);
                m_ListData->SetCapacity<T>(value);
            }
        }

        /// <summary>
        /// Return internal UnsafeList*
        /// </summary>
        /// <returns></returns>
        public UnsafeList* GetUnsafeList() => m_ListData;

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <param name="value">The value to be added at the end of the list.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddNoResize(T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            m_ListData->AddNoResize(value);
        }

        /// <summary>
        /// Adds elements from a buffer to this list.
        /// </summary>
        /// <param name="ptr">A pointer to the buffer.</param>
        /// <param name="length">The number of elements to add to the list.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if length is negative.</exception>
        public void AddRangeNoResize(void* ptr, int length)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            CheckArgPositive(length);
            m_ListData->AddRangeNoResize<T>(ptr, length);
        }

        /// <summary>
        /// Adds elements from a list to this list.
        /// </summary>
        /// <param name="list">Other container to copy elements from.</param>
        /// <remarks>
        /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
        /// </remarks>
        public void AddRangeNoResize(NativeList<T> list)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            m_ListData->AddRangeNoResize<T>(*list.m_ListData);
        }

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <param name="value">The struct to be added at the end of the list.</param>
        /// <remarks>If the list has reached its current capacity, it copies the original, internal array to
        /// a new, larger array, and then deallocates the original.
        /// </remarks>
        public void Add(in T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_ListData->Add(value);
        }

        /// <summary>
        /// Adds the elements of a NativeArray to this list.
        /// </summary>
        /// <param name="elements">The items to add.</param>
        public void AddRange(NativeArray<T> elements)
        {
            AddRange(elements.GetUnsafeReadOnlyPtr(), elements.Length);
        }

        /// <summary>
        /// Adds elements from a buffer to this list.
        /// </summary>
        /// <param name="elements">A pointer to the buffer.</param>
        /// <param name="count">The number of elements to add to the list.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        public void AddRange(void* elements, int count)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            CheckArgPositive(count);
            m_ListData->AddRange<T>(elements, CollectionHelper.AssumePositive(count));
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_ListData->InsertRangeWithBeginEnd<T>(CollectionHelper.AssumePositive(begin), CollectionHelper.AssumePositive(end));
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index with the last item in the list. The list
        /// is shortened by one.
        /// </summary>
        /// <param name="index">The index of the item to delete.</param>
        /// <exception cref="ArgumentOutOfRangeException">If index is negative or >= <see cref="Length"/>.</exception>
        public void RemoveAtSwapBack(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_ListData->RemoveAtSwapBack<T>(CollectionHelper.AssumePositive(index));
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_ListData->RemoveRangeSwapBackWithBeginEnd<T>(CollectionHelper.AssumePositive(begin), CollectionHelper.AssumePositive(end));
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_ListData->RemoveAt<T>(CollectionHelper.AssumePositive(index));
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_ListData->RemoveRangeWithBeginEnd<T>(begin, end);
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
        /// Accessing the data of a native container that has been disposed throws a <see cref='InvalidOperationException'/> exception.
        /// </remarks>
        public bool IsCreated => m_ListData != null;

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            UnsafeList.Destroy(m_ListData);
            m_ListData = null;
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // [DeallocateOnJobCompletion] is not supported, but we want the deallocation
            // to happen in a thread. DisposeSentinel needs to be cleared on main thread.
            // AtomicSafetyHandle can be destroyed after the job was scheduled (Job scheduling
            // will check that no jobs are writing to the container).
            DisposeSentinel.Clear(ref m_DisposeSentinel);

            var jobHandle = new NativeListDisposeJob { Data = new NativeListDispose { m_ListData = m_ListData, m_Safety = m_Safety } }.Schedule(inputDeps);

            AtomicSafetyHandle.Release(m_Safety);
#else
            var jobHandle = new NativeListDisposeJob { Data = new NativeListDispose { m_ListData = m_ListData } }.Schedule(inputDeps);
#endif
            m_ListData = null;

            return jobHandle;
        }

        /// <summary>
        /// Clears the list.
        /// </summary>
        /// <remarks>List <see cref="Capacity"/> remains unchanged.</remarks>
        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_ListData->Clear();
        }

        /// <summary>
        /// This list as a [NativeArray](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArray_1.html).
        /// </summary>
        /// <remarks>The array is not a copy; it references the same memory as the original list.</remarks>
        /// <param name="nativeList">A NativeList instance.</param>
        /// <returns>A NativeArray containing all the items in the list.</returns>
        public static implicit operator NativeArray<T>(NativeList<T> nativeList)
        {
            return nativeList.AsArray();
        }

        /// <summary>
        /// This list as a [NativeArray](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArray_1.html).
        /// </summary>
        /// <remarks>The array is not a copy; it references the same memory as the original list. You can use the
        /// NativeArray API to manipulate the list.</remarks>
        /// <returns>A NativeArray "view" of the list.</returns>
        public NativeArray<T> AsArray()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Safety);
            var arraySafety = m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref arraySafety);
#endif
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(m_ListData->Ptr, m_ListData->Length, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, arraySafety);
#endif
            return array;
        }

        /// <summary>
        /// Provides a NativeArray that you can pass into a job whose contents can be modified by a previous job.
        /// </summary>
        /// <remarks>Pass a deferred array to a job when the list is populated or modified by a previous job. Using a
        /// deferred array allows you to schedule both jobs at the same time. (Without a deferred array, you would
        /// have to wait for the results of the first job before you scheduling the second.)</remarks>
        /// <returns>A [NativeArray](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArray_1.html) that
        /// can be passed to one job as a "promise" that is fulfilled by a previous job.</returns>
        /// <example>
        /// The following example populates a list with integers in one job and passes that data to a second job as
        /// a deferred array. If you tried to pass the list directly to the second job, that job would get the contents
        /// of the list at the time you schedule the job and would not see any modifications made to the list by the
        /// first job.
        /// <code>
        /// using UnityEngine;
        /// using Unity.Jobs;
        /// using Unity.Collections;
        ///
        /// public class DeferredArraySum : MonoBehaviour
        ///{
        ///    public struct ListPopulatorJob : IJob
        ///    {
        ///        public NativeList&lt;int&gt; list;
        ///
        ///        public void Execute()
        ///        {
        ///            for (int i = list.Length; i &lt; list.Capacity; i++)
        ///            {
        ///                list.Add(i);
        ///            }
        ///        }
        ///    }
        ///
        ///    public struct ArraySummerJob : IJob
        ///    {
        ///        [ReadOnly] public NativeArray&lt;int&gt; deferredArray;
        ///        public NativeArray&lt;int&gt; sum;
        ///
        ///        public void Execute()
        ///        {
        ///            sum[0] = 0;
        ///            for (int i = 0; i &lt; deferredArray.Length; i++)
        ///            {
        ///                sum[0] += deferredArray[i];
        ///            }
        ///        }
        ///    }
        ///
        ///    void Start()
        ///    {
        ///        var deferredList = new NativeList&lt;int&gt;(100, Allocator.TempJob);
        ///
        ///        var populateJob = new ListPopulatorJob()
        ///        {
        ///            list = deferredList
        ///        };
        ///
        ///        var output = new NativeArray&lt;int&gt;(1, Allocator.TempJob);
        ///        var sumJob = new ArraySummerJob()
        ///        {
        ///            deferredArray = deferredList.AsDeferredJobArray(),
        ///            sum = output
        ///        };
        ///
        ///        var populateJobHandle = populateJob.Schedule();
        ///        var sumJobHandle = sumJob.Schedule(populateJobHandle);
        ///
        ///        sumJobHandle.Complete();
        ///
        ///        Debug.Log("Result: " + output[0]);
        ///
        ///        deferredList.Dispose();
        ///        output.Dispose();
        ///    }
        /// }
        /// </code>
        /// </example>
        public NativeArray<T> AsDeferredJobArray()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
#endif
            byte* buffer = (byte*)m_ListData;
            // We use the first bit of the pointer to infer that the array is in list mode
            // Thus the job scheduling code will need to patch it.
            buffer += 1;
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(buffer, 0, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
#endif

            return array;
        }

        /// <summary>
        /// A copy of this list as a [NativeArray](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArray_1.html).
        /// </summary>
        /// <returns>A NativeArray containing copies of all the items in the list.</returns>
        [NotBurstCompatible]
        public T[] ToArray()
        {
            return AsArray().ToArray();
        }

        /// <summary>
        /// A copy of this list as a [NativeArray](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArray_1.html)
        /// allocated with the specified type of memory.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns>A NativeArray containing copies of all the items in the list.</returns>
        public NativeArray<T> ToArray(Allocator allocator)
        {
            NativeArray<T> result = new NativeArray<T>(Length, allocator, NativeArrayOptions.UninitializedMemory);
            result.CopyFrom(this);
            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public NativeArray<T>.Enumerator GetEnumerator()
        {
            var array = AsArray();
            return new NativeArray<T>.Enumerator(ref array);
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
        /// Overwrites this list with the elements of an array.
        /// </summary>
        /// <param name="array">A managed array to copy  into this list.</param>
        [NotBurstCompatible]
        public void CopyFrom(T[] array)
        {
            Clear();
            Resize(array.Length, NativeArrayOptions.UninitializedMemory);
            NativeArray<T> na = AsArray();
            na.CopyFrom(array);
        }

        /// <summary>
        /// Overwrites this list with the elements of an array.
        /// </summary>
        /// <param name="array"> [NativeArray](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArray_1.html) to copy into this list.</param>
        public void CopyFrom(NativeArray<T> array)
        {
            Clear();
            Resize(array.Length, NativeArrayOptions.UninitializedMemory);
            NativeArray<T> thisArray = AsArray();
            thisArray.CopyFrom(array);
        }

        /// <summary>
        /// Changes the list length, resizing if necessary.
        /// </summary>
        /// <param name="length">The new length of the list.</param>
        /// <param name="options">Memory should be cleared on allocation or left uninitialized.</param>
        public void Resize(int length, NativeArrayOptions options)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            m_ListData->Resize(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), length, options);
        }

        /// <summary>
        /// Changes the container length, resizing if necessary, without initializing memory.
        /// </summary>
        /// <param name="length">The new length of the container.</param>
        public void ResizeUninitialized(int length)
        {
            Resize(length, NativeArrayOptions.UninitializedMemory);
        }

        /// <summary>
        /// Returns parallel reader instance.
        /// </summary>
        /// <returns>Parallel reader instance.</returns>
        public NativeArray<T>.ReadOnly AsParallelReader()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new NativeArray<T>.ReadOnly(m_ListData->Ptr, m_ListData->Length, ref m_Safety);
#else
            return new NativeArray<T>.ReadOnly(m_ListData->Ptr, m_ListData->Length);
#endif
        }

        /// <summary>
        /// Returns parallel writer instance.
        /// </summary>
        /// <returns>Parallel writer instance.</returns>
        public ParallelWriter AsParallelWriter()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new ParallelWriter(m_ListData->Ptr, m_ListData, ref m_Safety);
#else
            return new ParallelWriter(m_ListData->Ptr, m_ListData);
#endif
        }

        /// <summary>
        /// Implements parallel writer. Use AsParallelWriter to obtain it from container.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
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

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;

            internal unsafe ParallelWriter(void* ptr, UnsafeList* listData, ref AtomicSafetyHandle safety)
            {
                Ptr = ptr;
                ListData = listData;
                m_Safety = safety;
            }

#else
            internal unsafe ParallelWriter(void* ptr, UnsafeList* listData)
            {
                Ptr = ptr;
                ListData = listData;
            }

#endif

            /// <summary>
            /// Adds an element to the list.
            /// </summary>
            /// <param name="value">The value to be added at the end of the list.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            public void AddNoResize(T value)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                var idx = Interlocked.Increment(ref ListData->Length) - 1;
                CheckSufficientCapacity(ListData->Capacity, idx + 1);

                UnsafeUtility.WriteArrayElement(Ptr, idx, value);
            }

            void AddRangeNoResize(int sizeOf, int alignOf, void* ptr, int length)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                var idx = Interlocked.Add(ref ListData->Length, length) - length;
                CheckSufficientCapacity(ListData->Capacity, idx + length);

                void* dst = (byte*)Ptr + idx * sizeOf;
                UnsafeUtility.MemCpy(dst, ptr, length * sizeOf);
            }

            /// <summary>
            /// Adds elements from a buffer to this list.
            /// </summary>
            /// <param name="ptr">A pointer to the buffer.</param>
            /// <param name="length">The number of elements to add to the list.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if length is negative.</exception>
            public void AddRangeNoResize(void* ptr, int length)
            {
                CheckArgPositive(length);
                AddRangeNoResize(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), ptr, CollectionHelper.AssumePositive(length));
            }

            /// <summary>
            /// Adds elements from a list to this list.
            /// </summary>
            /// <param name="list">Other container to copy elements from.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            public void AddRangeNoResize(UnsafeList list)
            {
                AddRangeNoResize(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), list.Ptr, list.Length);
            }

            /// <summary>
            /// Adds elements from a list to this list.
            /// </summary>
            /// <param name="list">Other container to copy elements from.</param>
            /// <remarks>
            /// If the list has reached its current capacity, internal array won't be resized, and exception will be thrown.
            /// </remarks>
            public void AddRangeNoResize(NativeList<T> list)
            {
                AddRangeNoResize(*list.m_ListData);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckAllocator(Allocator allocator)
        {
            // Native allocation is only valid for Temp, Job and Persistent.
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckInitialCapacity(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Capacity must be >= 0");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckTotalSize(int initialCapacity, long totalSize)
        {
            // Make sure we cannot allocate more than int.MaxValue (2,147,483,647 bytes)
            // because the underlying UnsafeUtility.Malloc is expecting a int.
            // TODO: change UnsafeUtility.Malloc to accept a UIntPtr length instead to match C++ API
            if (totalSize > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), $"Capacity * sizeof(T) cannot exceed {int.MaxValue} bytes");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckSufficientCapacity(int capacity, int length)
        {
            if (capacity < length)
                throw new Exception($"Length {length} exceeds capacity Capacity {capacity}");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckIndexInRange(int value, int length)
        {
            if (value < 0)
                throw new IndexOutOfRangeException($"Value {value} must be positive.");

            if ((uint)value >= (uint)length)
                throw new IndexOutOfRangeException($"Value {value} is out of range in NativeList of '{length}' Length.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckCapacityInRange(int value, int length)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException($"Value {value} must be positive.");

            if ((uint)value < (uint)length)
                throw new ArgumentOutOfRangeException($"Value {value} is out of range in NativeList of '{length}' Length.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckArgPositive(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException($"Value {value} must be positive.");
        }
    }

    [NativeContainer]
    [BurstCompatible]
    internal unsafe struct NativeListDispose
    {
        [NativeDisableUnsafePtrRestriction]
        public UnsafeList* m_ListData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        public void Dispose()
        {
            UnsafeList.Destroy(m_ListData);
        }
    }

    [BurstCompile]
    [BurstCompatible]
    internal unsafe struct NativeListDisposeJob : IJob
    {
        internal NativeListDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    }

    sealed class NativeListDebugView<T> where T : struct
    {
        NativeList<T> m_Array;

        public NativeListDebugView(NativeList<T> array)
        {
            m_Array = array;
        }

        public T[] Items => m_Array.ToArray();
    }
}

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Utilities for unsafe access to a <see cref="NativeList{T}"/>.
    /// </summary>
    [BurstCompatible]
    public unsafe static class NativeListUnsafeUtility
    {
        /// <summary>
        /// Gets a pointer to the memory buffer containing the list items.
        /// </summary>
        /// <param name="list">The NativeList containing the buffer.</param>
        /// <typeparam name="T">The type of list element.</typeparam>
        /// <returns>A pointer to the memory buffer.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public static void* GetUnsafePtr<T>(this NativeList<T> list) where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(list.m_Safety);
#endif
            return list.m_ListData->Ptr;
        }

        /// <summary>
        /// Gets a pointer to the memory buffer containing the list items.
        /// </summary>
        /// <param name="list">The NativeList containing the buffer.</param>
        /// <typeparam name="T">The type of list element.</typeparam>
        /// <returns>A pointer to the memory buffer.</returns>
        /// <remarks>Thread safety mechanism is informed that this pointer will be used for read-only operations.</remarks>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public static unsafe void* GetUnsafeReadOnlyPtr<T>(this NativeList<T> list) where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(list.m_Safety);
#endif
            return list.m_ListData->Ptr;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        /// <summary>
        /// Gets the [AtomicSafetyHandle](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle.html)
        /// used by the C# Job system to validate safe access to the list.
        /// </summary>
        /// <param name="list">The NativeList.</param>
        /// <typeparam name="T">The type of list element.</typeparam>
        /// <returns>The atomic safety handle for the list.</returns>
        /// <remarks>The symbol, `ENABLE_UNITY_COLLECTIONS_CHECKS` must be defined for this function to be available.</remarks>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public static AtomicSafetyHandle GetAtomicSafetyHandle<T>(ref NativeList<T> list) where T : struct
        {
            return list.m_Safety;
        }

#endif

        /// <summary>
        /// Gets a pointer to the internal list data (without checking for safe access).
        /// </summary>
        /// <param name="list">The NativeList.</param>
        /// <typeparam name="T">The type of list element.</typeparam>
        /// <returns>A pointer to the list data.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public static void* GetInternalListDataPtrUnchecked<T>(ref NativeList<T> list) where T : struct
        {
            return list.m_ListData;
        }
    }
}
