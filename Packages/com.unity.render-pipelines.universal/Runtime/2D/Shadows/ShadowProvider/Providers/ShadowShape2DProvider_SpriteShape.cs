#if USING_SPRITESHAPE
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.U2D;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class ShadowShape2DProvider_SpriteShape : ShadowShape2DProvider
    {
        const float k_InitialTrim = 0.02f;

        internal void UpdateShadows(SpriteShapeController spriteShapeController, ShadowShape2D persistantShapeData)
        {
            NativeArray<float2> shadowData = spriteShapeController.GetShadowShapeData();

            int shadowDataCount = shadowData.Length;
            if (shadowDataCount > 0)
            {
                bool isClosed = shadowData[0].x == shadowData[shadowDataCount - 1].x && shadowData[0].y == shadowData[shadowDataCount - 1].y;
                int vertexCount = isClosed ? shadowDataCount - 1 : shadowDataCount;

                int indexArraySize = 2 * shadowDataCount;

                NativeArray<Vector3> vertices = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
                NativeArray<int> indices = new NativeArray<int>(indexArraySize-2, Allocator.Temp);

                // Copy vertices
                for (int i = 0; i < vertexCount; i++)
                {
                    vertices[i] = new Vector3(shadowData[i].x, shadowData[i].y, 0);
                }

                // Copy indices
                for (int i = 0; i < shadowDataCount - 1; i++)
                {
                    int startIndex = 2 * i;
                    indices[startIndex] = i;
                    indices[startIndex + 1] = i+1;
                }

                if(isClosed)
                {
                    int lastEdgeIndex = 2 * vertexCount;
                    indices[lastEdgeIndex - 1] = 0;
                }

                persistantShapeData.SetShape(vertices, indices, ShadowShape2D.OutlineTopology.Lines);

                vertices.Dispose();
                indices.Dispose();
            }

            shadowData.Dispose();
        }

        public override int Priority() { return 10; }  // give higher than default menu priority

        public override void Enabled(Component sourceComponent, ShadowShape2D persistantShadowShape)
        {
            ((SpriteShapeController)sourceComponent).ForceShadowShapeUpdate(true);
        }

        public override void Disabled(Component sourceComponent, ShadowShape2D persistantShadowShape)
        {
            ((SpriteShapeController)sourceComponent).ForceShadowShapeUpdate(false);
        }

        public override bool IsShapeSource(Component sourceComponent)
        {
            return sourceComponent as SpriteShapeController;
        }

        public override void OnPersistantDataCreated(Component sourceComponent, ShadowShape2D persistantShadowShape)
        {
            SpriteShapeController spriteShapeController = (SpriteShapeController)sourceComponent;
            SpriteShapeRenderer spriteShapeRenderer;

            spriteShapeController.TryGetComponent<SpriteShapeRenderer>(out spriteShapeRenderer);

            float trimEdge = ShadowShapeProvider2DUtility.GetTrimEdgeFromBounds(spriteShapeRenderer.bounds, k_InitialTrim);
            persistantShadowShape.SetDefaultTrim(trimEdge);

            UpdateShadows(spriteShapeController, persistantShadowShape);
        }

        public override void OnBeforeRender(Component sourceComponent, Bounds worldCullingBounds, ShadowShape2D persistantShadowShape)
        {
            UpdateShadows((SpriteShapeController)sourceComponent, persistantShadowShape);
        }
    }
}
#endif
