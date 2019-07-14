using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public class LightTest : MonoBehaviour
    {
        public MeshFilter filter;
        public float m_DebugShadowLength = 20;
        public float m_ShadowSize = 1;

        void DebugDrawSoftShadowEdge()
        {
            int vertexCount = filter.mesh.vertexCount;
            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 position = filter.mesh.vertices[i];
                Vector2 lightDirection = Vector3.Normalize(transform.position - position);

                Vector4 tangent = filter.mesh.tangents[i];
                Vector2 tangentXY = new Vector2(tangent.x, tangent.y);
                Vector2 tangentZW = new Vector2(tangent.z, tangent.w);

                float passesXY = Mathf.Clamp01(Mathf.Ceil(-Vector2.Dot(lightDirection, -tangentXY)));
                float passesZW = Mathf.Clamp01(Mathf.Ceil(-Vector2.Dot(lightDirection, -tangentZW)));

                float isSoftShadow = Mathf.Clamp01(Mathf.Ceil((Mathf.Abs(tangent.z) + Mathf.Abs(tangent.w))));
                float isSoftShadowCorner = isSoftShadow * Mathf.Abs(passesXY - passesZW);

                Vector3 endpoint = position + isSoftShadowCorner * (m_DebugShadowLength * -(Vector3)lightDirection);
                Debug.DrawLine(position, endpoint, Color.white);

                Vector2 softShadowTangentDir = passesZW * tangentZW + passesXY * tangentXY;
                Vector3 cross1 = Vector3.Cross(softShadowTangentDir, -lightDirection);
                Vector3 maxAngle = Vector3.Normalize(Vector3.Cross(cross1, -lightDirection));

                float angle = Vector2.Dot(softShadowTangentDir, lightDirection);
                float t = 1-Mathf.Abs(2 * angle * angle - 1);

                Vector3 offset = t * maxAngle;

                Debug.DrawLine(position, endpoint + isSoftShadowCorner * m_ShadowSize*(Vector3)offset, Color.green);
            }

        }


        void Update()
        {
            Shader.SetGlobalVector("_LightPos", transform.position);
            Shader.SetGlobalFloat("_ShadowLength", 20);

            DebugDrawSoftShadowEdge();
        }
    }
}
