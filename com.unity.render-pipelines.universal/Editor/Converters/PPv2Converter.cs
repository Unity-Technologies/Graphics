using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.Search;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Editor.Converters
{
    public class PPv2Converter : RenderPipelineConverter
    {
        public override string name => "Post-Processing Stack v2 Converter";
        public override string info => "Converts PPv2 Volumes, Profiles, and Layers to URP Volumes, Profiles, and Cameras.";
        public override Type conversion => typeof(BuiltInToURPConversion);

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
            using var searchRequest = SearchService.CreateContext("asset", "urp:convert");
            using var searchItems = SearchService.Request(searchRequest);
            foreach (var searchItem in searchItems)
            {
                if (searchItem == null || !GlobalObjectId.TryParse(searchItem.id, out var gid))
                {
                    continue;
                }

                var item = new ConverterItemDescriptor()
                {
                    name = searchItem.description,
                    path = gid.ToString(),
                };

                ctx.AddAssetToConvert(item);
            }
        }

        public override void OnRun(RunConverterContext ctx)
        {
            var items = ctx.items.ToList();

            foreach (var (index, obj) in EnumerateObjects(items, ctx).Where(item => item != null))
            {
                var materials = MaterialReferenceBuilder.GetMaterialsFromObject(obj);

                var result = true;
                var errorString = new StringBuilder();
                foreach (var material in materials)
                {
                    // there might be multiple materials on this object, we only care about the ones we explicitly try to remap that fail
                    if (!MaterialReferenceBuilder.GetIsReadonlyMaterial(material)) continue;
                    if (!ReadonlyMaterialMap.Map.ContainsKey(material.name)) continue;
                    if (!ReAssignMaterial(obj, material.name, ReadonlyMaterialMap.Map[material.name]))
                    {
                        result = false;
                        errorString.AppendLine($"Material {material.name} failed to be reassigned");
                    }
                }

                if (result)
                {
                    ctx.MarkSuccessful(index);
                    // todo: Save assets
                }
                else
                {
                    ctx.MarkFailed(index, errorString.ToString());
                }
            }
        }

        private static IEnumerable<Tuple<int,Object>> EnumerateObjects(IReadOnlyList<ConverterItemInfo> items, RunConverterContext ctx)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                if (GlobalObjectId.TryParse(item.descriptor.path, out var gid))
                {
                    var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                    if (!obj)
                    {
                        // Open container scene
                        if (gid.identifierType == (int)IdentifierType.kSceneObject)
                        {
                            var containerPath = AssetDatabase.GUIDToAssetPath(gid.assetGUID);

                            var mainInstanceID = AssetDatabase.LoadAssetAtPath<Object>(containerPath);
                            AssetDatabase.OpenAsset(mainInstanceID);
                            yield return null;

                            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                            while (!scene.isLoaded)
                            {
                                yield return null;
                            }
                        }

                        // Reload object
                        obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                        if (!obj)
                        {
                            ctx.MarkFailed(i, $"Object {gid.assetGUID} failed to load...");
                            continue;
                        }
                    }

                    yield return new Tuple<int, Object>(i, obj);
                }
                else
                {
                    ctx.MarkFailed(i, $"Failed to parse Global ID {item.descriptor.path}...");
                }
            }
        }
    }
}
