using System;
using UnityEngine;
using UnityEngine.U2D;

// Graphics test driver for Graphics.RenderUnifiedRenderData: replays the SpriteRenderer render data
// at four corner transforms around the Sprite bounds.
[ExecuteAlways]
public class UnifiedRenderDataSprite : MonoBehaviour
{
    // Assigned by the scene author. Must be a material capable of rendering sprite geometry
    // (the material m_Renderer itself uses is the usual choice).
    [SerializeField] Material m_Material;

    // Uniform scale applied to each replayed instance so the four corner copies stay visually
    // distinct from one another. Constant - graphics tests must be frame-deterministic.
    [SerializeField] float m_InstanceScale = 0.35f;

    SpriteRenderer m_Renderer;
    public Color m_Color = Color.white;
    public SpriteMaskInteraction m_MaskInteraction;

    UnifiedRenderData   m_Data;         // Extracted once, replayed every frame.
    Matrix4x4[]         m_LocalOffsets; // Corner placement in this object's local space.
    bool                m_Extracted;    // Extraction attempted (successfully or not).
    bool                m_Failed;       // Hard failure: log once, then stop drawing.

    void OnEnable()
    {
        // Reset so a domain reload / re-enable re-extracts against the Sprite's current geometry.
        m_Extracted = false;
        m_Failed = false;
        m_LocalOffsets = null;
        ReleaseData();
    }

    void OnDisable()
    {
        ReleaseData();
    }

    // The handle holds a reference on the RENDERER's render data, taken on its behalf when the
    // handle was created, so it has to be disposed rather than dropped on the floor.
    void ReleaseData()
    {
        m_Data?.Dispose();
        m_Data = null;
    }

    void Fail(string what)
    {
        m_Failed = true;
        Debug.LogError($"{nameof(UnifiedRenderDataSprite)}: {what}. Disabling.", this);
    }

    void Update()
    {
        if (m_Failed)
            return;

        if (!m_Extracted)
        {
            m_Extracted = true;
            Extract();
            if (m_Failed)
                return;
        }

        if (m_Data == null || m_Material == null || m_LocalOffsets == null)
            return;

        // No camera (so every camera culls against it), shadows off, sorting layer 0 at order 0.
        var renderParams = new RenderParams(m_Material)
        {
            layer = gameObject.layer,
        };
        var spriteParams = new SpriteParams(m_Data)
        {
            color = m_Color,
            maskInteraction = m_MaskInteraction
        };

        Matrix4x4 parentToWorld = transform.localToWorldMatrix;

        for (int i = 0; i < m_LocalOffsets.Length; ++i)
        {
            // Invalid data throws; surface it once, then disable further drawing to avoid throwing every frame.
            try
            {
                Graphics.RenderUnifiedRenderData(renderParams, spriteParams, parentToWorld * m_LocalOffsets[i]);
            }
            catch (Exception e)
            {
                m_Failed = true;
                Debug.LogException(e, this);
                return;
            }
        }
    }

    void Extract()
    {
        // Not just defensive: nothing else guarantees m_Data is null on the retry path below.
        ReleaseData();

        m_Renderer = GetComponentInParent<SpriteRenderer>();
        if (m_Renderer == null)
        {
            Fail("m_Renderer is not assigned");
            return;
        }

        if (m_Material == null)
        {
            Fail("m_Material is not assigned");
            return;
        }

        // Each extraction allocates a registry entry scanned linearly per draw, so extract once.
        // The entry references the Sprite by EntityId without keeping it alive.
        m_Data = m_Renderer.GetUnifiedRenderData();
        if (m_Data == null)
        {
            Fail("GetUnifiedRenderData() returned null");
            return;
        }

        if (m_Data.drawRangeCount == 0)
        {
            // The Sprite's geometry has not been resolved into a range yet; retry next frame.
            ReleaseData();
            m_Extracted = false;
            return;
        }

        BuildCornerOffsets();
    }

    // Four corners of the Sprite local bounds in fixed order, composed with this transform at draw time.
    void BuildCornerOffsets()
    {
        // The SPRITE's bounds, not Renderer.bounds, which is world space.
        Bounds b = m_Renderer.sprite.bounds;

        // Sliced and Tiled geometry spans the renderer's size rather than the Sprite's rect, still
        // laid out around the Sprite's pivot.
        if (m_Renderer.drawMode != SpriteDrawMode.Simple)
        {
            Sprite sprite = m_Renderer.sprite;
            Vector2 size = m_Renderer.size;
            Vector2 pivot = sprite.pivot / sprite.rect.size;
            b = new Bounds(new Vector3((0.5f - pivot.x) * size.x, (0.5f - pivot.y) * size.y, b.center.z), new Vector3(size.x, size.y, b.size.z));
        }
        Vector3 c = b.center;
        Vector3 e = b.extents * 1.34f;
        Vector3 scale = Vector3.one * m_InstanceScale;

        m_LocalOffsets = new[]
        {
            Matrix4x4.TRS(new Vector3(c.x - e.x, c.y - e.y, c.z), Quaternion.identity, scale),
            Matrix4x4.TRS(new Vector3(c.x + e.x, c.y - e.y, c.z), Quaternion.identity, scale),
            Matrix4x4.TRS(new Vector3(c.x - e.x, c.y + e.y, c.z), Quaternion.identity, scale),
            Matrix4x4.TRS(new Vector3(c.x + e.x, c.y + e.y, c.z), Quaternion.identity, scale),
        };
    }
}
