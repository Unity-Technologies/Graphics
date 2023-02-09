using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class FresnelEquationNode : IStandardNode
    {
        public static string Name => "FresnelEquation";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new (
                    "Schlick",
@"   Fresnel = F_Schlick(F0, DotVector);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("DotVector", TYPE.Float, Usage.In),
                        new ParameterDescriptor("F0", TYPE.Float, Usage.In, new float[] { 0.04f }),
                        new ParameterDescriptor("Fresnel", TYPE.Float, Usage.Out)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl\"",
                    }
                ),
                new (
                    "Dielectric",
@"   Fresnel = F_FresnelDielectric(IORMedium/IORSource, DotVector);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("DotVector", TYPE.Float, Usage.In),
                        new ParameterDescriptor("IORSource", TYPE.Float, Usage.In, new float[] { 1.0f }),
                        new ParameterDescriptor("IORMedium", TYPE.Float, Usage.In, new float[] { 1.5f }),
                        new ParameterDescriptor("Fresnel", TYPE.Float, Usage.Out)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl\"",
                    }
                ),
                new (
                    "DielectricGeneric",
@"   Fresnel = F_FresnelConductor(IORMedium/IORSource, IORMediumK/IORSource, DotVector);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("DotVector", TYPE.Float, Usage.In),
                        new ParameterDescriptor("IORSource", TYPE.Float, Usage.In, new float[] { 1.0f }),
                        new ParameterDescriptor("IORMedium", TYPE.Float, Usage.In, new float[] { 1.5f }),
                        new ParameterDescriptor("IORMediumK", TYPE.Float, Usage.In, new float[] { 2.0f }),
                        new ParameterDescriptor("Fresnel", TYPE.Float, Usage.Out)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl\"",
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Fresnel Equation",
            tooltip: "adds equations that affect Material interactions to the Fresnel Component",
            category: "Math/Advanced",
            synonyms: new string[6] { "schlick", "metal", "dielectric", "tir", "reflection", "critical" },
            description: "pkg://Documentation~/previews/FresnelEquation.md",
            selectableFunctions: new Dictionary<string, string>
            {
                { "Schlick", "Schlick" },
                { "Dielectric", "Dielectric" },
                { "DielectricGeneric", "Dielectric Generic" }
            },
            hasModes: true,
            functionSelectorLabel: "Mode",
            parameters: new ParameterUIDescriptor[6] {
                new ParameterUIDescriptor(
                    name: "DotVector",
                    displayName: "Dot Vector",
                    tooltip: "The dot product between the normal and the surface"
                ),
                new ParameterUIDescriptor(
                    name: "F0",
                    tooltip: "the reflection of the surface when facing the viewer"
                ),
                new ParameterUIDescriptor(
                    name: "IORSource",
                    displayName: "IOR Source",
                    tooltip: "refractive index of the medium the light source originates in."
                ),
                new ParameterUIDescriptor(
                    name: "IORMedium",
                    displayName: "IOR Medium",
                    tooltip: "refractive index of the medium that the light refracts into."
                ),
                new ParameterUIDescriptor(
                    name: "IORMediumK",
                    displayName: "IOR Medium K",
                    tooltip: "refractive index Medium (imaginary part), or the medium causing the refraction."
                ),
                new ParameterUIDescriptor(
                    name: "Fresnel",
                    tooltip: "The fresnel coefficient, which describes the amount of light reflected or transmitted."
                )
            }
        );
    }
}
