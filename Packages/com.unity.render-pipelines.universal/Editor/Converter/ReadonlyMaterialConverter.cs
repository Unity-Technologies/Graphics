using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.Universal
{
    enum IdentifierType { kNullIdentifier = 0, kImportedAsset = 1, kSceneObject = 2, kSourceAsset = 3, kBuiltInAsset = 4 };

    internal static class ReadonlyMaterialMap
    {
        public static readonly Dictionary<string, string> Map = new Dictionary<string, string>
        {
            {"Default-Diffuse", "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Lit.mat"},
            {"Default-Material", "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Lit.mat"},
            {"Default-ParticleSystem", "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat"},
            {"Default-Particle", "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat"},
            {"Default-Terrain-Diffuse", "Packages/com.unity.render-pipelines.universal/Runtime/Materials/TerrainLit.mat"},
            {"Default-Terrain-Specular", "Packages/com.unity.render-pipelines.universal/Runtime/Materials/TerrainLit.mat"},
            {"Default-Terrain-Standard", "Packages/com.unity.render-pipelines.universal/Runtime/Materials/TerrainLit.mat"},
            {"Sprites-Default", "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat"},
            {"Sprites-Mask", "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat"},
            {"SpatialMappingOcclusion", "Packages/com.unity.render-pipelines.universal/Runtime/Materials/SpatialMappingOcclusion.mat"},
            {"SpatialMappingWireframe", "Packages/com.unity.render-pipelines.universal/Runtime/Materials/SpatialMappingWireframe.mat"},

            // TODO: These currently render in URP, but they are using BIRP shaders. Create a task to convert these.
            // {"Default UI Material", "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Lit.mat"},
            // {"ETC1 Supported UI Material", "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Lit.mat"},
            // {"Default-Line", "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Lit.mat"},
            // {"Default-Skybox", "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Lit.mat"},
        };
    }

    internal class ReadonlyMaterialConverter : RenderPipelineConverter
    {
        public override string name => "Readonly Material Converter";
        public override string info => "Converts references to Built-In readonly materials to URP readonly materials. This will create temporarily a .index file and that can take a long time.";
        public override Type container => typeof(BuiltInToURPConverterContainer);
        public override bool needsIndexing => true;

        List<string> guids = new List<string>();

        public override void OnInitialize(InitializeConverterContext ctx, Action callback)
        {
            Search.SearchService.Request
            (
                Search.SearchService.CreateContext("asset", "urp=convert-readonly a=URPConverterIndex"),
                (searchContext, items) =>
                {
                    // we're going to do this step twice in order to get them ordered, but it should be fast
                    var orderedRequest = items.OrderBy(req =>
                    {
                        GlobalObjectId.TryParse(req.id, out var gid);
                        return gid.assetGUID;
                    });

                    foreach (var r in orderedRequest)
                    {
                        if (string.IsNullOrEmpty(r?.id) ||
                            !GlobalObjectId.TryParse(r.id, out var gid))
                        {
                            continue;
                        }

                        var label = r.provider.fetchLabel(r, r.context);
                        var description = r.provider.fetchDescription(r, r.context);

                        var item = new ConverterItemDescriptor()
                        {
                            name = description.Split('/').Last().Split('.').First(),
                            info = $"{label}",
                        };
                        guids.Add(gid.ToString());

                        ctx.AddAssetToConvert(item);
                    }

                    callback.Invoke();
                    searchContext?.Dispose();
                }
            );
        }

        public override void OnRun(ref RunItemContext ctx)
        {
            var obj = LoadObject(ref ctx);
            var result = true;
            var errorString = new StringBuilder();

            if (obj != null)
            {
                var materials = MaterialReferenceBuilder.GetMaterialsFromObject(obj);

                foreach (var material in materials)
                {
                    if (material == null)
                    {
                        continue;
                    }
                    // there might be multiple materials on this object, we only care about the ones we explicitly try to remap that fail
                    if (!MaterialReferenceBuilder.GetIsReadonlyMaterial(material)) continue;
                    if (!ReadonlyMaterialMap.Map.ContainsKey(material.name)) continue;
                    if (!ReassignMaterial(obj, material.name, ReadonlyMaterialMap.Map[material.name]))
                    {
                        result = false;
                        errorString.AppendLine($"Material {material.name} failed to be reassigned");
                    }
                }
            }
            else
            {
                result = false;
                errorString.AppendLine($"Object {ctx.item.descriptor.name} could not be loaded");
            }

            if (!result)
            {
                ctx.didFail = true;
                ctx.info = errorString.ToString();
            }
            else
            {
                // make sure the changes get saved
                EditorUtility.SetDirty(obj);
                var currentScene = SceneManager.GetActiveScene();
                EditorSceneManager.SaveScene(currentScene);
            }
        }

        public override void OnClicked(int index)
        {
            if (GlobalObjectId.TryParse(guids[index], out var gid))
            {
                var containerPath = AssetDatabase.GUIDToAssetPath(gid.assetGUID);
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(containerPath));
            }
        }

        private static bool ReassignMaterial(Object obj, string oldMaterialName, string newMaterialPath)
        {
            var result = true;

            // do the reflection to make sure we get the right material reference
            if (obj is GameObject go)
            {
                foreach (var key in MaterialReferenceBuilder.GetComponentTypes())
                {
                    var components = go.GetComponentsInChildren(key);
                    foreach (var component in components)
                    {
                        result &= ReassignMaterialOnComponentOrObject(component,
                            oldMaterialName,
                            newMaterialPath);
                    }
                }
            }
            else
            {
                result &= ReassignMaterialOnComponentOrObject(obj,
                    oldMaterialName,
                    newMaterialPath);
            }

            return result;
        }

        private static bool ReassignMaterialOnComponentOrObject(Object obj,
            string oldMaterialName,
            string newMaterialPath)
        {
            var result = true;

            var materialProperties = obj.GetType().GetMaterialPropertiesWithoutLeaking();

            foreach (var property in materialProperties)
            {
                var materialValue = property.GetGetMethod().GetMaterialFromMethod(obj, (methodName, objectName) =>
                    $"The method {methodName} was not found on {objectName}. Ignoring this property.");

                if (materialValue is Material material)
                {
                    if (material.name.Equals(oldMaterialName, StringComparison.OrdinalIgnoreCase))
                    {
                        var newMaterial = AssetDatabase.LoadAssetAtPath<Material>(newMaterialPath);

                        if (newMaterial != null)
                        {
                            var setMethod = property.GetSetMethod();
                            if (setMethod != null)
                            {
                                setMethod.Invoke(obj, new object[] { newMaterial });
                            }
                            else
                            {
                                // failed to set the material from the SetMethod
                                result = false;
                            }
                        }
                        else
                        {
                            // a material we expected to exist does not
                            result = false;
                        }
                    }
                }
                else if (materialValue is Material[] materialList)
                {
                    for (int i = 0; i < materialList.Length; i++)
                    {
                        var mat = materialList[i];
                        if (mat == null)
                        {
                            continue;
                        }
                        if (mat.name.Equals(oldMaterialName, StringComparison.OrdinalIgnoreCase))
                        {
                            var newMaterial = AssetDatabase.LoadAssetAtPath<Material>(newMaterialPath);

                            if (newMaterial != null)
                            {
                                materialList[i] = newMaterial;
                            }
                            else
                            {
                                // a material we expected to exist does not
                                result = false;
                            }
                        }
                    }

                    var setMethod = property.GetSetMethod();
                    if (setMethod != null)
                    {
                        setMethod.Invoke(obj, new object[] { materialList });
                    }
                    else
                    {
                        // failed to set the material from the SetMethod
                        result = false;
                    }
                }
            }

            return result;
        }

        private Object LoadObject(ref RunItemContext ctx)
        {
            var item = ctx.item;
            var guid = guids[item.index];

            if (GlobalObjectId.TryParse(guid, out var gid))
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

                        // if we have a prefab open, then we already have the object we need to update
                        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                        if (prefabStage != null)
                        {
                            obj = mainInstanceID;
                        }

                        // Reload object if it is still null
                        if (obj == null)
                        {
                            obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                            if (!obj)
                            {
                                ctx.didFail = true;
                                ctx.info = $"Object {gid.assetGUID} failed to load...";
                            }
                        }
                    }
                }

                return obj;
            }

            ctx.didFail = true;
            ctx.info = $"Failed to parse Global ID {item.descriptor.info}...";

            return null;
        }
    }
}
