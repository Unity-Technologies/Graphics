using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    ///   <para>Prevents ScriptableRendererFeatures of same type to be added more than once to a Scriptable Renderer.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal class DisallowMultipleRendererFeature : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class HideRendererFeatureName : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ExecuteAfterRendererFeature : Attribute
    {
        public Type rendererFeatureType { get; private set; }
        public ExecuteAfterRendererFeature(Type type) { type = rendererFeatureType; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ExecuteBeforeRendererFeature : Attribute
    {
        public Type rendererFeatureType { get; private set; }
        public ExecuteBeforeRendererFeature(Type type) { rendererFeatureType = type; }
    }
}
