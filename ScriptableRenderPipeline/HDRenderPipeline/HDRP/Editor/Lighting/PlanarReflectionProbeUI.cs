using UnityEditor.AnimatedValues;
using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class PlanarReflectionProbeUI : BaseUI<SerializedPlanarReflectionProbe>
    {
        public InfluenceVolumeUI influenceVolume = new InfluenceVolumeUI();

        public AnimBool isSectionExpandedAdvancedSettings { get { return m_AnimBools[0]; } }

        public PlanarReflectionProbeUI()
            : base(1)
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
    }
}
