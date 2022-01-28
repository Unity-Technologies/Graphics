using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace UnityEngine.Rendering.UIGen
{
    // TODO: [Fred] extension for parser to add attributes.
    public interface IFeatureParser<in TAttribute>
    {
        bool Parse(TAttribute attribute, MemberInfo info, UIDefinition.Property property, out Exception error);
    }
}
