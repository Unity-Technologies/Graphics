using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    enum FloatType
    {
        Default,
        Slider,
        Integer
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
            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
            switch (floatType)
            {
                case FloatType.Slider:
                    result.Append("\", Range(");
                    result.Append(NodeUtils.FloatToShaderValue(m_RangeValues.x) + ", " + NodeUtils.FloatToShaderValue(m_RangeValues.y));
                    result.Append(")) = ");
                    break;
                case FloatType.Integer:
                    result.Append("\", Int) = ");
                    break;
                default:
                    result.Append("\", Float) = ");
                    break;
            }
            result.Append(NodeUtils.FloatToShaderValue(value));
            return result.ToString();
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("float {0}{1}", referenceName, delimiter);
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
            return copied;
        }
    }
}
