using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using Unity.Collections;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public class SpriteRenderable : IRenderable2D
    {
        Sprite m_Sprite;

        public SpriteRenderable(Sprite sprite)
        {
            m_Sprite = sprite;
        }

        public NativeArray<ushort> GetTriangles()
        {
            return m_Sprite.GetIndices();
        }
        public NativeSlice<Vector3> GetVertices()
        {
            return m_Sprite.GetVertexAttribute<Vector3>(VertexAttribute.Position);
        }

        public NativeSlice<Vector3> GetNormals()
        {
            return m_Sprite.GetVertexAttribute<Vector3>(VertexAttribute.Normal);
        }

        public int GetId()
        {
            return m_Sprite.GetInstanceID(); // not sure if this is the right thing at the moment...
        }

        public static SpriteRenderable GetSpriteRenderable(GameObject go)
        {
            if (go != null)
            {
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    return new SpriteRenderable(sr.sprite);
                }
            }
            return null;
        }
    }
}
