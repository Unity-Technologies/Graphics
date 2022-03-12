using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    public abstract class ShadowShape2D
    {
        public enum OutlineTopology
        {
            Lines,
            Triangles
        }

        public enum WindingOrder
        {
            Clockwise,
            CounterClockwise
        }

        public abstract void SetShape(NativeArray<Vector3> vertices, NativeArray<int> indices, NativeArray<float> radii, WindingOrder windingOrder = WindingOrder.Clockwise, bool allowContraction = true);
        public abstract void SetShape(NativeArray<Vector3> vertices, NativeArray<int> indices, OutlineTopology outlineTopology, WindingOrder windingOrder = WindingOrder.Clockwise, bool allowContraction = true);
        public abstract void UpdateVertices(NativeArray<Vector3> vertices);
    }
}
