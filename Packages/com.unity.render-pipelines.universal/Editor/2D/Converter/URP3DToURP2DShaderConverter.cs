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
    [PipelineConverter("Universal Render Pipeline (Universal Renderer)", "Universal Render Pipeline (2D Renderer)")]
    [ElementInfo(Name = "Material Shader Converter",
                 Order = 100,
                 Description = "Converts references to URP Lit and Simple Lit shaders to Mesh 2D Lit shader.")]
    internal sealed class URP3DToURP2DShaderConverter : RenderPipelineConverterMaterialUpgrader
    {
        protected override List<MaterialUpgrader> upgraders
        {
            get
            {
                var allURPUpgraders = MaterialUpgrader.FetchAllUpgradersForPipeline(typeof(UniversalRenderPipelineAsset));

                var builtInToURPUpgraders = new List<MaterialUpgrader>();
                foreach (var upgrader in allURPUpgraders)
                {
                    if (upgrader is IURP3DToURP2dMaterialUpgrader)
                        builtInToURPUpgraders.Add(upgrader);
                }

                return builtInToURPUpgraders;
            }
        }
    }
}
