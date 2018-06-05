using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Collision")]
    class CollisionSDF : CollisionBase
    {
        public override string name { get { return "Collide (Signed Distance Field)"; } }

        public class InputProperties
        {
            public Texture3D DistanceField = VFXResources.defaultResources.signedDistanceField;
            public Transform FieldTransform = Transform.defaultValue;
        }

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

        public override string source
        {
            get
            {
                string Source = @"
float3 nextPos = position + velocity * deltaTime;

float3 tPos = mul(InvFieldTransform, float4(nextPos,1.0f)).xyz;
float3 coord = saturate(tPos + 0.5f);
float dist = SampleSDF(DistanceField, coord);

if (colliderSign * dist <= 0.0f) // collision
{
    float3 n = SampleSDFDerivatives(DistanceField, coord, dist);

    // back in system space
    float3 delta = colliderSign * mul(FieldTransform,float4(normalize(n) * abs(dist),0)).xyz;
    n = normalize(delta);
";

                Source += collisionResponseSource;
                Source += @"
    position += delta;
}";
                return Source;
            }
        }
    }
}
