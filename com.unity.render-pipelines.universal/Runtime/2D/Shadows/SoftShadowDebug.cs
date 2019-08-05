using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.Experimental.Rendering.Universal
{

    [ExecuteInEditMode]
    public class SoftShadowDebug : MonoBehaviour
    {
        public Light2D m_Light;
        public float m_LightRadius;
        public LightReactor2D m_LightReactor;
        public bool m_ShadowOn = true;
        public bool m_ShadowClipping = true;


        void DrawDebugCircle(Vector3 position, float radius, Color color)
        {
            const int sides = 100;
            
            for(int i=0;i<sides;i++)
            {
                Vector3 v0 = new Vector3();
                Vector3 v1 = new Vector3();

                float angle = (2*Mathf.PI * (float)i) / (float)sides;
                v0.x = radius * Mathf.Cos(angle) + position.x;
                v0.y = radius * Mathf.Sin(angle) + position.y;
                v0.z = 0;

                float nextAngle = (2 * Mathf.PI * (float)(i+1)) / (float)sides; 
                v1.x = radius * Mathf.Cos(nextAngle) + position.x;
                v1.y = radius * Mathf.Sin(nextAngle) + position.y;
                Debug.DrawLine(v0, v1, color);
            }
        }

        void DrawTangents(Vector3 point, Vector3 circleOrigin, float radius, Vector3 tangent0, Vector3 tangent1)
        {
            Vector3 calcCenter = 0.5f * (point + circleOrigin);
            Vector3 A = circleOrigin;
            Vector3 B = calcCenter;
            float d = Vector3.Distance(calcCenter, circleOrigin);

            float ex = (B.x - A.x) / d;
            float ey = (B.y - A.y) / d;

            float x = (radius * radius - d * d + d * d) / (2 * d);
            float y = Mathf.Sqrt(radius * radius - x * x);

            Vector3 intersectionPt1 = new Vector3();
            intersectionPt1.x = A.x + x * ex - y * ey;
            intersectionPt1.y = A.y + x * ey + y * ex;

            Vector3 dir1 = Vector3.Normalize(intersectionPt1 - point);

            float dot1 = Vector3.Dot(dir1, tangent0);
            float dot2 = Vector3.Dot(dir1, tangent1);

            if (Mathf.Sign(dot1) != Mathf.Sign(dot2))
                Debug.DrawLine(point, intersectionPt1, Color.green);

            Vector3 intersectionPt2 = new Vector3();
            intersectionPt2.x = A.x + x * ex + y * ey;
            intersectionPt2.y = A.y + x * ey - y * ex;

            Vector3 dir2 = Vector3.Normalize(intersectionPt2 - point);
            float dot3 = Vector3.Dot(dir2, tangent0);
            float dot4 = Vector3.Dot(dir2, tangent1);

            if (Mathf.Sign(dot3) != Mathf.Sign(dot4))
                Debug.DrawLine(point, intersectionPt2, Color.magenta);
        }

        void DrawUnclippedShadows(Vector3 point, Vector3 circleOrigin, float radius, Vector3 tangent0, Vector3 tangent1, float shadowDist)
        {
            Vector3 calcCenter = 0.5f * (point + circleOrigin);
            Vector3 a = circleOrigin;
            Vector3 b = calcCenter;
            float dist = Vector3.Distance(calcCenter, circleOrigin);

            float nx = (b.x - a.x) / dist;
            float ny = (b.y - a.y) / dist;

            float distSq = dist * dist;
            float x = (radius * radius - distSq + distSq) / (2 * dist);
            float y = Mathf.Sqrt(radius * radius - x * x);

            Vector3 lightTan1 = new Vector3();
            lightTan1.x = a.x + x * nx - y * ny;
            lightTan1.y = a.y + x * ny + y * nx;

            // Should I relocate the normalize?
            Vector3 dir1 = Vector3.Normalize(lightTan1 - point);

            float dot1 = Vector3.Dot(dir1, tangent0);
            float dot2 = Vector3.Dot(dir1, tangent1);

            Vector3 lightTan2 = new Vector3();
            lightTan2.x = a.x + x * nx + y * ny;
            lightTan2.y = a.y + x * ny - y * nx;

            // Should I relocate the normalize?
            Vector3 dir2 = Vector3.Normalize(lightTan2 - point);
            float dot3 = Vector3.Dot(dir2, tangent0);
            float dot4 = Vector3.Dot(dir2, tangent1);

            if ((Mathf.Sign(dot1) != Mathf.Sign(dot2)) || (Mathf.Sign(dot3) != Mathf.Sign(dot4)))
            {
                Color shadowColor1 = Color.green;
                Color shadowColor2 = Color.green;

                Vector3 shadowPos1 = shadowDist * -dir1;
                Vector3 shadowPos2 = shadowDist * -dir2;

                Debug.DrawLine(point, point + shadowPos1, shadowColor1);
                Debug.DrawLine(point, point + shadowPos2, shadowColor2);
            }
        }

        void DrawClippedShadows(Vector3 point, Vector3 circleOrigin, float radius, Vector3 tangent0, Vector3 tangent1, float shadowDist)
        {
            Vector3 calcCenter = 0.5f * (point + circleOrigin);
            Vector3 A = circleOrigin;
            Vector3 B = calcCenter;
            float d = Vector3.Distance(calcCenter, circleOrigin);

            float ex = (B.x - A.x) / d;
            float ey = (B.y - A.y) / d;

            float x = (radius * radius - d * d + d * d) / (2 * d);
            float y = Mathf.Sqrt(radius * radius - x * x);

            Vector3 intersectionPt1 = new Vector3();
            intersectionPt1.x = A.x + x * ex - y * ey;
            intersectionPt1.y = A.y + x * ey + y * ex;

            Vector3 dir1 = Vector3.Normalize(intersectionPt1 - point);

            float dot1 = Vector3.Dot(dir1, tangent0);
            float dot2 = Vector3.Dot(dir1, tangent1);

            Vector3 intersectionPt2 = new Vector3();
            intersectionPt2.x = A.x + x * ex + y * ey;
            intersectionPt2.y = A.y + x * ey - y * ex;

            Vector3 dir2 = Vector3.Normalize(intersectionPt2 - point);
            float dot3 = Vector3.Dot(dir2, tangent0);
            float dot4 = Vector3.Dot(dir2, tangent1);

            if ((Mathf.Sign(dot1) != Mathf.Sign(dot2)) || (Mathf.Sign(dot3) != Mathf.Sign(dot4)))
            {
                Color shadowColor1 = Color.magenta;
                Color shadowColor2 = Color.magenta;

                Vector3 shadowPos1 = shadowDist * -dir1;
                Vector3 shadowPos2 = shadowDist * -dir2;

                if (Mathf.Max(dot1, dot2) > Mathf.Max(dot3, dot4))
                {
                    shadowColor1 = Color.black;
                    shadowColor2 = Color.white;
                }
                else
                {
                    shadowColor1 = Color.white;
                    shadowColor2 = Color.black;
                }

                if ((Mathf.Sign(dot1) != Mathf.Sign(dot2)) && (Mathf.Sign(dot3) == Mathf.Sign(dot4)))
                {
                    Vector3 dir;
                    if (Mathf.Sign(dot3) > 0)
                        dir = dot1 > dot2 ? tangent0 : tangent1;
                    else
                        dir = dot1 > dot2 ? -tangent1 : -tangent0;

                    shadowColor1 = Color.white;
                    shadowColor2 = Color.black;

                    shadowPos2 = shadowDist * -dir;
                }
                else if ((Mathf.Sign(dot1) == Mathf.Sign(dot2)) && (Mathf.Sign(dot3) != Mathf.Sign(dot4)))
                {
                    Vector3 dir;
                    if (Mathf.Sign(dot1) > 0)
                        dir = dot3 > dot4 ? tangent0 : tangent1;
                    else
                        dir = dot3 > dot4 ? -tangent1 : -tangent0;

                    shadowColor1 = Color.black;
                    shadowColor2 = Color.white;

                    shadowPos1 = shadowDist * -dir;
                }

                Debug.DrawLine(point, point + shadowPos1, shadowColor1);
                Debug.DrawLine(point, point + shadowPos2, shadowColor2);
            }
        }


        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            Vector3 circleCenter = m_Light.transform.position;

            DrawDebugCircle(circleCenter, m_LightRadius, Color.red);
            DrawDebugCircle(circleCenter, 0.02f, Color.red);

            Vector3[] vertices = m_LightReactor.softShadowMesh.vertices;
            Vector4[] tangents = m_LightReactor.softShadowMesh.tangents;

            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertexPos = m_LightReactor.transform.TransformPoint(vertices[i]);
                Vector3 tangentXY = m_LightReactor.transform.TransformDirection(new Vector4(tangents[i].x, tangents[i].y, 0, 0));
                Vector3 tangentZW = m_LightReactor.transform.TransformDirection(new Vector4(tangents[i].z, tangents[i].w, 0, 0));

                Debug.DrawLine(vertexPos, vertexPos + 0.3f * new Vector3(tangentXY.x, tangentXY.y), Color.red);
                Debug.DrawLine(vertexPos, vertexPos + 0.3f * new Vector3(tangentZW.x, tangentZW.y), Color.blue);

                if(!m_ShadowOn)
                    DrawTangents(vertexPos, circleCenter, m_LightRadius, tangentXY, tangentZW);
                else
                {
                    if(m_ShadowClipping)
                        DrawClippedShadows(vertexPos, circleCenter, m_LightRadius, tangentXY, tangentZW, 20);
                    else
                        DrawUnclippedShadows(vertexPos, circleCenter, m_LightRadius, tangentXY, tangentZW, 20);
                }
            }
        }
    }
}
