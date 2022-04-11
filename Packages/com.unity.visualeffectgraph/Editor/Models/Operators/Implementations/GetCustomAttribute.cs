using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.VFX;
using UnityEditor.VFX.Block;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Attribute", experimental = true)]
    class GetCustomAttribute : VFXOperator
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Delayed, Tooltip("Specifies the name of the custom attribute to use.")]
        public string attribute = "CustomAttribute";

        [VFXSetting, Tooltip("Specifies which version of the parameter to use. It can return the current value, or the source value derived from a GPU event or a spawn attribute.")]
        public VFXAttributeLocation location = VFXAttributeLocation.Current;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies the type of the custom attribute to use.")]
        public CustomAttributeUtility.Signature AttributeType = CustomAttributeUtility.Signature.Float;

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                var attribute = currentAttribute;
                yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(attribute.type), attribute.name));
            }
        }

        private VFXAttribute currentAttribute
        {
            get
            {
                return new VFXAttribute(attribute, CustomAttributeUtility.GetValueType(AttributeType));
            }
        }

        override public string libraryName { get { return "Get Custom Attribute"; } }

        override public string name
        {
            get
            {
                return "Get " + attribute + " (" + AttributeType.ToString() + ")";
            }
        }
        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var attribute = currentAttribute;

            var expression = new VFXAttributeExpression(attribute, location);
            return new VFXExpression[] { expression };
        }
    }
}
