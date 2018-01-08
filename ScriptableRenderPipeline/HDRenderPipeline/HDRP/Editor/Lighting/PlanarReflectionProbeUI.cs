using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<PlanarReflectionProbeUI, SerializedPlanarReflectionProbe>;

    class PlanarReflectionProbeUI : BaseUI<SerializedPlanarReflectionProbe>
    {
        public static readonly CED.IDrawer Inspector = CED.Group(
            CED.Action(Drawer_FieldProjectionVolumeReference),
            CED.Select(
                (s, d, o) => s.influenceVolume,
                (s, d, o) => d.influenceVolume,
                InfluenceVolumeUI.SectionShapeBox
            )
        );

        public InfluenceVolumeUI influenceVolume = new InfluenceVolumeUI();

        public PlanarReflectionProbeUI()
            : base(0)
        {
            
        }

        public override void Reset(SerializedPlanarReflectionProbe data, UnityAction repaint)
        {
            influenceVolume.Reset(data.influenceVolume, repaint);
            base.Reset(data, repaint);
        }

        public override void Update()
        {
            influenceVolume.Update();
            base.Update();
        }

        static void Drawer_FieldProjectionVolumeReference(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            EditorGUILayout.PropertyField(d.projectionVolumeReference, _.GetContent("Projection Volume Reference"));
        }
    }
}
