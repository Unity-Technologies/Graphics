using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.U2D;

namespace UnityEngine.Rendering.Universal
{
    class ShadowShape2DProvider_SpriteRenderer : ShadowShape2DProvider
    {
        ShadowShape2D m_PersistantShapeData;

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

        void UpdatePersistantShapeData(SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                Sprite sprite = spriteRenderer.sprite;
                NativeSlice <Vector3> vertexSlice = sprite.GetVertexAttribute<Vector3>(UnityEngine.Rendering.VertexAttribute.Position);
                SetPersistantShapeData(sprite, m_PersistantShapeData, vertexSlice);
            }
        }

        // This was added for the 2D Animation System
        void SetShadowShape(NativeArray<Vector3> vertices, NativeArray<int> indices, ShadowShape2D.OutlineTopology topology)
        {
            m_PersistantShapeData.SetShape(vertices, indices, topology);
        }

        // This was added for the 2D Animation System
        void UpdateShadowVertices(NativeArray<Vector3> vertices)
        {
            m_PersistantShapeData.UpdateVertices(vertices);
        }

        //============================================================================================================
        //                                                  Public
        //============================================================================================================
        public override int MenuPriority()  { return 1; }  // give higher than default menu priority
        public override bool CanProvideShape(in Component sourceComponent) { return sourceComponent is SpriteRenderer; }

        public override void OnPersistantDataCreated(in Component sourceComponent, ShadowShape2D persistantShapeData)
        {
            SpriteRenderer sr = (SpriteRenderer)sourceComponent;

            if (sr != null && sr.sprite != null)
            {
                Sprite sprite = sr.sprite;
                NativeSlice<Vector3> vertexSlice = sprite.GetVertexAttribute<Vector3>(UnityEngine.Rendering.VertexAttribute.Position);

                m_PersistantShapeData = persistantShapeData;
                SetPersistantShapeData(sprite, m_PersistantShapeData, vertexSlice);
                sr.RegisterSpriteChangeCallback(UpdatePersistantShapeData);
            }
        }

        public override void OnBeforeRender(in Component sourceComponent, in Bounds worldCullingBounds, ShadowShape2D persistantShapeObject)
        {
            SpriteRenderer sr = (SpriteRenderer)sourceComponent;
            if (sr != null && sr.sprite != null)
            {
                if (sr.HasDeformedVertices())
                {
                    Sprite sprite = sr.sprite;

                    NativeSlice<Vector3> vertices = sr.GetDeformedVertices();
                    SetPersistantShapeData(sprite, persistantShapeObject, vertices);
                }
            }
        }
    }
}
