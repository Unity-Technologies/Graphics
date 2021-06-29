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

        public static void MigrateTCircleFromCircle(VFXSlot to, VFXSlot from)
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

                var value = new TCircle()
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

        public static void MigrateTTorusFromTorus(VFXSlot to, VFXSlot from)
        {
            var to_center = to[0][0];
            var to_majorRadius = to[1];
            var to_minorRadius = to[2];

            if (from.HasLink(false))
            {
                var parent = from.refSlot;
                var parentCenter = parent[0];
                var parentMajorRadius = parent[1];
                var parentMinorRadius = parent[2];

                to_center.Link(parentCenter, true);
                to_majorRadius.Link(parentMajorRadius, true);
                to_minorRadius.Link(parentMinorRadius, true);
                VFXSlot.CopySpace(to, parent, true);
            }
            else
            {
                var center = from[0];
                var majorRadius = from[1];
                var minorRadius = from[2];

                var value = new TTorus()
                {
                    transform = new Transform()
                    {
                        position = (Vector3)center.value,
                        scale = Vector3.one
                    },

                    majorRadius = (float)majorRadius.value,
                    minorRadius = (float)minorRadius.value
                };

                to.value = value;
                VFXSlot.CopyLinksAndValue(to_center, center, true);
                VFXSlot.CopyLinksAndValue(to_majorRadius, majorRadius, true);
                VFXSlot.CopyLinksAndValue(to_minorRadius, minorRadius, true);
            }
        }

        public static void MigrateTConeFromCone(VFXSlot to, VFXSlot from)
        {
            var to_center = to[0][0];
            var to_baseRadius = to[1];
            var to_topRadius = to[2];
            var to_height = to[3];

            if (from.HasLink(false))
            {
                var parent = from.refSlot;
                var parentCenter = parent[0];
                var parentRadius = parent[1];
                var parentHeight = parent[2];

                to_center.Link(parentCenter, true);
                to_baseRadius.Link(parentRadius, true);
                to_topRadius.Link(parentRadius, true);
                to_height.Link(parentHeight, true);
                VFXSlot.CopySpace(to, parent, true);
            }
            else
            {
                var center = from[0];
                var baseRadius = from[1];
                var topRadius = from[2];
                var height = from[3];

                var value = new TCone()
                {
                    transform = new Transform()
                    {
                        position = (Vector3)center.value,
                        scale = Vector3.one
                    },

                    baseRadius = (float)baseRadius.value,
                    topRadius = (float)topRadius.value,
                    height = (float)height.value
                };

                to.value = value;
                VFXSlot.CopyLinksAndValue(to_center, center, true);
                VFXSlot.CopyLinksAndValue(to_baseRadius, baseRadius, true);
                VFXSlot.CopyLinksAndValue(to_topRadius, topRadius, true);
                VFXSlot.CopyLinksAndValue(to_height, height, true);
            }
        }

        private static VFXSlot CorrectPositionFromCylinderToCone(VFXGraph graph, Vector2 basePosition, VFXSlot height, VFXSlot center)
        {
            var inlineHeight = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inlineHeight.SetSettingValue("m_Type", (SerializableType)typeof(float));
            inlineHeight.position = basePosition - new Vector2(128.0f, 128.0f);
            VFXSlot.CopyLinksAndValue(inlineHeight.inputSlots[0], height, true);
            graph.AddChild(inlineHeight);

            var halfHeight = ScriptableObject.CreateInstance<Multiply>();
            halfHeight.SetOperandType(0, typeof(float));
            halfHeight.SetOperandType(1, typeof(float));
            halfHeight.inputSlots[0].Link(inlineHeight.outputSlots[0]);
            halfHeight.inputSlots[1].value = 0.5f;
            halfHeight.position = basePosition - new Vector2(128.0f, 96.0f);
            graph.AddChild(halfHeight);

            var inlinePosition = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inlinePosition.SetSettingValue("m_Type", (SerializableType)typeof(Position));
            inlinePosition.position = basePosition - new Vector2(96.0f, 128.0f);
            VFXSlot.CopyLinksAndValue(inlinePosition.inputSlots[0], center, true);
            graph.AddChild(inlinePosition);

            var correctedPosition = ScriptableObject.CreateInstance<Subtract>();
            correctedPosition.SetOperandType(0, typeof(Position));
            correctedPosition.SetOperandType(1, typeof(Position));
            VFXSlot.CopySpace(correctedPosition.inputSlots[0], inlinePosition.outputSlots[0], true);
            VFXSlot.CopySpace(correctedPosition.inputSlots[1], inlinePosition.outputSlots[0], true);
            correctedPosition.inputSlots[0].Link(inlinePosition.outputSlots[0]);
            correctedPosition.inputSlots[1][0][1].Link(halfHeight.outputSlots[0]);
            correctedPosition.position = basePosition - new Vector2(96.0f, 96.0f);
            graph.AddChild(correctedPosition);

            return correctedPosition.outputSlots[0];
        }

        public static void MigrateTConeFromCylinder(VFXSlot to, VFXSlot from)
        {
            var basePosition = (from.owner as VFXModel).position;
            var graph = (from.owner as VFXModel).GetParent() as VFXGraph;

            var to_center = to[0][0];
            var to_baseRadius = to[1];
            var to_topRadius = to[2];
            var to_height = to[3];

            if (from.HasLink(false))
            {
                var parent = from.refSlot;
                var parentCenter = parent[0];
                var parentRadius = parent[1];
                var parentHeight = parent[2];

                var correctedPosition = CorrectPositionFromCylinderToCone(graph, basePosition, parentHeight, parentCenter);
                correctedPosition.Link(to_center);

                to_baseRadius.Link(parentRadius, true);
                to_topRadius.Link(parentRadius, true);
                to_height.Link(parentHeight, true);
                VFXSlot.CopySpace(to, parent, true);
            }
            else
            {
                var center = from[0];
                var radius = from[1];
                var height = from[2];

                var value = new TCone()
                {
                    transform = new Transform()
                    {
                        position = (Vector3)center.value - new Vector3(0, (float)height.value * 0.5f, 0),
                        scale = Vector3.one
                    },
                    height = (float)height.value,
                    baseRadius = (float)radius.value,
                    topRadius = (float)radius.value,
                };
                to.value = value;

                if (from.HasLink(true))
                {
                    var correctedPosition = CorrectPositionFromCylinderToCone(graph, basePosition, height, center);
                    correctedPosition.Link(to_center);
                }

                VFXSlot.CopyLinksAndValue(to_baseRadius, radius, true);
                VFXSlot.CopyLinksAndValue(to_topRadius, radius, true);
                VFXSlot.CopyLinksAndValue(to_height, height, true);
            }
        }
    }
}
