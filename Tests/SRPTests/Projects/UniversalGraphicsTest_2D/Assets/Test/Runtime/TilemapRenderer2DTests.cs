using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Profiling;
#endif

[TestFixture]
class TilemapRenderer2DTests
{
    private GameObject m_BaseObj;
    private Camera m_BaseCamera;
    private UniversalAdditionalCameraData m_BaseCameraData;

    private Tilemap m_Tilemap;
    private TilemapRenderer m_TilemapRenderer;
    private Tile m_Tile;
    private Sprite m_Sprite;

    private bool m_OriginalUseSRPBatching = true;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        m_BaseObj = new GameObject();
        m_BaseObj.transform.position = new Vector3(0.0f, 0.0f, -10.0f);
        m_BaseObj.transform.forward = Vector3.forward;
        m_BaseCamera = m_BaseObj.AddComponent<Camera>();
        m_BaseCameraData = m_BaseObj.AddComponent<UniversalAdditionalCameraData>();

        m_BaseCamera.allowHDR = false;
        m_BaseCamera.clearFlags = CameraClearFlags.SolidColor;
        m_BaseCameraData.SetRenderer(2); // 2D Renderer. See the list of Renderers in CommonAssets/UniversalRPAsset.
        m_BaseCameraData.renderType = CameraRenderType.Base;
        m_BaseCameraData.renderPostProcessing = false;
        m_BaseCamera.targetTexture = new RenderTexture(m_BaseCamera.pixelWidth, m_BaseCamera.pixelHeight, 24);

        m_Tile = ScriptableObject.CreateInstance<Tile>();
        m_Sprite = Sprite.Create(Texture2D.whiteTexture,
            new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.zero, 1f);
        m_Tile.sprite = m_Sprite;

#if UNITY_EDITOR
        EditorApplication.ExecuteMenuItem("Window/General/Game");
#endif
    }

    [SetUp]
    public void Setup()
    {
        var go = new GameObject("Tilemap", typeof(Grid), typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.position = Vector3.zero;
        m_TilemapRenderer = go.GetComponent<TilemapRenderer>();
        m_TilemapRenderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Auto;
        m_TilemapRenderer.chunkCullingBounds = Vector3.zero;
        m_Tilemap = go.GetComponent<Tilemap>();

        m_OriginalUseSRPBatching = GraphicsSettings.useScriptableRenderPipelineBatching;
    }

    [TearDown]
    public void TearDown()
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = m_OriginalUseSRPBatching;
        Object.DestroyImmediate(m_Tilemap.gameObject);
    }

    [OneTimeTearDown]
    public void OneTimeCleanup()
    {
        Object.DestroyImmediate(m_Tile);
        Object.DestroyImmediate(m_Sprite);
        Object.DestroyImmediate(m_BaseObj);
    }

#if UNITY_EDITOR
    [UnityTest]
    public IEnumerator TilemapRenderer_IndividualMode_SRPBatcherOn_DoesDrawSRPBatcher()
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = true;

        m_TilemapRenderer.mode = TilemapRenderer.Mode.Individual;

        m_Tilemap.SetTile(Vector3Int.zero, m_Tile);
        m_Tilemap.SetTile(new Vector3Int(0, 1, 0), m_Tile);
        m_Tilemap.SetTile(new Vector3Int(1, 0, 0), m_Tile);
        m_Tilemap.SetTile(new Vector3Int(1, 1, 0), m_Tile);
        Assert.AreEqual(m_Tile, m_Tilemap.GetTile(Vector3Int.zero));

        Recorder recorder = null;
        var sampler = Sampler.Get("RenderLoop.DrawSRPBatcher");
        if (sampler.isValid)
            recorder = sampler.GetRecorder();

        // Wait for rendering
        int count = 0;
        for (var i = 0; i < 3; ++i)
        {
            if (recorder != null)
                count += recorder.sampleBlockCount;
            yield return null;
        }

        // SRP Batching is used
        Assert.Greater(count, 0);

        // Must have rendered with at least one draw
        Assert.Greater(UnityStats.drawCalls, 0);
    }

    [UnityTest]
    public IEnumerator TilemapRenderer_IndividualMode_WithSameSpriteRenderers_SRPBatcherOn_DoesDrawSRPBatcher()
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = true;

        m_TilemapRenderer.mode = TilemapRenderer.Mode.Individual;

        m_Tilemap.SetTile(Vector3Int.zero, m_Tile);
        m_Tilemap.SetTile(new Vector3Int(0, 1, 0), m_Tile);
        m_Tilemap.SetTile(new Vector3Int(1, 0, 0), m_Tile);
        m_Tilemap.SetTile(new Vector3Int(1, 1, 0), m_Tile);
        Assert.AreEqual(m_Tile, m_Tilemap.GetTile(Vector3Int.zero));

        var srgo1 = new GameObject("Sprite", typeof(SpriteRenderer));
        srgo1.transform.position = Vector3Int.zero;
        var sr = srgo1.GetComponent<SpriteRenderer>();
        sr.sprite = m_Tile.sprite;
        var srgo2 = new GameObject("Sprite", typeof(SpriteRenderer));
        srgo2.transform.position = new Vector3(1, 0, 0);
        sr = srgo2.GetComponent<SpriteRenderer>();
        sr.sprite = m_Tile.sprite;

        Recorder recorder = null;
        var sampler = Sampler.Get("RenderLoop.DrawSRPBatcher");
        if (sampler.isValid)
            recorder = sampler.GetRecorder();

        // Wait for rendering
        int count = 0;
        for (var i = 0; i < 3; ++i)
        {
            if (recorder != null)
                count += recorder.sampleBlockCount;
            yield return null;
        }

        // SRP Batching is used
        Assert.Greater(count, 0);

        Object.Destroy(srgo1);
        Object.Destroy(srgo2);

        // Must have rendered with at least one draw
        Assert.Greater(UnityStats.drawCalls, 0);
    }

    [UnityTest]
    public IEnumerator TilemapRenderer_IndividualMode_WithSameSpriteRenderers_SRPBatcherOff_DoesNotDrawSRPBatcher()
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = false;

        m_TilemapRenderer.mode = TilemapRenderer.Mode.Individual;

        m_Tilemap.SetTile(Vector3Int.zero, m_Tile);
        m_Tilemap.SetTile(new Vector3Int(0, 1, 0), m_Tile);
        m_Tilemap.SetTile(new Vector3Int(1, 0, 0), m_Tile);
        m_Tilemap.SetTile(new Vector3Int(1, 1, 0), m_Tile);
        Assert.AreEqual(m_Tile, m_Tilemap.GetTile(Vector3Int.zero));

        var srgo1 = new GameObject("Sprite", typeof(SpriteRenderer));
        srgo1.transform.position = Vector3Int.zero;
        var sr = srgo1.GetComponent<SpriteRenderer>();
        sr.sprite = m_Tile.sprite;
        var srgo2 = new GameObject("Sprite", typeof(SpriteRenderer));
        srgo2.transform.position = new Vector3(1, 0, 0);
        sr = srgo2.GetComponent<SpriteRenderer>();
        sr.sprite = m_Tile.sprite;

        Recorder recorder = null;
        var sampler = Sampler.Get("RenderLoop.DrawSRPBatcher");
        if (sampler.isValid)
            recorder = sampler.GetRecorder();

        // Wait for rendering
        int count = 0;
        for (var i = 0; i < 3; ++i)
        {
            if (recorder != null)
                count += recorder.sampleBlockCount;
            yield return null;
        }

        // SRP Batching is not used
        Assert.AreEqual(count, 0);

        Object.Destroy(srgo1);
        Object.Destroy(srgo2);

        // Must have rendered with at least one draw
        Assert.Greater(UnityStats.drawCalls, 0);
    }

#if UNITY_EDITOR_OSX
    [Ignore("UUM-110269")]
#endif
    [UnityTest]
    public IEnumerator TilemapRenderer_IndividualMode_WithSameSpriteRenderers_DynamicBatched_SingleBatchDrawCall()
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = true;

        var spritesDefaultMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        Assert.NotNull(spritesDefaultMat);

        m_TilemapRenderer.mode = TilemapRenderer.Mode.Individual;
        m_TilemapRenderer.material = spritesDefaultMat;

        m_Tilemap.SetTile(Vector3Int.zero, m_Tile);
        m_Tilemap.SetTile(new Vector3Int(0, 1, 0), m_Tile);
        m_Tilemap.SetTile(new Vector3Int(1, 0, 0), m_Tile);
        m_Tilemap.SetTile(new Vector3Int(1, 1, 0), m_Tile);
        Assert.AreEqual(m_Tile, m_Tilemap.GetTile(Vector3Int.zero));

        var srgo1 = new GameObject("Sprite", typeof(SpriteRenderer));
        srgo1.transform.position = Vector3Int.zero;
        var sr = srgo1.GetComponent<SpriteRenderer>();
        sr.material = spritesDefaultMat;
        sr.sprite = m_Tile.sprite;
        var srgo2 = new GameObject("Sprite", typeof(SpriteRenderer));
        srgo2.transform.position = new Vector3(1, 0, 0);
        sr = srgo2.GetComponent<SpriteRenderer>();
        sr.material = spritesDefaultMat;
        sr.sprite = m_Tile.sprite;

        Recorder recorder = null;
        var sampler = Sampler.Get("RenderLoop.DrawSRPBatcher");
        if (sampler.isValid)
            recorder = sampler.GetRecorder();

        // Wait for rendering
        int count = 0;
        for (var i = 0; i < 3; ++i)
        {
            if (recorder != null)
                count += recorder.sampleBlockCount;

            yield return null;
        }

        Object.Destroy(srgo1);
        Object.Destroy(srgo2);

        // Must have batched properly
        // Windows/OSX without batching: 5
        // Linux without batching: 4
        // Double for XR
        var multiplier = 1;
        if (UnityEngine.Rendering.XRGraphicsAutomatedTests.enabled)
            multiplier = 2;
        Assert.GreaterOrEqual(2 * multiplier, UnityStats.drawCalls);

        // SRP Batching is not used
        Assert.AreEqual(count, 0);

        // Must have rendered with at least one draw
        Assert.Greater(UnityStats.drawCalls, 0);

        // Must have done dynamic batching for both TilemapRenderer and SpriteRenderers
        Assert.AreEqual(1, UnityStats.dynamicBatches);
    }
#endif
}
