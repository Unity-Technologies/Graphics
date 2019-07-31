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


        void DrawDebugCircle(Vector3 position, float radius, Color color)
        {


            const int sides = 100;
            
            for(int i=0;i<sides;i++)
            {
                Vector3 v0 = new Vector3();
                Vector3 v1 = new Vector3();

                float angle = (2*Mathf.PI * (float)i) / (float)sides;
                v0.x = radius * Mathf.Cos(angle);
                v0.y = radius * Mathf.Sin(angle);
                v0.z = 0;

                float nextAngle = (2 * Mathf.PI * (float)(i+1)) / (float)sides; 
                v1.x = radius * Mathf.Cos(nextAngle);
                v1.y = radius * Mathf.Sin(nextAngle);
                Debug.DrawLine(v0, v1, color);
            }
        }

        //void DrawDebugCross(Vector3 position)



        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            DrawDebugCircle(m_Light.transform.position, m_LightRadius, Color.red);
        }
    }
}
