using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class BooleanShaderProperty : AbstractShaderProperty<bool>
    {
        public BooleanShaderProperty()
        {
            displayName = "Boolean";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Boolean; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(); }
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
        bool m_Hidden = false;

        public bool hidden
        {
            get { return m_Hidden; }
            set { m_Hidden = value; }
        }

        public override bool isRenamable
        {
            get { return true; }
        }

        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            if (hidden)
            {
                result.Append("[HideInInspector] ");
            }
            result.Append("[ToggleUI] ");
            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
            result.Append("\", Float) = ");
            result.Append(value == true ? 1 : 0);
            return result.ToString();
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("{0} {1}{2}", concretePrecision.ToShaderString(), referenceName, delimiter);
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Boolean)
            {
                name = referenceName,
                booleanValue = value
            };
        }

        public override AbstractMaterialNode ToConcreteNode()
        {
            return new BooleanNode { value = new ToggleData(value) };
        }

        public override AbstractShaderProperty Copy()
        {
            var copied = new BooleanShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
