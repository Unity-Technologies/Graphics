using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDProbeUI
    {

        internal static void DrawHandles(HDProbeUI s, HDProbe d, Editor o)
        {
            var mat = Matrix4x4.TRS(d.transform.position, d.transform.rotation, Vector3.one);

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
                //[TODO]
                //case EditCenter:
                //{
                //    EditorGUI.BeginChangeCheck();
                //    var m = Handles.matrix;
                //    Handles.matrix = mat;
                //    var p = Handles.PositionHandle(d.captureLocalPosition, d.transform.rotation);
                //    if (EditorGUI.EndChangeCheck())
                //    {
                //        Undo.RecordObject(d, "Translate Capture Position");
                //        d.captureLocalPosition = p;
                //        EditorUtility.SetDirty(d);
                //    }
                //    Handles.matrix = m;
                //    break;
                //}
            }
        }

        [DrawGizmo(GizmoType.Selected)]
        internal static void DrawGizmos(HDProbe d, GizmoType gizmoType)
        {
            HDProbeUI s;
            if (!HDProbeEditor.TryGetUIStateFor(d, out s))
                return;

            var mat = Matrix4x4.TRS(d.transform.position, d.transform.rotation, Vector3.one);

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

            if (d.proxyVolume != null)
                ReflectionProxyVolumeComponentUI.DrawGizmos_EditNone(d.proxyVolume);
        }
    }
}
