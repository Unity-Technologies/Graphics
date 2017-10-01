using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public abstract class AbstractSubGraphNode : AbstractMaterialNode
        , IGeneratesFunction
        , IOnAssetEnabled
        , IMayRequireNormal
        , IMayRequireTangent
        , IMayRequireBitangent
        , IMayRequireMeshUV
        , IMayRequireScreenPosition
        , IMayRequireViewDirection
        , IMayRequirePosition
        , IMayRequireVertexColor
        , IMayRequireTime
    {

        protected virtual AbstractSubGraph subGraph { get; }

        public override bool hasPreview
        {
            get { return subGraph != null; }
        }

        public override PreviewMode previewMode
        {
            get
            {
                if (subGraph == null)
                    return PreviewMode.Preview2D;

                return PreviewMode.Preview3D;
            }
        }

        public virtual INode outputNode { get; } = null;

        public virtual void OnEnable()
        {
            var validNames = new List<int>();
            if (subGraph == null)
            {
                RemoveSlotsNameNotMatching(validNames);
                return;
            }

            var props = subGraph.properties;
            foreach (var prop in props)
            {
                var propType = prop.propertyType;
                SlotValueType slotType;

                switch (propType)
                {
                    case PropertyType.Color:
                        slotType = SlotValueType.Vector4;
                        break;
                    case PropertyType.Texture:
                        slotType = SlotValueType.Texture2D;
                        break;
                    case PropertyType.Float:
                        slotType = SlotValueType.Vector1;
                        break;
                    case PropertyType.Vector2:
                        slotType = SlotValueType.Vector2;
                        break;
                    case PropertyType.Vector3:
                        slotType = SlotValueType.Vector3;
                        break;
                    case PropertyType.Vector4:
                        slotType = SlotValueType.Vector4;
                        break;
                    case PropertyType.Matrix2:
                        slotType = SlotValueType.Matrix2;
                        break;
                    case PropertyType.Matrix3:
                        slotType = SlotValueType.Matrix3;
                        break;
                    case PropertyType.Matrix4:
                        slotType = SlotValueType.Matrix4;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var id = prop.guid.GetHashCode();
                AddSlot(new MaterialSlot(id, prop.displayName, prop.referenceName, SlotType.Input, slotType, prop.defaultValue));
                validNames.Add(id);
            }

            var subGraphOutputNode = outputNode;
            if (outputNode != null)
            {
                foreach (var slot in subGraphOutputNode.GetInputSlots<MaterialSlot>())
                {
                    AddSlot(new MaterialSlot(slot.id, slot.displayName, slot.shaderOutputName, SlotType.Output, slot.valueType, slot.defaultValue));
                    validNames.Add(slot.id);
                }
            }

            RemoveSlotsNameNotMatching(validNames);
        }

        public override void CollectShaderProperties(PropertyCollector visitor, GenerationMode generationMode)
        {
            base.CollectShaderProperties(visitor, generationMode);

            if (subGraph == null)
                return;

            subGraph.CollectShaderProperties(visitor, GenerationMode.ForReals);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            if (subGraph == null)
                return;

            properties.AddRange(subGraph.GetPreviewProperties());
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (subGraph == null)
                return;

            subGraph.GenerateNodeFunction(visitor, GenerationMode.ForReals);
        }

        public NeededCoordinateSpace RequiresNormal()
        {
            if (subGraph == null)
                return NeededCoordinateSpace.None;

            return subGraph.activeNodes.OfType<IMayRequireNormal>().Aggregate(NeededCoordinateSpace.None, (mask, node) =>
            {
                mask |= node.RequiresNormal();
                return mask;
            });
        }

        public bool RequiresMeshUV(UVChannel channel)
        {
            if (subGraph == null)
                return false;

            return subGraph.activeNodes.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV(channel));
        }

        public bool RequiresScreenPosition()
        {
            if (subGraph == null)
                return false;

            return subGraph.activeNodes.OfType<IMayRequireScreenPosition>().Any(x => x.RequiresScreenPosition());
        }

        public NeededCoordinateSpace RequiresViewDirection()
        {
            if (subGraph == null)
                return NeededCoordinateSpace.None;

            return subGraph.activeNodes.OfType<IMayRequireViewDirection>().Aggregate(NeededCoordinateSpace.None, (mask, node) =>
            {
                mask |= node.RequiresViewDirection();
                return mask;
            });
        }


        public NeededCoordinateSpace RequiresPosition()
        {
            if (subGraph == null)
                return NeededCoordinateSpace.None;

            return subGraph.activeNodes.OfType<IMayRequirePosition>().Aggregate(NeededCoordinateSpace.None, (mask, node) =>
            {
                mask |= node.RequiresPosition();
                return mask;
            });
        }

        public NeededCoordinateSpace RequiresTangent()
        {
            if (subGraph == null)
                return NeededCoordinateSpace.None;

            return subGraph.activeNodes.OfType<IMayRequireTangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) =>
            {
                mask |= node.RequiresTangent();
                return mask;
            });
        }

        public bool RequiresTime()
        {
            if (subGraph == null)
                return false;

            return subGraph.activeNodes.OfType<IMayRequireTime>().Any(x => x.RequiresTime());
        }

        public NeededCoordinateSpace RequiresBitangent()
        {
            if (subGraph == null)
                return NeededCoordinateSpace.None;

            return subGraph.activeNodes.OfType<IMayRequireBitangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) =>
            {
                mask |= node.RequiresBitangent();
                return mask;
            });
        }

        public bool RequiresVertexColor()
        {
            if (subGraph == null)
                return false;

            return subGraph.activeNodes.OfType<IMayRequireVertexColor>().Any(x => x.RequiresVertexColor());
        }
    }
}