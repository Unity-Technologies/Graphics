using System.Collections.Generic;
using System.IO;
using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.LightTransport;
using UnityEngine.PathTracing.Integration;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using UnityEngine.PathTracing.Core;
using UnityEngine.PathTracing.Lightmapping;
using UnityEngine.Rendering.UnifiedRayTracing;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using UnityEditor.PathTracing.Debugging;

namespace UnityEditor.PathTracing.LightBakerBridge
{
    using static BakeLightmapDriver;
    using InstanceHandle = Handle<World.InstanceKey>;

    internal static class WorldHelpers
    {
        static void MultiValueDictAdd<TKey, TValue>(Dictionary<TKey, List<TValue>> dict, TKey key, TValue value)
        {
            List<TValue> values = null;
            if (dict.TryGetValue(key, out values))
            {
                values.Add(value);
            }
            else
            {
                values = new List<TValue> { value };
                dict.Add(key, values);
            }
        }

        internal static void AddContributingInstancesToWorld(World world, in FatInstance[] fatInstances, out Dictionary<int, List<LodInstanceBuildData>> lodInstances, out Dictionary<Int32, List<ContributorLodInfo>> lodgroupToContributorInstances)
        {
            lodInstances = new();
            lodgroupToContributorInstances = new();
            // Add instances to world
            foreach (var fatInstance in fatInstances)
            {
                if (fatInstance.LodIdentifier.IsValid() && !fatInstance.LodIdentifier.IsContributor())
                {
                    WorldHelpers.MultiValueDictAdd(lodInstances, fatInstance.LodIdentifier.LodGroup, new LodInstanceBuildData
                    {
                        LodMask = fatInstance.LodIdentifier.LodMask,
                        Mesh = fatInstance.Mesh,
                        Materials = fatInstance.Materials,
                        Masks = fatInstance.SubMeshMasks,
                        LocalToWorldMatrix = fatInstance.LocalToWorldMatrix,
                        Bounds = fatInstance.Bounds,
                        IsStatic = fatInstance.IsStatic,
                        Filter = fatInstance.Filter
                    });
                    continue;
                }

                var instanceHandle = world.AddInstance(
                    fatInstance.Mesh,
                    fatInstance.Materials,
                    fatInstance.SubMeshMasks,
                    fatInstance.RenderingObjectLayer,
                    fatInstance.LocalToWorldMatrix,
                    fatInstance.Bounds,
                    fatInstance.IsStatic,
                    fatInstance.Filter,
                    fatInstance.EnableEmissiveSampling);

                if (fatInstance.LodIdentifier.IsValid() && fatInstance.LodIdentifier.IsContributor())
                    WorldHelpers.MultiValueDictAdd(lodgroupToContributorInstances, fatInstance.LodIdentifier.LodGroup, new ContributorLodInfo
                    {
                        LodMask = fatInstance.LodIdentifier.LodMask,
                        InstanceHandle = instanceHandle,
                        Masks = fatInstance.SubMeshMasks
                    });
            }
        }
    }

    [InitializeOnLoad]
    internal class SetLightmappingUnifiedBaker
    {
        static SetLightmappingUnifiedBaker()
        {
            try
            {
                var lightmappingType = typeof(UnityEditor.Lightmapping);
                var unifiedBakerProperty = lightmappingType.GetProperty("UnifiedBaker",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                if (unifiedBakerProperty != null && unifiedBakerProperty.CanWrite)
                {
    #if UNIFIED_BAKER
                    unifiedBakerProperty.SetValue(null, true);
    #else
                    unifiedBakerProperty.SetValue(null, false);
    #endif
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Could not find or access UnifiedBaker property on Lightmapping class");
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to set UnifiedBaker property via reflection: {ex.Message}");
            }
        }
    }

    internal class LightBakerStrangler
    {
        internal enum Result
        {
            Success,
            InitializeFailure,
            CreateDirectoryFailure,
            CreateLightmapFailure,
            AddResourcesToCacheFailure,
            InitializeExpandedBufferFailure,
            WriteToDiskFailure,
            Cancelled
        };

        // File layout matches WriteInterleavedSHArrayToFile.
        private static bool SaveProbesToFileInterleaved(string filename, NativeArray<SphericalHarmonicsL2> shArray)
        {
            Debug.Assert(filename is not null, "Filename is null");

            string path = Path.GetDirectoryName(filename);
            Debug.Assert(path is not null, "path is null");
            var info = Directory.CreateDirectory(path);
            if (info.Exists == false)
                return false;

            // Write the number of probes to file as Int64.
            Int64 arraySize = shArray.Length;
            byte[] probeCountBytes = BitConverter.GetBytes(arraySize);
            List<byte> byteList = new();
            byteList.AddRange(probeCountBytes);

            // Write all probe coefficients, ordered by coefficient.
            const int sphericalHarmonicsL2CoeffCount = 9;
            byte[] floatPaddingBytes = BitConverter.GetBytes(0.0f);
            for (int coefficient = 0; coefficient < sphericalHarmonicsL2CoeffCount; ++coefficient)
            {
                for (int i = 0; i < shArray.Length; i++)
                {
                    SphericalHarmonicsL2 sh = shArray[i];
                    for (int rgb = 0; rgb < 3; rgb++)
                    {
                        float coefficientValue = sh[rgb, coefficient];
                        byte[] floatBytes = BitConverter.GetBytes(coefficientValue);
                        byteList.AddRange(floatBytes);
                    }
                    byteList.AddRange(floatPaddingBytes); // pad to match Vector4 size.
                }
            }
            File.WriteAllBytes(filename, byteList.ToArray());
            return true;
        }

        private static bool SaveArrayToFile<T>(string filename, int count, T[] array)
            where T : unmanaged
        {
            // Create output directory if it doesn't exist already.
            Debug.Assert(filename is not null, "Filename is null");
            string path = Path.GetDirectoryName(filename);
            Debug.Assert(path is not null, "path is null");
            var info = Directory.CreateDirectory(path);
            if (info.Exists == false)
                return false;

            // Prepare output buffer.
            int numElementBytes = array.Length * UnsafeUtility.SizeOf<T>();
            byte[] bytes = new byte[sizeof(Int64) + numElementBytes]; // + sizeof(Int64) for the count.

            // Write the number of elements to file as Int64.
            Int64 arraySize = count;
            byte[] countBytes = BitConverter.GetBytes(arraySize);
            Buffer.BlockCopy(countBytes, 0, bytes, 0, countBytes.Length);

            // Write contents of array to file. This is safe because of the unmanaged constraint on T.
            unsafe
            {
                fixed (T* arrayPtr = array)
                fixed (byte* bytesPtr = bytes)
                {
                    UnsafeUtility.MemCpy(bytesPtr + sizeof(Int64), arrayPtr, numElementBytes);
                }
            }
            File.WriteAllBytes(filename, bytes);
            return true;
        }

        private static HashSet<LightmapRequestOutputType> BitfieldToList(int bitfield)
        {
            var outputTypes = new HashSet<LightmapRequestOutputType>();

            // Loop through the enum values
            foreach (LightmapRequestOutputType channel in Enum.GetValues(typeof(LightmapRequestOutputType)))
            {
                // Check if the bitfield has the current value set
                if ((bitfield & (int) channel) == 0)
                    continue;
                // Add the value to the list
                if (channel != LightmapRequestOutputType.All)
                    outputTypes.Add(channel);
            }
            return outputTypes;
        }

        private static bool LightmapRequestOutputTypeToIntegratedOutputType(LightmapRequestOutputType type, out IntegratedOutputType integratedOutputType)
        {
            integratedOutputType = IntegratedOutputType.AO;
            switch (type)
            {
                case LightmapRequestOutputType.IrradianceIndirect:
                    integratedOutputType = IntegratedOutputType.Indirect;
                    return true;
                case LightmapRequestOutputType.IrradianceDirect:
                    integratedOutputType = IntegratedOutputType.Direct;
                    return true;
                case LightmapRequestOutputType.Occupancy: // Occupancy do not need accumulation
                    return false;
                case LightmapRequestOutputType.Validity:
                    integratedOutputType = IntegratedOutputType.Validity;
                    return true;
                case LightmapRequestOutputType.DirectionalityIndirect:
                    integratedOutputType = IntegratedOutputType.DirectionalityIndirect;
                    return true;
                case LightmapRequestOutputType.DirectionalityDirect:
                    integratedOutputType = IntegratedOutputType.DirectionalityDirect;
                    return true;
                case LightmapRequestOutputType.AmbientOcclusion:
                    integratedOutputType = IntegratedOutputType.AO;
                    return true;
                case LightmapRequestOutputType.Shadowmask:
                    integratedOutputType = IntegratedOutputType.ShadowMask;
                    return true;
                // These types do not need accumulation:
                case LightmapRequestOutputType.Normal:
                case LightmapRequestOutputType.ChartIndex:
                case LightmapRequestOutputType.OverlapPixelIndex:
                case LightmapRequestOutputType.IrradianceEnvironment:
                    return false;
            }
            Debug.Assert(false, $"Error unknown LightmapRequestOutputType {type}.");
            return false;
        }

        // We can ignore the G-Buffer data part of atlassing, we are using stochastic sampling instead.
        internal static LightmapDesc[] PopulateLightmapDescsFromAtlassing(in PVRAtlassingData atlassing, in FatInstance[] fatInstances)
        {
            int atlasCount = atlassing.m_AtlasHashToGBufferHash.Count;
            var lightmapDescs = new LightmapDesc[atlasCount];
            foreach (var atlasIdToAtlasHash in atlassing.m_AtlasIdToAtlasHash)
            {
                int atlasId = atlasIdToAtlasHash.m_AtlasId;

                // Get the array of object ID hashes for the given atlas.
                bool found = atlassing.m_AtlasHashToObjectIDHashes.TryGetValue(
                        atlasIdToAtlasHash.m_AtlasHash, out IndexHash128[] objectIDHashes);
                Debug.Assert(found, $"Couldn't find object ID hashes for atlas {atlasIdToAtlasHash.m_AtlasHash}.");

                // Get the GBuffer data for the given atlas, so we can use it to find instance data like IDs and transforms.
                found = atlassing.m_AtlasHashToGBufferHash.TryGetValue(
                    atlasIdToAtlasHash.m_AtlasHash, out _);
                if (found == false)
                    continue; // Skip atlasses without GBuffers, they are used for SSD objects.

                uint lightmapResolution = (uint)atlassing.m_AtlasSizes[atlasId].width;
                Debug.Assert(lightmapResolution == atlassing.m_AtlasSizes[atlasId].height,
                    "The following code assumes that we always have square lightmaps.");
                uint antiAliasingSampleCount = (uint)((int)atlasIdToAtlasHash.m_BakeParameters.supersamplingMultiplier * (int)atlasIdToAtlasHash.m_BakeParameters.supersamplingMultiplier);
                var lightmapDesc = new LightmapDesc
                {
                    Resolution = lightmapResolution,
                    PushOff = atlasIdToAtlasHash.m_BakeParameters.pushOff
                };
                int instanceCount = objectIDHashes.Length;
                var bakeInstances = new BakeInstance[instanceCount];
                int instanceCounter = 0;
                foreach (var instanceHash in objectIDHashes)
                {
                    // Get the AtlassedInstanceData for the instance.
                    found = atlassing.m_InstanceAtlassingData.TryGetValue(instanceHash,
                        out AtlassedInstanceData atlassedInstanceData);
                    Debug.Assert(found, $"Didn't find AtlassedInstanceData for instance {instanceHash}.");
                    Debug.Assert(atlassedInstanceData.m_AtlasId == atlasId, "Atlas ID mismatch.");

                    // Get the instance from bakeInput and create a Mesh.
                    uint bakeInputInstanceIndex = (uint)instanceHash.Index;
                    ref readonly FatInstance fatInstance = ref fatInstances[bakeInputInstanceIndex];
                    LightmapIntegrationHelpers.ComputeOccupiedTexelRegionForInstance(
                        lightmapResolution, lightmapResolution, atlassedInstanceData.m_LightmapST, fatInstance.UVBoundsSize, fatInstance.UVBoundsOffset,
                        out Vector4 normalizedOccupiedST, out Vector2Int occupiedTexelSize, out Vector2Int occupiedTexelOffset);
                    bakeInstances[instanceCounter].Build(
                        fatInstance.Mesh,
                        normalizedOccupiedST,
                        atlassedInstanceData.m_LightmapST,
                        occupiedTexelSize,
                        occupiedTexelOffset,
                        fatInstance.LocalToWorldMatrix,
                        fatInstance.ReceiveShadows,
                        fatInstance.LodIdentifier,
                        bakeInputInstanceIndex);
                    ++instanceCounter;
                }

                lightmapDesc.BakeInstances = bakeInstances;
                lightmapDescs[atlasId] = lightmapDesc;
            }

            return lightmapDescs;
        }

        private static void MakeOutputFolderPathsFullyQualified(ProbeRequest[] probeRequestsToFixUp, string bakeOutputFolderPath)
        {
            for (int i = 0; i < probeRequestsToFixUp.Length; i++)
            {
                ref ProbeRequest request = ref probeRequestsToFixUp[i];
                request.outputFolderPath = Path.GetFullPath(request.outputFolderPath, bakeOutputFolderPath);
            }
        }

        private static void MakeOutputFolderPathsFullyQualified(LightmapRequest[] requestsToFixUp, string bakeOutputFolderPath)
        {
            for (int i = 0; i < requestsToFixUp.Length; i++)
            {
                ref LightmapRequest request = ref requestsToFixUp[i];
                request.outputFolderPath = Path.GetFullPath(request.outputFolderPath, bakeOutputFolderPath);
            }
        }

        internal static bool Bake(string bakeInputPath, string lightmapRequestsPath, string lightProbeRequestsPath, string bakeOutputFolderPath, BakeProgressState progressState)
        {
            using (new BakeProfilingScope())
            {
                // Read BakeInput and requests from LightBaker
                if (!BakeInputSerialization.Deserialize(bakeInputPath, out BakeInput bakeInput))
                    return false;
                if (!BakeInputSerialization.Deserialize(lightmapRequestsPath, out LightmapRequestData lightmapRequestData))
                    return false;
                if (!BakeInputSerialization.Deserialize(lightProbeRequestsPath, out ProbeRequestData probeRequestData))
                    return false;

                // Change output folder paths from relative to absolute
                MakeOutputFolderPathsFullyQualified(probeRequestData.requests, bakeOutputFolderPath);
                MakeOutputFolderPathsFullyQualified(lightmapRequestData.requests, bakeOutputFolderPath);
                IntegrationSettings integrationSettings = GetIntegrationSettings(bakeInput);

                // Setup ray tracing context
                RayTracingBackend backend = integrationSettings.Backend;
                Debug.Assert(RayTracingContext.IsBackendSupported(backend), $"Backend {backend} is not supported!");
                RayTracingResources rayTracingResources = new RayTracingResources();
                rayTracingResources.Load();
                using var rayTracingContext = new RayTracingContext(backend, rayTracingResources);

                // Setup device context
                using UnityComputeDeviceContext deviceContext = new();
                bool initOk = deviceContext.Initialize();
                Assert.IsTrue(initOk, "Failed to initialize DeviceContext.");

                // Create and init world
                var worldResources = new WorldResourceSet();
                worldResources.LoadFromAssetDatabase();
                using UnityComputeWorld world = new();
                world.Init(rayTracingContext, worldResources);

                using var samplingResources = new UnityEngine.Rendering.Sampling.SamplingResources();
                samplingResources.Load((uint)UnityEngine.Rendering.Sampling.SamplingResources.ResourceType.All);

                // Deserialize BakeInput, inject data into world
                const bool useLegacyBakingBehavior = true;
                const bool autoEstimateLUTRange = true;
                BakeInputToWorldConversion.InjectBakeInputData(world.PathTracingWorld, autoEstimateLUTRange, in bakeInput,
                    out Bounds sceneBounds, out world.Meshes, out FatInstance[] fatInstances, out world.LightHandles,
                    world.TemporaryObjects, UnityComputeWorld.RenderingObjectLayer);

                // Add instances to world
                WorldHelpers.AddContributingInstancesToWorld(world.PathTracingWorld, in fatInstances, out var lodInstances, out var lodgroupToContributorInstances);

                // Build world with extracted data
                const bool emissiveSampling = true;
                world.PathTracingWorld.Build(sceneBounds, deviceContext.GetCommandBuffer(), ref world.ScratchBuffer, samplingResources, emissiveSampling, 1024);

                LightmapBakeSettings lightmapBakeSettings = GetLightmapBakeSettings(bakeInput);
                // Build array of lightmap descriptors based on the atlassing data and instances.
                LightmapDesc[] lightmapDescriptors = PopulateLightmapDescsFromAtlassing(in lightmapRequestData.atlassing, in fatInstances);
                SortInstancesByMeshAndResolution(lightmapDescriptors);

                ulong probeWorkSteps = CalculateWorkStepsForProbeRequests(in bakeInput, in probeRequestData);
                ulong lightmapWorkSteps = CalculateWorkStepsForLightmapRequests(in lightmapRequestData, lightmapDescriptors, lightmapBakeSettings);
                progressState.SetTotalWorkSteps(probeWorkSteps + lightmapWorkSteps);

                if (!ExecuteProbeRequests(in bakeInput, in probeRequestData, deviceContext, useLegacyBakingBehavior, world, progressState, samplingResources))
                    return false;

                if (lightmapRequestData.requests.Length <= 0)
                    return true;

                // Populate resources structure
                LightmapResourceLibrary resources = new();
                resources.Load(world.RayTracingContext);

                if (ExecuteLightmapRequests(in lightmapRequestData, deviceContext, world, in fatInstances, in lodInstances, in lodgroupToContributorInstances, integrationSettings, useLegacyBakingBehavior, resources, progressState, lightmapDescriptors, lightmapBakeSettings, samplingResources) != Result.Success)
                    return false;

                CoreUtils.Destroy(resources.UVFallbackBufferGenerationMaterial);

                return true;
            }
        }

        internal static ulong CalculateWorkStepsForProbeRequests(in BakeInput bakeInput, in ProbeRequestData probeRequestData)
        {
            ulong calculatedWorkSteps = 0;
            foreach (ProbeRequest probeRequest in probeRequestData.requests)
            {
                (uint directSampleCount, uint effectiveIndirectSampleCount) = GetProbeSampleCounts(probeRequest.sampleCount);

                calculatedWorkSteps += CalculateProbeWorkSteps(probeRequest.count, probeRequest.outputTypeMask,
                    directSampleCount, effectiveIndirectSampleCount,
                    bakeInput.lightingSettings.mixedLightingMode != MixedLightingMode.IndirectOnly,
                    probeRequest.maxBounces);
            }

            return calculatedWorkSteps;
        }

        private static ulong CalculateProbeWorkSteps(ulong count, ProbeRequestOutputType outputTypeMask, uint directSampleCount, uint effectiveIndirectSampleCount, bool usesProbeOcclusion, uint bounceCount)
        {
            ulong workSteps = 0;
            if (outputTypeMask.HasFlag(ProbeRequestOutputType.RadianceIndirect))
                workSteps += ProbeIntegrator.CalculateWorkSteps(count, effectiveIndirectSampleCount, bounceCount);
            if (outputTypeMask.HasFlag(ProbeRequestOutputType.RadianceDirect))
                workSteps += ProbeIntegrator.CalculateWorkSteps(count, directSampleCount, 0);
            if (outputTypeMask.HasFlag(ProbeRequestOutputType.Validity))
                workSteps += ProbeIntegrator.CalculateWorkSteps(count, effectiveIndirectSampleCount, 0);
            if (outputTypeMask.HasFlag(ProbeRequestOutputType.LightProbeOcclusion) && usesProbeOcclusion)
                workSteps += ProbeIntegrator.CalculateWorkSteps(count, effectiveIndirectSampleCount, 0);

            return workSteps;
        }

        internal static ulong CalculateWorkStepsForLightmapRequests(in LightmapRequestData lightmapRequestData, LightmapDesc[] lightmapDescriptors, LightmapBakeSettings lightmapBakeSettings)
        {
            ulong calculatedWorkSteps = 0;
            foreach (LightmapRequest r in lightmapRequestData.requests)
            {
                ref readonly var request = ref r;
                if (request.lightmapCount == 0)
                    continue;
                for (int lightmapIndex = 0; lightmapIndex < request.lightmapCount; lightmapIndex++)
                {
                    LightmapDesc currentLightmapDesc = lightmapDescriptors[lightmapIndex];
                    Dictionary<IntegratedOutputType, RequestedSubOutput> requestedLightmapTypes = GetRequestedIntegratedOutputTypes(request);
                    foreach (IntegratedOutputType lightmapType in requestedLightmapTypes.Keys)
                    {
                        uint sampleCount = lightmapBakeSettings.GetSampleCount(lightmapType);
                        foreach (BakeInstance bakeInstance in currentLightmapDesc.BakeInstances)
                        {
                            uint instanceWidth = (uint)bakeInstance.TexelSize.x;
                            uint instanceHeight = (uint)bakeInstance.TexelSize.y;

                            calculatedWorkSteps += CalculateIntegratedLightmapWorkSteps(sampleCount, instanceWidth * instanceHeight, lightmapType, lightmapBakeSettings.BounceCount, 1);
                        }
                    }
                    HashSet<LightmapRequestOutputType> requestedNonIntegratedLightmapTypes = GetRequestedNonIntegratedOutputTypes(request);
                    foreach (LightmapRequestOutputType _ in requestedNonIntegratedLightmapTypes)
                        calculatedWorkSteps += CalculateNonIntegratedLightmapWorkSteps(currentLightmapDesc.Resolution * currentLightmapDesc.Resolution);
                }
            }

            return calculatedWorkSteps;
        }

        private static ulong CalculateIntegratedLightmapWorkSteps(uint samplesPerTexel, uint chunkSize, IntegratedOutputType outputType, uint bounces, uint multiplier)
        {
            uint bouncesMultiplier = outputType == IntegratedOutputType.Indirect
                ? 0 == bounces ? 1 : bounces
                : 1;

            return samplesPerTexel*chunkSize*bouncesMultiplier*multiplier;
        }

        private static ulong CalculateNonIntegratedLightmapWorkSteps(uint lightmapResolution) => lightmapResolution;

        private static IntegrationSettings GetIntegrationSettings(in BakeInput bakeInput)
        {
            var retVal = IntegrationSettings.Default;
            retVal.Backend =
                bakeInput.lightingSettings.useHardwareRayTracing && RayTracingContext.IsBackendSupported(RayTracingBackend.Hardware) ?
                    RayTracingBackend.Hardware : RayTracingBackend.Compute;

            return retVal;
        }

        internal static LightmapBakeSettings GetLightmapBakeSettings(in BakeInput bakeInput)
        {
            // Lightmap settings
            LightmapBakeSettings lightmapBakeSettings = new()
            {
                AOSampleCount = math.max(0, bakeInput.lightingSettings.lightmapSampleCounts.indirectSampleCount),
                DirectSampleCount = math.max(0, bakeInput.lightingSettings.lightmapSampleCounts.directSampleCount),
                IndirectSampleCount = math.max(0, bakeInput.lightingSettings.lightmapSampleCounts.indirectSampleCount),
                BounceCount = math.max(0, bakeInput.lightingSettings.maxBounces),
                AOMaxDistance = math.max(0.0f, bakeInput.lightingSettings.aoDistance)
            };
            lightmapBakeSettings.ValiditySampleCount = lightmapBakeSettings.IndirectSampleCount;
            return lightmapBakeSettings;
        }

        [Flags]
        private enum RequestedSubOutput
        {
            PrimaryTexture = 1 << 0,
            DirectionalityTexture = 1 << 1
        }

        private struct HashedInstanceIndex : IComparable<HashedInstanceIndex>
        {
            public BakeInstance bakeInstance;
            public int texelCount;
            public int offsetX;
            public int offsetY;
            public int hashCode;

            public int CompareTo(HashedInstanceIndex other)
            {
                    int size = other.texelCount - texelCount;
                    if (size != 0)
                        return size;
                    int xOffset = other.offsetX - offsetX;
                    if (xOffset != 0)
                        return xOffset;
                    int yOffset = offsetY - other.offsetY;
                    if (yOffset != 0)
                        return yOffset;
                    return hashCode - other.hashCode;
            }
        };

        internal static void SortInstancesByMeshAndResolution(LightmapDesc[] lightmapDescriptors)
        {
            int lightmapDescIndex = 0;
            foreach (var lightmapDesc in lightmapDescriptors)
            {
                HashedInstanceIndex[] hashedInstances = new HashedInstanceIndex[lightmapDesc.BakeInstances.Length];
                int instanceIndex = 0;
                foreach (var instance in lightmapDesc.BakeInstances)
                {
                    int hashCode = System.HashCode.Combine(instance.TexelSize.x, instance.TexelSize.y);
                    hashedInstances[instanceIndex] = new();
                    hashedInstances[instanceIndex].bakeInstance = instance;
                    hashedInstances[instanceIndex].texelCount = instance.TexelSize.x * instance.TexelSize.y;
                    hashedInstances[instanceIndex].offsetX = instance.TexelOffset.x;
                    hashedInstances[instanceIndex].offsetY = instance.TexelOffset.y;
                    hashedInstances[instanceIndex].hashCode = hashCode;
                    instanceIndex++;
                }
                Array.Sort(hashedInstances);
                instanceIndex = 0;
                foreach (var hashedInstance in hashedInstances)
                {
                    lightmapDescriptors[lightmapDescIndex].BakeInstances[instanceIndex++] = hashedInstance.bakeInstance;
                }
                lightmapDescIndex++;
            }
        }

        // Gets all meshes used by the specified instances, ordered by the first instance they are used in.
        private static List<Mesh> GetMeshesInInstanceOrder(LightmapDesc[] lightmapDescriptors)
        {
            List<Mesh> sortedMeshes = new List<Mesh>();
            HashSet<Mesh> seenMeshes = new HashSet<Mesh>();
            foreach (var lightmapDesc in lightmapDescriptors)
            {
                foreach (var instance in lightmapDesc.BakeInstances)
                {
                    if (seenMeshes.Add(instance.Mesh))
                    {
                        sortedMeshes.Add(instance.Mesh);
                    }
                }
            }
            return sortedMeshes;
        }

        private static bool AnyLightmapRequestHasOutput(LightmapRequest[] requests, LightmapRequestOutputType type)
        {
            foreach (var req in requests)
            {
                if (req.outputTypeMask.HasFlag(type))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool EnsureInstanceInCacheAndClearExistingEntries(CommandBuffer cmd,
            LightmappingContext lightmappingContext,
            BakeInstance bakeInstance)
        {
            var bakeInstances = new[] { bakeInstance };
            if (!lightmappingContext.ResourceCache.CacheIsHot(bakeInstances))
            {
                // Build the required resources for the current instance
                GraphicsHelpers.Flush(cmd); // need to flush as we are removing resources from the cache which might be in use
                lightmappingContext.ResourceCache.FreeResources(bakeInstances);

                if (!lightmappingContext.ResourceCache.AddResources(
                    bakeInstances, lightmappingContext.World.RayTracingContext, cmd,
                    lightmappingContext.IntegratorContext.UVFallbackBufferBuilder))
                {
                    return false;
                }
                // Flush as there can be a substantial amount of work done in the commandbuffer
                GraphicsHelpers.Flush(cmd);
            }

            return true;
        }

        private static bool GetInstanceUVResources(
            CommandBuffer cmd,
            LightmappingContext lightmappingContext,
            BakeInstance bakeInstance,
            out UVMesh uvMesh,
            out UVAccelerationStructure uvAS,
            out UVFallbackBuffer uvFallbackBuffer)
        {
            uvMesh = default;
            uvAS = default;
            uvFallbackBuffer = default;

            if (!EnsureInstanceInCacheAndClearExistingEntries(cmd, lightmappingContext, bakeInstance))
                return false;

            bool gotResources = lightmappingContext.ResourceCache.GetResources(new[] { bakeInstance },
                out UVMesh[] uvMeshes, out UVAccelerationStructure[] uvAccelerationStructures, out UVFallbackBuffer[] uvFallbackBuffers);
            if (!gotResources)
                return false;

            uvMesh = uvMeshes[0];
            uvAS = uvAccelerationStructures[0];
            uvFallbackBuffer = uvFallbackBuffers[0];

            return true;
        }

        static void GetLodZeroInstanceMasks(bool pathtracingShader, uint[] originalMasks, Span<uint> lodZeroMasks)
        {
            for (int j = 0; j < originalMasks.Length; j++)
            {
                // if we don't need bounce rays, we just make the lod0 invisible (instanceMask=0), so that only the current lod can be traced against
                // otherwise we set bits so that we can select one or the other using the raymask in the shader
                if (pathtracingShader)
                {
                    lodZeroMasks[j] = (uint)InstanceFlags.LOD_ZERO_FOR_LIGHTMAP_INSTANCE;
                    if ((originalMasks[j] & (uint)InstanceFlags.SHADOW_RAY_VIS_MASK) != 0)
                        lodZeroMasks[j] |= (uint)InstanceFlags.LOD_ZERO_FOR_LIGHTMAP_INSTANCE_SHADOW;
                }
                else
                {
                    lodZeroMasks[j] = 0;
                }
            }
        }

        static void GetCurrentLodInstanceMasks(bool pathtracingShader, uint[] originalMasks, Span<uint> currentLodMasks)
        {
            for (int j = 0; j < originalMasks.Length; j++)
            {
                if (pathtracingShader)
                {
                    currentLodMasks[j] = (uint)InstanceFlags.CURRENT_LOD_FOR_LIGHTMAP_INSTANCE;
                    if ((originalMasks[j] & (uint)InstanceFlags.SHADOW_RAY_VIS_MASK) != 0)
                        currentLodMasks[j] |= (uint)InstanceFlags.CURRENT_LOD_FOR_LIGHTMAP_INSTANCE_SHADOW;
                }
                else
                {
                    currentLodMasks[j] = originalMasks[j];
                }
            }
        }

        internal static InstanceHandle[] AddLODInstances(World world, CommandBuffer cmd, LodIdentifier lodIdentifier, in List<LodInstanceBuildData> lodInstancesBuildData, bool pathtracingShader, Dictionary<Int32, List<ContributorLodInfo>> lodgroupToContributorInstances)
        {
            Debug.Assert(lodIdentifier.IsValid() && !lodIdentifier.IsContributor());

            // add current lod instances
            var instanceHandles = new InstanceHandle[lodInstancesBuildData.Count];
            int i = 0;
            uint currentLodLevel = lodIdentifier.MinLodLevelMask();
            foreach (LodInstanceBuildData lodBuildData in lodInstancesBuildData)
            {
                if ((lodBuildData.LodMask & currentLodLevel) == 0)
                    continue;

                Span<uint> currentLodMasks = stackalloc uint[lodBuildData.Masks.Length];
                GetCurrentLodInstanceMasks(pathtracingShader, lodBuildData.Masks, currentLodMasks);

                instanceHandles[i++] = world.AddInstance(lodBuildData.Mesh, lodBuildData.Materials, currentLodMasks, UnityComputeWorld.RenderingObjectLayer, lodBuildData.LocalToWorldMatrix, lodBuildData.Bounds, lodBuildData.IsStatic, lodBuildData.Filter, true);
            }
            Array.Resize(ref instanceHandles, i);

            // update lod0 instance mask
            List<ContributorLodInfo> lodZeroInstances;
            if (lodgroupToContributorInstances.TryGetValue(lodIdentifier.LodGroup, out lodZeroInstances))
            {
                foreach (var lodZeroInstance in lodZeroInstances)
                {
                    if ((lodZeroInstance.LodMask & currentLodLevel) != 0)
                        continue;

                    Span<uint> lodZeroMasks = stackalloc uint[lodZeroInstance.Masks.Length];
                    GetLodZeroInstanceMasks(pathtracingShader, lodZeroInstance.Masks, lodZeroMasks);

                    world.UpdateInstanceMask(lodZeroInstance.InstanceHandle, lodZeroMasks);
                }
            }

            return instanceHandles;
        }

        internal static void RemoveLODInstances(World world, CommandBuffer cmd, LodIdentifier lodIdentifier, Span<InstanceHandle> currentLodInstanceHandles, Dictionary<Int32, List<ContributorLodInfo>> lodgroupToContributorInstances)
        {
            if (!lodIdentifier.IsValid() || lodIdentifier.IsContributor())
                return;

            // Remove current lod instances
            foreach (var instanceHandle in currentLodInstanceHandles)
                world.RemoveInstance(instanceHandle);

            // Restore lod0 instances
            List<ContributorLodInfo> lodZeroInstances;
            if (lodgroupToContributorInstances.TryGetValue(lodIdentifier.LodGroup, out lodZeroInstances))
            {
                foreach (var lodZeroInstance in lodZeroInstances)
                    world.UpdateInstanceMask(lodZeroInstance.InstanceHandle, lodZeroInstance.Masks);
            }
        }

        private static InstanceHandle[] PrepareLodInstances(
            CommandBuffer cmd,
            UnityComputeWorld world,
            BakeInstance bakeInstance,
            Dictionary<int, List<LodInstanceBuildData>> lodInstances,
            Dictionary<Int32, List<ContributorLodInfo>> lodgroupToContributorInstances,
            bool isPathTracingPass)
        {
            if (bakeInstance.LodIdentifier.IsValid() && !bakeInstance.LodIdentifier.IsContributor())
            {
                // AddLODInstances calls _pathTracingWorld.Add/RemoveInstance(...) that doesn't take a cmd buffer arg. Need to flush as cmdbuffer and immediate calls cannot be mixed.
                GraphicsHelpers.Flush(cmd);

                var currentLodInstancesBuildData = lodInstances[bakeInstance.LodIdentifier.LodGroup];
                InstanceHandle[] handles = AddLODInstances(world.PathTracingWorld, cmd, bakeInstance.LodIdentifier, currentLodInstancesBuildData, isPathTracingPass, lodgroupToContributorInstances);
                world.BuildAccelerationStructure(cmd);
                return handles;
            }
            return null;
        }

        private static void ClearLodInstances(
            CommandBuffer cmd,
            UnityComputeWorld world,
            BakeInstance bakeInstance,
            InstanceHandle[] currentLodInstances,
            Dictionary<Int32, List<ContributorLodInfo>> lodgroupToContributorInstances)
        {
            if (bakeInstance.LodIdentifier.IsValid() && !bakeInstance.LodIdentifier.IsContributor())
            {
                // RemoveLODInstances calls _pathTracingWorld.Add/RemoveInstance(...) that doesn't take a cmd buffer arg. Need to flush as cmdbuffer and immediate calls cannot be mixed.
                GraphicsHelpers.Flush(cmd);

                RemoveLODInstances(world.PathTracingWorld, cmd, bakeInstance.LodIdentifier, currentLodInstances, lodgroupToContributorInstances);
                world.BuildAccelerationStructure(cmd);
            }
        }

         private static Result IntegrateLightmapInstance(CommandBuffer cmd,
            int lightmapIndex,
            IntegratedOutputType integratedOutputType,
            BakeInstance bakeInstance,
            in Dictionary<int, List<LodInstanceBuildData>> lodInstances,
            in Dictionary<Int32, List<ContributorLodInfo>> lodgroupToContributorInstances,
            LightmappingContext lightmappingContext,
            UVAccelerationStructure uvAS,
            UVFallbackBuffer uvFallbackBuffer,
            IntegrationSettings integrationSettings,
            LightmapBakeSettings lightmapBakeSettings,
            bool doDirectionality,
            BakeProgressState progressState,
            LightmapIntegrationHelpers.GPUSync gpuSync,
            bool debugDispatches)
        {
            Debug.Assert(!lightmappingContext.ExpandedBufferNeedsUpdating(lightmapBakeSettings.ExpandedBufferSize), "The integration data must be allocated at this point.");

            // Bake the lightmap instances
            int bakeDispatches = 0;
            LightmapBakeState bakeState = new();
            bakeState.Init();
            uint samples = 0;
            bool isInstanceDone = false;
            uint dispatchCount = 0;
            uint instanceWidth = (uint)bakeInstance.TexelSize.x;
            uint instanceHeight = (uint)bakeInstance.TexelSize.y;
            InstanceHandle[] currentLodInstances = null;

            // accumulate the instance
            System.Diagnostics.Stopwatch instanceFlushStopwatch = System.Diagnostics.Stopwatch.StartNew();
            System.Diagnostics.Stopwatch dispatchStopwatch = System.Diagnostics.Stopwatch.StartNew();
            do
            {
                if (debugDispatches)
                {
                    gpuSync.Sync(cmd);
                    dispatchStopwatch.Restart();
                    Console.WriteLine($"Begin pass {bakeDispatches} for lm: {lightmapIndex}, type: {integratedOutputType}, sample count: {bakeState.SampleIndex}, res: [{bakeInstance.TexelSize.x} x {bakeInstance.TexelSize.y}], offset: [{bakeInstance.TexelOffset.x} x {bakeInstance.TexelOffset.y}].");
                }

                bool isInstanceStart = (bakeState.SampleIndex == 0);
                if (isInstanceStart)
                {
                    bool isPathTracingPass = integratedOutputType == IntegratedOutputType.Indirect || integratedOutputType == IntegratedOutputType.DirectionalityIndirect;
                    currentLodInstances = PrepareLodInstances(cmd, lightmappingContext.World, bakeInstance, lodInstances, lodgroupToContributorInstances, isPathTracingPass);
                }

                // Enqueue some baking work in the command buffer.
                cmd.BeginSample($"Bake {integratedOutputType}");
                dispatchCount++;

                uint passSamplesPerTexel = BakeLightmapDriver.AccumulateLightmapInstance(
                    bakeState,
                    bakeInstance,
                    lightmapBakeSettings,
                    integratedOutputType,
                    lightmappingContext,
                    uvAS,
                    uvFallbackBuffer,
                    doDirectionality,
                    out uint chunkSize,
                    out isInstanceDone);

                samples += instanceWidth * instanceHeight * passSamplesPerTexel;
                cmd.EndSample($"Bake {integratedOutputType}");

                if (isInstanceDone)
                {
                    ClearLodInstances(cmd, lightmappingContext.World, bakeInstance, currentLodInstances, lodgroupToContributorInstances);
                }

                if (debugDispatches)
                {
                    gpuSync.Sync(cmd);
                    dispatchStopwatch.Stop();
                    Console.WriteLine($"Finished pass {bakeDispatches}. Elapsed ms: {dispatchStopwatch.ElapsedMilliseconds}");
                }

                bakeDispatches++;

                // Execute the baking work scheduled in BakeLightmaps.
                if (bakeDispatches % integrationSettings.MaxDispatchesPerFlush != 0 && !isInstanceDone)
                    continue;

                // Chip-off work steps based on the chunk done so far
                ulong completedWorkSteps =
                    CalculateIntegratedLightmapWorkSteps(passSamplesPerTexel, chunkSize, integratedOutputType, lightmapBakeSettings.BounceCount, integrationSettings.MaxDispatchesPerFlush);
                gpuSync.RequestAsyncReadback(cmd, _ => progressState.IncrementCompletedWorkSteps(completedWorkSteps));
                GraphicsHelpers.Flush(cmd);

                if (!debugDispatches)
                    continue;

                gpuSync.Sync(cmd);
                instanceFlushStopwatch.Stop();
                Console.WriteLine($"Bake dispatch flush -> Instance: {bakeInstance.Mesh.GetEntityId()}, dispatches {dispatchCount}, Samples: \t{samples}\t. Elapsed ms:\t{instanceFlushStopwatch.ElapsedMilliseconds}");
                samples = 0;
                dispatchCount = 0;
                instanceFlushStopwatch.Restart();

                //LightmapIntegrationHelpers.WriteRenderTexture(cmd, $"Temp/lm{lightmapIndex}_type{lightmapType}_pass{bakeDispatches}.r2d", lightmappingContext.AccumulatedOutput, lightmappingContext.AccumulatedOutput.width, lightmappingContext.AccumulatedOutput.height);
            }
            while (isInstanceDone == false);

            return Result.Success;
        }

        internal static Result ExecuteLightmapRequests(
            in LightmapRequestData lightmapRequestData,
            UnityComputeDeviceContext deviceContext,
            UnityComputeWorld world,
            in FatInstance[] fatInstances,
            in Dictionary<int, List<LodInstanceBuildData>> lodInstances,
            in Dictionary<Int32, List<ContributorLodInfo>> lodgroupToContributorInstances,
            IntegrationSettings integrationSettings,
            bool useLegacyBakingBehavior,
            LightmapResourceLibrary lightmapResourceLib,
            BakeProgressState progressState,
            LightmapDesc[] lightmapDescriptors,
            LightmapBakeSettings lightmapBakeSettings,
            UnityEngine.Rendering.Sampling.SamplingResources samplingResources)
        {
            using var lightmappingContext = new LightmappingContext();

            bool debugDispatches = integrationSettings.DebugDispatches;
            bool doDirectionality = AnyLightmapRequestHasOutput(lightmapRequestData.requests, LightmapRequestOutputType.DirectionalityDirect) || AnyLightmapRequestHasOutput(lightmapRequestData.requests, LightmapRequestOutputType.DirectionalityIndirect);

            // Find the max index count in any mesh, so we can pre-allocate various buffers based on it.
            uint maxIndexCount = 1;
            for (int meshIdx = 0; meshIdx < world.Meshes.Length; ++meshIdx)
            {
                maxIndexCount = Math.Max(maxIndexCount, world.Meshes[meshIdx].GetTotalIndexCount());
            }

            (int width, int height)[] atlasSizes = lightmapRequestData.atlassing.m_AtlasSizes;
            int initialLightmapResolution = atlasSizes.Length > 0 ? atlasSizes[0].width : 1024;
            if (!lightmappingContext.Initialize(deviceContext, initialLightmapResolution, initialLightmapResolution, world, maxIndexCount, lightmapResourceLib))
                return Result.InitializeFailure;

            lightmappingContext.IntegratorContext.Initialize(samplingResources, lightmapResourceLib, !useLegacyBakingBehavior);

            // Chart identification happens in multithreaded fashion on the CPU. We start it immediately so it can run in tandem with other work.
            bool usesChartIdentification = AnyLightmapRequestHasOutput(lightmapRequestData.requests, LightmapRequestOutputType.ChartIndex) ||
                                           AnyLightmapRequestHasOutput(lightmapRequestData.requests, LightmapRequestOutputType.OverlapPixelIndex);
            using ParallelChartIdentification chartIdentification = usesChartIdentification ? new ParallelChartIdentification(GetMeshesInInstanceOrder(lightmapDescriptors)) : null;
            if (usesChartIdentification)
                chartIdentification.Start();

            if (debugDispatches)
            {
                for (int i = 0; i < lightmapDescriptors.Length; ++i)
                {
                    Console.WriteLine($"Desc:");
                    int instanceIndex = 0;
                    foreach (var bakeInstance in lightmapDescriptors[i].BakeInstances)
                        Console.WriteLine($"  Instance[{instanceIndex++}]: {bakeInstance.Mesh.GetEntityId()}, res: [{bakeInstance.TexelSize.x} x {bakeInstance.TexelSize.y}], offset: [{bakeInstance.TexelOffset.x} x {bakeInstance.TexelOffset.y}].");
                }
            }

            CommandBuffer cmd = lightmappingContext.GetCommandBuffer();
            using LightmapIntegrationHelpers.GPUSync gpuSync = new(); // used for sync points in debug mode
            gpuSync.Create();

            // process requests
            for (int requestIndex = 0; requestIndex < lightmapRequestData.requests.Length; requestIndex++)
            {
                if (progressState.WasCancelled())
                    return Result.Cancelled;

                ref readonly var request = ref lightmapRequestData.requests[requestIndex];
                if (request.lightmapCount == 0)
                    continue;

                var createDirResult = Directory.CreateDirectory(request.outputFolderPath);
                if (createDirResult.Exists == false)
                    return Result.CreateDirectoryFailure;

                Dictionary<IntegratedOutputType, RequestedSubOutput> integratedRequestOutputs = GetRequestedIntegratedOutputTypes(request);
                bool needsNormalsForDirectionality = request.outputTypeMask.HasFlag(LightmapRequestOutputType.DirectionalityIndirect) || request.outputTypeMask.HasFlag(LightmapRequestOutputType.DirectionalityDirect);
                bool writeNormals = request.outputTypeMask.HasFlag(LightmapRequestOutputType.Normal);

                // Request related bake settings
                for (int lightmapIndex = 0; lightmapIndex < request.lightmapCount; lightmapIndex++)
                {
                    if (progressState.WasCancelled())
                        return Result.Cancelled;

                    LightmapDesc currentLightmapDesc = lightmapDescriptors[lightmapIndex];
                    lightmapBakeSettings.PushOff = currentLightmapDesc.PushOff;
                    int resolution = (int)currentLightmapDesc.Resolution;
                    UInt64 lightmapSize = (UInt64)resolution * (UInt64)resolution;
                    lightmapBakeSettings.ExpandedBufferSize = LightmapRequest.TilingModeToLightmapExpandedBufferSize(request.tilingMode);

                    // allocate expanded buffer
                    if (lightmappingContext.ExpandedBufferNeedsUpdating(lightmapBakeSettings.ExpandedBufferSize))
                    {
                        GraphicsHelpers.Flush(cmd); // need to flush as we are removing resources from the cache which might be in use
                        if (!lightmappingContext.InitializeExpandedBuffer(lightmapBakeSettings.ExpandedBufferSize))
                            return Result.InitializeExpandedBufferFailure;
                        // The scratch buffer is used for tracing rays in the lightmap integrators it needs to be sufficiently large to ray trace an expanded buffer
                        uint scratchBufferSize = (uint)lightmapBakeSettings.ExpandedBufferSize;
                        lightmappingContext.InitializeTraceScratchBuffer(scratchBufferSize, 1, 1);
                        if (debugDispatches)
                            Console.WriteLine($"Built expanded buffer for {lightmapBakeSettings.ExpandedBufferSize} samples, lm w: {lightmappingContext.AccumulatedOutput.width}, lm h: {lightmappingContext.AccumulatedOutput.height}].");
                    }

                    ref RenderTexture accumulatedOutput = ref lightmappingContext.AccumulatedOutput;
                    if ((accumulatedOutput.width != resolution) || (accumulatedOutput.height != resolution))
                        if (!lightmappingContext.SetOutputResolution(resolution, resolution))
                            return Result.InitializeFailure;

                    // Bake normals output
                    RenderTexture normalBuffer = null;
                    if (needsNormalsForDirectionality || writeNormals)
                    {
                        lightmappingContext.ClearOutputs();

                        IRayTracingShader normalShader = lightmapResourceLib.NormalAccumulationShader;
                        GraphicsBuffer compactedGBufferLength = lightmappingContext.CompactedGBufferLength;
                        GraphicsBuffer indirectDispatchBuffer = lightmappingContext.IndirectDispatchBuffer;
                        GraphicsBuffer indirectRayTracingDispatchBuffer = lightmappingContext.IndirectDispatchRayTracingBuffer;

                        uint maxChunkSize = (uint)lightmappingContext.ExpandedOutput.count;
                        var expansionShaders = lightmapResourceLib.ExpansionHelpers;
                        var compactionKernel = expansionShaders.FindKernel("CompactGBuffer");
                        var populateCopyDispatchKernel = expansionShaders.FindKernel("PopulateCopyDispatch");
                        var copyToLightmapKernel = expansionShaders.FindKernel("AdditivelyCopyCompactedTo2D");
                        var populateNormalShaderDispatchKernel = expansionShaders.FindKernel("PopulateAccumulationDispatch");

                        expansionShaders.GetKernelThreadGroupSizes(copyToLightmapKernel, out uint copyThreadGroupSizeX, out uint copyThreadGroupSizeY, out uint copyThreadGroupSizeZ);
                        Debug.Assert(copyThreadGroupSizeY == 1 && copyThreadGroupSizeZ == 1);

                        foreach (var bakeInstance in currentLightmapDesc.BakeInstances)
                        {
                            if (progressState.WasCancelled())
                                return Result.Cancelled;

                            if (!GetInstanceUVResources(cmd, lightmappingContext, bakeInstance, out _, out var uvAS, out var uvFallbackBuffer))
                                return Result.AddResourcesToCacheFailure;

                            InstanceHandle[] currentLodInstances = PrepareLodInstances(cmd, lightmappingContext.World, bakeInstance, lodInstances, lodgroupToContributorInstances, false);
                            var instanceGeometryIndex = lightmappingContext.World.PathTracingWorld.GetAccelerationStructure().GeometryPool.GetInstanceGeometryIndex(bakeInstance.Mesh);

                            uint instanceWidth = (uint)bakeInstance.TexelSize.x;
                            uint instanceHeight = (uint)bakeInstance.TexelSize.y;
                            UInt64 instanceSize = (UInt64)instanceWidth * (UInt64)instanceHeight;
                            UInt64 chunkTexelOffset = 0;
                            do
                            {
                                uint2 chunkOffset = new uint2((uint)(chunkTexelOffset % (UInt64)instanceWidth), (uint)(chunkTexelOffset / (UInt64)instanceWidth));
                                uint remainingTexels = instanceWidth - chunkOffset.x + ((instanceHeight - 1) - chunkOffset.y) * instanceWidth;
                                uint chunkSize = math.min(maxChunkSize, remainingTexels);
                                Debug.Assert(remainingTexels > 0);

                                cmd.BeginSample($"Bake Normals");
                                ExpansionHelpers.CompactGBuffer(
                                    cmd,
                                    expansionShaders,
                                    compactionKernel,
                                    instanceWidth,
                                    chunkSize,
                                    chunkOffset,
                                    uvFallbackBuffer,
                                    compactedGBufferLength,
                                    lightmappingContext.CompactedTexelIndices);

                                // build a gBuffer for the chunk
                                ExpansionHelpers.GenerateGBuffer(
                                    cmd,
                                    lightmappingContext.IntegratorContext.GBufferShader,
                                    lightmappingContext.GBuffer,
                                    lightmappingContext.TraceScratchBuffer,
                                    lightmappingContext.IntegratorContext.SamplingResources,
                                    uvAS,
                                    uvFallbackBuffer,
                                    compactedGBufferLength,
                                    lightmappingContext.CompactedTexelIndices,
                                    bakeInstance.TexelOffset,
                                    chunkOffset,
                                    chunkSize,
                                    expandedSampleWidth: 1,
                                    passSampleCount: 1,
                                    sampleOffset: 0,
                                    AntiAliasingType.SuperSampling,
                                    superSampleWidth: 1);
                                chunkTexelOffset += chunkSize;

                                // now perform the normal generation for the chunk
                                // geometry pool bindings
                                Util.BindAccelerationStructure(cmd, normalShader, world.PathTracingWorld.GetAccelerationStructure());

                                var requiredSizeInBytes = normalShader.GetTraceScratchBufferRequiredSizeInBytes((uint)chunkSize, 1, 1);
                                if (requiredSizeInBytes > 0)
                                {
                                    var actualScratchBufferSize = (ulong)(lightmappingContext.TraceScratchBuffer.count * lightmappingContext.TraceScratchBuffer.stride);
                                    Debug.Assert(lightmappingContext.TraceScratchBuffer.stride == sizeof(uint));
                                    Debug.Assert(requiredSizeInBytes <= actualScratchBufferSize);
                                }

                                normalShader.SetMatrixParam(cmd, LightmapIntegratorShaderIDs.ShaderLocalToWorld, bakeInstance.LocalToWorldMatrix);
                                normalShader.SetMatrixParam(cmd, LightmapIntegratorShaderIDs.ShaderLocalToWorldNormals, bakeInstance.LocalToWorldMatrixNormals);
                                normalShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.InstanceGeometryIndex, instanceGeometryIndex);
                                normalShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.InstanceWidth, (int)instanceWidth);

                                normalShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.GBuffer, lightmappingContext.GBuffer);
                                normalShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.CompactedGBuffer, lightmappingContext.CompactedTexelIndices);
                                normalShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.ExpandedOutput, lightmappingContext.ExpandedOutput);
                                normalShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ExpandedTexelSampleWidth, 1);
                                normalShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ChunkOffsetX, (int)chunkOffset.x);
                                normalShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ChunkOffsetY, (int)chunkOffset.y);

                                ExpansionHelpers.PopulateAccumulationIndirectDispatch(cmd, normalShader, expansionShaders, populateNormalShaderDispatchKernel, 1, compactedGBufferLength, indirectRayTracingDispatchBuffer);
                                normalShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.SampleOffset, 0);
                                normalShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.MaxLocalSampleCount, 1);
                                cmd.BeginSample("Normal Generation");
                                normalShader.Dispatch(cmd, lightmappingContext.TraceScratchBuffer, indirectRayTracingDispatchBuffer);
                                cmd.EndSample("Normal Generation");

                                // copy back to the output
                                ExpansionHelpers.PopulateCopyToLightmapIndirectDispatch(cmd, expansionShaders, populateCopyDispatchKernel, copyThreadGroupSizeX, compactedGBufferLength, indirectDispatchBuffer);
                                ExpansionHelpers.CopyToLightmap(cmd, expansionShaders, copyToLightmapKernel, 1, (int)instanceWidth, bakeInstance.TexelOffset, chunkOffset, compactedGBufferLength, lightmappingContext.CompactedTexelIndices, lightmappingContext.ExpandedOutput, indirectDispatchBuffer, lightmappingContext.AccumulatedOutput);
                                cmd.EndSample("Bake Normals");
                            }
                            while (chunkTexelOffset < instanceSize);

                            ClearLodInstances(cmd, lightmappingContext.World, bakeInstance, currentLodInstances, lodgroupToContributorInstances);
                        }

                        if (writeNormals)
                        {
                            if (LightmapIntegrationHelpers.WriteLightmap(cmd, accumulatedOutput, "normal", lightmapIndex, request.outputFolderPath) == false)
                                return Result.WriteToDiskFailure;
                        }

                        if (needsNormalsForDirectionality)
                        {
                            // Hang on to normal buffer for normalization of directionality
                            normalBuffer = LightmappingContext.MakeRenderTexture(accumulatedOutput.width, accumulatedOutput.height, "TempNormalBuffer");
                            cmd.CopyTexture(accumulatedOutput, normalBuffer);
                        }

                        ulong workSteps = CalculateNonIntegratedLightmapWorkSteps((uint) lightmapSize);
                        progressState.IncrementCompletedWorkSteps(workSteps);
                    }

                    // Bake occupancy output
                    if (request.outputTypeMask.HasFlag(LightmapRequestOutputType.Occupancy))
                    {
                        lightmappingContext.ClearOutputs();

                        foreach (var bakeInstance in currentLightmapDesc.BakeInstances)
                        {
                            if (!GetInstanceUVResources(cmd, lightmappingContext, bakeInstance, out _, out _, out var uvFallbackBuffer))
                                return Result.AddResourcesToCacheFailure;

                            lightmappingContext.IntegratorContext.LightmapOccupancyIntegrator.Accumulate(
                                cmd,
                                bakeInstance.TexelSize,
                                bakeInstance.TexelOffset,
                                uvFallbackBuffer,
                                accumulatedOutput);
                        }

                        if (LightmapIntegrationHelpers.WriteLightmap(cmd, accumulatedOutput, "occupancy", lightmapIndex, request.outputFolderPath) == false)
                            return Result.WriteToDiskFailure;

                        ulong workSteps = CalculateNonIntegratedLightmapWorkSteps((uint) lightmapSize);
                        progressState.IncrementCompletedWorkSteps(workSteps);
                    }

                    // Bake chart index output
                    if (request.outputTypeMask.HasFlag(LightmapRequestOutputType.ChartIndex))
                    {
                        var rasterizer = lightmappingContext.ChartRasterizer;

                        cmd.SetRenderTarget(lightmappingContext.AccumulatedOutput);
                        cmd.ClearRenderTarget(false, true, new Color(-1, -1, -1, -1)); // -1 = No chart

                        // Keep track of how many charts we have rasterized, so we can ensure uniqueness across the entire lightmap.
                        uint chartIndexOffset = 0;

                        foreach (var bakeInstance in currentLightmapDesc.BakeInstances)
                        {
                            if (!GetInstanceUVResources(cmd, lightmappingContext, bakeInstance, out var uvMesh, out _, out _))
                                return Result.AddResourcesToCacheFailure;

                            var vertexToChartId = chartIdentification.CompleteAndGetResult(bakeInstance.Mesh);
                            cmd.SetBufferData(lightmappingContext.ChartRasterizerBuffers.vertexToChartID, vertexToChartId.VertexChartIndices);

                            ChartRasterizer.PrepareRasterizeSoftware(cmd, uvMesh.Mesh,
                                lightmappingContext.ChartRasterizerBuffers.vertex, lightmappingContext.ChartRasterizerBuffers.vertexToOriginalVertex);
                            rasterizer.RasterizeSoftware(cmd, lightmappingContext.ChartRasterizerBuffers.vertex,
                                lightmappingContext.ChartRasterizerBuffers.vertexToOriginalVertex, lightmappingContext.ChartRasterizerBuffers.vertexToChartID,
                                uvMesh.Mesh.GetTotalIndexCount(), bakeInstance.NormalizedOccupiedST, chartIndexOffset, lightmappingContext.AccumulatedOutput);

                            chartIndexOffset += vertexToChartId.ChartCount;
                        }

                        // Readback texture
                        Vector4[] pixels = null;
                        cmd.RequestAsyncReadback(lightmappingContext.AccumulatedOutput, 0, request => pixels = request.GetData<Vector4>().ToArray());
                        cmd.WaitAllAsyncReadbackRequests();

                        GraphicsHelpers.Flush(cmd);

                        // Write it to disk
                        int[] chartIndices = new int[pixels.Length];
                        for (int i = 0; i < pixels.Length; i++)
                            chartIndices[i] = (int)pixels[i].x;
                        if (!SaveArrayToFile(request.outputFolderPath + $"/chartIndex{lightmapIndex}.int", chartIndices.Length, chartIndices))
                            return Result.WriteToDiskFailure;

                        ulong workSteps = CalculateNonIntegratedLightmapWorkSteps((uint)lightmapSize);
                        progressState.IncrementCompletedWorkSteps(workSteps);
                    }

                    // Compute pixel overlap indices
                    if (request.outputTypeMask.HasFlag(LightmapRequestOutputType.OverlapPixelIndex))
                    {
                        using var overlapDetection = new UVOverlapDetection();
                        overlapDetection.Initialize(UVOverlapDetection.LoadShader(), (uint)resolution, maxIndexCount, (uint)fatInstances.Length);

                        // Mark overlaps in every instance. Keep track of how many charts we have processed,
                        // so we can ensure uniqueness across the entire lightmap.
                        uint chartIndexOffset = 0;
                        foreach (var bakeInstance in currentLightmapDesc.BakeInstances)
                        {
                            if (!GetInstanceUVResources(cmd, lightmappingContext, bakeInstance, out var uvMesh, out _, out _))
                                return Result.AddResourcesToCacheFailure;

                            var vertexToChartId = chartIdentification.CompleteAndGetResult(bakeInstance.Mesh);

                            cmd.BeginSample("UV Overlap Detection");
                            overlapDetection.MarkOverlapsInInstance(
                                cmd,
                                uvMesh.Mesh,
                                vertexToChartId.VertexChartIndicesIgnoringNormals,
                                bakeInstance.NormalizedOccupiedST,
                                bakeInstance.InstanceIndex,
                                chartIndexOffset);
                            cmd.EndSample("UV Overlap Detection");

                            chartIndexOffset += vertexToChartId.ChartCount;
                        }

                        // Readback result, write to disk
                        overlapDetection.CompactAndReadbackOverlaps(
                            cmd,
                            out var uniqueOverlapPixelIndices,
                            out var uniqueOverlapInstanceIndices);

                        SaveArrayToFile(request.outputFolderPath + $"/overlapPixelIndex{lightmapIndex}.uint", uniqueOverlapPixelIndices.Length, uniqueOverlapPixelIndices);
                        SaveArrayToFile(request.outputFolderPath + $"/uvOverlapInstanceIds{lightmapIndex}.size_t", uniqueOverlapInstanceIndices.Length, uniqueOverlapInstanceIndices);

                        ulong workSteps = CalculateNonIntegratedLightmapWorkSteps((uint)lightmapSize);
                        progressState.IncrementCompletedWorkSteps(workSteps);
                    }

                    // Bake all the integrator components requests
                    foreach (var integratedRequestOutputType in integratedRequestOutputs)
                    {
                        if (progressState.WasCancelled())
                            return Result.Cancelled;

                        lightmappingContext.ClearOutputs();

                        IntegratedOutputType integratedOutputType = integratedRequestOutputType.Key;
                        foreach (var bakeInstance in currentLightmapDesc.BakeInstances)
                        {
                            if (!GetInstanceUVResources(cmd, lightmappingContext, bakeInstance, out _, out var uvAS, out var uvFallbackBuffer))
                                return Result.AddResourcesToCacheFailure;

                            var result = IntegrateLightmapInstance(
                                cmd,
                                lightmapIndex,
                                integratedRequestOutputType.Key,
                                bakeInstance,
                                lodInstances,
                                lodgroupToContributorInstances,
                                lightmappingContext,
                                uvAS,
                                uvFallbackBuffer,
                                integrationSettings,
                                lightmapBakeSettings,
                                doDirectionality,
                                progressState,
                                gpuSync,
                                debugDispatches
                            );

                            if (result != Result.Success)
                                return result;
                        }

                        // Copy out the results
                        bool hasDirectionality = integratedRequestOutputType.Value.HasFlag(RequestedSubOutput.DirectionalityTexture);
                        if (hasDirectionality)
                        {
                            Debug.Assert(normalBuffer != null, "Normal buffer must be set when directionality is requested.");
                            Debug.Assert(needsNormalsForDirectionality);

                            // This uses the accumulatedOutput to get the sample count, so directionality must be normalized first since the sample count will be normalized when the lightmap is normalized
                            // Currently the directional output from LightBaker is not normalized - so for now we don't do it here either. JIRA: https://jira.unity3d.com/browse/LIGHT-1814
                            switch (integratedOutputType)
                            {
                                case IntegratedOutputType.Direct:
                                    lightmappingContext.IntegratorContext.LightmapDirectIntegrator.NormalizeDirectional(cmd, lightmappingContext.AccumulatedDirectionalOutput, accumulatedOutput, normalBuffer);
                                    break;
                                case IntegratedOutputType.Indirect:
                                    lightmappingContext.IntegratorContext.LightmapIndirectIntegrator.NormalizeDirectional(cmd, lightmappingContext.AccumulatedDirectionalOutput, accumulatedOutput, normalBuffer);
                                    break;
                            }
                        }

                        switch (integratedOutputType)
                        {
                            case IntegratedOutputType.AO:
                                lightmappingContext.IntegratorContext.LightmapAOIntegrator.Normalize(cmd, accumulatedOutput);
                                break;
                            case IntegratedOutputType.Direct:
                                lightmappingContext.IntegratorContext.LightmapDirectIntegrator.Normalize(cmd, accumulatedOutput);
                                break;
                            case IntegratedOutputType.Indirect:
                                lightmappingContext.IntegratorContext.LightmapIndirectIntegrator.Normalize(cmd, accumulatedOutput);
                                break;
                            case IntegratedOutputType.Validity:
                                lightmappingContext.IntegratorContext.LightmapValidityIntegrator.Normalize(cmd, accumulatedOutput);
                                break;
                            case IntegratedOutputType.ShadowMask:
                                lightmappingContext.IntegratorContext.LightmapShadowMaskIntegrator.Normalize(cmd, accumulatedOutput, lightmappingContext.AccumulatedDirectionalOutput);
                                break;
                        }

                        // Make sure all the work is done before writing to disk
                        GraphicsHelpers.Flush(cmd);

                        if (debugDispatches)
                        {
                            gpuSync.Sync(cmd);
                            Console.WriteLine($"Write {integratedOutputType} lightmap {lightmapIndex} to disk.");
                            //LightmapIntegrationHelpers.WriteRenderTexture(cmd, $"Temp/lm{lightmapIndex}_type{lightmapType}_final.r2dr", lightmappingContext.AccumulatedOutput, lightmappingContext.AccumulatedOutput.width, lightmappingContext.AccumulatedOutput.height);
                        }

                        // Write lightmap to disk. TODO: kick off a C# job so the compression and file IO happens in a different thread LIGHT-1772
                        bool outputPrimaryTexture = integratedRequestOutputType.Value.HasFlag(RequestedSubOutput.PrimaryTexture);
                        // We explicitly take into account whether to write for directionality, no directionality or for both.
                        if (outputPrimaryTexture)
                            if (LightmapIntegrationHelpers.WriteLightmap(cmd, accumulatedOutput, integratedOutputType, lightmapIndex, request.outputFolderPath) == false)
                                return Result.WriteToDiskFailure;

                        if (!integratedRequestOutputType.Value.HasFlag(RequestedSubOutput.DirectionalityTexture))
                            continue;

                        // Write 'direct/indirect directionality' if requested - it is baked in the same pass as 'direct/indirect'.
                        IntegratedOutputType directionalityLightmapType =
                            integratedOutputType == IntegratedOutputType.Direct ? IntegratedOutputType.DirectionalityDirect : IntegratedOutputType.DirectionalityIndirect;
                        if (LightmapIntegrationHelpers.WriteLightmap(cmd, lightmappingContext.AccumulatedDirectionalOutput, directionalityLightmapType, lightmapIndex, request.outputFolderPath) == false)
                            return Result.WriteToDiskFailure;
                    }

                    // Get rid of the normal output
                    if (normalBuffer is not null)
                        normalBuffer.Release();
                    CoreUtils.Destroy(normalBuffer);

                    // Write black outputs for the lightmap components we didn't bake. LightBaker does this too.
                    Texture2D blackOutput = new Texture2D(resolution, resolution, TextureFormat.RGBAFloat, false);
                    blackOutput.name = "BlackOutput";
                    blackOutput.hideFlags = HideFlags.HideAndDontSave;
                    blackOutput.SetPixels(new Color[resolution * resolution]);
                    blackOutput.Apply();
                    byte[] compressedBlack = blackOutput.EncodeToR2D();
                    try
                    {
                        // Environment is part of indirect irradiance.
                        if (request.outputTypeMask.HasFlag(LightmapRequestOutputType.IrradianceEnvironment))
                            File.WriteAllBytes(request.outputFolderPath + $"/irradianceEnvironment{lightmapIndex}.r2d", compressedBlack);

                        // occupiedTexels is needed for analytics, don't fail the bake if we cannot write it.
                        UInt64 occupiedTexels = (UInt64)lightmapRequestData.atlassing.m_EstimatedTexelCount;
                        if (request.outputTypeMask.HasFlag(LightmapRequestOutputType.Occupancy))
                            File.WriteAllBytes(request.outputFolderPath + $"/occupiedTexels{lightmapIndex}.UInt64", BitConverter.GetBytes(occupiedTexels));
                    }
                    catch (Exception e)
                    {
                        CoreUtils.Destroy(blackOutput);
                        Debug.Assert(false, e.Message);
                        return Result.WriteToDiskFailure;
                    }
                    CoreUtils.Destroy(blackOutput);
                }
            }
            return Result.Success;
        }

        private static Dictionary<IntegratedOutputType, RequestedSubOutput> GetRequestedIntegratedOutputTypes(in LightmapRequest request)
        {
            // TODO handle lightmapOffset and lightmapCount from the request.
            // Turn the bits set in the request bitmask into an array of LightmapRequestOutputType.
            HashSet<LightmapRequestOutputType> lightmapRequestOutputTypes = BitfieldToList((int)request.outputTypeMask);

            // Make a dictionary of the lightmap types that are requested, the value indicates if directionality is requested for that stage (when it makes sense).
            // This accounts for the fact that for direct and indirect directionality is baked in the same pass. But for purposes of deciding what lightmap output
            // files to write we need to store that information as a bit flag.
            Dictionary<IntegratedOutputType, RequestedSubOutput> requestedLightmapTypes = new();
            void AddToRequestedLightmapTypes(IntegratedOutputType lightmapType, RequestedSubOutput type)
            {
                if (!requestedLightmapTypes.TryAdd(lightmapType, type))
                    requestedLightmapTypes[lightmapType] |= type;
            }

            foreach (var lightmapRequestOutputType in lightmapRequestOutputTypes)
            {
                if (!LightmapRequestOutputTypeToIntegratedOutputType(lightmapRequestOutputType, out IntegratedOutputType lightmapType))
                    continue;

                switch (lightmapType)
                {
                    case IntegratedOutputType.Indirect:
                        AddToRequestedLightmapTypes(IntegratedOutputType.Indirect, RequestedSubOutput.PrimaryTexture);
                        break;
                    case IntegratedOutputType.DirectionalityIndirect:
                        AddToRequestedLightmapTypes(IntegratedOutputType.Indirect, RequestedSubOutput.DirectionalityTexture);
                        break;
                    case IntegratedOutputType.Direct:
                        AddToRequestedLightmapTypes(IntegratedOutputType.Direct, RequestedSubOutput.PrimaryTexture);
                        break;
                    case IntegratedOutputType.DirectionalityDirect:
                        AddToRequestedLightmapTypes(IntegratedOutputType.Direct, RequestedSubOutput.DirectionalityTexture);
                        break;
                    default:
                        AddToRequestedLightmapTypes(lightmapType, RequestedSubOutput.PrimaryTexture);
                        break;
                }
            }

            return requestedLightmapTypes;
        }

        private static HashSet<LightmapRequestOutputType> GetRequestedNonIntegratedOutputTypes(in LightmapRequest request)
        {
            HashSet<LightmapRequestOutputType> outputTypes =
                BitfieldToList((int) (request.outputTypeMask & (LightmapRequestOutputType.Normal | LightmapRequestOutputType.Occupancy | LightmapRequestOutputType.ChartIndex | LightmapRequestOutputType.OverlapPixelIndex)));
            bool needsNormalsForDirectionality = request.outputTypeMask.HasFlag(LightmapRequestOutputType.DirectionalityIndirect) || request.outputTypeMask.HasFlag(LightmapRequestOutputType.DirectionalityDirect);
            if (needsNormalsForDirectionality)
                outputTypes.Add(LightmapRequestOutputType.Normal);

            return outputTypes;
        }

        private static (uint directSampleCount, uint effectiveIndirectSampleCount) GetProbeSampleCounts(in SampleCount probeSampleCounts)
        {
            uint indirectSampleCount = probeSampleCounts.indirectSampleCount;

            return (probeSampleCounts.directSampleCount, indirectSampleCount);
        }

        internal static bool ExecuteProbeRequests(in BakeInput bakeInput, in ProbeRequestData probeRequestData, UnityComputeDeviceContext deviceContext,
            bool useLegacyBakingBehavior, UnityComputeWorld world, BakeProgressState progressState,
            UnityEngine.Rendering.Sampling.SamplingResources samplingResources)
        {
            if (probeRequestData.requests.Length == 0)
                return true;

            ProbeIntegratorResources integrationResources = new();
            integrationResources.Load(world.RayTracingContext);

            var probeOcclusionLightIndexMappingShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.unity.render-pipelines.core/Runtime/PathTracing/Shaders/ProbeOcclusionLightIndexMapping.compute");
            using UnityComputeProbeIntegrator probeIntegrator = new(!useLegacyBakingBehavior, samplingResources, integrationResources, probeOcclusionLightIndexMappingShader);
            probeIntegrator.SetProgressReporter(progressState);

            // Create input position buffer
            using NativeArray<float3> inputPositions = new(probeRequestData.positions, Allocator.Temp);
            var positionsBuffer = deviceContext.CreateBuffer((ulong)probeRequestData.positions.Length, sizeof(float) * 3);
            BufferSlice<float3> positionsBufferSlice = positionsBuffer.Slice<float3>();
            var positionsWriteEvent = deviceContext.CreateEvent();
            deviceContext.WriteBuffer(positionsBufferSlice, inputPositions, positionsWriteEvent);
            deviceContext.DestroyEvent(positionsWriteEvent);

            // Create input probe occlusion light indices buffer (shared for all requests)
            using NativeArray<int> inputPerProbeLightIndices = new(probeRequestData.occlusionLightIndices, Allocator.Temp);
            var perProbeLightIndicesBuffer = deviceContext.CreateBuffer((ulong)probeRequestData.occlusionLightIndices.Length, sizeof(int));
            BufferSlice<int> perProbeLightIndicesBufferSlice = perProbeLightIndicesBuffer.Slice<int>();
            deviceContext.WriteBuffer(perProbeLightIndicesBufferSlice, inputPerProbeLightIndices);

            ProbeRequest[] probeRequests = probeRequestData.requests;
            for (int probeRequestIndex = 0; probeRequestIndex < probeRequests.Length; probeRequestIndex++)
            {
                // Read data from request and prepare integrator
                ref readonly ProbeRequest request = ref probeRequests[probeRequestIndex];
                int requestOffset = (int)request.positionOffset;
                int requestLength = (int)request.count;
                ulong floatBufferSize = sizeof(float) * request.count;
                float pushoff = request.pushoff;
                int bounceCount = (int)request.maxBounces;
                (uint directSampleCount, uint effectiveIndirectSampleCount) = GetProbeSampleCounts(request.sampleCount);

                probeIntegrator.Prepare(deviceContext, world, positionsBuffer.Slice<Vector3>(), pushoff, (int)bounceCount);

                List<EventID> eventsToWaitFor = new();
                List<BufferID> buffersToDestroy = new();

                // Integrate indirect radiance
                using NativeArray<SphericalHarmonicsL2> outputIndirectRadiance = new(requestLength, Allocator.Persistent);
                if (request.outputTypeMask.HasFlag(ProbeRequestOutputType.RadianceIndirect) && effectiveIndirectSampleCount > 0)
                {
                    var shIndirectBuffer = deviceContext.CreateBuffer(request.count * 27, sizeof(float));
                    buffersToDestroy.Add(shIndirectBuffer);
                    var shIndirectBufferSlice = shIndirectBuffer.Slice<SphericalHarmonicsL2>();
                    var integrationResult = probeIntegrator.IntegrateIndirectRadiance(deviceContext, requestOffset, requestLength,
                        (int)effectiveIndirectSampleCount, request.ignoreIndirectEnvironment, shIndirectBufferSlice);
                    Assert.AreEqual(IProbeIntegrator.ResultType.Success, integrationResult.type, "IntegrateIndirectRadiance failed.");
                    var readEvent = deviceContext.CreateEvent();
                    deviceContext.ReadBuffer(shIndirectBufferSlice, outputIndirectRadiance, readEvent);
                    eventsToWaitFor.Add(readEvent);
                }

                // Integrate direct radiance
                using NativeArray<SphericalHarmonicsL2> outputDirectRadiance = new(requestLength, Allocator.Persistent);
                if (request.outputTypeMask.HasFlag(ProbeRequestOutputType.RadianceDirect) && directSampleCount > 0)
                {
                    var shDirectBuffer = deviceContext.CreateBuffer(request.count * 27, sizeof(float));
                    buffersToDestroy.Add(shDirectBuffer);
                    var shDirectBufferSlice = shDirectBuffer.Slice<SphericalHarmonicsL2>();
                    var integrationResult = probeIntegrator.IntegrateDirectRadiance(deviceContext, requestOffset, requestLength,
                        (int)directSampleCount, request.ignoreDirectEnvironment, shDirectBufferSlice);
                    Assert.AreEqual(IProbeIntegrator.ResultType.Success, integrationResult.type, "IntegrateDirectRadiance failed.");
                    var readEvent = deviceContext.CreateEvent();
                    deviceContext.ReadBuffer(shDirectBufferSlice, outputDirectRadiance, readEvent);
                    eventsToWaitFor.Add(readEvent);
                }

                // Integrate validity
                using NativeArray<float> outputValidity = new(requestLength, Allocator.Persistent);
                if (request.outputTypeMask.HasFlag(ProbeRequestOutputType.Validity) && effectiveIndirectSampleCount > 0)
                {
                    var validityBuffer = deviceContext.CreateBuffer(request.count, sizeof(float));
                    buffersToDestroy.Add(validityBuffer);
                    var validityBufferSlice = validityBuffer.Slice<float>();
                    var integrationResult = probeIntegrator.IntegrateValidity(deviceContext, requestOffset, requestLength,
                        (int)effectiveIndirectSampleCount, validityBufferSlice);
                    Assert.AreEqual(IProbeIntegrator.ResultType.Success, integrationResult.type, "IntegrateValidity failed.");
                    var readEvent = deviceContext.CreateEvent();
                    deviceContext.ReadBuffer(validityBufferSlice, outputValidity, readEvent);
                    eventsToWaitFor.Add(readEvent);
                }

                // Integrate occlusion values
                const int maxOcclusionLightsPerProbe = 4;
                bool usesProbeOcclusion = bakeInput.lightingSettings.mixedLightingMode != MixedLightingMode.IndirectOnly;
                using NativeArray<float> outputOcclusion = new(requestLength * maxOcclusionLightsPerProbe, Allocator.Persistent);
                if (request.outputTypeMask.HasFlag(ProbeRequestOutputType.LightProbeOcclusion) && usesProbeOcclusion && effectiveIndirectSampleCount > 0)
                {
                    var occlusionBuffer = deviceContext.CreateBuffer(maxOcclusionLightsPerProbe * request.count, sizeof(float));
                    buffersToDestroy.Add(occlusionBuffer);
                    var occlusionBufferSlice = occlusionBuffer.Slice<float>();

                    var integrationResult = probeIntegrator.IntegrateOcclusion(deviceContext, requestOffset, requestLength,
                        (int)effectiveIndirectSampleCount, maxOcclusionLightsPerProbe, perProbeLightIndicesBufferSlice, occlusionBufferSlice);
                    Assert.AreEqual(IProbeIntegrator.ResultType.Success, integrationResult.type, "IntegrateOcclusion failed.");

                    EventID readEvent = deviceContext.CreateEvent();
                    deviceContext.ReadBuffer(occlusionBufferSlice, outputOcclusion, readEvent);
                    eventsToWaitFor.Add(readEvent);
                }

                // Gather occlusion containers / indices
                var outputOcclusionIndices = new LightProbeOcclusion[requestLength];
                for (int probeIdx = 0; probeIdx < requestLength; probeIdx++)
                {
                    ref var occlusion = ref outputOcclusionIndices[probeIdx];
                    occlusion.SetDefaultValues();
                }
                if (request.outputTypeMask.HasFlag(ProbeRequestOutputType.MixedLightOcclusion) && usesProbeOcclusion)
                {
                    // Build output data
                    for (int probeIdx = 0; probeIdx < requestLength; probeIdx++)
                    {
                        LightProbeOcclusion occlusion = new();
                        occlusion.SetDefaultValues();
                        for (int indirectLightIdx = 0; indirectLightIdx < maxOcclusionLightsPerProbe; indirectLightIdx++)
                        {
                            int bakeInputLightIdx = inputPerProbeLightIndices[probeIdx * maxOcclusionLightsPerProbe + indirectLightIdx];
                            if (bakeInputLightIdx >= 0)
                            {
                                sbyte occlusionMaskChannel = (sbyte)bakeInput.lightData[bakeInputLightIdx].shadowMaskChannel;

                                occlusion.SetProbeOcclusionLightIndex(indirectLightIdx, bakeInputLightIdx);
                                occlusion.SetOcclusion(indirectLightIdx, 0.0f);
                                occlusion.SetOcclusionMaskChannel(indirectLightIdx, occlusionMaskChannel);
                            }
                        }
                        outputOcclusionIndices[probeIdx] = occlusion;
                    }
                }

                // Flush and wait for all events to complete
                deviceContext.Flush();
                foreach (var evt in eventsToWaitFor)
                {
                    bool ok = deviceContext.Wait(evt);
                    Assert.IsTrue(ok);
                    deviceContext.DestroyEvent(evt);
                }

                // Cleanup temporary buffers
                foreach (var buffer in buffersToDestroy)
                {
                    deviceContext.DestroyBuffer(buffer);
                }

                // Write output data to disk
                // Write light probe data to disk, so the C++ side post processing stage can pick it up.
                // We can use the C# side post processing API in the future, once we can write the LDA from C#.
                string path = request.outputFolderPath;
                if (request.outputTypeMask.HasFlag(ProbeRequestOutputType.RadianceDirect))
                {
                    string filePath = path;
                    filePath += string.Format("/radianceDirect{0}.shl2", probeRequestIndex);
                    if (!SaveProbesToFileInterleaved(filePath, outputDirectRadiance))
                        return false;
                }
                if (request.outputTypeMask.HasFlag(ProbeRequestOutputType.RadianceIndirect))
                {
                    string filePath = path;
                    filePath += string.Format("/radianceIndirect{0}.shl2", probeRequestIndex);
                    if (!SaveProbesToFileInterleaved(filePath, outputIndirectRadiance))
                        return false;
                }
                if (request.outputTypeMask.HasFlag(ProbeRequestOutputType.Validity))
                {
                    string filePath = path;
                    filePath += string.Format("/validity{0}.float", probeRequestIndex);
                    if (!SaveArrayToFile(filePath, requestLength, outputValidity.ToArray()))
                        return false;
                }
                if (request.outputTypeMask.HasFlag(ProbeRequestOutputType.LightProbeOcclusion))
                {
                    string filePath = path;
                    filePath += string.Format("/lightProbeOcclusion{0}.vec4", probeRequestIndex);
                    if (!SaveArrayToFile(filePath, requestLength, outputOcclusion.ToArray()))
                        return false;
                }
                if (request.outputTypeMask.HasFlag(ProbeRequestOutputType.MixedLightOcclusion))
                {
                    // TODO(pema.malling): dummy data, the C++ side post processing stage needs this.
                    // https://jira.unity3d.com/browse/LIGHT-1102
                    string filePath = path;
                    filePath += string.Format("/mixedLightOcclusion{0}.occ", probeRequestIndex);
                    if (!SaveArrayToFile(filePath, requestLength, outputOcclusionIndices))
                        return false;
                }
            }

            // Cleanup created buffers
            deviceContext.DestroyBuffer(positionsBuffer);
            deviceContext.DestroyBuffer(perProbeLightIndicesBuffer);

            return true;
        }
    }
}
