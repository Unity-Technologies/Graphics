using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(HDRISky))]
    class HDRISkySettingsEditor : SkySettingsEditor
    {
        SerializedDataParameter m_hdriSky;
        // TODO More parameters

        // TODO Advanced mode

        public override void OnEnable()
        {
            base.OnEnable();

            m_EnableLuxIntensityMode = true;

            // m_CommonUIElementsMask = (uint)SkySettingsUIElement.UpdateMode | (uint)SkySettingsUIElement.Rotation | (uint)SkySettingsUIElement.SkyIntensity; // TODO Reenable SkyIntensity
            m_CommonUIElementsMask = (uint)SkySettingsUIElement.UpdateMode | (uint)SkySettingsUIElement.Rotation | (uint)SkySettingsUIElement.SkyIntensity;

            var o = new PropertyFetcher<HDRISky>(serializedObject);
            m_hdriSky = Unpack(o.Find(x => x.hdriSky));
            // TODO More parameters
        }

        public override void OnInspectorGUI()
        {
            // TODO Change check to trigger recalculation of upper hemisphere lux
            PropertyField(m_hdriSky);

            // TODO More parameters

            CommonSkySettingsGUI();

            // TODO Advanced mode
        }
    }
}
