using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Remapper/Remap Input Node")]
    public class MasterRemapInputNode : AbstractSubGraphIONode
        , IGeneratesBodyCode
        , IGeneratesFunction
        , IMayRequireNormal
        , IMayRequireTangent
        , IMayRequireBitangent
        , IMayRequireMeshUV
        , IMayRequireScreenPosition
        , IMayRequireViewDirection
        , IMayRequireWorldPosition
        , IMayRequireVertexColor
    {
        [NonSerialized]
        internal RemapMasterNode m_RemapTarget;

        public MasterRemapInputNode()
        {
            name = "Inputs";
        }

        public override int AddSlot()
        {
            var nextSlotId = GetOutputSlots<ISlot>().Count() + 1;
            AddSlot(new MaterialSlot(-nextSlotId, "Input " + nextSlotId, "Input" + nextSlotId, SlotType.Output, SlotValueType.Vector4, Vector4.zero));

            if (onModified != null)
            {
                onModified(this, ModificationScope.Graph);
            }

            return -nextSlotId;
        }

        public override void RemoveSlot()
        {
            var lastSlotId = GetOutputSlots<ISlot>().Count();
            if (lastSlotId == 0)
                return;

            RemoveSlot(-lastSlotId);

            if (onModified != null)
            {
                onModified(this, ModificationScope.Graph);
            }

        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);
            foreach (var slot in GetOutputSlots<MaterialSlot>())
            {
                properties.Add(
                    new PreviewProperty
                {
                    m_Name = GetVariableNameForSlot(slot.id),
                    m_PropType = PropertyType.Vector4,
                    m_Vector4 = slot.defaultValue
                }
                    );
            }
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (m_RemapTarget != null)
                m_RemapTarget.GenerateNodeCode(visitor, generationMode);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (m_RemapTarget != null)
                m_RemapTarget.GenerateNodeFunction(visitor, generationMode);
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (m_RemapTarget != null)
                m_RemapTarget.GeneratePropertyBlock(visitor, generationMode);
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (m_RemapTarget == null)
            {
                foreach (var slot in GetOutputSlots<MaterialSlot>())
                {
                    var outDimension = ConvertConcreteSlotValueTypeToString(slot.concreteValueType);
                    visitor.AddShaderChunk("float" + outDimension + " " + GetVariableNameForSlot(slot.id) + ";", true);
                }
            }
            else
            {
                if (m_RemapTarget != null)
                    m_RemapTarget.GeneratePropertyUsages(visitor, generationMode);
            }
        }

        public bool RequiresNormal()
        {
            if (m_RemapTarget == null)
                return false;

            return m_RemapTarget.RequiresNormal();
        }

        public bool RequiresTangent()
        {
            if (m_RemapTarget == null)
                return false;

            return m_RemapTarget.RequiresTangent();
        }

        public bool RequiresBitangent()
        {
            if (m_RemapTarget == null)
                return false;

            return m_RemapTarget.RequiresBitangent();
        }

        public bool RequiresMeshUV()
        {
            if (m_RemapTarget == null)
                return false;

            return m_RemapTarget.RequiresMeshUV();
        }

        public bool RequiresScreenPosition()
        {
            if (m_RemapTarget == null)
                return false;

            return m_RemapTarget.RequiresScreenPosition();
        }

        public bool RequiresViewDirection()
        {
            if (m_RemapTarget == null)
                return false;

            return m_RemapTarget.RequiresViewDirection();
        }

        public bool RequiresWorldPosition()
        {
            if (m_RemapTarget == null)
                return false;

            return m_RemapTarget.RequiresWorldPosition();
        }

        public bool RequiresVertexColor()
        {
            if (m_RemapTarget == null)
                return false;

            return m_RemapTarget.RequiresVertexColor();
        }
    }
}
