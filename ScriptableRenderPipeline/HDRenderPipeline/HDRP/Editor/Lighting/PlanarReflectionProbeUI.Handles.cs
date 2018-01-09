using UnityEditorInternal;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class PlanarReflectionProbeUI
    {
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
                    InfluenceVolumeUI.DrawHandles_EditCenter(s.influenceVolume, d.influenceVolume, o, d.transform, d);
                    break;
            }
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
                case EditCenter:
                    InfluenceVolumeUI.DrawGizmos_EditCenter(s.influenceVolume, d.influenceVolume, mat);
                    break;
                default:
                    InfluenceVolumeUI.DrawGizmos_EditNone(s.influenceVolume, d.influenceVolume, mat);
                    break;
            }
        }
    }
}
