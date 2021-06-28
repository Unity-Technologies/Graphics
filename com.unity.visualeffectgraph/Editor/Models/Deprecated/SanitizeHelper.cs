using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    static class SanitizeHelper
    {
        public static readonly bool s_Enable_Sanitize_of_TShape = true; //TODOPAUL : Remove this

        public static void MigrateVector3OutputToSpaceableKeepingLegacyBehavior(VFXOperator op, string newTypeInfo)
        {
            Debug.LogFormat("Sanitizing Graph: Automatically replace Vector3 to {0} for {1}. An inline Vector3 operator has been added.", newTypeInfo, op.name);

            var inlineVector3 = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inlineVector3.SetSettingValue("m_Type", (SerializableType)typeof(Vector3));
            inlineVector3.position = op.position + new Vector2(128.0f, 64.0f);
            VFXSlot.CopyLinksAndValue(inlineVector3.outputSlots[0], op.outputSlots[0], false /* we should avoid ReSyncSlot at this stage*/);
            op.outputSlots[0].Link(inlineVector3.inputSlots[0], true /* notify here to correctly invalidate */);
            op.GetParent().AddChild(inlineVector3);
        }

        public static void MigrateTSphereFromSphere(VFXSlot to, VFXSlot from)
        {
            var to_center = to[0][0];
            var to_radius = to[1];

            if (from.HasLink(false))
            {
                var parent = from.refSlot;
                var parentCenter = parent[0];
                var parentRadius = parent[1];

                to_center.Link(parentCenter, true);
                to_radius.Link(parentRadius, true);
                VFXSlot.CopySpace(to, parent, true);
            }
            else
            {
                var center = from[0];
                var radius = from[1];

                var value = new TSphere()
                {
                    transform = new Transform()
                    {
                        position = (Vector3)center.value,
                        scale = Vector3.one
                    },
                    radius = (float)radius.value
                };

                to.value = value;
                VFXSlot.CopyLinksAndValue(to_center, center, true);
                VFXSlot.CopyLinksAndValue(to_radius, radius, true);
            }
        }
    }
}
