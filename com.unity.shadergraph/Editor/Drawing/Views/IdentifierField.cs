using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    /*
        Field that allows entering a valid HLSL identifier.
        (variable name, function name, ...) this means
        no spaces, no funny characters, never starts with a number, ...
    */
    public class IdentifierField : UIElements.TextValueField<string>
    {
        IdentifierInput tsInput => (IdentifierInput)textInputBase;

        public new class UxmlFactory : UxmlFactory<IdentifierField, UxmlTraits> {}
        public new class UxmlTraits : UIElements.TextValueFieldTraits<string, UxmlStringAttributeDescription> {}

        protected override string ValueToString(string v)
        {
            return v;
        }

        protected override string StringToValue(string str)
        {
            // Make sure this is a valid hlsl identifier. Allowed characters already ensures the characters are valid
            // but identifiers can't start with a number so fix this here.
            if (string.IsNullOrEmpty(str))
            {
                return "_0";
            }
            else if (Char.IsDigit(str[0]))
            {
                return "_" + str;
            }
            else
            {
                return str;
            }
        }

        public new static readonly string ussClassName = "unity-identifierfield-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public IdentifierField() : this((string)null) {}

        public IdentifierField(string label) : base(label, -1, new IdentifierInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            tsInput.AddToClassList(inputUssClassName);
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, UIElements.DeltaSpeed speed, string startValue)
        {
            tsInput.ApplyInputDeviceDelta(delta, speed, startValue);
        }

        class IdentifierInput : TextValueInput
        {
            IdentifierField parentField => (IdentifierField)parent;

            internal IdentifierInput()
            {
                formatString = null;
            }

            protected override string allowedCharacters
            {
                get { return "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_"; }
            }

            public override void ApplyInputDeviceDelta(Vector3 delta, UIElements.DeltaSpeed speed, string startValue)
            {
            }

            protected override string ValueToString(string v)
            {
                return v;
            }

            protected override string StringToValue(string str)
            {
                // Make sure this is a valid hlsl identifier. Allowed characters already ensures the characters are valid
                // but identifiers can't start with a number so fix this here.
                if (string.IsNullOrEmpty(str))
                {
                    return "_0";
                }
                else if (Char.IsDigit(str[0]))
                {
                    return "_" + str;
                }
                else
                {
                    return str;
                }
            }
        }
    }
}
