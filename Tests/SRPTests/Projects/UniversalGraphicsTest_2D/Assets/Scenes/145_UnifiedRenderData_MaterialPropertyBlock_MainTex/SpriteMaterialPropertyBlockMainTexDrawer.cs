using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.U2D;

// Graphics test driver for UUM-158701: draws a red Sprite through every sprite draw API with a blue _MainTex
// in RenderParams.matProps. Columns are RenderSprite, RenderSpriteInstanced, RenderUnifiedRenderData and
// RenderUnifiedRenderDataInstanced; the top row uses no mask interaction, the bottom row VisibleOutsideMask.
// Every cell must be blue.
[ExecuteAlways]
public class SpriteMaterialPropertyBlockMainTexDrawer : MonoBehaviour
{
    struct InstanceData
    {
        public Matrix4x4 objectToWorld;
    }

    const int k_TextureSize = 4;
    const float k_CellSpacing = 6f;

    static readonly SpriteMaskInteraction[] k_MaskInteractions = { SpriteMaskInteraction.None, SpriteMaskInteraction.VisibleOutsideMask };

    // Must have GPU instancing enabled, as the instanced APIs require it.
    [SerializeField] Material m_Material;

    Texture2D m_SpriteTexture;
    Texture2D m_OverrideTexture;
    Sprite m_Sprite;
    GameObject m_SourceObject;
    SpriteRenderer m_SourceRenderer;
    UnifiedRenderData m_Data;
    MaterialPropertyBlock m_Block;
    InstanceData[] m_InstanceData;
    NativeArray<Matrix4x4> m_UnifiedTransforms;
    bool m_Failed;

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
        m_Failed = false;
        m_SpriteTexture = CreateSolidTexture(Color.red);
        m_OverrideTexture = CreateSolidTexture(Color.blue);
        m_Sprite = Sprite.Create(m_SpriteTexture, new Rect(0, 0, k_TextureSize, k_TextureSize), new Vector2(0.5f, 0.5f), 1, 0, SpriteMeshType.FullRect);
        m_Sprite.hideFlags = HideFlags.DontSave;

        // Only the source of the UnifiedRenderData, never drawn itself.
        m_SourceObject = new GameObject("UnifiedRenderDataSource") { hideFlags = HideFlags.HideAndDontSave };
        m_SourceRenderer = m_SourceObject.AddComponent<SpriteRenderer>();
        m_SourceRenderer.sprite = m_Sprite;
        m_SourceRenderer.sharedMaterial = m_Material;
        m_SourceRenderer.enabled = false;

        m_Block = new MaterialPropertyBlock();
        m_Block.SetTexture("_MainTex", m_OverrideTexture);

        m_InstanceData = new InstanceData[1];
        m_UnifiedTransforms = new NativeArray<Matrix4x4>(1, Allocator.Persistent);
    }

    void OnDisable()
    {
        m_Data?.Dispose();
        m_Data = null;
        if (m_UnifiedTransforms.IsCreated)
            m_UnifiedTransforms.Dispose();

        DestroyImmediate(m_SourceObject);
        DestroyImmediate(m_Sprite);
        DestroyImmediate(m_OverrideTexture);
        DestroyImmediate(m_SpriteTexture);
    }

    void Update()
    {
        if (m_Failed || m_Material == null)
            return;

        if (m_Data == null)
        {
            m_Data = m_SourceRenderer.GetUnifiedRenderData();
            // The Sprite's geometry is not resolved into a range yet; retry next frame.
            if (m_Data != null && m_Data.drawRangeCount == 0)
            {
                m_Data.Dispose();
                m_Data = null;
            }
        }

        var renderParams = new RenderParams(m_Material)
        {
            layer = gameObject.layer,
            matProps = m_Block,
        };

        try
        {
            for (int row = 0; row < k_MaskInteractions.Length; ++row)
            {
                SpriteMaskInteraction maskInteraction = k_MaskInteractions[row];
                var spriteParams = new SpriteParams(m_Sprite, Color.white, maskInteraction);

                Graphics.RenderSprite(renderParams, spriteParams, 0, CellToWorld(0, row));

                m_InstanceData[0].objectToWorld = CellToWorld(1, row);
                Graphics.RenderSpriteInstanced(renderParams, spriteParams, 0, m_InstanceData);

                if (m_Data == null)
                    continue;

                var unifiedParams = new SpriteParams(m_Data) { maskInteraction = maskInteraction };
                Graphics.RenderUnifiedRenderData(renderParams, unifiedParams, CellToWorld(2, row));

                m_UnifiedTransforms[0] = CellToWorld(3, row);
                Graphics.RenderUnifiedRenderDataInstanced(renderParams, unifiedParams, m_UnifiedTransforms);
            }
        }
        catch (Exception e)
        {
            m_Failed = true;
            Debug.LogException(e, this);
        }
    }

    Matrix4x4 CellToWorld(int column, int row)
    {
        var offset = new Vector3((column - 1.5f) * k_CellSpacing, (0.5f - row) * k_CellSpacing, 0f);
        return transform.localToWorldMatrix * Matrix4x4.Translate(offset);
    }
}
