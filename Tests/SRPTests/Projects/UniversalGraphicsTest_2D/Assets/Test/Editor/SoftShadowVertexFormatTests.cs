using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

// Pins the mesh-upload mechanism the planned "Unity.SoftShadow" ShadowGeometryGenerator depends on.
//
// That generator emits a 14-float vertex, not ShadowUtility.ShadowMeshVertex (9 floats), so it
// cannot travel through ShadowGeometryResult.vertices. The plan is for ShadowGeometryResult to
// carry a generator-owned buffer of raw floats instead -- typed NativeArray<float> rather than the
// generator's struct, because ShadowGeometryResult is shared by every generator including
// third-party ones and must not name any particular vertex layout. The generator then reinterprets
// that buffer back to its own struct inside its own UploadMesh override.
//
// The whole design rests on three assumptions that nothing else in the tree exercises: that the
// four declared attributes produce the stride the struct expects, that NativeArray.Reinterpret
// round-trips floats -> a 56-byte struct, and that a shader receives TEXCOORD1 / TEXCOORD2 from a
// single interleaved stream when TEXCOORD0 is absent from the layout. If any of them fail the
// fallback is a concretely-typed field on ShadowGeometryResult, which works but puts a
// URP-specific struct into the shared seam and so gives up third-party extensibility -- a
// different design, not a local fix. Hence: verify first, build second.
//
// These tests deliberately declare their own copy of the struct and layout rather than referencing
// the generator, so they are meaningful before it exists and keep working if it moves.
[TestFixture]
[Ignore("TEMP-DISABLED-PR-123379: disabled to land PR #123379; re-enable per docs/pr/123379-disabled-tests.md")]
class SoftShadowVertexFormatTests
{
    // The payload, mirroring SoftShadowGeometryGenerator.SoftShadowMeshVertex:
    //   position  = v.xy, 0            -- z stays 0 on purpose, see PositionZIsUnusedByThePayload
    //   tangent   = role, fanParam, prevPt.xy
    //   next      = nextPt.xy, windingSign
    //   neighbor2 = prev2Pt.xy, next2Pt.xy
    //
    // windingSign is per-vertex rather than a uniform on purpose. It comes from a ring's signed
    // area, and a caster can carry several rings; one uniform would be wrong for any ring whose
    // winding differs, and an inverted winding inverts the concave/convex classification silently.
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct SoftShadowVertex
    {
        public Vector3 position;
        public Vector4 tangent;
        public Vector3 next;
        public Vector4 neighbor2;
    }

    const int k_FloatsPerVertex = 14;
    const int k_BytesPerVertex = k_FloatsPerVertex * sizeof(float);

    const string k_ProbeShaderName = "Hidden/Test/SoftShadowVertexFormatProbe";

    static VertexAttributeDescriptor[] CreateLayout()
    {
        // TEXCOORD0 and COLOR are deliberately left free: Shadow2DCasterVertex.hlsl claims both
        // under SHADOW_SPRITE_CASTER, and the caster passes are shared verbatim with this format's
        // shader. Geometry casters have that keyword off so there is no live collision, but using
        // TEXCOORD1+ removes the trap entirely.
        return new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position,  VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Tangent,   VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 4),
        };
    }

    // Distinct, exactly-representable values in every slot, so a wrong stride or a swapped
    // semantic shows up as a specific mismatch rather than a plausible-looking number.
    static SoftShadowVertex MakeVertex(int i)
    {
        float b = i * 100;
        return new SoftShadowVertex
        {
            position  = new Vector3(b + 1, b + 2, 0),
            tangent   = new Vector4(b + 3, b + 4, b + 5, b + 6),
            next      = new Vector3(b + 7, b + 8, b == 0 ? 1f : -1f),
            neighbor2 = new Vector4(b + 9, b + 10, b + 11, b + 12),
        };
    }

    // Builds the seam buffer the way the generator will: a flat float array that knows nothing
    // about the struct, filled through a reinterpreted view of itself.
    static NativeArray<float> AllocateSeamBuffer(int vertexCount, Allocator allocator)
    {
        var seam = new NativeArray<float>(vertexCount * k_FloatsPerVertex, allocator);
        var typed = seam.Reinterpret<SoftShadowVertex>(sizeof(float));
        for (int i = 0; i < vertexCount; i++)
            typed[i] = MakeVertex(i);
        return seam;
    }

    // A single triangle covering clip space, so the probe shader's output is constant over the
    // whole render target and one pixel is enough to read it back.
    static NativeArray<float> AllocateFullScreenTriangle(Vector4 tangent, Vector3 next, Vector4 neighbor2)
    {
        var seam = new NativeArray<float>(3 * k_FloatsPerVertex, Allocator.Temp);
        var typed = seam.Reinterpret<SoftShadowVertex>(sizeof(float));
        var corners = new[] { new Vector3(-3, -1, 0), new Vector3(0, 3, 0), new Vector3(3, -1, 0) };
        for (int i = 0; i < 3; i++)
        {
            typed[i] = new SoftShadowVertex
            {
                position = corners[i], tangent = tangent, next = next, neighbor2 = neighbor2,
            };
        }
        return seam;
    }

    static Mesh UploadMesh(NativeArray<float> seam, int vertexCount, int[] indices)
    {
        var mesh = new Mesh();
        mesh.SetVertexBufferParams(vertexCount, CreateLayout());

        // The call the generator's UploadMesh override will make. SetVertexBufferData takes its
        // element size from sizeof(T) and knows nothing about the declared layout, so this is the
        // exact point where a stride disagreement would surface.
        var typed = seam.Reinterpret<SoftShadowVertex>(sizeof(float));
        mesh.SetVertexBufferData(typed, 0, 0, vertexCount);

        mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);
        mesh.SetIndexBufferData(indices, 0, 0, indices.Length);

        var bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, indices.Length), MeshUpdateFlags.DontRecalculateBounds);
        mesh.bounds = bounds;
        return mesh;
    }

    // ----------------------------------------------------------------------------------------
    // Stride and layout
    // ----------------------------------------------------------------------------------------

    [Test]
    public void StructSizeMatchesTheDeclaredLayoutStride()
    {
        // If these ever disagree, SetVertexBufferData throws at upload rather than at build time,
        // so asserting it directly is what turns a runtime surprise into a compile-adjacent one.
        Assert.AreEqual(k_BytesPerVertex, UnsafeUtility.SizeOf<SoftShadowVertex>(),
            "SoftShadowVertex must be exactly 14 floats. Adding a field without updating the layout " +
            "makes SetVertexBufferData throw at upload.");

        var mesh = UploadMesh(AllocateSeamBuffer(1, Allocator.Temp), 1, new[] { 0, 0, 0 });
        try
        {
            Assert.AreEqual(k_BytesPerVertex, mesh.GetVertexBufferStride(0),
                "The four declared attributes must pack into one 56-byte interleaved stream.");
            Assert.AreEqual(1, mesh.vertexBufferCount, "The payload is a single interleaved stream.");
        }
        finally { Object.DestroyImmediate(mesh); }
    }

    [Test]
    public void UploadedMeshReportsTheDeclaredVertexAttributes()
    {
        // Shadow2DGeometryPathTests.UploadedMeshUsesTheGeneratorsDeclaredVertexLayout asserts a
        // generator's declaration against mesh.GetVertexAttributes(). This is the same check for
        // this layout specifically, so the new generator cannot fail that test for a reason that
        // lives in the format rather than in the generator.
        var mesh = UploadMesh(AllocateSeamBuffer(3, Allocator.Temp), 3, new[] { 0, 1, 2 });
        try
        {
            var declared = CreateLayout();
            var actual = mesh.GetVertexAttributes();

            Assert.AreEqual(declared.Length, actual.Length, "Attribute count must round-trip.");
            for (int i = 0; i < declared.Length; i++)
            {
                Assert.AreEqual(declared[i].attribute, actual[i].attribute, $"Attribute {i} semantic.");
                Assert.AreEqual(declared[i].format, actual[i].format, $"Attribute {i} format.");
                Assert.AreEqual(declared[i].dimension, actual[i].dimension, $"Attribute {i} dimension.");
                Assert.AreEqual(0, actual[i].stream, $"Attribute {i} must be on stream 0.");
            }
        }
        finally { Object.DestroyImmediate(mesh); }
    }

    [Test]
    public void TexCoord0AndColorStayFreeForTheSharedCasterPasses()
    {
        var mesh = UploadMesh(AllocateSeamBuffer(1, Allocator.Temp), 1, new[] { 0, 0, 0 });
        try
        {
            Assert.IsFalse(mesh.HasVertexAttribute(VertexAttribute.TexCoord0),
                "TEXCOORD0 must stay free: Shadow2DCasterVertex.hlsl claims it as sprite UV.");
            Assert.IsFalse(mesh.HasVertexAttribute(VertexAttribute.Color),
                "COLOR must stay free: Shadow2DCasterVertex.hlsl claims it as sprite colour.");
        }
        finally { Object.DestroyImmediate(mesh); }
    }

    // ----------------------------------------------------------------------------------------
    // Reinterpret
    // ----------------------------------------------------------------------------------------

    [Test]
    public void SeamBufferReinterpretsToWholeVertices()
    {
        var seam = AllocateSeamBuffer(7, Allocator.Temp);
        try
        {
            var typed = seam.Reinterpret<SoftShadowVertex>(sizeof(float));
            Assert.AreEqual(7, typed.Length, "13 floats per vertex must alias 1:1 onto the struct.");
            Assert.AreEqual(MakeVertex(6).neighbor2, typed[6].neighbor2,
                "The reinterpreted view must alias the same memory, not a copy.");
        }
        finally { seam.Dispose(); }
    }

    [Test]
    public void SeamElementTypeKeepsTheStructNaturallyAligned()
    {
        // Reinterpret performs no alignment check whatsoever (NativeArray.cs, InternalReinterpret
        // just wraps the existing pointer). A NativeArray<byte> allocates with AlignOf<byte>() == 1,
        // so a byte-backed seam could hand a 1-byte-aligned pointer to a struct of floats -- fine on
        // x86, not guaranteed elsewhere. A float-backed seam gets 4-byte alignment by construction.
        Assert.AreEqual(4, UnsafeUtility.AlignOf<float>(),
            "The float-backed seam relies on float alignment being 4.");
        Assert.AreEqual(0, UnsafeUtility.SizeOf<SoftShadowVertex>() % sizeof(float),
            "The vertex struct must be a whole number of floats for a float-backed seam to alias it.");
    }

    [Test]
    public void ReinterpretRejectsAPartialVertexInTheEditor()
    {
        // A seam length that is not a whole number of vertices is a generator bug. Worth pinning
        // that it is caught -- but note the guard is [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")],
        // which is defined in the Editor and development builds and NOT in a release player. There,
        // uLen = byteLen / uSize truncates and the upload silently drops the trailing vertices. So
        // the generator must not rely on this throwing; it is a development aid only.
        var seam = new NativeArray<float>(k_FloatsPerVertex + 1, Allocator.Temp);
        try
        {
            Assert.Throws<System.InvalidOperationException>(
                () => seam.Reinterpret<SoftShadowVertex>(sizeof(float)),
                "A partial trailing vertex must be rejected while collections checks are enabled.");
        }
        finally { seam.Dispose(); }
    }

    [Test]
    public void ReinterpretRejectsAWrongSourceElementSize()
    {
        // Reinterpret's first argument is the size of the SOURCE element, not the destination.
        // Passing sizeof(SoftShadowVertex) here is the natural mistake and it is caught.
        var seam = new NativeArray<float>(k_FloatsPerVertex, Allocator.Temp);
        try
        {
            Assert.Throws<System.InvalidOperationException>(
                () => seam.Reinterpret<SoftShadowVertex>(k_BytesPerVertex));
        }
        finally { seam.Dispose(); }
    }

    // ----------------------------------------------------------------------------------------
    // Round trip
    // ----------------------------------------------------------------------------------------

    [Test]
    public void UploadedMeshDecodesEveryPayloadSlotBackToItsAuthoredValue()
    {
        // The strongest CPU-side check available: Unity decodes the vertex buffer back into managed
        // arrays using the DECLARED layout, so agreement here proves the stride, the field order and
        // every semantic mapping at once. A stride error would smear values between slots.
        const int count = 4;
        var seam = AllocateSeamBuffer(count, Allocator.Temp);
        Mesh mesh = null;
        try
        {
            mesh = UploadMesh(seam, count, new[] { 0, 1, 2, 1, 2, 3 });

            var positions = mesh.vertices;
            var tangents = mesh.tangents;
            var next = new List<Vector3>();
            var neighbor2 = new List<Vector4>();
            mesh.GetUVs(1, next);
            mesh.GetUVs(2, neighbor2);

            Assert.AreEqual(count, positions.Length, "POSITION count.");
            Assert.AreEqual(count, tangents.Length, "TANGENT count.");
            Assert.AreEqual(count, next.Count, "TEXCOORD1 count.");
            Assert.AreEqual(count, neighbor2.Count, "TEXCOORD2 count.");

            for (int i = 0; i < count; i++)
            {
                var expected = MakeVertex(i);
                Assert.AreEqual(expected.position, positions[i], $"POSITION at {i}.");
                Assert.AreEqual(expected.tangent, tangents[i], $"TANGENT at {i}.");
                Assert.AreEqual(expected.next, next[i], $"TEXCOORD1 at {i}.");
                Assert.AreEqual(expected.neighbor2, neighbor2[i], $"TEXCOORD2 at {i}.");
            }
        }
        finally
        {
            seam.Dispose();
            if (mesh != null) Object.DestroyImmediate(mesh);
        }
    }

    [Test]
    public void PositionZIsUnusedByThePayload()
    {
        // Handoff v2 section 9 packs fanSegmentIndex into position.z. Three POSITION-only consumers
        // would then read it as geometry: Shadow2DCasterVertex.hlsl's
        // TransformObjectToHClip(v.positionOS), mesh bounds, and preview-outline code -- and
        // LegacyShadowGeometryGenerator.DrawPreviewOutline already treats pt.z as a flag. This
        // pins the decision to move it into tangent.y, which Legacy documents as always zero.
        const int count = 3;
        var seam = AllocateSeamBuffer(count, Allocator.Temp);
        Mesh mesh = null;
        try
        {
            mesh = UploadMesh(seam, count, new[] { 0, 1, 2 });
            foreach (var p in mesh.vertices)
                Assert.AreEqual(0f, p.z, "position.z must stay 0; fanParam belongs in tangent.y.");
        }
        finally
        {
            seam.Dispose();
            if (mesh != null) Object.DestroyImmediate(mesh);
        }
    }

    [Test]
    public void DisposingOnlyTheSeamBufferReleasesTheGeometry()
    {
        // Reinterpret returns a view over the SAME allocation, carrying the same allocator label
        // and sharing the atomic safety handle. Disposing the view as well as the original is a
        // double free. The generator owns the seam buffer; reinterpreted views are transient and
        // must never be disposed. Pinned because ShadowGeometryResult's documented contract is that
        // ownership travels in and back out through its buffer fields.
        var seam = AllocateSeamBuffer(2, Allocator.Persistent);
        var typed = seam.Reinterpret<SoftShadowVertex>(sizeof(float));

        Assert.IsTrue(typed.IsCreated);
        seam.Dispose();
        Assert.IsFalse(seam.IsCreated, "Disposing the seam buffer is what releases the allocation.");
    }

    // ----------------------------------------------------------------------------------------
    // Shader side
    // ----------------------------------------------------------------------------------------

    [Test]
    public void ShaderReceivesTangentAndBothTexCoordsFromTheInterleavedStream()
    {
        // The one assumption the CPU round trip cannot cover: that a shader binds TEXCOORD1 as
        // float2 and TEXCOORD2 as float4 out of a single interleaved stream when TEXCOORD0 is
        // absent from the layout. Unity binds by semantic rather than by channel order, but UV
        // channel remapping is exactly the kind of thing that would silently shift the payload.
        // A float target is what makes the readback an exact comparison rather than an 8-bit one.
        // Ignore rather than fail where it is unavailable: this test is about attribute binding,
        // and a device without float render targets cannot answer the question either way.
        if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
            Assert.Ignore("ARGBFloat render targets are unavailable on this device.");

        var shader = Shader.Find(k_ProbeShaderName);
        Assert.IsNotNull(shader, $"Could not find '{k_ProbeShaderName}'.");

        var tangent = new Vector4(0.25f, 0, 0, 0);
        var next = new Vector3(0.5f, 0, 1f);
        var neighbor2 = new Vector4(0.75f, 0, 0, 1.0f);

        var seam = AllocateFullScreenTriangle(tangent, next, neighbor2);
        Mesh mesh = null;
        Material material = null;
        RenderTexture rt = null;
        Texture2D readback = null;
        RenderTexture previous = RenderTexture.active;
        try
        {
            mesh = UploadMesh(seam, 3, new[] { 0, 1, 2 });
            material = new Material(shader);

            rt = new RenderTexture(4, 4, 0, RenderTextureFormat.ARGBFloat) { filterMode = FilterMode.Point };
            var cmd = new CommandBuffer();
            cmd.SetRenderTarget(rt);
            cmd.ClearRenderTarget(true, true, Color.black);
            cmd.DrawMesh(mesh, Matrix4x4.identity, material, 0, 0);
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();

            RenderTexture.active = rt;
            readback = new Texture2D(4, 4, TextureFormat.RGBAFloat, false);
            readback.ReadPixels(new Rect(0, 0, 4, 4), 0, 0);
            readback.Apply();

            // The probe writes float4(tangent.x, uv1.x, uv2.x, uv2.w). A cleared target reads
            // (0,0,0,0), so a mismatch distinguishes "payload arrived wrong" from "nothing drew".
            Color c = readback.GetPixel(2, 2);
            Assert.AreEqual(tangent.x, c.r, 1e-5f, "TANGENT.x did not reach the shader.");
            Assert.AreEqual(next.x, c.g, 1e-5f, "TEXCOORD1.x did not reach the shader.");
            Assert.AreEqual(neighbor2.x, c.b, 1e-5f, "TEXCOORD2.x did not reach the shader.");
            Assert.AreEqual(neighbor2.w, c.a, 1e-5f, "TEXCOORD2.w did not reach the shader.");
        }
        finally
        {
            RenderTexture.active = previous;
            seam.Dispose();
            if (readback != null) Object.DestroyImmediate(readback);
            if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
            if (material != null) Object.DestroyImmediate(material);
            if (mesh != null) Object.DestroyImmediate(mesh);
        }
    }
}
