using System;
using System.Collections.Generic;
using UnityEditor.Rendering.Converter;
using UnityEngine.Categorization;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [PipelineConverter("Built-in", "Universal Render Pipeline (Universal Renderer)")]
    [ElementInfo(Name = "Shaders Converter",
                 Order = 100,
                 Description = "This converter scans all materials that reference Built-in shaders and upgrades them to use Universal Render Pipeline (URP) shaders.")]
    internal sealed class BuiltInToURP3DMaterialUpgrader : RenderPipelineConverterMaterialUpgrader
    {
        protected override List<MaterialUpgrader> upgraders
        {
            get
            {
                var allURPUpgraders = MaterialUpgrader.FetchAllUpgradersForPipeline(typeof(UniversalRenderPipelineAsset));

                var builtInToURPUpgraders = new List<MaterialUpgrader>();
                foreach (var upgrader in allURPUpgraders)
                {
                    if (upgrader is IBuiltInToURPMaterialUpgrader)
                        builtInToURPUpgraders.Add(upgrader);
                }

                return builtInToURPUpgraders;
            }
        }
            
    }
}
