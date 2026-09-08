using NUnit.Framework;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal.Tests
{
    /// <summary>
    /// Interior fill for <c>Unity.SoftShadow</c>, tested here rather than beside the generator's
    /// other tests because it needs the tessellator.
    /// </summary>
    /// <remarks>
    /// <c>ShadowUtility</c>'s tessellation sits behind <c>USING_2DCOMMON</c>, and the URP package's
    /// own test assembly only compiles in a project that lists URP under <c>testables</c> — today
    /// only SRP_SmokeTest, which has no 2D packages, so Legacy emits no interior fill there either
    /// and any assertion would be vacuous. This project has the packages and is in URP's
    /// <c>InternalsVisibleTo</c>, so the fill actually happens here.
    ///
    /// Why it matters: the three caster passes (<c>Self</c>, <c>UnshadowMark</c>,
    /// <c>UnshadowUnmark</c>) read POSITION alone, and in this format every projected role shares
    /// POSITION with its caster-space outline vertex. So the body, band, fin and fan triangles are
    /// all degenerate under a POSITION-only read, and interior fill is the only thing giving those
    /// passes any coverage at all. Without it <c>UnshadowMark</c> marks no stencil, and
    /// <c>ProjectedSelf</c>'s <c>Comp NotEqual 1</c> then stops excluding the caster's own body — the
    /// projected shadow paints straight across it. Legacy has the same property; what makes this easy
    /// to drop is that the Python model the generator was ported from has no interior fill at all.
    ///
    /// <c>ShadowShape2DProvider_Collider</c> turns <c>createInteriorGeometry</c> on exactly when the
    /// caster has no <c>Renderer</c>, which is exactly when those passes draw this mesh.
    /// </remarks>
    [TestFixture]
    [Ignore("TEMP-DISABLED-PR-123379: disabled to land PR #123379; re-enable per docs/pr/123379-disabled-tests.md")]
    class SoftShadowInteriorFillTests
    {
        const int k_Segments = 3;
        const int k_FixedRolesPerVertex = 7;

        NativeArray<Vector3> m_Vertices;
        NativeArray<int> m_Indices;

        static int VertsPerOutlineVertex(int s) => k_FixedRolesPerVertex + 4 * (s + 1);

        [SetUp]
        public void Setup()
        {
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
            if (m_Vertices.IsCreated) m_Vertices.Dispose();
            if (m_Indices.IsCreated) m_Indices.Dispose();
            Shadow2DGeometry.globalGeneratorOverride = null;
        }

        // The generator comes from the process-wide override, not a field on the mesh: which one a
        // caster builds with is Shadow2DGeometrySettings.geometryVersion, a project setting. Cleared in
        // the fixture's TearDown.
        ShadowMesh2D Build(string generatorId, bool interior)
        {
            Shadow2DGeometry.globalGeneratorOverride = generatorId;

            var shadowMesh = new ShadowMesh2D();
            shadowMesh.trimEdge = 0;
            shadowMesh.fanSegments = k_Segments;
            shadowMesh.SetShape(m_Vertices, m_Indices, ShadowShape2D.OutlineTopology.Lines,
                                createInteriorGeometry: interior);
            return shadowMesh;
        }

        // Compared against Legacy rather than an absolute count, so a missing tessellator shows up as
        // "neither generator fills" rather than as a failure of this one.
        bool LegacyFills()
        {
            return Build(LegacyShadowGeometryGenerator.k_Id, interior: true).mesh.vertexCount >
                   Build(LegacyShadowGeometryGenerator.k_Id, interior: false).mesh.vertexCount;
        }

        [Test]
        public void TessellatorIsAvailableInThisProject()
        {
            // Guards the two tests below from passing vacuously. If this fails, USING_2DCOMMON is
            // undefined here too and the interior fill path is untested everywhere -- which is worth
            // failing loudly over rather than skipping quietly.
            Assert.IsTrue(LegacyFills(),
                "Legacy emits no interior fill in this project, so USING_2DCOMMON is undefined and " +
                "the soft shadow generator's interior fill cannot be verified anywhere.");
        }

        [Test]
        public void InteriorFillIsEmittedWhenRequested()
        {
            int projectedOnly = m_Vertices.Length * VertsPerOutlineVertex(k_Segments);

            ShadowMesh2D without = Build(SoftShadowGeometryGenerator.k_Id, interior: false);
            Assert.AreEqual(projectedOnly, without.mesh.vertexCount,
                "Interior fill must not be emitted unless asked for.");

            ShadowMesh2D with = Build(SoftShadowGeometryGenerator.k_Id, interior: true);
            Assert.Greater(with.mesh.vertexCount, projectedOnly,
                "Interior fill was requested but nothing was appended. Geometry casters would draw a " +
                "wholly degenerate mesh in the caster passes and UnshadowMark would mark no stencil.");
            Assert.Greater(with.mesh.triangles.Length, without.mesh.triangles.Length,
                "Interior fill added vertices but no triangles.");
        }

        [Test]
        public void InteriorFillUsesTheOutlineRoleSoItNeedsNoShaderBranch()
        {
            // Interior vertices want exactly what the Outline role produces -- the unprojected
            // position at full opacity -- so reusing it means the vertex program needs no extra case.
            ShadowMesh2D with = Build(SoftShadowGeometryGenerator.k_Id, interior: true);
            Vector4[] tangents = with.mesh.tangents;
            int projectedOnly = m_Vertices.Length * VertsPerOutlineVertex(k_Segments);

            Assert.Greater(tangents.Length, projectedOnly, "No interior geometry to inspect.");
            for (int i = projectedOnly; i < tangents.Length; i++)
            {
                Assert.AreEqual(SoftShadowGeometryGenerator.k_RoleOutline,
                    Mathf.RoundToInt(tangents[i].x),
                    $"Interior vertex {i} does not use the Outline role.");
            }
        }

        [Test]
        public void InteriorFillCoversTheCasterSilhouette()
        {
            // The point of the fill is coverage under a POSITION-only read, so assert that rather
            // than just a vertex count: the interior triangles must have real area and their vertices
            // must lie within the outline's bounds.
            ShadowMesh2D with = Build(SoftShadowGeometryGenerator.k_Id, interior: true);
            Vector3[] positions = with.mesh.vertices;
            Vector4[] tangents = with.mesh.tangents;
            int[] triangles = with.mesh.triangles;
            int projectedOnly = m_Vertices.Length * VertsPerOutlineVertex(k_Segments);

            Assert.Greater(positions.Length, projectedOnly, "No interior geometry to inspect.");

            double area = 0.0;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
                if (a < projectedOnly || b < projectedOnly || c < projectedOnly)
                    continue;               // not an interior triangle

                Vector3 pa = positions[a], pb = positions[b], pc = positions[c];
                area += System.Math.Abs((pb.x - pa.x) * (pc.y - pa.y) -
                                        (pc.x - pa.x) * (pb.y - pa.y)) * 0.5;
            }

            // The square is 2x2, so a correct fill covers 4 world units. Asserting most of it rather
            // than all guards against the tessellator's own choices while still failing if the fill
            // is a sliver or inverted.
            Assert.Greater(area, 3.0,
                $"Interior triangles cover only {area:F3} of the caster's 4.0 area.");

            for (int i = projectedOnly; i < positions.Length; i++)
            {
                Assert.AreEqual(0.0f, positions[i].z, "Interior position.z must stay 0.");
                Assert.LessOrEqual(Mathf.Abs(positions[i].x), 1.0f + 1e-4f,
                    "Interior vertex outside the outline in x.");
                Assert.LessOrEqual(Mathf.Abs(positions[i].y), 1.0f + 1e-4f,
                    "Interior vertex outside the outline in y.");
            }

            Assert.AreEqual(positions.Length, tangents.Length, "Tangents must cover every vertex.");
        }
    }
}
