using System;
using UnityEngine;

namespace UnityEditor.VFX
{
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
