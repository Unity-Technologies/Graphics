# Rounded Polygon Node

## Description

Generates a rounded polygon shape based on input **UV** at the size specified by inputs **Width** and **Height**. The input **Sides** specifies the number of sides, and the input **Roundness** defines the roundness of each corner.

You can connect a [Tiling And Offset Node](Tiling-And-Offset-Node.md) to offset or tile the shape. To preserve the ability to offset the shape within the UV space, the shape does not automatically repeat if you tile it. To achieve a repeating rounded polygon effect, first connect your **UV** input through a [Fraction Node](Fraction-Node.md).

You can only use the Rounded Polygon Node in the **Fragment** [Shader Stage](Shader-Stage.md).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Width      | Input | Vector 1 | None | Rounded Polygon width |
| Height      | Input | Vector 1 | None | Rounded Polygon height |
| Sides      | Input | Vector 1 | None | Number of sides of the polygon |
| Roundness      | Input | Vector 1 | None | Roundness of corners |
| Out | Output      |    Vector 1 | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void RoundedPolygon_Func_float(float2 UV, float Width, float Height, float Sides, float Roundness, out float Out)
{
    UV = UV * 2. + float2(-1.,-1.);
    float epsilon = 1e-6;
    UV.x = UV.x / ( Width + (Width==0)*epsilon);
    UV.y = UV.y / ( Height + (Height==0)*epsilon);
    Roundness = clamp(Roundness, 1e-6, 1.);
    float i_sides = floor( abs( Sides ) );
    float fullAngle = 2. * PI / i_sides;
    float halfAngle = fullAngle / 2.;
    float opositeAngle = HALF_PI - halfAngle;
    float diagonal = 1. / cos( halfAngle );
    // Chamfer values
    float chamferAngle = Roundness * halfAngle; // Angle taken by the chamfer
    float remainingAngle = halfAngle - chamferAngle; // Angle that remains
    float ratio = tan(remainingAngle) / tan(halfAngle); // This is the ratio between the length of the polygon's triangle and the distance of the chamfer center to the polygon center
    // Center of the chamfer arc
    float2 chamferCenter = float2(
        cos(halfAngle) ,
        sin(halfAngle)
    )* ratio * diagonal;
    // starting of the chamfer arc
    float2 chamferOrigin = float2(
        1.,
        tan(remainingAngle)
    );
    // Using Al Kashi algebra, we determine:
    // The distance distance of the center of the chamfer to the center of the polygon (side A)
    float distA = length(chamferCenter);
    // The radius of the chamfer (side B)
    float distB = 1. - chamferCenter.x;
    // The refence length of side C, which is the distance to the chamfer start
    float distCref = length(chamferOrigin);
    // This will rescale the chamfered polygon to fit the uv space
    // diagonal = length(chamferCenter) + distB;
    float uvScale = diagonal;
    UV *= uvScale;
    float2 polaruv = float2 (
        atan2( UV.y, UV.x ),
        length(UV)
    );
    polaruv.x += HALF_PI + 2*PI;
    polaruv.x = fmod( polaruv.x + halfAngle, fullAngle );
    polaruv.x = abs(polaruv.x - halfAngle);
    UV = float2( cos(polaruv.x), sin(polaruv.x) ) * polaruv.y;
    // Calculate the angle needed for the Al Kashi algebra
    float angleRatio = 1. - (polaruv.x-remainingAngle) / chamferAngle;
    // Calculate the distance of the polygon center to the chamfer extremity
    float distC = sqrt( distA*distA + distB*distB - 2.*distA*distB*cos( PI - halfAngle * angleRatio ) );
    Out = UV.x;
    float chamferZone = ( halfAngle - polaruv.x ) < chamferAngle;
    Out = lerp( UV.x, polaruv.y / distC, chamferZone );
    // Output this to have the shape mask instead of the distance field
    Out = saturate((1 - Out) / fwidth(Out));
}
```