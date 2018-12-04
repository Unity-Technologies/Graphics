namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal partial class CameraSettingsUI : IUpdateable<SerializedCameraSettings>
    {
        public FrameSettingsUI frameSettings = new FrameSettingsUI();

        public void Update(SerializedCameraSettings s)
        {
            frameSettings.Reset(s.frameSettings, null);
        }
    }
}
