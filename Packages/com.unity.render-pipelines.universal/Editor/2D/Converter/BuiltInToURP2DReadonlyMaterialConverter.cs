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
    [PipelineConverter("Built-in", "Universal Render Pipeline (2D Renderer)")]
    [ElementInfo(Name = "Material Reference Converter",
                 Order = 100,
                 Description = "Converts references to Built-In readonly materials to URP (2D) readonly materials.")]
    internal class BuiltInToURP2DReadonlyMaterialConverter : ReadonlyMaterialConverter
    {
        public override bool isEnabled
        {
            get
            {
                if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urpAsset)
                    return false;

                return urpAsset.scriptableRenderer is Renderer2D;
            }
        }

        public override string isDisabledMessage => "Converter requires URP with a Renderer 2D. Convert your project to URP to use this converter.";

        protected override Dictionary<string, Func<Material>> materialMappings
        {
            get
            {
                return new()
                {
                    ["Default-Material"] = () => GraphicsSettings.GetRenderPipelineSettings<Renderer2DResources>().defaultMesh2DLitMaterial,
                    ["Sprites-Default"] = () => GraphicsSettings.GetRenderPipelineSettings<Renderer2DResources>().defaultLitMaterial,
                    ["Sprites-Mask"] = () => GraphicsSettings.GetRenderPipelineSettings<Renderer2DResources>().defaultMaskMaterial,
                };
            }
        }
    }
}
