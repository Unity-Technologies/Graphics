using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(PhysicallyBasedSky))]
    public class PhysicallyBasedSkyEditor : SkySettingsEditor
    {
        SerializedDataParameter m_EarthPreset;
        SerializedDataParameter m_PlanetaryRadius;
        SerializedDataParameter m_PlanetCenterPosition;
        SerializedDataParameter m_PlanetRotation;
        SerializedDataParameter m_GroundColor;
        SerializedDataParameter m_GroundAlbedoTexture;
        SerializedDataParameter m_GroundEmissionTexture;

        SerializedDataParameter m_SpaceRotation;
        SerializedDataParameter m_SpaceEmissionTexture;

        SerializedDataParameter m_AirMaximumAltitude;
        SerializedDataParameter m_AirDensityR;
        SerializedDataParameter m_AirDensityG;
        SerializedDataParameter m_AirDensityB;
        SerializedDataParameter m_AirColor;

        SerializedDataParameter m_AerosolMaximumAltitude;
        SerializedDataParameter m_AerosolDensity;
        SerializedDataParameter m_AerosolColor;
        SerializedDataParameter m_AerosolAnisotropy;

        SerializedDataParameter m_NumberOfBounces;

        public override void OnEnable()
        {
            base.OnEnable();

            m_CommonUIElementsMask = (uint)SkySettingsUIElement.UpdateMode
                                   | (uint)SkySettingsUIElement.Exposure
                                   | (uint)SkySettingsUIElement.Multiplier
                                   | (uint)SkySettingsUIElement.IncludeSunInBaking;

            var o = new PropertyFetcher<PhysicallyBasedSky>(serializedObject);

			m_EarthPreset            = Unpack(o.Find(x => x.earthPreset));
			m_PlanetaryRadius        = Unpack(o.Find(x => x.planetaryRadius));
			m_PlanetCenterPosition   = Unpack(o.Find(x => x.planetCenterPosition));
			m_PlanetRotation         = Unpack(o.Find(x => x.planetRotation));
			m_GroundColor            = Unpack(o.Find(x => x.groundColor));
			m_GroundAlbedoTexture    = Unpack(o.Find(x => x.groundAlbedoTexture));
			m_GroundEmissionTexture  = Unpack(o.Find(x => x.groundEmissionTexture));

			m_SpaceRotation          = Unpack(o.Find(x => x.spaceRotation));
			m_SpaceEmissionTexture   = Unpack(o.Find(x => x.spaceEmissionTexture));

			m_AirMaximumAltitude     = Unpack(o.Find(x => x.airMaximumAltitude));
			m_AirDensityR            = Unpack(o.Find(x => x.airDensityR));
			m_AirDensityG            = Unpack(o.Find(x => x.airDensityG));
			m_AirDensityB            = Unpack(o.Find(x => x.airDensityB));
			m_AirColor               = Unpack(o.Find(x => x.airColor));

			m_AerosolMaximumAltitude = Unpack(o.Find(x => x.aerosolMaximumAltitude));
			m_AerosolDensity         = Unpack(o.Find(x => x.aerosolDensity));
			m_AerosolColor           = Unpack(o.Find(x => x.aerosolColor));
			m_AerosolAnisotropy      = Unpack(o.Find(x => x.aerosolAnisotropy));

			m_NumberOfBounces        = Unpack(o.Find(x => x.numberOfBounces));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Planet");
			PropertyField(m_EarthPreset);

            bool isEarth = !m_EarthPreset.overrideState.boolValue || m_EarthPreset.value.boolValue;
            if (!isEarth)
            {
			    PropertyField(m_PlanetaryRadius);
            }
			PropertyField(m_PlanetCenterPosition);
			PropertyField(m_PlanetRotation);
			PropertyField(m_GroundColor);
			PropertyField(m_GroundAlbedoTexture);
			PropertyField(m_GroundEmissionTexture);
            EditorGUILayout.LabelField("Space");
			PropertyField(m_SpaceRotation);
			PropertyField(m_SpaceEmissionTexture);
            if (!isEarth)
            {
                EditorGUILayout.LabelField("Air");
			    PropertyField(m_AirMaximumAltitude);
			    PropertyField(m_AirDensityR);
                PropertyField(m_AirDensityG);
                PropertyField(m_AirDensityB);
			    PropertyField(m_AirColor);
            }
            EditorGUILayout.LabelField("Aerosols");
			PropertyField(m_AerosolMaximumAltitude);
			PropertyField(m_AerosolDensity);
			PropertyField(m_AerosolColor);
			PropertyField(m_AerosolAnisotropy);
            EditorGUILayout.LabelField("Miscellaneous");
			PropertyField(m_NumberOfBounces);

            base.CommonSkySettingsGUI();
        }
    }
}
