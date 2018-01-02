using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Vector3ShaderProperty : VectorShaderProperty
    {
        public Vector3ShaderProperty()
        {
            displayName = "Vector3";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector3; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(value.x, value.y, value.z, 0); }
        }

        public override string GetInlinePropertyDeclarationString()
        {
            return "float3 " + referenceName + ";";
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Vector3)
            {
                name = referenceName,
                vector4Value = value
            };
        }

        public override INode ToConcreteNode()
        {
            return new Vector3Node { value = value };
        }
    }
}
