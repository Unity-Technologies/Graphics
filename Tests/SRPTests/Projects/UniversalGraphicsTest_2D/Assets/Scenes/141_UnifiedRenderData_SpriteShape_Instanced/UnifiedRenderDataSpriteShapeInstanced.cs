using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.U2D;

// Instanced sibling of 140_UnifiedRenderData_SpriteShape: one RenderUnifiedRenderDataInstanced call instead of four,
// otherwise identical so pixel differences are due to the draw path. The fill is skipped (null sprite) and range draw order is not preserved.
[ExecuteAlways]
public class UnifiedRenderDataSpriteShapeInstanced : MonoBehaviour
{
    // Assigned by the scene author. Must be a material capable of rendering SpriteShape edge
    // geometry, with instancing variants enabled.
    [SerializeField] Material m_Material;

    // Uniform scale applied to each replayed instance so the four corner copies stay visually
    // distinct from the parent shape. Constant - graphics tests must be frame-deterministic.
    [SerializeField] float m_InstanceScale = 0.35f;

    const int kInstanceCount = 4;

    SpriteShapeRenderer m_Renderer;
    UnifiedRenderData   m_Data;         // Extracted once, replayed every frame.
    Matrix4x4[]         m_LocalOffsets; // Corner placement in the parent's local space.
    bool                m_Extracted;    // Extraction attempted (successfully or not).
    bool                m_Failed;       // Hard failure: log once, then stop drawing.

    // Persistent native array read by the binding without copying, since it outlives the frame it is filled in.
    NativeArray<Matrix4x4> m_Transforms;

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

    // Dispose the handle and the native transform array; OnDisable runs before a domain reload so nothing leaks.
    void ReleaseData()
    {
        m_Data?.Dispose();
        m_Data = null;

        if (m_Transforms.IsCreated)
            m_Transforms.Dispose();
    }

    void Fail(string what)
    {
        m_Failed = true;
        Debug.LogError($"{nameof(UnifiedRenderDataSpriteShapeInstanced)}: {what}. Disabling.", this);
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

        if (m_Data == null || m_Material == null || m_LocalOffsets == null || !m_Transforms.IsCreated)
            return;

        // Recomputed every frame so the scene stays correct if the author moves, rotates or scales
        // the parent. Four elements, so the cost is irrelevant next to the draw.
        Matrix4x4 parentToWorld = m_Renderer.transform.localToWorldMatrix;
        for (int i = 0; i < m_LocalOffsets.Length; ++i)
            m_Transforms[i] = parentToWorld * m_LocalOffsets[i];

        // Defaults match what 140 draws with: no camera (so every camera culls against it),
        // shadows off, sorting layer 0 at order 0.
        var renderParams = new RenderParams(m_Material)
        {
            layer = gameObject.layer,
        };
        var spriteParams = new SpriteParams(m_Data)
        {
            color = m_Renderer.color,
            maskInteraction = m_Renderer.maskInteraction
        };

        // One call for all four instances; invalid data throws, so surface it once and then disable drawing.
        try
        {
            Graphics.RenderUnifiedRenderDataInstanced(renderParams, spriteParams, m_Transforms);
        }
        catch (Exception e)
        {
            m_Failed = true;
            Debug.LogException(e, this);
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

        if (m_Transforms.IsCreated)
            m_Transforms.Dispose();
        m_Transforms = new NativeArray<Matrix4x4>(kInstanceCount, Allocator.Persistent);
    }
}
