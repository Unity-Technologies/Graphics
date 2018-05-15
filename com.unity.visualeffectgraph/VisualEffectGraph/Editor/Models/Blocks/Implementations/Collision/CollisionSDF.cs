using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Collision")]
    class CollisionSDF : VFXBlock
    {
        public override string name { get { return "Collide with SDF"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var input in GetExpressionsFromSlots(this))
                {
                    if (input.name == "FieldTransform")
                        yield return new VFXNamedExpression(new VFXExpressionInverseMatrix(input.exp), "InvFieldTransform");
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
                //yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
            }
        }

        public class InputProperties
        {
            public Texture3D DistanceField;
            public Transform FieldTransform = Transform.defaultValue;
            public float Elasticity = 0.1f;
            public float Friction = 0.2f;
        }

        public override string source
        {
            get
            {
                return @"
float3 nextPos = position + velocity * deltaTime;
float3 tPos = mul(InvFieldTransform, float4(position,1.0f)).xyz;

float3 coord = tPos + 0.5f;
float dist = SampleTexture(DistanceField, coord).x;

if (dist <= 0.0f) // collision
{
    float3 n;
    n.x = SampleTexture(DistanceField, coord + float3(0.01,0,0)).x;
    n.y = SampleTexture(DistanceField, coord + float3(0,0.01,0)).x;
    n.z = SampleTexture(DistanceField, coord + float3(0,0,0.01)).x;
    n = normalize((float3)dist - n);

    tPos += n * dist; // push on boundaries
    
    // back in system space
    position = mul(FieldTransform,float4(tPos,1.0f)).xyz;
    n = normalize(mul(FieldTransform,float4(n,0)));    

    float projVelocity = dot(n,velocity);
	if (projVelocity > 0)
    {
        float3 nVelocity = projVelocity * n; // normal component
        float3 tVelocity = velocity - nVelocity; // tangential component

        velocity -= (1 + saturate(Elasticity)) * nVelocity;
        velocity -= saturate(Friction) * tVelocity;

        //position -= velocity * deltaTime;
    }
}";
                ;
            }
        }
    }
}
