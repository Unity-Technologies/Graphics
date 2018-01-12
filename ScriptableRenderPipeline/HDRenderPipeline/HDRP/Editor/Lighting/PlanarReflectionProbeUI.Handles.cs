using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class PlanarReflectionProbeUI
    {
        static readonly Color k_GizmoCamera = new Color(233f / 255f, 233f / 255f, 233f / 255f, 128f / 255f);

        public static void DrawHandles(PlanarReflectionProbeUI s, PlanarReflectionProbe d, Editor o)
        {
            var mat = d.transform.localToWorldMatrix;

            switch (EditMode.editMode)
            {
                case EditBaseShape:
                    InfluenceVolumeUI.DrawHandles_EditBase(s.influenceVolume, d.influenceVolume, o, mat, d);
                    break;
                case EditInfluenceShape:
                    InfluenceVolumeUI.DrawHandles_EditInfluence(s.influenceVolume, d.influenceVolume, o, mat, d);
                    break;
                case EditInfluenceNormalShape:
                    InfluenceVolumeUI.DrawHandles_EditInfluenceNormal(s.influenceVolume, d.influenceVolume, o, mat, d);
                    break;
                case EditCenter:
                    {
                        EditorGUI.BeginChangeCheck();
                        var m = Handles.matrix;
                        Handles.matrix = mat;
                        d.captureLocalPosition = Handles.PositionHandle(d.captureLocalPosition, d.transform.rotation);
                        if (EditorGUI.EndChangeCheck())
                            EditorUtility.SetDirty(d);
                        Handles.matrix = m;
                        break;
                    }
            }

            if (d.proxyVolumeReference != null)
                ProxyVolumeComponentUI.DrawHandles_EditNone(s.proxyVolume, d.proxyVolumeReference);
        }

        [DrawGizmo(GizmoType.Selected)]
        public static void DrawGizmos(PlanarReflectionProbe d, GizmoType gizmoType)
        {
            PlanarReflectionProbeUI s;
            if (!PlanarReflectionProbeEditor.TryGetUIStateFor(d, out s))
                return;

            var mat = d.transform.localToWorldMatrix;

            switch (EditMode.editMode)
            {
                case EditBaseShape:
                    InfluenceVolumeUI.DrawGizmos_EditBase(s.influenceVolume, d.influenceVolume, mat);
                    break;
                case EditInfluenceShape:
                    InfluenceVolumeUI.DrawGizmos_EditInfluence(s.influenceVolume, d.influenceVolume, mat);
                    break;
                case EditInfluenceNormalShape:
                    InfluenceVolumeUI.DrawGizmos_EditInfluenceNormal(s.influenceVolume, d.influenceVolume, mat);
                    break;
                default:
                    InfluenceVolumeUI.DrawGizmos_EditNone(s.influenceVolume, d.influenceVolume, mat);
                    break;
            }

            if (d.proxyVolumeReference != null)
                ProxyVolumeComponentUI.DrawGizmos_EditNone(s.proxyVolume, d.proxyVolumeReference);

            DrawGizmos_CaptureFrustrum(s, d);
        }

        static void DrawGizmos_CaptureFrustrum(PlanarReflectionProbeUI s, PlanarReflectionProbe d)
        {
            var farClipPlane = d.captureFarPlane;
            var nearClipPlane = d.captureNearPlane;
            var mat = Matrix4x4.TRS(d.capturePosition, d.captureRotation, Vector3.one);

            var near = new Vector3[4];
            var far = new Vector3[4];
            far[0] = new Vector3(0, 0, farClipPlane); // leftBottomFar
            far[1] = new Vector3(0, 1, farClipPlane); // leftTopFar
            far[2] = new Vector3(1, 1, farClipPlane); // rightTopFar
            far[3] = new Vector3(1, 0, farClipPlane); // rightBottomFar
            for (var i = 0; i < 4; ++i)
                far[i] = mat.MultiplyPoint(far[i]);
            near[0] = new Vector3(0, 0, nearClipPlane); // leftBottomNear
            near[1] = new Vector3(0, 1, nearClipPlane); // leftTopNear
            near[2] = new Vector3(1, 1, nearClipPlane); // rightTopNear
            near[3] = new Vector3(1, 0, nearClipPlane); // rightBottomNear
            for (var i = 0; i < 4; ++i)
                near[i] = mat.MultiplyPoint(near[i]);

            var c = Gizmos.color;
            Gizmos.color = k_GizmoCamera;
            for (var i = 0; i < 4; ++i)
            {
                Gizmos.DrawLine(near[i], near[(i + 1) % 4]);
                Gizmos.DrawLine(far[i], far[(i + 1) % 4]);
                Gizmos.DrawLine(near[i], far[i]);
            }
            Gizmos.color = c;
        }
    }
}
