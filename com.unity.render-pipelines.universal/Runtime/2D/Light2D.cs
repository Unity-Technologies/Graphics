using System;
using UnityEngine.Serialization;
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
    [AddComponentMenu("Rendering/2D/Light 2D (Experimental)")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/2DLightProperties.html")]
    public sealed partial class Light2D : MonoBehaviour
    {
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

#if USING_ANIMATION_MODULE
        [UnityEngine.Animations.NotKeyable]
#endif
        [SerializeField] LightType m_LightType = LightType.Parametric;
        [SerializeField, FormerlySerializedAs("m_LightOperationIndex")]
        int m_BlendStyleIndex = 0;

        [SerializeField] float m_FalloffIntensity = 0.5f;

        [ColorUsage(false)]
        [SerializeField] Color m_Color = Color.white;
        [SerializeField] float m_Intensity = 1;

        [SerializeField] float m_LightVolumeOpacity = 0.0f;
        [SerializeField] int[] m_ApplyToSortingLayers = new int[1];     // These are sorting layer IDs. If we need to update this at runtime make sure we add code to update global lights
        [SerializeField] Sprite m_LightCookieSprite = null;
        [SerializeField] bool m_UseNormalMap = false;

        [SerializeField] int m_LightOrder = 0;
        [SerializeField] bool m_AlphaBlendOnOverlap = false;

        [Range(0,1)]
        [SerializeField] float m_ShadowIntensity    = 0.0f;
        [Range(0,1)]
        [SerializeField] float m_ShadowVolumeIntensity = 0.0f;

        [SerializeField]
        Mesh m_Mesh;

        // Transients
        int m_PreviousLightCookieSprite;
        int m_LightMeshHash = 0;

        internal int lightMeshHash => m_LightMeshHash;

        internal int[] affectedSortingLayers => m_ApplyToSortingLayers;

        private int lightCookieSpriteInstanceID => m_LightCookieSprite?.GetInstanceID() ?? 0;

        private Bounds m_LocalBounds;
        internal BoundingSphere boundingSphere { get; private set; }

        internal Mesh lightMesh
        {
            get
            {
                if ( null == m_Mesh )
                    m_Mesh = new Mesh();
                return m_Mesh;
            }
        }

        internal bool hasCachedMesh => ( lightMesh.vertices.Length != 0 && lightMesh.triangles.Length != 0 );

        /// <summary>
        /// The lights current type
        /// </summary>
        public LightType lightType
        {
            get => m_LightType;
            set
            {
                if(m_LightType != value)
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
        /// Specifies the darkness of the shadow
        /// </summary>
        public float shadowVolumeIntensity { get => m_ShadowVolumeIntensity; set => m_ShadowVolumeIntensity = Mathf.Clamp01(value); }

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
        public float volumeOpacity => m_LightVolumeOpacity;
        public Sprite lightCookieSprite => m_LightCookieSprite;
        public float falloffIntensity => m_FalloffIntensity;
        public bool useNormalMap => m_UseNormalMap;
        public bool alphaBlendOnOverlap => m_AlphaBlendOnOverlap;
        public int lightOrder { get => m_LightOrder; set => m_LightOrder = value; }

        internal int GetTopMostLitLayer()
        {
            var largestIndex = Int32.MinValue;
            var largestLayer = 0;

            var layers = Light2DManager.GetCachedSortingLayer();
            for (var i = 0; i < m_ApplyToSortingLayers.Length; ++i)
            {
                for(var layer = layers.Length - 1; layer >= largestLayer; --layer)
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
            int hashCode = CalculateHash(this);
            if (forceUpdate && hashCode != m_LightMeshHash)
            {
				switch(m_LightType)
				{
                	case LightType.Freeform:
	                    m_LocalBounds = LightUtility.GenerateShapeMesh(lightMesh, color * intensity, m_ShapePath, m_ShapeLightFalloffSize, falloffIntensity, volumeOpacity);
    	                break;
        	        case LightType.Parametric:
            	        m_LocalBounds = LightUtility.GenerateParametricMesh(lightMesh, color * intensity, m_ShapeLightParametricRadius, m_ShapeLightFalloffSize, m_ShapeLightParametricAngleOffset, m_ShapeLightParametricSides, falloffIntensity, volumeOpacity);
                	    break;
                	case LightType.Sprite:
                    	m_LocalBounds = LightUtility.GenerateSpriteMesh(lightMesh, color * intensity, m_LightCookieSprite);
                    	break;
                	case LightType.Point:
	                    m_LocalBounds = LightUtility.GenerateParametricMesh(lightMesh, color * intensity,1.412135f, 0, 0, 4, falloffIntensity, volumeOpacity);
    	                break;
				}
            }
            m_LightMeshHash = hashCode;
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

            for(var i = 0; i < m_ApplyToSortingLayers.Length; i++)
                if (m_ApplyToSortingLayers[i] == layer)
                    return true;

            return false;
        }

        private void Awake()
        {
            UpdateMesh(!hasCachedMesh);
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

        internal static int CalculateHash(Light2D light)
        {
            var hashCode = 0;
            unchecked
            {
                // Spline.
                hashCode = (int) 2166136261 ^ LightUtility.GetShapePathHash(light.shapePath);
                hashCode = hashCode * 16777619 ^ light.m_FalloffIntensity.GetHashCode();
                hashCode = hashCode * 16777619 ^ light.m_LightVolumeOpacity.GetHashCode();
                hashCode = hashCode * 16777619 ^ light.m_ShapeLightFalloffSize.GetHashCode();
                hashCode = hashCode * 16777619 ^ light.m_ShapeLightParametricRadius.GetHashCode();
                hashCode = hashCode * 16777619 ^ light.m_ShapeLightParametricSides.GetHashCode();
                hashCode = hashCode * 16777619 ^ light.m_ShapeLightParametricAngleOffset.GetHashCode();
                hashCode = hashCode * 16777619 ^ light.m_Color.GetHashCode();
                hashCode = hashCode * 16777619 ^ light.m_LightType.GetHashCode();
                hashCode = hashCode * 16777619 ^ light.lightCookieSpriteInstanceID.GetHashCode();
                hashCode = hashCode * 16777619 ^ light.m_Intensity.GetHashCode();
            }
            return hashCode;
        }

        private void LateUpdate()
        {
            if (m_LightType == LightType.Global)
                return;

            UpdateMesh(true);
            UpdateBoundingSphere();
        }
    }
}
