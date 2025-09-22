using System.Collections.Generic;
using UnityEditor.Rendering.Converter;
using UnityEngine.Categorization;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [PipelineConverter("Built-in", "High Definition Render Pipeline (HDRP)")]
    [ElementInfo(Name = "Material Shader Converter",
                 Order = 100,
                 Description = "This converter scans all materials that reference Built-in shaders and upgrades them to use High Definition Render Pipeline (HDRP) shaders.")]
    internal sealed class BuiltInToHDRPMaterialUpgrader : RenderPipelineConverterMaterialUpgrader
    {
        protected override List<MaterialUpgrader> upgraders
        {
            get
            {
                var allHDRPUpgraders = MaterialUpgrader.FetchAllUpgradersForPipeline(typeof(HDRenderPipelineAsset));
                return allHDRPUpgraders;
            }
        }
            
    }
}
