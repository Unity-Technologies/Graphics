using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using Object = System.Object;

namespace UnityEditor.VFX
{
    class InlineTypeProvider : VariantProvider
    {
        private static IEnumerable<Type> GetValidTypes()
        {
            return VFXLibrary.GetSlotsType().Where(x => VFXLibrary.GetAttributeFromSlotType(x)?.usages.HasFlag(VFXTypeAttribute.Usage.ExcludeFromProperty) != true);
        }

        public override IEnumerable<Variant> GetVariants()
        {
            foreach (var validType in GetValidTypes())
            {
                yield return new Variant(
                    validType.UserFriendlyName(),
                    "Inline",
                    typeof(VFXInlineOperator),
                    new [] { new KeyValuePair<string, object>("m_Type", new SerializableType(validType)) }
                );

            }
        }
    }

    [VFXInfo(variantProvider = typeof(InlineTypeProvider))]
    class VFXInlineOperator : VFXOperator
    {
        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.None)]
        private SerializableType m_Type;

        public Type type
        {
            get
            {
                return (Type)m_Type;
            }
        }

        public override string name
        {
            get
            {
                var type = (Type)m_Type;
                return type == null ? string.Empty : VFXTypeExtension.UserFriendlyName(type);
            }
        }

        private IEnumerable<VFXPropertyWithValue> property
        {
            get
            {
                var type = (Type)m_Type;
                if (type != null)
                {
                    var property = new VFXProperty(type, string.Empty);
                    yield return new VFXPropertyWithValue(property, VFXTypeExtension.GetDefaultField(type));
                }
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties { get { return property; } }
        protected override IEnumerable<VFXPropertyWithValue> outputProperties { get { return property; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return inputExpression;
        }

        internal override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);

            var type = this.type;
            if (Deprecated.s_Types.Contains(type))
            {
                report.RegisterError(
                    "DeprecatedTypeInlineOperator",
                    VFXErrorType.Warning,
                    string.Format("The structure of the '{0}' has changed, the position property has been moved to a transform type. You should consider to recreate this operator.", type), this);
            }
        }

        public override void Sanitize(int version)
        {
            if (type == null)
            {
                // First try to force deserialization
                if (m_Type != null)
                {
                    m_Type.OnAfterDeserialize();
                }
                // if it doesn't work set it to int.
                if (type == null)
                    m_Type = new SerializableType(typeof(int));
            }
            base.Sanitize(version);
        }
    }
}
