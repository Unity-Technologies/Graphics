using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class CollisionSDF : CollisionShapeBase
    {
        public class InputProperties
        {
            [Tooltip("Sets the Signed Distance Field to sample from.")]
            public Texture3D DistanceField = VFXResources.defaultResources.signedDistanceField;
            [Tooltip("Sets the transform with which to position, scale, or rotate the field.")]
            public OrientedBox FieldTransform = OrientedBox.defaultValue;
        }

        public override IEnumerable<VFXNamedExpression> GetParameters(CollisionBase collisionBase, IEnumerable<VFXNamedExpression> collisionBaseParameters)
        {
            VFXExpression transform = null;
            VFXExpression SDF = null;
            foreach (var p in base.GetParameters(collisionBase, collisionBaseParameters))
            {
                if (p.name == "FieldTransform")
                {
                    transform = p.exp;
                    VFXExpression scale = new VFXExpressionAbs(new VFXExpressionExtractScaleFromMatrix(transform));
                    yield return new VFXNamedExpression(VFXOperatorUtility.Reciprocal(scale), "invScale");
                    yield return new VFXNamedExpression(VFXOperatorUtility.IsTRSMatrixZeroScaled(transform), "isZeroScaled");
                    yield return new VFXNamedExpression(new VFXExpressionInverseTRSMatrix(transform), "InvFieldTransform");
                }

                if (p.name == "DistanceField")
                    SDF = p.exp;

                yield return p;
            }
       
            var w = new VFXExpressionCastUintToFloat(new VFXExpressionTextureWidth(SDF));
            var h = new VFXExpressionCastUintToFloat(new VFXExpressionTextureHeight(SDF));
            var d = new VFXExpressionCastUintToFloat(new VFXExpressionTextureDepth(SDF));
            var uvStep = VFXOperatorUtility.Reciprocal(new VFXExpressionCombine(w, h, d));
            var maxDim = VFXOperatorUtility.Max3(w, h, d);
            var textureDimScale = uvStep * new VFXExpressionCombine(maxDim, maxDim, maxDim);
            var textureDimInvScale = VFXOperatorUtility.Reciprocal(textureDimScale);
            var stepSize = VFXOperatorUtility.Reciprocal(maxDim);
            yield return new VFXNamedExpression(uvStep, "uvStep");
            yield return new VFXNamedExpression(textureDimScale, "textureDimScale");
            yield return new VFXNamedExpression(textureDimInvScale, "textureDimInvScale");
            yield return new VFXNamedExpression(stepSize, "stepSizeMeter");
        }

        public override string GetSource(CollisionBase collisionBase)
        {
            string handlingSelectionCode = collisionBase.mode == CollisionBase.Mode.Solid ? @"
if(currentDistanceToBox <= length(tDelta)) //Potential hit
{
    if(currentDistanceToBox >= 0) //Find first ray box intersection
    {
        float3 dummyNormal;
        bool boxHit = RayBoxIntersection(tPos, tDelta, halfBoxSize, 1, tHit, dummyNormal);
        needsSphereMarching = boxHit;
        tPos += tHit * tDelta;
    }
    else
    {
        float3 uvw = saturate(tPos + 0.5f);
        float dist = SampleSDF(DistanceField, uvw) - radiusOffset;
        needsSphereMarching = dist > 0;
        needsProjecting = !needsSphereMarching;
    }
}
" : @"
if(currentDistanceToBox > 0)
{
    needsProjecting = true;
    tPos = ProjectOnBox(tPos, halfBoxSize);
}
else
{
    float3 uvw = saturate(tPos + 0.5f);
    float dist = SampleSDF(DistanceField, uvw) - radiusOffset;
    needsSphereMarching = dist < 0 && abs(dist) <= length(tDelta) ;
    needsProjecting = dist >= 0;
}
";
            string sphereMarchingCode = @"
        float3 uvw = saturate(tPos + 0.5f);
        float dist = colliderSign * (SampleSDF(DistanceField, uvw) - radiusOffset);

        //Sphere March
        const int ITERATION_COUNT = 8;
        int i = 0;
        hit = false;
        float maxDist = length(tDelta * textureDimInvScale);
        for(i = 0; i < ITERATION_COUNT; i++)
        {
            uvw = uvw + tDir * textureDimScale * dist;
            float newDist = colliderSign * (SampleSDF(DistanceField, uvw) - radiusOffset);
            tHit += dist/maxDist;
            if(newDist < VFX_EPSILON)
            {
                hit = tHit <= 1 && tHit >= 0;
                break;
            }
            if(tHit > 1)
            {
                hit = false;
                break;
            }
            dist = newDist;
        }
        if(hit)
        {
            tPos = uvw - 0.5f;
            hitPos = mul(FieldTransform, float4(tPos, 1.0f)).xyz;
            hitNormal = SampleSDFUnscaledDerivatives(DistanceField, uvw, uvStep) * textureDimScale;
            hitNormal = colliderSign * VFXSafeNormalize(mul(float4(hitNormal, 0.0f), InvFieldTransform).xyz);
        }";

            string projectOnSurfaceCode = @"
        hit = true;
        const int ITERATION_COUNT = 4;
        int i = 0;
        float3 uvw = saturate(tPos + 0.5f);
        float3 sdfNormal = normalize(SampleSDFUnscaledDerivatives(DistanceField, uvw, uvStep));
        float radiusOffset = colliderSign * dot(sdfNormal*sdfNormal ,invScale * textureDimInvScale) * radius;

        for(i = 0; i < ITERATION_COUNT; i++)
        {
            uvw = IterateTowardSDFSurface(DistanceField, uvw, uvStep, radiusOffset, stepSizeMeter, sdfNormal);
        }
        tPos = uvw - 0.5f;
        hitPos = mul(FieldTransform, float4(tPos, 1.0f)).xyz;
        hitNormal = sdfNormal * textureDimScale;
        hitNormal = colliderSign * VFXSafeNormalize(mul(float4(hitNormal, 0.0f), InvFieldTransform).xyz);
        tHit = 0;
";


            var Source = new StringBuilder($@"
if (isZeroScaled)
    return;

float3 tPos = mul(InvFieldTransform, float4(position,1.0f)).xyz;
float3 tVel = mul(InvFieldTransform, float4(velocity, 0.0f)).xyz;
float3 tDelta = tVel * deltaTime;
float3 tDir = VFXSafeNormalize(tVel);
float radiusOffset = colliderSign * dot(tDir * tDir,invScale * textureDimInvScale) * radius;

float3 halfBoxSize = 0.5f + radius * invScale * colliderSign;
float currentDistanceToBox = DistanceToBox(tPos, halfBoxSize);

bool needsSphereMarching = false;
bool needsProjecting = false;
{handlingSelectionCode}

if(needsSphereMarching)
{{
   {sphereMarchingCode}
}}
else if (needsProjecting)
{{
    {projectOnSurfaceCode}
}}
else
{{
    hit = false;
    tHit = 0;
}}");
            return Source.ToString();
        }
    }
}
