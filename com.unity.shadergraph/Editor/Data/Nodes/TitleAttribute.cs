using System;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    class TitleAttribute : ContextFilterableAttribute
    {
        public string[] title;
        public TitleAttribute(params string[] title) { this.title = title; }
    }
}
