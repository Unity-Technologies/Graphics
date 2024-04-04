using System;
using UnityEngine.Jobs;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

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
        public uint renderingLayerMask;
        public float fadeDistance;
        public float distance;
        public float angularDiameter;
        public float volumetricFadeDistance;
        public bool includeForRayTracing;
        public bool includeForPathTracing;
        public bool useScreenSpaceShadows;
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
        public bool affectVolumetric;
        public bool affectDiffuse;
        public bool affectSpecular;
        public bool applyRangeAttenuation;
        public bool penumbraTint;
        public bool interactsWithSky;
        public Color shadowTint;
    }

    internal struct HDAdditionalLightDataUpdateInfo
    {
        const int ShadowUpdateModeTypeDataIndex = 0;

        const int UseCustomSpotLightShadowConeFlagsIndex = 0;
        const int AlwaysDrawDynamicShadowsFlagsIndex = 1;
        const int UpdateUponLightMovementFlagsIndex = 2;

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
        public float cachedShadowTranslationUpdateThreshold;
        public float cachedShadowAngleUpdateThreshold;
        public float dirLightPCSSMaxPenumbraSize;
        public float dirLightPCSSMaxSamplingDistance;
        public float dirLightPCSSMinFilterSizeTexels;
        public float dirLightPCSSMinFilterMaxAngularDiameter;
        public float dirLightPCSSBlockerSearchAngularDiameter;
        public float dirLightPCSSBlockerSamplingClumpExponent;
        public int lightIdxForCachedShadows;
        public byte dirLightPCSSBlockerSampleCount;
        public byte dirLightPCSSFilterSampleCount;
        public byte filterSampleCount;
        public byte blockerSampleCount;
        public byte kernelSize;
        public byte evsmBlurPasses;
        public BitArray8 flags;
        public byte typeData;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTypeData(byte typeIndex, byte value)
        {
            typeData = (byte)((typeData & ~(0b11 << (typeIndex * 2))) | (value << (typeIndex * 2)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetTypeData(byte typeIndex)
        {
            return (byte)((typeData >> (typeIndex * 2)) & 0b11);
        }

        public ShadowUpdateMode shadowUpdateMode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (ShadowUpdateMode)GetTypeData(ShadowUpdateModeTypeDataIndex);
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => SetTypeData(ShadowUpdateModeTypeDataIndex, (byte)value);
        }

        public bool useCustomSpotLightShadowCone
        {
            get => flags[UseCustomSpotLightShadowConeFlagsIndex];
            set => flags[UseCustomSpotLightShadowConeFlagsIndex] = value;
        }

        public bool alwaysDrawDynamicShadows
        {
            get => flags[AlwaysDrawDynamicShadowsFlagsIndex];
            set => flags[AlwaysDrawDynamicShadowsFlagsIndex] = value;
        }

        public bool updateUponLightMovement
        {
            get => flags[UpdateUponLightMovementFlagsIndex];
            set => flags[UpdateUponLightMovementFlagsIndex] = value;
        }

        public bool hasCachedComponent => shadowUpdateMode != ShadowUpdateMode.EveryFrame;

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
            cachedShadowTranslationUpdateThreshold = additionalLightData.cachedShadowTranslationUpdateThreshold;
            cachedShadowAngleUpdateThreshold = additionalLightData.cachedShadowAngleUpdateThreshold;
            dirLightPCSSMaxPenumbraSize = additionalLightData.dirLightPCSSMaxPenumbraSize;
            dirLightPCSSMaxSamplingDistance = additionalLightData.dirLightPCSSMaxSamplingDistance;
            dirLightPCSSMinFilterSizeTexels = additionalLightData.dirLightPCSSMinFilterSizeTexels;
            dirLightPCSSMinFilterMaxAngularDiameter = additionalLightData.dirLightPCSSMinFilterMaxAngularDiameter;
            dirLightPCSSBlockerSearchAngularDiameter = additionalLightData.dirLightPCSSBlockerSearchAngularDiameter;
            dirLightPCSSBlockerSamplingClumpExponent = additionalLightData.dirLightPCSSBlockerSamplingClumpExponent;
            dirLightPCSSBlockerSampleCount = (byte)additionalLightData.dirLightPCSSBlockerSampleCount;
            dirLightPCSSFilterSampleCount = (byte)additionalLightData.dirLightPCSSFilterSampleCount;
            blockerSampleCount = (byte)additionalLightData.blockerSampleCount;
            filterSampleCount = (byte)additionalLightData.filterSampleCount;
            kernelSize = (byte)additionalLightData.kernelSize;
            evsmBlurPasses = (byte)additionalLightData.evsmBlurPasses;
            lightIdxForCachedShadows = additionalLightData.lightIdxForCachedShadows;
            shadowUpdateMode = additionalLightData.shadowUpdateMode;
            useCustomSpotLightShadowCone = additionalLightData.useCustomSpotLightShadowCone;
            alwaysDrawDynamicShadows = additionalLightData.alwaysDrawDynamicShadows;
            updateUponLightMovement = additionalLightData.updateUponLightMovement;
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

        public NativeList<HDShadowRequestSetHandle> packedShadowRequestSetHandles => m_ShadowRequestSetPackedHandles;

        public NativeArray<HDAdditionalLightDataUpdateInfo> additionalLightDataUpdateInfos => m_AdditionalLightDataUpdateInfos;

        public int validCustomViewCallbackEvents => m_ValidCustomViewCallbackEvents;

        public DynamicArray<SpotLightCallbackData> customViewCallbackEvents => m_CustomViewCallbackEvents;

        public HDShadowRequestDatabase shadowRequests => HDShadowRequestDatabase.instance;

        // This array tracks directional lights for the PBR sky
        // We need this as VisibleLight result from culling ignores lights with intensity == 0
        public List<HDAdditionalLightData> directionalLights = new();

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

        public ref HDAdditionalLightDataUpdateInfo EditAdditionalLightUpdateDataAsRef(in HDLightRenderEntity entity)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetShapeWidth(in HDLightRenderEntity entity, float shapeWidth)
        {
            if (!entity.valid)
                return;

            EditLightDataAsRef(entity).shapeWidth = shapeWidth;
            EditAdditionalLightUpdateDataAsRef(entity).shapeWidth = shapeWidth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetShapeHeight(in HDLightRenderEntity entity, float shapeHeight)
        {
            if (!entity.valid)
                return;

            EditLightDataAsRef(entity).shapeHeight = shapeHeight;
            EditAdditionalLightUpdateDataAsRef(entity).shapeHeight = shapeHeight;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetShapeRadius(in HDLightRenderEntity entity, float shapeRadius)
        {
            if (!entity.valid)
                return;

            EditLightDataAsRef(entity).shapeRadius = shapeRadius;
            EditAdditionalLightUpdateDataAsRef(entity).shapeRadius = shapeRadius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAspectRatio(in HDLightRenderEntity entity, float aspectRatio)
        {
            if (!entity.valid)
                return;

            EditLightDataAsRef(entity).aspectRatio = aspectRatio;
            EditAdditionalLightUpdateDataAsRef(entity).aspectRatio = aspectRatio;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAngularDiameter(in HDLightRenderEntity entity, float angularDiameter)
        {
            if (!entity.valid)
                return;

            EditLightDataAsRef(entity).angularDiameter = angularDiameter;
            EditAdditionalLightUpdateDataAsRef(entity).angularDiameter = angularDiameter;
        }

        public void SetCustomCallback(in HDLightRenderEntity entity, HDAdditionalLightData.CustomViewCallback callback)
        {
            int dataIndex = m_LightEntities[entity.entityIndex].dataIndex;
            if ( dataIndex >= m_LightCount)
                throw new Exception("Entity passed in is out of bounds. Index requested " + dataIndex + " and maximum length is " + m_LightCount);
            ref var currentCallback = ref m_CustomViewCallbackEvents[dataIndex];
            bool wasSet = currentCallback.isAnythingRegistered;
            bool isSet = callback != null;

            currentCallback.callback = callback;
            currentCallback.isAnythingRegistered = isSet;

            if (wasSet != isSet)
            {
                m_ValidCustomViewCallbackEvents += isSet ? 1 : -1;
            }
        }

        // Creates a light render entity.
        public HDLightRenderEntity CreateEntity(bool autoDestroy)
        {
            if (!m_LightEntities.IsCreated)
            {
                m_LightEntities = new NativeList<LightEntityInfo>(Allocator.Persistent);
            }

            LightEntityInfo newData = AllocateEntityData();

            HDLightRenderEntity newLightEntity = HDLightRenderEntity.Invalid;
            if (m_FreeIndices.Count == 0)
            {
                newLightEntity.entityIndex = m_LightEntities.Length;
                m_LightEntities.Add(newData);

                EnsureNativeListsAreCreated();

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

        private void EnsureNativeListsAreCreated()
        {
            if (!m_ShadowRequestSetHandles.IsCreated)
                m_ShadowRequestSetHandles = new NativeList<HDShadowRequestSetHandle>(Allocator.Persistent);
            if (!m_ShadowRequestSetPackedHandles.IsCreated)
                m_ShadowRequestSetPackedHandles = new NativeList<HDShadowRequestSetHandle>(Allocator.Persistent);
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
            m_CustomViewCallbackEvents[dataIndex] = new SpotLightCallbackData() { callback = additionalLightData.CustomViewCallbackEvent, isAnythingRegistered = additionalLightData.CustomViewCallbackEvent != null};
            if (additionalLightData.CustomViewCallbackEvent != null)
                ++m_ValidCustomViewCallbackEvents;
            ++m_AttachedGameObjects;

            if (additionalLightData.legacyLight.type == LightType.Directional
#if UNITY_EDITOR
                 && !UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(additionalLightData.gameObject.scene)
#endif
                )
                directionalLights.Add(additionalLightData);
        }

        // Destroys a light render entity.
        public unsafe void DestroyEntity(HDLightRenderEntity lightEntity)
        {
            if (!lightEntity.valid)
                return;

            m_FreeIndices.Enqueue(lightEntity.entityIndex);
            LightEntityInfo entityData = m_LightEntities[lightEntity.entityIndex];
            m_LightsToEntityItem.Remove(entityData.lightInstanceID);

            var lightData = m_HDAdditionalLightData[entityData.dataIndex];
            if (lightData != null)
            {
                int idx = directionalLights.FindIndex((x) => ReferenceEquals(x, lightData));
                if (idx != -1) directionalLights.RemoveAt(idx);

                --m_AttachedGameObjects;
            }

            FreeHDShadowRequests(lightEntity);

            RemoveAtSwapBackArrays(entityData.dataIndex);

            if (m_LightCount == 0)
            {
                DeleteArrays();
                HDShadowRequestDatabase.instance.DeleteArrays();
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

        public void AllocateHDShadowRequests(HDLightRenderEntity entity)
        {
            if (!entity.valid)
                return;

            HDShadowRequestSetHandle shadowSethandle = m_ShadowRequestSetHandles[entity.entityIndex];
            if (!shadowSethandle.valid)
            {
                shadowSethandle = HDShadowRequestDatabase.instance.AllocateHDShadowRequests();
                m_ShadowRequestSetHandles[entity.entityIndex] = shadowSethandle;
                m_ShadowRequestSetPackedHandles[GetEntityData(entity).dataIndex] = shadowSethandle;
            }
        }

        public void FreeHDShadowRequests(HDLightRenderEntity lightEntity)
        {
            if (!lightEntity.valid)
                return;

            HDShadowRequestSetHandle shadowRequestSetHandle = m_ShadowRequestSetHandles[lightEntity.entityIndex];
            HDShadowRequestDatabase.instance.FreeHDShadowRequests(ref shadowRequestSetHandle);
            m_ShadowRequestSetHandles[lightEntity.entityIndex] = shadowRequestSetHandle;
            m_ShadowRequestSetPackedHandles[GetEntityData(lightEntity).dataIndex] = shadowRequestSetHandle;
        }

        public HDShadowRequestSetHandle GetShadowRequestSetHandle(HDLightRenderEntity entity)
        {
            return m_ShadowRequestSetHandles[entity.entityIndex];
        }

        [BurstCompile]
        private struct GetDataIndicesFromHDLightRenderEntitiesArrayJob : IJob
        {
            [ReadOnly] public NativeArray<HDLightRenderEntity> lightEntityLookups;
            [ReadOnly] public NativeArray<LightEntityInfo> lightEntityStorage;
            [WriteOnly] public NativeList<int> dataIndices;

            public void Execute()
            {
                for (int i = 0; i < lightEntityLookups.Length; i++)
                {
                    LightEntityInfo entityInfo = lightEntityStorage[lightEntityLookups[i].entityIndex];
                    dataIndices.Add(entityInfo.dataIndex);
                }
            }
        }

        [BurstCompile]
        private struct GetDataIndicesFromHDLightRenderEntitiesHashmapJob : IJob
        {
            [ReadOnly] public NativeParallelHashMap<int, HDLightRenderEntity> lightEntityLookups;
            [ReadOnly] public NativeArray<LightEntityInfo> lightEntityStorage;
            [WriteOnly] public NativeList<int> dataIndices;

            public void Execute()
            {
                foreach (var kvp in lightEntityLookups)
                {
                    LightEntityInfo entityInfo = lightEntityStorage[kvp.Value.entityIndex];
                    dataIndices.Add(entityInfo.dataIndex);
                }
            }
        }

        public void GetDataIndicesFromEntities(NativeArray<HDLightRenderEntity> inLightEntities, NativeList<int> outDataIndices)
        {
            GetDataIndicesFromHDLightRenderEntitiesArrayJob getDataIndicesJob = new GetDataIndicesFromHDLightRenderEntitiesArrayJob()
            {
                lightEntityLookups = inLightEntities,
                lightEntityStorage = m_LightEntities.AsArray(),
                dataIndices = outDataIndices
            };

            getDataIndicesJob.Run();
        }

        public void GetDataIndicesFromEntities(NativeParallelHashMap<int, HDLightRenderEntity> inLightEntities, NativeList<int> outDataIndices)
        {
            GetDataIndicesFromHDLightRenderEntitiesHashmapJob getDataIndicesJob = new GetDataIndicesFromHDLightRenderEntitiesHashmapJob()
            {
                lightEntityLookups = inLightEntities,
                lightEntityStorage = m_LightEntities.AsArray(),
                dataIndices = outDataIndices
            };

            getDataIndicesJob.Run();
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
            return entity.valid && m_LightEntities.IsCreated && entity.entityIndex < m_LightEntities.Length;
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

        internal struct SpotLightCallbackData
        {
            public HDAdditionalLightData.CustomViewCallback callback;
            public bool isAnythingRegistered;
        }

        private const int ArrayCapacity = 100;
        private static HDLightRenderDatabase s_Instance = null;

        private int m_Capacity = 0;
        private int m_LightCount = 0;
        private int m_AttachedGameObjects = 0;
        private HDLightRenderEntity m_DefaultLightEntity = HDLightRenderEntity.Invalid;

        private NativeList<LightEntityInfo> m_LightEntities;

        // Technically only used for spot lights. Not good for perf, would like to deprecate this whenever possible.
        private DynamicArray<SpotLightCallbackData> m_CustomViewCallbackEvents = new DynamicArray<SpotLightCallbackData>();
        private int m_ValidCustomViewCallbackEvents;
        private NativeList<HDShadowRequestSetHandle> m_ShadowRequestSetHandles;
        private NativeList<HDShadowRequestSetHandle> m_ShadowRequestSetPackedHandles;

        private Queue<int> m_FreeIndices = new Queue<int>();
        private Dictionary<int, LightEntityInfo> m_LightsToEntityItem = new Dictionary<int, LightEntityInfo>();

        private NativeArray<HDLightRenderData> m_LightData;
        private NativeArray<HDLightRenderEntity> m_OwnerEntity;
        private NativeArray<bool> m_AutoDestroy;
        private NativeArray<HDAdditionalLightDataUpdateInfo> m_AdditionalLightDataUpdateInfos;

        //TODO: Hack array just used for shadow allocation. Need to refactor this so we dont depend on hdAdditionalData
        private DynamicArray<GameObject> m_AOVGameObjects = new DynamicArray<GameObject>();
        private DynamicArray<HDAdditionalLightData> m_HDAdditionalLightData = new DynamicArray<HDAdditionalLightData>();

        private void ResizeArrays()
        {
            m_HDAdditionalLightData.Resize(m_Capacity, true);
            m_AOVGameObjects.Resize(m_Capacity, true);
            m_CustomViewCallbackEvents.Resize(m_Capacity, true);

            m_LightData.ResizeArray(m_Capacity);
            m_OwnerEntity.ResizeArray(m_Capacity);
            m_AutoDestroy.ResizeArray(m_Capacity);
            m_AdditionalLightDataUpdateInfos.ResizeArray(m_Capacity);
        }

        private void RemoveAtSwapBackArrays(int removeIndexAt)
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
            m_CustomViewCallbackEvents[removeIndexAt] = m_CustomViewCallbackEvents[lastIndex];
            m_CustomViewCallbackEvents[lastIndex] = default;

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
            m_LightEntities.Dispose();
            m_LightEntities = default;
            m_ShadowRequestSetHandles.Dispose();
            m_ShadowRequestSetHandles = default;
            m_ShadowRequestSetPackedHandles.Dispose();
            m_ShadowRequestSetPackedHandles = default;

            m_ValidCustomViewCallbackEvents = 0;
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
