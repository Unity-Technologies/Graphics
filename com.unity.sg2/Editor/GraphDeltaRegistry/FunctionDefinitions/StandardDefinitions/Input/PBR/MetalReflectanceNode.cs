using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class MetalReflectanceNode : IStandardNode
    {
        static string Name => "MetalReflectance";
        static int Version => 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "Iron",
                    "    Out = metal;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("metal", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.560f, 0.570f, 0.580f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Silver",
                    "    Out = metal;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("metal", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.972f, 0.960f, 0.915f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Aluminium",
                    "    Out = metal;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("metal", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.913f, 0.921f, 0.925f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Gold",
                    "    Out = metal;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("metal", TYPE.Vec3, GraphType.Usage.Local, new float[] { 1.000f, 0.766f, 0.336f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Copper",
                    "    Out = metal;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("metal", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.955f, 0.637f, 0.538f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Chromium",
                    "    Out = metal;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("metal", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.550f, 0.556f, 0.554f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Nickel",
                    "    Out = metal;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("metal", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.660f, 0.609f, 0.526f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Titanium",
                    "    Out = metal;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("metal", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.542f, 0.497f, 0.449f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Cobalt",
                    "    Out = metal;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("metal", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.662f, 0.655f, 0.634f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Platinum",
                    "    Out = metal;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("metal", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.672f, 0.637f, 0.585f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Brass",
                    "    Out = metal;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("metal", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.888f, 0.745f, 0.451f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Lead",
                    "    Out = metal;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("metal", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.491f, 0.558f, 0.591f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Tin",
                    "    Out = metal;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("metal", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.723f, 0.584f, 0.479f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Steel",
                    "    Out = metal;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("metal", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.61f, 0.546f, 0.509f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Bronze",
                    "    Out = metal;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("metal", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.88f, 0.591f, 0.558f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Tungsten",
                    "    Out = metal;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("metal", TYPE.Vec3, GraphType.Usage.Local, new float[] { 0.503f, 0.491f, 0.479f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gives a specified metal reflectance value.",
            category: "Input/PBR",
            synonyms: Array.Empty<string>(),
            displayName: "Metal Reflectance",
            description: "pkg://Documentation~/previews/MetalReflectance.md",
            selectableFunctions: new Dictionary<string, string>
            {
                { "Iron", "Iron" },
                { "Silver", "Silver" },
                { "Aluminium", "Aluminium" },
                { "Gold", "Gold" },
                { "Copper", "Copper" },
                { "Chromium", "Chromium" },
                { "Nickel", "Nickel" },
                { "Titanium", "Titanium" },
                { "Cobalt", "Cobalt" },
                { "Platinum", "Platinum" },
                { "Brass", "Brass" },
                { "Lead", "Lead" },
                { "Tin", "Tin" },
                { "Steel", "Steel" },
                { "Bronze", "Bronze" },
                { "Tungsten", "Tungsten" }
            },
            hasModes: true,
            functionSelectorLabel: "Material",
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the reflectance value of the metal selected with the dropdown"
                )
            }
        );
    }
}
