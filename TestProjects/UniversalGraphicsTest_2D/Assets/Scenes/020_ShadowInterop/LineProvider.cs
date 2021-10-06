using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    public class LineProvider : MonoBehaviour, IShadowShape2DProvider
    {
        void IShadowShape2DProvider.OnPersistantDataCreated(IShadowShape2DProvider.ShadowShapes2D persistantShapeObject)
        {
            NativeArray<Vector3> vertices = new NativeArray<Vector3>(3 + 4 + 3, Allocator.Temp);
            NativeArray<int> indices = new NativeArray<int>(4 + 4 + 8, Allocator.Temp);
            //NativeArray<Vector3> vertices = new NativeArray<Vector3>(3, Allocator.Temp);
            //NativeArray<int> indices = new NativeArray<int>(4, Allocator.Temp);

            // Two lines
            vertices[0] = new Vector3(-2, -1, 0);
            vertices[1] = new Vector3(-1, -1, 0);
            vertices[2] = new Vector3(-1,  1, 0);

            // Square
            vertices[3] = new Vector3(1, 1, 0);
            vertices[4] = new Vector3(2, 1, 0);
            vertices[5] = new Vector3(2, -1, 0);
            vertices[6] = new Vector3(1, -1, 0);

            vertices[7] = new Vector3(4, -1, 0);
            vertices[8] = new Vector3(3, -1, 0);
            vertices[9] = new Vector3(3,  1, 0);


            // indices lines
            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 1;
            indices[3] = 2;

            // indices square
            indices[4] = 3;
            indices[5] = 4;
            indices[6] = 4;
            indices[7] = 5;
            indices[8] = 5;
            indices[9] = 6;
            indices[10] = 6;
            indices[11] = 3;

            indices[12] = 7;
            indices[13] = 8;
            indices[14] = 8;
            indices[15] = 9;

            persistantShapeObject.SetShape(vertices, indices, IShadowShape2DProvider.OutlineTopology.Lines, true);
        }

        void IShadowShape2DProvider.OnBeforeRender(IShadowShape2DProvider.ShadowShapes2D persistantShapeObject, Bounds globalBounds) { }
    }
}
