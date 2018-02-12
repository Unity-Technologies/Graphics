using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Attribute/Source", variantProvider = typeof(AttributeVariant))]
    class VFXSourceAttributeParameter : VFXAttributeParameter
    {
        public override VFXAttributeLocation location
        {
            get
            {
                return VFXAttributeLocation.Source;
            }
        }
    }
}
