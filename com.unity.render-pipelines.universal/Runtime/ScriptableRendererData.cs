using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

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

    /// <summary>
    /// Class <c>ScriptableRendererData</c> contains resources for a <c>ScriptableRenderer</c>.
    /// <seealso cref="ScriptableRenderer"/>
    /// </summary>
    [Obsolete("ScriptableRendererData no longer uses ScriptableObject in order to avoid having to many assets.")]
    public abstract class ScriptableRendererDataAssetLegacy : ScriptableObject
    {
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

        [SerializeReference] internal List<ScriptableRendererFeature> m_RendererFeaturesReferences = new List<ScriptableRendererFeature>(10);

        [SerializeField] internal List<ScriptableRendererFeatureAssetLegacy> m_RendererFeatures = new List<ScriptableRendererFeatureAssetLegacy>(10);
        [SerializeField] internal List<long> m_RendererFeatureMap = new List<long>(10);
        [SerializeField] bool m_UseNativeRenderPass = false;

        /// <summary>
        /// Function is called to convert the asset settings into the URP asset such that setting wont be lost.
        /// </summary>
        /// <returns> Returns a new renderer version without sciptable object inheritance. </returns>
        protected virtual ScriptableRendererData UpgradeRendererWithoutAsset() => null;
        void UpgradeRendererFeaturesWithoutAsset(ref Dictionary<string, ScriptableRendererFeature> rendererFeatures)
        {
            foreach (var feature in m_RendererFeatures)
            {
                m_RendererFeaturesReferences.Add(feature.UpgradeToRendererFeatureWithoutAsset(ref rendererFeatures));
            }
        }
        public static ScriptableRendererData UpgradeRendererWithoutAsset(ScriptableRendererDataAssetLegacy data, ref Dictionary<string, ScriptableRendererData> renderers, ref Dictionary<string, ScriptableRendererFeature> rendererFeatures)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(data, out string guid, out long localid);
            if (renderers.TryGetValue(guid, out ScriptableRendererData value))
            {
                return value;
            }
            else
            {
                data.UpgradeRendererFeaturesWithoutAsset(ref rendererFeatures);
                var rendererData = data.UpgradeRendererWithoutAsset();
                rendererData.m_RendererFeatures = data.m_RendererFeaturesReferences;
                renderers.Add(guid, rendererData);
                return rendererData;
            }
        }

        /// <summary>
        /// Use SetDirty when changing seeings in the ScriptableRendererData.
        /// It will rebuild the render passes with the new data.
        /// </summary>
        public new void SetDirty()
        {
            isInvalidated = true;
        }

        protected virtual void OnValidate()
        {
            SetDirty();
#if UNITY_EDITOR
            if (m_RendererFeatures.Contains(null))
                ValidateRendererFeatures();
#endif
        }

        protected virtual void OnEnable()
        {
            SetDirty();
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

        internal bool ValidateRendererFeatures()
        {
            // Get all Subassets
            var subassets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this));
            var linkedIds = new List<long>();
            var loadedAssets = new Dictionary<long, object>();
            var mapValid = m_RendererFeatureMap != null && m_RendererFeatureMap?.Count == m_RendererFeatures?.Count;
            var debugOutput = $"{name}\nValid Sub-assets:\n";

            // Collect valid, compiled sub-assets
            foreach (var asset in subassets)
            {
                if (asset == null || asset.GetType().BaseType != typeof(ScriptableRendererFeatureAssetLegacy)) continue;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long localId);
                loadedAssets.Add(localId, asset);
                debugOutput += $"-{asset.name}\n--localId={localId}\n";
            }

            // Collect assets that are connected to the list
            for (var i = 0; i < m_RendererFeatures?.Count; i++)
            {
                if (!m_RendererFeatures[i]) continue;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_RendererFeatures[i], out var guid, out long localId))
                {
                    linkedIds.Add(localId);
                }
            }

            var mapDebug = mapValid ? "Linking" : "Map missing, will attempt to re-map";
            debugOutput += $"Feature List Status({mapDebug}):\n";

            // Try fix missing references
            for (var i = 0; i < m_RendererFeatures?.Count; i++)
            {
                if (m_RendererFeatures[i] == null)
                {
                    if (mapValid && m_RendererFeatureMap[i] != 0)
                    {
                        var localId = m_RendererFeatureMap[i];
                        loadedAssets.TryGetValue(localId, out var asset);
                        m_RendererFeatures[i] = (ScriptableRendererFeatureAssetLegacy)asset;
                    }
                    else
                    {
                        m_RendererFeatures[i] = (ScriptableRendererFeatureAssetLegacy)GetUnusedAsset(ref linkedIds, ref loadedAssets);
                    }
                }

                debugOutput += m_RendererFeatures[i] != null ? $"-{i}:Linked\n" : $"-{i}:Missing\n";
            }

            UpdateMap();

            if (!m_RendererFeatures.Contains(null))
                return true;

            Debug.LogError($"{name} is missing RendererFeatures\nThis could be due to missing scripts or compile error.", this);
            return false;
        }



        private static object GetUnusedAsset(ref List<long> usedIds, ref Dictionary<long, object> assets)
        {
            foreach (var asset in assets)
            {
                var alreadyLinked = usedIds.Any(used => asset.Key == used);

                if (alreadyLinked)
                    continue;

                usedIds.Add(asset.Key);
                return asset.Value;
            }

            return null;
        }

        private void UpdateMap()
        {
            if (m_RendererFeatureMap.Count != m_RendererFeatures.Count)
            {
                m_RendererFeatureMap.Clear();
                m_RendererFeatureMap.AddRange(new long[m_RendererFeatures.Count]);
            }

            for (int i = 0; i < m_RendererFeatures.Count; i++)
            {
                if (m_RendererFeatures[i] == null) continue;
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_RendererFeatures[i], out var guid, out long localId)) continue;

                m_RendererFeatureMap[i] = localId;
            }
        }
#endif
    }
}
