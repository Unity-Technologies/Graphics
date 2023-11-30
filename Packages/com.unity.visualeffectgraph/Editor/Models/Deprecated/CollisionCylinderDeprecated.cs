using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class CollisionCylinderDeprecated : CollisionBase
    {
        public override string name { get { return "Collide with Cylinder (deprecated)"; } }

        public class InputProperties
        {
            [Tooltip("Sets the cylinder with which particles can collide.")]
            public Cylinder Cylinder = new Cylinder() { height = 1.0f, radius = 0.5f };
        }

        private string collisionTestSource
        {
            get
            {
                if (mode == Mode.Solid)
                    return @"
bool collision = abs(dir.y) < halfHeight && sqrLength < cylinderRadius * cylinderRadius;
";
                else
                    return @"
bool collision = abs(dir.y) > halfHeight || sqrLength > cylinderRadius * cylinderRadius;
";
            }
        }

        private string normalAndPushSource
        {
            get
            {
                if (mode == Mode.Solid)
                    return @"
    n *= distToSide < distToCap ? float3(1,0,1) : float3(0,1,0);
    position += n * min(distToSide,distToCap);
";
                else
                    return @"
    position += n * float3(max(0,distToSide).xx,max(0,distToCap)).xzy;
    n *= distToSide > distToCap ? float3(1,0,1) : float3(0,1,0);
";
            }
        }

        public override void Sanitize(int version)
        {
            var newCollisionCone = ScriptableObject.CreateInstance<CollisionConeDeprecatedV2>();
            SanitizeHelper.MigrateBlockTShapeFromShape(newCollisionCone, this);
            var newCollisionConeShape = ScriptableObject.CreateInstance<CollisionShape>();
            SanitizeHelper.MigrateBlockCollisionShapeToComposed(newCollisionConeShape, newCollisionCone, CollisionShapeBase.Type.Cone);
            ReplaceModel(newCollisionConeShape, this);
        }
    }
}
