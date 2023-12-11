using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    class CollisionPlaneDeprecatedV2 : CollisionBase
    {
        public override string name { get { return "Collide with Plane"; } }

        public class InputProperties
        {
            [Tooltip("Sets the plane with which particles can collide.")]
            public Plane Plane = new Plane() { normal = Vector3.up };
        }

        public override void Sanitize(int version)
        {
            var newCollisionPlaneShape = ScriptableObject.CreateInstance<CollisionShape>();
            SanitizeHelper.MigrateBlockCollisionShapeToComposed(newCollisionPlaneShape, this, CollisionShapeBase.Type.Plane);
            ReplaceModel(newCollisionPlaneShape, this);
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in base.parameters)
                    yield return p;

                VFXExpression sign = (mode == Mode.Solid) ? VFXValue.Constant(1.0f) : VFXValue.Constant(-1.0f);
                VFXExpression position = inputSlots[0][0].GetExpression();
                VFXExpression normal = inputSlots[0][1].GetExpression() * VFXOperatorUtility.CastFloat(sign, VFXValueType.Float3);

                var plane = new List<VFXExpression>(VFXOperatorUtility.ExtractComponents(normal));
                plane.Add(VFXOperatorUtility.Dot(position, normal));

                yield return new VFXNamedExpression(new VFXExpressionCombine(plane.ToArray()), "plane");
            }
        }
    }
}
