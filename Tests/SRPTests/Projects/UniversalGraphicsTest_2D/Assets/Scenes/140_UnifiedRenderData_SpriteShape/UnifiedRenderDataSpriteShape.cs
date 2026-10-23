using System;
using UnityEngine;
using UnityEngine.U2D;

// Graphics test driver for Graphics.RenderUnifiedRenderData: replays the parent SpriteShapeRenderer geometry
// at four corner transforms. The fill has a null sprite by design, so only edges draw here; this is expected.
[ExecuteAlways]
public class UnifiedRenderDataSpriteShape : MonoBehaviour
{
    // Assigned by the scene author. Must be a material capable of rendering SpriteShape edge
    // geometry (the same material the parent SpriteShapeRenderer uses is the usual choice).
    [SerializeField] Material m_Material;

    // Uniform scale applied to each replayed instance so the four corner copies stay visually
    // distinct from the parent shape. Constant - graphics tests must be frame-deterministic.
    [SerializeField] float m_InstanceScale = 0.35f;

    SpriteShapeRenderer m_Renderer;
    UnifiedRenderData   m_Data;         // Extracted once, replayed every frame.
    Matrix4x4[]         m_LocalOffsets; // Corner placement in the parent's local space.
    bool                m_Extracted;    // Extraction attempted (successfully or not).
    bool                m_Failed;       // Hard failure: log once, then stop drawing.

    void OnEnable()
    {
        // Reset so a domain reload / re-enable re-extracts against current tessellation.
        m_Extracted = false;
        m_Failed = false;
        m_LocalOffsets = null;
        ReleaseData();
    }

    void OnDisable()
    {
        ReleaseData();
    }

    // The handle holds a reference on the renderer's native render data, so it has to be
    // disposed rather than dropped on the floor.
    void ReleaseData()
    {
        m_Data?.Dispose();
        m_Data = null;
    }

    void Fail(string what)
    {
        m_Failed = true;
        Debug.LogError($"{nameof(UnifiedRenderDataSpriteShape)}: {what}. Disabling.", this);
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
            color = m_Renderer.color,
            maskInteraction = m_Renderer.maskInteraction
        };

        Matrix4x4 parentToWorld = m_Renderer.transform.localToWorldMatrix;

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
        m_Renderer = GetComponentInParent<SpriteShapeRenderer>();
        if (m_Renderer == null)
        {
            Fail("no SpriteShapeRenderer found on this object or any parent");
            return;
        }

        if (m_Material == null)
        {
            Fail("m_Material is not assigned");
            return;
        }

        // Each extraction allocates a handle and a registry entry scanned linearly per draw, so extract once.
        // The handle keeps the extracted geometry alive while held.
        m_Data = m_Renderer.GetUnifiedRenderData();
        if (m_Data == null)
        {
            Fail("GetUnifiedRenderData() returned null");
            return;
        }

        if (m_Data.drawRangeCount == 0)
        {
            // Tessellation has not produced geometry yet; drop the handle and retry next frame.
            ReleaseData();
            m_Extracted = false;
            return;
        }

        BuildCornerOffsets();
    }

    // Four corners of the parent's local bounds in fixed order, composed with the parent transform at draw time.
    void BuildCornerOffsets()
    {
        Vector3 c = m_Renderer.localBounds.center;
        Vector3 e = m_Renderer.localBounds.extents * 1.34f;
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
