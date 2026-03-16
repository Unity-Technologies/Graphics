using System;
using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

public class BlendShapeTest : MonoBehaviour
{
    [Serializable]
    public class SpriteRendererAndWeight
    {
        public SpriteRenderer spriteRenderer;
        public int index;
        public float weight;
    }

    public Sprite sprite;
    public SpriteRendererAndWeight[] spriteRenderersAndWeights;

    public float firstFrameWeight = 50f;
    public float secondFrameWeight = 100f;

    void OnValidate()
    {
        if (sprite == null)
            return;

        AddBlendShape();
        SetWeightToSpriteRenderers();
    }

    void OnEnable()
    {
        if (sprite == null)
            return;

        AddBlendShape();
        SetWeightToSpriteRenderers();
    }

    public void AddBlendShape()
    {
        if (sprite == null)
            return;

        Vector3[] positions = sprite.GetVertexAttribute<Vector3>(VertexAttribute.Position).ToArray();

        // Clear all existing blend shapes.
        sprite.ClearBlendShapes();

        int index = sprite.AddBlendShape("Blend Shape");

        {
            // Add shrunk shape frame at firstFrameWeight.
            NativeArray<SpriteBlendShapeVertex> blendShapeVertices = new(positions.Length, Allocator.Temp);
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 position = positions[i];
                Vector3 targetPosition = new(position.x * 1.5f, position.y * 0.5f, position.z);

                blendShapeVertices[i] = new SpriteBlendShapeVertex
                {
                    index = (uint)i,
                    vertex = targetPosition - position,
                    normal = Vector3.zero,
                    tangent = Vector3.zero,
                };
            }

            sprite.AddBlendShapeFrame(index, firstFrameWeight, blendShapeVertices);
            blendShapeVertices.Dispose();
        }

        {
            // Add round shape frame at secondFrameWeight
            Bounds bounds = sprite.bounds;
            float radius = Mathf.Max(bounds.size.x, bounds.size.y) / 2.0f;

            NativeArray<SpriteBlendShapeVertex> blendShapeVertices = new(positions.Length, Allocator.Temp);
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 position = positions[i];
                Vector3 targetPosition = position.magnitude > 0
                    ? new Vector3(position.normalized.x * radius, position.normalized.y * radius, position.z)
                    : position;

                blendShapeVertices[i] = new SpriteBlendShapeVertex
                {
                    index = (uint)i,
                    vertex = targetPosition - position,
                    normal = Vector3.zero,
                    tangent = Vector3.zero,
                };
            }

            sprite.AddBlendShapeFrame(index, secondFrameWeight, blendShapeVertices);
            blendShapeVertices.Dispose();
        }
    }

    void SetWeightToSpriteRenderers()
    {
        foreach (var srAndWeight in spriteRenderersAndWeights)
        {
            if (srAndWeight.spriteRenderer != null)
            {
                srAndWeight.spriteRenderer.SetBlendShapeWeight(srAndWeight.index, srAndWeight.weight);
            }
        }
    }

}
