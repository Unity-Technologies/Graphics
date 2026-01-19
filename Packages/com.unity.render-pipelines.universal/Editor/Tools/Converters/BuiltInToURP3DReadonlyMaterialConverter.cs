using System;
using System.Collections.Generic;
using UnityEditor.Rendering.Converter;
using UnityEngine;
using UnityEngine.Categorization;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [Serializable]
    [PipelineConverter("Built-in", "Universal Render Pipeline (Universal Renderer)")]
    [ElementInfo(Name = "Material Reference Converter",
                 Order = 100,
                 Description = "Converts references to Built-In readonly materials to URP readonly materials.")]
    internal class BuiltInToURP3DReadonlyMaterialConverter : ReadonlyMaterialConverter
    {
        public override bool isEnabled
        {
            get
            {
                if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urpAsset)
                    return false;

                return urpAsset.scriptableRenderer is UniversalRenderer;
            }
        }
        public override string isDisabledMessage => "Converter requires URP with an Universal Renderer. Convert your project to URP to use this converter.";

        protected override Dictionary<string, Func<Material>> materialMappings
        {
            get
            {
                return new()
                {
                    ["Default-Diffuse"] = () => GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>().defaultMaterial,
                    ["Default-Material"] = () => GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>().defaultMaterial,
                    ["Default-ParticleSystem"] = () => GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>().defaultParticleUnlitMaterial,
                    ["Default-Particle"] = () => GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>().defaultParticleUnlitMaterial,
                    ["Default-Terrain-Diffuse"] = () => GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>().defaultTerrainLitMaterial,
                    ["Default-Terrain-Specular"] = () => GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>().defaultTerrainLitMaterial,
                    ["Default-Terrain-Standard"] = () => GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>().defaultTerrainLitMaterial,
                    ["Sprites-Default"] = () => GraphicsSettings.GetRenderPipelineSettings<Renderer2DResources>().defaultLitMaterial,
                    ["Sprites-Mask"] = () => GraphicsSettings.GetRenderPipelineSettings<Renderer2DResources>().defaultLitMaterial,
                    ["SpatialMappingOcclusion"] = () => AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/SpatialMappingOcclusion.mat"),
                    ["SpatialMappingWireframe"] = () => AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/SpatialMappingWireframe.mat"),
                };
            }
        }
    }
}
