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
                case EditCenter:
                    {
                        using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one)))
                        {
                            Vector3 offsetWorld = d.transform.position + d.transform.rotation * d.influenceVolume.offset;
                            EditorGUI.BeginChangeCheck();
                            var newOffsetWorld = Handles.PositionHandle(offsetWorld, d.transform.rotation);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Vector3 newOffset = Quaternion.Inverse(d.transform.rotation) * (newOffsetWorld - d.transform.position);
                                Undo.RecordObjects(new Object[] { d, d.transform }, "Translate Capture Position");
                                d.influenceVolume.offset = newOffset;
                                EditorUtility.SetDirty(d);
                            }
                        }
                        break;
                    }
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
