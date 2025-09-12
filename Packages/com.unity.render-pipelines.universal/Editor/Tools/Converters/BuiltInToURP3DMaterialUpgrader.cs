using System;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal sealed class BuiltInToURP3DMaterialUpgrader : RenderPipelineConverterMaterialUpgrader
    {
        public override string name => "Shaders Converter";
        public override string info => "This converter scans all materials that reference Built-in shaders and upgrades them to use Universal Render Pipeline (URP) shaders.";

        public override int priority => -1000;
        public override Type container => typeof(BuiltInToURPConverterContainer);

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
