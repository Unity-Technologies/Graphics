using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class CollisionConeDeprecatedV2 : CollisionBase
    {
        public override string name { get { return "Collide with Cone"; } }

        public class InputProperties
        {
            [Tooltip("Sets the cone with which particles can collide.")]
            public TCone cone = TCone.defaultValue;
        }

        public override void Sanitize(int version)
        {
            var newCollisionShape = ScriptableObject.CreateInstance<CollisionShape>();
            SanitizeHelper.MigrateBlockCollisionShapeToComposed(newCollisionShape, this, CollisionShapeBase.Type.Cone);
            ReplaceModel(newCollisionShape, this);
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                VFXExpression transform = null;
                VFXExpression height = null;
                VFXExpression baseRadius = null;
                VFXExpression topRadius = null;

                foreach (var param in base.parameters)
                {
                    if (param.name.StartsWith("cone"))
                    {
                        if (param.name == "cone_" + nameof(TCone.transform))
                            transform = param.exp;
                        if (param.name == "cone_" + nameof(TCone.height))
                            height = param.exp;
                        if (param.name == "cone_" + nameof(TCone.baseRadius))
                            baseRadius = param.exp;
                        if (param.name == "cone_" + nameof(TCone.topRadius))
                            topRadius = param.exp;

                        continue; //exclude all automatic cone inputs
                    }
                    yield return param;
                }

                var finalTransform = transform;

                var isZeroScaled = VFXOperatorUtility.IsTRSMatrixZeroScaled(finalTransform);
                yield return new VFXNamedExpression(isZeroScaled, "isZeroScaled");

                yield return new VFXNamedExpression(finalTransform, "fieldTransform");
                yield return new VFXNamedExpression(new VFXExpressionInverseTRSMatrix(finalTransform), "invFieldTransform");
                if (radiusMode != RadiusMode.None)
                {
                    var scale = new VFXExpressionExtractScaleFromMatrix(finalTransform);
                    yield return new VFXNamedExpression(VFXOperatorUtility.Reciprocal(scale), "invFieldScale");
                }

                yield return new VFXNamedExpression(baseRadius, "cone_baseRadius");
                yield return new VFXNamedExpression(topRadius, "cone_topRadius");
                yield return new VFXNamedExpression(height, "cone_height");
            }
        }
    }
}
