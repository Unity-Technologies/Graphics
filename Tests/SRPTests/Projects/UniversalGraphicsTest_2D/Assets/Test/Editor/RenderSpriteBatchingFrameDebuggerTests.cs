using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditorInternal.FrameDebuggerInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Captures Graphics.RenderSprite draws with the Frame Debugger to check how Sprites that share a
// MaterialPropertyBlock are batched, and reads the rendered pixels back to check what each Sprite drew.
[TestFixture]
class RenderSpriteBatchingFrameDebuggerTests
{
    const string k_UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
    const string k_MainTex = "_MainTex";
    const int k_TextureSize = 4;

    static readonly string[] k_TextureNames = { "BatchingRed", "BatchingGreen", "BatchingBlue", "BatchingYellow" };
    static readonly Color[] k_Colors = { Color.red, Color.green, Color.blue, Color.yellow };

    struct CapturedDraw
    {
        public FrameEventType type;
        public string mainTexName;
        public int drawCallCount;
        public int instanceCount;
        public string breakCause;

        public override string ToString()
        {
            return type + " _MainTex=" + mainTexName + " drawCalls=" + drawCallCount + " instances=" + instanceCount + " cause=" + breakCause;
        }
    }

    GameObject m_CameraObject;
    Camera m_Camera;
    RenderTexture m_RenderTarget;
    Material m_Material;
    Texture2D[] m_Textures;
    Sprite[] m_Sprites;
    MaterialPropertyBlock m_Block;
    SpriteMaskInteraction m_MaskInteraction;
    bool m_PreviousSRPBatcherEnabled;

    [SetUp]
    public void Setup()
    {
        m_PreviousSRPBatcherEnabled = GraphicsSettings.useScriptableRenderPipelineBatching;
        GraphicsSettings.useScriptableRenderPipelineBatching = true;

        m_RenderTarget = new RenderTexture(256, 256, 24);
        m_CameraObject = new GameObject("BatchingCamera");
        m_Camera = m_CameraObject.AddComponent<Camera>();
        m_Camera.transform.position = new Vector3(0, 0, -10);
        m_Camera.orthographic = true;
        m_Camera.orthographicSize = 6;
        m_Camera.allowHDR = false;
        m_Camera.targetTexture = m_RenderTarget;
        var cameraData = m_CameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.SetRenderer(2);    // 2D Renderer. See the list of Renderers in CommonAssets/UniversalRPAsset.
        cameraData.renderType = CameraRenderType.Base;
        cameraData.renderPostProcessing = false;

        m_Material = new Material(AssetDatabase.LoadAssetAtPath<Material>(k_UnlitMaterialPath));

        m_Textures = new Texture2D[k_Colors.Length];
        m_Sprites = new Sprite[k_Colors.Length];
        for (int i = 0; i < k_Colors.Length; ++i)
        {
            m_Textures[i] = new Texture2D(k_TextureSize, k_TextureSize, TextureFormat.RGBA32, false) { name = k_TextureNames[i] };
            m_Textures[i].SetPixels(Enumerable.Repeat(k_Colors[i], k_TextureSize * k_TextureSize).ToArray());
            m_Textures[i].Apply();
            m_Sprites[i] = Sprite.Create(m_Textures[i], new Rect(0, 0, k_TextureSize, k_TextureSize), new Vector2(0.5f, 0.5f), k_TextureSize);
        }

        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    [TearDown]
    public void Cleanup()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        FrameDebuggerUtility.SetEnabled(false, -1);

        GraphicsSettings.useScriptableRenderPipelineBatching = m_PreviousSRPBatcherEnabled;

        if (m_Sprites != null)
        {
            for (int i = 0; i < m_Sprites.Length; ++i)
            {
                UnityEngine.Object.DestroyImmediate(m_Sprites[i]);
                UnityEngine.Object.DestroyImmediate(m_Textures[i]);
            }
        }

        UnityEngine.Object.DestroyImmediate(m_Material);
        UnityEngine.Object.DestroyImmediate(m_CameraObject);
        if (m_RenderTarget != null)
        {
            m_RenderTarget.Release();
            UnityEngine.Object.DestroyImmediate(m_RenderTarget);
        }
    }

    void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera != m_Camera)
            return;

        var renderParams = new RenderParams(m_Material) { matProps = m_Block };
        for (int i = 0; i < m_Sprites.Length; ++i)
        {
            var spriteParams = new SpriteParams(m_Sprites[i], Color.white, m_MaskInteraction);
            var objectToWorld = Matrix4x4.Translate(new Vector3((i - 1.5f) * 2f, 0, 0));
            Graphics.RenderSprite(renderParams, spriteParams, 0, objectToWorld);
        }
    }

    static bool IsDrawEvent(FrameEventType type) =>
        type == FrameEventType.Mesh ||
        type == FrameEventType.DynamicBatch ||
        type == FrameEventType.DynamicGeometry ||
        type == FrameEventType.InstancedMesh ||
        type == FrameEventType.SRPBatch;

    void RenderUnderCapture()
    {
        FrameDebuggerUtility.EnterCapturingScope();
        m_Camera.Render();
        FrameDebuggerUtility.LeaveCapturingScope();
    }

    // Every draw event that uses the test material's shader, with the _MainTex it bound.
    List<CapturedDraw> CaptureSpriteDraws()
    {
        if (!FrameDebuggerUtility.locallySupported)
            Assert.Ignore("Frame Debugger requires threaded rendering. Run with -force-gfx-mt.");

        // Warm-up: newly created materials need one render to become SRP Batcher compatible.
        m_Camera.Render();

        FrameDebuggerUtility.SetEnabled(true, -1);
        RenderUnderCapture();

        FrameDebuggerEvent[] events = FrameDebuggerUtility.GetFrameEvents();
        Assert.That(events, Is.Not.Null.And.Not.Empty, "No Frame Debugger events were captured.");

        string[] breakCauses = FrameDebuggerUtility.GetBatchBreakCauseStrings();
        string shaderName = m_Material.shader.name;
        var draws = new List<CapturedDraw>();
        for (int index = 0; index < events.Length; ++index)
        {
            if (!IsDrawEvent(events[index].m_Type))
                continue;

            FrameDebuggerUtility.limit = index + 1;
            RenderUnderCapture();

            var data = new FrameDebuggerEventData();
            FrameDebuggerUtility.GetFrameEventData(index, data);
            if (data.m_RealShaderName != shaderName)
                continue;

            var draw = new CapturedDraw
            {
                type = events[index].m_Type,
                drawCallCount = data.m_DrawCallCount,
                instanceCount = data.m_InstanceCount,
                breakCause = breakCauses[data.m_BatchBreakCause],
            };
            foreach (var texture in data.m_ShaderInfo.m_Textures)
            {
                if (texture.m_Name == k_MainTex)
                    draw.mainTexName = texture.m_TextureName;
            }

            draws.Add(draw);
        }

        // Otherwise an assertion that no draw has some type would pass on an empty capture.
        Assert.That(draws, Is.Not.Empty, $"No draws of '{shaderName}' were captured among {events.Length} events.");
        return draws;
    }

    // Control: without a block the four Sprites take the SRP Batcher path.
    [Test]
    public void RenderSprite_WithoutMaterialPropertyBlock_IsSRPBatched()
    {
        m_Block = null;
        m_MaskInteraction = SpriteMaskInteraction.None;

        var draws = CaptureSpriteDraws();

        Assert.That(draws.Any(d => d.type == FrameEventType.SRPBatch), Is.True, "Expected the Sprites to be SRP batched:\n" + string.Join("\n", draws));
    }

    // A shared block gives every node custom props, which must keep the Sprites out of the SRP Batcher.
    [Test]
    public void RenderSprite_SharedMaterialPropertyBlock_IsNotSRPBatched(
        [Values(SpriteMaskInteraction.None, SpriteMaskInteraction.VisibleOutsideMask)] SpriteMaskInteraction maskInteraction,
        [Values] bool enableInstancing)
    {
        m_Block = new MaterialPropertyBlock();
        m_Block.SetFloat("_BatchProbe", 1f);
        m_MaskInteraction = maskInteraction;
        m_Material.enableInstancing = enableInstancing;

        var draws = CaptureSpriteDraws();

        Assert.That(draws.Any(d => d.type == FrameEventType.SRPBatch), Is.False, "Sprites with a MaterialPropertyBlock were SRP batched:\n" + string.Join("\n", draws));
    }

    // One plain render, so nothing from an earlier render in the same frame is drawn again.
    [Test]
    public void RenderSprite_SharedMaterialPropertyBlock_DrawsEachSpriteWithItsOwnTexture(
        [Values(SpriteMaskInteraction.None, SpriteMaskInteraction.VisibleOutsideMask)] SpriteMaskInteraction maskInteraction,
        [Values] bool enableInstancing)
    {
        m_Block = new MaterialPropertyBlock();
        m_Block.SetFloat("_BatchProbe", 1f);
        m_MaskInteraction = maskInteraction;
        m_Material.enableInstancing = enableInstancing;

        Color[] cellColors = RenderAndReadCellColors();

        string report = "Cell colours: " + string.Join(", ", cellColors);
        for (int i = 0; i < k_Colors.Length; ++i)
            Assert.That(IsApproximately(cellColors[i], k_Colors[i]), Is.True, $"Sprite {i} ({k_TextureNames[i]}) was drawn wrongly. " + report);
    }

    // Control: without a block, with or without the SRP Batcher.
    [Test]
    public void RenderSprite_WithoutMaterialPropertyBlock_DrawsEachSpriteWithItsOwnTexture(
        [Values(SpriteMaskInteraction.None, SpriteMaskInteraction.VisibleOutsideMask)] SpriteMaskInteraction maskInteraction,
        [Values] bool useSRPBatcher,
        [Values] bool enableInstancing)
    {
        m_Block = null;
        m_MaskInteraction = maskInteraction;
        m_Material.enableInstancing = enableInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;

        Color[] cellColors = RenderAndReadCellColors();

        string report = "Cell colours: " + string.Join(", ", cellColors);
        for (int i = 0; i < k_Colors.Length; ++i)
            Assert.That(IsApproximately(cellColors[i], k_Colors[i]), Is.True, $"Sprite {i} ({k_TextureNames[i]}) was drawn wrongly. " + report);
    }

    // Replaces the four Sprites with four cut from the colour quadrants of one atlas texture.
    void UseAtlasSprites()
    {
        for (int i = 0; i < m_Sprites.Length; ++i)
        {
            UnityEngine.Object.DestroyImmediate(m_Sprites[i]);
            UnityEngine.Object.DestroyImmediate(m_Textures[i]);
            m_Textures[i] = null;
        }

        const int atlasSize = k_TextureSize * 2;
        var atlas = new Texture2D(atlasSize, atlasSize, TextureFormat.RGBA32, false) { name = "BatchingAtlas", filterMode = FilterMode.Point };
        for (int i = 0; i < k_Colors.Length; ++i)
        {
            int x = (i % 2) * k_TextureSize;
            int y = (i / 2) * k_TextureSize;
            atlas.SetPixels(x, y, k_TextureSize, k_TextureSize, Enumerable.Repeat(k_Colors[i], k_TextureSize * k_TextureSize).ToArray());
            m_Sprites[i] = Sprite.Create(atlas, new Rect(x, y, k_TextureSize, k_TextureSize), new Vector2(0.5f, 0.5f), k_TextureSize);
        }

        atlas.Apply();
        m_Textures[0] = atlas;
    }

    // The texture in the batching key must not stop Sprites that share an atlas from dynamic batching.
    [Test]
    public void RenderSprite_SharedMaterialPropertyBlockSameAtlas_IsDynamicBatched(
        [Values(SpriteMaskInteraction.None, SpriteMaskInteraction.VisibleOutsideMask)] SpriteMaskInteraction maskInteraction)
    {
        UseAtlasSprites();
        m_Block = new MaterialPropertyBlock();
        m_Block.SetFloat("_BatchProbe", 1f);
        m_MaskInteraction = maskInteraction;
        m_Material.enableInstancing = false;

        var draws = CaptureSpriteDraws();

        // With graphics jobs the Frame Debugger can list an extra event whose counts it does not record when replaying up to it (UUM-160512).
        int batchCount = draws.Count(d => d.drawCallCount > 0);
        Assert.That(batchCount, Is.EqualTo(1), "Expected the atlas Sprites in one dynamic batch:\n" + string.Join("\n", draws));
    }

    static bool IsApproximately(Color actual, Color expected)
    {
        const float tolerance = 0.2f;
        return Mathf.Abs(actual.r - expected.r) < tolerance && Mathf.Abs(actual.g - expected.g) < tolerance && Mathf.Abs(actual.b - expected.b) < tolerance;
    }

    // The colour at the centre of each Sprite.
    Color[] RenderAndReadCellColors()
    {
        m_Camera.Render();

        var previous = RenderTexture.active;
        RenderTexture.active = m_RenderTarget;
        var readback = new Texture2D(m_RenderTarget.width, m_RenderTarget.height, TextureFormat.RGBA32, false);
        readback.ReadPixels(new Rect(0, 0, m_RenderTarget.width, m_RenderTarget.height), 0, 0);
        readback.Apply();
        RenderTexture.active = previous;

        float worldToPixels = m_RenderTarget.height / (2f * m_Camera.orthographicSize);
        var colors = new Color[k_Colors.Length];
        for (int i = 0; i < k_Colors.Length; ++i)
        {
            int x = Mathf.RoundToInt(m_RenderTarget.width * 0.5f + (i - 1.5f) * 2f * worldToPixels);
            colors[i] = readback.GetPixel(x, m_RenderTarget.height / 2);
        }

        UnityEngine.Object.DestroyImmediate(readback);
        return colors;
    }
}
