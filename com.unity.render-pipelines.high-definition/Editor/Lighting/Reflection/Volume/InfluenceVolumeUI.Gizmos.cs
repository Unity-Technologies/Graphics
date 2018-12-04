using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class InfluenceVolumeUI
    {
        static HierarchicalSphere s_SphereHandle = new HierarchicalSphere(k_GizmoThemeColorBase);
        static HierarchicalBox s_BoxHandle = new HierarchicalBox(k_GizmoThemeColorBase);

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
            var mat = Handles.matrix;
            Handles.matrix = matrix;

            if ((showedHandle & HandleType.Base) != 0)
            {
                switch (d.shape)
                {
                    case InfluenceShape.Box:
                        s_BoxHandle.baseColor = k_GizmoThemeColorBase;
                        s_BoxHandle.center = Vector3.zero;
                        s_BoxHandle.size = d.boxSize;
                        s_BoxHandle.DrawHull(false);
                        break;
                    case InfluenceShape.Sphere:
                        s_SphereHandle.baseColor = k_GizmoThemeColorBase;
                        s_SphereHandle.center = Vector3.zero;
                        s_SphereHandle.radius = d.sphereRadius;
                        s_SphereHandle.DrawHull(false);
                        break;
                }
            }

            if ((showedHandle & HandleType.Influence) != 0)
            {
                switch (d.shape)
                {
                    case InfluenceShape.Box:
                        s_BoxHandle.baseColor = k_GizmoThemeColorInfluence;
                        s_BoxHandle.center = d.boxBlendOffset;
                        s_BoxHandle.size = d.boxSize + d.boxBlendSize;
                        s_BoxHandle.DrawHull(false);
                        break;
                    case InfluenceShape.Sphere:
                        s_SphereHandle.baseColor = k_GizmoThemeColorInfluence;
                        s_SphereHandle.center = Vector3.zero;
                        s_SphereHandle.radius = d.sphereRadius - d.sphereBlendDistance;
                        s_SphereHandle.DrawHull(false);
                        break;
                }
            }

            if ((showedHandle & HandleType.InfluenceNormal) != 0)
            {
                switch (d.shape)
                {
                    case InfluenceShape.Box:
                        s_BoxHandle.baseColor = k_GizmoThemeColorInfluenceNormal;
                        s_BoxHandle.center = d.boxBlendNormalOffset;
                        s_BoxHandle.size = d.boxSize + d.boxBlendNormalSize;
                        s_BoxHandle.DrawHull(false);
                        break;
                    case InfluenceShape.Sphere:
                        s_SphereHandle.baseColor = k_GizmoThemeColorInfluenceNormal;
                        s_SphereHandle.center = Vector3.zero;
                        s_SphereHandle.radius = d.sphereRadius - d.sphereBlendNormalDistance;
                        s_SphereHandle.DrawHull(false);
                        break;
                }
            }

            Handles.matrix = mat;
        }
    }
}
