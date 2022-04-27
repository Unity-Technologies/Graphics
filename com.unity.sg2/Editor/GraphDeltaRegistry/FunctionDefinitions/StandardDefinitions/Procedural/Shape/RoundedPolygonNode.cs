using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class RoundedPolygonNode : IStandardNode
    {
        static string Name = "RoundedPolygon";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
{
    UV = UV * 2. + negone;
    UV.x = UV.x / ( Width + (Width==0)*1e-6);
    UV.y = UV.y / ( Height + (Height==0)*1e-6);
    Roundness = clamp(Roundness, 1e-6, 1.);
    fullAngle = 2. * PI / (floor( abs( Sides ) ));
    halfAngle = fullAngle / 2.;
    diagonal = 1. / cos( halfAngle );
    // Chamfer values
    chamferAngle = Roundness * halfAngle; // Angle taken by the chamfer
    remainingAngle = halfAngle - chamferAngle; // Angle that remains
    // Center of the chamfer arc
	sinecosine.x = cos(halfAngle);
	sinecosine.y = sin(halfAngle);
    chamferCenter = sinecosine * (tan(remainingAngle) / tan(halfAngle)) * diagonal;
    // Using Al Kashi algebra, we determine:
    // The distance distance of the center of the chamfer to the center of the polygon (side A)
    distA = length(chamferCenter);
    // The radius of the chamfer (side B)
    distB = 1. - chamferCenter.x;
    // This will rescale the chamfered polygon to fit the uv space
    UV *= diagonal;
    polaruv.x = atan2( UV.y, UV.x );
	polaruv.y = length(UV);
    polaruv.x += HALF_PI + 2*PI;
    polaruv.x = fmod( polaruv.x + halfAngle, fullAngle );
    polaruv.x = abs(polaruv.x - halfAngle);
    UV.x = cos(polaruv.x);
	UV.y = sin(polaruv.x);
	UV *= polaruv.y;
    // Calculate the distance of the polygon center to the chamfer extremity
    distC = sqrt( distA*distA + distB*distB - 2.*distA*distB*cos( PI - halfAngle * (1. - (polaruv.x-remainingAngle) / chamferAngle) ) );
    Out = lerp( UV.x, polaruv.y / distC, (( halfAngle - polaruv.x ) < chamferAngle) );
    // Output this to have the shape mask instead of the distance field
#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate((1 - Out) * 1e7);
#else
    Out = saturate((1 - Out) / fwidth(Out));
#endif
}",
            new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),
            new ParameterDescriptor("Width", TYPE.Float, Usage.In, new float[] {0.5f}),
            new ParameterDescriptor("Height", TYPE.Float, Usage.In, new float[] {0.5f}),
            new ParameterDescriptor("Sides", TYPE.Float, Usage.In, new float[] { 5f }),
            new ParameterDescriptor("Roundness", TYPE.Float, Usage.In, new float[] { 0.3f }),
            new ParameterDescriptor("negone", TYPE.Vec2, Usage.Local, new float[] { -1.0f, -1.0f }),
            new ParameterDescriptor("fullAngle", TYPE.Float, Usage.Local),
            new ParameterDescriptor("halfAngle", TYPE.Float, Usage.Local),
            new ParameterDescriptor("diagonal", TYPE.Float, Usage.Local),
            new ParameterDescriptor("chamferAngle", TYPE.Float, Usage.Local),
            new ParameterDescriptor("remainingAngle", TYPE.Float, Usage.Local),
            new ParameterDescriptor("sinecosine", TYPE.Vec2, Usage.Local),
            new ParameterDescriptor("chamferCenter", TYPE.Vec2, Usage.Local),
            new ParameterDescriptor("polaruv", TYPE.Vec2, Usage.Local),
            new ParameterDescriptor("distA", TYPE.Float, Usage.Local),
            new ParameterDescriptor("distB", TYPE.Float, Usage.Local),
            new ParameterDescriptor("distC", TYPE.Float, Usage.Local),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "generates a polygon shape with rounded corners",
            categories: new string[2] { "Procedural", "Shape" },
            synonyms: new string[1] { "shape" },
            parameters: new ParameterUIDescriptor[6] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the coordinates used to create the rectangle"
                ),
                new ParameterUIDescriptor(
                    name: "Width",
                    tooltip: "the width of the polygon"
                ),
                new ParameterUIDescriptor(
                    name: "Height",
                    tooltip: "the height of the polygon"
                ),
                new ParameterUIDescriptor(
                    name: "Sides",
                    tooltip: "number of sides of the polygon"
                ),
                new ParameterUIDescriptor(
                    name: "Roundness",
                    tooltip: "roundness of the corners"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "an n-sided polygon with rounded corners"
                )
            }
        );
    }
}
