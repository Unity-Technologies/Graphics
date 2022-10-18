using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class RefractNode : IStandardNode
    {
        public static string Name => "Refract";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new (
                    "Safe",
@"   internalIORSource = max(IORSource, 1.0);
   internalIORMedium = max(IORMedium, 1.0);
   eta = internalIORSource/internalIORMedium;
   cos0 = dot(Incident, Normal);
   k = 1.0 - eta*eta*(1.0 - cos0*cos0);
   Refracted = eta*Incident - (eta*cos0 + sqrt(max(k, 0.0)))*Normal;
   Intensity = internalIORSource <= internalIORMedium ?
       saturate(F_Transm_Schlick(IorToFresnel0(internalIORMedium, internalIORSource), -cos0)) :
       (k >= 0.0 ? F_FresnelDielectric(internalIORMedium/internalIORSource, -cos0) : 0.0);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Incident", TYPE.Vec3, Usage.In, new float[] { 0.0f, 0.0f, 1.0f }),
                        new ParameterDescriptor("Normal", TYPE.Vec3, Usage.In, new float[] { 0.0f, 0.0f, -1.0f }),
                        new ParameterDescriptor("IORSource", TYPE.Float, Usage.In, new float[] { 1.0f }),
                        new ParameterDescriptor("IORMedium", TYPE.Float, Usage.In, new float[] { 1.5f }),
                        new ParameterDescriptor("Refracted", TYPE.Vec3, Usage.Out),
                        new ParameterDescriptor("Intensity", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("internalIORSource", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("internalIORMedium", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("eta", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("cos0", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("k", TYPE.Float, Usage.Local)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl\"",
                    }
                ),
                new (
                    "CriticalAngle",
@"   internalIORSource = max(IORSource, 1.0);
   internalIORMedium = max(IORMedium, 1.0);
   eta = internalIORSource/internalIORMedium;
   cos0 = dot(Incident, Normal);
   k = 1.0 - eta*eta*(1.0 - cos0*cos0);
   Refracted = k >= 0.0 ? eta*Incident - (eta*cos0 + sqrt(k))*Normal : reflect(Incident, Normal);
   Intensity = internalIORSource <= internalIORMedium ?
       saturate(F_Transm_Schlick(IorToFresnel0(internalIORMedium, internalIORSource), -cos0)) :
       (k >= 0.0 ? F_FresnelDielectric(internalIORMedium/internalIORSource, -cos0) : 1.0);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Incident", TYPE.Vec3, Usage.In, new float[] { 0.0f, 0.0f, 1.0f }),
                        new ParameterDescriptor("Normal", TYPE.Vec3, Usage.In, new float[] { 0.0f, 0.0f, -1.0f }),
                        new ParameterDescriptor("IORSource", TYPE.Float, Usage.In, new float[] { 1.0f }),
                        new ParameterDescriptor("IORMedium", TYPE.Float, Usage.In, new float[] { 1.5f }),
                        new ParameterDescriptor("Refracted", TYPE.Vec3, Usage.Out),
                        new ParameterDescriptor("Intensity", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("internalIORSource", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("internalIORMedium", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("eta", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("cos0", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("k", TYPE.Float, Usage.Local)
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
            tooltip: "Bends the Incident vector based on the index of refraction",
            category: "Math/Vector",
            synonyms: new string[3] { "warp", "bend", "distort" },
            selectableFunctions: new()
            {
                { "Safe", "Safe" },
                { "CriticalAngle", "Critical Angle" }
            },
            functionSelectorLabel: "Mode",
            parameters: new ParameterUIDescriptor[6] {
                new ParameterUIDescriptor(
                    name: "Incident",
                    tooltip: "The incoming vector to refract"
                ),
                new ParameterUIDescriptor(
                    name: "Normal",
                    tooltip: "The facing direction of the surface"
                ),
                new ParameterUIDescriptor(
                    name: "IORSource",
                    displayName: "IOR Source",
                    tooltip: "the index of refraction of the source medium"
                ),
                new ParameterUIDescriptor(
                    name: "IORMedium",
                    displayName: "IOR Medium",
                    tooltip: "the index of refraction of the target medium"
                ),
                new ParameterUIDescriptor(
                    name: "Refracted",
                    tooltip: "the refracted vector"
                ),
                new ParameterUIDescriptor(
                    name: "Intensity",
                    tooltip: "the strength of the refraction"
                )
            }
        );
    }
}
