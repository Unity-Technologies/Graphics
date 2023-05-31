using System.Collections.Generic;

using UnityEditor.VFX.Block;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXHelpURL("Operator-GetCustomAttribute")]
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
                var vfxAttribute = currentAttribute;
                yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(vfxAttribute.type), vfxAttribute.name));
            }
        }

        private VFXAttribute currentAttribute => new(attribute, CustomAttributeUtility.GetValueType(AttributeType));

        public override string libraryName { get; } = "Get Attribute: custom";

        public override string name => $"Get '{attribute}' ({AttributeType})";

        internal sealed override void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            base.GenerateErrors(manager);

            if (!CustomAttributeUtility.IsShaderCompilableName(attribute))
            {
                manager.RegisterError("InvalidCustomAttributeName", VFXErrorType.Error, $"Custom attribute name '{attribute}' is not valid.\n\t- The name must not contain spaces or any special character\n\t- The name must not start with a digit character");
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var vfxAttribute = currentAttribute;

            var expression = new VFXAttributeExpression(vfxAttribute, location);
            return new VFXExpression[] { expression };
        }
    }
}
