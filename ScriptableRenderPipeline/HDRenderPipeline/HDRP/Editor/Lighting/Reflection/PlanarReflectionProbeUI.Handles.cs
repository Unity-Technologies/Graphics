using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class PlanarReflectionProbeUI
    {
        static readonly Color k_GizmoCamera = new Color(233f / 255f, 233f / 255f, 233f / 255f, 128f / 255f);
        static readonly Color k_GizmoMirrorPlaneCamera = new Color(128f / 255f, 128f / 255f, 233f / 255f, 128f / 255f);

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
                case EditMirrorPosition:
                    {
                        EditorGUI.BeginChangeCheck();
                        var m = Handles.matrix;
                        Handles.matrix = mat;
                        var p = Handles.PositionHandle(d.captureMirrorPlaneLocalPosition, d.transform.rotation);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(d, "Translate Mirror Plane");
                            d.captureMirrorPlaneLocalPosition = p;
                            EditorUtility.SetDirty(d);
                        }
                        Handles.matrix = m;
                        break;
                    }
                case EditMirrorRotation:
                    {
                        EditorGUI.BeginChangeCheck();
                        var m = Handles.matrix;
                        Handles.matrix = mat;
                        var q = Quaternion.LookRotation(d.captureMirrorPlaneLocalNormal, Vector3.up);
                        q = Handles.RotationHandle(q, d.captureMirrorPlaneLocalPosition);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(d, "Rotate Mirror Plane");
                            d.captureMirrorPlaneLocalNormal = q * Vector3.forward;
                            EditorUtility.SetDirty(d);
                        }
                        Handles.matrix = m;
                        break;
                    }
                case EditCenter:
                    {
                        EditorGUI.BeginChangeCheck();
                        var m = Handles.matrix;
                        Handles.matrix = mat;
                        var p = Handles.PositionHandle(d.captureLocalPosition, d.transform.rotation);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(d, "Translate Capture Position");
                            d.captureLocalPosition = p;
                            EditorUtility.SetDirty(d);
                        }
                        Handles.matrix = m;
                        break;
                    }
            }

            if (d.proxyVolumeReference != null)
                ReflectionProxyVolumeComponentUI.DrawHandles_EditNone(s.reflectionProxyVolume, d.proxyVolumeReference);
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
                    InfluenceVolumeUI.DrawGizmos(
                        s.influenceVolume, 
                        d.influenceVolume, 
                        mat, 
                        InfluenceVolumeUI.HandleType.Base, 
                        InfluenceVolumeUI.HandleType.All);
                    break;
                case EditInfluenceShape:
                    InfluenceVolumeUI.DrawGizmos(
                        s.influenceVolume, 
                        d.influenceVolume, 
                        mat,
                        InfluenceVolumeUI.HandleType.Influence, 
                        InfluenceVolumeUI.HandleType.All);
                    break;
                case EditInfluenceNormalShape:
                    InfluenceVolumeUI.DrawGizmos(
                        s.influenceVolume, 
                        d.influenceVolume,
                        mat,
                        InfluenceVolumeUI.HandleType.InfluenceNormal, 
                        InfluenceVolumeUI.HandleType.All);
                    break;
                default:
                    {
                        var showedHandles = s.influenceVolume.showInfluenceHandles
                            ? InfluenceVolumeUI.HandleType.All
                            : InfluenceVolumeUI.HandleType.Base;
                        InfluenceVolumeUI.DrawGizmos(
                            s.influenceVolume,
                            d.influenceVolume,
                            mat,
                            InfluenceVolumeUI.HandleType.None,
                            showedHandles);
                        break;
                    }
            }

            if (d.proxyVolumeReference != null)
                ReflectionProxyVolumeComponentUI.DrawGizmos_EditNone(s.reflectionProxyVolume, d.proxyVolumeReference);

            var showFrustrum = s.showCaptureHandles
                || EditMode.editMode == EditCenter;
            var showCaptureMirror = (s.showCaptureHandles && d.useMirrorPlane)
                || EditMode.editMode == EditMirrorPosition
                || EditMode.editMode == EditMirrorRotation;

            if (showFrustrum)
                DrawGizmos_CaptureFrustrum(s, d);

            if (showCaptureMirror)
                DrawGizmos_CaptureMirror(s, d);
        }

        static void DrawGizmos_CaptureMirror(PlanarReflectionProbeUI s, PlanarReflectionProbe d)
        {
            var c = Gizmos.color;
            var m = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                d.captureMirrorPlanePosition,
                Quaternion.LookRotation(d.captureMirrorPlaneNormal, Vector3.up),
                Vector3.one);
            Gizmos.color = k_GizmoMirrorPlaneCamera;

            Gizmos.DrawCube(Vector3.zero, new Vector3(1, 1, 0));

            Gizmos.matrix = m;
            Gizmos.color = c;
        }

        static void DrawGizmos_CaptureFrustrum(PlanarReflectionProbeUI s, PlanarReflectionProbe d)
        {
            var viewerCamera = Camera.current;
            var c = Gizmos.color;
            var m = Gizmos.matrix;

            float nearClipPlane, farClipPlane, aspect, fov;
            Color backgroundColor;
            CameraClearFlags clearFlags;
            Vector3 capturePosition;
            Quaternion captureRotation;
            Matrix4x4 worldToCameraRHS, projection;

            ReflectionSystem.CalculateCaptureCameraProperties(d,
                out nearClipPlane, out farClipPlane,
                out aspect, out fov, out clearFlags, out backgroundColor,
                out worldToCameraRHS, out projection,
                out capturePosition, out captureRotation, viewerCamera);

#if false
            // TODO: fix frustrum drawing

            var viewProj = projection * worldToCameraRHS;
            var invViewProj = viewProj.inverse;

            var near = new[]
            {
                new Vector3(-1, -1, -1),
                new Vector3(-1, 1, -1),
                new Vector3(1, 1, -1),
                new Vector3(1, -1, -1),
            };

            var far = new[]
            {
                new Vector3(-1, -1, 1),
                new Vector3(-1, 1, 1),
                new Vector3(1, 1, 1),
                new Vector3(1, -1, 1),
            };

            for (var i = 0; i < near.Length; ++i)
            {
                var p = invViewProj * new Vector4(near[i].x, near[i].y, near[i].z, 1);
                var w = Mathf.Abs(p.w);
                near[i].Set(p.x / w, p.y / w, p.z / w);
            }

            for (var i = 0; i < far.Length; ++i)
            {
                var p = invViewProj * new Vector4(far[i].x, far[i].y, far[i].z, 1);
                var w = Mathf.Abs(p.w);
                far[i].Set(p.x / w, p.y / w, p.z / w);
            }

            Gizmos.color = k_GizmoCamera;
            for (var i = 0; i < 4; ++i)
            {
                Gizmos.DrawLine(near[i], near[(i + 1) % 4]);
                Gizmos.DrawLine(far[i], far[(i + 1) % 4]);
                Gizmos.DrawLine(near[i], far[i]);
            }
            Gizmos.matrix = m;
#endif

            Gizmos.DrawSphere(capturePosition, HandleUtility.GetHandleSize(capturePosition) * 0.2f);
            Gizmos.color = c;
        }
    }
}
