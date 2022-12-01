using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class HDShadowRequestDatabase
    {
#region ASCII Diagram
        // Diagram of HDShadowRequestDatabase and its relation to HDLightRenderDatabase:
        //
        // ===================================================
        // key:                               +---+         ||
        //         array index (* is value):  | * |         ||
        //                                    +---+         ||
        //                                                  ||
        //                                    --+           ||
        //                  index reference:    |           ||
        //                                      +-->        ||
        //                                                  ||
        //                                                  ||
        // ===================================================
        //
        //                                       +----------------------------------------------------+
        //                                       |                                                    |
        //                                       |              free index: 0-----+       +-------+   |   +---+
        //                                       |                                |       |       |   |   |   |
        //                 +---+---+---+---+---+---+---+---+                      v       |       v   v   |   v
        // sparse handles: |-1 |-1 |-1 |-1 |-1 | 5 |-1 |-1 |                    +---+---+---+---+---+---+---+---+
        //                 +---+---+---+---+---+---+---+---+    shadow requests:| 1-->2-->4 | 6<--3 |   | 7 | 8 --->
        //  lightEntities: |-1 |-1 | 1 |-1 |-1 | 0 |-1 |-1 |                    |   |   |   | +-----------^ |   |
        //                 +---+---+---+---+---+---+---+---+                    |   |   |   |   |   |   |   |   |
        //                           |           |                    request 0 | * | * | * | * | * | * | * | * |
        //                           |           |                    request 1 | * | * | * | * | * | * | * | * |
        //                       +---+           |                    request 2 | * | * | * | * | * | * | * | * |
        //                   +---|---------------+                    request 3 | * | * | * | * | * | * | * | * |
        //                   |   |                                    request 4 | * | * | * | * | * | * | * | * |
        //                   |   |                                    request 5 | * | * | * | * | * | * | * | * |
        //                   v   v                                              +---+---+---+---+---+---+---+---+
        //                 +---+---+---+---+---+---+---+---+                                          ^
        // packed handles: | 5 |-1 |-1 |-1 |-1 |-1 |-1 |-1 |                                          |
        //                 +---+---+---+---+---+---+---+---+                                          |
        //                   |                                                                        |
        //                   |                                                                        |
        //                   +------------------------------------------------------------------------+
        //
        //
        // Explanation:
        // A lightEntity in the HDLightRenderDatabase may or may not allocate a shadow from the HDShadowRequestDatabase.
        // If it does, it maintains references in both its sparse array for quick lookup by entity,
        // and in its packed (aka dense) array for quick lookup while looping through packed lightEntity data.
        //
        // As for the shadow requests themselves, they are allocated via an in-place free list,
        // where a linked list of free indices is maintained within the free indices themselves.
        // Additionally, each shadow index is actually six shadow indices,
        // as lights can have up to six shadow requests depending on light type.
        //
        // When light render data is normalized in the future (as arrays of arrays),
        // the shadow database will be removed. In its place will be a subset of the normalized light arrays,
        // each of which will contain shadow data that is index-aligned with the other light data.
#endregion

        private static HDShadowRequestDatabase s_Instance;
        // TODO: Pad each set of HDShadowRequests to eliminate cache line sharing between sets, as a prerequisite for parallel shadow request update work.
        private int m_HDShadowRequestFreeListIndex;
        private int m_HDShadowRequestCount;
        private int m_HDShadowRequestCapacity;
        private NativeList<HDShadowRequest> m_HDShadowRequestStorage = new NativeList<HDShadowRequest>(Allocator.Persistent);
        private NativeList<int> m_HDShadowRequestIndicesStorage = new NativeList<int>(Allocator.Persistent);
        private NativeList<float4> m_FrustumPlanesStorage = new NativeList<float4>(Allocator.Persistent);
        private NativeList<Vector3> m_CachedViewPositionsStorage = new NativeList<Vector3>(Allocator.Persistent);
        private bool m_HDShadowRequestsCreated = true;

        public NativeList<HDShadowRequest> hdShadowRequestStorage => m_HDShadowRequestStorage;
        public NativeList<int> hdShadowRequestIndicesStorage => m_HDShadowRequestIndicesStorage;
        public NativeList<float4> frustumPlanesStorage => m_FrustumPlanesStorage;
        public NativeList<Vector3> cachedViewPositionsStorage => m_CachedViewPositionsStorage;
        static public HDShadowRequestDatabase instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new HDShadowRequestDatabase();
                return s_Instance;
            }
        }

        public bool IsCreated => m_HDShadowRequestsCreated;

        public void EnsureNativeListsAreCreated()
        {
            if (!m_HDShadowRequestsCreated)
            {
                m_HDShadowRequestsCreated = true;
                m_HDShadowRequestStorage = new NativeList<HDShadowRequest>(Allocator.Persistent);
                m_HDShadowRequestIndicesStorage = new NativeList<int>(Allocator.Persistent);
                m_FrustumPlanesStorage = new NativeList<float4>(Allocator.Persistent);
                m_CachedViewPositionsStorage = new NativeList<Vector3>(Allocator.Persistent);
            }
        }

        public unsafe HDShadowRequestSetHandle AllocateHDShadowRequests()
        {
            EnsureNativeListsAreCreated();

            int oldFreeIndex = m_HDShadowRequestFreeListIndex;
            int oldCapacity = m_HDShadowRequestCapacity;

            if (oldFreeIndex >= oldCapacity)
            {
                int newCapacity = oldCapacity * 2;
                newCapacity = newCapacity < 1 ? 1 : newCapacity;
                m_HDShadowRequestStorage.Length = newCapacity * HDShadowRequest.maxLightShadowRequestsCount;
                m_HDShadowRequestIndicesStorage.Length = newCapacity * HDShadowRequest.maxLightShadowRequestsCount;
                m_FrustumPlanesStorage.Length = newCapacity * HDShadowRequest.maxLightShadowRequestsCount * HDShadowRequest.frustumPlanesCount;
                m_CachedViewPositionsStorage.Length = newCapacity * HDShadowRequest.maxLightShadowRequestsCount;

                ref UnsafeList<int> requestIndicesExpanded = ref UnsafeUtility.AsRef<UnsafeList<int>>(m_HDShadowRequestIndicesStorage.GetUnsafeList());
                for (int i = oldCapacity; i < newCapacity; i++)
                {
                    requestIndicesExpanded[i * HDShadowRequest.maxLightShadowRequestsCount] = i + 1;
                }

                m_HDShadowRequestCapacity = newCapacity;
            }

            ref UnsafeList<HDShadowRequest> requests = ref UnsafeUtility.AsRef<UnsafeList<HDShadowRequest>>(m_HDShadowRequestStorage.GetUnsafeList());
            int sizeOfRequests = UnsafeUtility.SizeOf<HDShadowRequest>();
            int requestsByteCount = sizeOfRequests * HDShadowRequest.maxLightShadowRequestsCount;
            UnsafeUtility.MemClear(requests.Ptr + oldFreeIndex * HDShadowRequest.maxLightShadowRequestsCount, requestsByteCount);
            for (int i = oldFreeIndex * HDShadowRequest.maxLightShadowRequestsCount; i < oldFreeIndex * HDShadowRequest.maxLightShadowRequestsCount + HDShadowRequest.maxLightShadowRequestsCount; i++)
            {
                requests.ElementAt(i).InitDefault();
            }

            ref UnsafeList<int> requestIndices = ref UnsafeUtility.AsRef<UnsafeList<int>>(m_HDShadowRequestIndicesStorage.GetUnsafeList());
            m_HDShadowRequestFreeListIndex = requestIndices[oldFreeIndex * HDShadowRequest.maxLightShadowRequestsCount];
            int sizeOfIndices = sizeof(int);
            int indicesByteCount = sizeOfIndices * HDShadowRequest.maxLightShadowRequestsCount;
            UnsafeUtility.MemClear(requestIndices.Ptr + oldFreeIndex * HDShadowRequest.maxLightShadowRequestsCount, indicesByteCount);

            ref UnsafeList<Vector4> planes = ref UnsafeUtility.AsRef<UnsafeList<Vector4>>(m_FrustumPlanesStorage.GetUnsafeList());
            int sizeOfPlane = UnsafeUtility.SizeOf<Vector4>();
            int planesByteCount = sizeOfPlane * HDShadowRequest.frustumPlanesCount * HDShadowRequest.maxLightShadowRequestsCount;
            UnsafeUtility.MemClear(planes.Ptr + oldFreeIndex * HDShadowRequest.frustumPlanesCount * HDShadowRequest.maxLightShadowRequestsCount, planesByteCount);

            ref UnsafeList<Vector3> cachedViewPositions = ref UnsafeUtility.AsRef<UnsafeList<Vector3>>(m_CachedViewPositionsStorage.GetUnsafeList());
            int sizeOfViewPositions = UnsafeUtility.SizeOf<Vector3>();
            int viewPositionsByteCount = sizeOfViewPositions * HDShadowRequest.maxLightShadowRequestsCount;
            UnsafeUtility.MemClear(cachedViewPositions.Ptr + oldFreeIndex * HDShadowRequest.maxLightShadowRequestsCount, viewPositionsByteCount);

            ++m_HDShadowRequestCount;

            return new HDShadowRequestSetHandle { relativeDataOffset = oldFreeIndex };
        }

        public unsafe void FreeHDShadowRequests(ref HDShadowRequestSetHandle shadowHandle)
        {
            if (!shadowHandle.valid)
                return;

            ref UnsafeList<int> requestIndices = ref UnsafeUtility.AsRef<UnsafeList<int>>(m_HDShadowRequestIndicesStorage.GetUnsafeList());
            requestIndices[shadowHandle.storageIndexForCachedViewPositions] = m_HDShadowRequestFreeListIndex;
            m_HDShadowRequestFreeListIndex = shadowHandle.relativeDataOffset;

            --m_HDShadowRequestCount;

            if (m_HDShadowRequestCount == 0)
            {
                DeleteArrays();
            }

            shadowHandle = new HDShadowRequestSetHandle(){relativeDataOffset = HDShadowRequestSetHandle.InvalidIndex};
        }

        public void DeleteArrays()
        {
            m_HDShadowRequestFreeListIndex = 0;
            m_HDShadowRequestCapacity = 0;
            m_HDShadowRequestCapacity = 0;

            if (m_HDShadowRequestsCreated)
            {
                m_HDShadowRequestStorage.Dispose();
                m_HDShadowRequestStorage = default;
                m_HDShadowRequestIndicesStorage.Dispose();
                m_HDShadowRequestIndicesStorage = default;
                m_CachedViewPositionsStorage.Dispose();
                m_CachedViewPositionsStorage = default;
                m_FrustumPlanesStorage.Dispose();
                m_FrustumPlanesStorage = default;
            }

            m_HDShadowRequestsCreated = false;
        }
    }
}
