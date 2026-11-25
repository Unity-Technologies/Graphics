#if ENABLE_2D_GRAPHICS
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.U2D.LowLevelGraphics.Experimental;

// Builds a 128x128 Tilemap at runtime and draws it in TilemapChunkPackMode.InstancedChunks (chunks with identical Tiles share one geometry copy).
// Must match 142 (Chunks); chunks collapse onto three distinct contents via (cx + 2 * cy) % 3. Textures are solid colours: UVs are covered by 140 and 141.
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public class TilemapChunkRendererInstancedChunksScene : MonoBehaviour
{
    const TilemapChunkPackMode k_PackMode = TilemapChunkPackMode.InstancedChunks;

    const int k_MapSize = 128;
    const int k_ChunkSize = 32;

    // Coarser than a Tile so flat regions dominate and only block edges differ across backends.
    // Texture varies by y in 4-row bands so a batched run spans a whole chunk row; tint supplies the x variation without ending runs.
    const int k_TextureBlockShiftX = 5;  // 32 cells: one chunk wide
    const int k_TextureBlockShiftY = 2;  // 4 cells, ~32 px
    const int k_TintBlockShift = 3;      // 8 cells, ~64 px

    [SerializeField]
    [Tooltip("Sprite material with instancing variants enabled. Deliberately the same in both scenes so the material is not one of the differences between them.")]
    Material m_Material;

    // Distinct enough to tell apart at eight pixels a Tile, and none of them near the
    // background, so a missing draw range reads as a hole rather than as a subtle shift.
    static readonly Color[] k_TextureColors =
    {
        new Color(0.85f, 0.20f, 0.20f, 1.0f),
        new Color(0.20f, 0.65f, 0.30f, 1.0f),
        new Color(0.25f, 0.40f, 0.85f, 1.0f),
    };

    // Part of what tells two chunks apart, because it is baked into the vertices here rather
    // than supplied per draw.
    static readonly Color[] k_Tints =
    {
        Color.white,
        new Color(0.45f, 0.45f, 0.45f, 1.0f),
    };

    Texture2D[] m_Textures;
    Sprite[] m_Sprites;
    Tile[] m_Tiles;
    TilemapChunkRenderDataCache m_Cache;
    TilemapChunkRenderer m_Renderer;
    GameObject m_TilemapGameObject;
    bool m_Checked;

    public SpriteMaskInteraction m_MaskInteraction;

    void Awake()
    {
        if (m_Material == null)
        {
            Debug.LogError($"{GetType().Name}: no Material assigned; nothing will be rendered.", this);
            return;
        }

        BuildTiles();
        BuildTilemap();
    }

    void BuildTiles()
    {
        m_Textures = new Texture2D[k_TextureColors.Length];
        m_Sprites = new Sprite[k_TextureColors.Length];

        for (int i = 0; i < k_TextureColors.Length; ++i)
        {
            // No mip chain and point filtering: a mip chain would make the minified result
            // depend on how each backend picks and rounds the level.
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false)
            {
                name = "TilemapChunkRendererTexture" + i,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[8 * 8];
            Color32 packed = k_TextureColors[i];
            for (int p = 0; p < pixels.Length; ++p)
                pixels[p] = packed;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            m_Textures[i] = texture;

            // Eight pixels per unit over an 8x8 texture, so one Tile covers exactly one cell.
            m_Sprites[i] = Sprite.Create(texture, new Rect(0.0f, 0.0f, 8.0f, 8.0f), new Vector2(0.5f, 0.5f), 8.0f);
            m_Sprites[i].name = "TilemapChunkRendererSprite" + i;
        }

        m_Tiles = new Tile[k_TextureColors.Length * k_Tints.Length];
        for (int t = 0; t < k_Tints.Length; ++t)
        {
            for (int i = 0; i < m_Sprites.Length; ++i)
            {
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = m_Sprites[i];
                // Tile.color rides TileData.color into the chunk build; Tilemap.SetColor over every cell would
                // not finish before capture.
                tile.color = k_Tints[t];
                m_Tiles[t * m_Sprites.Length + i] = tile;
            }
        }
    }

    void BuildTilemap()
    {
        m_TilemapGameObject = new GameObject("Tilemap", typeof(Grid), typeof(Tilemap), typeof(TilemapRenderer));
        m_TilemapGameObject.GetComponent<Grid>().cellSize = Vector3.one;

        var tilemap = m_TilemapGameObject.GetComponent<Tilemap>();

        // Present because the cache reads chunk size, sort order and culling bounds from it, but disabled so
        // Tiles are not drawn twice.
        var tilemapRenderer = m_TilemapGameObject.GetComponent<TilemapRenderer>();
        tilemapRenderer.enabled = false;
        tilemapRenderer.chunkSize = new Vector3Int(k_ChunkSize, k_ChunkSize, 1);
        tilemapRenderer.sortOrder = TilemapRenderer.SortOrder.BottomLeft;
        tilemapRenderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Manual;
        tilemapRenderer.chunkCullingBounds = Vector3.zero;

        PaintTiles(tilemap);

        m_Cache = m_TilemapGameObject.AddComponent<TilemapChunkRenderDataCache>();
        m_Cache.tilemap = tilemap;
        m_Cache.camera = Camera.main;
        m_Cache.packMode = k_PackMode;
        // Every visible chunk must be ready on the capture frame and none evicted; the default build throttle
        // would make the image frame-dependent.
        m_Cache.maxChunkBuildsPerFrame = 1024;
        m_Cache.residentChunkThreshold = 4096;

        // Added after the cache so RequireComponent does not create a second one.
        m_Renderer = m_TilemapGameObject.AddComponent<TilemapChunkRenderer>();
        m_Renderer.cache = m_Cache;
        m_Renderer.material = m_Material;
        m_Renderer.maskInteraction = m_MaskInteraction;
    }

    void PaintTiles(Tilemap tilemap)
    {
        var tiles = new TileBase[k_MapSize * k_MapSize];
        for (int y = 0; y < k_MapSize; ++y)
        {
            int row = y * k_MapSize;
            for (int x = 0; x < k_MapSize; ++x)
            {
                // Derived from the cell position, never from a random source, so the same
                // picture comes out on every run and on every platform.
                int texture = ((x >> k_TextureBlockShiftX) + (y >> k_TextureBlockShiftY)) % k_TextureColors.Length;
                int tint = ((x >> k_TintBlockShift) + (y >> k_TintBlockShift)) & 1;
                tiles[row + x] = m_Tiles[tint * k_TextureColors.Length + texture];
            }
        }

        // One bulk call. 16,384 individual SetTile calls would not finish in time.
        tilemap.SetTilesBlock(new BoundsInt(0, 0, 0, k_MapSize, k_MapSize, 1), tiles);
    }

    void LateUpdate()
    {
        // An empty frame would otherwise be published as a perfectly stable reference image,
        // so say so once rather than let it pass silently.
        if (m_Checked || m_Renderer == null)
            return;

        m_Checked = true;
        if (m_Renderer.renderedChunkCount == 0)
            Debug.LogError($"{GetType().Name}: no chunks were submitted; the captured image will be empty.", this);

        // Sharing nothing still draws the right picture, so without this the scene could
        // silently stop being a test of the mode.
        else if (m_Cache.sharedChunkDataCount >= m_Renderer.renderedChunkCount)
            Debug.LogError($"{GetType().Name}: {m_Renderer.renderedChunkCount} chunks hold {m_Cache.sharedChunkDataCount} distinct contents, so nothing was shared.", this);
    }

    void OnDestroy()
    {
        // Created with new/CreateInstance, so nothing else owns them when the harness moves on
        // to the next scene.
        if (m_Tiles != null)
        {
            for (int i = 0; i < m_Tiles.Length; ++i)
                Destroy(m_Tiles[i]);
        }
        if (m_Sprites != null)
        {
            for (int i = 0; i < m_Sprites.Length; ++i)
                Destroy(m_Sprites[i]);
        }
        if (m_Textures != null)
        {
            for (int i = 0; i < m_Textures.Length; ++i)
                Destroy(m_Textures[i]);
        }
        if (m_TilemapGameObject != null)
            Destroy(m_TilemapGameObject);
    }
}
#endif
