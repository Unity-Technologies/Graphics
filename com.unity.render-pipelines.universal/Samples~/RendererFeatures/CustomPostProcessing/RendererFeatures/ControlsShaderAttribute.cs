using System;

[AttributeUsage(AttributeTargets.Class)]
public class ControlsShaderAttribute : Attribute
{
    public string shaderPath;

    public ControlsShaderAttribute(string shaderPath)
    {
        this.shaderPath = shaderPath;
    }
    
}
