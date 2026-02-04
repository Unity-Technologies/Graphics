using System;
using System.Collections.Generic;
using UnityEditor.Rendering.Converter;
using UnityEngine.Categorization;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [Serializable]
    [PipelineConverter("Built-in", "Universal Render Pipeline (2D Renderer)")]
    [ElementInfo(Name = "Material Shader Converter",
                 Order = 100,
                 Description = "Converts references to Built-In shaders to URP (2D) shaders.")]
    internal sealed class BuiltInToURP2DShaderConverter : RenderPipelineConverterMaterialUpgrader
    {
        protected override List<MaterialUpgrader> upgraders
        {
            get
            {
                var allURPUpgraders = MaterialUpgrader.FetchAllUpgradersForPipeline(typeof(UniversalRenderPipelineAsset));

                var builtInToURPUpgraders = new List<MaterialUpgrader>();
                foreach (var upgrader in allURPUpgraders)
                {
                    if (upgrader is IBuiltInToURP2dMaterialUpgrader)
                        builtInToURPUpgraders.Add(upgrader);
                }

                return builtInToURPUpgraders;
            }
        }
    }
}
