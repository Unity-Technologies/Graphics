using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PhysicallyBasedSky))]
    class PhysicallyBasedSkyEditor : SkySettingsEditor
    {
        SerializedDataParameter m_Type;
        SerializedDataParameter m_AtmosphericScattering;
        SerializedDataParameter m_Mode;
        SerializedDataParameter m_Material;
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

        SerializedDataParameter m_OzoneDensity;
        SerializedDataParameter m_OzoneMinimumAltitude;
        SerializedDataParameter m_OzoneLayerWidth;

        SerializedDataParameter m_ColorSaturation;
        SerializedDataParameter m_AlphaSaturation;
        SerializedDataParameter m_AlphaMultiplier;
        SerializedDataParameter m_HorizonTint;
        SerializedDataParameter m_ZenithTint;
        SerializedDataParameter m_HorizonZenithShift;

        GUIContent[] m_ModelTypes = { new GUIContent("Earth (Simple)"), new GUIContent("Earth (Advanced)"), new GUIContent("Custom Planet") };
        int[] m_ModelTypeValues = { (int)PhysicallyBasedSkyModel.EarthSimple, (int)PhysicallyBasedSkyModel.EarthAdvanced, (int)PhysicallyBasedSkyModel.Custom };

        static public readonly GUIContent k_NewMaterialButtonText = EditorGUIUtility.TrTextContent("New", "Creates a new Physically Based Sky Material asset template.");
        static public readonly GUIContent k_CustomMaterial = EditorGUIUtility.TrTextContent("Material", "Sets a custom material that will be used to render the PBR Sky. If set to None, the default Rendering Mode is used.");

        static public readonly string k_NewSkyMaterialText = "Physically Based Sky";

        public override void OnEnable()
        {
            base.OnEnable();

            m_CommonUIElementsMask = (uint)SkySettingsUIElement.UpdateMode
                | (uint)SkySettingsUIElement.SkyIntensity
                | (uint)SkySettingsUIElement.IncludeSunInBaking;

            var o = new PropertyFetcher<PhysicallyBasedSky>(serializedObject);

            m_Type = Unpack(o.Find(x => x.type));
            m_AtmosphericScattering = Unpack(o.Find(x => x.atmosphericScattering));
            m_Mode = Unpack(o.Find(x => x.renderingMode));
            m_Material = Unpack(o.Find(x => x.material));
            m_PlanetRotation = Unpack(o.Find(x => x.planetRotation));
            m_GroundColorTexture = Unpack(o.Find(x => x.groundColorTexture));
            m_GroundTint = Unpack(o.Find(x => x.groundTint));
            m_GroundEmissionTexture = Unpack(o.Find(x => x.groundEmissionTexture));
            m_GroundEmissionMultiplier = Unpack(o.Find(x => x.groundEmissionMultiplier));

            m_SpaceRotation = Unpack(o.Find(x => x.spaceRotation));
            m_SpaceEmissionTexture = Unpack(o.Find(x => x.spaceEmissionTexture));
            m_SpaceEmissionMultiplier = Unpack(o.Find(x => x.spaceEmissionMultiplier));

            m_AirMaximumAltitude = Unpack(o.Find(x => x.airMaximumAltitude));
            m_AirDensityR = Unpack(o.Find(x => x.airDensityR));
            m_AirDensityG = Unpack(o.Find(x => x.airDensityG));
            m_AirDensityB = Unpack(o.Find(x => x.airDensityB));
            m_AirTint = Unpack(o.Find(x => x.airTint));

            m_AerosolMaximumAltitude = Unpack(o.Find(x => x.aerosolMaximumAltitude));
            m_AerosolDensity = Unpack(o.Find(x => x.aerosolDensity));
            m_AerosolTint = Unpack(o.Find(x => x.aerosolTint));
            m_AerosolAnisotropy = Unpack(o.Find(x => x.aerosolAnisotropy));

            m_OzoneDensity = Unpack(o.Find(x => x.ozoneDensityDimmer));
            m_OzoneMinimumAltitude = Unpack(o.Find(x => x.ozoneMinimumAltitude));
            m_OzoneLayerWidth = Unpack(o.Find(x => x.ozoneLayerWidth));

            m_ColorSaturation = Unpack(o.Find(x => x.colorSaturation));
            m_AlphaSaturation = Unpack(o.Find(x => x.alphaSaturation));
            m_AlphaMultiplier = Unpack(o.Find(x => x.alphaMultiplier));
            m_HorizonTint = Unpack(o.Find(x => x.horizonTint));
            m_ZenithTint = Unpack(o.Find(x => x.zenithTint));
            m_HorizonZenithShift = Unpack(o.Find(x => x.horizonZenithShift));
        }

        void ModelTypeField(SerializedDataParameter property)
        {
            var title = EditorGUIUtility.TrTextContent(property.displayName,
                property.GetAttribute<TooltipAttribute>()?.tooltip);

            using (var scope = new OverridablePropertyScope(property, title, this))
            {
                if (!scope.displayed)
                    return;

                var rect = EditorGUILayout.GetControlRect();
                EditorGUI.BeginProperty(rect, title, property.value);

                EditorGUI.BeginChangeCheck();
                var value = EditorGUI.IntPopup(rect, title, property.value.intValue, m_ModelTypes, m_ModelTypeValues);
                if (EditorGUI.EndChangeCheck())
                    property.value.intValue = value;

                EditorGUI.EndProperty();
            }
        }

        public override void OnInspectorGUI()
        {
            DrawHeader("Model");

            ModelTypeField(m_Type);
            PropertyField(m_AtmosphericScattering);

            DrawHeader("Planet and Space");

            PropertyField(m_Mode);
            bool hasMaterial = m_Mode.value.intValue == 1;
            if (hasMaterial)
            {
                using (new IndentLevelScope())
                {
                    MaterialFieldWithButton(m_Material, k_CustomMaterial);
                }
                    
            }

            DrawHeader("Planet");

            PhysicallyBasedSkyModel type = (PhysicallyBasedSkyModel)m_Type.value.intValue;
            if (type != PhysicallyBasedSkyModel.EarthSimple && !hasMaterial)
            {
                PropertyField(m_PlanetRotation);
                PropertyField(m_GroundColorTexture);
            }

            ColorFieldLinear(m_GroundTint);

            if (type != PhysicallyBasedSkyModel.EarthSimple && !hasMaterial)
            {
                PropertyField(m_GroundEmissionTexture);
                PropertyField(m_GroundEmissionMultiplier);
            }

            if (type != PhysicallyBasedSkyModel.EarthSimple && !hasMaterial)
            {
                DrawHeader("Space");
                PropertyField(m_SpaceRotation);
                PropertyField(m_SpaceEmissionTexture);
                PropertyField(m_SpaceEmissionMultiplier);
            }

            if (type == PhysicallyBasedSkyModel.Custom)
            {
                DrawHeader("Air");
                PropertyField(m_AirMaximumAltitude);
                PropertyField(m_AirDensityR);
                PropertyField(m_AirDensityG);
                PropertyField(m_AirDensityB);
                PropertyField(m_AirTint);
            }

            DrawHeader("Aerosols");
            PropertyField(m_AerosolDensity);
            PropertyField(m_AerosolTint);
            PropertyField(m_AerosolAnisotropy);
            if (type != PhysicallyBasedSkyModel.EarthSimple)
                PropertyField(m_AerosolMaximumAltitude);

            if (type != PhysicallyBasedSkyModel.EarthSimple)
            {
                DrawHeader("Ozone");
                PropertyField(m_OzoneDensity);
                if (type == PhysicallyBasedSkyModel.Custom)
                {
                    PropertyField(m_OzoneMinimumAltitude);
                    PropertyField(m_OzoneLayerWidth);
                }
            }

            EditorGUILayout.Space();
            DrawHeader("Artistic Overrides");
            PropertyField(m_ColorSaturation);
            PropertyField(m_AlphaSaturation);
            PropertyField(m_AlphaMultiplier);
            PropertyField(m_HorizonTint);
            PropertyField(m_HorizonZenithShift);
            PropertyField(m_ZenithTint);

            EditorGUILayout.Space();
            DrawHeader("Miscellaneous");

            base.CommonSkySettingsGUI();
        }

        internal void MaterialFieldWithButton(SerializedDataParameter prop, GUIContent label)
        {
            using (var scope = new OverridablePropertyScope(prop, prop.displayName, this))
            {
                if (!scope.displayed)
                    return;

                const int k_NewFieldWidth = 70;
                var rect = EditorGUILayout.GetControlRect();
                rect.xMax -= k_NewFieldWidth + 2;

                var newFieldRect = rect;
                newFieldRect.x = rect.xMax + 2;
                newFieldRect.width = k_NewFieldWidth;

                EditorGUI.PropertyField(rect, prop.value, label);

                if (GUI.Button(newFieldRect, k_NewMaterialButtonText))
                {
                    string materialName = "New " + k_NewSkyMaterialText + ".mat";
                    var materialIcon = AssetPreview.GetMiniTypeThumbnail(typeof(Material));
                    var action = ScriptableObject.CreateInstance<DoCreatePBRSkyDefaultMaterial>();
                    action.physicallyBasedSky = target as PhysicallyBasedSky;
                    ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, action, materialName, materialIcon, null);
                }
            }
        }
    }

    class DoCreatePBRSkyDefaultMaterial : ProjectWindowCallback.EndNameEditAction
    {
        public PhysicallyBasedSky physicallyBasedSky;
        public Material material = null;
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var shader = GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineRuntimeMaterials>().pbrSkyMaterial;
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, pathName);
            ProjectWindowUtil.ShowCreatedAsset(material);
            physicallyBasedSky.material.value = material;
        }
    }
}
