using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class CorneaRefractionNode : IStandardNode
    {
        public static string Name => "CorneaRefraction";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"    CorneaNormalOS = normalize(CorneaNormalOS);
    ViewDirectionOS = -normalize(ViewDirectionOS);
    refractedViewDirectionOS = refract(ViewDirectionOS, CorneaNormalOS, 1.0 / CorneaIOR);

    // Find the distance to intersection point
    t = -(PositionOS.z + IrisPlaneOffset) / refractedViewDirectionOS.z;

    // Output the refracted point in OS
    RefractedPositionOS = float3(refractedViewDirectionOS.z < 0 ? PositionOS.xy + refractedViewDirectionOS.xy * t: float2(1.5, 1.5), 0.0);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("PositionOS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("ViewDirectionOS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("CorneaNormalOS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("CorneaIOR", TYPE.Float, Usage.In, new float[] { 1.333f }),
                new ParameterDescriptor("IrisPlaneOffset", TYPE.Float, Usage.In, new float[] { 0.02f }),
                new ParameterDescriptor("RefractedPositionOS", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("refractedViewDirectionOS", TYPE.Vec3, Usage.Local),
                new ParameterDescriptor("t", TYPE.Float, Usage.Local)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Cornea Refraction",
            tooltip: "Calculates the refraction of the view ray in object space to return the object space position.",
            category: "Utility/HDRP/Eye",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[6] {
                new ParameterUIDescriptor(
                    name: "PositionOS",
                    displayName: "Position OS",
                    tooltip: "Position of the fragment to shade in object space."
                ),
                new ParameterUIDescriptor(
                    name: "ViewDirectionOS",
                    displayName: "View Direction OS",
                    tooltip: "Direction of the incident ray in object space"
                ),
                new ParameterUIDescriptor(
                    name: "CorneaNormalOS",
                    displayName: "Cornea Normal OS",
                    tooltip: "The normal of the eye surface in object space."
                ),
                new ParameterUIDescriptor(
                    name: "CorneaIOR",
                    displayName: "Cornea IOR",
                    tooltip: "The index of refraction of the eye (1.333 by default)"
                ),
                new ParameterUIDescriptor(
                    name: "IrisPlaneOffset",
                    displayName: "Iris Plane Offset",
                    tooltip: "Distance between the end of the cornea and the iris plane."
                ),
                new ParameterUIDescriptor(
                    name: "RefractedPositionOS",
                    displayName: "Refracted Position OS",
                    tooltip: "Position of the refracted point on the iris plane in object space"
                )
            }
        );
    }
}
