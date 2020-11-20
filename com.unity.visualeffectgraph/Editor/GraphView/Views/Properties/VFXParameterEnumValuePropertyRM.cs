using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
using Object = UnityEngine.Object;
using Type = System.Type;
using EnumField = UnityEditor.VFX.UIElements.VFXEnumField;
using VFXVector2Field = UnityEditor.VFX.UI.VFXVector2Field;
using VFXVector4Field = UnityEditor.VFX.UI.VFXVector4Field;

namespace UnityEditor.VFX.UI
{
    class VFXListParameterEnumValuePropertyRM : ListPropertyRM<string, StringPropertyRM>
    {
        public VFXListParameterEnumValuePropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        protected override StringPropertyRM CreateField(IPropertyRMProvider provider)
        {
            return new StringPropertyRM(provider, 18);
        }

        protected override string CreateItem()
        {
            return "New item";
        }
    }
}
