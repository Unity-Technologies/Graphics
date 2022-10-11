using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ScleraLimbalRingNode : IStandardNode
    {
        public static string Name => "ScleraLimbalRing";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"    // Compute the radius of the point inside the eye
    scleraRadius = length(PositionOS.xy);
    LimbalRingFactor = scleraRadius > IrisRadius ? (scleraRadius > (LimbalRingSize + IrisRadius) ? 1.0 : lerp(0.5, 1.0, (scleraRadius - IrisRadius) / (LimbalRingSize))) : 1.0;
    LimbalRingFactor = PositivePow(LimbalRingFactor, LimbalRingIntensity);
    LimbalRingFactor = lerp(LimbalRingFactor, PositivePow(LimbalRingFactor, LimbalRingFade), 1.0 - dot(float3(0.0, 0.0, 1.0), ViewDirectionOS));",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("PositionOS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("ViewDirectionOS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("IrisRadius", TYPE.Float, Usage.In),
                new ParameterDescriptor("LimbalRingSize", TYPE.Float, Usage.In),
                new ParameterDescriptor("LimbalRingFade", TYPE.Float, Usage.In),
                new ParameterDescriptor("LimbalRingIntensity", TYPE.Float, Usage.In),
                new ParameterDescriptor("LimbalRingFactor", TYPE.Float, Usage.Out),
                new ParameterDescriptor("scleraRadius", TYPE.Float, Usage.Local)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Sclera Limbal Ring",
            tooltip: "calculates the intensity of the Sclera ring, a darkening feature of eyes.",
            category: "Utility/HDRP/Eye",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[7] {
                new ParameterUIDescriptor(
                    name: "PositionOS",
                    displayName: "Position OS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "ViewDirectionOS",
                    displayName: "View Direction OS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "IrisRadius",
                    displayName: "Iris Radius",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "LimbalRingSize",
                    displayName: "Limbal Ring Size",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "LimbalRingFade",
                    displayName: "Limbal Ring Fade",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "LimbalRingIntensity",
                    displayName: "Limbal Ring Intensity",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "LimbalRingFactor",
                    displayName: "Limbal Ring Factor",
                    tooltip: ""
                )
            }
        );
    }
}
