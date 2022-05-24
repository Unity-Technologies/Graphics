using System;
using UnityEngine.Jobs;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.HighDefinition
{
    //Light rendering entity. This struct acts as a handle to set / get light render information into the database.
    internal struct HDLightRenderEntity
    {
        public int entityIndex;
        public static readonly HDLightRenderEntity Invalid = new HDLightRenderEntity() { entityIndex = HDLightRenderDatabase.InvalidDataIndex };
        public bool valid { get { return entityIndex != HDLightRenderDatabase.InvalidDataIndex; } }
    }

    //Data of the lights inside the database.
    //TODO: as a next round of optimizations, this should be reorganized to be cache friendly, and possibly split into SoAs for that matter.
    internal struct HDLightRenderData
    {
        public HDAdditionalLightData.PointLightHDType pointLightType;
        public SpotLightShape spotLightShape;
        public AreaLightShape areaLightShape;
        public LightLayerEnum lightLayer;
        public bool affectDynamicGI;
        public bool mixedDynamicGI;
        public float fadeDistance;
        public float distance;
        public float angularDiameter;
        public float volumetricFadeDistance;
        public bool includeForRayTracing;
        public bool useScreenSpaceShadows;
        public bool useHierarchicalVarianceScreenSpaceShadows;
        public bool useRayTracedShadows;
        public bool colorShadow;
        public float lightDimmer;
        public float volumetricDimmer;
        public float shadowDimmer;
        public float shadowFadeDistance;
        public float volumetricShadowDimmer;
        public float shapeWidth;
        public float shapeHeight;
        public float aspectRatio;
        public float innerSpotPercent;
        public float spotIESCutoffPercent;
        public float shapeRadius;
        public float barnDoorLength;
        public float barnDoorAngle;
        public float flareSize;
        public float flareFalloff;
        public bool affectVolumetric;
        public bool affectDiffuse;
        public bool affectSpecular;
        public bool applyRangeAttenuation;
        public bool penumbraTint;
        public bool interactsWithSky;
        public Color surfaceTint;
        public Color shadowTint;
        public Color flareTint;
    }

    internal struct HDAdditionalLightDataUpdateInfo
    {
        public float shadowNearPlane;
        public float normalBias;
        public float shapeHeight;
        public float aspectRatio;
        public float shapeWidth;
        public float areaLightShadowCone;
        public float softnessScale;
        public float angularDiameter;
        public float shapeRadius;
        public float slopeBias;
        public float minFilterSize;
        public float lightAngle;
        public float maxDepthBias;
        public float evsmExponent;
        public float evsmLightLeakBias;
        public float evsmVarianceBias;
        public float customSpotLightShadowCone;
        public int blockerSampleCount;
        public int filterSampleCount;
        public int kernelSize;
        public int evsmBlurPasses;
        public SpotLightShape spotLightShape;
        public AreaLightShape areaLightShape;
        public bool useCustomSpotLightShadowCone;

        public void Set(HDAdditionalLightData additionalLightData)
        {
            shadowNearPlane = additionalLightData.shadowNearPlane;
            normalBias = additionalLightData.normalBias;
            shapeHeight = additionalLightData.shapeHeight;
            aspectRatio = additionalLightData.aspectRatio;
            shapeWidth = additionalLightData.shapeWidth;
            areaLightShadowCone = additionalLightData.areaLightShadowCone;
            softnessScale = additionalLightData.softnessScale;
            angularDiameter = additionalLightData.angularDiameter;
            shapeRadius = additionalLightData.shapeRadius;
            slopeBias = additionalLightData.slopeBias;
            minFilterSize = additionalLightData.minFilterSize;
            lightAngle = additionalLightData.lightAngle;
            maxDepthBias = additionalLightData.maxDepthBias;
            evsmExponent = additionalLightData.evsmExponent;
            evsmLightLeakBias = additionalLightData.evsmLightLeakBias;
            evsmVarianceBias = additionalLightData.evsmVarianceBias;
            customSpotLightShadowCone = additionalLightData.customSpotLightShadowCone;
            blockerSampleCount = additionalLightData.blockerSampleCount;
            filterSampleCount = additionalLightData.filterSampleCount;
            kernelSize = additionalLightData.kernelSize;
            evsmBlurPasses = additionalLightData.evsmBlurPasses;
            spotLightShape = additionalLightData.spotLightShape;
            areaLightShape = additionalLightData.areaLightShape;
            useCustomSpotLightShadowCone = additionalLightData.useCustomSpotLightShadowCone;
        }
    }

    //Class representing a rendering side database of lights in the world
    internal partial class HDLightRenderDatabase
    {
        #region internal HDRP API

        public static int InvalidDataIndex = -1;

        //total light count of all lights in the world.
        public int lightCount => m_LightCount;

        //gets the list of render light data. Use lightCount to iterate over all the world light data.
        public NativeArray<HDLightRenderData> lightData => m_LightData;

        //gets the list of render light entities handles. Use this entities to access or set light data indirectly.
        public NativeArray<HDLightRenderEntity> lightEntities => m_OwnerEntity;

        //Attachments in case the rendering pipeline uses game objects
        public DynamicArray<HDAdditionalLightData> hdAdditionalLightData => m_HDAdditionalLightData;
        public DynamicArray<GameObject> aovGameObjects => m_AOVGameObjects;

        public NativeList<HDShadowRequest> hdShadowRequestStorage => m_HDShadowRequestStorage;
        public NativeList<int> hdShadowRequestIndicesStorage => m_HDShadowRequestIndicesStorage;
        public NativeList<Vector4> frustumPlanesStorage => m_FrustumPlanesStorage;
        public NativeList<Vector3> cachedViewPositionsStorage => m_CachedViewPositionsStorage;

        public NativeList<HDShadowRequestSetHandle> packedShadowRequestSetHandles => m_ShadowRequestSetPackedHandles;

        public NativeArray<HDAdditionalLightDataUpdateInfo> additionalLightDataUpdateInfos => m_AdditionalLightDataUpdateInfos;

        // One per HDLightType
        public NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo>[] shadowRequestDataLists = new NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo>[]
        {
            new NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo>(Allocator.Persistent),
            new NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo>(Allocator.Persistent),
            new NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo>(Allocator.Persistent),
            new NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo>(Allocator.Persistent)
        };

        public NativeArray<Matrix4x4> cachedCubeMapFaces => m_CachedCubeMapFaces;

        //Access of main instance
        static public HDLightRenderDatabase instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new HDLightRenderDatabase();
                return s_Instance;
            }
        }

        //Gets a data reference from an entity. C# doesnt have a const modifier, however we keep this for convension, this ref shoulnd't be modified.
        public ref HDLightRenderData GetLightDataAsRef(in HDLightRenderEntity entity) => ref EditLightDataAsRef(entity);

        //Gets and edits a reference. Must be not called during rendering pipeline, only during game object modification.
        public ref HDLightRenderData EditLightDataAsRef(in HDLightRenderEntity entity) => ref EditLightDataAsRef(m_LightEntities[entity.entityIndex].dataIndex);

        //Gets a data reference from an entity. C# doesnt have a const modifier, however we keep this for convension, this ref shoulnd't be modified.
        public ref HDLightRenderData GetLightDataAsRef(int dataIndex) => ref EditLightDataAsRef(dataIndex);

        //Gets and edits a reference. Must be not called during rendering pipeline, only during game object modification.
        public ref HDLightRenderData EditLightDataAsRef(int dataIndex)
        {
            if (dataIndex >= m_LightCount)
                throw new Exception("Entity passed in is out of bounds. Index requested " + dataIndex + " and maximum length is " + m_LightCount);

            unsafe
            {
                HDLightRenderData* data = (HDLightRenderData*)m_LightData.GetUnsafePtr<HDLightRenderData>() + dataIndex;
                return ref UnsafeUtility.AsRef<HDLightRenderData>(data);
            }
        }

        public ref HDAdditionalLightDataUpdateInfo GetShadowRequestUpdateInfoAsRef(in HDLightRenderEntity entity)
        {
            int dataIndex = m_LightEntities[entity.entityIndex].dataIndex;
            if ( dataIndex >= m_LightCount)
                throw new Exception("Entity passed in is out of bounds. Index requested " + dataIndex + " and maximum length is " + m_LightCount);

            unsafe
            {
                HDAdditionalLightDataUpdateInfo* data = (HDAdditionalLightDataUpdateInfo*)m_AdditionalLightDataUpdateInfos.GetUnsafePtr<HDAdditionalLightDataUpdateInfo>() + dataIndex;
                return ref UnsafeUtility.AsRef<HDAdditionalLightDataUpdateInfo>(data);
            }
        }

        // Creates a light render entity.
        public HDLightRenderEntity CreateEntity(bool autoDestroy)
        {
            LightEntityInfo newData = AllocateEntityData();

            HDLightRenderEntity newLightEntity = HDLightRenderEntity.Invalid;
            if (m_FreeIndices.Count == 0)
            {
                newLightEntity.entityIndex = m_LightEntities.Count;
                m_LightEntities.Add(newData);
                m_ShadowRequestSetHandles.Add(new HDShadowRequestSetHandle{ relativeDataOffset =  HDShadowRequestSetHandle.InvalidIndex });
                m_ShadowRequestSetPackedHandles.Add(new HDShadowRequestSetHandle{ relativeDataOffset =  HDShadowRequestSetHandle.InvalidIndex });
            }
            else
            {
                newLightEntity.entityIndex = m_FreeIndices.Dequeue();
                m_LightEntities[newLightEntity.entityIndex] = newData;
                m_ShadowRequestSetHandles[newLightEntity.entityIndex] = new HDShadowRequestSetHandle {relativeDataOffset = HDShadowRequestSetHandle.InvalidIndex};
            }

            m_OwnerEntity[newData.dataIndex] = newLightEntity;
            m_AutoDestroy[newData.dataIndex] = autoDestroy;
            m_ShadowRequestSetPackedHandles[newData.dataIndex] = new HDShadowRequestSetHandle {relativeDataOffset = HDShadowRequestSetHandle.InvalidIndex};
            return newLightEntity;
        }

        //Must be called by game object so we can gather all the information needed,
        //This will prep this system when dots lights come in.
        //Dots lights wont have to use this and instead will have to set this data on their own.
        public unsafe void AttachGameObjectData(
            HDLightRenderEntity entity,
            int instanceID,
            HDAdditionalLightData additionalLightData,
            GameObject aovGameObject)
        {
            if (!IsValid(entity))
                return;

            var entityInfo = m_LightEntities[entity.entityIndex];
            int dataIndex = entityInfo.dataIndex;
            if (dataIndex == InvalidDataIndex)
                return;

            entityInfo.lightInstanceID = instanceID;
            m_LightEntities[entity.entityIndex] = entityInfo;

            m_LightsToEntityItem.Add(entityInfo.lightInstanceID, entityInfo);
            m_HDAdditionalLightData[dataIndex] = additionalLightData;
            m_AOVGameObjects[dataIndex] = aovGameObject;
            HDAdditionalLightDataUpdateInfo updateInfo = default;
            updateInfo.Set(additionalLightData);
            m_AdditionalLightDataUpdateInfos[dataIndex] = updateInfo;
            ++m_AttachedGameObjects;
        }

        // Destroys a light render entity.
        public unsafe void DestroyEntity(HDLightRenderEntity lightEntity)
        {
            Assert.IsTrue(IsValid(lightEntity));

            m_FreeIndices.Enqueue(lightEntity.entityIndex);
            LightEntityInfo entityData = m_LightEntities[lightEntity.entityIndex];
            m_LightsToEntityItem.Remove(entityData.lightInstanceID);

            if (m_HDAdditionalLightData[entityData.dataIndex] != null)
                --m_AttachedGameObjects;

            FreeHDShadowRequests(lightEntity);

            RemoveAtSwapBackArrays(entityData.dataIndex);

            if (m_LightCount == 0)
            {
                DeleteArrays();
            }
            else
            {
                HDLightRenderEntity entityToUpdate = m_OwnerEntity[entityData.dataIndex];
                LightEntityInfo dataToUpdate = m_LightEntities[entityToUpdate.entityIndex];
                dataToUpdate.dataIndex = entityData.dataIndex;
                m_LightEntities[entityToUpdate.entityIndex] = dataToUpdate;
                if (dataToUpdate.lightInstanceID != entityData.lightInstanceID)
                    m_LightsToEntityItem[dataToUpdate.lightInstanceID] = dataToUpdate;
            }
        }

        //Must be called at the destruction of the rendering pipeline to delete all the internal buffers.
        public void Cleanup()
        {
            m_DefaultLightEntity = HDLightRenderEntity.Invalid;
            HDUtils.s_DefaultHDAdditionalLightData.DestroyHDLightRenderEntity();

            var datasToDestroy = new List<HDAdditionalLightData>();
            for (int i = 0; i < m_LightCount; ++i)
            {
                if (m_AutoDestroy[i] && m_HDAdditionalLightData[i] != null)
                    datasToDestroy.Add(m_HDAdditionalLightData[i]);
            }

            foreach (var d in datasToDestroy)
                d.DestroyHDLightRenderEntity();
        }

        // Gets a default entity. This is mostly used to be compatible with things like shuriken lights, which lack a proper HDAdditionalLightData
        public HDLightRenderEntity GetDefaultLightEntity()
        {
            if (!IsValid(m_DefaultLightEntity))
            {
                HDUtils.s_DefaultHDAdditionalLightData.CreateHDLightRenderEntity(autoDestroy: true);
                m_DefaultLightEntity = HDUtils.s_DefaultHDAdditionalLightData.lightEntity;
            }

            return m_DefaultLightEntity;
        }

        // Returns true / false wether the entity has been destroyed or not.
        public bool IsValid(HDLightRenderEntity entity)
        {
            return entity.valid && entity.entityIndex < m_LightEntities.Count;
        }

        // Returns the index in data of an entity. Use this index to access lightData.
        // If the entity is invalid, it returns InvalidDataIndex
        public int GetEntityDataIndex(HDLightRenderEntity entity) => GetEntityData(entity).dataIndex;

        // Returns the index in data of an entity. Use this index to access lightData.
        // If the entity is invalid, it returns InvalidDataIndex
        public int FindEntityDataIndex(in VisibleLight visibleLight) => FindEntityDataIndex(visibleLight.light);

        // Returns the index in data of an entity. Use this index to access lightData.
        // If the entity is invalid, it returns InvalidDataIndex
        public int FindEntityDataIndex(in Light light)
        {
            if (light != null && m_LightsToEntityItem.TryGetValue(light.GetInstanceID(), out var foundEntity))
                return foundEntity.dataIndex;

            return -1;
        }

        public HDShadowRequestSetHandle GetShadowRequestSetHandle(HDLightRenderEntity entity)
        {
            return m_ShadowRequestSetHandles[entity.entityIndex];
        }

        public unsafe void AllocateHDShadowRequests(HDLightRenderEntity entity)
        {
            if (!m_ShadowRequestSetHandles[entity.entityIndex].valid)
            {
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
                for (int i = oldFreeIndex * HDShadowRequest.maxLightShadowRequestsCount; i < oldFreeIndex * HDShadowRequest.maxLightShadowRequestsCount + HDShadowRequest.maxLightShadowRequestsCount; i++)
                {
                    requests[i].InitDefault();
                }

                ref UnsafeList<int> requestIndices = ref UnsafeUtility.AsRef<UnsafeList<int>>(m_HDShadowRequestIndicesStorage.GetUnsafeList());
                m_HDShadowRequestFreeListIndex = requestIndices[oldFreeIndex * HDShadowRequest.maxLightShadowRequestsCount];
                requestIndices[oldFreeIndex * HDShadowRequest.maxLightShadowRequestsCount] = default;

                m_ShadowRequestSetHandles[entity.entityIndex] = new HDShadowRequestSetHandle { relativeDataOffset = oldFreeIndex };
                m_ShadowRequestSetPackedHandles[GetEntityData(entity).dataIndex] = new HDShadowRequestSetHandle { relativeDataOffset = oldFreeIndex };
                ++m_HDShadowRequestCount;
            }
        }

        public unsafe void FreeHDShadowRequests(HDLightRenderEntity lightEntity)
        {
            HDShadowRequestSetHandle shadowRequestSetHandle = m_ShadowRequestSetHandles[lightEntity.entityIndex];

            if (shadowRequestSetHandle.valid)
            {
                ref UnsafeList<HDShadowRequest> requests = ref UnsafeUtility.AsRef<UnsafeList<HDShadowRequest>>(m_HDShadowRequestStorage.GetUnsafeList());
                int sizeOfRequests = UnsafeUtility.SizeOf<HDShadowRequest>();
                int requestsByteCount = sizeOfRequests * HDShadowRequest.maxLightShadowRequestsCount;
                UnsafeUtility.MemClear(requests.Ptr + shadowRequestSetHandle.storageIndexForShadowRequests, requestsByteCount);

                ref UnsafeList<Vector4> planes = ref UnsafeUtility.AsRef<UnsafeList<Vector4>>(m_FrustumPlanesStorage.GetUnsafeList());
                int sizeOfPlane = UnsafeUtility.SizeOf<Vector4>();
                int planesByteCount = sizeOfPlane * HDShadowRequest.frustumPlanesCount * HDShadowRequest.maxLightShadowRequestsCount;
                UnsafeUtility.MemClear(planes.Ptr + shadowRequestSetHandle.storageIndexForFrustumPlanes, planesByteCount);

                ref UnsafeList<int> requestIndices = ref UnsafeUtility.AsRef<UnsafeList<int>>(m_HDShadowRequestIndicesStorage.GetUnsafeList());
                int sizeOfIndices = sizeof(int);
                int indicesByteCount = sizeOfIndices * HDShadowRequest.maxLightShadowRequestsCount;
                UnsafeUtility.MemClear(requestIndices.Ptr + shadowRequestSetHandle.storageIndexForRequestIndices, indicesByteCount);

                ref UnsafeList<Vector3> cachedViewPositions = ref UnsafeUtility.AsRef<UnsafeList<Vector3>>(m_CachedViewPositionsStorage.GetUnsafeList());
                int sizeOfViewPositions = UnsafeUtility.SizeOf<Vector3>();
                int viewPositionsByteCount = sizeOfViewPositions * HDShadowRequest.maxLightShadowRequestsCount;
                UnsafeUtility.MemClear(cachedViewPositions.Ptr + shadowRequestSetHandle.storageIndexForCachedViewPositions, viewPositionsByteCount);

                requestIndices[shadowRequestSetHandle.storageIndexForCachedViewPositions] = m_HDShadowRequestFreeListIndex;
                m_HDShadowRequestFreeListIndex = shadowRequestSetHandle.relativeDataOffset;

                m_ShadowRequestSetHandles[lightEntity.entityIndex] = new HDShadowRequestSetHandle {relativeDataOffset = HDShadowRequestSetHandle.InvalidIndex};
                m_ShadowRequestSetPackedHandles[GetEntityData(lightEntity).dataIndex] = new HDShadowRequestSetHandle {relativeDataOffset = HDShadowRequestSetHandle.InvalidIndex};
                --m_HDShadowRequestCount;
            }
        }

        #endregion

        #region private definitions

        // Intermediate struct which holds the data index of an entity and other information.
        private struct LightEntityInfo
        {
            public int dataIndex;
            public int lightInstanceID;
            public static readonly LightEntityInfo Invalid = new LightEntityInfo() { dataIndex = InvalidDataIndex, lightInstanceID = -1 };
            public bool valid { get { return dataIndex != -1 && lightInstanceID != -1; } }
        }

        private const int ArrayCapacity = 100;
        private static HDLightRenderDatabase s_Instance = null;

        private int m_Capacity = 0;
        private int m_LightCount = 0;
        private int m_AttachedGameObjects = 0;
        private HDLightRenderEntity m_DefaultLightEntity = HDLightRenderEntity.Invalid;

        private List<LightEntityInfo> m_LightEntities = new List<LightEntityInfo>();
        private NativeList<HDShadowRequestSetHandle> m_ShadowRequestSetHandles = new NativeList<HDShadowRequestSetHandle>(Allocator.Persistent);
        private NativeList<HDShadowRequestSetHandle> m_ShadowRequestSetPackedHandles = new NativeList<HDShadowRequestSetHandle>(Allocator.Persistent);
        private Queue<int> m_FreeIndices = new Queue<int>();
        private Dictionary<int, LightEntityInfo> m_LightsToEntityItem = new Dictionary<int, LightEntityInfo>();

        private NativeArray<HDLightRenderData> m_LightData;
        private NativeArray<HDLightRenderEntity> m_OwnerEntity;
        private NativeArray<bool> m_AutoDestroy;
        private NativeArray<HDAdditionalLightDataUpdateInfo> m_AdditionalLightDataUpdateInfos;

        //TODO: Hack array just used for shadow allocation. Need to refactor this so we dont depend on hdAdditionalData
        private DynamicArray<GameObject> m_AOVGameObjects = new DynamicArray<GameObject>();
        private DynamicArray<HDAdditionalLightData> m_HDAdditionalLightData = new DynamicArray<HDAdditionalLightData>();

        // TODO: Pad each set of HDShadowRequests to eliminate cache line sharing between sets, as a prerequisite for parallel shadow request update work.
        private int m_HDShadowRequestFreeListIndex;
        private int m_HDShadowRequestCount;
        private int m_HDShadowRequestCapacity;
        private NativeList<HDShadowRequest> m_HDShadowRequestStorage = new NativeList<HDShadowRequest>(Allocator.Persistent);
        private NativeList<int> m_HDShadowRequestIndicesStorage = new NativeList<int>(Allocator.Persistent);
        private NativeList<Vector4> m_FrustumPlanesStorage = new NativeList<Vector4>(Allocator.Persistent);
        private NativeList<Vector3> m_CachedViewPositionsStorage = new NativeList<Vector3>(Allocator.Persistent);

        private NativeArray<Matrix4x4> m_CachedCubeMapFaces = new NativeArray<Matrix4x4>(HDShadowUtils.kCubemapFaces, Allocator.Persistent);

        private void ResizeArrays()
        {
            m_HDAdditionalLightData.Resize(m_Capacity, true);
            m_AOVGameObjects.Resize(m_Capacity, true);

            m_LightData.ResizeArray(m_Capacity);
            m_OwnerEntity.ResizeArray(m_Capacity);
            m_AutoDestroy.ResizeArray(m_Capacity);
            m_AdditionalLightDataUpdateInfos.ResizeArray(m_Capacity);
        }

        private unsafe void RemoveAtSwapBackArrays(int removeIndexAt)
        {
            int lastIndex = m_LightCount - 1;
            m_HDAdditionalLightData[removeIndexAt] = m_HDAdditionalLightData[lastIndex];
            m_HDAdditionalLightData[lastIndex] = null;

            m_AOVGameObjects[removeIndexAt] = m_AOVGameObjects[lastIndex];
            m_AOVGameObjects[lastIndex] = null;

            m_LightData[removeIndexAt] = m_LightData[lastIndex];
            m_OwnerEntity[removeIndexAt] = m_OwnerEntity[lastIndex];
            m_AutoDestroy[removeIndexAt] = m_AutoDestroy[lastIndex];
            m_AdditionalLightDataUpdateInfos[removeIndexAt] = m_AdditionalLightDataUpdateInfos[lastIndex];
            m_ShadowRequestSetPackedHandles[removeIndexAt] = m_ShadowRequestSetPackedHandles[lastIndex];
            m_ShadowRequestSetPackedHandles[lastIndex] = new HDShadowRequestSetHandle {relativeDataOffset = HDShadowRequestSetHandle.InvalidIndex};

            --m_LightCount;
        }

        private void DeleteArrays()
        {
            if (m_Capacity == 0)
                return;

            m_HDAdditionalLightData.Clear();
            m_AOVGameObjects.Clear();
            m_LightData.Dispose();
            m_OwnerEntity.Dispose();
            m_AutoDestroy.Dispose();
            m_AdditionalLightDataUpdateInfos.Dispose();

            m_FreeIndices.Clear();
            m_LightEntities.Clear();
            m_ShadowRequestSetHandles.Dispose();
            m_ShadowRequestSetHandles = default;
            m_ShadowRequestSetPackedHandles.Dispose();
            m_ShadowRequestSetPackedHandles = default;

            m_HDShadowRequestStorage.Dispose();
            m_HDShadowRequestStorage = default;
            m_HDShadowRequestIndicesStorage.Dispose();
            m_HDShadowRequestIndicesStorage = default;
            m_FrustumPlanesStorage.Dispose();
            m_FrustumPlanesStorage = default;

            for (int i = 0; i < shadowRequestDataLists.Length; i++)
            {
                shadowRequestDataLists[i].Dispose();
                shadowRequestDataLists[i] = default;
            }

            m_CachedCubeMapFaces.Dispose();

            m_Capacity = 0;
        }

        private LightEntityInfo GetEntityData(HDLightRenderEntity entity)
        {
            Assert.IsTrue(IsValid(entity));
            return m_LightEntities[entity.entityIndex];
        }

        private LightEntityInfo AllocateEntityData()
        {
            if (m_Capacity == 0 || m_LightCount == m_Capacity)
            {
                m_Capacity = Math.Max(Math.Max(m_Capacity * 2, m_LightCount), ArrayCapacity);
                ResizeArrays();
            }

            int newIndex = m_LightCount++;
            LightEntityInfo newDataIndex = new LightEntityInfo { dataIndex = newIndex, lightInstanceID = -1 };
            return newDataIndex;
        }

        ~HDLightRenderDatabase()
        {
            DeleteArrays();
        }

        #endregion
    }
}
