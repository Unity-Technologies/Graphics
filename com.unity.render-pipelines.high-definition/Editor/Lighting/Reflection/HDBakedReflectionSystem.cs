using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Experimental.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    unsafe class HDBakedReflectionSystem : ScriptableBakedReflectionSystem
    {
        struct HDProbeBakingState
        {
            public struct ProbeBakingHash : CoreUnsafeUtils.IKeyGetter<HDProbeBakingState, Hash128>
            { public Hash128 Get(ref HDProbeBakingState v) { return v.probeBakingHash; } }

            public int instanceID;
            public Hash128 probeSettingsHash;
            public Hash128 probeBakingHash;
        }

        struct HDProbeBakedState
        {
            public struct ProbeBakedHash : CoreUnsafeUtils.IKeyGetter<HDProbeBakedState, Hash128>
            { public Hash128 Get(ref HDProbeBakedState v) { return v.probeBakedHash; } }

            public int instanceID;
            public Hash128 probeBakedHash;
        }

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            if (GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset)
                ScriptableBakedReflectionSystemSettings.system = new HDBakedReflectionSystem();
        }

        enum BakingStages
        {
            ReflectionProbes
        }

        Hash128[] m_StateHashes;
        HDProbeBakedState[] m_HDProbeBakedStates = new HDProbeBakedState[0];
        float m_DateSinceLastLegacyWarning = float.MinValue;
        Dictionary<UnityEngine.Rendering.RenderPipeline, float> m_DateSinceLastInvalidSRPWarning
            = new Dictionary<UnityEngine.Rendering.RenderPipeline, float>();

        HDBakedReflectionSystem() : base(1)
        {
        }

        public override bool BakeAllReflectionProbes()
        {
            if (!AreAllOpenedSceneSaved())
                return false;

            DeleteCubemapAssets(true);
            var bakedProbes = HDProbeSystem.bakedProbes;

            return BakeProbes(bakedProbes);
        }

        public override void Clear()
        {
            if (!AreAllOpenedSceneSaved())
                return;

            DeleteCubemapAssets(false);
        }

        public override void Tick(
            SceneStateHash sceneStateHash,
            IScriptableBakedReflectionSystemStageNotifier handle
        )
        {
            if (!AreAllOpenedSceneSaved())
            {
                handle.SetIsDone(true);
                return;
            }

            // On the C# side, we don't have non blocking asset import APIs, and we don't want to block the
            //   UI when the user is editing the world.
            //   So, we skip the baking when the user is editing any UI control.
            if (GUIUtility.hotControl != 0)
                return;

            if (!IsCurrentSRPValid(out HDRenderPipeline hdPipeline))
            {
                if (ShouldIssueWarningForCurrentSRP())
                    Debug.LogWarning("HDBakedReflectionSystem work with HDRP, " +
                        "Either switch your render pipeline or use a different reflection system. You may need to trigger a " +
                        "C# domain reload to initialize the appropriate reflection system. One way to do this is to compile a script.");

                handle.ExitStage((int)BakingStages.ReflectionProbes);
                handle.SetIsDone(true);
                return;
            }

            var ambientProbeHash = sceneStateHash.ambientProbeHash;
            var sceneObjectsHash = sceneStateHash.sceneObjectsHash;
            var skySettingsHash = sceneStateHash.skySettingsHash;

            DeleteCubemapAssets(true);

            // Explanation of the algorithm:
            // 1. First we create the hash of the world that can impact the reflection probes.
            // 2. Then for each probe, we calculate a hash that represent what this specific probe should have baked.
            // 3. We compare those hashes against the baked one and decide:
            //   a. If we have to remove a baked data
            //   b. If we have to bake a probe
            // 4. Bake all required probes
            //   a. Bake probe that were added or modified
            //   b. Bake probe with a missing baked texture
            // 5. Remove unused baked data
            // 6. Update probe assets

            // == 1. ==
            var allProbeDependencyHash = new Hash128();
            // TODO: All baked probes depend on custom probes (hash all custom probes and set as dependency)
            // TODO: All baked probes depend on HDRP specific Light settings
            HashUtilities.AppendHash(ref ambientProbeHash, ref allProbeDependencyHash);
            HashUtilities.AppendHash(ref sceneObjectsHash, ref allProbeDependencyHash);
            HashUtilities.AppendHash(ref skySettingsHash, ref allProbeDependencyHash);

            var bakedProbes = HDProbeSystem.bakedProbes;
            var bakedProbeCount = HDProbeSystem.bakedProbeCount;

            // == 2. ==
            var states = stackalloc HDProbeBakingState[bakedProbeCount];
            // A list of indices of probe we may want to force to rebake, even if the hashes matches.
            // Usually, add a probe when something external to its state or the world state forces the bake.
            var probeForcedToBakeIndices = stackalloc int[bakedProbeCount];
            var probeForcedToBakeIndicesCount = 0;
            var probeForcedToBakeIndicesList = new ListBuffer<int>(
                probeForcedToBakeIndices,
                &probeForcedToBakeIndicesCount,
                bakedProbeCount
            );

            ComputeProbeInstanceID(bakedProbes, states);
            ComputeProbeSettingsHashes(bakedProbes, states);
            // TODO: Handle bounce dependency here
            ComputeProbeBakingHashes(bakedProbeCount, allProbeDependencyHash, states);

            // Force to rebake probe with missing baked texture
            for (var i = 0; i < bakedProbeCount; ++i)
            {
                var instanceId = states[i].instanceID;
                var probe = (HDProbe)EditorUtility.InstanceIDToObject(instanceId);
                if (probe.bakedTexture != null && !probe.bakedTexture.Equals(null)) continue;

                probeForcedToBakeIndicesList.TryAdd(i);
            }

            CoreUnsafeUtils.QuickSort<HDProbeBakingState, Hash128, HDProbeBakingState.ProbeBakingHash>(
                bakedProbeCount, states
            );

            int operationCount = 0, addCount = 0, remCount = 0;
            var maxProbeCount = Mathf.Max(bakedProbeCount, m_HDProbeBakedStates.Length);
            var addIndices = stackalloc int[maxProbeCount];
            var remIndices = stackalloc int[maxProbeCount];

            if (m_HDProbeBakedStates.Length == 0)
            {
                for (int i = 0; i < bakedProbeCount; ++i)
                    addIndices[addCount++] = i;
                operationCount = addCount;
            }
            else
            {
                fixed(HDProbeBakedState* oldBakedStates = &m_HDProbeBakedStates[0])
                {
                    // == 3. ==
                    // Compare hashes between baked probe states and desired probe states
                    operationCount = CoreUnsafeUtils.CompareHashes<
                        HDProbeBakedState, HDProbeBakedState.ProbeBakedHash,
                        HDProbeBakingState, HDProbeBakingState.ProbeBakingHash
                        >(
                            m_HDProbeBakedStates.Length, oldBakedStates, // old hashes
                            bakedProbeCount, states,              // new hashes
                            addIndices, remIndices,
                            out addCount, out remCount
                        );
                }
            }

            if (operationCount > 0 || probeForcedToBakeIndicesList.Count > 0)
            {
                // == 4. ==
                var cubemapSize = (int)hdPipeline.currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionCubemapSize;
                // We force RGBAHalf as we don't support 11-11-10 textures (only RT)
                var probeFormat = GraphicsFormat.R16G16B16A16_SFloat;

                var cubeRT = HDRenderUtilities.CreateReflectionProbeRenderTarget(cubemapSize, probeFormat);

                handle.EnterStage(
                    (int)BakingStages.ReflectionProbes,
                    string.Format("Reflection Probes | {0} jobs", addCount),
                    0
                );

                // Compute indices of probes to bake: added, modified probe or with a missing baked texture.
                var toBakeIndices = stackalloc int[bakedProbeCount];
                var toBakeIndicesCount = 0;
                var toBakeIndicesList = new ListBuffer<int>(toBakeIndices, &toBakeIndicesCount, bakedProbeCount);
                {
                    // Note: we will add probes from change check and baked texture missing check.
                    //   So we can add at most 2 time the probe in the list.
                    var toBakeIndicesTmp = stackalloc int[bakedProbeCount * 2];
                    var toBakeIndicesTmpCount = 0;
                    var toBakeIndicesTmpList =
                        new ListBuffer<int>(toBakeIndicesTmp, &toBakeIndicesTmpCount, bakedProbeCount * 2);

                    // Add the indices from the added or modified detection check
                    toBakeIndicesTmpList.TryCopyFrom(addIndices, addCount);
                    // Add the probe with missing baked texture check
                    probeForcedToBakeIndicesList.TryCopyTo(toBakeIndicesTmpList);

                    // Sort indices
                    toBakeIndicesTmpList.QuickSort();
                    // Add to final list without the duplicates
                    var lastValue = int.MaxValue;
                    for (var i = 0; i < toBakeIndicesTmpList.Count; ++i)
                    {
                        if (lastValue == toBakeIndicesTmpList.GetUnchecked(i))
                            // Skip duplicates
                            continue;

                        lastValue = toBakeIndicesTmpList.GetUnchecked(i);
                        toBakeIndicesList.TryAdd(lastValue);
                    }
                }

                // Render probes that were added or modified
                for (int i = 0; i < toBakeIndicesList.Count; ++i)
                {
                    handle.EnterStage(
                        (int)BakingStages.ReflectionProbes,
                        string.Format("Reflection Probes | {0} jobs", addCount),
                        i / (float)toBakeIndicesCount
                    );

                    var index = toBakeIndicesList.GetUnchecked(i);
                    var instanceId = states[index].instanceID;
                    var probe = (HDProbe)EditorUtility.InstanceIDToObject(instanceId);
                    var cacheFile = GetGICacheFileForHDProbe(states[index].probeBakingHash);

                    // Get from cache or render the probe
                    if (!File.Exists(cacheFile))
                    {
                        var planarRT = HDRenderUtilities.CreatePlanarProbeRenderTarget((int)probe.resolution, probeFormat);
                        RenderAndWriteToFile(probe, cacheFile, cubeRT, planarRT);
                        planarRT.Release();
                    }
                }
                cubeRT.Release();

                // Copy texture from cache
                for (int i = 0; i < toBakeIndicesList.Count; ++i)
                {
                    var index = toBakeIndicesList.GetUnchecked(i);
                    var instanceId = states[index].instanceID;
                    var probe = (HDProbe)EditorUtility.InstanceIDToObject(instanceId);
                    var cacheFile = GetGICacheFileForHDProbe(states[index].probeBakingHash);

                    Assert.IsTrue(File.Exists(cacheFile));

                    var bakedTexturePath = HDBakingUtilities.GetBakedTextureFilePath(probe);
                    HDBakingUtilities.CreateParentDirectoryIfMissing(bakedTexturePath);
                    Checkout(bakedTexturePath);
                    // Checkout will make those file writeable, but this is not immediate,
                    // so we retries when this fails.
                    if (!HDEditorUtils.CopyFileWithRetryOnUnauthorizedAccess(cacheFile, bakedTexturePath))
                        return;
                }
                // AssetPipeline bug
                // Sometimes, the baked texture reference is destroyed during 'AssetDatabase.StopAssetEditing()'
                //   thus, the reference to the baked texture in the probe is lost
                // Although, importing twice the texture seems to workaround the issue
                for (int j = 0; j < 2; ++j)
                {
                    AssetDatabase.StartAssetEditing();
                    for (int i = 0; i < bakedProbeCount; ++i)
                    {
                        var index = toBakeIndicesList.GetUnchecked(i);
                        var instanceId = states[index].instanceID;
                        var probe = (HDProbe)EditorUtility.InstanceIDToObject(instanceId);
                        var bakedTexturePath = HDBakingUtilities.GetBakedTextureFilePath(probe);
                        AssetDatabase.ImportAsset(bakedTexturePath);
                        ImportAssetAt(probe, bakedTexturePath);
                    }
                    AssetDatabase.StopAssetEditing();
                }
                // Import assets
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < toBakeIndicesList.Count; ++i)
                {
                    var index = toBakeIndicesList.GetUnchecked(i);
                    var instanceId = states[index].instanceID;
                    var probe = (HDProbe)EditorUtility.InstanceIDToObject(instanceId);
                    var bakedTexturePath = HDBakingUtilities.GetBakedTextureFilePath(probe);
                    var bakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(bakedTexturePath);
                    Assert.IsNotNull(bakedTexture, "The baked texture was imported before, " +
                        "so it must exists in AssetDatabase");

                    probe.SetTexture(ProbeSettings.Mode.Baked, bakedTexture);
                    EditorUtility.SetDirty(probe);
                }
                AssetDatabase.StopAssetEditing();

                // == 5. ==

                // Create new baked state array
                var targetSize = m_HDProbeBakedStates.Length - remCount + toBakeIndicesList.Count;
                var targetBakedStates = stackalloc HDProbeBakedState[targetSize];
                // Copy baked state that are not removed
                var targetI = 0;
                for (int i = 0; i < m_HDProbeBakedStates.Length; ++i)
                {
                    if (CoreUnsafeUtils.IndexOf(remIndices, remCount, i) != -1)
                        continue;
                    Assert.IsTrue(targetI < targetSize);
                    targetBakedStates[targetI++] = m_HDProbeBakedStates[i];
                }
                // Add new baked states
                for (int i = 0; i < toBakeIndicesList.Count; ++i)
                {
                    var state = states[toBakeIndicesList.GetUnchecked(i)];
                    Assert.IsTrue(targetI < targetSize);
                    targetBakedStates[targetI++] = new HDProbeBakedState
                    {
                        instanceID = state.instanceID,
                        probeBakedHash = state.probeBakingHash
                    };
                }
                CoreUnsafeUtils.QuickSort<HDProbeBakedState, Hash128, HDProbeBakedState.ProbeBakedHash>(
                    targetI, targetBakedStates
                );

                Array.Resize(ref m_HDProbeBakedStates, targetSize);
                if (targetSize > 0)
                {
                    fixed(HDProbeBakedState* bakedStates = &m_HDProbeBakedStates[0])
                    {
                        UnsafeUtility.MemCpy(
                            bakedStates,
                            targetBakedStates,
                            sizeof(HDProbeBakedState) * targetSize
                        );
                    }
                }

                // Update state hash
                Array.Resize(ref m_StateHashes, m_HDProbeBakedStates.Length);
                for (int i = 0; i < m_HDProbeBakedStates.Length; ++i)
                    m_StateHashes[i] = m_HDProbeBakedStates[i].probeBakedHash;
                stateHashes = m_StateHashes;
            }

            handle.ExitStage((int)BakingStages.ReflectionProbes);

            handle.SetIsDone(true);
        }

        public static bool BakeProbes(IEnumerable<HDProbe> bakedProbes)
        {
            if (!(RenderPipelineManager.currentPipeline is HDRenderPipeline hdPipeline))
            {
                Debug.LogWarning("HDBakedReflectionSystem only works with HDRP, " +
                    "please switch your render pipeline or use another reflection probe system.");
                return false;
            }

            hdPipeline.reflectionProbeBaking = true;

            var cubemapSize = (int)hdPipeline.currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionCubemapSize;
            // We force RGBAHalf as we don't support 11-11-10 textures (only RT)
            var probeFormat = GraphicsFormat.R16G16B16A16_SFloat;

            var cubeRT = HDRenderUtilities.CreateReflectionProbeRenderTarget(cubemapSize, probeFormat);

            // Render and write the result to disk
            foreach (var probe in bakedProbes)
            {
                var bakedTexturePath = HDBakingUtilities.GetBakedTextureFilePath(probe);
                var planarRT = HDRenderUtilities.CreatePlanarProbeRenderTarget((int)probe.resolution, probeFormat);
                RenderAndWriteToFile(probe, bakedTexturePath, cubeRT, planarRT);
                planarRT.Release();
            }

            // AssetPipeline bug
            // Sometimes, the baked texture reference is destroyed during 'AssetDatabase.StopAssetEditing()'
            //   thus, the reference to the baked texture in the probe is lost
            // Although, importing twice the texture seems to workaround the issue
            for (int j = 0; j < 2; ++j)
            {
                AssetDatabase.StartAssetEditing();
                foreach (var probe in bakedProbes)
                {
                    var bakedTexturePath = HDBakingUtilities.GetBakedTextureFilePath(probe);
                    AssetDatabase.ImportAsset(bakedTexturePath);
                    ImportAssetAt(probe, bakedTexturePath);
                }
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.StartAssetEditing();
            foreach (var probe in bakedProbes)
            {
                var bakedTexturePath = HDBakingUtilities.GetBakedTextureFilePath(probe);

                // Get or create the baked texture asset for the probe
                var bakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(bakedTexturePath);
                Assert.IsNotNull(bakedTexture, "The baked texture was imported before, " +
                    "so it must exists in AssetDatabase");

                // Update import settings
                ImportAssetAt(probe, bakedTexturePath);
                probe.SetTexture(ProbeSettings.Mode.Baked, bakedTexture);
                AssignRenderData(probe, bakedTexturePath);
                EditorUtility.SetDirty(probe);
            }
            AssetDatabase.StopAssetEditing();

            // case 1158677
            // The AssetPipeline will destroy and recreate the textures
            // So all transient data will be lost after the call to `AssetDatabase.StopAssetEditing`
            // Here, we increment the updateCount to with an arbitrary number to force the reflection probe cache
            // to update the texture.
            // updateCount is a transient data, so don't execute this code before the asset reload.
            {
                UnityEngine.Random.InitState((int)(1000 * EditorApplication.timeSinceStartup));
                foreach (var probe in bakedProbes)
                {
                    var c = UnityEngine.Random.Range(2, 10);
                    while (probe.texture.updateCount < c) probe.texture.IncrementUpdateCount();
                }
            }

            cubeRT.Release();

            hdPipeline.reflectionProbeBaking = false;

            return true;
        }

        bool IsCurrentSRPValid(out HDRenderPipeline hdPipeline)
        {
            if (RenderPipelineManager.currentPipeline is HDRenderPipeline hd)
            {
                hdPipeline = hd;
                return true;
            }
            hdPipeline = default;
            return false;
        }

        bool ShouldIssueWarningForCurrentSRP()
        {
            var pipeline = RenderPipelineManager.currentPipeline;
            var issueWarning = false;
            if (pipeline == null || pipeline.Equals(null))
            {
                if (Time.realtimeSinceStartup - m_DateSinceLastLegacyWarning > 1800)
                {
                    issueWarning = true;
                    m_DateSinceLastLegacyWarning = Time.realtimeSinceStartup;
                }
            }
            else if (!m_DateSinceLastInvalidSRPWarning.TryGetValue(
                RenderPipelineManager.currentPipeline,
                out float value
            ))
            {
                if ((Time.realtimeSinceStartup - value) > 1800)
                {
                    issueWarning = true;
                    m_DateSinceLastInvalidSRPWarning[RenderPipelineManager.currentPipeline]
                        = Time.realtimeSinceStartup;
                }
            }
            return issueWarning;
        }

        void DeleteCubemapAssets(bool deleteUnusedOnly)
        {
            var gameObjects = new List<GameObject>();
            var indices = new List<int>();
            var scenes = new List<Scene>();
            SceneObjectIDMap.GetAllIDsForAllScenes(
                HDBakingUtilities.SceneObjectCategory.ReflectionProbe,
                gameObjects, indices, scenes
            );

            var indicesSet = new HashSet<int>(indices);

            const int bufferLength = 1 << 10;
            var bufferStart = stackalloc byte[bufferLength];
            var buffer = new CoreUnsafeUtils.FixedBufferStringQueue(bufferStart, bufferLength);

            // Look for baked assets in scene folders
            for (int sceneI = 0, sceneC = SceneManager.sceneCount; sceneI < sceneC; ++sceneI)
            {
                var scene = SceneManager.GetSceneAt(sceneI);
                var sceneFolder = HDBakingUtilities.GetBakedTextureDirectory(scene);
                if (!Directory.Exists(sceneFolder))
                    continue;

                var types = UnityEngine.Rendering.HighDefinition.TypeInfo.GetEnumValues<ProbeSettings.ProbeType>();
                for (int typeI = 0; typeI < types.Length; ++typeI)
                {
                    var files = Directory.GetFiles(
                        sceneFolder,
                        HDBakingUtilities.HDProbeAssetPattern(types[typeI])
                    );
                    for (int fileI = 0; fileI < files.Length; ++fileI)
                    {
                        if (!HDBakingUtilities.TryParseBakedProbeAssetFileName(
                            files[fileI], out ProbeSettings.ProbeType fileProbeType, out int fileIndex
                            ))
                            continue;

                        // This file is a baked asset for a destroyed game object
                        // We can destroy it
                        if (!indicesSet.Contains(fileIndex) && deleteUnusedOnly
                            // Or we delete all assets
                            || !deleteUnusedOnly)
                        {
                            // If the buffer is full we empty it and then push again the element we were trying to
                            // push but failed.
                            if (!buffer.TryPush(files[fileI]))
                            {
                                DeleteAllAssetsIn(ref buffer);
                                buffer.TryPush(files[fileI]);
                            }
                        }
                    }
                }
            }
            DeleteAllAssetsIn(ref buffer);
        }

        static void DeleteAllAssetsIn(ref CoreUnsafeUtils.FixedBufferStringQueue queue)
        {
            if (queue.Count == 0)
                return;

            AssetDatabase.StartAssetEditing();
            while (queue.TryPop(out string path))
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.StopAssetEditing();

            // Clear the queue so that can be filled again.
            queue.Clear();
        }

        internal static void Checkout(string targetFile)
        {
            // Try to checkout through the VCS
            if (Provider.isActive
                && HDEditorUtils.IsAssetPath(targetFile)
                && Provider.GetAssetByPath(targetFile) != null)
            {
                Provider.Checkout(targetFile, CheckoutMode.Both).Wait();
            }
            else if (File.Exists(targetFile))
            {
                // There is no VCS, but the file is still locked
                // Try to make it writeable
                var attributes = File.GetAttributes(targetFile);
                if ((attributes & FileAttributes.ReadOnly) == 0) return;
                attributes &= ~FileAttributes.ReadOnly;
                File.SetAttributes(targetFile, attributes);
            }
        }

        internal static void AssignRenderData(HDProbe probe, string bakedTexturePath)
        {
            switch (probe.settings.type)
            {
                case ProbeSettings.ProbeType.PlanarProbe:
                {
                    var planarProbe = (PlanarReflectionProbe)probe;
                    var dataFile = bakedTexturePath + ".renderData";
                    if (File.Exists(dataFile))
                    {
                        if (HDBakingUtilities.TryDeserializeFromDisk(dataFile, out HDProbe.RenderData renderData))
                        {
                            HDProbeSystem.AssignRenderData(probe, renderData, ProbeSettings.Mode.Baked);
                            EditorUtility.SetDirty(probe);
                        }
                    }
                    break;
                }
            }
        }

        internal static void RenderAndWriteToFile(
            HDProbe probe, string targetFile,
            RenderTexture cubeRT, RenderTexture planarRT
        )
        {
            RenderAndWriteToFile(probe, targetFile, cubeRT, planarRT, out _, out _);
        }

        internal static void RenderAndWriteToFile(
            HDProbe probe, string targetFile,
            RenderTexture cubeRT, RenderTexture planarRT,
            out CameraSettings cameraSettings,
            out CameraPositionSettings cameraPositionSettings
        )
        {
            var settings = probe.settings;
            switch (settings.type)
            {
                case ProbeSettings.ProbeType.ReflectionProbe:
                {
                    var positionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, null);
                    HDRenderUtilities.Render(probe.settings, positionSettings, cubeRT,
                        out cameraSettings, out cameraPositionSettings,
                        forceFlipY: true,
                        forceInvertBackfaceCulling: true,     // Cubemap have an RHS standard, so we need to invert the face culling
                        (uint)StaticEditorFlags.ReflectionProbeStatic
                    );
                    HDBakingUtilities.CreateParentDirectoryIfMissing(targetFile);
                    Checkout(targetFile);
                    HDTextureUtilities.WriteTextureFileToDisk(cubeRT, targetFile);
                    break;
                }
                case ProbeSettings.ProbeType.PlanarProbe:
                {
                    var planarProbe = (PlanarReflectionProbe)probe;
                    var positionSettings = ProbeCapturePositionSettings.ComputeFromMirroredReference(
                        probe,
                        planarProbe.referencePosition
                    );

                    HDRenderUtilities.Render(
                        settings,
                        positionSettings,
                        planarRT,
                        out cameraSettings, out cameraPositionSettings
                    );
                    HDBakingUtilities.CreateParentDirectoryIfMissing(targetFile);
                    Checkout(targetFile);
                    HDTextureUtilities.WriteTextureFileToDisk(planarRT, targetFile);
                    var renderData = new HDProbe.RenderData(cameraSettings, cameraPositionSettings);
                    var targetRenderDataFile = targetFile + ".renderData";
                    Checkout(targetRenderDataFile);
                    HDBakingUtilities.TrySerializeToDisk(renderData, targetRenderDataFile);
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(probe.settings.type));
            }
        }

        internal static void ImportAssetAt(HDProbe probe, string file)
        {
            var hd = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
            switch (probe.settings.type)
            {
                case ProbeSettings.ProbeType.ReflectionProbe:
                {
                    var importer = AssetImporter.GetAtPath(file) as TextureImporter;
                    if (importer == null)
                        return;
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.sRGBTexture = false;
                    settings.filterMode = FilterMode.Bilinear;
                    settings.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
                    settings.cubemapConvolution = TextureImporterCubemapConvolution.None;
                    settings.seamlessCubemap = false;
                    settings.wrapMode = TextureWrapMode.Repeat;
                    settings.aniso = 1;
                    importer.SetTextureSettings(settings);
                    importer.mipmapEnabled = false;
                    importer.textureCompression = hd.currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionCacheCompressed
                        ? TextureImporterCompression.Compressed
                        : TextureImporterCompression.Uncompressed;
                    importer.textureShape = TextureImporterShape.TextureCube;
                    importer.SaveAndReimport();
                    break;
                }
                case ProbeSettings.ProbeType.PlanarProbe:
                {
                    var importer = AssetImporter.GetAtPath(file) as TextureImporter;
                    if (importer == null)
                        return;
                    importer.sRGBTexture = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.textureShape = TextureImporterShape.Texture2D;
                    importer.SaveAndReimport();
                    break;
                }
            }
        }

        static bool AreAllOpenedSceneSaved()
        {
            for (int i = 0, c = SceneManager.sceneCount; i < c; ++i)
            {
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                    return false;
            }
            return true;
        }

        static string GetGICacheFolderFor(Hash128 hash)
        {
            var cacheFolder = GetGICachePath();
            var hashFolder = Path.Combine(cacheFolder, hash.ToString().Substring(0, 2));
            return hashFolder;
        }

        string GetGICacheFileForHDProbe(Hash128 hash)
        {
            var hashFolder = GetGICacheFolderFor(hash);
            return Path.Combine(hashFolder, string.Format("HDProbe-{0}.exr", hash));
        }

        static void ComputeProbeInstanceID(IEnumerable<HDProbe> probes, HDProbeBakingState* states)
        {
            var i = 0;
            foreach (var probe in probes)
            {
                states[i].instanceID = probe.GetInstanceID();
                ++i;
            }
        }

        static void ComputeProbeSettingsHashes(IEnumerable<HDProbe> probes, HDProbeBakingState* states)
        {
            var i = 0;
            foreach (var probe in probes)
            {
                var positionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, null);
                var positionSettingsHash = positionSettings.ComputeHash();
                // TODO: make ProbeSettings and unmanaged type so its hash can be the hash of its memory
                var probeSettingsHash = probe.settings.ComputeHash();
                HashUtilities.AppendHash(ref positionSettingsHash, ref probeSettingsHash);
                states[i].probeSettingsHash = probeSettingsHash;
                ++i;
            }
        }

        static void ComputeProbeBakingHashes(int count, Hash128 allProbeDependencyHash, HDProbeBakingState* states)
        {
            for (int i = 0; i < count; ++i)
            {
                states[i].probeBakingHash = states[i].probeSettingsHash;
                HashUtilities.ComputeHash128(ref allProbeDependencyHash, ref states[i].probeBakingHash);
            }
        }

        private static void CreateAndImportDummyBakedTextureIfRequired(HDProbe probe, string bakedTexturePath)
        {
            var bytes = Texture2D.whiteTexture.EncodeToPNG();
            File.WriteAllBytes(bakedTexturePath, bytes);
            AssetDatabase.ImportAsset(bakedTexturePath);
            ImportAssetAt(probe, bakedTexturePath);
        }

        static Func<string> GetGICachePath = Expression.Lambda<Func<string>>(
            Expression.Call(
                typeof(Lightmapping)
                    .GetProperty("diskCachePath", BindingFlags.Static | BindingFlags.NonPublic)
                    .GetGetMethod(true)
            )
            ).Compile();
    }
}
