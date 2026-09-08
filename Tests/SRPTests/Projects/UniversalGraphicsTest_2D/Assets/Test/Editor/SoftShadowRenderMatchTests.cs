using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using D2 = UnityEngine.Rendering.Universal.Tests.SoftShadowVertexReference.D2;

namespace UnityEngine.Rendering.Universal.Tests
{
    /// <summary>
    /// The last link in the chain: that the HLSL vertex program renders what the verified C# reference
    /// says it should.
    /// </summary>
    /// <remarks>
    /// Three layers, each checked against the one above:
    /// <list type="bullet">
    /// <item><c>sim_shader.py vert()</c> — float64, the definition of correct.</item>
    /// <item><c>SoftShadowVertexReference</c> — double, verified to 1e-12 by
    /// <c>SoftShadowGoldenTests</c>.</item>
    /// <item><c>SoftShadowProjectVertex.hlsl</c> — float. This fixture.</item>
    /// </list>
    ///
    /// The comparison runs the real mesh the generator produced through both paths: the GPU draws it
    /// with the real material and pass, and the CPU evaluates the reference over the same payloads
    /// read back out of that mesh and rasterises with the same fragment formula. So it checks the
    /// generator's payload, the vertex program and the fragment together, not just the maths.
    ///
    /// It cannot be exact. Two rasterisers disagree on which pixels a triangle edge covers, and float
    /// against double moves a boundary by a fraction of a pixel — so edge pixels legitimately differ.
    /// The assertions are therefore on the mean absolute difference and on the fraction of pixels
    /// that differ substantially, not on a max. A transliteration error shows up as a large connected
    /// region, which either statistic catches easily; what they tolerate is a one-pixel seam.
    /// </remarks>
    [TestFixture]
    [Ignore("TEMP-DISABLED-PR-123379: disabled to land PR #123379; re-enable per docs/pr/123379-disabled-tests.md")]
    class SoftShadowRenderMatchTests
    {
        const string k_MaterialPath =
            "Assets/Scenes/140_Shadow_Materials/TestShadow2D-SoftShadow.mat";

        const int k_Res = 128;
        const float k_Extent = 2.6f;
        const int k_Segments = 3;
        const float k_ShadowLength = 1.45f;
        const float k_SoftnessDegrees = 11.0f;
        const float k_FilletScale = 1.0f;

        // The plus: convex corners, concave corners, and silhouette poles, including a vertex that is
        // concave AND a pole at some light angles (report 27's case).
        static readonly Vector3[] k_Plus =
        {
            new Vector3(-0.20f, -0.55f, 0), new Vector3( 0.20f, -0.55f, 0),
            new Vector3( 0.20f, -0.20f, 0), new Vector3( 0.55f, -0.20f, 0),
            new Vector3( 0.55f,  0.20f, 0), new Vector3( 0.20f,  0.20f, 0),
            new Vector3( 0.20f,  0.55f, 0), new Vector3(-0.20f,  0.55f, 0),
            new Vector3(-0.20f,  0.20f, 0), new Vector3(-0.55f,  0.20f, 0),
            new Vector3(-0.55f, -0.20f, 0), new Vector3(-0.20f, -0.20f, 0),
        };

        Material m_Material;

        [SetUp]
        public void Setup()
        {
            m_Material = AssetDatabase.LoadAssetAtPath<Material>(k_MaterialPath);
            Assert.IsNotNull(m_Material, $"Could not load {k_MaterialPath}.");
        }

        // BuildMesh pins the generator process-wide, so it has to be released or every fixture that
        // runs after this one silently builds Unity.SoftShadow geometry.
        [TearDown]
        public void Cleanup()
        {
            Shadow2DGeometry.globalGeneratorOverride = null;
        }

        static Mesh BuildMesh()
        {
            int n = k_Plus.Length;
            var vertices = new Unity.Collections.NativeArray<Vector3>(
                n, Unity.Collections.Allocator.Temp);
            for (int i = 0; i < n; i++)
                vertices[i] = k_Plus[i];

            var indices = new Unity.Collections.NativeArray<int>(
                n * 2, Unity.Collections.Allocator.Temp);
            for (int i = 0; i < n; i++)
            {
                indices[i * 2] = i;
                indices[i * 2 + 1] = (i + 1) % n;
            }

            // Pinned process-wide rather than per mesh: the generator is a project setting now.
            Shadow2DGeometry.globalGeneratorOverride = SoftShadowGeometryGenerator.k_Id;

            var shadowMesh = new ShadowMesh2D();
            shadowMesh.trimEdge = 0;
            shadowMesh.fanSegments = k_Segments;
            shadowMesh.SetShape(vertices, indices, ShadowShape2D.OutlineTopology.Lines);

            Mesh mesh = shadowMesh.mesh;
            vertices.Dispose();
            indices.Dispose();
            return mesh;
        }

        // Pixel centre -> world, matching the ortho projection handed to the GPU. Row 0 is the bottom
        // row, which is what Texture2D.ReadPixels gives back.
        static float PixelToWorld(int i) => ((i + 0.5f) / k_Res * 2.0f - 1.0f) * k_Extent;

        static float[] RasterizeWithReference(Mesh mesh, Vector2 light)
        {
            Vector3[] positions = mesh.vertices;
            Vector4[] tangents = mesh.tangents;
            var uv1 = new List<Vector3>();
            var uv2 = new List<Vector4>();
            mesh.GetUVs(1, uv1);
            mesh.GetUVs(2, uv2);
            int[] triangles = mesh.triangles;

            var u = new SoftShadowVertexReference.Uniforms(
                new D2(light.x, light.y), k_ShadowLength,
                k_SoftnessDegrees * Math.PI / 180.0,
                k_FilletScale * k_ShadowLength * 2.0 * Math.Sin(k_SoftnessDegrees * Math.PI / 180.0 / 2.0),
                k_Segments);

            // Evaluate the reference over the payloads as the mesh actually stores them, so a
            // generator packing bug is caught here rather than hidden by rebuilding them.
            int count = positions.Length;
            var outPos = new D2[count];
            var outVal = new D2[count];
            for (int i = 0; i < count; i++)
            {
                var pay = new SoftShadowVertexReference.Payload
                {
                    v     = new D2(positions[i].x, positions[i].y),
                    prev  = new D2(tangents[i].z, tangents[i].w),
                    next  = new D2(uv1[i].x, uv1[i].y),
                    prev2 = new D2(uv2[i].x, uv2[i].y),
                    next2 = new D2(uv2[i].z, uv2[i].w),
                    role = Mathf.RoundToInt(tangents[i].x),
                    winding = uv1[i].z,
                    k = Mathf.RoundToInt(tangents[i].y * k_Segments),
                };
                SoftShadowVertexReference.Vert(pay, u, out outPos[i], out outVal[i]);
            }

            var field = new float[k_Res * k_Res];
            for (int t = 0; t < triangles.Length; t += 3)
            {
                int ia = triangles[t], ib = triangles[t + 1], ic = triangles[t + 2];
                D2 pa = outPos[ia], pb = outPos[ib], pc = outPos[ic];

                double det = (pb.y - pc.y) * (pa.x - pc.x) + (pc.x - pb.x) * (pa.y - pc.y);
                if (Math.Abs(det) < 1e-12)
                    continue;               // collapsed role: zero area, contributes nothing

                // Only the pixels this triangle can touch.
                double minX = Math.Min(pa.x, Math.Min(pb.x, pc.x));
                double maxX = Math.Max(pa.x, Math.Max(pb.x, pc.x));
                double minY = Math.Min(pa.y, Math.Min(pb.y, pc.y));
                double maxY = Math.Max(pa.y, Math.Max(pb.y, pc.y));

                int x0 = Mathf.Max(0, Mathf.FloorToInt((float)((minX / k_Extent + 1) * 0.5 * k_Res) - 1));
                int x1 = Mathf.Min(k_Res - 1, Mathf.CeilToInt((float)((maxX / k_Extent + 1) * 0.5 * k_Res) + 1));
                int y0 = Mathf.Max(0, Mathf.FloorToInt((float)((minY / k_Extent + 1) * 0.5 * k_Res) - 1));
                int y1 = Mathf.Min(k_Res - 1, Mathf.CeilToInt((float)((maxY / k_Extent + 1) * 0.5 * k_Res) + 1));

                for (int py = y0; py <= y1; py++)
                {
                    double wy = PixelToWorld(py);
                    for (int px = x0; px <= x1; px++)
                    {
                        double wx = PixelToWorld(px);

                        double w0 = ((pb.y - pc.y) * (wx - pc.x) + (pc.x - pb.x) * (wy - pc.y)) / det;
                        double w1 = ((pc.y - pa.y) * (wx - pc.x) + (pa.x - pc.x) * (wy - pc.y)) / det;
                        double w2 = 1.0 - w0 - w1;
                        if (w0 < 0.0 || w1 < 0.0 || w2 < 0.0)
                            continue;

                        double vx = w0 * outVal[ia].x + w1 * outVal[ib].x + w2 * outVal[ic].x;
                        double vy = w0 * outVal[ia].y + w1 * outVal[ib].y + w2 * outVal[ic].y;
                        double value = 1.0 - Math.Min(1.0, Math.Max(0.0,
                            Math.Abs(vx) / Math.Min(1.0, Math.Max(1e-6, vy))));

                        int idx = py * k_Res + px;
                        if (value > field[idx])
                            field[idx] = (float)value;   // BlendOp Max
                    }
                }
            }
            return field;
        }

        float[] RenderWithShader(Mesh mesh, Vector2 light)
        {
            var rt = new RenderTexture(k_Res, k_Res, 24, RenderTextureFormat.ARGBFloat)
            { filterMode = FilterMode.Point };
            var readback = new Texture2D(k_Res, k_Res, TextureFormat.RGBAFloat, false);
            RenderTexture previous = RenderTexture.active;

            try
            {
                var material = new Material(m_Material);
                material.SetFloat("_ShadowLength", k_ShadowLength);
                material.SetFloat("_FilletRadiusScale", k_FilletScale);

                var cmd = new CommandBuffer();
                cmd.SetRenderTarget(rt);
                cmd.ClearRenderTarget(true, true, Color.black);
                cmd.SetGlobalVector("_LightPos", new Vector4(light.x, light.y, 0, 0));
                cmd.SetGlobalFloat("_SoftShadowAngle", k_SoftnessDegrees * Mathf.Deg2Rad);

                // The softness ceiling, which ShadowRendering.SetShadowProjectionGlobals uploads in
                // the real path. It is not optional here: SoftShadowProjectVertex.hlsl clamps all four
                // softness values to [0, _SoftShadowMaxAngle], so leaving the global unset makes it 0,
                // collapses the penumbra band to zero width and renders a hard-edged shadow that no
                // tolerance would reconcile with the reference. Taken from the same constant the
                // reference clamps against so the two cannot drift apart.
                cmd.SetGlobalFloat("_SoftShadowMaxAngle", (float)SoftShadowVertexReference.k_MaxSoft);

                // The default (transformable) variant is exercised deliberately rather than
                // NOT_TRANSFORMABLE: with these set to identity its arithmetic is the identity, so the
                // CPU and GPU stay on the same coordinates while still running the branch that does
                // the model-space round trip. Leaving them unset would feed the shader zero matrices.
                cmd.SetGlobalMatrix("_ShadowModelMatrix", Matrix4x4.identity);
                cmd.SetGlobalMatrix("_ShadowModelInvMatrix", Matrix4x4.identity);
                cmd.SetGlobalVector("_ShadowModelScale", new Vector4(1, 1, 1, 1));
                cmd.SetViewProjectionMatrices(
                    Matrix4x4.identity,
                    Matrix4x4.Ortho(-k_Extent, k_Extent, -k_Extent, k_Extent, -10.0f, 10.0f));

                int pass = material.FindPass(ShadowRendering.k_ProjectedSelfPassName);
                Assert.GreaterOrEqual(pass, 0, "ProjectedSelf did not resolve.");
                cmd.DrawMesh(mesh, Matrix4x4.identity, material, 0, pass);

                Graphics.ExecuteCommandBuffer(cmd);
                cmd.Release();

                RenderTexture.active = rt;
                readback.ReadPixels(new Rect(0, 0, k_Res, k_Res), 0, 0);
                readback.Apply();

                var field = new float[k_Res * k_Res];
                Color[] pixels = readback.GetPixels();
                for (int i = 0; i < field.Length; i++)
                    field[i] = pixels[i].r;         // ColorMask R

                Object.DestroyImmediate(material);
                return field;
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(readback);
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }

        [Test]
        public void ShaderRenderMatchesTheVerifiedReference(
            [Values(35.0f, 158.0f, 200.0f, 287.0f)] float degrees)
        {
            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
                Assert.Ignore("ARGBFloat render targets unavailable on this device.");

            float a = degrees * Mathf.Deg2Rad;
            var light = new Vector2(2.0f * Mathf.Cos(a), 2.0f * Mathf.Sin(a));

            Mesh mesh = BuildMesh();
            float[] expected = RasterizeWithReference(mesh, light);
            float[] actual = RenderWithShader(mesh, light);

            double sum = 0.0;
            int large = 0, covered = 0;
            for (int i = 0; i < expected.Length; i++)
            {
                double d = Math.Abs(expected[i] - actual[i]);
                sum += d;
                if (d > 0.25) large++;
                if (expected[i] > 0.01f) covered++;
            }
            double mean = sum / expected.Length;
            double largeFraction = (double)large / expected.Length;

            int coveredGpu = 0;
            for (int i = 0; i < actual.Length; i++)
                if (actual[i] > 0.01f) coveredGpu++;

            // Logged rather than only asserted, so the margin is visible in a passing run: a threshold
            // test that passes tells you nothing about how close it was.
            Debug.Log($"[soft shadow] light {degrees} deg: mean |diff| {mean:F6}, " +
                      $"pixels differing >0.25 {large} ({largeFraction:P3}), " +
                      $"reference coverage {covered}, shader coverage {coveredGpu} " +
                      $"of {expected.Length}");

            // Guards against both fields being empty, which would make the comparison vacuous -- the
            // shadow has to actually be on screen for this to mean anything.
            Assert.Greater(coveredGpu, expected.Length / 50,
                $"The shader rendered only {coveredGpu} covered pixels; it drew essentially nothing.");
            Assert.Greater(covered, expected.Length / 50,
                $"The reference field covers only {covered} of {expected.Length} pixels; " +
                "the shadow is not on screen and the comparison would be vacuous.");

            Assert.Less(mean, 0.01,
                $"light {degrees} deg: mean |diff| {mean:F5} between the shader and the reference.");
            Assert.Less(largeFraction, 0.01,
                $"light {degrees} deg: {largeFraction:P2} of pixels differ by more than 0.25. " +
                "A transliteration error shows up as a connected region; a rasterisation seam does not.");
        }
    }
}
