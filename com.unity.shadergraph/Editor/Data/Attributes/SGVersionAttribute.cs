
using System;
using UnityEditor.ShaderGraph.Serialization;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class SGVersionAttribute : Attribute
{
    private SGVersion m_Version;
    public SGVersion version { get => m_Version; }
    public SGVersionAttribute(Type type, int behaviorVersion = 0, int explicitVersion = 0)
    {
        m_Version = new SGVersion()
        {
            type = type,
            behaviorVersion = behaviorVersion,
            explicitVersion = explicitVersion
        };
    }
}
