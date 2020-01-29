using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(PhysicallyBasedSky))]
    class PhysicallyBasedSkyEditor : SkySettingsEditor
    {
        SerializedDataParameter m_EarthPreset;
        SerializedDataParameter m_SphericalMode;
        SerializedDataParameter m_SeaLevel;
        SerializedDataParameter m_PlanetaryRadius;
        SerializedDataParameter m_PlanetCenterPosition;
        SerializedDataParameter m_PlanetRotation;
        SerializedDataParameter m_GroundColorTexture;
        SerializedDataParameter m_GroundTint;
        SerializedDataParameter m_GroundEmissionTexture;
        SerializedDataParameter m_GroundEmissionMultiplier;

        SerializedDataParameter m_SpaceRotation;
        SerializedDataParameter m_SpaceEmissionTexture;
        SerializedDataParameter m_SpaceEmissionMultiplier;

        SerializedDataParameter m_AirMaximumAltitude;
        SerializedDataParameter m_AirDensityR;
        SerializedDataParameter m_AirDensityG;
        SerializedDataParameter m_AirDensityB;
        SerializedDataParameter m_AirTint;

        SerializedDataParameter m_AerosolMaximumAltitude;
        SerializedDataParameter m_AerosolDensity;
        SerializedDataParameter m_AerosolTint;
        SerializedDataParameter m_AerosolAnisotropy;

        SerializedDataParameter m_ColorSaturation;
        SerializedDataParameter m_AlphaSaturation;
        SerializedDataParameter m_AlphaMultiplier;
        SerializedDataParameter m_HorizonTint;
        SerializedDataParameter m_ZenithTint;
        SerializedDataParameter m_HorizonZenithShift;

        SerializedDataParameter m_NumberOfBounces;

        public override void OnEnable()
        {
            base.OnEnable();

            m_CommonUIElementsMask = (uint)SkySettingsUIElement.UpdateMode
                                   | (uint)SkySettingsUIElement.SkyIntensity
                                   | (uint)SkySettingsUIElement.IncludeSunInBaking;

            var o = new PropertyFetcher<PhysicallyBasedSky>(serializedObject);

			m_EarthPreset              = Unpack(o.Find(x => x.earthPreset));
			m_SphericalMode            = Unpack(o.Find(x => x.sphericalMode));
			m_SeaLevel                 = Unpack(o.Find(x => x.seaLevel));
			m_PlanetaryRadius          = Unpack(o.Find(x => x.planetaryRadius));
			m_PlanetCenterPosition     = Unpack(o.Find(x => x.planetCenterPosition));
			m_PlanetRotation           = Unpack(o.Find(x => x.planetRotation));
			m_GroundColorTexture       = Unpack(o.Find(x => x.groundColorTexture));
			m_GroundTint               = Unpack(o.Find(x => x.groundTint));
			m_GroundEmissionTexture    = Unpack(o.Find(x => x.groundEmissionTexture));
			m_GroundEmissionMultiplier = Unpack(o.Find(x => x.groundEmissionMultiplier));

			m_SpaceRotation            = Unpack(o.Find(x => x.spaceRotation));
			m_SpaceEmissionTexture     = Unpack(o.Find(x => x.spaceEmissionTexture));
			m_SpaceEmissionMultiplier  = Unpack(o.Find(x => x.spaceEmissionMultiplier));

			m_AirMaximumAltitude       = Unpack(o.Find(x => x.airMaximumAltitude));
			m_AirDensityR              = Unpack(o.Find(x => x.airDensityR));
			m_AirDensityG              = Unpack(o.Find(x => x.airDensityG));
			m_AirDensityB              = Unpack(o.Find(x => x.airDensityB));
			m_AirTint                  = Unpack(o.Find(x => x.airTint));

			m_AerosolMaximumAltitude   = Unpack(o.Find(x => x.aerosolMaximumAltitude));
			m_AerosolDensity           = Unpack(o.Find(x => x.aerosolDensity));
			m_AerosolTint              = Unpack(o.Find(x => x.aerosolTint));
			m_AerosolAnisotropy        = Unpack(o.Find(x => x.aerosolAnisotropy));

			m_ColorSaturation          = Unpack(o.Find(x => x.colorSaturation));
			m_AlphaSaturation          = Unpack(o.Find(x => x.alphaSaturation));
			m_AlphaMultiplier          = Unpack(o.Find(x => x.alphaMultiplier));
			m_HorizonTint              = Unpack(o.Find(x => x.horizonTint));
			m_ZenithTint               = Unpack(o.Find(x => x.zenithTint));
			m_HorizonZenithShift       = Unpack(o.Find(x => x.horizonZenithShift));

			m_NumberOfBounces          = Unpack(o.Find(x => x.numberOfBounces));
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

			PropertyField(m_SphericalMode);
            bool isSpherical = !m_SphericalMode.overrideState.boolValue || m_SphericalMode.value.boolValue;
            if (isSpherical)
            {
			    PropertyField(m_PlanetCenterPosition);
            }
            else
            {
			    PropertyField(m_SeaLevel);
            }

			PropertyField(m_PlanetRotation);
			PropertyField(m_GroundColorTexture);
			PropertyField(m_GroundTint);
			PropertyField(m_GroundEmissionTexture);
			PropertyField(m_GroundEmissionMultiplier);

            EditorGUILayout.LabelField("Space");
			PropertyField(m_SpaceRotation);
			PropertyField(m_SpaceEmissionTexture);
			PropertyField(m_SpaceEmissionMultiplier);
            if (!isEarth)
            {
                EditorGUILayout.LabelField("Air");
			    PropertyField(m_AirMaximumAltitude);
			    PropertyField(m_AirDensityR);
                PropertyField(m_AirDensityG);
                PropertyField(m_AirDensityB);
			    PropertyField(m_AirTint);
            }
            EditorGUILayout.LabelField("Aerosols");
			PropertyField(m_AerosolMaximumAltitude);
			PropertyField(m_AerosolDensity);
			PropertyField(m_AerosolTint);
			PropertyField(m_AerosolAnisotropy);

            EditorGUILayout.LabelField("Artistic Overrides");
			PropertyField(m_ColorSaturation);
			PropertyField(m_AlphaSaturation);
			PropertyField(m_AlphaMultiplier);
			PropertyField(m_HorizonTint);
			PropertyField(m_HorizonZenithShift);
			PropertyField(m_ZenithTint);

            EditorGUILayout.LabelField("Miscellaneous");
			PropertyField(m_NumberOfBounces);

            base.CommonSkySettingsGUI();
        }
    }
}
