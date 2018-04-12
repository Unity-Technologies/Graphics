using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public interface IShaderProperty
    {
        string displayName { get; set; }

        string referenceName { get; }

        PropertyType propertyType { get; }
        Guid guid { get; }
        bool generatePropertyBlock { get; set; }
        bool useCustomReferenceName { get; set; }
        string customReferenceName { get; set; }
        Vector4 defaultValue { get; }
        string overrideReferenceName { get; set; }

        string GetPropertyBlockString();
        string GetPropertyDeclarationString(string delimiter = ";");

        string GetPropertyAsArgumentString();

        PreviewProperty GetPreviewMaterialProperty();
        INode ToConcreteNode();
    }
}
