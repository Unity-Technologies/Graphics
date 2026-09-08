using System;
using Unity.Collections;
using static UnityEngine.Rendering.Universal.ShadowUtility;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class ShadowMesh2D : ShadowShape2D
    {
        internal const float k_TrimEdgeUninitialized = -1;

        /// <summary>
        /// Floor for <see cref="fanSegments"/>. Zero is not merely coarse, it is wrong: a fan is
        /// what joins two band ends, or a fin to a band, so removing it disconnects the outer
        /// boundary — measured at up to 2.00x the band width. For a concave corner it is worse, since
        /// the band corners are set back to the fillet tangent points by a loop that never consults
        /// the segment count, so at zero the shadow that setback removed is never replaced. See
        /// simulation report 29.
        /// </summary>
        internal const int k_MinFanSegments = 1;

        internal const int k_DefaultFanSegments = 3;

        [NonSerialized]  Mesh               m_Mesh;
        [NonSerialized]  bool               m_IsDirty;
        [SerializeField] ShadowMeshVertex[] m_Vertices;
        [SerializeField] int[]              m_Indices;

        NativeArray<ShadowMeshVertex>       m_NativeVertices;
        NativeArray<int>                    m_NativeIndices;

        // Vertex buffer for a generator whose format is not ShadowMeshVertex. Never serialized: a
        // generator using it declares persistsGeometry == false and its geometry is rebuilt on load.
        [NonSerialized] NativeArray<float>  m_NativeCustomVertices;
        [NonSerialized] int                 m_CustomVertexCount;

        [SerializeField] bool               m_IsTransformable = true;


        [SerializeField] Bounds m_LocalBounds;
        [SerializeField] float m_TrimEdge = k_TrimEdgeUninitialized;
        [SerializeField] bool  m_FlipX;
        [SerializeField] bool  m_FlipY;
        [SerializeField] float m_InitialTrim = 0;

        // Which generator produced the baked m_Vertices / m_Indices, by id. No longer a request --
        // the format is a project decision (Shadow2DGeometrySettings.geometryVersion) -- but still
        // serialized, because it is what says which layout the bytes on disk are in. A mismatch
        // against the project format on load means the baked geometry is in the wrong layout and
        // must be rebuilt; see ConsumeGeneratorRebuildRequest.
        [SerializeField] string m_GeneratorId = ShadowGeometryGeneratorRegistry.k_DefaultGeneratorId;

        // Triangles per fan, for generators that tessellate curved shadow features. Per caster
        // rather than per light because it fixes the vertex and index counts, which are decided at
        // mesh-build time.
        [SerializeField] int m_FanSegments = k_DefaultFanSegments;

        // The generator version that produced the baked m_Vertices / m_Indices. A mismatch on
        // load means the baked geometry predates the current generator and must be rebuilt.
        [SerializeField] int m_GeneratorVersion;

        // Pre-id enum value, kept only to migrate assets written before generators were named.
        // -1 means "nothing to migrate"; old assets serialize 0 (Legacy) or 1 (the since-removed
        // Passthrough placeholder). Both migrate to Legacy -- see OnAfterDeserialize.
        [SerializeField] int m_GeometryPath = -1;

        // Which generator actually produced the buffers in memory.
        [NonSerialized] string m_GeneratedId = ShadowGeometryGeneratorRegistry.k_DefaultGeneratorId;

        // Whether the buffers in memory were built under a global generator override, so
        // OnBeforeSerialize can refuse to persist them. Recorded at build time rather than read back
        // from Shadow2DGeometry, because OnBeforeSerialize cannot resolve the override's meaning
        // without reading graphics settings -- which Unity forbids from a serialization callback.
        [NonSerialized] bool m_GeneratedUnderOverride;

        // Whether the generator that produced the buffers allows them on disk. Cached at build time
        // for the same reason m_GeneratedId is: OnBeforeSerialize cannot resolve a generator, because
        // that builds the registry and its discovery pass reads GraphicsSettings, which Unity forbids
        // from a serialization callback.
        [NonSerialized] bool m_GeneratedPersists = true;

        // Set when a generator-version check is owed, so ShadowCaster2D can ask for one on the
        // main thread. The check itself cannot run in OnAfterDeserialize: resolving a generator
        // builds the registry, whose discovery pass reads GraphicsSettings, and Unity forbids
        // that from a serialization callback.
        [NonSerialized] bool m_GeneratorVersionCheckPending;

        public  Mesh mesh
        {
            get
            {
                if(m_Mesh == null || m_Mesh.vertexCount == 0 || m_IsDirty)
                {
                    ShadowGeometryResult current = CurrentGeometry();
                    ShadowGeometryGeneratorRegistry.Get(m_GeneratedId).UploadMesh(ref m_Mesh, in current);
                    m_IsDirty = false;
                }

                return m_Mesh;
            }
        }

        /// <summary>
        /// Releases the <c>Mesh</c> this instance built.
        /// </summary>
        /// <remarks>
        /// The mesh is created at runtime and is not serialized, so nothing else owns it and nothing
        /// else will free it: destroying the managed wrapper is not enough, because a
        /// <c>UnityEngine.Object</c> wrapper being collected does not destroy the native object
        /// behind it. Left alone, one mesh per caster outlives the caster.
        ///
        /// Called from <c>ShadowCaster2D.OnDestroy</c> rather than from the finalizer below, because
        /// <c>Object.Destroy</c> is not legal off the main thread.
        ///
        /// The geometry buffers are deliberately left alone. They are released by the finalizer, and
        /// dropping them here would leave a caster that Undo brings back with no geometry until
        /// something happened to rebuild it.
        /// </remarks>
        internal void DestroyMesh()
        {
            if (m_Mesh == null)
                return;

            CoreUtils.Destroy(m_Mesh);
            m_Mesh = null;

            // So a later access rebuilds from the buffers instead of handing out null.
            m_IsDirty = true;
        }

        public void Clear()
        {
            m_Vertices = null;
            m_Indices = null;

            // Release the geometry too. Leaving the natives alive meant a degenerate shape kept
            // rendering its previous mesh, and the next OnBeforeSerialize repopulated the managed
            // arrays from those stale buffers.
            if (m_NativeVertices.IsCreated)
                m_NativeVertices.Dispose();
            if (m_NativeIndices.IsCreated)
                m_NativeIndices.Dispose();
            if (m_NativeCustomVertices.IsCreated)
                m_NativeCustomVertices.Dispose();

            m_NativeVertices = default;
            m_NativeIndices = default;
            m_NativeCustomVertices = default;
            m_CustomVertexCount = 0;
            m_IsDirty = true;
        }


        public bool isTransformable => m_IsTransformable;

        public  BoundingSphere boundingSphere { get => m_BoundingSphere; }
        internal BoundingSphere m_BoundingSphere;   // update to world space
        public float trimEdge { get { return m_TrimEdge; } set { m_TrimEdge = value; } }

        /// <summary>
        /// The generator this caster will actually build with: the project's format, or a global
        /// override when one is active.
        /// </summary>
        /// <remarks>
        /// There is deliberately no setter for <c>m_GeneratorId</c>. It records which layout the
        /// baked bytes are in, not a choice -- the choice is
        /// <c>Shadow2DGeometrySettings.geometryVersion</c>, which is project-wide so that a caster and
        /// whatever material draws it cannot disagree about the vertex layout.
        /// </remarks>
        internal string activeGeneratorId => Shadow2DGeometry.ResolveId(m_GeneratorId);

        /// <summary>
        /// The generator that actually produced the buffers currently in memory.
        /// </summary>
        /// <remarks>
        /// Differs from <see cref="activeGeneratorId"/> exactly when the mesh is stale, which is the
        /// one state that renders a wrong shadow rather than a missing one -- the layouts disagree but
        /// both are self-consistent, so nothing else detects it. Exposed for diagnostics.
        /// </remarks>
        internal string generatedGeneratorId => m_GeneratedId;

        /// <summary>
        /// Triangles per fan, for generators that tessellate curved shadow features. Clamped to at
        /// least <see cref="k_MinFanSegments"/>: see that field for why zero is not legal. The
        /// generator clamps again on the way in, because this can also arrive straight off disk.
        /// </summary>
        internal int fanSegments
        {
            get { return Mathf.Max(k_MinFanSegments, m_FanSegments); }
            set { m_FanSegments = Mathf.Max(k_MinFanSegments, value); }
        }

        internal void OnBeforeSerialize()
        {
            // Geometry built under a global path override must never reach disk. ShadowCaster2D
            // is [ExecuteInEditMode], so an override would otherwise be baked into scenes on save
            // and into prefab assets from Prefab Mode, leaving a large spurious override on every
            // prefab instance. Skipping the copy leaves whatever was last legitimately serialized;
            // the mesh rebuilds on load either way.
            //
            // Read from the flag captured at build time rather than compared against the project
            // format here -- resolving that reads graphics settings, which Unity forbids from a
            // serialization callback.
            if (m_GeneratedUnderOverride)
                return;

            // A generator can also opt out of persisting entirely, in which case its geometry is
            // rebuilt on load instead. Read from the cached flag rather than the registry — see
            // m_GeneratedPersists.
            if (!m_GeneratedPersists)
                return;

            if(m_NativeVertices.IsCreated)
                m_Vertices = m_NativeVertices.ToArray();

            if (m_NativeIndices.IsCreated)
                m_Indices = m_NativeIndices.ToArray();
        }

        internal void OnAfterDeserialize()
        {
            if(m_NativeVertices.IsCreated)
                m_NativeVertices.Dispose();
            m_NativeVertices = new NativeArray<ShadowMeshVertex>(m_Vertices, Allocator.Persistent);

            if(m_NativeIndices.IsCreated)
                m_NativeIndices.Dispose();
            m_NativeIndices = new NativeArray<int>(m_Indices, Allocator.Persistent);

            // Migrate assets written before generators had names. Runs before anything reads
            // m_GeneratorId, and clears the marker so it only happens once.
            if (m_GeometryPath >= 0)
            {
                // Both old values land on Legacy. 0 was Legacy; 1 was the Passthrough development
                // placeholder, which delegated to Legacy's algorithm and produced identical geometry,
                // so collapsing it loses nothing. Passthrough was removed once Unity.SoftShadow made it
                // redundant as a proof that the generator abstraction works.
                m_GeneratorId = LegacyShadowGeometryGenerator.k_Id;
                m_GeometryPath = -1;
            }

            // Whatever was on disk was written by the serialized generator (see the guard above).
            m_GeneratedId = m_GeneratorId;

            // Baked geometry from an older version of this generator is stale. Only *flagged*
            // here, not decided: deciding means resolving the generator, which builds the registry,
            // whose discovery pass reads GraphicsSettings -- and that throws when called from a
            // serialization callback ("not allowed to be called during serialization"). The flag is
            // resolved by ConsumeGeneratorRebuildRequest, which ShadowCaster2D calls on the main
            // thread.
            m_GeneratorVersionCheckPending = true;

            // The buffers were just replaced. m_Mesh is [NonSerialized] and so is normally null
            // here, but this also runs on an existing instance for every Undo step and prefab
            // revert -- without this the mesh would keep showing pre-Undo geometry.
            m_IsDirty = true;
        }

        /// <summary>
        /// Whether this caster's geometry needs rebuilding after a load. True when the baked geometry
        /// came from a different version of its generator, and true when the generator does not
        /// persist geometry at all — in that case nothing was written, so there is nothing to load.
        /// </summary>
        /// <remarks>
        /// Answered on demand rather than at deserialization time (see
        /// <see cref="OnAfterDeserialize"/>) and cleared once, so a load triggers exactly one
        /// rebuild. Must be called from the main thread, since resolving a generator reads
        /// GraphicsSettings.
        /// </remarks>
        internal bool ConsumeGeneratorRebuildRequest()
        {
            if (!m_GeneratorVersionCheckPending)
                return false;

            m_GeneratorVersionCheckPending = false;

            // The project's format decides what this caster builds, so geometry baked in any other
            // layout is stale no matter how current the generator that wrote it was. This is the
            // check that makes flipping Shadow2DGeometrySettings.geometryVersion take effect, and it
            // has to come first: m_GeneratorId names the layout on disk, which is exactly what the
            // version comparison below would otherwise be asking about the wrong generator.
            if (!string.Equals(m_GeneratorId, activeGeneratorId, StringComparison.Ordinal))
                return true;

            if (!ShadowGeometryGeneratorRegistry.Contains(m_GeneratorId))
                return false;

            ShadowGeometryGenerator generator = ShadowGeometryGeneratorRegistry.Get(m_GeneratorId);

            return !generator.persistsGeometry || generator.version != m_GeneratorVersion;
        }

        // Snapshot of the live buffers in the shape a generator expects. Used by the lazy mesh
        // getter, which has geometry but no in-flight build to draw a result from.
        ShadowGeometryResult CurrentGeometry()
        {
            return new ShadowGeometryResult
            {
                vertices = m_NativeVertices,
                indices = m_NativeIndices,
                customVertices = m_NativeCustomVertices,
                customVertexCount = m_CustomVertexCount,
                localBounds = m_LocalBounds,
                trimEdge = m_TrimEdge,
                isTransformable = m_IsTransformable,
            };
        }

        /// <summary>
        /// Runs the active generator and applies everything it produced. The generator owns the
        /// algorithm and the vertex format; this class stays the sole owner of the buffers.
        /// </summary>
        void BuildShape(ref ShadowShapeInput input)
        {
            string activeId = activeGeneratorId;
            ShadowGeometryGenerator generator = ShadowGeometryGeneratorRegistry.Get(activeId);

            // Seeded with the live buffers: ShadowUtility.GenerateShadowGeometry disposes
            // whatever it is handed, so ownership travels in and back out through the result.
            ShadowGeometryResult result = CurrentGeometry();

            // Clipper faults instead of rejecting out-of-range input in player builds, where its own
            // range test is compiled out. Every generator hands its vertices to ShadowUtility, so the
            // check belongs here rather than in each one: bail out as a degenerate shape does.
            if (HasUnrepresentableVertices(input.vertices, input.indices))
                result.cleared = true;
            else
                generator.Build(ref input, ref result);

            m_NativeVertices = result.vertices;
            m_NativeIndices = result.indices;
            m_NativeCustomVertices = result.customVertices;
            m_CustomVertexCount = result.customVertexCount;
            m_TrimEdge = result.trimEdge;
            m_IsTransformable = result.isTransformable;
            m_GeneratedId = activeId;
            m_GeneratedPersists = generator.persistsGeometry;
            m_GeneratorVersion = generator.version;
            m_GeneratedUnderOverride = Shadow2DGeometry.globalGeneratorOverride != null;
            // Just built with the current generator, so no version check is owed.
            m_GeneratorVersionCheckPending = false;

            // Record which layout the buffers are in, so a later load can tell whether what is on
            // disk matches the project's format. Skipped under an override for the same reason
            // OnBeforeSerialize skips the geometry itself: an A/B sweep must leave no trace in the
            // asset.
            if (!m_GeneratedUnderOverride)
                m_GeneratorId = activeId;

            if (result.cleared)
                Clear();

            if (result.localBoundsAssigned)
                m_LocalBounds = result.localBounds;

            if (result.markDirty)
                m_IsDirty = true;

            if (result.uploadMesh)
            {
                if (m_Mesh == null)
                    m_Mesh = new Mesh();

                // Upload from the applied state rather than from `result`: Clear() may have just
                // released the buffers `result` still points at. Clearing m_IsDirty here is what
                // stops the lazy getter immediately re-uploading the same data.
                ShadowGeometryResult current = CurrentGeometry();
                generator.UploadMesh(ref m_Mesh, in current);
                m_IsDirty = false;
            }
        }

        ShadowShapeInput CreateInput(ShadowShapeKind kind, NativeArray<Vector3> vertices, NativeArray<int> indices)
        {
            return new ShadowShapeInput
            {
                kind = kind,
                vertices = vertices,
                indices = indices,
                trimEdge = m_TrimEdge,
                initialTrim = m_InitialTrim,
                outputTransform = Matrix4x4.identity,
                fanSegments = fanSegments,
            };
        }

        public override void SetShape(NativeArray<Vector3> vertices, NativeArray<int> indices, NativeArray<float> radii, Matrix4x4 transform, ShadowShape2D.WindingOrder windingOrder = ShadowShape2D.WindingOrder.Clockwise, bool allowTriming = true, bool createInteriorGeometry = false)
        {
            ShadowShapeInput input = CreateInput(ShadowShapeKind.RadiiWithTransform, vertices, indices);
            input.radii = radii;
            input.outputTransform = transform;
            input.windingOrder = windingOrder;
            input.allowTrimming = allowTriming;
            input.createInteriorGeometry = createInteriorGeometry;

            BuildShape(ref input);
        }


        // Clipper scales incoming coordinates by ShadowUtility's fixed point precision and keeps them in a 64 bit
        // integer. Its own range test is compiled out when exceptions are disabled, as they are in player builds, so
        // a coordinate past this bound overflows inside native Clipper and faults instead of being rejected. Staying
        // within Clipper's low range (0x3FFFFFFF) after scaling by the precision (65536) keeps that arithmetic valid.
        const float k_MaxClipperCoordinate = 16383.0f;

        static bool IsRepresentableCoordinate(float value)
        {
            // Both comparisons are false for NaN, so this also rejects NaN and infinities.
            return value >= -k_MaxClipperCoordinate && value <= k_MaxClipperCoordinate;
        }

        // Returns true when any vertex the shape actually USES holds a coordinate Clipper cannot
        // represent, which makes the whole shape unusable. Callers must bail out instead of passing
        // the data on to native code.
        //
        // Only indexed vertices are checked. A provider may hand over a vertex buffer larger than
        // its index set references -- SpriteSkin does exactly that: outlineVertices is the whole
        // m_DeformedOutlineVertexCache, allocated with NativeArrayOptions.UninitializedMemory, while
        // UpdateDeformedOutlineCache only writes the entries m_OutlineIndexCache names (e.g. 63 of
        // 79 for the head caster in 120_SpriteSkin_Shadows_*). Scanning the unreferenced tail meant
        // uninitialised garbage -- a NaN or a huge float -- rejected an otherwise valid outline, and
        // that caster then cast no shadow at all. It only ever bit CPU deformation, because the GPU
        // path returns the live deformation buffer rather than this cache, and it was intermittent
        // because it depended on what happened to be in unwritten memory.
        //
        // Skipping the tail is also sound for Clipper: edges are built from the index buffer, so a
        // vertex no index references never reaches native code.
        static bool HasUnrepresentableVertices(NativeArray<Vector3> vertices, NativeArray<int> indices)
        {
            if (vertices == null)
                return true;

            // No index buffer means every vertex is potentially consumed, so all of them must be
            // representable.
            if (indices == null || indices.Length == 0)
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 vertex = vertices[i];
                    if (!IsRepresentableCoordinate(vertex.x) || !IsRepresentableCoordinate(vertex.y))
                        return true;
                }

                return false;
            }

            for (int i = 0; i < indices.Length; i++)
            {
                int index = indices[i];

                // An out-of-range index is itself unusable data; reject rather than throw.
                if (index < 0 || index >= vertices.Length)
                    return true;

                Vector3 vertex = vertices[index];
                if (!IsRepresentableCoordinate(vertex.x) || !IsRepresentableCoordinate(vertex.y))
                    return true;
            }

            return false;
        }

        public override void SetShape(NativeArray<Vector3> vertices, NativeArray<int> indices, NativeArray<float> radii, ShadowShape2D.WindingOrder windingOrder = ShadowShape2D.WindingOrder.Clockwise, bool allowTriming = true, bool createInteriorGeometry = false, bool inWorldSpace = false)
        {
            ShadowShapeInput input = CreateInput(ShadowShapeKind.Radii, vertices, indices);
            input.radii = radii;
            input.windingOrder = windingOrder;
            input.allowTrimming = allowTriming;
            input.createInteriorGeometry = createInteriorGeometry;
            input.inWorldSpace = inWorldSpace;

            BuildShape(ref input);
        }

        public override void SetShape(NativeArray<Vector3> vertices, NativeArray<int> indices, ShadowShape2D.OutlineTopology outlineTopology, ShadowShape2D.WindingOrder windingOrder = ShadowShape2D.WindingOrder.Clockwise, bool allowTrimming = true,  bool createInteriorGeometry = false, bool inWorldSpace = false)
        {
            ShadowShapeInput input = CreateInput(ShadowShapeKind.Topology, vertices, indices);
            input.topology = outlineTopology;
            input.windingOrder = windingOrder;
            input.allowTrimming = allowTrimming;
            input.createInteriorGeometry = createInteriorGeometry;
            input.inWorldSpace = inWorldSpace;

            BuildShape(ref input);
        }

        public override void SetShapeDirect(NativeArray<Vector3> vertices, NativeArray<int> indices, bool inWorldSpace = false)
        {
            ShadowShapeInput input = CreateInput(ShadowShapeKind.Direct, vertices, indices);
            input.inWorldSpace = inWorldSpace;

            BuildShape(ref input);
        }


        public void SetShapeWithLines(NativeArray<Vector3> vertices, NativeArray<int> indices, bool allowTrimming)
        {
            SetShape(vertices, indices, ShadowShape2D.OutlineTopology.Lines, allowTrimming: allowTrimming);
        }

        public override void SetFlip(bool flipX, bool flipY)
        {
            m_FlipX = flipX;
            m_FlipY = flipY;
        }

        public override void GetFlip(out bool flipX, out bool flipY)
        {
            flipX = m_FlipX;
            flipY = m_FlipY;

        }

        public override void SetDefaultTrim(float trim)
        {
            m_InitialTrim = trim;
        }

#if UNITY_EDITOR
        internal void DrawPreviewOutline(Matrix4x4 outlineMatrix, float trimDistance)
        {
            ShadowGeometryGeneratorRegistry.Get(m_GeneratedId).DrawPreviewOutline(mesh, outlineMatrix, trimDistance);
        }
#endif

        public void UpdateBoundingSphere(Transform transform)
        {
            var maxBound = m_LocalBounds.max;
            var minBound = m_LocalBounds.min;

            if (isTransformable)
            {
                maxBound = transform.TransformPoint(m_LocalBounds.max);
                minBound = transform.TransformPoint(m_LocalBounds.min);
            }

            var center = 0.5f * (maxBound + minBound);
            var radius = Vector3.Magnitude(maxBound - center);

            m_BoundingSphere = new BoundingSphere(center, radius);
        }

        ~ShadowMesh2D()
        {
            if(m_NativeIndices.IsCreated)
                m_NativeIndices.Dispose();

            if(m_NativeVertices.IsCreated)
                m_NativeVertices.Dispose();

            if(m_NativeCustomVertices.IsCreated)
                m_NativeCustomVertices.Dispose();
        }
    }
}
