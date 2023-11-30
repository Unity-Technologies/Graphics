using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    class CollisionPlane : CollisionShapeBase
    {
        public class InputProperties
        {
            [Tooltip("Sets the plane with which particles can collide.")]
            public Plane Plane = new Plane() {normal = Vector3.up};
        }

        public override IEnumerable<VFXNamedExpression> GetParameters(CollisionBase collisionBase, IEnumerable<VFXNamedExpression> collisionBaseParameters)
        {
            foreach (var p in base.GetParameters(collisionBase, collisionBaseParameters))
                yield return p;

            VFXExpression sign = (collisionBase.mode == CollisionBase.Mode.Solid)
                ? VFXValue.Constant(1.0f)
                : VFXValue.Constant(-1.0f);
            VFXExpression position = collisionBase.inputSlots[0][0].GetExpression();
            VFXExpression normal = collisionBase.inputSlots[0][1].GetExpression() *
                                   VFXOperatorUtility.CastFloat(sign, VFXValueType.Float3);

            var plane = new List<VFXExpression>(VFXOperatorUtility.ExtractComponents(normal));
            plane.Add(VFXOperatorUtility.Dot(position, normal));

            yield return new VFXNamedExpression(new VFXExpressionCombine(plane.ToArray()), "plane");
        }

        public override string GetSource(CollisionBase collisionBase)
        {
            string Source = @"
hitNormal = plane.xyz; // plane.xyz is already multiplied by collider sign
float w = plane.w;
float distA = dot(position, hitNormal) - w - radius;
float distB = distA + dot(velocity, hitNormal) * deltaTime;

if (distA > 0.0f && distB < 0.0f) // collision 
{
    hit = true;
    tHit = saturate(distA / (distA - distB)); // point of intersection
    hitPos = position + (tHit * deltaTime) * velocity;
}
else if (distA < VFX_EPSILON)  // Inside volume - (Plus an epsilon to improve stability)
{
    hit = true;
    tHit = 0.0f;
    hitPos = position - hitNormal * distA; // Teleport outside
}
";  

            return Source;
        }
    }
}
