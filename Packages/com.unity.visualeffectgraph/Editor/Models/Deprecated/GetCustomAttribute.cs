using System;
using System.Collections.Generic;

using UnityEditor.VFX.Block;
using UnityEngine;

namespace UnityEditor.VFX
{
    [Obsolete]
    [VFXHelpURL("Operator-GetCustomAttribute")]
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
                var vfxAttribute = currentAttribute;
                yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(vfxAttribute.type), vfxAttribute.name));
            }
        }

        public override string name => $"Get '{attribute}' ({AttributeType})";

        public override void Sanitize(int version)
        {
            GetGraph().TryAddCustomAttribute(attribute, CustomAttributeUtility.GetValueType(AttributeType), string.Empty, false, out var vfxAttribute);
            var vfxAttributeParameter = ScriptableObject.CreateInstance<VFXAttributeParameter>();
            if (attribute != vfxAttribute.name)
            {
                Debug.Log($"[Sanitize] Get Custom Attribute {attribute} has been renamed into {vfxAttribute.name}");
            }
            vfxAttributeParameter.attribute = vfxAttribute.name;
            vfxAttributeParameter.location = location;
            vfxAttributeParameter.ResyncSlots(true);
            ReplaceModel(vfxAttributeParameter, this, true, false);
            VFXSlot.CopyLinksAndValue(vfxAttributeParameter.outputSlots[0], outputSlots[0]);
        }

        internal sealed override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);

            if (!CustomAttributeUtility.IsShaderCompilableName(attribute))
            {
                report.RegisterError("InvalidCustomAttributeName", VFXErrorType.Error, $"Custom attribute name '{attribute}' is not valid.\n\t- The name must not contain spaces or any special character\n\t- The name must not start with a digit character", this);
            }
        }

        protected override void OnAdded()
        {
            Sanitize(0);
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var vfxAttribute = currentAttribute;

            var expression = new VFXAttributeExpression(vfxAttribute, location);
            return new VFXExpression[] { expression };
        }

        private VFXAttribute currentAttribute => new VFXAttribute(attribute, CustomAttributeUtility.GetValueType(AttributeType), string.Empty);
    }
}
