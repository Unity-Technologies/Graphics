using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Kill")]
    class KillAABox : VFXBlock
    {
        [VFXSetting]
        [Tooltip("Specifies the mode by which particles are killed off. ‘Solid’ affects only particles within the specified volume, while ‘Inverted’ affects only particles outside of the volume.")]
        public CollisionBase.Mode mode = CollisionBase.Mode.Solid;

        public override string name { get { return "Kill (AABox)"; } }

        public override VFXContextType compatibleContexts { get { return VFXContextType.InitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        public class InputProperties
        {
            [Tooltip("Sets the center and size of the axis-aligned box used to determine the kill volume.")]
            public AABox box = new AABox() { size = Vector3.one };
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Write);
            }
        }

        public override string source
        {
            get
            {
                string Source = @"
float3 dir = position - box_center;
float3 absDir = abs(dir);
float3 size = box_size * 0.5f;
";

                if (mode == CollisionBase.Mode.Solid)
                    Source += @"bool collision = all(absDir <= size);";
                else
                    Source += @"bool collision = any(absDir >= size);";

                Source += @"
if (collision)
    alive = false;";

                return Source;
            }
        }
    }
}
