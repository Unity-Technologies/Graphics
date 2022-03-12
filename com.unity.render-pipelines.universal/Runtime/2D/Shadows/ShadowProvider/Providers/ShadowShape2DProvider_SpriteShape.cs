using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.U2D;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    public class ShadowShape2DProvider_SpriteShape : ShadowShape2DProvider
    {
        int ColliderDataCount(NativeArray<float2> colliderData)
        {
            int count = 0;
            for (; count < colliderData.Length; count++)
            {
                if (colliderData[count].x == 0 && colliderData[count].y == 0)
                    return count;
            }
            return count;
        }

        internal void UpdateShadows(SpriteShapeController spriteShapeController, ShadowShape2D persistantShapeData)
        {
            NativeArray<float2> colliderData = spriteShapeController.GetColliderData();

            bool isOpenEnded = spriteShapeController.spline.isOpenEnded;

            int colliderDataCount = ColliderDataCount(colliderData);
            if (colliderDataCount > 0)
            {

                int indexArraySize = 2 * (colliderDataCount - (isOpenEnded ? 1 : 0));

                NativeArray<Vector3> vertices = new NativeArray<Vector3>(colliderDataCount, Allocator.Temp);
                NativeArray<int> indices = new NativeArray<int>(indexArraySize, Allocator.Temp);

                // Copy vertices
                for (int i = 0; i < colliderDataCount; i++)
                {
                    vertices[i] = new Vector3(colliderData[i].x, colliderData[i].y, 0);
                }

                // Copy indices
                int lastIndex = colliderDataCount - 1;
                for (int i = 0; i < colliderDataCount; i++)
                {
                    if (!isOpenEnded || i > 0)
                    {
                        int startIndex = 2 * (i - (isOpenEnded ? 1 : 0));
                        indices[startIndex] = lastIndex;
                        indices[startIndex + 1] = i;
                    }

                    lastIndex = i;
                }

                persistantShapeData.SetShape(vertices, indices, ShadowShape2D.OutlineTopology.Lines);
            }
        }

        public override bool CanProvideShape(in Component sourceComponent) { return sourceComponent is SpriteShapeController; }

        public override void OnPersistantDataCreated(in Component sourceComponent, ShadowShape2D persistantShapeData)
        {
            UpdateShadows((SpriteShapeController)sourceComponent, persistantShapeData);
        }

        public override void OnBeforeRender(in Component sourceComponent, in Bounds worldCullingBounds, ShadowShape2D persistantShapeObject)
        {
            UpdateShadows((SpriteShapeController)sourceComponent, persistantShapeObject);
        }
    }
}
