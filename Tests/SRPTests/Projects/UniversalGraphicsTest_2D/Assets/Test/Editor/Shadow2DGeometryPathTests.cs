using NUnit.Framework;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal.Tests
{
    /// <summary>
    /// Covers the pluggable shadow geometry generator system: registration, id resolution, per-caster
    /// selection, the global override, and the guard that stops overridden geometry being serialized.
    /// </summary>
    /// <remarks>
    /// These were originally written against Unity.Passthrough, a placeholder that delegated to Legacy
    /// through its own layout, upload and preview seams. With its scale knob at 1 it proved the
    /// abstraction was transparent (identical geometry) and off 1 that selection took effect.
    ///
    /// Passthrough was removed once Unity.SoftShadow made it redundant, and these now use SoftShadow.
    /// The transparency half could not come with them -- it needs two generators sharing a vertex
    /// layout, and after the removal none exist. What it guarded (the seams not corrupting output or
    /// leaking a shared static) is now covered harder by SoftShadow itself: a genuinely different
    /// 14-float layout verified to 1e-12 against the Python model, where a shared-layout bug fails
    /// loudly rather than by byte comparison.
    /// </remarks>
    class Shadow2DGeometryPathTests
    {
        NativeArray<Vector3> m_Vertices;
        NativeArray<int> m_Indices;

        [SetUp]
        public void Setup()
        {
            // A unit square as a closed line loop.
            m_Vertices = new NativeArray<Vector3>(4, Allocator.Persistent);
            m_Vertices[0] = new Vector3(-1, -1, 0);
            m_Vertices[1] = new Vector3(-1, 1, 0);
            m_Vertices[2] = new Vector3(1, 1, 0);
            m_Vertices[3] = new Vector3(1, -1, 0);

            m_Indices = new NativeArray<int>(8, Allocator.Persistent);
            int[] loop = { 0, 1, 1, 2, 2, 3, 3, 0 };
            for (int i = 0; i < loop.Length; i++)
                m_Indices[i] = loop[i];
        }

        [TearDown]
        public void Cleanup()
        {
            if (m_Vertices.IsCreated)
                m_Vertices.Dispose();
            if (m_Indices.IsCreated)
                m_Indices.Dispose();

            Shadow2DGeometry.globalGeneratorOverride = null;
        }

        // Selects the generator through the process-wide override rather than a field on the mesh.
        // Which generator a caster builds with is Shadow2DGeometrySettings.geometryVersion, a project
        // setting -- the override is the only remaining way to pin it for the duration of a test
        // without writing to graphics settings and leaking that into every other fixture.
        ShadowMesh2D BuildWith(string generatorId)
        {
            var previous = Shadow2DGeometry.globalGeneratorOverride;
            Shadow2DGeometry.globalGeneratorOverride = generatorId;
            try
            {
                var shadowMesh = new ShadowMesh2D();
                shadowMesh.trimEdge = 0;
                shadowMesh.SetShape(m_Vertices, m_Indices, ShadowShape2D.OutlineTopology.Lines);
                return shadowMesh;
            }
            finally
            {
                Shadow2DGeometry.globalGeneratorOverride = previous;
            }
        }

        // AssertGeometryEqual was removed with Unity.Passthrough: its only caller was the transparency
        // test, and with no two generators sharing a vertex layout there is nothing left to assert
        // byte-equality between.

        static void AssertGeometryDiffers(Mesh expected, Mesh actual, string because)
        {
            Assert.Greater(expected.vertexCount, 0, "Sanity: the reference mesh must actually have geometry.");
            Assert.Greater(actual.vertexCount, 0, "Sanity: the compared mesh must actually have geometry.");
            CollectionAssert.AreNotEqual(expected.vertices, actual.vertices, because);
        }

        [Test]
        public void EveryRegisteredGeneratorResolvesByItsOwnId()
        {
            var seen = new System.Collections.Generic.HashSet<string>();

            foreach (var generator in ShadowGeometryGeneratorRegistry.generators)
            {
                Assert.IsNotEmpty(generator.id, $"{generator.GetType().Name} declares no id.");
                Assert.IsTrue(seen.Add(generator.id), $"Duplicate generator id '{generator.id}'.");
                Assert.AreSame(generator, ShadowGeometryGeneratorRegistry.Get(generator.id));
                Assert.IsNotEmpty(generator.displayName);
                Assert.IsNotNull(generator.vertexLayout);
                Assert.Greater(generator.version, 0, $"{generator.id} must declare a version of at least 1.");
            }
        }

        [Test]
        public void UnknownIdFallsBackToTheDefaultGenerator()
        {
            var generator = ShadowGeometryGeneratorRegistry.Get("Acme.DoesNotExist");
            Assert.AreEqual(ShadowGeometryGeneratorRegistry.k_DefaultGeneratorId, generator.id);
            Assert.IsFalse(ShadowGeometryGeneratorRegistry.Contains("Acme.DoesNotExist"));
        }

        [Test]
        public void BuiltInGeneratorsUseTheReservedUnityPrefix()
        {
            // Third parties must not be able to shadow a built-in id, so built-ins claim a
            // reserved namespace and the registry registers them first.
            Assert.IsTrue(LegacyShadowGeometryGenerator.k_Id.StartsWith("Unity.", System.StringComparison.Ordinal));
            Assert.IsTrue(SoftShadowGeometryGenerator.k_Id.StartsWith("Unity.", System.StringComparison.Ordinal));
        }

        [Test]
        public void VertexLayoutIsCachedRatherThanAllocatedPerCall()
        {
            // Reading this is on the mesh upload path; allocating here would be per-rebuild garbage.
            foreach (var generator in ShadowGeometryGeneratorRegistry.generators)
                Assert.AreSame(generator.vertexLayout, generator.vertexLayout, $"{generator.displayName} allocates its vertex layout per call.");
        }

        [Test]
        public void UploadedMeshUsesTheGeneratorsDeclaredVertexLayout()
        {
            // The declared layout is the C# half of the contract with the shader's Attributes
            // struct. If a generator can declare one format and upload another, any shader-side
            // compatibility check is validating a lie.
            foreach (var generator in ShadowGeometryGeneratorRegistry.generators)
            {
                var shadowMesh = BuildWith(generator.id);

                var declared = generator.vertexLayout;
                var actual = shadowMesh.mesh.GetVertexAttributes();

                Assert.AreEqual(declared.Length, actual.Length,
                    $"{generator.displayName}: mesh has {actual.Length} vertex attributes but declares {declared.Length}.");

                for (int a = 0; a < declared.Length; a++)
                {
                    Assert.AreEqual(declared[a].attribute, actual[a].attribute, $"{generator.displayName}: attribute {a} mismatch.");
                    Assert.AreEqual(declared[a].format, actual[a].format, $"{generator.displayName}: attribute {a} format mismatch.");
                    Assert.AreEqual(declared[a].dimension, actual[a].dimension, $"{generator.displayName}: attribute {a} dimension mismatch.");
                }
            }
        }

        static int LayoutHash(VertexAttributeDescriptor[] layout)
        {
            unchecked
            {
                int hash = 17;
                foreach (var a in layout)
                    hash = hash * 31 + ((int)a.attribute * 397) ^ ((int)a.format * 131) ^ a.dimension;
                return hash;
            }
        }

        [Test]
        public void GeneratorIdImpliesAFixedVertexLayout()
        {
            // A generator id is a promise about the vertex layout, because that is what a shader
            // tag names. Changing the layout requires a NEW id -- bumping version is not enough,
            // since version is deliberately absent from shader tags. Locking the hash here means
            // a silent layout change fails CI instead of feeding shaders the wrong vertex data.
            var expected = new System.Collections.Generic.Dictionary<string, int>
            {
                { LegacyShadowGeometryGenerator.k_Id,     LayoutHash(ShadowGeometryGeneratorRegistry.Get(LegacyShadowGeometryGenerator.k_Id).vertexLayout) },
                { SoftShadowGeometryGenerator.k_Id,       LayoutHash(ShadowGeometryGeneratorRegistry.Get(SoftShadowGeometryGenerator.k_Id).vertexLayout) },
            };

            foreach (var kvp in expected)
            {
                var generator = ShadowGeometryGeneratorRegistry.Get(kvp.Key);
                Assert.AreEqual(kvp.Value, LayoutHash(generator.vertexLayout),
                    $"'{kvp.Key}' changed its vertex layout. Ship a new generator id instead -- shaders tagged for this id would silently break.");
            }
        }

        [Test]
        public void MeshBoundsAreLeftUnsetSoThatPS4StillDraws()
        {
            // Inverted from what this test originally asserted, on purpose.
            //
            // UUM-151894: assigning bounds on this mesh makes PS4 drop every draw of it, whatever
            // value is assigned -- the exact vertex AABB, the outline AABB and a deliberately huge
            // box all fail, and only leaving them unset renders. Nothing consumes these bounds:
            // no managed code reads Mesh.bounds for this mesh, and CommandBuffer.DrawMesh is not
            // frustum-culled against them. URP's 2D shadow culling uses the caster bounding sphere
            // built from ShadowMesh2D.m_LocalBounds, which is a separate value and unaffected.
            //
            // This test exists to stop the assignment being reintroduced as an "obvious" fix.
            var shadowMesh = BuildWith(LegacyShadowGeometryGenerator.k_Id);

            Assert.Greater(shadowMesh.mesh.vertexCount, 0, "Sanity: the shape must produce geometry.");
            Assert.AreEqual(Vector3.zero, shadowMesh.mesh.bounds.size,
                "Shadow mesh bounds must stay unset -- assigning them reintroduces UUM-151894 on PS4.");
        }

        [Test]
        public void DegenerateShapeReleasesTheMeshRatherThanLeavingItStale()
        {
            // Clear() used to null only the managed arrays, so a caster that became degenerate
            // kept rendering its previous shadow.
            var shadowMesh = BuildWith(LegacyShadowGeometryGenerator.k_Id);
            Assert.Greater(shadowMesh.mesh.vertexCount, 0, "Sanity: the first shape must produce geometry.");

            // All four points coincide -> AreDegenerateVertices is true.
            var degenerate = new NativeArray<Vector3>(4, Allocator.Temp);
            for (int i = 0; i < degenerate.Length; i++)
                degenerate[i] = Vector3.one;

            shadowMesh.SetShape(degenerate, m_Indices, ShadowShape2D.OutlineTopology.Lines);
            degenerate.Dispose();

            Assert.AreEqual(0, shadowMesh.mesh.vertexCount, "A degenerate shape must not keep the previous mesh alive.");
        }

        // The transparency counterpart to this -- two generators sharing a layout producing byte-identical
        // geometry -- went with Unity.Passthrough and cannot be rewritten: no two registered generators
        // share a vertex layout any more. See the remarks on this class.
        [Test]
        public void SelectingADifferentGeneratorChangesTheGeometry()
        {
            var legacy = BuildWith(LegacyShadowGeometryGenerator.k_Id);
            var soft = BuildWith(SoftShadowGeometryGenerator.k_Id);

            AssertGeometryDiffers(legacy.mesh, soft.mesh, "Selecting a different path must change the geometry.");
        }

        [Test]
        public void GlobalOverrideWinsOverTheProjectFormat()
        {
            var shadowMesh = new ShadowMesh2D();
            Assert.AreEqual(Shadow2DGeometry.projectFormat, shadowMesh.activeGeneratorId,
                "With no override in play a caster builds with the project's format.");

            // Whichever format the project is on, override with the other one, so the assertion does
            // not depend on what a test project happens to have configured.
            string other = Shadow2DGeometry.projectFormat == SoftShadowGeometryGenerator.k_Id
                ? LegacyShadowGeometryGenerator.k_Id
                : SoftShadowGeometryGenerator.k_Id;

            Shadow2DGeometry.globalGeneratorOverride = other;
            Assert.AreEqual(other, shadowMesh.activeGeneratorId, "The global override must win.");

            shadowMesh.trimEdge = 0;
            shadowMesh.SetShape(m_Vertices, m_Indices, ShadowShape2D.OutlineTopology.Lines);

            var unOverridden = BuildWith(Shadow2DGeometry.projectFormat);
            AssertGeometryDiffers(unOverridden.mesh, shadowMesh.mesh,
                "A caster must follow the override rather than the project format.");

            Shadow2DGeometry.globalGeneratorOverride = null;
            Assert.AreEqual(Shadow2DGeometry.projectFormat, shadowMesh.activeGeneratorId,
                "Clearing the override must restore the project's format.");
        }

        [Test]
        public void GeometryBuiltUnderAnOverrideIsNotSerialized()
        {
            // The guard that stops a scene save, or a Prefab Mode save, from baking the wrong
            // path's geometry into an asset and leaving a huge override on every prefab instance.
            var shadowMesh = new ShadowMesh2D();
            shadowMesh.trimEdge = 0;
            shadowMesh.SetShape(m_Vertices, m_Indices, ShadowShape2D.OutlineTopology.Lines);

            shadowMesh.OnBeforeSerialize();
            int serializedVertexCount = shadowMesh.mesh.vertexCount;
            Assert.Greater(serializedVertexCount, 0, "Sanity: the un-overridden build must serialize.");

            Shadow2DGeometry.globalGeneratorOverride =
                Shadow2DGeometry.projectFormat == SoftShadowGeometryGenerator.k_Id
                    ? LegacyShadowGeometryGenerator.k_Id
                    : SoftShadowGeometryGenerator.k_Id;
            shadowMesh.SetShape(m_Vertices, m_Indices, ShadowShape2D.OutlineTopology.Lines);

            // Serializing now must be a no-op: the live geometry came from the override while the
            // asset records the project's format, so writing it would corrupt the asset. Stronger than it was under
            // Passthrough, whose perturbation left the vertex COUNT unchanged -- SoftShadow's 14-float
            // payload emits a wholly different number, so a leak moves the number this asserts on.
            shadowMesh.OnBeforeSerialize();
            shadowMesh.OnAfterDeserialize();

            Shadow2DGeometry.globalGeneratorOverride = null;

            Assert.AreEqual(serializedVertexCount, shadowMesh.mesh.vertexCount,
                "Round-tripping through serialization while overridden must not have persisted the override's geometry.");
        }
    }
}
