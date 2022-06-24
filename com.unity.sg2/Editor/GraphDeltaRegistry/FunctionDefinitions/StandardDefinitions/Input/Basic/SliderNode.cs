using System;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SliderNode : IStandardNode
    {
        public static string Name => "Slider";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = In;",
            parameters: new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Float, GraphType.Usage.Static),
                new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)//TODO: need to show slider value and min and max input text boxes 
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Uses a slider to give a float value between a given min and max value.",
            categories: new string[2] { "Input", "Basic" },
            synonyms: new string[0] { },
            hasPreview:false,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    useSlider: true
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a constant, single-channel float value defined by the slider UI"
                )
            }
        );
    }
}
