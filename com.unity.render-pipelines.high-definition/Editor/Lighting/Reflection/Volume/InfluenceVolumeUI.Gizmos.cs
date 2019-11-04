using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
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

        public static void DrawGizmos(InfluenceVolume serialized, Matrix4x4 matrix, HandleType editedHandle, HandleType showedHandle)
        {
            var mat = Handles.matrix;
            Handles.matrix = matrix;

            if ((showedHandle & HandleType.Base) != 0)
            {
                switch (serialized.shape)
                {
                    case InfluenceShape.Box:
                        s_BoxBaseHandle.baseColor = k_GizmoThemeColorBase;
                        s_BoxBaseHandle.center = Vector3.zero;
                        s_BoxBaseHandle.size = serialized.boxSize;
                        s_BoxBaseHandle.DrawHull(false);
                        break;
                    case InfluenceShape.Sphere:
                        s_SphereBaseHandle.baseColor = k_GizmoThemeColorBase;
                        s_SphereBaseHandle.center = Vector3.zero;
                        s_SphereBaseHandle.radius = serialized.sphereRadius;
                        s_SphereBaseHandle.DrawHull(false);
                        break;
                }
            }

            if ((showedHandle & HandleType.Influence) != 0)
            {
                switch (serialized.shape)
                {
                    case InfluenceShape.Box:
                        s_BoxBaseHandle.baseColor = k_GizmoThemeColorInfluence;
                        s_BoxBaseHandle.center = serialized.boxBlendOffset;
                        s_BoxBaseHandle.size = serialized.boxSize + serialized.boxBlendSize;
                        s_BoxBaseHandle.DrawHull(false);
                        break;
                    case InfluenceShape.Sphere:
                        s_SphereBaseHandle.baseColor = k_GizmoThemeColorInfluence;
                        s_SphereBaseHandle.center = Vector3.zero;
                        s_SphereBaseHandle.radius = serialized.sphereRadius - serialized.sphereBlendDistance;
                        s_SphereBaseHandle.DrawHull(false);
                        break;
                }
            }

            if ((showedHandle & HandleType.InfluenceNormal) != 0)
            {
                switch (serialized.shape)
                {
                    case InfluenceShape.Box:
                        s_BoxBaseHandle.baseColor = k_GizmoThemeColorInfluenceNormal;
                        s_BoxBaseHandle.center = serialized.boxBlendNormalOffset;
                        s_BoxBaseHandle.size = serialized.boxSize + serialized.boxBlendNormalSize;
                        s_BoxBaseHandle.DrawHull(false);
                        break;
                    case InfluenceShape.Sphere:
                        s_SphereBaseHandle.baseColor = k_GizmoThemeColorInfluenceNormal;
                        s_SphereBaseHandle.center = Vector3.zero;
                        s_SphereBaseHandle.radius = serialized.sphereRadius - serialized.sphereBlendNormalDistance;
                        s_SphereBaseHandle.DrawHull(false);
                        break;
                }
            }

            Handles.matrix = mat;
        }
    }
}
