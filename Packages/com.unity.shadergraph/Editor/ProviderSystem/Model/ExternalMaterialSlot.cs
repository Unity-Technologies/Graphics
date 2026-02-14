using System;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    interface INeedsExplicitCompatibilityTest
    {
        bool HasExplicitCompatibility(MaterialSlot other);

        static bool TestExplicitCompatibility(MaterialSlot a, MaterialSlot b)
        {
            return a is INeedsExplicitCompatibilityTest atester
                && b is INeedsExplicitCompatibilityTest btester
                && atester.HasExplicitCompatibility(b)
                && btester.HasExplicitCompatibility(a);
        }
    }

    [Serializable]
    internal class ExternalMaterialSlot : MaterialSlot, INeedsExplicitCompatibilityTest
    {
        public string TypeName => m_typeName;

        [SerializeField]
        string m_typeName;

        [SerializeField]
        string m_rawDefaultValueString;

        public override bool isDefaultValue => throw new System.NotImplementedException();

        public override SlotValueType valueType => SlotValueType.External;

        public override ConcreteSlotValueType concreteValueType => ConcreteSlotValueType.External;

        public ExternalMaterialSlot() : base()
        {

        }

        public ExternalMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            string typeName,
            SlotType slotType,
            string rawDefaultValue = null,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
        : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
            m_typeName = typeName;
            m_rawDefaultValueString = rawDefaultValue ?? "0";
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode) { }

        public override void CopyValuesFrom(MaterialSlot foundSlot) { }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return $"({TypeName})({m_rawDefaultValueString})";
        }

        public bool HasExplicitCompatibility(MaterialSlot other)
            => other is ExternalMaterialSlot externalSlot && externalSlot.TypeName == this.TypeName;
    }
}
