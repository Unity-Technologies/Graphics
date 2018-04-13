using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    class AttributeProvider : IStringProvider
    {
        public string[] GetAvailableString()
        {
            return VFXAttribute.AllExceptLocalOnly;
        }
    }

    class ReadWritableAttributeProvider : IStringProvider
    {
        public string[] GetAvailableString()
        {
            return VFXAttribute.AllReadWritable;
        }
    }

    class AttributeVariant : IVariantProvider
    {
        public Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "attribute", VFXAttribute.AllExceptLocalOnly.Concat(VFXAttribute.AllVariadic).Cast<object>().ToArray() }
                };
            }
        }
    }

    class AttributeVariantReadWritable : IVariantProvider
    {
        public Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "attribute", VFXAttribute.AllReadWritable.Cast<object>().ToArray() }
                };
            }
        }
    }

    [VFXInfo(category = "Attribute", variantProvider = typeof(AttributeVariant))]
    class VFXAttributeParameter : VFXOperator
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(AttributeProvider))]
        public string attribute = VFXAttribute.All.Concat(VFXAttribute.AllVariadic).First();

        [VFXSetting, Tooltip("Select the version of this parameter that is used.")]
        public VFXAttributeLocation location = VFXAttributeLocation.Current;

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                var attribute = VFXAttribute.Find(this.attribute);
                yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(attribute.type), attribute.name));
            }
        }

        override public string libraryName { get { return attribute; } }
        override public string name { get { return location + " " + attribute; } }

        public override void Sanitize()
        {
            if (attribute == "phase") // Replace old phase attribute with random operator
            {
                Debug.Log("Sanitizing Graph: Automatically replace Phase Attribute Parameter with a Fixed Random Operator");

                var randOp = ScriptableObject.CreateInstance<Operator.Random>();
                randOp.constant = true;
                randOp.seed = Operator.Random.SeedMode.PerParticle;

                VFXSlot.TransferLinksAndValue(randOp.GetOutputSlot(0), GetOutputSlot(0), true);
                ReplaceModel(randOp, this);
            }
            else
            {
                if (attribute == "size")   attribute = "sizeX";
                else if (attribute == "angle")  attribute = "angleZ";

                base.Sanitize();
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var attribute = VFXAttribute.Find(this.attribute);
            if (attribute.variadic == VFXVariadic.True)
            {
                var attributeX = VFXAttribute.Find(attribute.name + "X");
                var attributeY = VFXAttribute.Find(attribute.name + "Y");
                var attributeZ = VFXAttribute.Find(attribute.name + "Z");

                var expressionX = new VFXAttributeExpression(attributeX, location);
                var expressionY = new VFXAttributeExpression(attributeY, location);
                var expressionZ = new VFXAttributeExpression(attributeZ, location);

                return new VFXExpression[] { new VFXExpressionCombine(expressionX, expressionY, expressionZ) };
            }
            else
            {
                var expression = new VFXAttributeExpression(attribute, location);
                return new VFXExpression[] { expression };
            }
        }
    }
}
