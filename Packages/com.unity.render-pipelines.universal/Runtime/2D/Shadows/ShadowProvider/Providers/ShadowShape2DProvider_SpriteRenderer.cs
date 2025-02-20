using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.U2D;


namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    class ShadowShape2DProvider_SpriteRenderer : ShadowShape2DProvider
    {
        const float k_InitialTrim = 0.05f;

        ShadowShape2D m_PersistantShapeData;
        SpriteDrawMode m_CurrentDrawMode;
        Vector2 m_CurrentDrawModeSize;

        void SetFullRectShapeData(SpriteRenderer spriteRenderer, ShadowShape2D shadowShape2D)
        {
            // Draw shadows for the full rect of the sprite...
            if (spriteRenderer.drawMode != SpriteDrawMode.Simple)
            {
                Sprite sprite = spriteRenderer.sprite;

                Vector2 srSize = spriteRenderer.size;
                Vector3 pivot = new Vector2(srSize.x * sprite.pivot.x / sprite.rect.width, srSize.y * sprite.pivot.y / sprite.rect.height);
                Rect rect = new Rect(-pivot, new Vector2(srSize.x, srSize.y));

                NativeArray<Vector3> vertices = new NativeArray<Vector3>(4, Allocator.Temp);
                NativeArray<int> indices = new NativeArray<int>(8, Allocator.Temp);

                vertices[0] = new Vector3(rect.min.x, rect.min.y);
                vertices[1] = new Vector3(rect.min.x, rect.max.y);
                vertices[2] = new Vector3(rect.max.x, rect.max.y);
                vertices[3] = new Vector3(rect.max.x, rect.min.y);

                indices[0] = 0;
                indices[1] = 1;
                indices[2] = 1;
                indices[3] = 2;
                indices[4] = 2;
                indices[5] = 3;
                indices[6] = 3;
                indices[7] = 0;

                shadowShape2D.SetShape(vertices, indices, ShadowShape2D.OutlineTopology.Lines);

                vertices.Dispose();
                indices.Dispose();
            }
        }

        void SetPersistantShapeData(Sprite sprite, ShadowShape2D shadowShape2D, NativeSlice<Vector3> vertexSlice)
        {
            if (shadowShape2D != null)
            {
                NativeArray<ushort> ushortIndices = sprite.GetIndices();

                NativeArray<int> indices = new NativeArray<int>(ushortIndices.Length, Allocator.Temp);
                NativeArray<Vector3> vertices = new NativeArray<Vector3>(vertexSlice.Length, Allocator.Temp);

                for (int i = 0; i < indices.Length; i++)
                    indices[i] = ushortIndices[i];

                for (int i = 0; i < vertices.Length; i++)
                    vertices[i] = vertexSlice[i];


                shadowShape2D.SetShape(vertices, indices, ShadowShape2D.OutlineTopology.Triangles);

                vertices.Dispose();
                indices.Dispose();
            }
        }

        void TryToSetPersistantShapeData(SpriteRenderer spriteRenderer, ShadowShape2D persistantShadowShape, bool force)
        {
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                if (spriteRenderer.drawMode != SpriteDrawMode.Simple && (spriteRenderer.size.x != m_CurrentDrawModeSize.x || spriteRenderer.size.y != m_CurrentDrawModeSize.y || spriteRenderer.drawMode != m_CurrentDrawMode || force))
                {
                    m_CurrentDrawModeSize = spriteRenderer.size;
                    SetFullRectShapeData(spriteRenderer, persistantShadowShape);
                }
                else if (spriteRenderer.drawMode != m_CurrentDrawMode || force)
                {
                    Sprite sprite = spriteRenderer.sprite;
                    NativeSlice<Vector3> vertexSlice = sprite.GetVertexAttribute<Vector3>(UnityEngine.Rendering.VertexAttribute.Position);
                    SetPersistantShapeData(sprite, m_PersistantShapeData, vertexSlice);
                }

                m_CurrentDrawMode = spriteRenderer.drawMode;
            }
        }

        void UpdatePersistantShapeData(SpriteRenderer spriteRenderer)
        {
            TryToSetPersistantShapeData(spriteRenderer, m_PersistantShapeData, true);
        }

        //============================================================================================================
        //                                                  Public
        //============================================================================================================
        public override int Priority() { return 1; }  // give higher than default menu priority
        public override bool IsShapeSource(Component sourceComponent) { return sourceComponent is SpriteRenderer; }

        public override void OnPersistantDataCreated(Component sourceComponent, ShadowShape2D persistantShadowShape)
        {
            SpriteRenderer spriteRenderer = (SpriteRenderer)sourceComponent;

            m_PersistantShapeData = persistantShadowShape as ShadowMesh2D;

            if (spriteRenderer.sprite != null)
            {
                float trimEdge = ShadowShapeProvider2DUtility.GetTrimEdgeFromBounds(spriteRenderer.bounds, k_InitialTrim);
                persistantShadowShape.SetDefaultTrim(trimEdge);
            }

            TryToSetPersistantShapeData(spriteRenderer, persistantShadowShape, true);
        }

        public override void OnBeforeRender(Component sourceComponent, Bounds worldCullingBounds, ShadowShape2D persistantShadowShape)
        {
            SpriteRenderer spriteRenderer = (SpriteRenderer)sourceComponent;
            persistantShadowShape.SetFlip(spriteRenderer.flipX, spriteRenderer.flipY);
            TryToSetPersistantShapeData(spriteRenderer, persistantShadowShape, false);
        }

        public override void Enabled(Component sourceComponent, ShadowShape2D persistantShadowShape)
        {
            SpriteRenderer spriteRenderer = (SpriteRenderer)sourceComponent;

            m_PersistantShapeData = persistantShadowShape;
            spriteRenderer.RegisterSpriteChangeCallback(UpdatePersistantShapeData);
        }

        public override void Disabled(Component sourceComponent, ShadowShape2D persistantShadowShape)
        {
            SpriteRenderer spriteRenderer = (SpriteRenderer)sourceComponent;
            spriteRenderer.UnregisterSpriteChangeCallback(UpdatePersistantShapeData);
        }
    }
}
