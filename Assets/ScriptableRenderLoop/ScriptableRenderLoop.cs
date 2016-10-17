using UnityEngine.Rendering;

namespace UnityEngine.ScriptableRenderLoop
{
    public abstract class ScriptableRenderLoop : ScriptableObject
    {
        public abstract void Render(Camera[] cameras, RenderLoop renderLoop);
        public virtual void Rebuild() {}

        #if UNITY_EDITOR
        public virtual UnityEditor.SupportedRenderingFeatures GetSupportedRenderingFeatures() { return new UnityEditor.SupportedRenderingFeatures(); }
        #endif
    }
}
