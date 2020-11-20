using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.AttributeUsage(System.AttributeTargets.Field)]
public class ShaderReferenceAttribute : System.Attribute
{
    public string shaderReferenceName;

    public ShaderReferenceAttribute(string shaderReferenceName)
    {
        this.shaderReferenceName = shaderReferenceName;
    }
}
