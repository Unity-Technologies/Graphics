using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEditor.Experimental.Rendering.Universal
{
    internal static class FreeformPathPresets
    {
        public static Vector3[] CreateSquare()
        {
            Vector3[] returnPath = new Vector3[4]
            {
                new Vector3(-0.5f, -0.5f),
                new Vector3(0.5f, -0.5f),
                new Vector3(0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f)
            };

            return returnPath;
        }

        public static Vector3[] CreateIsometricDiamond()
        {
            Vector3[] returnPath = new Vector3[4]
            {
                new Vector3(-0.5f, 0.0f),
                new Vector3(0.0f, -0.25f),
                new Vector3(0.5f, 0.0f),
                new Vector3(0.0f, 0.25f)
            };

            return returnPath;
        }

        private static Vector3[] CreateShape(int vertices, float angleOffset)
        {
            Vector3[] returnPath = new Vector3[vertices];
            const float kRadius = 0.5f;

            for (int i = 0; i < vertices; i++)
            {
                float angle = ((float)i * 2 * Mathf.PI / (float)vertices) + angleOffset;
                float x = kRadius * Mathf.Cos(angle);
                float y = kRadius * Mathf.Sin(angle);

                returnPath[i] = new Vector3(x, y);
            }

            return returnPath;
        }

        public static Vector3[] CreateCircle()
        {
            return CreateShape(32, 0);
        }

        public static Vector3[] CreateHexagonFlatTop()
        {
            return CreateShape(6, 0);
        }

        public static Vector3[] CreateHexagonPointedTop()
        {
            return CreateShape(6, 0.5f * Mathf.PI);
        }
    }
}
