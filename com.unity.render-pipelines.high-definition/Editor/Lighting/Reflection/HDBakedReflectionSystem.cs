using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.Experimental.Rendering;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityEngine.Experimental.Rendering.HDPipeline
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
                        "please switch your render pipeline or use another reflection system");

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

            // == 2. ==
            var states = stackalloc HDProbeBakingState[bakedProbes.Count];
            ComputeProbeInstanceID(bakedProbes, states);
            ComputeProbeSettingsHashes(bakedProbes, states);
            // TODO: Handle bounce dependency here
            ComputeProbeBakingHashes(bakedProbes.Count, allProbeDependencyHash, states);

            CoreUnsafeUtils.QuickSort<HDProbeBakingState, Hash128, HDProbeBakingState.ProbeBakingHash>(
                bakedProbes.Count, states
            );

            int operationCount = 0, addCount = 0, remCount = 0;
            var maxProbeCount = Mathf.Max(bakedProbes.Count, m_HDProbeBakedStates.Length);
            var addIndices = stackalloc int[maxProbeCount];
            var remIndices = stackalloc int[maxProbeCount];

            if (m_HDProbeBakedStates.Length == 0)
            {
                for (int i = 0; i < bakedProbes.Count; ++i)
                    addIndices[addCount++] = i;
                operationCount = addCount;
            }
            else
            {
                fixed (HDProbeBakedState* oldBakedStates = &m_HDProbeBakedStates[0])
                {
                    // == 3. ==
                    // Compare hashes between baked probe states and desired probe states
                    operationCount = CoreUnsafeUtils.CompareHashes<
                            HDProbeBakedState, HDProbeBakedState.ProbeBakedHash,
                            HDProbeBakingState, HDProbeBakingState.ProbeBakingHash
                       > (
                       m_HDProbeBakedStates.Length, oldBakedStates, // old hashes
                       bakedProbes.Count, states,                   // new hashes
                       addIndices, remIndices,
                       out addCount, out remCount
                    );
                }
            }

            if (operationCount > 0)
            {
                // == 4. ==
                var cubemapSize = (int)hdPipeline.currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionCubemapSize;
                var planarSize = (int)hdPipeline.currentPlatformRenderPipelineSettings.lightLoopSettings.planarReflectionTextureSize;
                var cubeRT = HDRenderUtilities.CreateReflectionProbeRenderTarget(cubemapSize);
                var planarRT = HDRenderUtilities.CreatePlanarProbeRenderTarget(planarSize);

                handle.EnterStage(
                    (int)BakingStages.ReflectionProbes,
                    string.Format("Reflection Probes | {0} jobs", addCount),
                    0
                );

                // Render probes
                for (int i = 0; i < addCount; ++i)
                {
                    handle.EnterStage(
                        (int)BakingStages.ReflectionProbes,
                        string.Format("Reflection Probes | {0} jobs", addCount),
                        i / (float)addCount
                    );

                    var index = addIndices[i];
                    var instanceId = states[index].instanceID;
                    var probe = (HDProbe)EditorUtility.InstanceIDToObject(instanceId);
                    var cacheFile = GetGICacheFileForHDProbe(states[index].probeBakingHash);

                    // Get from cache or render the probe
                    if (!File.Exists(cacheFile))
                        RenderAndWriteToFile(probe, cacheFile, cubeRT, planarRT);
                }
                cubeRT.Release();
                planarRT.Release();

                // Copy texture from cache
                for (int i = 0; i < addCount; ++i)
                {
                    var index = addIndices[i];
                    var instanceId = states[index].instanceID;
                    var probe = (HDProbe)EditorUtility.InstanceIDToObject(instanceId);
                    var cacheFile = GetGICacheFileForHDProbe(states[index].probeBakingHash);

                    Assert.IsTrue(File.Exists(cacheFile));

                    var bakedTexturePath = HDBakingUtilities.GetBakedTextureFilePath(probe);
                    HDBakingUtilities.CreateParentDirectoryIfMissing(bakedTexturePath);
                    File.Copy(cacheFile, bakedTexturePath, true);
                }
                // AssetPipeline bug
                // Sometimes, the baked texture reference is destroyed during 'AssetDatabase.StopAssetEditing()'
                //   thus, the reference to the baked texture in the probe is lost
                // Although, importing twice the texture seems to workaround the issue
                for (int j = 0; j < 2; ++j)
                {
                    AssetDatabase.StartAssetEditing();
                    for (int i = 0; i < bakedProbes.Count; ++i)
                    {
                        var index = addIndices[i];
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
                for (int i = 0; i < addCount; ++i)
                {
                    var index = addIndices[i];
                    var instanceId = states[index].instanceID;
                    var probe = (HDProbe)EditorUtility.InstanceIDToObject(instanceId);
                    var cacheFile = GetGICacheFileForHDProbe(states[index].probeBakingHash);

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
                var targetSize = m_HDProbeBakedStates.Length + addCount - remCount;
                var targetBakedStates = stackalloc HDProbeBakedState[targetSize];
                // Copy baked state that are not removed
                var targetI = 0;
                for (int i = 0; i < m_HDProbeBakedStates.Length; ++i)
                {
                    if (CoreUnsafeUtils.IndexOf(remIndices, remCount, i) != -1)
                        continue;
                    targetBakedStates[targetI++] = m_HDProbeBakedStates[i];
                }
                // Add new baked states
                for (int i = 0; i < addCount; ++i)
                {
                    var state = states[addIndices[i]];
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
                    fixed (HDProbeBakedState* bakedStates = &m_HDProbeBakedStates[0])
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

        public static bool BakeProbes(IList<HDProbe> bakedProbes)
        {
            if (!(RenderPipelineManager.currentPipeline is HDRenderPipeline hdPipeline))
            {
                Debug.LogWarning("HDBakedReflectionSystem work with HDRP, " +
                    "please switch your render pipeline or use another reflection system");
                return false;
            }

            var cubemapSize = (int)hdPipeline.currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionCubemapSize;
            var planarSize = (int)hdPipeline.currentPlatformRenderPipelineSettings.lightLoopSettings.planarReflectionTextureSize;

            var cubeRT = HDRenderUtilities.CreateReflectionProbeRenderTarget(cubemapSize);
            var planarRT = HDRenderUtilities.CreatePlanarProbeRenderTarget(planarSize);

            // Render and write the result to disk
            for (int i = 0; i < bakedProbes.Count; ++i)
            {
                var probe = bakedProbes[i];
                var bakedTexturePath = HDBakingUtilities.GetBakedTextureFilePath(probe);
                RenderAndWriteToFile(probe, bakedTexturePath, cubeRT, planarRT);
            }

            // AssetPipeline bug
            // Sometimes, the baked texture reference is destroyed during 'AssetDatabase.StopAssetEditing()'
            //   thus, the reference to the baked texture in the probe is lost
            // Although, importing twice the texture seems to workaround the issue
            for (int j = 0; j < 2; ++j)
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < bakedProbes.Count; ++i)
                {
                    var probe = bakedProbes[i];
                    var bakedTexturePath = HDBakingUtilities.GetBakedTextureFilePath(probe);
                    AssetDatabase.ImportAsset(bakedTexturePath);
                    ImportAssetAt(probe, bakedTexturePath);
                }
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < bakedProbes.Count; ++i)
            {
                var probe = bakedProbes[i];
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

            cubeRT.Release();
            planarRT.Release();

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
            for (int sceneI = 0, sceneC = SceneManager.sceneCount; sceneI< sceneC; ++sceneI)
            {
                var scene = SceneManager.GetSceneAt(sceneI);
                var sceneFolder = HDBakingUtilities.GetBakedTextureDirectory(scene);
                if (!Directory.Exists(sceneFolder))
                    continue;

                var types = TypeInfo.GetEnumValues<ProbeSettings.ProbeType>();
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
                            if (!buffer.TryPush(files[fileI]))
                                DeleteAllAssetsIn(ref buffer);
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
            var settings = probe.settings;
            switch (settings.type)
            {
                case ProbeSettings.ProbeType.ReflectionProbe:
                    {
                        var positionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, null);
                        HDRenderUtilities.Render(probe.settings, positionSettings, cubeRT,
                            forceFlipY: true,
                            forceInvertBackfaceCulling: true, // TODO: for an unknown reason, we need to invert the backface culling for baked reflection probes, Remove this
                            (uint)StaticEditorFlags.ReflectionProbeStatic
                        );
                        HDBakingUtilities.CreateParentDirectoryIfMissing(targetFile);
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
                            out CameraSettings cameraSettings, out CameraPositionSettings cameraPositionSettings
                        );
                        HDBakingUtilities.CreateParentDirectoryIfMissing(targetFile);
                        HDTextureUtilities.WriteTextureFileToDisk(planarRT, targetFile);
                        var renderData = new HDProbe.RenderData(cameraSettings, cameraPositionSettings);
                        HDBakingUtilities.TrySerializeToDisk(renderData, targetFile + ".renderData");
                        break;
                    }
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
                        importer.sRGBTexture = false;
                        importer.filterMode = FilterMode.Bilinear;
                        importer.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
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
                        importer.textureCompression = hd.currentPlatformRenderPipelineSettings.lightLoopSettings.planarReflectionCacheCompressed
                            ? TextureImporterCompression.Compressed
                            : TextureImporterCompression.Uncompressed;
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

        static void ComputeProbeInstanceID(IList<HDProbe> probes, HDProbeBakingState* states)
        {
            for (int i = 0; i < probes.Count; ++i)
                states[i].instanceID = probes[i].GetInstanceID();
        }

        static void ComputeProbeSettingsHashes(IList<HDProbe> probes, HDProbeBakingState* states)
        {
            for (int i = 0; i < probes.Count; ++i)
            {
                var probe = probes[i];
                var positionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, null);
                var positionSettingsHash = positionSettings.ComputeHash();
                // TODO: make ProbeSettings and unmanaged type so its hash can be the hash of its memory
                var probeSettingsHash = probe.settings.ComputeHash();
                HashUtilities.AppendHash(ref positionSettingsHash, ref probeSettingsHash);
                states[i].probeSettingsHash = probeSettingsHash;
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
