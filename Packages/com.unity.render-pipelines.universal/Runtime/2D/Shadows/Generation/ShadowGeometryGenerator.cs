using Unity.Collections;
using static UnityEngine.Rendering.Universal.ShadowUtility;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Which <c>ShadowShape2D.SetShape</c> entry point produced a <see cref="ShadowShapeInput"/>.
    /// The four entry points differ in more than their arguments (see the per-kind notes in
    /// <see cref="LegacyShadowGeometryGenerator"/>), so the generator has to be able to tell
    /// them apart.
    /// </summary>
    internal enum ShadowShapeKind
    {
        /// <summary>Radii, plus a transform applied after the shape is expanded.</summary>
        RadiiWithTransform,
        /// <summary>Radii, no output transform.</summary>
        Radii,
        /// <summary>Raw outline described by an <see cref="ShadowShape2D.OutlineTopology"/>.</summary>
        Topology,
        /// <summary>Pre-processed geometry: no edge extraction, winding fixup or clipping.</summary>
        Direct,
    }

    /// <summary>
    /// Everything a generator needs in order to build shadow geometry. Kept blittable
    /// (no nullables, no managed references) so it can be passed into Burst code later.
    /// </summary>
    internal struct ShadowShapeInput
    {
        internal ShadowShapeKind kind;

        internal NativeArray<Vector3> vertices;
        internal NativeArray<int> indices;
        internal NativeArray<float> radii;

        /// <summary>
        /// Triangles per fan for generators that tessellate curved shadow features. Ignored by
        /// generators that have none. Always at least
        /// <see cref="ShadowMesh2D.k_MinFanSegments"/> — zero is not a legal value: it leaves the
        /// outer shadow boundary disconnected, and for a concave fillet it removes the band setback's
        /// replacement arc entirely.
        /// </summary>
        internal int fanSegments;

        internal Matrix4x4 outputTransform;

        internal ShadowShape2D.OutlineTopology topology;
        internal ShadowShape2D.WindingOrder windingOrder;

        /// <summary>Current trim, possibly <see cref="ShadowMesh2D.k_TrimEdgeUninitialized"/>.</summary>
        internal float trimEdge;
        /// <summary>Value <see cref="trimEdge"/> resolves to the first time it is used.</summary>
        internal float initialTrim;

        internal bool allowTrimming;
        internal bool createInteriorGeometry;
        internal bool inWorldSpace;
    }

    /// <summary>
    /// What a generator produces. Also carries the state the old inline implementation used to
    /// write straight back into <see cref="ShadowMesh2D"/>; the caller applies it after the
    /// build so that <see cref="ShadowMesh2D"/> remains the only owner of the geometry buffers.
    /// </summary>
    internal struct ShadowGeometryResult
    {
        /// <summary>
        /// Seeded with the caller's current buffers and handed back containing the new ones.
        /// <see cref="ShadowUtility.GenerateShadowGeometry"/> disposes whatever it is given, so
        /// ownership transfers in both directions through these two fields.
        /// </summary>
        internal NativeArray<ShadowMeshVertex> vertices;
        internal NativeArray<int> indices;

        /// <summary>
        /// Vertex buffer for generators whose format is not <see cref="ShadowMeshVertex"/>. Such a
        /// generator writes this and leaves <see cref="vertices"/> alone; the two are never both in
        /// use. <see cref="customVertexCount"/> is the vertex count, not the float count.
        /// </summary>
        /// <remarks>
        /// Typed as <c>float</c> rather than <c>byte</c> on purpose. <c>NativeArray.Reinterpret</c>
        /// performs no alignment check — it wraps the existing pointer — and a
        /// <c>NativeArray&lt;byte&gt;</c> allocates with <c>AlignOf&lt;byte&gt;() == 1</c>, so a
        /// byte-backed buffer can hand a 1-byte-aligned pointer to a struct of floats. Float gives
        /// 4-byte alignment by construction and every payload struct is a whole number of floats.
        ///
        /// Ownership travels in and back out exactly as it does for <see cref="vertices"/>. Views
        /// obtained from <c>Reinterpret</c> alias the same allocation and share its safety handle, so
        /// only this buffer may be disposed — never a view of it.
        ///
        /// Pinned by <c>SoftShadowVertexFormatTests</c> in the UniversalGraphicsTest_2D project.
        /// </remarks>
        internal NativeArray<float> customVertices;
        internal int customVertexCount;

        internal Bounds localBounds;

        /// <summary>Write-back: trim after any <c>initialTrim</c> resolution.</summary>
        internal float trimEdge;
        /// <summary>Write-back: whether the caster's transform may be applied to the geometry.</summary>
        internal bool isTransformable;

        /// <summary>The caller must call <c>Clear()</c> once the build has finished.</summary>
        internal bool cleared;
        /// <summary>The caller must adopt <see cref="localBounds"/>.</summary>
        internal bool localBoundsAssigned;
        /// <summary>
        /// The caller must mark the mesh dirty. Deliberately separate from
        /// <see cref="localBoundsAssigned"/>: the clipped-to-nothing branch assigns empty bounds
        /// without marking dirty, and that difference is observable.
        /// </summary>
        internal bool markDirty;
        /// <summary>This entry point uploads the mesh eagerly rather than leaving it to the lazy getter.</summary>
        internal bool uploadMesh;
    }

    /// <summary>
    /// One way of generating 2D shadow geometry. Implementations own the whole pipeline from the
    /// caller's input shape through to the finished vertex buffer, plus the vertex format that
    /// buffer is in and how it reaches the <see cref="Mesh"/>.
    /// </summary>
    /// <remarks>
    /// Subclasses are discovered by reflection in the editor and baked into
    /// <c>Shadow2DGeometrySettings</c>, so a third party only has to declare a type with a unique
    /// <see cref="id"/> -- there is no list to edit. Instances are shared process-wide and survive
    /// domain reload, so they must be stateless apart from readonly caches.
    ///
    /// Concrete generators must be <c>[Serializable]</c> and have a parameterless constructor:
    /// the baked list is persisted with <c>[SerializeReference]</c>, and Unity silently stores
    /// nothing for a type that is not serializable.
    /// </remarks>
    [System.Serializable]
    internal abstract class ShadowGeometryGenerator
    {
        /// <summary>
        /// Stable identity for this generator. This is the registry key, the value serialized on
        /// every caster, and the name a shader uses to declare compatibility, so it must never
        /// change once shipped.
        /// </summary>
        /// <remarks>
        /// Vendor-prefixed, e.g. "Acme.SoftShadow". The <c>Unity.</c> prefix is reserved for
        /// generators shipped with the package. Because the id implies the vertex layout, a
        /// layout change requires a NEW id rather than a version bump -- register the old
        /// generator alongside the new one so existing content keeps working.
        /// </remarks>
        internal abstract string id { get; }

        /// <summary>
        /// Bumped whenever this generator would produce different mesh bytes for the same input.
        /// </summary>
        /// <remarks>
        /// Never part of the registry key and never named in a shader tag -- it exists so that
        /// geometry baked into a scene or prefab by an older version is detected as stale and
        /// rebuilt. Bump it freely for algorithm changes; bump the <see cref="id"/> instead if
        /// the vertex layout changes.
        /// </remarks>
        internal abstract int version { get; }

        /// <summary>Name shown in the ShadowCaster2D inspector.</summary>
        internal abstract string displayName { get; }

        /// <summary>
        /// Whether geometry this generator produces may be written to disk.
        /// </summary>
        /// <remarks>
        /// False means <see cref="ShadowMesh2D"/> skips the copy in <c>OnBeforeSerialize</c> and
        /// rebuilds on load instead. Worth turning off for a format that is large relative to the
        /// outline it came from, or one still changing shape: baked geometry is stored as YAML in
        /// every scene and prefab that contains a caster.
        /// </remarks>
        internal virtual bool persistsGeometry => true;

        /// <summary>
        /// Whether this generator tessellates curved shadow features, and so honours
        /// <see cref="ShadowShapeInput.fanSegments"/>. Drives whether the inspector offers the
        /// control at all.
        /// </summary>
        internal virtual bool usesFanSegments => false;

        /// <summary>
        /// Vertex format this generator emits. Must return a cached array — this is read on the
        /// mesh-upload path and allocating here would show up as per-rebuild garbage.
        /// </summary>
        internal abstract VertexAttributeDescriptor[] vertexLayout { get; }

        /// <summary>Builds shadow geometry for <paramref name="input"/> into <paramref name="result"/>.</summary>
        internal abstract void Build(ref ShadowShapeInput input, ref ShadowGeometryResult result);

        /// <summary>Pushes <paramref name="result"/> into <paramref name="mesh"/> using this generator's vertex layout.</summary>
        internal abstract void UploadMesh(ref Mesh mesh, in ShadowGeometryResult result);

#if UNITY_EDITOR
        /// <summary>
        /// Draws the scene-view outline for a mesh this generator produced. Lives here because
        /// it has to decode whatever vertex encoding the generator chose.
        /// </summary>
        internal abstract void DrawPreviewOutline(Mesh mesh, Matrix4x4 outlineMatrix, float trimDistance);
#endif
    }
}
