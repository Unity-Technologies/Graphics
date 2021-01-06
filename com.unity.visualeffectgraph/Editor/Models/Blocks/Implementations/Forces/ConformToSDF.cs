using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Force")]
    class ConformToSDF : VFXBlock
    {
        public override string name { get { return "Conform to Signed Distance Field"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.Update; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var input in GetExpressionsFromSlots(this))
                {
                    if (input.name == "FieldTransform")
                    {
                        yield return new VFXNamedExpression(new VFXExpressionInverseTRSMatrix(input.exp), "InvFieldTransform");
                        yield return new VFXNamedExpression(VFXOperatorUtility.Max3(new VFXExpressionExtractScaleFromMatrix(input.exp)), "scalingFactor");
                    }

                    yield return input;
                }

                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
            }
        }

        public class InputProperties
        {
            [Tooltip("Specifies the signed distance field texture to which particles can conform.")]
            public Texture3D DistanceField = VFXResources.defaultResources.signedDistanceField;
            [Tooltip("Sets the transform with which to position, scale, or rotate the field.")]
            public OrientedBox FieldTransform = OrientedBox.defaultValue;
            [Tooltip("Sets the speed with which particles are attracted towards the signed distance field.")]
            public float attractionSpeed = 5.0f;
            [Tooltip("Sets the strength of the force pulling particles towards the signed distance field.")]
            public float attractionForce = 20.0f;
            [Tooltip("Sets the distance at which particles attempt to stick to the signed distance field.")]
            public float stickDistance = 0.1f;
            [Tooltip("Sets the strength of the force keeping particles on the signed distance field.")]
            public float stickForce = 50.0f;
        }

        public override string source
        {
            get
            {
                return @"
float3 tPos = mul(InvFieldTransform, float4(position,1.0f)).xyz;
float3 coord = saturate(tPos + 0.5f);
float dist = SampleSDF(DistanceField, coord);

float3 absPos = abs(tPos);
float outsideDist = max(absPos.x,max(absPos.y,absPos.z));
float3 dir;
if (outsideDist > 0.5f) // Check wether point is outside the box
{
    // in that case just move towards center
    dist += outsideDist - 0.5f;
    dir = normalize(float3(FieldTransform[0][3],FieldTransform[1][3],FieldTransform[2][3]) - position);
}
else
{
    // compute normal
    dir = SampleSDFDerivativesFast(DistanceField, coord, dist);
    if (dist > 0)
        dir = -dir;
    dir = normalize(mul(float4(dir,0), InvFieldTransform).xyz);
}

float distToSurface = abs(dist) * scalingFactor;

float spdNormal = dot(dir,velocity);
float ratio = smoothstep(0.0,stickDistance * 2.0,abs(distToSurface));
float tgtSpeed = sign(distToSurface) * attractionSpeed * ratio;
float deltaSpeed = tgtSpeed - spdNormal;
velocity += sign(deltaSpeed) * min(abs(deltaSpeed),deltaTime * lerp(stickForce,attractionForce,ratio)) * dir / mass ;";
            }
        }
    }
}
