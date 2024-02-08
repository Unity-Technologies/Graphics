using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.Block;
using UnityEngine;

namespace UnityEditor.VFX
{
    static class SanitizeHelper
    {
        public static void MigrateSGOutputToComposed(VFXShaderGraphParticleOutput shaderGraphOutput)
        {
            var shaderGraph = shaderGraphOutput.GetOrRefreshShaderGraphObject(false);
            if (shaderGraph == null)
                return;

            if (!shaderGraph.generatesWithShaderGraph)
                return;

            Type topologyType = null;
            Type composedType = null;
            if (shaderGraphOutput.HasStrips())
            {
                topologyType = typeof(ParticleTopologyQuadStrip);
                composedType = typeof(VFXComposedParticleStripOutput);
            }
            else
            {
                switch (shaderGraphOutput.taskType)
                {
                    case VFXTaskType.ParticleMeshOutput:
                        topologyType = typeof(ParticleTopologyMesh);
                        break;
                    case VFXTaskType.ParticleQuadOutput:
                    case VFXTaskType.ParticleTriangleOutput:
                    case VFXTaskType.ParticleOctagonOutput:
                        topologyType = typeof(ParticleTopologyPlanarPrimitive);
                        break;
                }

                composedType = typeof(VFXComposedParticleOutput);
            }

            if (topologyType == null)
            {
                Debug.LogError("Unexpected output primitive: " + shaderGraphOutput);
                return;
            }

            var composed = (VFXAbstractComposedParticleOutput)ScriptableObject.CreateInstance(composedType);
            composed.SetSettingValue("m_Topology", Activator.CreateInstance(topologyType));
            composed.label = shaderGraphOutput.label;

            //Transfer blocks
            var sourceBlocks = new List<VFXBlock>(shaderGraphOutput.children);
            foreach (var block in sourceBlocks)
                composed.AddChild(block, -1, false);

            //Transfer settings (it should include materialSettings)
            var sourceSettings = new List<KeyValuePair<string, object>>();
            var destSettings = new List<VFXSetting>(composed.GetSettings(true, VFXSettingAttribute.VisibleFlags.Default));
            foreach (var setting in destSettings)
            {
                var sourceSetting = shaderGraphOutput.GetSetting(setting.name);
                if (!sourceSetting.valid)
                    continue;

                if (VFXConverter.TryConvertTo(sourceSetting.value, setting.field.FieldType, out var value))
                    sourceSettings.Add(new KeyValuePair<string, object>(setting.field.Name, value));
            }
            composed.SetSettingValues(sourceSettings);

            //Transfer slots
            foreach (var slot in composed.inputSlots)
            {
                var refSlot = shaderGraphOutput.inputSlots.FirstOrDefault(o => o.name == slot.name);
                VFXSlot.CopyLinksAndValue(slot, refSlot, false);
            }

            //Transfer flow edges
            foreach (var link in shaderGraphOutput.inputFlowSlot[0].link)
            {
                composed.LinkFrom(link.context, link.slotIndex);
            }

            //Unlink previous flow before replacing model to avoid being kept in data owners
            shaderGraphOutput.UnlinkAll();

            VFXModel.ReplaceModel(composed, shaderGraphOutput);
        }

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

        public static void MigrateBlockPositionToComposed(VFXGraph graph, Vector2 position, PositionShape to, VFXBlock from, PositionShapeBase.Type shapeType)
        {
            var fromSettings = from.GetSettings(true);
            var toSettings = to.GetSettings(true);

            foreach (var fromSetting in fromSettings)
            {
                var toSetting = toSettings.FirstOrDefault(o =>
                    o.name.Equals(fromSetting.name, StringComparison.InvariantCultureIgnoreCase));

                if (toSetting.field == null)
                    throw new InvalidOperationException("Unexpected migration, can't find approriate settings: " + fromSetting.name);

                var fromValue = fromSetting.value;
                to.SetSettingValue(toSetting.name, fromValue);
            }

            to.SetSettingValue("shape", shapeType);
            VFXSlot.CopyLinksAndValue(to.activationSlot, from.activationSlot);

            if (from.inputSlots.Count == to.inputSlots.Count)
            {
                // Special case for AABox as it is migrated to OBox with no rotation
                int slotStartOffset = 0;
                if (from.inputSlots[0].property.type == typeof(AABox) && shapeType == PositionShapeBase.Type.OrientedBox)
                {
                    CopyLinksAndValueFromAABoxToOBox(to.inputSlots[0], from.inputSlots[0]);
                    slotStartOffset = 1;
                }

                for (int i = slotStartOffset; i < from.inputSlots.Count; ++i)
                {
                    var fromInputSlot = from.inputSlots[i];
                    var toInputSlot = to.inputSlots.FirstOrDefault(o => o.name == fromInputSlot.name);
                    VFXSlot.CopyLinksAndValue(toInputSlot, fromInputSlot, true);
                }
            }
            else // Add height sequencer
            {
                if (shapeType != PositionShapeBase.Type.Torus &&
                    shapeType != PositionShapeBase.Type.Sphere &&
                    shapeType != PositionShapeBase.Type.Cone)
                    throw new InvalidOperationException("Unexpected migration to " + shapeType);

                if (to.spawnMode != PositionBase.SpawnMode.Custom)
                    throw new InvalidOperationException("Unexpected migration using spawn mode " + to.spawnMode);

                //Copy Matching node by name
                foreach (var fromInputSlot in from.inputSlots)
                {
                    var toInputSlot = to.inputSlots.FirstOrDefault(o => o.name == fromInputSlot.name);
                    if (toInputSlot == null)
                        throw new InvalidOperationException("Unexpected migration, can't find slot named " + fromInputSlot.name);

                    VFXSlot.CopyLinksAndValue(toInputSlot, fromInputSlot, true);
                }

                var heightSlot = to.inputSlots.FirstOrDefault(o => o.name == "heightSequencer");
                if (heightSlot == null)
                    throw new NullReferenceException();

                var randomNode = ScriptableObject.CreateInstance<Operator.Random>();
                randomNode.SetSettingValue("constant", false);
                if (randomNode.inputSlots.Count != 2)
                    throw new InvalidOperationException("Unexpected migration, can't setup property random operator");
                graph.AddChild(randomNode);
                randomNode.position = position - new Vector2(120, 0);

                if (!heightSlot.Link(randomNode.outputSlots[0]))
                    throw new InvalidOperationException("Unexpected migration, can't setup property random operator");
            }

            //Extra clean up, some blocks are migrated twice, avoid previous migration keeping unwanted references in m_SlotOwners
            VFXModel.UnlinkModel(from);
        }

        public static void MigrateBlockCollisionShapeToComposed(CollisionShape to, VFXBlock from, CollisionShapeBase.Type shapeType)
        {
            var fromSettings = from.GetSettings(true);
            var toSettings = to.GetSettings(true);

            foreach (var fromSetting in fromSettings)
            {
                var toSetting = toSettings.FirstOrDefault(o =>
                    o.name.Equals(fromSetting.name, StringComparison.InvariantCultureIgnoreCase));
                var fromValue = fromSetting.value;
                to.SetSettingValue(toSetting.name, fromValue);
            }

            to.SetSettingValue("shape", shapeType);

            // Special case for AABox as it is migrated to OBox with no rotation
            int slotStartOffset = 0;
            if (from.inputSlots[0].property.type == typeof(AABox) && shapeType == CollisionShapeBase.Type.OrientedBox)
            {
                CopyLinksAndValueFromAABoxToOBox(to.inputSlots[0], from.inputSlots[0]);
                slotStartOffset = 1;
            }

            if (from.inputSlots.Count != to.inputSlots.Count)
                throw new InvalidOperationException();

            VFXSlot.CopyLinksAndValue(to.activationSlot, from.activationSlot);
            for (int i = slotStartOffset; i < from.inputSlots.Count; ++i)
            {
                var fromInputSlot = from.inputSlots[i];
                var toInputSlot = to.inputSlots.FirstOrDefault(o => o.name == fromInputSlot.name);
                VFXSlot.CopyLinksAndValue(toInputSlot, fromInputSlot, true);
            }
            
            // Override bounce speed limit to 0 for sanitized block to avoid changes in behavior
            if (to.behavior == CollisionBase.Behavior.Collision)
            {
                to.SetSettingValue(nameof(CollisionBase.overrideBounceThreshold), true);
                var slot = to.inputSlots.First(s => s.name == nameof(CollisionBase.CollisionProperties.BounceSpeedThreshold));
                slot.value = 0.0f;
            }

            //Extra clean up, some blocks are migrated twice, avoid previous migration keeping unwanted references in m_SlotOwners
            VFXModel.UnlinkModel(from);
        }

        public static void MigrateBlockTShapeFromShape(VFXBlock to, VFXBlock from)
        {
            var fromSettings = from.GetSettings(true);
            var toSettings = to.GetSettings(true);

            foreach (var fromSetting in fromSettings)
            {
                var toSetting = toSettings.FirstOrDefault(o => o.name.Equals(fromSetting.name, StringComparison.InvariantCultureIgnoreCase));
                if (toSetting.field != null)
                    to.SetSettingValue(toSetting.name, fromSetting.value);
            }

            if (from.inputSlots.Count != to.inputSlots.Count)
                throw new InvalidOperationException();

            for (int i = 0; i < from.inputSlots.Count; ++i)
            {
                var fromInputSlot = from.inputSlots[i];
                var toInputSlot = to.inputSlots[i];

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
                    MigrateTArcCircleFromArcCircle(toInputSlot, fromInputSlot);
                }
                else if (toInputSlot.property.type == typeof(TArcTorus))
                {
                    MigrateTArcTorusFromArcTorus(toInputSlot, fromInputSlot);
                }
                else if (toInputSlot.property.type == typeof(TArcCone))
                {
                    //There wasn't a TArcCylinder type
                    MigrateTArcConeFromArcCone(toInputSlot, fromInputSlot);
                }
                else if (toInputSlot.property.type == typeof(TSphere))
                {
                    MigrateTSphereFromSphere(toInputSlot, fromInputSlot);
                }
                else if (toInputSlot.property.type == typeof(TCircle))
                {
                    MigrateTCircleFromCircle(toInputSlot, fromInputSlot);
                }
                else if (toInputSlot.property.type == typeof(TTorus))
                {
                    MigrateTTorusFromTorus(toInputSlot, fromInputSlot);
                }
                else if (toInputSlot.property.type == typeof(TCone) && fromInputSlot.property.type == typeof(Cone))
                {
                    //Actually, no reference of this case
                    MigrateTConeFromCone(toInputSlot, fromInputSlot);
                }
                else if (toInputSlot.property.type == typeof(TCone) && fromInputSlot.property.type == typeof(Cylinder))
                {
                    MigrateTConeFromCylinder(toInputSlot, fromInputSlot);
                }
                else
                {
                    throw new NotImplementedException();
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
                //TODO : This behavior leads to an UX issue with if the parent owner of refslot is VFXParameter
                //This is the same issue than OnCopyLinksMySlot/OnCopyLinksOtherSlot needed in VFXSlot.CopyLinks
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

        public static void MigrateTArcCircleFromArcCircle(VFXSlot to, VFXSlot from)
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

        public static void MigrateTArcTorusFromArcTorus(VFXSlot to, VFXSlot from)
        {
            var to_torus = to[0];
            var to_arc = to[1];

            var refSlot = from.refSlot;
            var from_torus = refSlot; //The torus wasn't a composition
            var from_arc = refSlot[3];
            VFXSlot.CopySpace(to, refSlot, true);

            var hasDirectLink = from.HasLink(false);
            MigrateTTorusFromTorus(to_torus, from_torus, hasDirectLink);
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

        public static void MigrateTTorusFromTorus(VFXSlot to, VFXSlot from, bool hasLink = false)
        {
            var to_center = to[0][0];
            var to_majorRadius = to[1];
            var to_minorRadius = to[2];
            var refSlot = from.refSlot;
            VFXSlot.CopySpace(to, refSlot, true);

            if (from.HasLink(false) || hasLink)
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

        public static void MigrateTArcConeFromArcCone(VFXSlot to, VFXSlot from)
        {
            var to_cone = to[0];
            var to_arc = to[1];

            var refSlot = from.refSlot;
            var from_cone = refSlot; //The ArcCone wasn't a composition
            var from_arc = refSlot[4];
            VFXSlot.CopySpace(to, refSlot, true);

            var hasDirectLink = from.HasLink(false);
            MigrateTConeFromCone(to_cone, from_cone, hasDirectLink);
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

        public static void MigrateTConeFromCone(VFXSlot to, VFXSlot from, bool hasLink = false)
        {
            var to_center = to[0][0];
            var to_baseRadius = to[1];
            var to_topRadius = to[2];
            var to_height = to[3];

            var refSlot = from.refSlot;
            VFXSlot.CopySpace(to, refSlot, true);

            if (from.HasLink(false) || hasLink)
            {
                var parentCenter = refSlot[0];
                var parentBaseRadius = refSlot[1];
                var parentTopRadius = refSlot[2];
                var parentHeight = refSlot[3];

                to_center.Link(parentCenter, true);
                to_baseRadius.Link(parentBaseRadius, true);
                to_topRadius.Link(parentTopRadius, true);
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
            inlineHeight.position = basePosition - new Vector2(700.0f, -151.0f);
            VFXSlot.CopyLinksAndValue(inlineHeight.inputSlots[0], height, true);
            graph.AddChild(inlineHeight);

            var halfHeight = ScriptableObject.CreateInstance<Operator.Multiply>();
            halfHeight.SetOperandType(0, typeof(float));
            halfHeight.SetOperandType(1, typeof(float));
            halfHeight.inputSlots[0].Link(inlineHeight.outputSlots[0]);
            halfHeight.inputSlots[1].value = 0.5f;
            halfHeight.position = basePosition - new Vector2(480.0f, -111.0f);
            graph.AddChild(halfHeight);

            var inlinePosition = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inlinePosition.SetSettingValue("m_Type", (SerializableType)typeof(Position));
            inlinePosition.position = basePosition - new Vector2(555.0f, -20.0f);
            VFXSlot.CopyLinksAndValue(inlinePosition.inputSlots[0], center, true);
            graph.AddChild(inlinePosition);

            var correctedPosition = ScriptableObject.CreateInstance<Operator.Subtract>();
            correctedPosition.SetOperandType(0, typeof(Position));
            correctedPosition.SetOperandType(1, typeof(Position));
            VFXSlot.CopySpace(correctedPosition.inputSlots[0], inlinePosition.outputSlots[0], true);
            VFXSlot.CopySpace(correctedPosition.inputSlots[1], inlinePosition.outputSlots[0], true);
            correctedPosition.inputSlots[0].Link(inlinePosition.outputSlots[0]);
            correctedPosition.inputSlots[1][0][1].Link(halfHeight.outputSlots[0]);
            correctedPosition.position = basePosition - new Vector2(282.0f, -20.0f);
            graph.AddChild(correctedPosition);

            return correctedPosition.outputSlots[0];
        }

        public static void MigrateTConeFromCylinder(VFXSlot to, VFXSlot from)
        {
            var lastModel = from.owner as VFXModel;
            while (!(lastModel.GetParent() is VFXGraph))
            {
                lastModel = lastModel.GetParent();
            }
            var basePosition = lastModel.position;
            var graph = lastModel.GetParent() as VFXGraph;

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

        [Flags]
        enum SampleMesh_VertexAttributeFlag_Before_Version_9
        {
            None = 0,
            Position = 1 << 0,
            Normal = 1 << 1,
            Tangent = 1 << 2,
            Color = 1 << 3,
            TexCoord0 = 1 << 4,
            TexCoord1 = 1 << 5,
            TexCoord2 = 1 << 6,
            TexCoord3 = 1 << 7,
            TexCoord4 = 1 << 8,
            TexCoord5 = 1 << 9,
            TexCoord6 = 1 << 10,
            TexCoord7 = 1 << 11,
            BlendWeight = 1 << 12,
            BlendIndices = 1 << 13,
        }

        public static void MigrateSampleMeshFrom9To10(Operator.SampleMesh op)
        {
            Debug.Log("Sanitize Graph: Sample Mesh & Skinned Mesh");

            //Starting copying everything to a new instance
            var newSampleMesh = ScriptableObject.CreateInstance<Operator.SampleMesh>();
            var settings = op.GetSettings(true);
            newSampleMesh.SetSettingValues(settings.Select(o => new KeyValuePair<string, object>(o.name, o.value)));

            //Migrate newly added settings, SkinnedTransform.None to keep the previous behavior
            newSampleMesh.SetSettingValue(nameof(Operator.SampleMesh.skinnedTransform), Operator.SampleMesh.SkinnedRootTransform.None);

            var vertexOutputName = "output";

            //Start with empty output, will migrate output slot one by one
            var currentFlag = Operator.SampleMesh.VertexAttributeFlag.None;
            newSampleMesh.SetSettingValue(vertexOutputName, currentFlag);

            //Skip newly added transform slot
            var toInputSlots = newSampleMesh.inputSlots.Take(newSampleMesh.inputSlots.Count - 1).ToList();
            var fromInputSlots = op.inputSlots;

            if (fromInputSlots.Count != toInputSlots.Count)
                throw new InvalidOperationException();

            for (int i = 0; i < fromInputSlots.Count; ++i)
            {
                var fromInputSlot = fromInputSlots[i];
                var toInputSlot = toInputSlots[i];
                VFXSlot.CopyLinksAndValue(toInputSlot, fromInputSlot, true);
            }

            var previousFlag = (SampleMesh_VertexAttributeFlag_Before_Version_9)op.GetSettingValue(vertexOutputName);
            var oldVertexAttributes = Enum.GetValues(typeof(SampleMesh_VertexAttributeFlag_Before_Version_9))
                .Cast<SampleMesh_VertexAttributeFlag_Before_Version_9>()
                .Where(o => previousFlag.HasFlag(o) && o != SampleMesh_VertexAttributeFlag_Before_Version_9.None)
                .ToArray();

            foreach (var oldVertexAttribute in oldVertexAttributes)
            {
                if (oldVertexAttribute == SampleMesh_VertexAttributeFlag_Before_Version_9.Tangent)
                {
                    //Special case: The old tangent as vector4 as been split in Vector & independent float
                    currentFlag = currentFlag | Operator.SampleMesh.VertexAttributeFlag.Tangent | Operator.SampleMesh.VertexAttributeFlag.BitangentSign;
                }
                else
                {
                    var name = Enum.GetName(typeof(SampleMesh_VertexAttributeFlag_Before_Version_9), oldVertexAttribute);
                    if (!Enum.TryParse<Operator.SampleMesh.VertexAttributeFlag>(name, out var newVertexAttribute))
                        throw new InvalidOperationException();

                    currentFlag = currentFlag | newVertexAttribute;
                }
            }
            newSampleMesh.SetSettingValue(vertexOutputName, currentFlag);

            foreach (var oldVertexAttribute in oldVertexAttributes)
            {
                var name = Enum.GetName(typeof(SampleMesh_VertexAttributeFlag_Before_Version_9), oldVertexAttribute);
                var fromOutputSlot = op.outputSlots.FirstOrDefault(o => o.name == name);
                var toOutputSlot = newSampleMesh.outputSlots.FirstOrDefault(o => o.name == ObjectNames.NicifyVariableName(name));
                if (fromOutputSlot == null || toOutputSlot == null)
                    throw new InvalidOperationException();

                var invalidate = false; //calling invalidate at the end, prevent event propagation
                if (fromOutputSlot.property.type == toOutputSlot.property.type)
                {
                    VFXSlot.CopyLinks(toOutputSlot, fromOutputSlot, invalidate);
                }
                else
                {
                    if (oldVertexAttribute == SampleMesh_VertexAttributeFlag_Before_Version_9.Position || oldVertexAttribute == SampleMesh_VertexAttributeFlag_Before_Version_9.Normal)
                    {
                        //Converting back to not spaceable Vector3 output
                        var newVector3 = ScriptableObject.CreateInstance<VFXInlineOperator>();
                        newVector3.SetSettingValue("m_Type", (SerializableType)typeof(Vector3));
                        toOutputSlot.Link(newVector3.inputSlots[0]);
                        newVector3.position = op.position + new Vector2(100, 0);
                        VFXSlot.CopyLinks(newVector3.outputSlots[0], fromOutputSlot, invalidate);
                        op.GetParent().AddChild(newVector3);
                    }
                    else if (oldVertexAttribute == SampleMesh_VertexAttributeFlag_Before_Version_9.Tangent)
                    {
                        var tangent = toOutputSlot;
                        var bitangentSign = newSampleMesh.outputSlots.FirstOrDefault(o => o.name == ObjectNames.NicifyVariableName(Operator.SampleMesh.VertexAttributeFlag.BitangentSign.ToString()));
                        if (bitangentSign == null)
                            throw new InvalidOperationException();

                        var newVector4 = ScriptableObject.CreateInstance<VFXInlineOperator>();
                        newVector4.SetSettingValue("m_Type", (SerializableType)typeof(Vector4));
                        newVector4.inputSlots[0][0].Link(tangent[0][0]);
                        newVector4.inputSlots[0][1].Link(tangent[0][1]);
                        newVector4.inputSlots[0][2].Link(tangent[0][2]);
                        newVector4.inputSlots[0][3].Link(bitangentSign);
                        newVector4.position = op.position + new Vector2(110, 0);
                        VFXSlot.CopyLinks(newVector4.outputSlots[0], fromOutputSlot, invalidate);
                        op.GetParent().AddChild(newVector4);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            newSampleMesh.Invalidate(VFXModel.InvalidationCause.kConnectionChanged);
            VFXModel.ReplaceModel(newSampleMesh, op);
        }

        public static void CopyLinksAndValueFromAABoxToOBox(VFXSlot to, VFXSlot from)
        {
            if (from.property.type != typeof(AABox) || to.property.type != typeof(OrientedBox))
                throw new ArgumentException("Slots are not of the expected type");

            if (from.direction != VFXSlot.Direction.kInput || to.direction != VFXSlot.Direction.kInput)
                throw new ArgumentException("Slots are not input slots");

            to.UnlinkAll(true);

            // First copy value and space
            var aab = (AABox)from.value;
            var ob = new OrientedBox
            {
                center = aab.center,
                angles = Vector3.zero,
                size = aab.size,
            };
            to.value = ob;
            
            VFXSlot.CopyLinks(to, from, true); // Will work as sub-slots names match
            VFXSlot.CopySpace(to, from, true);
        }
    }
}
