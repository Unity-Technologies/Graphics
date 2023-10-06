using System;
using System.Reflection;
using Unity.Collections;


namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Defines the compatiblility with a set of renderer(s)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SupportedOnRendererAttribute : Attribute
    {
        /// <summary>
        /// Returns the compatible renderers
        /// </summary>
        public Type[] rendererTypes { get; }

        /// <summary>
        /// Sets a compatible renderer
        /// </summary>
        /// <param name="renderer">The compatible renderer to set.</param>
        public SupportedOnRendererAttribute(Type renderer)
            : this(new[] { renderer }) {}

        /// <summary>
        /// Sets one or more compatible renderers
        /// </summary>
        /// <param name="renderers">The compatible renderer(s) to set.</param>
        public SupportedOnRendererAttribute(params Type[] renderers)
        {
            if (renderers == null)
            {
                Debug.LogError($"The {nameof(SupportedOnRendererAttribute)} parameters cannot be null.");
                return;
            }

            for (var i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || !typeof(ScriptableRendererData).IsAssignableFrom(r))
                {
                    Debug.LogError($"The {nameof(SupportedOnRendererAttribute)} Attribute targets an invalid {nameof(ScriptableRendererData)}. One of the types cannot be assigned from {nameof(ScriptableRendererData)}");
                    return;
                }
            }

            rendererTypes = renderers;
        }
    }
}
