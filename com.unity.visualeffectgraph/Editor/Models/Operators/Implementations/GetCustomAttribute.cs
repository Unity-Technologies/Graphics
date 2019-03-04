using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX.Block;
using UnityEngine;

namespace UnityEditor.VFX
{
    [Obsolete]
    class GetCustomAttribute : VFXOperator
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Delayed]
        public string attribute = "CustomAttribute";

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
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

            var expression = new VFXAttributeExpression(attribute, VFXAttributeLocation.Current);
            return new VFXExpression[] { expression };
        }

        public override void Sanitize(int version)
        {
            var newOperator = ScriptableObject.CreateInstance<VFXAttributeParameter>();

            var graph = GetGraph();
            if (graph != null)
            {
                if (!graph.customAttributes.Contains(attribute))
                {
                    graph.AddCustomAttribute(attribute, CustomAttributeUtility.GetValueType(AttributeType));
                }
            }

            newOperator.SetSettingValue("attribute", attribute);

            VFXSlot.CopyLinksAndValue(newOperator.GetOutputSlot(0), GetOutputSlot(0), true);

            ReplaceModel(newOperator, this);
            base.Sanitize(version);
        }
    }
}
