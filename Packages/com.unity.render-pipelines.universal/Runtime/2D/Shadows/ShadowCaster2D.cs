using System;
using UnityEngine.Scripting.APIUpdating;

#if UNITY_EDITOR
using System.Linq;
#endif

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class <c>ShadowCaster2D</c> contains properties used for shadow casting
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("Rendering/2D/Shadow Caster 2D")]
    [MovedFrom("UnityEngine.Experimental.Rendering.Universal")]
    public class ShadowCaster2D : ShadowCasterGroup2D, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Enum used for different component versions.
        /// </summary>
        public enum ComponentVersions
        {
            /// <summary>
            /// Used for unserialized version.
            /// </summary>
            Version_Unserialized = 0,

            /// <summary>
            /// Used for version 1.
            /// </summary>
            Version_1 = 1
        }
        const ComponentVersions k_CurrentComponentVersion = ComponentVersions.Version_1;
        [SerializeField] ComponentVersions m_ComponentVersion = ComponentVersions.Version_Unserialized;

        [SerializeField] bool m_HasRenderer = false;
        [SerializeField] bool m_UseRendererSilhouette = true;
        [SerializeField] bool m_CastsShadows = true;
        [SerializeField] bool m_SelfShadows = false;
        [SerializeField] int[] m_ApplyToSortingLayers = null;
        [SerializeField] Vector3[] m_ShapePath = null;
        [SerializeField] int m_ShapePathHash = 0;
        [SerializeField] Mesh m_Mesh;
        [SerializeField] int m_InstanceId;

        internal ShadowCasterGroup2D m_ShadowCasterGroup = null;
        internal ShadowCasterGroup2D m_PreviousShadowCasterGroup = null;

        [SerializeField]
        internal Bounds m_LocalBounds;
        internal BoundingSphere m_BoundingSphere;

        /// <summary>
        /// The mesh to draw with.
        /// </summary>
        public Mesh mesh => m_Mesh;

        /// <summary>
        /// The path for the shape.
        /// </summary>
        public Vector3[] shapePath => m_ShapePath;
        internal int shapePathHash { get { return m_ShapePathHash; } set { m_ShapePathHash = value; } }

        int m_PreviousShadowGroup = 0;
        bool m_PreviousCastsShadows = true;
        int m_PreviousPathHash = 0;

        internal Vector3 m_CachedPosition;
        internal Vector3 m_CachedLossyScale;
        internal Quaternion m_CachedRotation;
        internal Matrix4x4 m_CachedShadowMatrix;
        internal Matrix4x4 m_CachedInverseShadowMatrix;
        internal Matrix4x4 m_CachedLocalToWorldMatrix;

        internal override void CacheValues()
        {
            m_CachedPosition = transform.position;
            m_CachedLossyScale = transform.lossyScale;
            m_CachedRotation = transform.rotation;

            m_CachedShadowMatrix = Matrix4x4.TRS(m_CachedPosition, m_CachedRotation, Vector3.one);
            m_CachedInverseShadowMatrix = m_CachedShadowMatrix.inverse;

            m_CachedLocalToWorldMatrix = transform.localToWorldMatrix;
        }

        /// <summary>
        /// If selfShadows is true, useRendererSilhoutte specifies that the renderer's sihouette should be considered part of the shadow. If selfShadows is false, useRendererSilhoutte specifies that the renderer's sihouette should be excluded from the shadow
        /// </summary>
        public bool useRendererSilhouette
        {
            set { m_UseRendererSilhouette = value; }
            get { return m_UseRendererSilhouette && m_HasRenderer; }
        }

        /// <summary>
        /// If true, the shadow casting shape is included as part of the shadow. If false, the shadow casting shape is excluded from the shadow.
        /// </summary>
        public bool selfShadows
        {
            set { m_SelfShadows = value; }
            get { return m_SelfShadows; }
        }

        /// <summary>
        /// Specifies if shadows will be cast.
        /// </summary>
        public bool castsShadows
        {
            set { m_CastsShadows = value; }
            get { return m_CastsShadows; }
        }

        static int[] SetDefaultSortingLayers()
        {
            int layerCount = SortingLayer.layers.Length;
            int[] allLayers = new int[layerCount];

            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                allLayers[layerIndex] = SortingLayer.layers[layerIndex].id;
            }

            return allLayers;
        }

        internal bool IsLit(Light2D light)
        {
            // Oddly adding and subtracting vectors is expensive here because of the new structures created...
            Vector3 deltaPos;
            deltaPos.x = light.m_CachedPosition.x - m_BoundingSphere.position.x;
            deltaPos.y = light.m_CachedPosition.y - m_BoundingSphere.position.y;
            deltaPos.z = light.m_CachedPosition.z - m_BoundingSphere.position.z;

            float distanceSq = Vector3.SqrMagnitude(deltaPos);

            float radiiLength = light.boundingSphere.radius + m_BoundingSphere.radius;
            return distanceSq <= (radiiLength * radiiLength);
        }

        internal bool IsShadowedLayer(int layer)
        {
            return m_ApplyToSortingLayers != null ? Array.IndexOf(m_ApplyToSortingLayers, layer) >= 0 : false;
        }

        private void Awake()
        {
            if (m_ApplyToSortingLayers == null)
                m_ApplyToSortingLayers = SetDefaultSortingLayers();

            Bounds bounds = new Bounds(transform.position, Vector3.one);

            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                bounds = renderer.bounds;
            }
#if USING_PHYSICS2D_MODULE
            else
            {
                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null)
                    bounds = collider.bounds;
            }
#endif
            Vector3 inverseScale = Vector3.zero;
            Vector3 relOffset = transform.position;

            if (transform.lossyScale.x != 0 && transform.lossyScale.y != 0)
            {
                inverseScale = new Vector3(1 / transform.lossyScale.x, 1 / transform.lossyScale.y);
                relOffset = new Vector3(inverseScale.x * -transform.position.x, inverseScale.y * -transform.position.y);
            }

            if (m_ShapePath == null || m_ShapePath.Length == 0)
            {
                m_ShapePath = new Vector3[]
                {
                    relOffset + new Vector3(inverseScale.x * bounds.min.x, inverseScale.y * bounds.min.y),
                    relOffset + new Vector3(inverseScale.x * bounds.min.x, inverseScale.y * bounds.max.y),
                    relOffset + new Vector3(inverseScale.x * bounds.max.x, inverseScale.y * bounds.max.y),
                    relOffset + new Vector3(inverseScale.x * bounds.max.x, inverseScale.y * bounds.min.y),
                };
            }
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        protected void OnEnable()
        {
            if (m_Mesh == null || m_InstanceId != GetInstanceID())
            {
                m_Mesh = new Mesh();
                m_LocalBounds = ShadowUtility.GenerateShadowMesh(m_Mesh, m_ShapePath);
                m_InstanceId = GetInstanceID();
            }

            m_ShadowCasterGroup = null;

#if UNITY_EDITOR
            SortingLayer.onLayerAdded += OnSortingLayerAdded;
            SortingLayer.onLayerRemoved += OnSortingLayerRemoved;
#endif
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        protected void OnDisable()
        {
            ShadowCasterGroup2DManager.RemoveFromShadowCasterGroup(this, m_ShadowCasterGroup);

#if UNITY_EDITOR
            SortingLayer.onLayerAdded -= OnSortingLayerAdded;
            SortingLayer.onLayerRemoved -= OnSortingLayerRemoved;
#endif
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        public void Update()
        {
            Renderer renderer;
            m_HasRenderer = TryGetComponent<Renderer>(out renderer);

            bool rebuildMesh = LightUtility.CheckForChange(m_ShapePathHash, ref m_PreviousPathHash);
            if (rebuildMesh)
            {
                m_LocalBounds = ShadowUtility.GenerateShadowMesh(m_Mesh, m_ShapePath);
            }

            m_PreviousShadowCasterGroup = m_ShadowCasterGroup;
            bool addedToNewGroup = ShadowCasterGroup2DManager.AddToShadowCasterGroup(this, ref m_ShadowCasterGroup);
            if (addedToNewGroup && m_ShadowCasterGroup != null)
            {
                if (m_PreviousShadowCasterGroup == this)
                    ShadowCasterGroup2DManager.RemoveGroup(this);

                ShadowCasterGroup2DManager.RemoveFromShadowCasterGroup(this, m_PreviousShadowCasterGroup);
                if (m_ShadowCasterGroup == this)
                    ShadowCasterGroup2DManager.AddGroup(this);
            }

            if (LightUtility.CheckForChange(m_ShadowGroup, ref m_PreviousShadowGroup))
            {
                ShadowCasterGroup2DManager.RemoveGroup(this);
                ShadowCasterGroup2DManager.AddGroup(this);
            }

            if (LightUtility.CheckForChange(m_CastsShadows, ref m_PreviousCastsShadows))
            {
                if (m_CastsShadows)
                    ShadowCasterGroup2DManager.AddGroup(this);
                else
                    ShadowCasterGroup2DManager.RemoveGroup(this);
            }

            UpdateBoundingSphere();
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

        /// <inheritdoc/>
        public void OnBeforeSerialize()
        {
            m_ComponentVersion = k_CurrentComponentVersion;
        }

        /// <inheritdoc/>
        public void OnAfterDeserialize()
        {
            // Upgrade from no serialized version
            if (m_ComponentVersion == ComponentVersions.Version_Unserialized)
            {
                m_LocalBounds = ShadowUtility.CalculateLocalBounds(m_ShapePath);
                m_ComponentVersion = ComponentVersions.Version_1;
            }
        }

        private void UpdateBoundingSphere()
        {
            var maxBound = transform.TransformPoint(m_LocalBounds.max);
            var minBound = transform.TransformPoint(m_LocalBounds.min);
            var center = 0.5f * (maxBound + minBound);
            var radius = Vector3.Magnitude(maxBound - center);

            m_BoundingSphere = new BoundingSphere(center, radius);
        }

#if UNITY_EDITOR
        void Reset()
        {
            Awake();
            OnEnable();
        }

#endif
    }
}
