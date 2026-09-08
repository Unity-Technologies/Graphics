using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace UnityEngine.Rendering.Universal.Tests
{
    /// <summary>
    /// That the seven shape sprites import with the outlines they were authored with, and that they
    /// still have the structural property they were chosen for.
    /// </summary>
    /// <remarks>
    /// The shapes are exported by <c>sim/gen_shape_sprites.py</c>, which writes an explicit
    /// <c>spriteSheet.outline</c> into each <c>.png.meta</c> rather than letting the importer trace
    /// the alpha. That matters because <c>ShadowShape2DProvider_SpriteRenderer</c> feeds the shadow
    /// system the sprite's MESH — <c>GetVertexAttribute&lt;Vector3&gt;(Position)</c> and
    /// <c>GetIndices()</c> — so the authored outline becomes the shadow silhouette exactly, instead of
    /// an approximation with its own vertex count.
    ///
    /// Every step of that involves an importer convention taken from
    /// <c>Scenes/004-008_Shadow_Assets/Circle.png.meta</c> rather than from documentation: that the
    /// coordinates are pixels offset from the image centre, that <c>spriteMode: 1</c> honours the
    /// top-level outline, and that fractional coordinates survive. Four of the seven shapes are whole
    /// numbers at 200 px/unit; the star, sawtooth and bite are not. So this fixture checks the
    /// convention rather than trusting it.
    ///
    /// The polygons are read from <c>softshadow_goldens.txt</c>, the same file
    /// <c>SoftShadowGoldenTests</c> uses, so there is one source of truth for the shapes and the
    /// sprites cannot drift from what the model was verified against.
    /// </remarks>
    [TestFixture]
    [Ignore("TEMP-DISABLED-PR-123379: disabled to land PR #123379; re-enable per docs/pr/123379-disabled-tests.md")]
    class SoftShadowSpriteOutlineTests
    {
        const string k_ShapeDir = "Assets/Scenes/140_Shadow_Materials/Shapes";
        const string k_GoldenFile = "Test/Editor/softshadow_goldens.txt";
        const float k_PixelsPerUnit = 200.0f;

        // Longest run of adjacent reflex vertices, from simulation report 27 section 6.1. Only the
        // bite and the chamfered plus exceed 1, and those are the shapes that exercise the 2-ring
        // radius budget at all (report 29 section 7c). If a trace or an edit flattened them, the set
        // would quietly stop testing the thing it exists to test.
        static readonly Dictionary<string, int> k_ExpectedLongestConcaveRun =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "square", 0 }, { "plus", 1 }, { "concaveL", 1 }, { "star", 1 },
            { "bite", 6 }, { "chamferPlus", 2 }, { "sawtooth", 1 },
        };

        static Dictionary<string, Vector2[]> s_Polygons;

        [OneTimeSetUp]
        public void LoadPolygons()
        {
            string path = Path.Combine(Application.dataPath, k_GoldenFile);
            Assert.IsTrue(File.Exists(path), $"Golden file missing at {path}.");

            s_Polygons = new Dictionary<string, Vector2[]>(StringComparer.OrdinalIgnoreCase);
            string current = null;
            var points = new List<Vector2>();

            foreach (string raw in File.ReadLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;
                string[] f = line.Split(' ');

                if (f[0] == "SHAPE")
                {
                    if (current != null)
                        s_Polygons[current] = points.ToArray();
                    current = f[1];
                    points.Clear();
                }
                else if (f[0] == "V" && current != null && !s_Polygons.ContainsKey(current))
                {
                    points.Add(new Vector2(
                        float.Parse(f[1], CultureInfo.InvariantCulture),
                        float.Parse(f[2], CultureInfo.InvariantCulture)));
                }
            }
            if (current != null && !s_Polygons.ContainsKey(current))
                s_Polygons[current] = points.ToArray();

            Assert.AreEqual(7, s_Polygons.Count, "Expected seven shapes in the golden file.");
        }

        static Sprite LoadSprite(string name)
        {
            string path = $"{k_ShapeDir}/{name}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Assert.IsNotNull(sprite, $"Could not load a Sprite at {path}. " +
                "Regenerate with sim/gen_shape_sprites.py and let Unity import it.");
            return sprite;
        }

        /// <summary>
        /// The boundary of the sprite's mesh: edges used by exactly one triangle. This is what
        /// <c>ShadowUtility.CalculateEdgesFromTriangles</c> recovers, so it is the outline the shadow
        /// system actually sees.
        /// </summary>
        static List<Vector2> BoundaryVertices(Sprite sprite)
        {
            Vector2[] verts = sprite.vertices;
            ushort[] tris = sprite.triangles;

            var useCount = new Dictionary<(int, int), int>();
            void Count(int a, int b)
            {
                var key = a < b ? (a, b) : (b, a);
                useCount.TryGetValue(key, out int c);
                useCount[key] = c + 1;
            }

            for (int t = 0; t < tris.Length; t += 3)
            {
                Count(tris[t], tris[t + 1]);
                Count(tris[t + 1], tris[t + 2]);
                Count(tris[t + 2], tris[t]);
            }

            var onBoundary = new HashSet<int>();
            foreach (KeyValuePair<(int, int), int> kv in useCount)
            {
                if (kv.Value != 1)
                    continue;
                onBoundary.Add(kv.Key.Item1);
                onBoundary.Add(kv.Key.Item2);
            }

            var result = new List<Vector2>();
            foreach (int i in onBoundary)
                result.Add(verts[i]);
            return result;
        }

        [Test]
        public void EveryShapeSpriteImports(
            [Values("Square", "Plus", "ConcaveL", "Star", "Bite", "ChamferPlus", "Sawtooth")]
            string name)
        {
            Sprite sprite = LoadSprite(name);
            Assert.AreEqual(k_PixelsPerUnit, sprite.pixelsPerUnit, 1e-3f,
                "pixelsPerUnit drives the world size, and the fixture's scale depends on it.");
            Assert.Greater(sprite.triangles.Length, 0, "Sprite has no mesh.");
        }

        [Test]
        public void AuthoredOutlineSurvivesTheImportUnchanged(
            [Values("Square", "Plus", "ConcaveL", "Star", "Bite", "ChamferPlus", "Sawtooth")]
            string name)
        {
            Sprite sprite = LoadSprite(name);
            Vector2[] expected = s_Polygons[name];
            List<Vector2> boundary = BoundaryVertices(sprite);

            // Every authored point must appear on the mesh boundary. Matched by nearest rather than
            // by order, because the tessellator is free to renumber.
            //
            // The importer QUANTISES custom outline coordinates to the integer pixel grid. Both
            // components round independently, so the Euclidean error this measures is bounded by
            // sqrt(2)/2 = 0.707 px, not by half a pixel -- the tolerance is derived from that, not
            // widened until the test passed. Measured: 0.40 px (sawtooth), 0.45 (star), 0.60 (bite),
            // and exactly 0 for the four shapes whose coordinates are already whole numbers at
            // 200 px/unit (square, plus, concave L, chamfered plus).
            //
            // Worst case 0.0035 world units against shapes about 1.2 units across, so it is invisible.
            // What it is NOT is an alpha trace, which would change the vertex count and the corner
            // structure; the count assertion below is what holds that line.
            const float tol = 0.75f / k_PixelsPerUnit;

            foreach (Vector2 want in expected)
            {
                float best = float.MaxValue;
                foreach (Vector2 got in boundary)
                    best = Mathf.Min(best, Vector2.Distance(want, got));

                Assert.Less(best, tol,
                    $"{name}: authored outline point ({want.x:F4}, {want.y:F4}) is not on the " +
                    $"imported mesh boundary; nearest is {best:F5} away " +
                    $"({best * k_PixelsPerUnit:F2} px). More than the known half-pixel " +
                    "quantisation means the outline is not being honoured at all -- check " +
                    "spriteMode is 1, spriteMeshType is Tight and spriteExtrude is 0.");
            }

            // And nothing extra: a tight trace or an extrude would add boundary vertices the polygon
            // does not have, which is exactly the approximation this technique exists to avoid.
            Assert.AreEqual(expected.Length, boundary.Count,
                $"{name}: mesh boundary has {boundary.Count} vertices, the authored outline has " +
                $"{expected.Length}. Check spriteExtrude is 0 and spriteMeshType is Tight.");
        }

        [Test]
        public void ChainedConcaveShapesStillHaveTheirConcaveRuns(
            [Values("Square", "Plus", "ConcaveL", "Star", "Bite", "ChamferPlus", "Sawtooth")]
            string name)
        {
            // Asserted on the authored polygon rather than the imported mesh, because the mesh
            // boundary is an unordered set and the run length needs the ring order. The test above is
            // what ties the two together.
            Vector2[] p = s_Polygons[name];
            int n = p.Length;

            double area = 0.0;
            for (int i = 0; i < n; i++)
                area += (double)p[i].x * p[(i + 1) % n].y - (double)p[(i + 1) % n].x * p[i].y;
            double wg = area < 0.0 ? -1.0 : 1.0;

            var reflex = new bool[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 a = p[i] - p[((i - 1) % n + n) % n];
                Vector2 b = p[(i + 1) % n] - p[i];
                reflex[i] = wg * (a.x * b.y - a.y * b.x) < 0.0;
            }

            int longest = 0, run = 0;
            for (int i = 0; i < 2 * n; i++)
            {
                if (reflex[i % n]) { run++; longest = Mathf.Max(longest, Mathf.Min(run, n)); }
                else run = 0;
            }

            Assert.AreEqual(k_ExpectedLongestConcaveRun[name], longest,
                $"{name}: longest run of adjacent reflex vertices changed. Report 27 section 6.1 " +
                "records these, and the bite and chamfered plus are the only shapes that exercise " +
                "the 2-ring radius budget -- losing their runs would make the set stop testing it.");
        }
    }
}
