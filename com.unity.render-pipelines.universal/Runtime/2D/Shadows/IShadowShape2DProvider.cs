using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace UnityEngine.U2D
{
    public interface IShadowShape2DProvider
    {
        public enum OutlineTopology
        {
            Lines,
            LineStrip,
            Triangles
        }

        public abstract class ShadowShapes2D
        {
            public abstract bool supportsContraction { get; set; }

            public abstract void CreateShape(Vector2[] vertices, ushort[] indices, OutlineTopology outlineTopology);
            public abstract void CreateShape(Vector3[] vertices, ushort[] indices, OutlineTopology outlineTopology);
            public abstract void CreateShape(NativeArray<Vector2> vertices, NativeArray<int> indices, OutlineTopology outlineTopology);
            public abstract void UpdateVertices(Vector2[] vertices);
            public abstract void UpdateVertices(NativeArray<Vector2> vertices);
        }

        void OnShapeObjectCreated(ShadowShapes2D persistantShapeObject);
    }
}
