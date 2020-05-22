using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class UnknownMaterialSlot : MaterialSlot
    {
        public override bool isDefaultValue => true;

        public override SlotValueType valueType => SlotValueType.Unknown;

        public override ConcreteSlotValueType concreteValueType => ConcreteSlotValueType.Unknown;

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
        }
    }
}
