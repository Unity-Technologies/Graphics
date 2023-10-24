using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.U2D;
using Unity.Collections;

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEditor.EditorTools;
#endif

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class <c>ShadowCaster2D</c> contains properties used for shadow casting
    /// </summary>
    [CoreRPHelpURL("2DShadows", "com.unity.render-pipelines.universal")]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]

    [AddComponentMenu("Rendering/2D/Shadow Caster 2D")]
    [MovedFrom(false, "UnityEngine.Experimental.Rendering.Universal", "com.unity.render-pipelines.universal")]

    public class ShadowCaster2D : ShadowCasterGroup2D, ISerializationCallbackReceiver
    {

        internal enum ComponentVersions
        {
            Version_Unserialized = 0,
            Version_1 = 1,
            Version_2 = 2,
            Version_3 = 3,
            Version_4 = 4,
            Version_5 = 5
        }

        const ComponentVersions k_CurrentComponentVersion = ComponentVersions.Version_5;
        [SerializeField] ComponentVersions m_ComponentVersion = ComponentVersions.Version_Unserialized;

        internal enum ShadowCastingSources
        {
            None,
            ShapeEditor,
            ShapeProvider
        }


        /// <summary>
        /// Options for what type of shadows are cast.
        /// </summary>
        public enum ShadowCastingOptions
        {
            /// <summary>
            /// Renders a shadows only for the sprite.
            /// </summary>
            SelfShadow,

            /// <summary>
            /// Renders a shadows only a cast shadow.
            /// </summary>
            CastShadow,

            /// <summary>
            /// Renders both a shadows for the sprite and a cast shadow.
            /// </summary>
            CastAndSelfShadow,

            /// <summary>
            /// Renders a sprite without shadow casting correctly on top of other shadow casting sprites
            /// </summary>
            NoShadow
        }

        internal enum EdgeProcessing
        {
            None = ShadowMesh2D.EdgeProcessing.None,
            Clipping = ShadowMesh2D.EdgeProcessing.Clipping,
        }

        [SerializeField] bool m_HasRenderer = false;
        [SerializeField] bool m_UseRendererSilhouette = true;
        [SerializeField] bool m_CastsShadows = true;
        [SerializeField] bool m_SelfShadows = false;
        [Range(0, 1)]
        [SerializeField] float m_AlphaCutoff = 0.1f;
        [SerializeField] int[] m_ApplyToSortingLayers = null;
        [SerializeField] Vector3[] m_ShapePath = null;
        [SerializeField] int m_ShapePathHash = 0;

        [SerializeField] int m_InstanceId;
        [SerializeField] Component m_ShadowShape2DComponent;
        [SerializeReference] ShadowShape2DProvider m_ShadowShape2DProvider;
        [SerializeField] ShadowCastingSources m_ShadowCastingSource = (ShadowCastingSources)(-1);

        [SerializeField] internal ShadowMesh2D m_ShadowMesh;
        [SerializeField] ShadowCastingOptions m_CastingOption = ShadowCastingOptions.CastShadow;

        [SerializeField] internal float m_PreviousTrimEdge = 0;
        [SerializeField] internal int m_PreviousEdgeProcessing;
        [SerializeField] internal int m_PreviousShadowCastingSource;
        [SerializeField] internal Component m_PreviousShadowShape2DSource = null;

        internal ShadowCasterGroup2D m_ShadowCasterGroup = null;
        internal ShadowCasterGroup2D m_PreviousShadowCasterGroup = null;


        internal bool m_ForceShadowMeshRebuild;

        internal EdgeProcessing edgeProcessing
        {
            get { return (EdgeProcessing)m_ShadowMesh.edgeProcessing; }
            set { m_ShadowMesh.edgeProcessing = (ShadowMesh2D.EdgeProcessing)value; }
        }

        /// <summary>
        /// The mesh to draw with.
        /// </summary>
        public Mesh mesh => m_ShadowMesh.mesh;

        /// <summary>
        /// The bounding sphere for the shadow caster
        /// </summary>
        public BoundingSphere boundingSphere => m_ShadowMesh.boundingSphere;

        /// <summary>
        /// The amount the shadow's edge is trimed
        /// </summary>
        public float trimEdge
        {
            get { return m_ShadowMesh.trimEdge; } set { m_ShadowMesh.trimEdge = value; }
        }

        /// <summary>
        /// The sets the renderer's shadow cutoff
        /// </summary>
        public float alphaCutoff
        {
            get { return m_AlphaCutoff; }
            set { m_AlphaCutoff = value; }
        }

        /// <summary>
        /// The path for the shape.
        /// </summary>
        public Vector3[] shapePath => m_ShapePath;

        internal int shapePathHash { get { return m_ShapePathHash; } set { m_ShapePathHash = value; } }

        internal ShadowCastingSources shadowCastingSource { get { return m_ShadowCastingSource; } set { m_ShadowCastingSource = value; } }


        // Make this public if possible...
        internal Component shadowShape2DComponent { get { return m_ShadowShape2DComponent; } set { m_ShadowShape2DComponent = value; } }
        internal ShadowShape2DProvider shadowShape2DProvider { get { return m_ShadowShape2DProvider; } set { m_ShadowShape2DProvider = value; } }

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

            bool flipX, flipY;
            m_ShadowMesh.GetFlip(out flipX, out flipY);
            Vector3 scale = new Vector3(flipX ? -1 : 1, flipY ? -1 : 1, 1);

            m_CachedShadowMatrix = Matrix4x4.TRS(m_CachedPosition, m_CachedRotation, scale);
            m_CachedInverseShadowMatrix = m_CachedShadowMatrix.inverse;

            m_CachedLocalToWorldMatrix = transform.localToWorldMatrix;
        }

        /// <summary>
        /// Sets the type of shadow cast.
        /// </summary>
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
        public bool selfShadows
        {
            set
            {
                if (value)
                {
                    if (castingOption == ShadowCastingOptions.CastShadow)
                        castingOption = ShadowCastingOptions.CastAndSelfShadow;
                    else if (castingOption == ShadowCastingOptions.NoShadow)
                        castingOption = ShadowCastingOptions.SelfShadow;
                }
                else
                {
                    if (castingOption == ShadowCastingOptions.CastAndSelfShadow )
                        castingOption = ShadowCastingOptions.CastShadow;
                    else if(castingOption == ShadowCastingOptions.SelfShadow)
                        castingOption = ShadowCastingOptions.NoShadow;
                }

            }
            get { return castingOption == ShadowCastingOptions.CastAndSelfShadow || castingOption == ShadowCastingOptions.SelfShadow; }
        }

        /// <summary>
        /// Specifies if shadows will be cast.
        /// </summary>
        ///
        public bool castsShadows
        {
            set
            {
                if(value)
                {
                    if (castingOption == ShadowCastingOptions.SelfShadow)
                        castingOption = ShadowCastingOptions.CastAndSelfShadow;
                    else if (castingOption == ShadowCastingOptions.NoShadow)
                        castingOption = ShadowCastingOptions.CastShadow;
                }
                else
                {
                    if (castingOption == ShadowCastingOptions.CastAndSelfShadow)
                        castingOption = ShadowCastingOptions.SelfShadow;
                    else if (castingOption == ShadowCastingOptions.CastShadow)
                        castingOption = ShadowCastingOptions.NoShadow;
                }
            }

            get { return castingOption == ShadowCastingOptions.CastShadow || castingOption == ShadowCastingOptions.CastAndSelfShadow; }
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
            deltaPos.x = light.m_CachedPosition.x - boundingSphere.position.x;
            deltaPos.y = light.m_CachedPosition.y - boundingSphere.position.y;
            deltaPos.z = light.m_CachedPosition.z - boundingSphere.position.z;

            float distanceSq = Vector3.SqrMagnitude(deltaPos);

            float radiiLength = light.boundingSphere.radius + boundingSphere.radius;
            return distanceSq <= (radiiLength * radiiLength);
        }

        internal bool IsShadowedLayer(int layer)
        {
            return m_ApplyToSortingLayers != null ? Array.IndexOf(m_ApplyToSortingLayers, layer) >= 0 : false;
        }

        void SetShadowShape(ShadowMesh2D shadowMesh)
        {
            m_ForceShadowMeshRebuild = false;

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

                shadowMesh.SetShapeWithLines(nativePath, nativeIndices, false);

                nativePath.Dispose();
                nativeIndices.Dispose();
            }
            if (m_ShadowCastingSource == ShadowCastingSources.ShapeProvider)
            {
                ShapeProviderUtility.PersistantDataCreated(m_ShadowShape2DProvider, m_ShadowShape2DComponent, shadowMesh);
            }
        }

        private void Awake()
        {
            if (m_ShadowCastingSource < 0)
            {
#if UNITY_EDITOR
                ShapeProviderUtility.TryGetDefaultShadowShapeProviderSource(gameObject, out var component, out var provider);
                if (component != null && provider != null && (shapePath == null || shapePath.Length == 0))
                {
                    m_ShadowShape2DComponent = component;
                    m_ShadowShape2DProvider = provider;
                    m_ShadowCastingSource = ShadowCastingSources.ShapeProvider;
                }
                else
                {
                    m_ShadowCastingSource = ShadowCastingSources.ShapeEditor;
                }
#else
                m_ShadowCastingSource = ShadowCastingSources.ShapeEditor;
#endif
            }

            Vector3 inverseScale = Vector3.zero;
            Vector3 relOffset = transform.position;

            if (transform.lossyScale.x != 0 && transform.lossyScale.y != 0)
            {
                inverseScale = new Vector3(1 / transform.lossyScale.x, 1 / transform.lossyScale.y);
                relOffset = new Vector3(inverseScale.x * -transform.position.x, inverseScale.y * -transform.position.y);
            }


            if (m_ApplyToSortingLayers == null)
                m_ApplyToSortingLayers = SetDefaultSortingLayers();


            Bounds bounds = new Bounds(transform.position, Vector3.one);
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                bounds = renderer.bounds;
                m_SpriteMaterialCount = renderer.sharedMaterials.Length;
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


            if (m_ShadowMesh == null)
            {
                ShadowMesh2D newShadowMesh = new ShadowMesh2D();
                SetShadowShape(newShadowMesh);
                m_ShadowMesh = newShadowMesh;
            }
#if UNITY_EDITOR
            // This step is required in case of copy/pasting an object with a shadow caster.
            else
            {
                ShadowMesh2D newShadowMesh = new ShadowMesh2D();
                newShadowMesh.CopyFrom(m_ShadowMesh);
                m_ShadowMesh = newShadowMesh;
            }
#endif

#if USING_PHYSICS2D_MODULE
            else
            {
                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null)
                    bounds = collider.bounds;
            }
#endif
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        protected void OnEnable()
        {
            if (m_ShadowShape2DProvider != null)
                m_ShadowShape2DProvider.Enabled(m_ShadowShape2DComponent);

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

            if (m_ShadowShape2DProvider != null)
                m_ShadowShape2DProvider.Disabled(m_ShadowShape2DComponent);

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

            bool rebuildMesh = LightUtility.CheckForChange((int)m_ShadowCastingSource, ref m_PreviousShadowCastingSource);
            rebuildMesh |= LightUtility.CheckForChange((int)edgeProcessing, ref m_PreviousEdgeProcessing);
            rebuildMesh |= edgeProcessing != EdgeProcessing.None && LightUtility.CheckForChange(trimEdge, ref m_PreviousTrimEdge);
            rebuildMesh |= m_ForceShadowMeshRebuild;

            if (m_ShadowCastingSource == ShadowCastingSources.ShapeEditor)
            {
                rebuildMesh |= LightUtility.CheckForChange(m_ShapePathHash, ref m_PreviousPathHash);
                if (rebuildMesh)
                {
                    SetShadowShape(m_ShadowMesh);
                }
            }
            else
            {
                if ((rebuildMesh || LightUtility.CheckForChange(m_ShadowShape2DComponent, ref m_PreviousShadowShape2DSource)) && m_ShadowShape2DComponent != null)
                {
                    SetShadowShape(m_ShadowMesh);
                }
            }

            m_PreviousShadowCasterGroup = m_ShadowCasterGroup;
            bool addedToNewGroup = ShadowCasterGroup2DManager.AddToShadowCasterGroup(this, ref m_ShadowCasterGroup, ref m_Priority);
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

            if(m_ShadowMesh != null)
                m_ShadowMesh.UpdateBoundingSphere(transform);
        }


#if UNITY_EDITOR
        internal void DrawPreviewOutline(Transform t, float trimionDistance)
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

                Vector3 trimPt0 = new Vector3(pt0.x + trimionDistance * tan0.x, pt0.y + trimionDistance * tan0.y, 0);
                Vector3 trimPt1 = new Vector3(pt1.x + trimionDistance * tan1.x, pt1.y + trimionDistance * tan1.y, 0);
                Vector3 trimPt2 = new Vector3(pt2.x + trimionDistance * tan2.x, pt2.y + trimionDistance * tan2.y, 0);

                bool flipX, flipY;
                m_ShadowMesh.GetFlip(out flipX, out flipY);
                Vector3 scale = new Vector3(t.lossyScale.x * (flipX ? -1 : 1), t.lossyScale.y * (flipY ? -1 : 1), 1);
                Matrix4x4 mat = Matrix4x4.TRS(t.position, t.rotation, scale);

                trimPt0 = mat.MultiplyPoint(trimPt0);
                trimPt1 = mat.MultiplyPoint(trimPt1);
                trimPt2 = mat.MultiplyPoint(trimPt2);

                if (pt0.z == 0 && pt1.z == 0)
                    Handles.DrawAAPolyLine(4, new Vector3[] { trimPt0, trimPt1 });
                if (pt1.z == 0 && pt2.z == 0)
                    Handles.DrawAAPolyLine(4, new Vector3[] { trimPt1, trimPt2 });
                if (pt2.z == 0 && pt0.z == 0)
                    Handles.DrawAAPolyLine(4, new Vector3[] { trimPt2, trimPt0 });
            }
        }

        internal void DrawPreviewOutline()
        {
            if (m_ShadowMesh != null && mesh != null && m_ShadowCastingSource != ShadowCastingSources.None && enabled)
            {
                if (edgeProcessing == EdgeProcessing.None)
                    DrawPreviewOutline(transform, trimEdge);
                else
                    DrawPreviewOutline(transform, 0);
            }
        }

        void Reset()
        {
            ShadowCasterGroup2DManager.RemoveFromShadowCasterGroup(this, m_ShadowCasterGroup);

            m_ShadowCasterGroup = null;
            m_PreviousShadowCasterGroup = null;
            m_PreviousShadowCastingSource = -1;
            m_PreviousShadowShape2DSource = null;
            m_PreviousTrimEdge = 0;
            m_PreviousEdgeProcessing = -1;
            m_ForceShadowMeshRebuild = true;

            m_HasRenderer = false;
            m_UseRendererSilhouette = true;
            m_CastsShadows = true;
            m_SelfShadows = false;
            m_ApplyToSortingLayers = null;
            m_ShapePath = null;
            m_ShapePathHash = 0;

            m_ShadowShape2DComponent = null;
            m_ShadowShape2DProvider = null;
            m_ShadowCastingSource = (ShadowCastingSources)(-1);

            m_ShadowMesh = null;
            m_CastingOption = ShadowCastingOptions.CastShadow;

            ToolManager.RestorePreviousTool(); // This is needed in case you have the shape editor active

            Awake();
            OnEnable();
        }
#endif

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
            if (m_ComponentVersion < ComponentVersions.Version_2)
            {
                // ----------------------------------------------------
                // m_SelfShadows | m_CastsShadows |    m_CastingOption
                // ----------------------------------------------------
                //       0       |       0        |     DontCast
                //       0       |       1        |   Renderer Only
                //       1       |       0        |     Cast Only
                //       1       |       1        |   CastAndSelfShadow
                // ----------------------------------------------------
                if (m_SelfShadows && m_CastsShadows)
                    m_CastingOption = ShadowCastingOptions.CastAndSelfShadow;
                else if (m_SelfShadows)
                    m_CastingOption = ShadowCastingOptions.SelfShadow;
                else if (m_CastsShadows)
                    m_CastingOption = ShadowCastingOptions.CastShadow;
                else
                    m_CastingOption = ShadowCastingOptions.NoShadow;
            }
            if(m_ComponentVersion < ComponentVersions.Version_3)
            {
                m_ShadowMesh = null;
                m_ForceShadowMeshRebuild = true;
            }
        }
    }
}
