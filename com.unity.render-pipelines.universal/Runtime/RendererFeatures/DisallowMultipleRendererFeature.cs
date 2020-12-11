using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    ///   <para>Prevents ScriptableRendererFeatures of same type to be added more than once to a Scriptable Renderer.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DisallowMultipleRendererFeature : Attribute
    {
        public bool displayName { get; private set; }
        public DisallowMultipleRendererFeature(bool displayName = true) { this.displayName = displayName; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ExecuteAfterRendererFeature : Attribute
    {
        public Type rendererFeatureType { get; private set; }
        public bool isRequired { get; private set; }
        public ExecuteAfterRendererFeature(Type type, bool require = false) { rendererFeatureType = type; isRequired = require; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ExecuteBeforeRendererFeature : Attribute
    {
        public Type rendererFeatureType { get; private set; }
        public bool isRequired { get; private set; }
        public ExecuteBeforeRendererFeature(Type type, bool require = false) { rendererFeatureType = type; isRequired = require; }
    }
}
