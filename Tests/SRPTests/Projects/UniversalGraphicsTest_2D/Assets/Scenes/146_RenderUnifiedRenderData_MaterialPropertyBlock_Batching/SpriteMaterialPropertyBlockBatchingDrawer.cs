using UnityEngine;
using UnityEngine.U2D;

// Graphics test driver: draws four Sprites with different solid-colour textures, all sharing one MaterialPropertyBlock
// that does not touch _MainTex. A shared block gives every node the same custom props, so it must not let Sprites with
// different textures be drawn as one batch.
// The left four columns use Graphics.RenderSprite and the right four Graphics.RenderUnifiedRenderData, each in red,
// green, blue and yellow. Rows are, top to bottom: no mask interaction and VisibleOutsideMask with an instancing
// material, then the same two with a non-instancing material.
// Every cell must show its own colour.
[ExecuteAlways]
public class SpriteMaterialPropertyBlockBatchingDrawer : MonoBehaviour
{
    const int k_TextureSize = 4;
    const float k_PixelsPerUnit = 2f;
    const float k_ColumnSpacing = 2.75f;
    const float k_RowSpacing = 5f;

    static readonly Color[] k_Colors = { Color.red, Color.green, Color.blue, Color.yellow };
    static readonly SpriteMaskInteraction[] k_MaskInteractions = { SpriteMaskInteraction.None, SpriteMaskInteraction.VisibleOutsideMask };

    // Must have GPU instancing enabled; a copy with it disabled covers the dynamic batching path.
    [SerializeField] Material m_Material;

    Material m_NonInstancedMaterial;
    Texture2D[] m_Textures;
    Sprite[] m_Sprites;
    UnifiedRenderData[] m_UnifiedData;
    MaterialPropertyBlock m_Block;

    static Texture2D CreateSolidTexture(Color color)
    {
        var texture = new Texture2D(k_TextureSize, k_TextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            hideFlags = HideFlags.DontSave,
        };
        var pixels = new Color[k_TextureSize * k_TextureSize];
        for (int i = 0; i < pixels.Length; ++i)
            pixels[i] = color;
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    void OnEnable()
    {
        m_Textures = new Texture2D[k_Colors.Length];
        m_Sprites = new Sprite[k_Colors.Length];
        m_UnifiedData = new UnifiedRenderData[k_Colors.Length];
        for (int i = 0; i < k_Colors.Length; ++i)
        {
            m_Textures[i] = CreateSolidTexture(k_Colors[i]);
            m_Sprites[i] = Sprite.Create(m_Textures[i], new Rect(0, 0, k_TextureSize, k_TextureSize), new Vector2(0.5f, 0.5f), k_PixelsPerUnit, 0, SpriteMeshType.FullRect);
            m_Sprites[i].hideFlags = HideFlags.DontSave;

            m_UnifiedData[i] = new UnifiedRenderData();
            m_UnifiedData[i].AddSprite(m_Sprites[i]);
            m_UnifiedData[i].RecalculateDrawRangesFromSprites();
        }

        if (m_Material != null)
        {
            m_NonInstancedMaterial = new Material(m_Material) { enableInstancing = false, hideFlags = HideFlags.DontSave };
        }

        // Any property other than the Sprite's textures; the block's presence is what matters.
        m_Block = new MaterialPropertyBlock();
        m_Block.SetFloat("_BatchProbe", 1f);
    }

    void OnDisable()
    {
        for (int i = 0; i < k_Colors.Length; ++i)
        {
            m_UnifiedData[i]?.Dispose();
            DestroyImmediate(m_Sprites[i]);
            DestroyImmediate(m_Textures[i]);
        }

        DestroyImmediate(m_NonInstancedMaterial);
    }

    void Update()
    {
        if (m_Material == null)
            return;

        Material[] materials = { m_Material, m_NonInstancedMaterial };
        for (int materialIndex = 0; materialIndex < materials.Length; ++materialIndex)
        {
            for (int maskIndex = 0; maskIndex < k_MaskInteractions.Length; ++maskIndex)
            {
                int row = materialIndex * k_MaskInteractions.Length + maskIndex;
                SpriteMaskInteraction maskInteraction = k_MaskInteractions[maskIndex];

                // A sorting order per row keeps each row's Sprites adjacent in the render queue, where they can batch.
                var renderParams = new RenderParams(materials[materialIndex])
                {
                    layer = gameObject.layer,
                    matProps = m_Block,
                    sortingOrder = row,
                };

                for (int i = 0; i < m_Sprites.Length; ++i)
                {
                    var spriteParams = new SpriteParams(m_Sprites[i], Color.white, maskInteraction);
                    Graphics.RenderSprite(renderParams, spriteParams, 0, CellToWorld(i, row));
                }

                for (int i = 0; i < m_UnifiedData.Length; ++i)
                {
                    var unifiedParams = new SpriteParams(m_UnifiedData[i]) { maskInteraction = maskInteraction };
                    Graphics.RenderUnifiedRenderData(renderParams, unifiedParams, CellToWorld(m_Sprites.Length + i, row));
                }
            }
        }
    }

    Matrix4x4 CellToWorld(int column, int row)
    {
        var offset = new Vector3((column - 3.5f) * k_ColumnSpacing, (1.5f - row) * k_RowSpacing, 0f);
        return transform.localToWorldMatrix * Matrix4x4.Translate(offset);
    }
}
