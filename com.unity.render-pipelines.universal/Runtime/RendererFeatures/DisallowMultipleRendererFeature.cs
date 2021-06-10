using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    ///   <para>Prevents ScriptableRendererFeatures of same type to be added more than once to a Scriptable Renderer.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DisallowMultipleRendererFeature : Attribute
    {
        /// <summary>
        /// Set the custom title for renderer feature.
        /// </summary>
        public string customTitle { private set; get; }

        public DisallowMultipleRendererFeature(string customTitle = null)
        {
            this.customTitle = customTitle;
        }
    }
}
