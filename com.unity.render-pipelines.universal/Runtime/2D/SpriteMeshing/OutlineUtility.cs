using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public class OutlineUtility : MonoBehaviour
    {
        static float k_ColinearError = 0.001f;


        public static bool GetConvexArea(float2 prevLinePt0, float2 prevLinePt1, float2 nextLinePt0, float2 nextLinePt1, out float2 newPt, out float area)
        {
            area = -1;
            bool hasIntersection = OutlineUtility.GetIntersection(prevLinePt0, prevLinePt1, nextLinePt1, nextLinePt0, out newPt);

            if (hasIntersection)
            {
                float height = -PointToTriBaseDistance(newPt, prevLinePt1, nextLinePt0);
                float width = math.distance(nextLinePt0, prevLinePt1);

                if (math.abs(height) <= k_ColinearError)
                    height = 0;

                area = 0.5f * height * width;
            }

            return area > 0;
        }

        public static bool GetConcaveArea(float2 pointToCheck, float2 prevPoint, float2 nextPoint, out float area)
        {
            float height = PointToTriBaseDistance(pointToCheck, prevPoint, nextPoint);
            float width = math.distance(nextPoint, prevPoint);

            if (math.abs(height) <= k_ColinearError)
                height = 0;

            area = 0.5f * height * width;

            return area >= 0;
        }

        public static float PointToTriBaseDistance(float2 pointToCheck, float2 prevPoint, float2 nextPoint)
        {
            float2 tangent = math.normalize(nextPoint - prevPoint);
            float2 normal = new float2(tangent.y, -tangent.x);

            float dist = math.dot(pointToCheck - prevPoint, normal);
            return dist;
        }

        public static bool IsValidIntersection(float2 pt0, float2 pt1, float2 intersectionPt)
        {
            if (intersectionPt.x < 0 || intersectionPt.x > 1 || intersectionPt.y < 0 || intersectionPt.y > 1)
                return false;

            float2 vector0 = pt1 - pt0;
            float2 vector1 = intersectionPt - pt0;

            float dot = math.dot(vector0, vector1);
            return dot >= 0.0f;
        }


        public static bool GetIntersection(float2 line0Pt0, float2 line0Pt1, float2 line1Pt0, float2 line1Pt1, out float2 intersectionPt)
        {
            intersectionPt = new float2();

            float A0 = line0Pt1.y - line0Pt0.y;
            float B0 = line0Pt0.x - line0Pt1.x;
            float C0 = A0 * line0Pt0.x + B0 * line0Pt0.y;

            float A1 = line1Pt1.y - line1Pt0.y;
            float B1 = line1Pt0.x - line1Pt1.x;
            float C1 = A1 * line1Pt0.x + B1 * line1Pt0.y;

            float determinant = (A0 * B1 - A1 * B0);

            if (determinant != 0)
            {
                float inverseDeterminant = 1.0f / determinant;
                intersectionPt.x = inverseDeterminant * (C0 * B1 - C1 * B0);
                intersectionPt.y = inverseDeterminant * (C1 * A0 - C0 * A1);
                return true;
            }

            return false;
        }

        public static void DrawCross(Vector2 point, float size, Color color)
        {
            Debug.DrawLine(point + Vector2.up * size, point + Vector2.down * size, color);
            Debug.DrawLine(point + Vector2.left * size, point + Vector2.right * size, color);
        }
    }
}
