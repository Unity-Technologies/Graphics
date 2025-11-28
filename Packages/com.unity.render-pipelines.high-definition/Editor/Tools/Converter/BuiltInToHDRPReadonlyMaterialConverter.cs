using System;
using System.Collections.Generic;
using UnityEditor.Rendering.Converter;
using UnityEngine;
using UnityEngine.Categorization;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [Serializable]
    [PipelineConverter("Built-in", "High Definition Render Pipeline (HDRP)")]
    [ElementInfo(Name = "Material Reference Converter",
                 Order = 100,
                 Description = "Converts references to Built-In readonly materials to HDRP readonly materials. This will create temporarily a .index file and that can take a long time.")]
    internal class BuiltInToURP3DReadonlyMaterialConverter : ReadonlyMaterialConverter
    {
        public override bool isEnabled => GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset;
        public override string isDisabledMessage => "Converter requires HDRP. Convert your project to HDRP to use this converter.";

        protected override Dictionary<string, Func<Material>> materialMappings
        {
            get
            {
                return new()
                {
                    ["Default-Diffuse"] = () => GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineEditorMaterials>().defaultMaterial,
                    ["Default-Material"] = () => GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineEditorMaterials>().defaultMaterial,
                    ["Default-ParticleSystem"] = () => GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineEditorMaterials>().defaultParticleMaterial,
                    ["Default-Particle"] = () => GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineEditorMaterials>().defaultParticleMaterial,
                    ["Default-Terrain-Diffuse"] = () => GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineEditorMaterials>().defaultTerrainMaterial,
                    ["Default-Terrain-Specular"] = () => GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineEditorMaterials>().defaultTerrainMaterial,
                    ["Default-Terrain-Standard"] = () => GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineEditorMaterials>().defaultTerrainMaterial,
                };
            }
        }
    }
}
