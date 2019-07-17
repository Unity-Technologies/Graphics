using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class ProceduralSpaceMaterialSlot : MaterialSlot, IProceduralMaterialSlot
    {
        [SerializeField]
        private CoordinateSpace m_Space = CoordinateSpace.World;
        public CoordinateSpace space => m_Space;

        [SerializeField]
        private ConcreteSlotValueType m_ValueType = ConcreteSlotValueType.Vector3;
        public override ConcreteSlotValueType concreteValueType => m_ValueType;
        public override SlotValueType valueType => m_ValueType.ToSlotValueType();

        [SerializeField]
        private string m_DefaultValue;

        protected override string ConcreteSlotValueAsVariable()
            => m_DefaultValue;

        public override VisualElement InstantiateControl()
            => new LabelSlotControlView(m_Space + " Space");

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        { }

        public ProceduralSpaceMaterialSlot()
        { }

        public ProceduralSpaceMaterialSlot(int slotId, string displayName, string shaderOutputName, string defaultValue, ConcreteSlotValueType valueType, CoordinateSpace space,
                                    ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, stageCapability, hidden: hidden)
        {
            m_Space = space;
            m_ValueType = valueType;
            m_DefaultValue = defaultValue;
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            if (foundSlot is ProceduralSpaceMaterialSlot proceduralSlot)
            {
                m_Space = proceduralSlot.m_Space;
                m_ValueType = proceduralSlot.m_ValueType;
                m_DefaultValue = proceduralSlot.m_DefaultValue;
            }
        }
    }
}
