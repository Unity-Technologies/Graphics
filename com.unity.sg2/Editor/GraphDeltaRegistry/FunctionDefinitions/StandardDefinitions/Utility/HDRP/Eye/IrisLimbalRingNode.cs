using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class IrisLimbalRingNode : IStandardNode
    {
        public static string Name => "IrisLimbalRing";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"    NdotV = dot(float3(0.0, 0.0, 1.0), ViewDirectionOS);

    // Compute the normalized iris position
    irisUVCentered = (IrisUV - 0.5f) * 2.0f;

    // Compute the radius of the point inside the eye
    localIrisRadius = length(irisUVCentered);
    LimbalRingFactor = localIrisRadius > (1.0 - LimbalRingSize) ? lerp(0.1, 1.0, saturate(1.0 - localIrisRadius) / LimbalRingSize) : 1.0;
    LimbalRingFactor = PositivePow(LimbalRingFactor, LimbalRingIntensity);
    LimbalRingFactor = lerp(LimbalRingFactor, PositivePow(LimbalRingFactor, LimbalRingFade), 1.0 - NdotV);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("IrisUV", TYPE.Vec2, Usage.In),
                new ParameterDescriptor("ViewDirectionOS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("LimbalRingSize", TYPE.Float, Usage.In),
                new ParameterDescriptor("LimbalRingFade", TYPE.Float, Usage.In),
                new ParameterDescriptor("LimbalRingIntensity", TYPE.Float, Usage.In),
                new ParameterDescriptor("LimbalRingFactor", TYPE.Float, Usage.Out),
                new ParameterDescriptor("NdotV", TYPE.Float, Usage.Local),
                new ParameterDescriptor("irisUVCentered", TYPE.Vec2, Usage.Local),
                new ParameterDescriptor("localIrisRadius", TYPE.Float, Usage.Local)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Iris Limbal Ring",
            tooltip: "calculates the intensity of the Limbal ring, a darkening feature of eyes.",
            category: "Utility/HDRP/Eye",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[6] {
                new ParameterUIDescriptor(
                    name: "IrisUV",
                    displayName: "Iris UV",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "ViewDirectionOS",
                    displayName: "View Direction OS",
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
