using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    class CollisionSphere : CollisionShapeBase
    {
        public class InputProperties
        {
            [Tooltip("Sets the sphere with which particles can collide.")]
            public TSphere sphere = TSphere.defaultValue;
        }

        public override IEnumerable<VFXNamedExpression> GetParameters(CollisionBase collisionBase, IEnumerable<VFXNamedExpression> collisionBaseParameters)
        {
            VFXExpression transform = null;
            VFXExpression radius = null;

            foreach (var param in collisionBaseParameters)
            {
                if (param.name.StartsWith("sphere"))
                {
                    if (param.name == "sphere_transform")
                        transform = param.exp;
                    if (param.name == "sphere_radius")
                        radius = param.exp;

                    continue; //exclude all automatic sphere inputs
                }
                yield return param;
            }

            VFXExpression finalTransform;

            //Integrate directly the radius into the common transform matrix
            var radiusScale = VFXOperatorUtility.UniformScaleMatrix(radius);
            finalTransform = new VFXExpressionTransformMatrix(transform, radiusScale);

            var isZeroScaled = VFXOperatorUtility.IsTRSMatrixZeroScaled(finalTransform);
            yield return new VFXNamedExpression(isZeroScaled, "isZeroScaled");

            var isUniformScaled = VFXOperatorUtility.IsTRSMatrixUniformScaled(finalTransform);
            yield return new VFXNamedExpression(isUniformScaled, "isUniformScale");

            yield return new VFXNamedExpression(finalTransform, "fieldTransform");
            yield return new VFXNamedExpression(new VFXExpressionInverseTRSMatrix(finalTransform), "invFieldTransform");
            
            VFXExpression scale = new VFXExpressionExtractScaleFromMatrix(finalTransform);
            yield return new VFXNamedExpression(VFXOperatorUtility.Reciprocal(scale), "invScale");
            yield return new VFXNamedExpression(scale, "scale");                
        }

        public override string GetSource(CollisionBase collisionBase)
        {

            var Source = string.Format("const bool kUseParticleRadius = {0};\n",
                collisionBase.radiusMode == CollisionBase.RadiusMode.None ? "false" : "true");

            Source += @"
if (isZeroScaled)
    return;

// Scale the ellipsoid to account for particle radius. This is not exactly correct as solving the distance function of an ellipsoid
// d(x,y,z) = r does not define an ellipsoid for r != 0 but it's a good enough approximation. The error will be noticeable only for very squashed ellipoids at boundaries
float3 s = float3(1,1,1);
if (kUseParticleRadius)
{
    s = (colliderSign * radius) * invScale + 1.0f;
    fieldTransform = mul(fieldTransform, GetScaleMatrix44(s));
    invFieldTransform = mul(GetScaleMatrix44(rcp(s)), invFieldTransform);
}

float3 tVel = mul(invFieldTransform, float4(velocity, 0.0f)).xyz;
float3 tPos = mul(invFieldTransform, float4(position, 1.0f)).xyz;
float3 dPos = tVel * deltaTime;
 
// unit sphere / ray intersection
float a = dot(dPos,dPos);
float b = dot(tPos,dPos);
float c = dot(tPos,tPos) - 1.0f;

float d = b*b - a*c;
tHit = -1.0f;
if (d >= 0) // Line intersection
{
    d = sqrt(d);
    float t0 = -(b + d);
    float t1 = -(b - d);
    tHit = t0 >= 0 ? t0 : t1;
}

if (tHit >= 0 && tHit < a) // Intersection with ray
{
    hit = true;
    tHit /= a;
    tPos += tHit * dPos; // Point of intersection
}
else if (c * colliderSign < 0) // Inside volume
{
    hit = true;
    tHit = 0.0f;
    if (isUniformScale) // Fast path for spheres
    {
        // Simply teleport on closest point on sphere
        tPos *= rsqrt(c + 1);
    }
    else
    {
#ifdef FAST_COLLISIONS
        float3 u = invScale;
        float3 n = normalize(tPos * u);
        b = dot(tPos,n);
        d = sqrt(b*b - c);
        tPos -= (b - d) * n;
#else
        // Find closest point on ellipsoid   
        // https://www.geometrictools.com/Documentation/DistancePointEllipseEllipsoid.pdf
        float3 z = abs(tPos) + VFX_EPSILON;
        float3 r = scale * s;
        float minScale = Min3(r.x, r.y, r.z);
        r *= rcp(minScale); 
        r = r * r;
        float3 m = r * z;
        float t0 = -1;
        if (r.y < r.x)
            t0 += r.z < r.y ? z.z : z.y;
        else
            t0 += r.z < r.x ? z.z : z.x;
        float t1 = colliderSign > 0 ? 0 : length(m) - 1;

        // Find root using bisection with N iteration
        const int ITERATION_COUNT = 16;
        float t = 0;
        int i = 0;
        for (i = 0; i < ITERATION_COUNT; ++i)
        {
            t = (t0 + t1) * 0.5f;
            float3 ratio = m * rcp(t + r);
            float dr = dot(ratio,ratio) - 1;
            if (abs(min(dr,t1 - t0)) < VFX_EPSILON * 10)
                break;
            else if (dr > 0)
                t0 = t;
            else
                t1 = t;
        }
        tPos *= (r + VFX_EPSILON * colliderSign) / (t + r);
#endif
    }
}

if (hit)
{
    hitPos = mul(fieldTransform, float4(tPos, 1.0f)).xyz;
    hitNormal = VFXSafeNormalize(mul(float4(tPos * colliderSign, 0.0f), invFieldTransform).xyz);
}
";              

            return Source;

        }
    }
}
