using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Attribute used to set path in advanced dropdown menu for Scriptable Renderer Feature.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class RendererFeatureInfoAttribute : Attribute
    {
        /// <summary>A string path split for each directory.</summary>
        public string[] Path { get; }

        /// <summary>A bool to set if duplicate of the same type is disallowed.</summary>
        public bool DisallowMultipleRendererFeatures { get; }

        public RendererFeatureInfoAttribute(string Path, bool DisallowMultipleRendererFeatures = false)
        {
            this.Path = Path.Split('/');
            this.DisallowMultipleRendererFeatures = DisallowMultipleRendererFeatures;
        }
    }
}
