using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    class CollisionOrientedBox : CollisionShapeBase
    {
        public class InputProperties
        {
            [Tooltip("Sets the oriented box with which particles can collide.")]
            public OrientedBox box = OrientedBox.defaultValue;
        }

        public override IEnumerable<VFXNamedExpression> GetParameters(CollisionBase collisionBase, IEnumerable<VFXNamedExpression> collisionBaseParameters)
        {
            foreach (var parameter in collisionBaseParameters)
            {
                if (parameter.name == nameof(InputProperties.box))
                {
                    yield return new VFXNamedExpression(VFXOperatorUtility.IsTRSMatrixZeroScaled(parameter.exp), "isZeroScaled");
                    VFXExpression finalTransform = parameter.exp;
                    VFXExpression scale = new VFXExpressionAbs(new VFXExpressionExtractScaleFromMatrix(finalTransform));
                    yield return new VFXNamedExpression(VFXOperatorUtility.Reciprocal(scale), "invScale");
                    yield return new VFXNamedExpression(scale, "scale");
                    yield return new VFXNamedExpression(finalTransform, "fieldTransform");
                    yield return new VFXNamedExpression(new VFXExpressionInverseTRSMatrix(parameter.exp), "invFieldTransform");
                    continue;
                }

                yield return parameter;
            }
        }

        public override string GetSource(CollisionBase collisionBase)
        {
            var Source = string.Format("const bool kUseParticleRadius = {0};",
                collisionBase.radiusMode == CollisionBase.RadiusMode.None ? "false" : "true");
            Source += @"
if (isZeroScaled)
    return;

float3 nextPos = position + deltaTime * velocity;
float3 tNextPos = mul(invFieldTransform, float4(nextPos, 1.0f)).xyz;
float3 halfBoxSize = 0.5f + radius * invScale * colliderSign;

float3 tDelta = mul(invFieldTransform, float4(deltaTime * velocity, 0)).xyz;
float3 tPos =  mul(invFieldTransform, float4(position, 1.0f)).xyz;

";

            if (collisionBase.mode == CollisionBase.Mode.Solid)
                Source += @"
if(DistanceToBox(tPos, halfBoxSize) <= length(tDelta)) //Potential hit
{
    if(DistanceToBox(tPos, halfBoxSize) >= 0)
    {
";
            else
                Source += @"
if(any(abs(tNextPos) > halfBoxSize))
{
    if( all(abs(tPos) <= halfBoxSize) )
    {
    ";
            Source += @"
        hit = RayBoxIntersection(tPos, tDelta, halfBoxSize, colliderSign, tHit, hitNormal);
        tPos += tHit * tDelta;
    }
    else
    {";
            if (collisionBase.mode == CollisionBase.Mode.Solid)
                Source += @"
        float3 distanceToEdge = abs(tPos) - halfBoxSize;
        float3 absDistanceToEdge = abs(distanceToEdge) * scale;

        float3 tPosSign = float3(FastSign(tPos.x), FastSign(tPos.y), FastSign(tPos.z));
        if (absDistanceToEdge.x < absDistanceToEdge.y && absDistanceToEdge.x < absDistanceToEdge.z)
            hitNormal = float3(tPosSign.x, 0.0f, 0.0f);
        else if (absDistanceToEdge.y < absDistanceToEdge.z)
            hitNormal = float3(0.0f, tPosSign.y, 0.0f);
        else
            hitNormal = float3(0.0f, 0.0f, tPosSign.z);

        hitNormal *= colliderSign;
        tPos -= hitNormal * distanceToEdge;
        hit = true;
    }";
            else
                Source += @"
        float3 distanceToEdge = max(0,abs(tPos) - halfBoxSize);

        float3 tPosSign = float3(FastSign(tPos.x), FastSign(tPos.y), FastSign(tPos.z));
        hitNormal = -normalize(tPosSign * distanceToEdge);
        tPos -= distanceToEdge * tPosSign ;
        hit = true;
    }";

            Source += @"
    hitPos = mul(fieldTransform, float4(tPos.xyz, 1.0f)).xyz;
    hitNormal = VFXSafeNormalize(mul(float4(hitNormal, 0.0f), invFieldTransform).xyz);
}";
            return Source;
        }
    }
}
