using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace UnityEngine.U2D
{
    public interface IShadowShapes2DProvider
    {
        public enum OutlineTopology
        {
            Lines,
            LineStrip,
            Triangles
        }
    
        public abstract class ShadowShapes2D
        {
            public abstract void SetEdges(Vector2[] vertices, ushort[] indices, OutlineTopology outlineTopology);
            public abstract void SetEdges(NativeArray<Vector2> vertices, NativeArray<ushort> indices, OutlineTopology outlineTopology);
            public abstract void UpdateEdges(Vector2[] vertices);
            public abstract void UpdateEdges(NativeArray<Vector2> vertices);
        }

        void OnShadowShapesStart(ShadowShapes2D shape);
    }
}
