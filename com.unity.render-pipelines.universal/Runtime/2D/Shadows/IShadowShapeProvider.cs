using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace UnityEngine.U2D
{
    interface IShadowShapesProvider
    {
        public enum OutlineTopology
        {
            Lines,
            LineStrip,
            Triangles
        }

    
        public abstract class ShadowShapes
        {
            public abstract void SetEdges(Vector2[] vertices, int[] indices, OutlineTopology outlineTopology);
            public abstract void SetEdges(NativeArray<Vector2> vertices, NativeArray<int> indices, OutlineTopology outlineTopology);
            public abstract void UpdateEdges(Vector2[] vertices);
            public abstract void UpdateEdges(NativeArray<Vector2> vertices);
        }

        void Initialize(ShadowShapes shape);
    }
}
