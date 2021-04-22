using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
#endif

namespace UnityEngine.Experimental.Rendering.Universal
{
    /// <summary>
    /// Class <c>Light2D</c> is a 2D light which can be used with the 2D Renderer.
    /// </summary>
    ///
    [ExecuteAlways, DisallowMultipleComponent]
    [AddComponentMenu("Rendering/2D/Light 2D")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/2DLightProperties.html")]
    public sealed partial class Light2D : MonoBehaviour, ISerializationCallbackReceiver
    {
        public enum DeprecatedLightType
        {
            Parametric = 0,
        }

        /// <summary>
        /// an enumeration of the types of light
        /// </summary>
        public enum LightType
        {
            Parametric = 0,
            Freeform = 1,
            Sprite = 2,
            Point = 3,
            Global = 4
        }

        public enum NormalMapQuality
        {
            Disabled = 2,
            Fast = 0,
            Accurate = 1
        }

        public enum OverlapOperation
        {
            Additive,
            AlphaBlend
        }


        public enum ComponentVersions
        {
            Version_Unserialized = 0,
            Version_1 = 1
        }

        const ComponentVersions k_CurrentComponentVersion = ComponentVersions.Version_1;
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
        [SerializeField] bool m_LightVolumeIntensityEnabled = false;
        [SerializeField] int[] m_ApplyToSortingLayers = new int[1];     // These are sorting layer IDs. If we need to update this at runtime make sure we add code to update global lights

        [Reload("Textures/2D/Sparkle.png")]
        [SerializeField] Sprite m_LightCookieSprite;

        [FormerlySerializedAs("m_LightCookieSprite")]
        [SerializeField] Sprite m_DeprecatedPointLightCookieSprite;

        [SerializeField] int m_LightOrder = 0;

        [SerializeField] OverlapOperation m_OverlapOperation = OverlapOperation.Additive;

        [FormerlySerializedAs("m_PointLightDistance")]
        [SerializeField] float m_NormalMapDistance = 3.0f;

#if USING_ANIMATION_MODULE
        [UnityEngine.Animations.NotKeyable]
#endif
        [FormerlySerializedAs("m_PointLightQuality")]
        [SerializeField] NormalMapQuality m_NormalMapQuality = NormalMapQuality.Disabled;

        [SerializeField] bool m_UseNormalMap = false;   // This is now deprecated. Keep it here for backwards compatibility.

        [SerializeField] bool m_ShadowIntensityEnabled = false;
        [Range(0, 1)]
        [SerializeField] float m_ShadowIntensity = 0.75f;

        [SerializeField] bool m_ShadowVolumeIntensityEnabled = false;
        [Range(0, 1)]
        [SerializeField] float m_ShadowVolumeIntensity = 0.75f;

        Mesh m_Mesh;

        [SerializeField]
        private LightUtility.LightMeshVertex[] m_Vertices = new LightUtility.LightMeshVertex[1];

        [SerializeField]
        private ushort[] m_Triangles = new ushort[1];

        internal LightUtility.LightMeshVertex[] vertices { get { return m_Vertices; } set { m_Vertices = value; } }

        internal ushort[] indices { get { return m_Triangles; } set { m_Triangles = value; } }

        // Transients
        int m_PreviousLightCookieSprite;
        internal int[] affectedSortingLayers => m_ApplyToSortingLayers;

        private int lightCookieSpriteInstanceID => m_LightCookieSprite?.GetInstanceID() ?? 0;

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

        /// <summary>
        /// The lights current type
        /// </summary>
        public LightType lightType
        {
            get => m_LightType;
            set
            {
                if (m_LightType != value)
                    UpdateMesh(true);

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
        /// Specifies that the shadows are enabled
        /// </summary>
        public bool shadowsEnabled { get => m_ShadowIntensityEnabled; set => m_ShadowIntensityEnabled = value; }

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
        public float volumeIntensity => m_LightVolumeIntensity;

        public bool volumeIntensityEnabled { get => m_LightVolumeIntensityEnabled; set => m_LightVolumeIntensityEnabled = value; }
        public Sprite lightCookieSprite { get { return m_LightType != LightType.Point ? m_LightCookieSprite : m_DeprecatedPointLightCookieSprite; } }
        public float falloffIntensity => m_FalloffIntensity;

        [Obsolete]
        public bool alphaBlendOnOverlap { get { return m_OverlapOperation == OverlapOperation.AlphaBlend; }}
        public OverlapOperation overlapOperation => m_OverlapOperation;

        public int lightOrder { get => m_LightOrder; set => m_LightOrder = value; }

        public float normalMapDistance => m_NormalMapDistance;
        public NormalMapQuality normalMapQuality => m_NormalMapQuality;


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

        internal void UpdateMesh(bool forceUpdate)
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
                parametricAngleOffsetChanged || spriteInstanceChanged || shapePathHashChanged || lightTypeChanged;
            // Mesh Rebuilding
            if (hashChanged && forceUpdate)
            {
                switch (m_LightType)
                {
                    case LightType.Freeform:
                        m_LocalBounds = LightUtility.GenerateShapeMesh(this, m_ShapePath, m_ShapeLightFalloffSize);
                        break;
                    case LightType.Parametric:
                        m_LocalBounds = LightUtility.GenerateParametricMesh(this, m_ShapeLightParametricRadius, m_ShapeLightFalloffSize, m_ShapeLightParametricAngleOffset, m_ShapeLightParametricSides);
                        break;
                    case LightType.Sprite:
                        m_LocalBounds = LightUtility.GenerateSpriteMesh(this, m_LightCookieSprite);
                        break;
                    case LightType.Point:
                        m_LocalBounds = LightUtility.GenerateParametricMesh(this, 1.412135f, 0, 0, 4);
                        break;
                }
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

        private void Awake()
        {
            if (!m_UseNormalMap && m_NormalMapQuality != NormalMapQuality.Disabled)
                m_NormalMapQuality = NormalMapQuality.Disabled;

            UpdateMesh(!hasCachedMesh);
            if (hasCachedMesh)
            {
                lightMesh.SetVertexBufferParams(vertices.Length, LightUtility.LightMeshVertex.VertexLayout);
                lightMesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);
                lightMesh.SetIndices(indices, MeshTopology.Triangles, 0, false);
            }
        }

        void OnEnable()
        {
            m_PreviousLightCookieSprite = lightCookieSpriteInstanceID;
            Light2DManager.RegisterLight(this);
        }

        private void OnDisable()
        {
            Light2DManager.DeregisterLight(this);
        }

        private void LateUpdate()
        {
            if (m_LightType == LightType.Global)
                return;

            UpdateMesh(true);
            UpdateBoundingSphere();
        }

        public void OnBeforeSerialize()
        {
            m_ComponentVersion = k_CurrentComponentVersion;
        }

        public void OnAfterDeserialize()
        {
            // Upgrade from no serialized version
            if (m_ComponentVersion == ComponentVersions.Version_Unserialized)
            {
                m_ShadowVolumeIntensityEnabled = m_ShadowVolumeIntensity > 0;
                m_ShadowIntensityEnabled = m_ShadowIntensity > 0;
                m_LightVolumeIntensityEnabled = m_LightVolumeIntensity > 0;

                m_ComponentVersion = ComponentVersions.Version_1;
            }
        }
    }
}
