using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class CollisionSphereDeprecatedV2 : CollisionBase
    {
        public override string name { get { return "Collide with Sphere"; } }

        public class InputProperties
        {
            [Tooltip("Sets the sphere with which particles can collide.")]
            public TSphere sphere = TSphere.defaultValue;
        }

        public override void Sanitize(int version)
        {
            var newCollisionSphere = ScriptableObject.CreateInstance<CollisionShape>();
            SanitizeHelper.MigrateBlockCollisionShapeToComposed(newCollisionSphere, this, CollisionShapeBase.Type.Sphere);
            ReplaceModel(newCollisionSphere, this);
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                VFXExpression transform = null;
                VFXExpression radius = null;

                foreach (var param in base.parameters)
                {
                    if (param.name.StartsWith("sphere"))
                    {
                        if (param.name == "sphere_transform")
                            transform = param.exp;
                        if (param.name == "sphere_radius")
                            radius = param.exp;

                        continue; //exclude all automatic sphere inputs
                    }
                    yield return param;
                }

                VFXExpression finalTransform;

                //Integrate directly the radius into the common transform matrix
                var radiusScale = VFXOperatorUtility.UniformScaleMatrix(radius);
                finalTransform = new VFXExpressionTransformMatrix(transform, radiusScale);

                var isZeroScaled = VFXOperatorUtility.IsTRSMatrixZeroScaled(finalTransform);
                yield return new VFXNamedExpression(isZeroScaled, "isZeroScaled");

                yield return new VFXNamedExpression(finalTransform, "fieldTransform");
                yield return new VFXNamedExpression(new VFXExpressionInverseTRSMatrix(finalTransform), "invFieldTransform");
                if (radiusMode != RadiusMode.None)
                {
                    var scale = new VFXExpressionExtractScaleFromMatrix(finalTransform);
                    yield return new VFXNamedExpression(VFXOperatorUtility.Reciprocal(scale), "invFieldScale");
                }
            }
        }
    }
}
