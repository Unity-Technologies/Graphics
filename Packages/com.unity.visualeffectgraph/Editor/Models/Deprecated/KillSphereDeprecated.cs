using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class KillSphereDeprecated : VFXBlock
    {
        [VFXSetting]
        [Tooltip("Specifies the mode by which particles are killed off. ‘Solid’ affects only particles within the specified volume, while ‘Inverted’ affects only particles outside of the volume.")]
        public CollisionBase.Mode mode = CollisionBase.Mode.Solid;

        public override string name { get { return "Kill (Sphere) (deprecated)"; } }

        public override VFXContextType compatibleContexts { get { return VFXContextType.InitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Write);
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this))
                    yield return p;

                if (mode == CollisionBase.Mode.Solid)
                    yield return new VFXNamedExpression(VFXValue.Constant(1.0f), "colliderSign");
                else
                    yield return new VFXNamedExpression(VFXValue.Constant(-1.0f), "colliderSign");
            }
        }

        public class InputProperties
        {
            [Tooltip("Sets the center and radius of the sphere used to determine the kill volume.")]
            public Sphere Sphere = new Sphere() { radius = 1.0f };
        }

        public override void Sanitize(int version)
        {
            var newKillSphere = ScriptableObject.CreateInstance<KillSphereDeprecatedV2>();
            SanitizeHelper.MigrateBlockTShapeFromShape(newKillSphere, this);
            var newKillSphereShape = ScriptableObject.CreateInstance<CollisionShape>();
            newKillSphereShape.SetSettingValue("behavior", CollisionBase.Behavior.Kill);
            SanitizeHelper.MigrateBlockCollisionShapeToComposed(newKillSphereShape, newKillSphere, CollisionShapeBase.Type.Sphere);
            ReplaceModel(newKillSphereShape, this);
        }

        public override string source
        {
            get
            {
                return @"
float3 dir = position - Sphere_center;
float sqrLength = dot(dir, dir);
if (colliderSign * sqrLength <= colliderSign * Sphere_radius * Sphere_radius)
    alive = false;";
            }
        }
    }
}
