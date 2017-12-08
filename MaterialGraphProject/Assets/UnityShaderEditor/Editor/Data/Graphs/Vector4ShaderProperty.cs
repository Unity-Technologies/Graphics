using System;
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
            return new PreviewProperty()
            {
                name = referenceName,
                propType = PropertyType.Vector4,
                vector4Value = value
            };
        }
    }
}
