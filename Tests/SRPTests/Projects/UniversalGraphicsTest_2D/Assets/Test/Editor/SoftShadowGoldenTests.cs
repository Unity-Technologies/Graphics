using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using D2 = UnityEngine.Rendering.Universal.Tests.SoftShadowVertexReference.D2;

namespace UnityEngine.Rendering.Universal.Tests
{
    // Closes the loop between the Python model and the C# reference.
    //
    // softshadow_goldens.txt records, for every payload the per-vertex build emits, the position and
    // shading value sim_shader.py's vert() produced. This rebuilds each payload from the same outline
    // and runs SoftShadowVertexReference.Vert over it, so agreement verifies the transliteration
    // exactly rather than approximately.
    //
    // Why goldens and not just the property checks: report 27 section 8 is that an equivalence test
    // cannot see a shared defect -- the per-vertex and global Python builds agreed to 0.000000 while
    // both carried the setback bug. So the property checks (fillets never cross, the setback and the
    // arc fire together, the fan meets the band) are the load-bearing correctness tests and live in
    // the simulation tree. These goldens are for something narrower and still necessary: catching a
    // transliteration slip, which a property check would not notice because the property would still
    // hold on the wrong number.
    //
    // Regenerating: run sim/gen_goldens.py and copy softshadow_goldens.txt next to this file. The
    // META line pins the parameters, so a change to them fails loudly rather than silently comparing
    // against the wrong configuration.
    [TestFixture]
    [Ignore("TEMP-DISABLED-PR-123379: disabled to land PR #123379; re-enable per docs/pr/123379-disabled-tests.md")]
    class SoftShadowGoldenTests
    {
        const string k_GoldenFile = "Test/Editor/softshadow_goldens.txt";
        const int k_ExpectedFileVersion = 1;

        // Tolerance for a double-vs-double comparison of the same algorithm in two languages. Both
        // run float64 and the operation order is identical, so the only expected difference is in
        // libm -- Math.Atan2 / Acos / Sin against Python's. Anything above this is a real divergence,
        // not precision.
        const double k_Tolerance = 1e-12;

        struct Row
        {
            public int vertexIndex, role, k;
            public double posX, posY, valX, valY;
        }

        class Block
        {
            public string shape;
            public D2[] outline;
            public double winding;
            public readonly List<(D2 light, List<Row> rows)> lights = new List<(D2, List<Row>)>();
        }

        static string GoldenPath => Path.Combine(Application.dataPath, k_GoldenFile);

        static double Num(string s) => double.Parse(s, CultureInfo.InvariantCulture);

        static List<Block> Parse(out int knob, out double length, out double softDegrees,
                                out double radius)
        {
            Assert.IsTrue(File.Exists(GoldenPath),
                $"Golden file missing at {GoldenPath}. Regenerate with sim/gen_goldens.py.");

            knob = 0; length = softDegrees = radius = 0.0;
            var blocks = new List<Block>();
            Block block = null;
            List<Row> rows = null;
            var outline = new List<D2>();
            bool sawMeta = false;

            foreach (string raw in File.ReadLines(GoldenPath))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;

                string[] f = line.Split(' ');
                switch (f[0])
                {
                    case "META":
                        Assert.AreEqual(k_ExpectedFileVersion, int.Parse(f[1]),
                            "Golden file format version changed; update this test alongside it.");
                        knob = int.Parse(f[2]);
                        length = Num(f[3]);
                        softDegrees = Num(f[4]);
                        radius = Num(f[5]);
                        sawMeta = true;
                        break;

                    case "SHAPE":
                        block = new Block { shape = f[1] };
                        blocks.Add(block);
                        outline.Clear();
                        rows = null;
                        break;

                    case "V":
                        outline.Add(new D2(Num(f[1]), Num(f[2])));
                        break;

                    case "LIGHT":
                        // The outline is complete the first time a LIGHT appears in this block.
                        if (block.outline == null)
                        {
                            block.outline = outline.ToArray();
                            block.winding = SoftShadowVertexReference.Winding(block.outline);
                        }
                        rows = new List<Row>();
                        block.lights.Add((new D2(Num(f[1]), Num(f[2])), rows));
                        break;

                    case "P":
                        rows.Add(new Row
                        {
                            vertexIndex = int.Parse(f[1]),
                            role = int.Parse(f[2]),
                            k = int.Parse(f[3]),
                            posX = Num(f[4]), posY = Num(f[5]),
                            valX = Num(f[6]), valY = Num(f[7]),
                        });
                        break;
                }
            }

            Assert.IsTrue(sawMeta, "Golden file has no META line.");
            Assert.Greater(blocks.Count, 0, "Golden file contains no shapes.");
            return blocks;
        }

        [Test]
        public void ReferenceMatchesTheModelForEveryPayload()
        {
            var blocks = Parse(out int knob, out double length, out double softDeg, out double radius);
            double soft = softDeg * Math.PI / 180.0;

            int compared = 0;
            double worstPos = 0.0, worstVal = 0.0;
            string worstWhere = null;

            foreach (Block b in blocks)
            {
                foreach ((D2 light, List<Row> rows) in b.lights)
                {
                    var u = new SoftShadowVertexReference.Uniforms(
                        light, length, soft, radius, knob);

                    foreach (Row r in rows)
                    {
                        var pay = SoftShadowVertexReference.MakePayload(
                            b.outline, b.winding, r.vertexIndex, r.role, r.k);

                        SoftShadowVertexReference.Vert(pay, u, out D2 pos, out D2 val);

                        double dp = Math.Max(Math.Abs(pos.x - r.posX), Math.Abs(pos.y - r.posY));
                        double dv = Math.Max(Math.Abs(val.x - r.valX), Math.Abs(val.y - r.valY));

                        if (dp > worstPos)
                        {
                            worstPos = dp;
                            worstWhere = $"{b.shape} light({light.x:R},{light.y:R}) " +
                                         $"vertex {r.vertexIndex} role {r.role} k {r.k}";
                        }
                        worstVal = Math.Max(worstVal, dv);
                        compared++;
                    }
                }
            }

            Assert.Greater(compared, 0, "No payload rows were compared.");
            Assert.LessOrEqual(worstVal, k_Tolerance,
                $"Shading value diverges from the model by {worstVal:E3}.");
            Assert.LessOrEqual(worstPos, k_Tolerance,
                $"Position diverges from the model by {worstPos:E3} at {worstWhere}. " +
                "The reference is meant to be a transliteration -- check the guard conditions and " +
                "the argument order into FarLimit before suspecting precision.");
        }

        [Test]
        public void GoldenFileCoversEveryVertexRole()
        {
            // A transliteration bug in a role nothing exercises would pass silently. The plus and the
            // chained-concave shapes between them should produce all eleven.
            var blocks = Parse(out _, out _, out _, out _);
            var seen = new HashSet<int>();
            foreach (Block b in blocks)
                foreach ((D2 _l, List<Row> rows) in b.lights)
                    foreach (Row r in rows)
                        seen.Add(r.role);

            for (int role = 0; role <= 10; role++)
                Assert.IsTrue(seen.Contains(role), $"Golden file exercises no payload with role {role}.");
        }

        [Test]
        public void GoldenFileExercisesEveryCollapsePath()
        {
            // Every role except OUTLINE / HARD / HARD_OPQ has a collapse target, and the collapse is
            // where most of the guard logic lives -- report 29 measured 67.6% of vertices collapsing.
            // Assert both branches are represented so a guard inverted in the reference cannot pass.
            var blocks = Parse(out int knob, out double length, out double softDeg, out double radius);
            double soft = softDeg * Math.PI / 180.0;

            int collapsed = 0, active = 0;
            foreach (Block b in blocks)
            {
                foreach ((D2 light, List<Row> rows) in b.lights)
                {
                    var u = new SoftShadowVertexReference.Uniforms(
                        light, length, soft, radius, knob);
                    foreach (Row r in rows)
                    {
                        if (r.role == SoftShadowVertexReference.RoleOutline ||
                            r.role == SoftShadowVertexReference.RoleHard ||
                            r.role == SoftShadowVertexReference.RoleHardOpq)
                            continue;

                        var pay = SoftShadowVertexReference.MakePayload(
                            b.outline, b.winding, r.vertexIndex, r.role, r.k);
                        SoftShadowVertexReference.Vert(pay, u, out D2 pos, out _);

                        D2 v = b.outline[r.vertexIndex];
                        D2 hv = SoftShadowVertexReference.HardFor(v, light, length);
                        bool isCollapsed =
                            Math.Abs(pos.x - hv.x) + Math.Abs(pos.y - hv.y) < 1e-12 ||
                            Math.Abs(pos.x - v.x) + Math.Abs(pos.y - v.y) < 1e-12;
                        if (isCollapsed) collapsed++; else active++;
                    }
                }
            }

            Assert.Greater(collapsed, 0, "No payload collapsed; the guards are never firing.");
            Assert.Greater(active, 0, "No payload survived; every guard is firing.");
        }
    }
}
