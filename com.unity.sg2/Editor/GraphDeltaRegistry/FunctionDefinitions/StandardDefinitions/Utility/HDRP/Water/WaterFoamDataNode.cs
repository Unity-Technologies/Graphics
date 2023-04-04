using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
    {
        internal class WaterFoamDataNode : IStandardNode
        {
            public static string Name => "WaterFoamData";
            public static int Version => 1;

            public static FunctionDescriptor FunctionDescriptor => new(
                Name,
    @"FoamData foamData;
ZERO_INITIALIZE(FoamData, foamData);
EvaluateFoamData(SimulationFoam, CustomFoam, UV0.xyz, foamData);
Smoothness = foamData.smoothness;
Foam = foamData.foamValue;
",
                new ParameterDescriptor[]
                {
                    new ParameterDescriptor("UV0", TYPE.Vec4, Usage.Local, REF.UV0),
                    new ParameterDescriptor("SimulationFoam", TYPE.Float, Usage.In),
                    new ParameterDescriptor("CustomFoam", TYPE.Float, Usage.In),
                    new ParameterDescriptor("Foam", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("Smoothness", TYPE.Float, Usage.Out)
                }
            );

            public static NodeUIDescriptor NodeUIDescriptor => new(
                Version,
                Name,
                displayName: "Water Foam Data",
                tooltip: "",
                category: "Utility/HDRP/Water",
                synonyms: new string[1] { "Evaluate Water Foam Data" },
                description: "pkg://Documentation~/previews/WaterFoamData.md",
                hasPreview: false,
                parameters: new ParameterUIDescriptor[] {
                    new ParameterUIDescriptor(
                        name: "SimulationFoam",
                        displayName: "Simulation Foam",
                        tooltip: ""
                    ),
                    new ParameterUIDescriptor(
                        name: "CustomFoam",
                        displayName: "Custom Foam",
                        tooltip: ""
                    ),
                    new ParameterUIDescriptor(
                        name: "Foam",
                        displayName: "Foam",
                        tooltip: ""
                    ),
                    new ParameterUIDescriptor(
                        name: "Smoothness",
                        displayName: "Smoothness",
                        tooltip: ""
                    )
                }
            );
        }
    }

