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
            return VFXAttribute.AllIncludingVariadicExceptLocalOnly.ToArray();
        }
    }

    class ReadWritableAttributeProvider : IStringProvider
    {
        public string[] GetAvailableString()
        {
            return VFXAttribute.AllIncludingVariadicReadWritable.ToArray();
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
                    { "attribute", VFXAttribute.AllIncludingVariadicExceptLocalOnly.Cast<object>().ToArray() }
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
                    { "attribute", VFXAttribute.AllIncludingVariadicReadWritable.Cast<object>().ToArray() }
                };
            }
        }
    }

    class AttributeVariantReadWritableNoVariadic : IVariantProvider
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
        public string attribute = VFXAttribute.AllIncludingVariadic.First();

        [VFXSetting, Tooltip("Select the version of this parameter that is used.")]
        public VFXAttributeLocation location = VFXAttributeLocation.Current;

        [VFXSetting, Regex("[^x-zX-Z]", 3)]
        public string mask = "xyz";

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (string setting in base.filteredOutSettings) yield return setting;
                var attribute = VFXAttribute.Find(this.attribute);
                if (attribute.variadic == VFXVariadic.False) yield return "mask";
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                var attribute = VFXAttribute.Find(this.attribute);
                if (attribute.variadic == VFXVariadic.True)
                {
                    Type slotType = null;
                    switch (mask.Length)
                    {
                        case 1: slotType = typeof(float); break;
                        case 2: slotType = typeof(Vector2); break;
                        case 3: slotType = typeof(Vector3); break;
                        case 4: slotType = typeof(Vector4); break;
                        default: break;
                    }

                    if (slotType != null)
                        yield return new VFXPropertyWithValue(new VFXProperty(slotType, attribute.name));
                }
                else
                {
                    yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(attribute.type), attribute.name));
                }
            }
        }

        override public string libraryName { get { return "Get Attribute: " + attribute; } }
        override public string name
        {
            get
            {
                string result = string.Format("Get Attribute: {0} ({1})", attribute, location);

                var attrib = VFXAttribute.Find(this.attribute);
                if (attrib.variadic == VFXVariadic.True)
                    result += "." + mask;

                return result;
            }
        }

        public override void Sanitize()
        {
            if (attribute == "phase") // Replace old phase attribute with random operator
            {
                Debug.Log("Sanitizing Graph: Automatically replace Phase Attribute Parameter with a Fixed Random Operator");

                var randOp = ScriptableObject.CreateInstance<Operator.Random>();
                randOp.constant = true;
                randOp.seed = Operator.Random.SeedMode.PerParticle;

                VFXSlot.CopyLinksAndValue(randOp.GetOutputSlot(0), GetOutputSlot(0), true);
                ReplaceModel(randOp, this);
            }
            else
            {
                base.Sanitize();
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var attribute = VFXAttribute.Find(this.attribute);
            if (attribute.variadic == VFXVariadic.True)
            {
                var attributes = new VFXAttribute[] { VFXAttribute.Find(attribute.name + "X"), VFXAttribute.Find(attribute.name + "Y"), VFXAttribute.Find(attribute.name + "Z") };
                var expressions = attributes.Select(a => new VFXAttributeExpression(a, location)).ToArray();

                var componentStack = new Stack<VFXExpression>();
                int outputSize = mask.Length;
                for (int iComponent = 0; iComponent < outputSize; iComponent++)
                {
                    char componentChar = char.ToLower(mask[iComponent]);
                    int currentComponent = Math.Min(componentChar - 'x', 2);
                    componentStack.Push(expressions[currentComponent]);
                }

                VFXExpression finalExpression = null;
                if (componentStack.Count == 1)
                {
                    finalExpression = componentStack.Pop();
                }
                else
                {
                    finalExpression = new VFXExpressionCombine(componentStack.Reverse().ToArray());
                }
                return new[] { finalExpression };
            }
            else
            {
                var expression = new VFXAttributeExpression(attribute, location);
                return new VFXExpression[] { expression };
            }
        }
    }
}
