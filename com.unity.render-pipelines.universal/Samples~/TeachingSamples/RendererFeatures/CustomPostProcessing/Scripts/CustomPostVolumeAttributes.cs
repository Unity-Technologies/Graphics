using System;

[AttributeUsage(AttributeTargets.Field)]
public class ShaderReferenceAttribute : Attribute
{
    public string shaderReferenceName;

    public ShaderReferenceAttribute(string shaderReferenceName)
    {
        this.shaderReferenceName = shaderReferenceName;
    }
}

public class ForCustomPostProcessingAttribute : Attribute
{
}