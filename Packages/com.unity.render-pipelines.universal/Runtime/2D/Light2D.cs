using System;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.U2D;
using UnityEngine.Rendering.RenderGraphModule;
#if UNITY_EDITOR
using System.Linq;
#endif

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class <c>Light2D</c> is a 2D light which can be used with the 2D Renderer.
    /// </summary>
    ///
    [ExecuteAlways, DisallowMultipleComponent]
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.Universal", "Unity.RenderPipelines.Universal.Runtime")]
    [AddComponentMenu("Rendering/2D/Light 2D")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/2DLightProperties.html")]
    public sealed partial class Light2D : Light2DBase, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Deprecated Light types that are no supported. Please migrate to either Freeform or Point lights.
        /// </summary>
        public enum DeprecatedLightType
        {
            /// <summary>
            /// N-gon shaped lights.
            /// </summary>
            Parametric = 0,
        }

        /// <summary>
        /// An enumeration of the types of light
        /// </summary>
        public enum LightType
        {
            /// <summary>
            /// N-gon shaped lights. Deprecated.
            /// </summary>
            Parametric = 0,
            /// <summary>
            /// The shape of the light is based on a user defined closed shape with multiple points.
            /// </summary>
            Freeform = 1,
            /// <summary>
            /// The shape of the light is based on a Sprite.
            /// </summary>
            Sprite = 2,
            /// <summary>
            /// The shape of light is circular and can also be configured into a pizza shape.
            /// </summary>
            Point = 3,
            /// <summary>
            /// Shapeless light that affects the entire screen.
            /// </summary>
            Global = 4
        }

        /// <summary>
        /// The accuracy of how the normal map calculation.
        /// </summary>
        public enum NormalMapQuality
        {
            /// <summary>
            /// Normal map not used.
            /// </summary>
            Disabled = 2,
            /// <summary>
            /// Faster calculation with less accuracy suited for small shapes on screen.
            /// </summary>
            Fast = 0,
            /// <summary>
            /// Accurate calculation useful for better output on bigger shapes on screen.
            /// </summary>
            Accurate = 1
        }

        /// <summary>
        /// Determines how the final color is calculated when multiple lights overlap each other
        /// </summary>
        public enum OverlapOperation
        {
            /// <summary>
            /// Colors are added together
            /// </summary>
            Additive,
            /// <summary>
            /// Colors are blended using standard blending (alpha, 1-alpha)
            /// </summary>
            AlphaBlend
        }

        private enum ComponentVersions
        {
            Version_Unserialized = 0,
            Version_1 = 1,
            Version_2 = 2
        }

        const ComponentVersions k_CurrentComponentVersion = ComponentVersions.Version_2;
        [SerializeField] ComponentVersions m_ComponentVersion = ComponentVersions.Version_Unserialized;


#if USING_ANIMATION_MODULE
        [UnityEngine.Animations.NotKeyable]
#endif
        [SerializeField] LightType m_LightType = LightType.Point;
        [SerializeField, FormerlySerializedAs("m_LightOperationIndex")]
        int m_BlendStyleIndex = 0;

        [SerializeField] float m_FalloffIntensity = 0.5f;

        [ColorUsage(true)]
        [SerializeField] Color m_Color = Color.white;
        [SerializeField] float m_Intensity = 1;

        [FormerlySerializedAs("m_LightVolumeOpacity")]
        [SerializeField] float m_LightVolumeIntensity = 1.0f;

        [FormerlySerializedAs("m_LightVolumeIntensityEnabled")]
        [SerializeField] bool m_LightVolumeEnabled = false;
        [SerializeField] int[] m_ApplyToSortingLayers;  // These are sorting layer IDs. If we need to update this at runtime make sure we add code to update global lights

        [Reload("Textures/2D/Sparkle.png")]
        [SerializeField] Sprite m_LightCookieSprite;

        [FormerlySerializedAs("m_LightCookieSprite")]
        [SerializeField] Sprite m_DeprecatedPointLightCookieSprite;

        [SerializeField] int m_LightOrder = 0;

        [SerializeField] bool m_AlphaBlendOnOverlap = false; // This is now deprecated. Keep it here for backwards compatibility.

        [SerializeField] OverlapOperation m_OverlapOperation = OverlapOperation.Additive;

        [FormerlySerializedAs("m_PointLightDistance")]
        [SerializeField] float m_NormalMapDistance = 3.0f;

#if USING_ANIMATION_MODULE
        [UnityEngine.Animations.NotKeyable]
#endif
        [FormerlySerializedAs("m_PointLightQuality")]
        [SerializeField] NormalMapQuality m_NormalMapQuality = NormalMapQuality.Disabled;

        [SerializeField] bool m_UseNormalMap = false;   // This is now deprecated. Keep it here for backwards compatibility.

        [FormerlySerializedAs("m_ShadowIntensityEnabled")]
        [SerializeField] bool m_ShadowsEnabled = true;

        [Range(0, 1)]
        [SerializeField] float m_ShadowIntensity = 0.75f;

        [Range(0, 1)]
        [SerializeField] float m_ShadowSoftness = 0.3f;

        [Range(0, 1)]
        [SerializeField] float m_ShadowSoftnessFalloffIntensity = 0.5f;

        [SerializeField] bool m_ShadowVolumeIntensityEnabled = false;
        [Range(0, 1)]
        [SerializeField] float m_ShadowVolumeIntensity = 0.75f;

        Mesh m_Mesh;

        [NonSerialized]
        private LightUtility.LightMeshVertex[] m_Vertices = new LightUtility.LightMeshVertex[1];

        [NonSerialized]
        private ushort[] m_Triangles = new ushort[1];

        internal LightUtility.LightMeshVertex[] vertices { get { return m_Vertices; } set { m_Vertices = value; } }

        internal ushort[] indices { get { return m_Triangles; } set { m_Triangles = value; } }

        // Transients
        int m_PreviousLightCookieSprite;
        internal Vector3 m_CachedPosition;

        // We use Blue Channel of LightMesh's vertex color to indicate Slot Index.
        int m_BatchSlotIndex = 0;
        internal int batchSlotIndex { get { return m_BatchSlotIndex; } set {  m_BatchSlotIndex = value; } }
        internal int[] affectedSortingLayers => m_ApplyToSortingLayers;

        private int lightCookieSpriteInstanceID => lightCookieSprite?.GetInstanceID() ?? 0;

        internal bool useCookieSprite => (lightType == LightType.Point || lightType == LightType.Sprite) && (lightCookieSprite != null && lightCookieSprite.texture != null);

        internal RTHandle m_CookieSpriteTexture = null;
        internal TextureHandle m_CookieSpriteTextureHandle;

        [SerializeField]
        Bounds m_LocalBounds;
        internal BoundingSphere boundingSphere { get; private set; }

        internal Mesh lightMesh
        {
            get
            {
                if (null == m_Mesh)
                    m_Mesh = new Mesh();
                return m_Mesh;
            }
        }

        internal bool hasCachedMesh => (vertices.Length > 1 && indices.Length > 1);

        internal bool forceUpdate = false;

        /// <summary>
        /// The light's current type
        /// </summary>
        public LightType lightType
        {
            get => m_LightType;
            set
            {
                if (m_LightType != value)
                    UpdateMesh();

                m_LightType = value;
                Light2DManager.ErrorIfDuplicateGlobalLight(this);
            }
        }

        /// <summary>
        /// The lights current operation index
        /// </summary>
        public int blendStyleIndex { get => m_BlendStyleIndex; set => m_BlendStyleIndex = value; }

        /// <summary>
        /// Specifies the darkness of the shadow
        /// </summary>
        public float shadowIntensity { get => m_ShadowIntensity; set => m_ShadowIntensity = Mathf.Clamp01(value); }


        /// <summary>
        /// Specifies the softness of the soft shadow
        /// </summary>
        public float shadowSoftness { get => m_ShadowSoftness; set => m_ShadowSoftness = value; }


        /// <summary>
        /// Specifies that the shadows are enabled
        /// </summary>
        public bool shadowsEnabled { get => m_ShadowsEnabled; set => m_ShadowsEnabled = value; }

        /// <summary>
        /// Specifies the darkness of the shadow
        /// </summary>
        public float shadowVolumeIntensity { get => m_ShadowVolumeIntensity; set => m_ShadowVolumeIntensity = Mathf.Clamp01(value); }

        /// <summary>
        /// Specifies that the volumetric shadows are enabled
        /// </summary>
        public bool volumetricShadowsEnabled { get => m_ShadowVolumeIntensityEnabled; set => m_ShadowVolumeIntensityEnabled = value; }

        /// <summary>
        /// The lights current color
        /// </summary>
        public Color color { get => m_Color; set => m_Color = value; }

        /// <summary>
        /// The lights current intensity
        /// </summary>
        public float intensity { get => m_Intensity; set => m_Intensity = value; }

        /// <summary>
        /// The lights current intensity
        /// </summary>
        ///
        [Obsolete]
        public float volumeOpacity => m_LightVolumeIntensity;

        /// <summary>
        /// Controls the visibility of the light's volume
        /// </summary>
        public float volumeIntensity { get => m_LightVolumeIntensity; set => m_LightVolumeIntensity = value; }

        /// <summary>
        /// Enables or disables the light's volume
        /// </summary>
        ///
        [Obsolete]
        public bool volumeIntensityEnabled { get => m_LightVolumeEnabled; set => m_LightVolumeEnabled = value; }


        /// <summary>
        /// Enables or disables the light's volume
        /// </summary>
        ///
        public bool volumetricEnabled { get => m_LightVolumeEnabled; set => m_LightVolumeEnabled = value; }

        /// <summary>
        /// The Sprite that's used by the Sprite Light type to control the shape light
        /// </summary>
        public Sprite lightCookieSprite { get { return m_LightType != LightType.Point ? m_LightCookieSprite : m_DeprecatedPointLightCookieSprite; } set => m_LightCookieSprite = value; }

        /// <summary>
        /// Controls the brightness and distance of the fall off (edge) of the light
        /// </summary>
        public float falloffIntensity { get => m_FalloffIntensity; set => m_FalloffIntensity = Mathf.Clamp(value, 0, 1); }

        /// <summary>
        /// Controls the falloff for soft shadows
        /// </summary>
        public float shadowSoftnessFalloffIntensity { get => m_ShadowSoftnessFalloffIntensity; set => m_ShadowSoftnessFalloffIntensity = Mathf.Clamp(value, 0, 1); }

        /// <summary>
        /// Checks if the alpha overlap operation is alpha blend.
        /// This is obsolete.
        /// </summary>
        [Obsolete]
        public bool alphaBlendOnOverlap { get { return m_OverlapOperation == OverlapOperation.AlphaBlend; } }

        /// <summary>
        /// Controls the overlap operation mode.
        /// </summary>
        public OverlapOperation overlapOperation { get => m_OverlapOperation; set => m_OverlapOperation = value; }

        /// <summary>
        /// Gets or sets the light order. The lightOrder determines the order in which the lights are rendered onto the light textures.
        /// </summary>
        public int lightOrder { get => m_LightOrder; set => m_LightOrder = value; }

        /// <summary>
        /// The simulated z distance of the light from the surface used in normal map calculation.
        /// </summary>
        public float normalMapDistance => m_NormalMapDistance;

        /// <summary>
        /// Returns the calculation quality for the normal map rendering. Please refer to NormalMapQuality.
        /// </summary>
        public NormalMapQuality normalMapQuality => m_NormalMapQuality;

        /// <summary>
        /// Returns if volumetric shadows should be rendered.
        /// </summary>
        public bool renderVolumetricShadows => volumetricShadowsEnabled && shadowVolumeIntensity > 0;

        internal void MarkForUpdate()
        {
            forceUpdate = true;
        }

        internal void CacheValues()
        {
            m_CachedPosition = transform.position;
        }

        internal int GetTopMostLitLayer()
        {
            var largestIndex = Int32.MinValue;
            var largestLayer = 0;

            var layers = Light2DManager.GetCachedSortingLayer();
            for (var i = 0; i < m_ApplyToSortingLayers.Length; ++i)
            {
                for (var layer = layers.Length - 1; layer >= largestLayer; --layer)
                {
                    if (layers[layer].id == m_ApplyToSortingLayers[i])
                    {
                        largestIndex = layers[layer].value;
                        largestLayer = layer;
                    }
                }
            }

            return largestIndex;
        }

        internal Bounds UpdateSpriteMesh()
        {
            if (m_LightCookieSprite == null && (m_Vertices.Length != 1 || m_Triangles.Length != 1))
            {
                m_Vertices = new LightUtility.LightMeshVertex[1];
                m_Triangles = new ushort[1];
            }
            return LightUtility.GenerateSpriteMesh(this, m_LightCookieSprite, LightBatch.GetBatchColor());
        }

        internal void UpdateBatchSlotIndex()
        {
            if (lightMesh && lightMesh.colors != null && lightMesh.colors.Length != 0)
                m_BatchSlotIndex = LightBatch.GetBatchSlotIndex(lightMesh.colors[0].b);
        }

        internal bool NeedsColorIndexBaking()
        {
            if (lightMesh && LightBatch.isBatchingSupported)
            {
                if (lightMesh.colors.Length != 0)
                    return lightMesh.colors[0].b == 0;
            }
            return false;
        }
        
        internal void UpdateCookieSpriteTexture()
        {
            m_CookieSpriteTexture?.Release();

            if (useCookieSprite)
                m_CookieSpriteTexture = RTHandles.Alloc(lightCookieSprite.texture);

        }

        internal void UpdateMesh(bool forceUpdate = false)
        {
            var shapePathHash = LightUtility.GetShapePathHash(shapePath);
            var fallOffSizeChanged = LightUtility.CheckForChange(m_ShapeLightFalloffSize, ref m_PreviousShapeLightFalloffSize);
            var parametricRadiusChanged = LightUtility.CheckForChange(m_ShapeLightParametricRadius, ref m_PreviousShapeLightParametricRadius);
            var parametricSidesChanged = LightUtility.CheckForChange(m_ShapeLightParametricSides, ref m_PreviousShapeLightParametricSides);
            var parametricAngleOffsetChanged = LightUtility.CheckForChange(m_ShapeLightParametricAngleOffset, ref m_PreviousShapeLightParametricAngleOffset);
            var spriteInstanceChanged = LightUtility.CheckForChange(lightCookieSpriteInstanceID, ref m_PreviousLightCookieSprite);
            var shapePathHashChanged = LightUtility.CheckForChange(shapePathHash, ref m_PreviousShapePathHash);
            var lightTypeChanged = LightUtility.CheckForChange(m_LightType, ref m_PreviousLightType);
            var hashChanged = fallOffSizeChanged || parametricRadiusChanged || parametricSidesChanged ||
                parametricAngleOffsetChanged || spriteInstanceChanged || shapePathHashChanged || lightTypeChanged || NeedsColorIndexBaking();

            // Mesh Rebuilding
            if (hashChanged || forceUpdate)
            {
                var batchChannelColor = LightBatch.GetBatchColor();

                switch (m_LightType)
                {
                    case LightType.Freeform:
                        m_LocalBounds = LightUtility.GenerateShapeMesh(this, m_ShapePath, m_ShapeLightFalloffSize, batchChannelColor);
                        break;
                    case LightType.Parametric:
                        m_LocalBounds = LightUtility.GenerateParametricMesh(this, m_ShapeLightParametricRadius, m_ShapeLightFalloffSize, m_ShapeLightParametricAngleOffset, m_ShapeLightParametricSides, batchChannelColor);
                        break;
                    case LightType.Sprite:
                        m_LocalBounds = UpdateSpriteMesh();
                        break;
                    case LightType.Point:
                        m_LocalBounds = LightUtility.GenerateParametricMesh(this, 1.412135f, 0, 0, 4, batchChannelColor);
                        break;
                }

                UpdateCookieSpriteTexture();
                UpdateBatchSlotIndex();
            }
        }

        internal void UpdateBoundingSphere()
        {
            if (isPointLight)
            {
                boundingSphere = new BoundingSphere(transform.position, m_PointLightOuterRadius);
                return;
            }

            var maxBound = transform.TransformPoint(Vector3.Max(m_LocalBounds.max, m_LocalBounds.max + (Vector3)m_ShapeLightFalloffOffset));
            var minBound = transform.TransformPoint(Vector3.Min(m_LocalBounds.min, m_LocalBounds.min + (Vector3)m_ShapeLightFalloffOffset));
            var center = 0.5f * (maxBound + minBound);
            var radius = Vector3.Magnitude(maxBound - center);

            boundingSphere = new BoundingSphere(center, radius);
        }

        internal bool IsLitLayer(int layer)
        {
            if (m_ApplyToSortingLayers == null)
                return false;

            for (var i = 0; i < m_ApplyToSortingLayers.Length; i++)
                if (m_ApplyToSortingLayers[i] == layer)
                    return true;

            return false;
        }

        internal Matrix4x4 GetMatrix()
        {
            var matrix = transform.localToWorldMatrix;
            if (lightType == Light2D.LightType.Point)
            {
                var scale = new Vector3(pointLightOuterRadius, pointLightOuterRadius, pointLightOuterRadius);
                matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
            }
            return matrix;
        }

        private void Awake()
        {
            // Default target sorting layers to "All"
            if (m_ApplyToSortingLayers == null)
            {
                m_ApplyToSortingLayers = new int[SortingLayer.layers.Length];
                for (int i = 0; i < m_ApplyToSortingLayers.Length; ++i)
                    m_ApplyToSortingLayers[i] = SortingLayer.layers[i].id;
            }
        }

        void OnEnable()
        {
            m_PreviousLightCookieSprite = lightCookieSpriteInstanceID;
            Light2DManager.RegisterLight(this);
            UpdateCookieSpriteTexture();

#if UNITY_EDITOR
            SortingLayer.onLayerAdded += OnSortingLayerAdded;
            SortingLayer.onLayerRemoved += OnSortingLayerRemoved;
#endif
        }

        private void OnDisable()
        {
            Light2DManager.DeregisterLight(this);
            m_CookieSpriteTexture?.Release();

#if UNITY_EDITOR
            SortingLayer.onLayerAdded -= OnSortingLayerAdded;
            SortingLayer.onLayerRemoved -= OnSortingLayerRemoved;
#endif
        }

        private void LateUpdate()
        {
            if (m_LightType == LightType.Global)
                return;

            UpdateMesh(forceUpdate);
            UpdateBoundingSphere();

            forceUpdate = false;
        }

#if UNITY_EDITOR
        private void OnSortingLayerAdded(SortingLayer layer)
        {
            m_ApplyToSortingLayers = m_ApplyToSortingLayers.Append(layer.id).ToArray();
        }

        private void OnSortingLayerRemoved(SortingLayer layer)
        {
            m_ApplyToSortingLayers = m_ApplyToSortingLayers.Where(x => x != layer.id && SortingLayer.IsValid(x)).ToArray();
        }
#endif

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        public void OnBeforeSerialize()
        {
            m_ComponentVersion = k_CurrentComponentVersion;
        }

        /// <summary>
        /// OnAfterSerialize implementation.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // Upgrade from no serialized version
            if (m_ComponentVersion == ComponentVersions.Version_Unserialized)
            {
                m_ShadowVolumeIntensityEnabled = m_ShadowVolumeIntensity > 0;
                m_ShadowsEnabled = m_ShadowIntensity > 0;
                m_LightVolumeEnabled = m_LightVolumeIntensity > 0;
                m_NormalMapQuality = !m_UseNormalMap ? NormalMapQuality.Disabled : m_NormalMapQuality;
                m_OverlapOperation = m_AlphaBlendOnOverlap ? OverlapOperation.AlphaBlend : m_OverlapOperation;
                m_ComponentVersion = ComponentVersions.Version_1;
            }

            if(m_ComponentVersion < ComponentVersions.Version_2)
            {
                m_ShadowSoftness = 0;
            }
        }
    }
}
