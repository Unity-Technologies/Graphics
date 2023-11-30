using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class KillSphereDeprecatedV2 : VFXBlock
    {
        [VFXSetting]
        [Tooltip("Specifies the mode by which particles are killed off. ‘Solid’ affects only particles within the specified volume, while ‘Inverted’ affects only particles outside of the volume.")]
        public CollisionBase.Mode mode = CollisionBase.Mode.Solid;

        public override string name { get { return "Kill (Sphere)"; } }

        public override VFXContextType compatibleContexts { get { return VFXContextType.InitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        public override void Sanitize(int version)
        {
            var newKillSphereShape = ScriptableObject.CreateInstance<CollisionShape>();
            newKillSphereShape.SetSettingValue("behavior", CollisionBase.Behavior.Kill);
            SanitizeHelper.MigrateBlockCollisionShapeToComposed(newKillSphereShape, this, CollisionShapeBase.Type.Sphere);
            ReplaceModel(newKillSphereShape, this);
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Write);
            }
        }

        public class InputProperties
        {
            [Tooltip("Sets the sphere used to determine the kill volume.")]
            public TSphere sphere = TSphere.defaultValue;
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                VFXExpression transform = null;
                VFXExpression radius = null;
                foreach (var param in GetExpressionsFromSlots(this))
                {
                    if (param.name.StartsWith("sphere"))
                    {
                        if (param.name == "sphere_" + nameof(TSphere.transform))
                            transform = param.exp;
                        if (param.name == "sphere_" + nameof(TSphere.radius))
                            radius = param.exp;

                        continue; //exclude all sphere automatic inputs
                    }
                    yield return param;
                }

                //Integrate directly the radius into the common transform matrix
                var radiusScale = VFXOperatorUtility.UniformScaleMatrix(radius);
                var finalTransform = new VFXExpressionTransformMatrix(transform, radiusScale);
                yield return new VFXNamedExpression(new VFXExpressionInverseTRSMatrix(finalTransform), "invFieldTransform");

                var isZeroScaled = VFXOperatorUtility.IsTRSMatrixZeroScaled(finalTransform);
                yield return new VFXNamedExpression(isZeroScaled, "isZeroScaled");

                if (mode == CollisionBase.Mode.Solid)
                    yield return new VFXNamedExpression(VFXValue.Constant(1.0f), "colliderSign");
                else
                    yield return new VFXNamedExpression(VFXValue.Constant(-1.0f), "colliderSign");
            }
        }

        public override string source
        {
            get
            {
                return @"
if (isZeroScaled)
    return;

float3 tPos = mul(invFieldTransform, float4(position, 1.0f)).xyz;
float sqrLength = dot(tPos, tPos);
if (colliderSign * sqrLength <= colliderSign)
    alive = false;";
            }
        }
    }
}
