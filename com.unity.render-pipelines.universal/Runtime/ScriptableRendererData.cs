using System;
using System.Collections.Generic;

#if UNITY_EDITOR
#endif

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class <c>ScriptableRendererData</c> contains resources for a <c>ScriptableRenderer</c>.
    /// <seealso cref="ScriptableRenderer"/>
    /// </summary>
    [Serializable]
    public abstract class ScriptableRendererData
    {
        public string name;
#if UNITY_EDITOR
        [SerializeField] internal int index;
#endif
        internal bool isInvalidated { get; set; }

        /// <summary>
        /// Class contains references to shader resources used by Rendering Debugger.
        /// </summary>
        [Serializable, ReloadGroup]
        public sealed class DebugShaderResources
        {
            /// <summary>
            /// Debug shader used to output interpolated vertex attributes.
            /// </summary>
            [Reload("Shaders/Debug/DebugReplacement.shader")]
            public Shader debugReplacementPS;
        }

        /// <summary>
        /// Container for shader resources used by Rendering Debugger.
        /// </summary>
        public DebugShaderResources debugShaders;

        /// <summary>
        /// Creates the instance of the ScriptableRenderer.
        /// </summary>
        /// <returns>The instance of ScriptableRenderer</returns>
        protected abstract ScriptableRenderer Create();

        [SerializeReference] internal List<ScriptableRendererFeature> m_RendererFeatures = new List<ScriptableRendererFeature>(10);

        /// <summary>
        /// List of additional render pass features for this renderer.
        /// </summary>
        public List<ScriptableRendererFeature> rendererFeatures
        {
            get => m_RendererFeatures;
        }

        public ScriptableRendererData()
        {
            name = this.GetType().Name;
            //TODO: Check if this is required.
            //Awake();
            //OnEnable();
        }

        /// <summary>
        /// Use SetDirty when changing seeings in the ScriptableRendererData.
        /// It will rebuild the render passes with the new data.
        /// </summary>
        public new void SetDirty()
        {
            isInvalidated = true;
        }

        internal ScriptableRenderer InternalCreateRenderer()
        {
            isInvalidated = false;
            return Create();
        }

        public virtual void OnValidate()
        {
            SetDirty();
        }

        public virtual void Awake() { }

        public virtual void OnEnable()
        {
            SetDirty();
        }

        public virtual void OnDisable() { }

        public virtual void OnBeforeSerialize() { }

        public virtual void OnAfterDeserialize() { }

        /// <summary>
        /// Returns true if contains renderer feature with specified type.
        /// </summary>
        /// <typeparam name="T">Renderer Feature type.</typeparam>
        /// <returns></returns>
        internal bool TryGetRendererFeature<T>(out T rendererFeature) where T : ScriptableRendererFeature
        {
            foreach (var target in rendererFeatures)
            {
                if (target.GetType() == typeof(T))
                {
                    rendererFeature = target as T;
                    return true;
                }
            }
            rendererFeature = null;
            return false;
        }

#if UNITY_EDITOR
        internal virtual Material GetDefaultMaterial(DefaultMaterialType materialType)
        {
            return null;
        }

        internal virtual Shader GetDefaultShader()
        {
            return null;
        }
#endif
    }
}
