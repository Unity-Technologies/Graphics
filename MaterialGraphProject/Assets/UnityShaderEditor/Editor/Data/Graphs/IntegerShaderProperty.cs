using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class IntegerShaderProperty : AbstractShaderProperty<int>
    {
        public IntegerShaderProperty()
        {
            displayName = "Integer";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Float; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(value, value, value, value); }
        }

        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
            result.Append("\", Int) = ");
            result.Append(value);
            return result.ToString();
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("float {0}{1}", referenceName, delimiter);
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Float)
            {
                name = referenceName,
                floatValue = value
            };
        }

        public override INode ToConcreteNode()
        {
            return new IntegerNode { value = value };
        }
    }
}
