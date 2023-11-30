using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class CollisionAABoxDeprecatedV2 : CollisionBase
    {
        public override string name { get { return "Collide with AABox"; } }

        public class InputProperties
        {
            [Tooltip("Sets the bounding box with which particles can collide.")]
            public AABox box = new AABox() { size = Vector3.one };
        }

        public override void Sanitize(int version)
        {
            var newCollisionShape = ScriptableObject.CreateInstance<CollisionShape>();
            SanitizeHelper.MigrateBlockCollisionShapeToComposed(newCollisionShape, this, CollisionShapeBase.Type.OrientedBox);
            ReplaceModel(newCollisionShape, this);
        }
    }
}
