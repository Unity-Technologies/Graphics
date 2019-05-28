using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    enum FloatType
    {
        Default,
        Slider,
        Integer,
        Enum
    }

    public enum EnumType
    {
        Enum,
        CSharpEnum,
        KeywordEnum,
    }

    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.FloatShaderProperty")]
    class Vector1ShaderProperty : AbstractShaderProperty<float>
    {
        public Vector1ShaderProperty()
        {
            displayName = "Vector1";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector1; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(value, value, value, value); }
        }

        public override bool isBatchable
        {
            get { return true; }
        }

        public override bool isExposable
        {
            get { return true; }
        }

        public override bool isRenamable
        {
            get { return true; }
        }

        [SerializeField]
        private FloatType m_FloatType = FloatType.Default;

        public FloatType floatType
        {
            get { return m_FloatType; }
            set
            {
                if (m_FloatType == value)
                    return;
                m_FloatType = value;
            }
        }

        [SerializeField]
        private Vector2 m_RangeValues = new Vector2(0, 1);

        public Vector2 rangeValues
        {
            get { return m_RangeValues; }
            set
            {
                if (m_RangeValues == value)
                    return;
                m_RangeValues = value;
            }
        }

        private EnumType m_EnumType = EnumType.Enum;

        public EnumType enumType
        {
            get { return m_EnumType; }
            set
            {
                if (m_EnumType == value)
                    return;
                m_EnumType = value;
            }
        }
    
        Type m_CSharpEnumType;

        public Type cSharpEnumType
        {
            get => m_CSharpEnumType;
            set => m_CSharpEnumType = value;
        }

        private List<string> m_EnumNames = new List<string>();
        private List<int> m_EnumValues = new List<int>();

        public List<string> enumNames
        {
            get => m_EnumNames;
            set => m_EnumNames = value;
        }

        public List<int> enumValues
        {
            get => m_EnumValues;
            set => m_EnumValues = value;
        }

        [SerializeField]
        bool    m_Hidden = false;

        public bool hidden
        {
            get { return m_Hidden; }
            set { m_Hidden = value; }
        }

        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            if (hidden)
                result.Append("[HideInInspector] ");
            switch (floatType)
            {
                case FloatType.Slider:
                    result.Append($"{referenceName}(\"{displayName} \", Range(");
                    result.Append(NodeUtils.FloatToShaderValue(m_RangeValues.x) + ", " + NodeUtils.FloatToShaderValue(m_RangeValues.y));
                    result.Append(")) = ");
                    break;
                case FloatType.Integer:
                    result.Append($"{referenceName}(\"{displayName} \", Int) = ");
                    break;
                case FloatType.Enum:
                    string enumValuesString = "";
                    string enumTypeString = enumType.ToString();
                    switch (enumType)
                    {
                        case EnumType.CSharpEnum:
                            enumValuesString = m_CSharpEnumType.ToString();
                            enumTypeString = "Enum";
                            break;
                        case EnumType.KeywordEnum:
                            enumValuesString = string.Join(", ", enumNames);
                            break;
                        default:
                            for (int i = 0; i < enumNames.Count; i++)
                            {
                                int value = (i < enumValues.Count) ? enumValues[i] : i;
                                enumValuesString += (enumNames[i] + ", " + value + ((i != enumNames.Count - 1) ? ", " : ""));
                            }
                            break;
                    }
                    result.Append($"[{enumTypeString}({enumValuesString})] {referenceName}(\"{displayName}\", Float) = ");
                    break;
                default:
                    result.Append($"{referenceName}(\"{displayName} \", Float) = ");
                    break;
            }
            result.Append(NodeUtils.FloatToShaderValue(value));
            return result.ToString();
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("{0} {1}{2}", concretePrecision.ToShaderString(), referenceName, delimiter);
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Vector1)
            {
                name = referenceName,
                floatValue = value
            };
        }

        public override AbstractMaterialNode ToConcreteNode()
        {
            switch (m_FloatType)
            {
                case FloatType.Slider:
                    return new SliderNode { value = new Vector3(value, m_RangeValues.x, m_RangeValues.y) };
                case FloatType.Integer:
                    return new IntegerNode { value = (int)value };
                default:
                    var node = new Vector1Node();
                    node.FindInputSlot<Vector1MaterialSlot>(Vector1Node.InputSlotXId).value = value;
                    return node;
            }
        }

        public override AbstractShaderProperty Copy()
        {
            var copied = new Vector1ShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            copied.floatType = floatType;
            copied.rangeValues = rangeValues;
            copied.enumType = enumType;
            copied.enumNames = enumNames;
            copied.enumValues = enumValues;
            return copied;
        }
    }
}
