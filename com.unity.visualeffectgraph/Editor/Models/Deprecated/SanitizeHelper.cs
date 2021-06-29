using System;
using System.Linq;
using UnityEditor.VFX.Block;
using UnityEngine;

namespace UnityEditor.VFX
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

        public static void MigrateBlockTShapeFromShape(VFXBlock to, VFXBlock from)
        {
            var fromSettings = from.GetSettings(true);
            var toSettings = to.GetSettings(true);

            foreach (var fromSetting in fromSettings)
            {
                var toSetting = toSettings.FirstOrDefault(o => o.name.Equals(fromSetting.name, StringComparison.InvariantCultureIgnoreCase));
                if (toSetting.field != null)
                    to.SetSettingValue(fromSetting.name, fromSetting.value);
            }

            foreach (var fromInputSlot in from.inputSlots)
            {
                var toInputSlot = to.inputSlots.FirstOrDefault(o => o.name.Equals(fromInputSlot.name, StringComparison.InvariantCultureIgnoreCase));
                if (toInputSlot != null)
                {
                    if (toInputSlot.property.type == fromInputSlot.property.type)
                    {
                        VFXSlot.CopyLinksAndValue(toInputSlot, fromInputSlot, true);
                    }
                    else if (toInputSlot.property.type == typeof(TArcSphere))
                    {
                        MigrateTArcSphereFromArcSphere(toInputSlot, fromInputSlot);
                    }
                    else if (toInputSlot.property.type == typeof(TArcCircle))
                    {
                        MigrateTArcCircleFromArcCirlce(toInputSlot, fromInputSlot);
                    }
                }
            }
        }

        public static void MigrateTArcSphereFromArcSphere(VFXSlot to, VFXSlot from)
        {
            var to_sphere = to[0];
            var to_arc = to[1];

            var refSlot = from.refSlot;
            var from_sphere = refSlot[0];
            var from_arc = refSlot[1];
            VFXSlot.CopySpace(to, refSlot, true);

            var hasDirectLink = from.HasLink(false);
            MigrateTSphereFromSphere(to_sphere, from_sphere, hasDirectLink);
            if (hasDirectLink)
            {
                to_arc.Link(from_arc, true);
            }
            else
            {
                to_arc.value = (float)from_arc.value; //The value transfer is only applied on masterslot
                VFXSlot.CopyLinksAndValue(to_arc, from_arc, true);
            }
        }

        public static void MigrateTSphereFromSphere(VFXSlot to, VFXSlot from, bool forceHasLink = false)
        {
            var to_center = to[0][0];
            var to_radius = to[1];

            var refSlot = from.refSlot;
            VFXSlot.CopySpace(to, refSlot, true);
            if (from.HasLink(false) || forceHasLink)
            {
                var parentCenter = refSlot[0];
                var parentRadius = refSlot[1];

                to_center.Link(parentCenter, true);
                to_radius.Link(parentRadius, true);
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

        public static void MigrateTArcCircleFromArcCirlce(VFXSlot to, VFXSlot from)
        {
            var to_circle = to[0];
            var to_arc = to[1];

            var refSlot = from.refSlot;
            var from_circle = refSlot[0];
            var from_arc = refSlot[1];
            VFXSlot.CopySpace(to, refSlot, true);

            var hasDirectLink = from.HasLink(false);
            MigrateTCircleFromCircle(to_circle, from_circle, hasDirectLink);
            if (hasDirectLink)
            {
                to_arc.Link(from_arc, true);
            }
            else
            {
                to_arc.value = (float)from_arc.value; //The value transfer is only applied on masterslot
                VFXSlot.CopyLinksAndValue(to_arc, from_arc, true);
            }
        }

        public static void MigrateTCircleFromCircle(VFXSlot to, VFXSlot from, bool forceHasLink = false)
        {
            var to_center = to[0][0];
            var to_radius = to[1];

            var refslot = from.refSlot;
            VFXSlot.CopySpace(to, refslot, true);

            if (from.HasLink(false) || forceHasLink)
            {
                var parentCenter = refslot[0];
                var parentRadius = refslot[1];

                to_center.Link(parentCenter, true);
                to_radius.Link(parentRadius, true);
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

        public static void MigrateTTorusFromTorus(VFXSlot to, VFXSlot from)
        {
            var to_center = to[0][0];
            var to_majorRadius = to[1];
            var to_minorRadius = to[2];
            var refSlot = from.refSlot;
            VFXSlot.CopySpace(to, refSlot, true);

            if (from.HasLink(false))
            {
                var parentCenter = refSlot[0];
                var parentMajorRadius = refSlot[1];
                var parentMinorRadius = refSlot[2];

                to_center.Link(parentCenter, true);
                to_majorRadius.Link(parentMajorRadius, true);
                to_minorRadius.Link(parentMinorRadius, true);
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

            var refSlot = from.refSlot;
            VFXSlot.CopySpace(to, refSlot, true);

            if (from.HasLink(false))
            {
                var parentCenter = refSlot[0];
                var parentRadius = refSlot[1];
                var parentHeight = refSlot[2];

                to_center.Link(parentCenter, true);
                to_baseRadius.Link(parentRadius, true);
                to_topRadius.Link(parentRadius, true);
                to_height.Link(parentHeight, true);
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

            var halfHeight = ScriptableObject.CreateInstance<Operator.Multiply>();
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

            var correctedPosition = ScriptableObject.CreateInstance<Operator.Subtract>();
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

            var refSlot = from.refSlot;
            VFXSlot.CopySpace(to, refSlot, true);

            if (from.HasLink(false))
            {
                var parentCenter = refSlot[0];
                var parentRadius = refSlot[1];
                var parentHeight = refSlot[2];

                var correctedPosition = CorrectPositionFromCylinderToCone(graph, basePosition, parentHeight, parentCenter);
                correctedPosition.Link(to_center);

                to_baseRadius.Link(parentRadius, true);
                to_topRadius.Link(parentRadius, true);
                to_height.Link(parentHeight, true);
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
