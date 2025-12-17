using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    internal static class GPUResidentUtils
    {
        public static void RunParallelByRef<T>(this ref T jobData, int arrayLength, int innerloopBatchCount, JobHandle dependsOn = default) where T : unmanaged, IJobParallelFor
        {
            if (arrayLength > innerloopBatchCount)
            {
                jobData.ScheduleByRef(arrayLength, innerloopBatchCount, dependsOn).Complete();
            }
            else
            {
                dependsOn.Complete();
                jobData.RunByRef(arrayLength);
            }
        }

        public static void RunParallel<T>(this T jobData, int arrayLength, int innerloopBatchCount, JobHandle dependsOn = default) where T : unmanaged, IJobParallelFor
        {
            RunParallelByRef(ref jobData, arrayLength, innerloopBatchCount, dependsOn);
        }

        public static void RunBatchParallelByRef<T>(this ref T jobData, int arrayLength, int innerloopBatchCount, JobHandle dependsOn = default) where T : unmanaged, IJobParallelForBatch
        {
            if (arrayLength > innerloopBatchCount)
            {
                jobData.ScheduleBatchByRef(arrayLength, innerloopBatchCount, dependsOn).Complete();
            }
            else
            {
                dependsOn.Complete();
                jobData.RunByRef(arrayLength, innerloopBatchCount);
            }
        }

        public static void RunBatchParallel<T>(this T jobData, int arrayLength, int innerloopBatchCount, JobHandle dependsOn = default) where T : unmanaged, IJobParallelForBatch
        {
            RunBatchParallelByRef(ref jobData, arrayLength, innerloopBatchCount, dependsOn);
        }

        public static unsafe ref T ElementAtRW<T>(this NativeArray<T> array, int index) where T : unmanaged
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
        }

        public static unsafe ref readonly T ElementAt<T>(this NativeArray<T> array, int index) where T : unmanaged
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }

        public static unsafe NativeArray<T> AsNativeArray<T>(this UnsafeList<T> list) where T : unmanaged
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(list.Ptr,
                list.Length,
                Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.Create());
#endif

            return array;
        }

        public static unsafe UnsafeList<T> AsUnsafeList<T>(this NativeArray<T> array) where T : unmanaged
        {
            return new UnsafeList<T>((T*)array.GetUnsafePtr(), array.Length);
        }

        public static unsafe UnsafeList<T> AsUnsafeListReadOnly<T>(this NativeArray<T> array) where T : unmanaged
        {
            return new UnsafeList<T>((T*)array.GetUnsafeReadOnlyPtr(), array.Length);
        }

        public static unsafe UnsafeList<UntypedUnsafeList> AsUntypedUnsafeList<T>(this UnsafeList<UnsafeList<T>> list) where T : unmanaged
        {
            Assert.IsTrue(UnsafeUtility.SizeOf<UnsafeList<T>>() == UnsafeUtility.SizeOf<UntypedUnsafeList>());
            return *(UnsafeList<UntypedUnsafeList>*)&list;
        }

        public static unsafe UnsafeBitArray AsUnsafeBitArray(this in NativeBitArray section) => *section.m_BitArray;

        public static unsafe ref T GetRef<T>(this NativeReference<T> reference) where T : unmanaged => ref *reference.GetUnsafePtr();
        public static unsafe ref readonly T GetRefRO<T>(this NativeReference<T> reference) where T : unmanaged => ref *reference.GetUnsafeReadOnlyPtr();

        public static bool HasAnyBit(this MeshRendererComponentMask mask, MeshRendererComponentMask bits) => (mask & bits) != 0;

        public static bool HasAnyBit(this LODGroupComponentMask mask, LODGroupComponentMask bits) => (mask & bits) != 0;
    }

    internal static class LightmapUtils
    {
        // See doc for more infos on lightmap index special values -1 and -2
        // https://docs.unity3d.com/Documentation/ScriptReference/Renderer-lightmapIndex.html

        // Object doesn't use lightmaps and don't influence them.
        public const short LightmapIndexNull = -1;

        // Object only influences lightmaps, but does not use them itself.
        public const short LightmapIndexInfluenceOnly = -2;

        public static readonly float4 kDefaultLightmapST = new float4(1.0f, 1.0f, 0.0f, 0.0f);

        public static bool IsNull(int lightmapIndex) => ((short)lightmapIndex) == LightmapIndexNull;

        public static bool IsInfluenceOnly(int lightmapIndex) => ((short)lightmapIndex) == LightmapIndexInfluenceOnly;

        public static bool UsesLightmaps(int lightmapIndex) => ((short)lightmapIndex) >= 0;

        // If the object is light mapped, or has the special influence-only value, it affects lightmaps
        public static bool AffectsLightmaps(int lightmapIndex) => ((short)lightmapIndex) >= 0 || ((short)lightmapIndex) == LightmapIndexInfluenceOnly;
    }
}
