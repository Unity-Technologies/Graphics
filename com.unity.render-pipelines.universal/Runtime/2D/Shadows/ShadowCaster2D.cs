using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.U2D;
using Unity.Collections;


#if UNITY_EDITOR
using UnityEditor;
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
        public enum ComponentVersions
        {
            Version_Unserialized = 0,
            Version_1 = 1
        }
        const ComponentVersions k_CurrentComponentVersion = ComponentVersions.Version_1;
        [SerializeField] ComponentVersions m_ComponentVersion = ComponentVersions.Version_Unserialized;

        public enum ShadowCastingSources
        {
            None,
            ShapeEditor,
            ShapeProvider
        }

        public enum ShadowCastingOptions
        {
            Self,
            Cast,
            Both
        }

        public enum EdgeProcessing
        {
            None = ShadowMesh2D.EdgeProcessing.None,
            Clipping = ShadowMesh2D.EdgeProcessing.Clipping,
        }

        [SerializeField] bool m_HasRenderer = false;
        [SerializeField] bool m_UseRendererSilhouette = true;
        [SerializeField] bool m_CastsShadows = true;
        [SerializeField] bool m_SelfShadows = false;
        [SerializeField] int[] m_ApplyToSortingLayers = null;
        [SerializeField] Vector3[] m_ShapePath = null;
        [SerializeField] int  m_ShapePathHash = 0;
        
        [SerializeField] int m_InstanceId;
        [SerializeField] Component m_ShadowShapeProvider;
        [SerializeField] ShadowCastingSources m_ShadowCastingSource = (ShadowCastingSources)(-1);

        [SerializeField] internal ShadowMesh2D   m_ShadowMesh;
        [SerializeField] ShadowCastingOptions    m_CastingOption = ShadowCastingOptions.Cast;

        internal ShadowCasterGroup2D  m_ShadowCasterGroup = null;
        internal ShadowCasterGroup2D  m_PreviousShadowCasterGroup = null;
        internal int                  m_PreviousShadowCastingSource;
        internal Component            m_PreviousShadowShapeProvider = null;
        internal float                m_PreviousContractEdge = 0;
        internal int                  m_PreviousEdgeProcessing;

        public EdgeProcessing edgeProcessing
        {
            get { return (EdgeProcessing)m_ShadowMesh.edgeProcessing; }
            set { m_ShadowMesh.edgeProcessing = (ShadowMesh2D.EdgeProcessing)value; }
        }

        public Mesh mesh => m_ShadowMesh.mesh;
        public BoundingSphere boundingSphere => m_ShadowMesh.boundingSphere;

        public float contractEdge
        {
            get { return m_ShadowMesh.contractEdge; } set { m_ShadowMesh.contractEdge = value; }
        }

        public Vector3[] shapePath => m_ShapePath;
        internal int shapePathHash { get { return m_ShapePathHash; } set { m_ShapePathHash = value; } }

        public ShadowCastingSources shadowCastingSource { get { return m_ShadowCastingSource; } set { m_ShadowCastingSource = value; } }

        internal Component shadowShape2DProvider { get { return m_ShadowShapeProvider; } set { m_ShadowShapeProvider = value; } }

        int m_PreviousShadowGroup = 0;
        bool m_PreviousCastsShadows = true;
        int m_PreviousPathHash = 0;

        int m_SpriteMaterialCount;

        internal Vector3 m_CachedPosition;
        internal Vector3 m_CachedLossyScale;
        internal Quaternion m_CachedRotation;
        internal Matrix4x4 m_CachedShadowMatrix;
        internal Matrix4x4 m_CachedInverseShadowMatrix;
        internal Matrix4x4 m_CachedLocalToWorldMatrix;

        internal int spriteMaterialCount => m_SpriteMaterialCount;

        internal override void CacheValues()
        {
            m_CachedPosition = transform.position;
            m_CachedLossyScale = transform.lossyScale;
            m_CachedRotation = transform.rotation;

            m_CachedShadowMatrix = Matrix4x4.TRS(m_CachedPosition, m_CachedRotation, Vector3.one);
            m_CachedInverseShadowMatrix = m_CachedShadowMatrix.inverse;

            m_CachedLocalToWorldMatrix = transform.localToWorldMatrix;
        }

        public ShadowCastingOptions castingOption
        {
            set { m_CastingOption = value; }
            get { return m_CastingOption; }
        }

        /// <summary>
        /// If selfShadows is true, useRendererSilhoutte specifies that the renderer's sihouette should be considered part of the shadow. If selfShadows is false, useRendererSilhoutte specifies that the renderer's sihouette should be excluded from the shadow
        /// </summary>
        [Obsolete("useRendererSilhoutte is deprecated. Use rendererSilhoutte instead")]
        public bool useRendererSilhouette
        {
            set { m_UseRendererSilhouette = value; }
            get { return m_UseRendererSilhouette && m_HasRenderer; }
        }

        /// <summary>
        /// If true, the shadow casting shape is included as part of the shadow. If false, the shadow casting shape is excluded from the shadow.
        /// </summary>
        [Obsolete("selfShadows is deprecated. Use rendererSilhoutte instead")]
        public bool selfShadows
        {
            set { m_SelfShadows = value; }
            get { return m_SelfShadows; }
        }

        /// <summary>
        /// Specifies if shadows will be cast.
        /// </summary>
        ///
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
            deltaPos.x = boundingSphere.position.x + m_CachedPosition.x;
            deltaPos.y = boundingSphere.position.y + m_CachedPosition.y;
            deltaPos.z = boundingSphere.position.z + m_CachedPosition.z;

            deltaPos.x = light.m_CachedPosition.x - deltaPos.x;
            deltaPos.y = light.m_CachedPosition.y - deltaPos.y;
            deltaPos.z = light.m_CachedPosition.z - deltaPos.z;

            float distanceSq = Vector3.SqrMagnitude(deltaPos);

            float radiiLength = light.boundingSphere.radius + boundingSphere.radius;
            return distanceSq <= (radiiLength * radiiLength);
        }

        internal bool IsShadowedLayer(int layer)
        {
            return m_ApplyToSortingLayers != null ? Array.IndexOf(m_ApplyToSortingLayers, layer) >= 0 : false;
        }

        void SetShadowShape()
        {
            if (m_ShadowMesh == null)
                m_ShadowMesh = new ShadowMesh2D();

            if (m_ShadowCastingSource == ShadowCastingSources.ShapeEditor)
            {
                NativeArray<Vector3> nativePath = new NativeArray<Vector3>(m_ShapePath, Allocator.Temp);
                NativeArray<int> nativeIndices = new NativeArray<int>(2 * m_ShapePath.Length, Allocator.Temp);

                int lastIndex = m_ShapePath.Length - 1;
                for (int i = 0; i < m_ShapePath.Length; i++)
                {
                    int startingIndex = i << 1;
                    nativeIndices[startingIndex] = lastIndex;
                    nativeIndices[startingIndex + 1] = i;
                    lastIndex = i;
                }

                m_ShadowMesh.SetShapeWithLines(nativePath, nativeIndices, false);

                nativePath.Dispose();
                nativeIndices.Dispose();
            }
            if (m_ShadowCastingSource == ShadowCastingSources.ShapeProvider)
            {
                ShapeProviderUtility.PersistantDataCreated(m_ShadowShapeProvider, m_ShadowMesh);
            }
        }

        private void Awake()
        {
            if (m_ApplyToSortingLayers == null)
                m_ApplyToSortingLayers = SetDefaultSortingLayers();

            Bounds bounds = new Bounds(transform.position, Vector3.one);

            if (m_ShadowCastingSource < 0)
            {
                Component component = ShapeProviderUtility.GetDefaultShadowCastingSource(gameObject);
                if (component != null && shapePath == null)
                {
                    m_ShadowShapeProvider = component;
                    m_ShadowCastingSource = ShadowCastingSources.ShapeProvider;
                }
                else
                {
                    m_ShadowCastingSource = ShadowCastingSources.ShapeEditor;
                }
            }

            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                bounds = renderer.bounds;
                m_SpriteMaterialCount = renderer.sharedMaterials.Length;
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

        protected void OnEnable()
        {
            if (m_ShadowMesh == null || m_InstanceId != GetInstanceID())
            {
                m_ShadowMesh = null;
                SetShadowShape();
                m_InstanceId = GetInstanceID();
            }

            m_ShadowCasterGroup = null;
        }

        protected void OnDisable()
        {
            ShadowCasterGroup2DManager.RemoveFromShadowCasterGroup(this, m_ShadowCasterGroup);
        }

        public void Update()
        {
            Renderer renderer;
            m_HasRenderer = TryGetComponent<Renderer>(out renderer);

            bool rebuildMesh = LightUtility.CheckForChange((int)m_ShadowCastingSource, ref m_PreviousShadowCastingSource);
            rebuildMesh |= LightUtility.CheckForChange((int)edgeProcessing, ref m_PreviousEdgeProcessing);
            rebuildMesh |= edgeProcessing != EdgeProcessing.None && LightUtility.CheckForChange(contractEdge, ref m_PreviousContractEdge);

            if (m_ShadowMesh == null)
            {
                SetShadowShape();
            }
            else if (m_ShadowCastingSource == ShadowCastingSources.ShapeEditor)
            {
                rebuildMesh |= LightUtility.CheckForChange(m_ShapePathHash, ref m_PreviousPathHash);
                if (rebuildMesh)
                {
                    SetShadowShape();
                }
            }
            else
            {
                if ((rebuildMesh || LightUtility.CheckForChange(m_ShadowShapeProvider, ref m_PreviousShadowShapeProvider)) && m_ShadowShapeProvider != null)
                {
                    SetShadowShape();
                }
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
                ShadowCasterGroup2DManager.AddGroup(this);
            }
        }

#if UNITY_EDITOR
        internal void DrawPreviewOutline(Transform t, float contractionDistance)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector4[] tangents = mesh.tangents;

            Handles.color = Color.white;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];

                Vector3 pt0 = vertices[v0];
                Vector3 pt1 = vertices[v1];
                Vector3 pt2 = vertices[v2];

                Vector4 tan0 = tangents[v0];
                Vector4 tan1 = tangents[v1];
                Vector4 tan2 = tangents[v2];

                Vector3 contractPt0 = new Vector3(pt0.x + contractionDistance * tan0.x, pt0.y + contractionDistance * tan0.y, 0);
                Vector3 contractPt1 = new Vector3(pt1.x + contractionDistance * tan1.x, pt1.y + contractionDistance * tan1.y, 0);
                Vector3 contractPt2 = new Vector3(pt2.x + contractionDistance * tan2.x, pt2.y + contractionDistance * tan2.y, 0);

                Handles.DrawAAPolyLine(4, new Vector3[] { t.TransformPoint(contractPt0), t.TransformPoint(contractPt1) });
                Handles.DrawAAPolyLine(4, new Vector3[] { t.TransformPoint(contractPt1), t.TransformPoint(contractPt2) });
                Handles.DrawAAPolyLine(4, new Vector3[] { t.TransformPoint(contractPt2), t.TransformPoint(contractPt0) });
            }
        }

        internal void DrawPreviewOutline()
        {
            if (m_ShadowMesh != null && mesh != null && m_ShadowCastingSource != ShadowCastingSources.None)
            {
                if (edgeProcessing == EdgeProcessing.None)
                    DrawPreviewOutline(transform, contractEdge);
                else
                    DrawPreviewOutline(transform, 0);
            }
        }

        void Reset()
        {
            Awake();
            OnEnable();
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
                // ----------------------------------------------------
                // m_SelfShadows | m_CastsShadows |    m_CastingOption
                // ----------------------------------------------------
                //       0       |       0        |        X
                //       0       |       1        |   Renderer Only
                //       1       |       0        |     Cast Only
                //       1       |       1        |       Both

                // Flag this for requiring an upgrade to run
                if (m_SelfShadows && m_CastsShadows)
                    m_CastingOption = ShadowCastingOptions.Both;
                else if (m_SelfShadows)
                    m_CastingOption = ShadowCastingOptions.Self;
                else
                    m_CastingOption = ShadowCastingOptions.Cast;
            }
        }

#endif
    }
}
