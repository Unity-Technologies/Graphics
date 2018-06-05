using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Collision")]
    class CollisionSDF : CollisionBase
    {
        public override string name { get { return "Collide with Signed Distance Field"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var input in base.parameters)
                    yield return input;

                foreach (var input in GetExpressionsFromSlots(this))
                {
                    if (input.name == "FieldTransform")
                        yield return new VFXNamedExpression(new VFXExpressionInverseMatrix(input.exp), "InvFieldTransform");
                }
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);
            }
        }

        public class InputProperties
        {
            public Texture3D DistanceField;
            public Transform FieldTransform = Transform.defaultValue;
        }

        public override string source
        {
            get
            {
                string Source = @"
float3 nextPos = position + velocity * deltaTime;
float3 tPos = mul(InvFieldTransform, float4(position,1.0f)).xyz;
float invMass = 1.0/mass;

float3 coord = tPos + 0.5f;
float dist = SampleTexture(DistanceField, coord).x * colliderSign;

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
    n = normalize(mul(FieldTransform,float4(n,0)));";
                Source += collisionResponseSource;
                Source += @"
}";
                return Source;
            }
        }
    }
}
