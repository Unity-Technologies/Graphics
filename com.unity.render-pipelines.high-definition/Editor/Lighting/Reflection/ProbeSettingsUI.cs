using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{

#pragma warning disable 618 // TODO: update to IUpdateable interface
    sealed internal partial class ProbeSettingsUI : BaseUI<SerializedProbeSettings>
#pragma warning restore 618
    {
        public InfluenceVolumeUI influence = new InfluenceVolumeUI();
        public CameraSettingsUI camera = new CameraSettingsUI();

        public ProbeSettingsUI() : base(0)
        {
        }

        public override void Reset(SerializedProbeSettings probeSettings, UnityAction repaint)
        {
            base.Reset(probeSettings, repaint);
            camera.Update(probeSettings.cameraSettings);
        }

        public override void Update()
        {
            influence.Update(data.influence);
            camera.Update(data.cameraSettings);
            base.Update();
        }
    }
}
