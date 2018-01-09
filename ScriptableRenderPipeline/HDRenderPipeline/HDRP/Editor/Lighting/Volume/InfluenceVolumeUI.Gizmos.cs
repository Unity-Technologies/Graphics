using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class InfluenceVolumeUI
    {
        public static void DrawGizmos_EditBase(InfluenceVolumeUI s, InfluenceVolume d, Matrix4x4 matrix)
        {
            DrawGizmos_Generic(s, d, matrix, 0);
        }

        public static void DrawGizmos_EditInfluence(InfluenceVolumeUI s, InfluenceVolume d, Matrix4x4 matrix)
        {
            DrawGizmos_Generic(s, d, matrix, 1);
        }

        public static void DrawGizmos_EditInfluenceNormal(InfluenceVolumeUI s, InfluenceVolume d, Matrix4x4 matrix)
        {
            DrawGizmos_Generic(s, d, matrix, 2);
            
        }

        public static void DrawGizmos_EditCenter(InfluenceVolumeUI s, InfluenceVolume d, Matrix4x4 matrix)
        {

        }

        public static void DrawGizmos_EditNone(InfluenceVolumeUI s, InfluenceVolume d, Matrix4x4 matrix)
        {
            DrawGizmos_Generic(s, d, matrix, -1);
        }

        static void DrawGizmos_Generic(InfluenceVolumeUI s, InfluenceVolume d, Matrix4x4 matrix, int solidIndex)
        {
            DrawGizmos_BaseHandle(s, d, matrix, solidIndex == 0, k_GizmoThemeColorBase);
            DrawGizmos_FadeHandle(
                s, d, matrix,
                d.boxInfluenceOffset, d.boxInfluenceSizeOffset,
                d.sphereInfluenceRadiusOffset,
                solidIndex == 1,
                k_GizmoThemeColorInfluence);
            DrawGizmos_FadeHandle(
                s, d, matrix,
                d.boxInfluenceNormalOffset, d.boxInfluenceNormalSizeOffset,
                d.sphereInfluenceNormalRadiusOffset,
                solidIndex == 2,
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
