using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Collision")]
    class CollisionSDF : CollisionBase
    {
        public override string name { get { return "Collide with Signed Distance Field"; } }

        public class InputProperties
        {
            public Texture3D DistanceField = VFXResources.defaultResources.signedDistanceField;
            public OrientedBox FieldTransform = OrientedBox.defaultValue;
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
                    {
                        yield return new VFXNamedExpression(new VFXExpressionInverseTRSMatrix(input.exp), "InvFieldTransform");
                        yield return new VFXNamedExpression( VFXOperatorUtility.Max3(new VFXExpressionExtractScaleFromMatrix(input.exp)) , "scalingFactor");
                    }
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
float dist = SampleSDF(DistanceField, coord) * scalingFactor - colliderSign * radius;


if (colliderSign * dist <= 0.0f) // collision
{
    float3 n = SampleSDFDerivatives(DistanceField, coord);
    // back in system space
    float3 delta = colliderSign * abs(dist) * normalize(mul(float4(n ,0), InvFieldTransform).xyz)  ;
    n = normalize(delta);
";

                Source += collisionResponseSource;

                if (mode == Mode.Inverted)
                {
                    Source += @"
    float3 absPos = abs(tPos);
    float outsideDist = max(absPos.x,max(absPos.y,absPos.z));
    if (outsideDist > 0.5f) // Check whether point is outside the box
        position = mul(FieldTransform,float4(coord - 0.5f,1)).xyz;
    

";
                }

                Source += @"
    position += delta;
}";
                return Source;
            }
        }
    }
}
