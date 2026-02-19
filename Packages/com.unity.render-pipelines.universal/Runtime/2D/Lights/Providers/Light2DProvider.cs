using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class <c>Light2DProvider</c> has methods called in URP 2D which can be used to create custom lights.
    /// </summary>
    [Serializable]
    public class Light2DProvider : Provider2D
    {
        /// <summary>
        /// Returns the display name shown in the Light Type dropdown of the Light2D component.
        /// </summary>
        /// <returns>The GUIContent used in the Light2D Light Type dropdown.</returns>
        public virtual GUIContent ProviderName() { return new GUIContent("Custom Light Provider", "Implemented by " + this.GetType().Name); }

        /// <summary>
        /// Returns the mesh used to render the Light2D.
        /// </summary>
        /// <returns>The mesh used for rendering.</returns>
        public virtual Mesh GetMesh() { return null; }

        /// <summary>
        /// Internal entry point used by the rendering pipeline to retrieve the provider name.
        /// </summary>
        internal override GUIContent Internal_ProviderName(string componentName) { return ProviderName(); }
    }
}
