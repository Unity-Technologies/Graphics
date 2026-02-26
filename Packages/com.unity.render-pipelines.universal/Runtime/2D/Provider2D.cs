using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Base class for 2D shadow shape providers.
    /// </summary>
    [Serializable]
    public abstract class Provider2D
    {
        /// <summary>
        /// Returns the priority used to order this provider in the ShadowCaster2D Casting Option dropdown.
        /// </summary>
        /// <returns>The menu priority for this provider.</returns>
        public virtual int MenuPriority() { return 0; }

        /// <summary>
        /// Indicates whether this provider reads data from a component on the GameObject.
        /// </summary>
        /// <returns>True if the provider uses component data; otherwise, false.</returns>
        public virtual bool UsesComponentData() { return true; }

        /// <summary>
        /// Called when the provider is initialized.
        /// </summary>
        public virtual void OnAwake() { }

#if UNITY_EDITOR
        public virtual void OnSelected() { }
        public virtual void OnDrawGizmos(Transform transform) {}
#endif

        internal abstract GUIContent Internal_ProviderName(string componentName);
        internal virtual bool Internal_IsRequiredComponentData(Component sourceComponent) { return sourceComponent is Transform; }
    }
}
