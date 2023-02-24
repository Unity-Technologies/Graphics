using System;
using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class EmissionNode : IStandardNode
    {
        public static string Name => "Emission";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new (
                    "Nits",
//GetInverseCurrentExposureMultiplier() isn't found
@"   if (NormalizeColor) Color = Color * rcp(max(Luminance(Color), 1e-6));
   hdrColor = Color * Intensity;
#ifdef SHADERGRAPH_PREVIEW
   inverseExposureMultiplier = 1.0;
#else
   inverseExposureMultiplier = GetInverseCurrentExposureMultiplier();
#endif
   // Inverse pre-expose using _EmissiveExposureWeight weight
   hdrColor = lerp(hdrColor * inverseExposureMultiplier, hdrColor, ExposureWeight);
   Out = hdrColor;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Color", TYPE.Vec3, Usage.In),
                        new ParameterDescriptor("Intensity", TYPE.Float, Usage.In, new float[] { 1.0f }),
                        new ParameterDescriptor("ExposureWeight", TYPE.Float, Usage.In, new float[] { 1.0f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out),
                        new ParameterDescriptor("NormalizeColor", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("hdrColor", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("inverseExposureMultiplier", TYPE.Float, Usage.Local)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl\"",
                    }
                ),
                new (
                    "EV100",
//GetInverseCurrentExposureMultiplier() isn't found
@"   Intensity = ConvertEvToLuminance(Intensity);
   if (NormalizeColor) Color = Color * rcp(max(Luminance(Color), 1e-6));
   hdrColor = Color * Intensity;
#ifdef SHADERGRAPH_PREVIEW
   inverseExposureMultiplier = 1.0;
#else
   inverseExposureMultiplier = GetInverseCurrentExposureMultiplier();
#endif
   // Inverse pre-expose using _EmissiveExposureWeight weight
   hdrColor = lerp(hdrColor * inverseExposureMultiplier, hdrColor, ExposureWeight);
   Out = hdrColor;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Color", TYPE.Vec3, Usage.In),
                        new ParameterDescriptor("Intensity", TYPE.Float, Usage.In, new float[] { 1.0f }),
                        new ParameterDescriptor("ExposureWeight", TYPE.Float, Usage.In, new float[] { 1.0f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out),
                        new ParameterDescriptor("NormalizeColor", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("hdrColor", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("inverseExposureMultiplier", TYPE.Float, Usage.Local)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl\"",
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Emission",
            tooltip: "allows you to apply emission in your shader",
            category: "Utility/HDRP",
            synonyms: Array.Empty<string>(),
            description: "pkg://Documentation~/previews/Emission.md",
            selectableFunctions: new Dictionary<string, string>
            {
                { "Nits", "Nits" },
                { "EV100", "EV100" }
            },
            functionSelectorLabel: "Intensity Unit",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[5] {
                new ParameterUIDescriptor(
                    name: "Color",
                    tooltip: "Sets the low dynamic range (LDR) color of the emission",
                    useColor: true
                ),
                new ParameterUIDescriptor(
                    name: "Intensity",
                    tooltip: "Sets the intensity of the emission color."
                ),
                new ParameterUIDescriptor(
                    name: "ExposureWeight",
                    displayName: "Exposure Weight",
                    tooltip: "Controls how much the exposure affects the emission."
                ),
                new ParameterUIDescriptor(
                    name: "NormalizeColor",
                    displayName: "Normalize Color",
                    tooltip: "Ensures the channels of the color are between zero and one."
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "the high dynamic range (HDR) color that this Node produces."
                )
            }
        );
    }
}
