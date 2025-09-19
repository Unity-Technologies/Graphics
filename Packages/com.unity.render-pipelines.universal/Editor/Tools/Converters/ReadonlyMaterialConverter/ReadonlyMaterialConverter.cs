using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Rendering.Converter;
using UnityEngine;
using UnityEngine.Categorization;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
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

    [PipelineConverter("Built-in", "Universal Render Pipeline (Universal Renderer)")]
    [ElementInfo(Name = "Materials Converter",
                 Order = 100,
                 Description = "Converts references to Built-In readonly materials to URP readonly materials. This will create temporarily a .index file and that can take a long time.")]
    internal class ReadonlyMaterialConverter : RenderPipelineAssetsConverter
    {
        public override bool isEnabled => GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;
        public override string isDisabledWarningMessage => "Converter requires URP. Convert your project to URP to use this converter.";

        protected override List<(string query, string description)> contextSearchQueriesAndIds
            => ReadonlyMaterialMap.GetMaterialSearchList();

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

        protected override Status ConvertObject(UnityEngine.Object obj, StringBuilder message)
        {
            if (!m_MaterialReferenceChanger.ReassignUnityObjectMaterials(obj, message))
            {
                return Status.Error;
            }

            return Status.Success;
        }
    }
}
