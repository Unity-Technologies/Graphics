using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class InfluenceVolumeUI
    {
        [Flags]
        public enum HandleType
        {
            None = 0,
            Base = 1,
            Influence = 1 << 1,
            InfluenceNormal = 1 << 2,

            All = ~0
        }

        public static void DrawGizmos(InfluenceVolumeUI s, InfluenceVolume d, Matrix4x4 matrix, HandleType editedHandle, HandleType showedHandle)
        {
            if ((showedHandle & HandleType.Base) != 0)
                DrawGizmos_BaseHandle(s, d, matrix, (editedHandle & HandleType.Base) != 0, k_GizmoThemeColorBase);

            if ((showedHandle & HandleType.Influence) != 0)
                DrawGizmos_FadeHandle(
                    s, d, matrix,
                    d.boxInfluenceOffset, d.boxInfluenceSizeOffset,
                    d.sphereInfluenceRadiusOffset,
                    (editedHandle & HandleType.Influence) != 0,
                    k_GizmoThemeColorInfluence);

            if ((showedHandle & HandleType.InfluenceNormal) != 0)
                DrawGizmos_FadeHandle(
                    s, d, matrix,
                    d.boxInfluenceNormalOffset, d.boxInfluenceNormalSizeOffset,
                    d.sphereInfluenceNormalRadiusOffset,
                    (editedHandle & HandleType.InfluenceNormal) != 0,
                    k_GizmoThemeColorInfluenceNormal);
        }

        static void DrawGizmos_BaseHandle(
            InfluenceVolumeUI s, InfluenceVolume d, Matrix4x4 matrix,
            bool isSolid, Color color)
        {
            var mat = Gizmos.matrix;
            var c = Gizmos.color;
            Gizmos.matrix = matrix;
            Gizmos.color = color;
            switch (d.shapeType)
            {
                case ShapeType.Box:
                {
                    if (isSolid)
                        Gizmos.DrawCube(d.boxBaseOffset, d.boxBaseSize);
                    else
                        Gizmos.DrawWireCube(d.boxBaseOffset, d.boxBaseSize);
                    break;
                }
                case ShapeType.Sphere:
                {
                    if (isSolid)
                        Gizmos.DrawSphere(d.sphereBaseOffset, d.sphereBaseRadius);
                    else
                        Gizmos.DrawWireSphere(d.sphereBaseOffset, d.sphereBaseRadius);
                    break;
                }
            }
            Gizmos.matrix = mat;
            Gizmos.color = c;
        }

        static void DrawGizmos_FadeHandle(
            InfluenceVolumeUI s, InfluenceVolume d, Matrix4x4 matrix,
            Vector3 boxOffset, Vector3 boxSizeOffset,
            float sphereOffset,
            bool isSolid, Color color)
        {
            var mat = Gizmos.matrix;
            var c = Gizmos.color;
            Gizmos.matrix = matrix;
            Gizmos.color = color;
            switch (d.shapeType)
            {
                case ShapeType.Box:
                {
                    if (isSolid)
                        Gizmos.DrawCube(d.boxBaseOffset + boxOffset, d.boxBaseSize + boxSizeOffset);
                    else
                        Gizmos.DrawWireCube(d.boxBaseOffset + boxOffset, d.boxBaseSize + boxSizeOffset);
                    break;
                }
                case ShapeType.Sphere:
                {
                    if (isSolid)
                        Gizmos.DrawSphere(d.sphereBaseOffset, d.sphereBaseRadius + sphereOffset);
                    else
                        Gizmos.DrawWireSphere(d.sphereBaseOffset, d.sphereBaseRadius + sphereOffset);
                    break;
                }
            }
            Gizmos.matrix = mat;
            Gizmos.color = c;
        }
    }
}
