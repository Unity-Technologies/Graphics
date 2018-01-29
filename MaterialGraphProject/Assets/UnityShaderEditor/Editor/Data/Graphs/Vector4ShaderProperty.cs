using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Vector4ShaderProperty : VectorShaderProperty
    {
        public Vector4ShaderProperty()
        {
            displayName = "Vector4";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector4; }
        }

        public override Vector4 defaultValue
        {
            get { return value; }
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Vector4)
            {
                name = referenceName,
                vector4Value = value
            };
        }

        public override INode ToConcreteNode()
        {
            return new Vector4Node { value = value };
        }
    }
}
