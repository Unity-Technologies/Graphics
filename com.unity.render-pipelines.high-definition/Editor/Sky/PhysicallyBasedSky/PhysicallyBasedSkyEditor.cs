using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(PhysicallyBasedSky))]
    public class PhysicallyBasedSkyEditor : SkySettingsEditor
    {
        SerializedDataParameter m_PlanetaryRadius;
        SerializedDataParameter m_PlanetCenterPosition;
        SerializedDataParameter m_AirAttenuationDistance;
        SerializedDataParameter m_AirAlbedo;
        SerializedDataParameter m_AirMaximumAltitude;
        SerializedDataParameter m_AerosolAttenuationDistance;
        SerializedDataParameter m_AerosolAlbedo;
        SerializedDataParameter m_AerosolMaximumAltitude;
        SerializedDataParameter m_AerosolAnisotropy;
        SerializedDataParameter m_NumberOfBounces;
        SerializedDataParameter m_GroundColor;
        SerializedDataParameter m_GroundAlbedoTexture;
        SerializedDataParameter m_GroundEmissionTexture;
        SerializedDataParameter m_PlanetRotation;
        SerializedDataParameter m_SpaceEmissionTexture;
        SerializedDataParameter m_SpaceRotation;

        public override void OnEnable()
        {
            base.OnEnable();

            m_CommonUIElementsMask = (uint)SkySettingsUIElement.UpdateMode
                                   | (uint)SkySettingsUIElement.Exposure
                                   | (uint)SkySettingsUIElement.Multiplier
                                   | (uint)SkySettingsUIElement.IncludeSunInBaking;

            var o = new PropertyFetcher<PhysicallyBasedSky>(serializedObject);

			m_PlanetaryRadius            = Unpack(o.Find(x => x.planetaryRadius));
			m_PlanetCenterPosition       = Unpack(o.Find(x => x.planetCenterPosition));
			m_AirAttenuationDistance     = Unpack(o.Find(x => x.airOpacity));
			m_AirAlbedo                  = Unpack(o.Find(x => x.airAlbedo));
			m_AirMaximumAltitude         = Unpack(o.Find(x => x.airMaximumAltitude));
			m_AerosolAttenuationDistance = Unpack(o.Find(x => x.aerosolOpacity));
			m_AerosolAlbedo              = Unpack(o.Find(x => x.aerosolAlbedo));
			m_AerosolMaximumAltitude     = Unpack(o.Find(x => x.aerosolMaximumAltitude));
			m_AerosolAnisotropy          = Unpack(o.Find(x => x.aerosolAnisotropy));
			m_NumberOfBounces            = Unpack(o.Find(x => x.numberOfBounces));
			m_GroundColor                = Unpack(o.Find(x => x.groundColor));
			m_GroundAlbedoTexture        = Unpack(o.Find(x => x.groundAlbedoTexture));
			m_GroundEmissionTexture      = Unpack(o.Find(x => x.groundEmissionTexture));
			m_PlanetRotation             = Unpack(o.Find(x => x.planetRotation));
			m_SpaceEmissionTexture       = Unpack(o.Find(x => x.spaceEmissionTexture));
			m_SpaceRotation              = Unpack(o.Find(x => x.spaceRotation));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Geometry");
			PropertyField(m_PlanetaryRadius);
			PropertyField(m_PlanetCenterPosition);
            EditorGUILayout.LabelField("Air");
			PropertyField(m_AirAttenuationDistance);
			PropertyField(m_AirAlbedo);
			PropertyField(m_AirMaximumAltitude);
            EditorGUILayout.LabelField("Aerosols");
			PropertyField(m_AerosolAttenuationDistance);
			PropertyField(m_AerosolAlbedo);
			PropertyField(m_AerosolMaximumAltitude);
			PropertyField(m_AerosolAnisotropy);
            EditorGUILayout.LabelField("Planet");
			PropertyField(m_PlanetRotation);
			PropertyField(m_GroundColor);
			PropertyField(m_GroundAlbedoTexture);
			PropertyField(m_GroundEmissionTexture);
            EditorGUILayout.LabelField("Space");
			PropertyField(m_SpaceRotation);
			PropertyField(m_SpaceEmissionTexture);
            EditorGUILayout.LabelField("Miscellaneous");
			PropertyField(m_NumberOfBounces);

            base.CommonSkySettingsGUI();
        }
    }
}
