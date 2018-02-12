using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Attribute/Current", variantProvider = typeof(AttributeVariant))]
    class VFXCurrentAttributeParameter : VFXAttributeParameter
    {
        public override VFXAttributeLocation location
        {
            get
            {
                return VFXAttributeLocation.Current;
            }
        }
    }
}
