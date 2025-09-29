using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.Universal
{
    enum IdentifierType { kNullIdentifier = 0, kImportedAsset = 1, kSceneObject = 2, kSourceAsset = 3, kBuiltInAsset = 4 };

    internal static class ReadonlyMaterialMap
    {
        public static bool TryGetMappingMaterial(Material material, out Material mappingMaterial)
        {
            mappingMaterial = material;

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
            {
                if (s_BuiltInMaterialsToURPMaterialsMappings.TryGetValue(material.name, out var mapping))
                    mappingMaterial = mapping();
            }

            return mappingMaterial != null;
        }

        public static int count => s_BuiltInMaterialsToURPMaterialsMappings.Count;

        public static IEnumerable<string> Keys => s_BuiltInMaterialsToURPMaterialsMappings.Keys;

        static Dictionary<string, Func<Material>> s_BuiltInMaterialsToURPMaterialsMappings = new()
        {
            ["Default-Diffuse"]             = () => GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>().defaultMaterial,
            ["Default-Material"]            = () => GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>().defaultMaterial,
            ["Default-ParticleSystem"]      = () => GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>().defaultParticleUnlitMaterial,
            ["Default-Particle"]            = () => GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>().defaultParticleUnlitMaterial,
            ["Default-Terrain-Diffuse"]     = () => GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>().defaultTerrainLitMaterial,
            ["Default-Terrain-Specular"]    = () => GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>().defaultTerrainLitMaterial,
            ["Default-Terrain-Standard"]    = () => GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>().defaultTerrainLitMaterial,
            ["Sprites-Default"]             = () => GraphicsSettings.GetRenderPipelineSettings<Renderer2DResources>().defaultLitMaterial,
            ["Sprites-Mask"]                = () => GraphicsSettings.GetRenderPipelineSettings<Renderer2DResources>().defaultLitMaterial,
            ["SpatialMappingOcclusion"]     = () => AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/SpatialMappingOcclusion.mat"),
            ["SpatialMappingWireframe"]     = () => AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/SpatialMappingWireframe.mat"),
        };

        public static List<(string materialName, string searchQuery)> GetMaterialSearchList()
        {
            List<(string materialName, string searchQuery)> list = new();
            foreach (var mat in GetBuiltInMaterials())
            {
                string formattedId = $"<$object:{GlobalObjectId.GetGlobalObjectIdSlow(mat)},UnityEngine.Object$>";
                list.Add(($"p: ref={formattedId}", $"{mat.name} is being referenced"));
            }
            return list;
        }

        public static Material[] GetBuiltInMaterials()
        {
            using(ListPool<Material>.Get(out var tmp))
            {
                foreach (var materialName in Keys)
                {
                    var name = materialName + ".mat";

                    Material mat = null;
                    foreach (var material in AssetDatabaseHelper.FindAssets<Material>())
                    {
                        if (material.name == materialName)
                        {
                            mat = material;
                            break;
                        }
                    }

                    if (mat == null)
                    {
                        mat = AssetDatabase.GetBuiltinExtraResource<Material>(name);
                        if (mat == null)
                        {
                            mat = Resources.GetBuiltinResource<Material>(name);
                            if (mat == null)
                            {
                                mat = Resources.Load<Material>(name);
                            }
                        }
                    }

                    if (mat == null)
                    {
                        Debug.LogError($"Material '{materialName}' not found in built-in resources or project assets.");
                        continue;
                    }

                    tmp.Add(mat);
                }
                return tmp.ToArray();
            }
        }
    }

    internal class ReadonlyMaterialConverter : RenderPipelineConverter
    {
        private static bool s_HasShownWarning = false;
        public override bool isEnabled
        {
            get
            {
                if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                {
                    if (!s_HasShownWarning)
                        Debug.LogWarning("[Render Pipeline Converter] Readonly Material Converter requires URP. Convert your project to URP to use this converter.");
                    s_HasShownWarning = true;
                    return false;
                }

                return true;
            }
        }
            
        public override string name => "Readonly Material Converter";
        public override string info => "Converts references to Built-In readonly materials to URP readonly materials. This will create temporarily a .index file and that can take a long time.";
        public override Type container => typeof(BuiltInToURPConverterContainer);

        List<string> guids = new();
        List<string> assetPaths = new ();

        internal void Add(string guid, string assetPath)
        {
            guids.Add(guid);
            assetPaths.Add(assetPath);
        }

        public override void OnInitialize(InitializeConverterContext ctx, Action callback)
        {
            SearchServiceUtils.RunQueuedSearch
            (
                SearchServiceUtils.IndexingOptions.DeepSearch,
                ReadonlyMaterialMap.GetMaterialSearchList(),
                (item, description) =>
                {
                    if (GlobalObjectId.TryParse(item.id, out var gid))
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(gid.assetGUID);
                        var itemDescriptor = new ConverterItemDescriptor()
                        {
                            name = assetPath,
                            info = description,
                        };
                        Add(gid.ToString(), assetPath);
                        ctx.AddAssetToConvert(itemDescriptor);
                    }
                },
                callback
            );
        }

        internal MaterialReferenceChanger m_MaterialReferenceChanger;

        public override void OnPreRun()
        {
            m_MaterialReferenceChanger = new MaterialReferenceChanger();
        }

        public override void OnPostRun()
        {
            m_MaterialReferenceChanger?.Dispose();
            m_MaterialReferenceChanger = null;
        }

        public override void OnRun(ref RunItemContext ctx)
        {
            var errorString = new StringBuilder();
            var obj = LoadObject(ref ctx, errorString);
            if (!m_MaterialReferenceChanger.ReassignUnityObjectMaterials(obj, errorString))
            {
                ctx.didFail = true;
                ctx.info = errorString.ToString();
            }
        }

        public override void OnClicked(int index)
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(assetPaths[index]));
        }

        private Object LoadObject(ref RunItemContext ctx, StringBuilder sb)
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
                                sb.AppendLine($"Object {gid.assetGUID} failed to load...");
                            }
                        }
                    }
                }

                return obj;
            }

            sb.AppendLine($"Failed to parse Global ID {item.descriptor.info}...");
            return null;
        }
    }
}
