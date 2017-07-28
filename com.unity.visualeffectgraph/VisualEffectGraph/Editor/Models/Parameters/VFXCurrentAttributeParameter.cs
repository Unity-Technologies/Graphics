using System;
using UnityEngine;

namespace UnityEditor.VFX
{
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
