using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#if UNITY_EDITOR
using UnityEngine.Rendering.UIGen.UXML;
#endif

namespace UnityEngine.Rendering.UIGen
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class UIPropertyGeneratorSupportsAttribute : Attribute
    {
        public readonly Type uiType;
        public UIPropertyGeneratorSupportsAttribute(Type uiType)
        {
            this.uiType = uiType;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class UIPropertyGeneratorAttribute : Attribute
    {
        public Type[] supportedTypes { get; }

        public UIPropertyGeneratorAttribute(params Type[] supportedTypes)
        {
            this.supportedTypes = supportedTypes;
        }
    }
}
