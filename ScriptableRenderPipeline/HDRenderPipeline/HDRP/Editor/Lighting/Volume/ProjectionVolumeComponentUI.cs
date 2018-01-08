using UnityEditor.Experimental.UIElements;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<ProjectionVolumeComponentUI, SerializedProjectionVolumeComponent>;

    class ProjectionVolumeComponentUI : BaseUI<SerializedProjectionVolumeComponent>
    {
        public static readonly CED.IDrawer Inspector;

        static ProjectionVolumeComponentUI()
        {
            Inspector = CED.Select(
                (s, d, o) => s.projectionVolume,
                (s, d, o) => d.projectionVolume,
                ProjectionVolumeUI.SectionShape
            );
        }

        public ProjectionVolumeUI projectionVolume = new ProjectionVolumeUI();

        public ProjectionVolumeComponentUI()
            : base(0)
        {
            
        }

        public override void Reset(SerializedProjectionVolumeComponent data, UnityAction repaint)
        {
            projectionVolume.Reset(data.projectionVolume, repaint);
            base.Reset(data, repaint);
        }

        public override void Update()
        {
            projectionVolume.Update();
            base.Update();
        }

        public static void DrawHandles(ProjectionVolumeComponent target, ProjectionVolumeComponentUI ui)
        {
            ProjectionVolumeUI.DrawHandles(target.transform, target.projectionVolume, ui.projectionVolume, target);
        }
    }
}
