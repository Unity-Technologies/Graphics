using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Rounded Polygon")]
    class RoundedPolygonNode : CodeFunctionNode
    {
        public RoundedPolygonNode()
        {
            name = "Rounded Polygon";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("RoundedPolygon_Func", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string RoundedPolygon_Func(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0, 0, 0)] Vector1 Width,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Vector1 Height,
            [Slot(3, Binding.None, 5f, 0, 0, 0)] Vector1 Sides,
            [Slot(4, Binding.None, 0.3f, 0, 0, 0)] Vector1 Roundness,
            [Slot(5, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
	UV = UV * 2. + $precision2(-1.,-1.);
    $precision epsilon = 1e-6;
    UV.x = UV.x / ( Width + (Width==0)*epsilon);
    UV.y = UV.y / ( Height + (Height==0)*epsilon);
    Roundness = clamp(Roundness, 1e-6, 1.);
    $precision i_sides = floor( abs( Sides ) );
    $precision fullAngle = 2. * PI / i_sides;
    $precision halfAngle = fullAngle / 2.;
    $precision opositeAngle = HALF_PI - halfAngle;
    $precision diagonal = 1. / cos( halfAngle );
    // Chamfer values
    $precision chamferAngle = Roundness * halfAngle; // Angle taken by the chamfer
    $precision remainingAngle = halfAngle - chamferAngle; // Angle that remains
    $precision ratio = tan(remainingAngle) / tan(halfAngle); // This is the ratio between the length of the polygon's triangle and the distance of the chamfer center to the polygon center
    // Center of the chamfer arc
    $precision2 chamferCenter = $precision2(
        cos(halfAngle) ,
        sin(halfAngle)
    )* ratio * diagonal;
    // starting of the chamfer arc
    $precision2 chamferOrigin = $precision2(
        1.,
        tan(remainingAngle)
    );
    // Using Al Kashi algebra, we determine:
    // The distance distance of the center of the chamfer to the center of the polygon (side A)
    $precision distA = length(chamferCenter);
    // The radius of the chamfer (side B)
    $precision distB = 1. - chamferCenter.x;
    // The refence length of side C, which is the distance to the chamfer start
    $precision distCref = length(chamferOrigin);
    // This will rescale the chamfered polygon to fit the uv space
    // diagonal = length(chamferCenter) + distB;
    $precision uvScale = diagonal;
    UV *= uvScale;
    $precision2 polaruv = $precision2 (
        atan2( UV.y, UV.x ),
        length(UV)
    );
    polaruv.x += HALF_PI + 2*PI;
    polaruv.x = fmod( polaruv.x + halfAngle, fullAngle );
    polaruv.x = abs(polaruv.x - halfAngle);
    UV = $precision2( cos(polaruv.x), sin(polaruv.x) ) * polaruv.y;
    // Calculate the angle needed for the Al Kashi algebra
    $precision angleRatio = 1. - (polaruv.x-remainingAngle) / chamferAngle;
    // Calculate the distance of the polygon center to the chamfer extremity
    $precision distC = sqrt( distA*distA + distB*distB - 2.*distA*distB*cos( PI - halfAngle * angleRatio ) );
    Out = UV.x;
    float chamferZone = ( halfAngle - polaruv.x ) < chamferAngle;
    Out = lerp( UV.x, polaruv.y / distC, chamferZone );
	// Output this to have the shape mask instead of the distance field
	Out = saturate((1 - Out) / fwidth(Out));
}
";
        }
    }
}