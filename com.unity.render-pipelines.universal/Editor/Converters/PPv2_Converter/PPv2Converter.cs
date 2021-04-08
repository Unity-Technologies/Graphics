using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
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
        public override string name => "Post-Processing Stack v2 Converter";
        public override string info => "Converts PPv2 Volumes, Profiles, and Layers to URP Volumes, Profiles, and Cameras.";
        public override Type conversion => typeof(BuiltInToURPConversion);

        private bool _startingSceneIsClosed;

        // TODO List:
        // - Find all PPv2 Volumes, Profiles, and Layers in Assets and Scenes
        // - Iterate through them, performing the appropriate conversion operations based on PPv2 Type
        //     - Only update scene instances if they are not prefab based, or if they override the prefab somehow
        //       (in which case, a new override should be used).
        //     - Remove Volumes and Layers as you go
        // - Delete PPv2 Profiles (assets) all at once as a final step, since the references will have been updated in scenes/prefabs

        // TODO Questions:
        // - Do Prefabs need to be explicitly searched and updated first, followed by a scene search for lingering items?
        //      - If so, should this also be the case with the Built-In material conversion work?

        public override void OnInitialize(InitializeConverterContext ctx)
        {
            // TODO: Why "using", what is "asset", what is "urp:convert"?
            using var searchContext = SearchService.CreateContext("asset", "urp:convert");
            using var searchItems = SearchService.Request(searchContext);
            {
                // we're going to do this step twice in order to get them ordered, but it should be fast
                var orderedSearchItems = searchItems.OrderBy(req =>
                    {
                        GlobalObjectId.TryParse(req.id, out var gid);
                        return gid.assetGUID;
                    })
                    .ToList();

                foreach (var searchItem in orderedSearchItems)
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

                    ctx.AddAssetToConvert(item);
                }
            }
        }

        public override void OnRun(RunConverterContext ctx)
        {
            var items = ctx.items.ToList();

            // TODO NEXT!!!: Modify EnumerateObjects to return
            foreach (var (index, obj) in EnumerateObjects(items, ctx).Where(item => item != null))
            {
                // TODO: Parse obj into PPv2 Types here (Volumes, Profiles, Layers)
                var materials = MaterialReferenceBuilder.GetMaterialsFromObject(obj);

                var result = true;
                var errorString = new StringBuilder();
                foreach (var material in materials)
                {
                    // TODO: Logic goes in here? (one for each type)

                    // TODO: Handle Failure State
                    if (false)
                    {
                        result = false;
                        errorString.AppendLine($"Material {material.name} failed to be reassigned");
                    }
                }

                if (result)
                {
                    ctx.MarkSuccessful(index);
                    // TODO: Save assets
                }
                else
                {
                    ctx.MarkFailed(index, errorString.ToString());
                }
            }
        }

        private IEnumerable<Tuple<int,Object>> EnumerateObjects(IReadOnlyList<ConverterItemInfo> items, RunConverterContext ctx)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                if (GlobalObjectId.TryParse(item.descriptor.info, out var gid))
                {
                    var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                    if (!obj)
                    {
                        // Open container scene
                        if (gid.identifierType == (int)IdentifierType.kSceneObject)
                        {
                            // Before we open a new scene, we need to save.
                            // However, we shouldn't save the first scene.
                            // Todo: This should probably be expanded to the context of all converters. This is an example for now.
                            if (_startingSceneIsClosed)
                            {
                                var currentScene = SceneManager.GetActiveScene();
                                EditorSceneManager.SaveScene(currentScene);
                            }

                            _startingSceneIsClosed = true;

                            var containerPath = AssetDatabase.GUIDToAssetPath(gid.assetGUID);

                            var mainInstanceID = AssetDatabase.LoadAssetAtPath<Object>(containerPath);
                            AssetDatabase.OpenAsset(mainInstanceID);
                            yield return null;

                            // if we have a prefab open, then we already have the object we need to update
                            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                            if (prefabStage != null)
                            {
                                obj = mainInstanceID;
                            }
                            else
                            {
                                var scene = SceneManager.GetActiveScene();
                                while (!scene.isLoaded)
                                {
                                    yield return null;
                                }
                            }
                        }

                        // Reload object if it is still null
                        if (obj == null)
                        {
                            obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                            if (!obj)
                            {
                                ctx.MarkFailed(i, $"Object {gid.assetGUID} failed to load...");
                                continue;
                            }
                        }
                    }

                    yield return new Tuple<int, Object>(i, obj);
                }
                else
                {
                    ctx.MarkFailed(i, $"Failed to parse Global ID {item.descriptor.info}...");
                }
            }
        }
    }
}
