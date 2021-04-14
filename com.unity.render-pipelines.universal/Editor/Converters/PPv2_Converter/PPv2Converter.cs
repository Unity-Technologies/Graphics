using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Rendering;
using BIRPRendering = UnityEngine.Rendering.PostProcessing;
using URPRendering = UnityEditor.Rendering.Universal;
using UnityEditor.SceneManagement;
using UnityEditor.Search;
using UnityEditor.Search.Providers;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Editor.Converters
{
    public class PPv2Converter : RenderPipelineConverter
    {
        private struct ExposedObjectIdentifier
        {
            public GUID m_GUID;
            public long m_LocalIdentifierInFile;
            public FileType m_FileType;
            public string m_FilePath;

            public ExposedObjectIdentifier(
                GUID guid,
                long localId,
                FileType fileType,
                string filePath)
            {
                m_GUID = guid;
                m_LocalIdentifierInFile = localId;
                m_FileType = fileType;
                m_FilePath = filePath;
            }
        }

        public override string name => "Post-Processing Stack v2 Converter";

        public override string info =>
            "Converts PPv2 Volumes, Profiles, and Layers to URP Volumes, Profiles, and Cameras.";

        public override Type conversion => typeof(URPRendering.BuiltInToURPConversion);

        private bool _startingSceneHasBeenClosed;

        public override void OnInitialize(InitializeConverterContext context)
        {
            // We are using separate searchContexts here and Adding them in this order:
            //      - Components from Prefabs & Scenes (Volumes & Layers)
            //      - ScriptableObjects (Profiles)
            //
            // This allows the old objects to be both re-referenced and deleted safely as they are converted in OnRun.
            // The process of converting Volumes will convert Profiles as-needed, and then the explicit followup Profile
            // conversion step will convert any non-referenced assets and delete all old Profiles.

            using var componentContext =
                SearchService.CreateContext("asset", "urp:convert-ppv2component");
            using var componentItems = SearchService.Request(componentContext);
            {
                AddSearchItemsAsConverterAssetEntries(componentItems, context);
            }

            using var scriptableObjectContext =
                SearchService.CreateContext("asset", "urp:convert-ppv2scriptableobject");
            using var scriptableObjectItems = SearchService.Request(scriptableObjectContext);
            {
                AddSearchItemsAsConverterAssetEntries(scriptableObjectItems, context);
            }
        }

        public override void OnRun(RunConverterContext ctx)
        {
            var items = ctx.items.ToList();

            foreach (var (index, obj) in EnumerateObjects(items, ctx).Where(item => item != null))
            {
                if (!obj)
                {
                    ctx.MarkFailed(index, "Could not be converted because the target object was lost.");
                    continue;
                }

                BIRPRendering.PostProcessVolume[] oldVolumes = null;
                BIRPRendering.PostProcessLayer[] oldLayers = null;

                // TODO: Upcoming changes to GlobalObjectIdentifierToObjectSlow will allow this to be inverted, and the else to be deleted.
#if false
                if (obj is GameObject go)
                {
                    oldVolumes = go.GetComponents<BIRPRendering.PostProcessVolume>();
                    oldLayers = go.GetComponents<BIRPRendering.PostProcessLayer>();
                }
                else if (obj is MonoBehaviour mb)
                {
                    oldVolumes = mb.GetComponents<BIRPRendering.PostProcessVolume>();
                    oldLayers = mb.GetComponents<BIRPRendering.PostProcessLayer>();
                }
#else
                if (obj is GameObject go)
                {
                    oldVolumes = go.GetComponentsInChildren<BIRPRendering.PostProcessVolume>();
                    oldLayers = go.GetComponentsInChildren<BIRPRendering.PostProcessLayer>();
                }
                else if (obj is MonoBehaviour mb)
                {
                    oldVolumes = mb.GetComponentsInChildren<BIRPRendering.PostProcessVolume>();
                    oldLayers = mb.GetComponentsInChildren<BIRPRendering.PostProcessLayer>();
                }
#endif

                // Note: even if nothing needs to be converted, that should still count as success,
                //       though it shouldn't ever actually occur.
                var succeeded = true;
                var errorString = new StringBuilder();

                if (oldVolumes != null)
                {
                    foreach (var oldVolume in oldVolumes)
                    {
                        ConvertVolume(oldVolume, ref succeeded, errorString);
                    }
                }

                if (oldLayers != null)
                {
                    foreach (var oldLayer in oldLayers)
                    {
                        ConvertLayer(oldLayer, ref succeeded, errorString);
                    }
                }

                if (obj is BIRPRendering.PostProcessProfile oldProfile)
                {
                    ConvertProfile(oldProfile, ref succeeded, errorString);
                }

                if (succeeded)
                    ctx.MarkSuccessful(index);
                else
                    ctx.MarkFailed(index, errorString.ToString());
            }
        }

        private void AddSearchItemsAsConverterAssetEntries(ISearchList searchItems, InitializeConverterContext context)
        {
            foreach (var searchItem in searchItems)
            {
                if (searchItem == null || !GlobalObjectId.TryParse(searchItem.id, out var globalId))
                {
                    continue;
                }

                var label = searchItem.provider.fetchLabel(searchItem, searchItem.context);
                var description = searchItem.provider.fetchDescription(searchItem, searchItem.context);

                var item = new ConverterItemDescriptor()
                {
                    name = $"{label} : {description}",
                    info = globalId.ToString(),
                    warningMessage = string.Empty,
                    helpLink = "// TODO",
                };

                context.AddAssetToConvert(item);
            }
        }

        private IEnumerable<Tuple<int, Object>> EnumerateObjects(IReadOnlyList<ConverterItemInfo> items,
            RunConverterContext ctx)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                if (GlobalObjectId.TryParse(item.descriptor.info, out var globalId))
                {
                    // Try loading the object
                    // TODO: Upcoming changes to GlobalObjectIdentifierToObjectSlow will allow it
                    //       to return direct references to prefabs and their children.
                    //       Once that change happens there are several items which should be adjusted,
                    //       and are commented with "// TODO: (prefab reference fix)"
                    var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);

                    // If the object was not loaded, it is probably part of an unopened scene;
                    // if so, then the solution is to first load the scene here.
                    var objIsInSceneOrPrefab = globalId.identifierType == (int) IdentifierType.kSceneObject;
                    if (!obj &&
                        objIsInSceneOrPrefab)
                    {
                        // Before we open a new scene, we need to save our changes;
                        // however, we should discard any existing changes to the
                        // scene that was open when conversion began.
                        if (_startingSceneHasBeenClosed)
                        {
                            var currentScene = SceneManager.GetActiveScene();
                            EditorSceneManager.SaveScene(currentScene);
                        }

                        _startingSceneHasBeenClosed = true;

                        // Open the Containing Scene Asset in the Hierarchy so the Object can be manipulated
                        var mainAssetPath = AssetDatabase.GUIDToAssetPath(globalId.assetGUID);
                        if (mainAssetPath.EndsWith(".unity", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var mainAsset = AssetDatabase.LoadAssetAtPath<Object>(mainAssetPath);
                            AssetDatabase.OpenAsset(mainAsset);

                            yield return null;

                            // Load the scene, as it cannot be operated on while closed
                            var scene = SceneManager.GetActiveScene();
                            while (!scene.isLoaded)
                            {
                                yield return null;
                            }

                            obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
                        }
                        // TODO: (prefab reference fix) This block should be removed once GlobalObjectIdentifierToObjectSlow
                        //       is updated to get proper direct references to prefabs and their child assets.
                        else
                        {
                            var mainAsset = AssetDatabase.LoadAssetAtPath<Object>(mainAssetPath);
                            AssetDatabase.OpenAsset(mainAsset);

                            yield return null;

                            // If a prefab stage was opened, then mainAsset is the root of the
                            // prefab that contains the target object, so reference that for now,
                            // until GlobalObjectIdentifierToObjectSlow is updated
                            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                            {
                                obj = mainAsset;
                            }
                        }
                    }

                    if (obj)
                        yield return new Tuple<int, Object>(i, obj);
                    else
                        ctx.MarkFailed(i, $"Object {globalId.assetGUID} failed to load...");
                }
                else
                {
                    ctx.MarkFailed(i, $"Failed to parse Global ID {item.descriptor.info}...");
                }
            }
        }

        // TODO: are there actually any failure states to catch here?
        //       It's a constructive process, so I think the most likely failure would be
        //       with permissions when saving the new Profile assets or prefab/scene changes.

        private void ConvertVolume(BIRPRendering.PostProcessVolume oldVolume, ref bool succeeded,
            StringBuilder errorString)
        {
            if (!succeeded || !oldVolume) return;

            // TODO: (keep this comment, but remove this todo)
            // Convert the components when:
            //  - They're overrides
            //  - They're NOT part of instances, as that would be non-variant prefab roots, and scene-only stuff
            // Convert Profile references on components when:
            //  - The component has been converted
            //  - The original reference was an override
            if (PrefabUtility.IsPartOfPrefabInstance(oldVolume))
            {
                // TODO: Convert component only if it's an override
                // TODO: Set reference only if the original component -or- reference was an override
                // TODO: Overriddes (components and references) should be set as such on the new instance as well.
            }
            else
            {
                // TODO: convert with wreckless abandon
            }

            // TODO: Volume Conversion Logic
            // TODO: Delete old component after conversion

            // TODO: Use this for error string:
            errorString.AppendLine("PPv2 PostProcessVolume failed to be converted with error:\n{error}");
        }

        private void ConvertLayer(BIRPRendering.PostProcessLayer oldLayer, ref bool succeeded,
            StringBuilder errorString)
        {
            if (!succeeded || !oldLayer) return;

            // TODO: (keep this comment, but remove this todo)
            // Convert the components when:
            //  - They're overrides
            //  - They're NOT part of instances, as that would be non-variant prefab roots, and scene-only stuff
            if (PrefabUtility.IsPartOfPrefabInstance(oldLayer))
            {
                // TODO: Convert component only if it's an override
                // TODO: Overriddes (components) should be set as such on the new instance as well.
            }
            else
            {
                // TODO: convert with wreckless abandon
            }

            // TODO: Layer Conversion Logic
            // TODO: Delete old component after conversion

            // TODO: Use this for error string:
            errorString.AppendLine("PPv2 PostProcessLayer failed to be converted with error:\n{error}");
        }

        private void ConvertProfile(BIRPRendering.PostProcessProfile oldProfile, ref bool succeeded,
            StringBuilder errorString)
        {
            if (!succeeded || !oldProfile) return;

            // TODO: Profile Conversion Logic

            // TODO:
            // - Profile assets (ScriptableObjects) should always be converted when encountered,
            //   including immediately when updating "sharedProfile" references.

            // TODO:
            // - Perhaps old Profiles should only be deleted if they actually no longer have references,
            // just in case some some Volume conversions are skipped and still need references for future conversion.
            // - Alternatively, leave deletion of Profiles entirely to the user. (prefer this?)

            // TODO: Use this for error string:
            errorString.AppendLine("PPv2 PostProcessProfile failed to be converted with error:\n{error}");
        }
    }
}
