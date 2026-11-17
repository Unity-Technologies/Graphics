using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

// GPU-skinned sprites draw from their own buffers, so flipX and flipY are compensated in the shader (NormalsRenderingShared.hlsl).
// Each test marks a sprite as GPU-skinned, draws it with CommandBuffer.DrawRenderer, and reads back the world-space V axis after that
// compensation and whether the bone offset reached the shader. A missing bone offset means the sprite was CPU-transformed and renders in bind pose.
class SpriteSkinnedTangentFrameTests
{
    const string k_ShaderPath = "Assets/Test/Editor/SpriteSkinnedTangentFrameDebug.shader";
    const int k_TargetSize = 64;
    const float k_PixelsPerUnit = 16.0f;
    const float k_MinimumDot = 0.98f;

    static readonly MethodInfo k_SetBoneTransformIndices = typeof(SpriteRendererDataAccessExtensions).GetMethod(
        "SetBatchBoneTransformIndexAndLocalAABBArray",
        BindingFlags.Static | BindingFlags.NonPublic,
        null,
        new[] { typeof(SpriteRenderer[]), typeof(NativeArray<int>), typeof(NativeArray<Bounds>) },
        null);

    public struct FlipCase
    {
        public string name;
        public Vector3 eulerAngles;
        public bool flipX;
        public bool flipY;

        public override string ToString() => name;
    }

    static FlipCase Case(string name, Vector3 eulerAngles, bool flipX, bool flipY)
    {
        return new FlipCase { name = name, eulerAngles = eulerAngles, flipX = flipX, flipY = flipY };
    }

    static IEnumerable<FlipCase> FlipCases()
    {
        yield return Case("Unflipped", Vector3.zero, false, false);
        yield return Case("FlipX", Vector3.zero, true, false);
        yield return Case("FlipY", Vector3.zero, false, true);
        yield return Case("FlipXAndFlipY", Vector3.zero, true, true);
        yield return Case("FlipXRotateZ90", new Vector3(0, 0, 90), true, false);
        yield return Case("FlipYRotateY180", new Vector3(0, 180, 0), false, true);
    }

    readonly List<Object> m_Cleanup = new List<Object>();
    Material m_Material;
    Sprite m_Sprite;

    [SetUp]
    public void SetUp()
    {
        Assert.IsNotNull(k_SetBoneTransformIndices, "SpriteRendererDataAccessExtensions.SetBatchBoneTransformIndexAndLocalAABBArray was not found.");

        var shader = AssetDatabase.LoadAssetAtPath<Shader>(k_ShaderPath);
        Assert.IsNotNull(shader, "Could not load " + k_ShaderPath);
        Assert.IsTrue(shader.isSupported, "The skinned tangent frame debug shader is not supported on this device.");
        m_Material = Track(new Material(shader));

        var texture = Track(new Texture2D(16, 16, TextureFormat.RGBA32, false));
        var pixels = new Color32[16 * 16];
        for (int i = 0; i < pixels.Length; ++i)
            pixels[i] = new Color32(255, 255, 255, 255);
        texture.SetPixels32(pixels);
        texture.Apply();

        m_Sprite = Track(Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), k_PixelsPerUnit, 0, SpriteMeshType.FullRect));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var obj in m_Cleanup)
        {
            if (obj != null)
                Object.DestroyImmediate(obj);
        }
        m_Cleanup.Clear();
    }

    [Test]
    public void GPUSkinnedSprite_IsDrawnFromItsGPUBuffers([ValueSource(nameof(FlipCases))] FlipCase flipCase)
    {
        var readback = RenderSkinnedSprite(flipCase, out _);
        var centre = k_TargetSize / 2;

        var alpha = readback.GetPixel(centre, centre).a;
        Assert.Greater(alpha, 0.75f, $"{flipCase.name}: the bone offset did not reach the shader, so the sprite was CPU-transformed and renders in bind pose.");
    }

    [Test]
    public void GPUSkinnedSprite_BitangentMatchesWorldVAxis([ValueSource(nameof(FlipCases))] FlipCase flipCase)
    {
        var readback = RenderSkinnedSprite(flipCase, out var expected);
        var centre = k_TargetSize / 2;

        var actual = DecodeBitangent(readback, centre, centre, flipCase.name);
        var dot = Vector3.Dot(expected, actual);
        Assert.Greater(dot, k_MinimumDot, $"{flipCase.name}: expected bitangent {expected:F3}, got {actual:F3}.");
    }

    Texture2D RenderSkinnedSprite(FlipCase flipCase, out Vector3 expectedBitangent)
    {
        var spriteObject = Track(new GameObject("SkinnedTangentTestSprite", typeof(SpriteRenderer)));
        spriteObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(flipCase.eulerAngles));
        var spriteRenderer = spriteObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = m_Sprite;
        spriteRenderer.sharedMaterial = m_Material;
        spriteRenderer.flipX = flipCase.flipX;
        spriteRenderer.flipY = flipCase.flipY;

        using (var boneTransformIndices = new NativeArray<int>(new[] { 0 }, Allocator.Temp))
        using (var bounds = new NativeArray<Bounds>(new[] { m_Sprite.bounds }, Allocator.Temp))
        {
            k_SetBoneTransformIndices.Invoke(null, new object[] { new[] { spriteRenderer }, boneTransformIndices, bounds });
        }

        var target = Track(new RenderTexture(k_TargetSize, k_TargetSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear));

        // Camera at z = -10 looking down +z, matching an orthographic camera of size 1.
        var view = Matrix4x4.Scale(new Vector3(1, 1, -1)) * Matrix4x4.Translate(new Vector3(0, 0, 10));
        var projection = Matrix4x4.Ortho(-1, 1, -1, 1, 0.1f, 20.0f);
        using (var cmd = new CommandBuffer { name = "SkinnedTangentTest" })
        {
            cmd.SetRenderTarget(target);
            cmd.ClearRenderTarget(true, true, Color.clear);
            cmd.SetViewProjectionMatrices(view, projection);
            cmd.DrawRenderer(spriteRenderer, m_Material);
            Graphics.ExecuteCommandBuffer(cmd);
        }

        var readback = Track(new Texture2D(k_TargetSize, k_TargetSize, TextureFormat.RGBAFloat, false, true));
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        readback.ReadPixels(new Rect(0, 0, k_TargetSize, k_TargetSize), 0, 0);
        readback.Apply();
        RenderTexture.active = previous;

        var flipYSign = flipCase.flipY ? -1.0f : 1.0f;
        expectedBitangent = spriteObject.transform.localToWorldMatrix.MultiplyVector(new Vector3(0, flipYSign, 0)).normalized;
        return readback;
    }

    static Vector3 DecodeBitangent(Texture2D readback, int x, int y, string label)
    {
        var sum = Vector3.zero;
        for (int dy = -1; dy <= 1; ++dy)
        {
            for (int dx = -1; dx <= 1; ++dx)
            {
                var pixel = readback.GetPixel(x + dx, y + dy);
                Assert.Greater(pixel.a, 0.25f, $"{label}: the sprite did not cover pixel ({x + dx}, {y + dy}).");
                sum += new Vector3(pixel.r, pixel.g, pixel.b) * 2.0f - Vector3.one;
            }
        }
        return sum.normalized;
    }

    T Track<T>(T obj) where T : Object
    {
        m_Cleanup.Add(obj);
        return obj;
    }
}
