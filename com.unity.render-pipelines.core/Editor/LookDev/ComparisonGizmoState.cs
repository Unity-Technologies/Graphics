using System;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityEditor.Rendering.LookDev
{
    [Serializable]
    public class ComparisonGizmoState
    {
        internal const float thickness = 0.0028f;
        internal const float thicknessSelected = 0.015f;
        internal const float circleRadius = 0.014f;
        internal const float circleRadiusSelected = 0.03f;
        internal const float blendFactorCircleRadius = 0.01f;
        internal const float blendFactorCircleRadiusSelected = 0.03f;

        /// <summary>Position of the first extremity</summary>
        public Vector2 point1 { get; private set; }
        /// <summary>Position of the second extremity</summary>
        public Vector2 point2 { get; private set; }
        /// <summary>Position of the center</summary>
        [field: SerializeField]
        public Vector2 center { get; private set; } = Vector2.zero;
        /// <summary>Angle from vertical in radian</summary>
        [field: SerializeField]
        public float angle { get; private set; }
        /// <summary>Length between extremity</summary>
        [field: SerializeField]
        public float length { get; private set; } = 0.2f;
        internal Vector4 plane { get; private set; }
        internal Vector4 planeOrtho { get; private set; }
        /// <summary>
        /// The position of the blending slider.
        /// From value -1 on first extremity to value 1 on second extremity.
        /// </summary>
        [field: SerializeField]
        public float blendFactor { get; set; }

        internal float blendFactorMaxGizmoDistance
            => length - circleRadius - blendFactorCircleRadius;

        internal float blendFactorMinGizmoDistance
            => length - circleRadius - blendFactorCircleRadiusSelected;

        internal void Init()
            => Update(center, length, angle);

        //TODO: optimize
        private Vector4 Get2DPlane(Vector2 firstPoint, float angle)
        {
            Vector4 result = new Vector4();
            angle = angle % (2.0f * (float)Math.PI);
            Vector2 secondPoint = new Vector2(firstPoint.x + Mathf.Sin(angle), firstPoint.y + Mathf.Cos(angle));
            Vector2 diff = secondPoint - firstPoint;
            if (Mathf.Abs(diff.x) < 1e-5)
            {
                result.Set(-1.0f, 0.0f, firstPoint.x, 0.0f);
                float sign = Mathf.Cos(angle) > 0.0f ? 1.0f : -1.0f;
                result *= sign;
            }
            else
            {
                float slope = diff.y / diff.x;
                result.Set(-slope, 1.0f, -(firstPoint.y - slope * firstPoint.x), 0.0f);
            }

            if (angle > Mathf.PI)
                result = -result;

            float length = Mathf.Sqrt(result.x * result.x + result.y * result.y);
            result = result / length;
            return result;
        }

        /// <summary>
        /// Update all fields while moving one extremity
        /// </summary>
        /// <param name="point1">The new first extremity position</param>
        /// <param name="point2">The new second extremity position</param>
        public void Update(Vector2 point1, Vector2 point2)
        {
            this.point1 = point1;
            this.point2 = point2;
            center = (point1 + point2) * 0.5f;
            length = (point2 - point1).magnitude * 0.5f;

            Vector3 verticalPlane = Get2DPlane(center, 0.0f);
            float side = Vector3.Dot(new Vector3(point1.x, point1.y, 1.0f), verticalPlane);
            angle = (Mathf.Deg2Rad * Vector2.Angle(new Vector2(0.0f, 1.0f), (point1 - point2).normalized));
            if (side > 0.0f)
                angle = 2.0f * Mathf.PI - angle;

            plane = Get2DPlane(center, angle);
            planeOrtho = Get2DPlane(center, angle + 0.5f * (float)Mathf.PI);
        }

        /// <summary>
        /// Update all fields while moving the bar
        /// </summary>
        /// <param name="center">The new center position</param>
        /// <param name="length">Tne new length of this gizmo</param>
        /// <param name="angle">The new angle of this gizmo</param>
        public void Update(Vector2 center, float length, float angle)
        {
            this.center = center;
            this.length = length;
            this.angle = angle;

            plane = Get2DPlane(center, angle);
            planeOrtho = Get2DPlane(center, angle + 0.5f * (float)Mathf.PI);

            Vector2 dir = new Vector2(planeOrtho.x, planeOrtho.y);
            point1 = center + dir * length;
            point2 = center - dir * length;
        }
    }
}
