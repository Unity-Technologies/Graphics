using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class SliderShaderProperty : AbstractShaderProperty<Vector3>
    {
        public SliderShaderProperty()
        {
            displayName = "Slider";
            value = new Vector3(0, 0, 1);
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Float; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(value.x, value.y, value.z, value.x); }
        }

        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
            result.Append("\", Range(");
            result.Append(value.y);
            result.Append(", ");
            result.Append(value.z);
            result.Append(")) = ");
            result.Append(value.x);
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
                floatValue = value.x
            };
        }

        public override INode ToConcreteNode()
        {
            return new SliderNode { value = value };
        }
    }
}
