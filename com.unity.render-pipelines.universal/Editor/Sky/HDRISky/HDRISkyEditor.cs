using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(HDRISky))]
    class HDRISkySettingsEditor : SkySettingsEditor
    {
        SerializedDataParameter m_hdriSky;
        // TODO More parameters
        SerializedDataParameter m_EnableBackplate;
        SerializedDataParameter m_BackplateType;
        SerializedDataParameter m_GroundLevel;
        SerializedDataParameter m_Scale;
        SerializedDataParameter m_ProjectionDistance;
        SerializedDataParameter m_PlateRotation;
        SerializedDataParameter m_PlateTexRotation;
        SerializedDataParameter m_PlateTexOffset;
        SerializedDataParameter m_BlendAmount;
        // TODO Shadow parameters

        // TODO Sky intensity calculation

        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            base.OnEnable();

            m_EnableLuxIntensityMode = true;

            // m_CommonUIElementsMask = (uint)SkySettingsUIElement.UpdateMode | (uint)SkySettingsUIElement.Rotation | (uint)SkySettingsUIElement.SkyIntensity; // TODO Reenable SkyIntensity
            m_CommonUIElementsMask = (uint)SkySettingsUIElement.UpdateMode | (uint)SkySettingsUIElement.Rotation | (uint)SkySettingsUIElement.SkyIntensity;

            var o = new PropertyFetcher<HDRISky>(serializedObject);
            m_hdriSky = Unpack(o.Find(x => x.hdriSky));
            // TODO More parameters
            m_EnableBackplate = Unpack(o.Find(x => x.enableBackplate));
            m_BackplateType = Unpack(o.Find(x => x.backplateType));
            m_GroundLevel = Unpack(o.Find(x => x.groundLevel));
            m_Scale = Unpack(o.Find(x => x.scale));
            m_ProjectionDistance = Unpack(o.Find(x => x.projectionDistance));
            m_PlateRotation = Unpack(o.Find(x => x.plateRotation));
            m_PlateTexRotation = Unpack(o.Find(x => x.plateTexRotation));
            m_PlateTexOffset = Unpack(o.Find(x => x.plateTexOffset));
            m_BlendAmount = Unpack(o.Find(x => x.blendAmount));
            // TODO Shadow parameters
        }

        public override void OnInspectorGUI()
        {
            // TODO Change check to trigger recalculation of upper hemisphere lux
            PropertyField(m_hdriSky);

            // TODO More parameters

            CommonSkySettingsGUI();

            if (isInAdvancedMode)
            {
                PropertyField(m_EnableBackplate, new GUIContent("Backplate", "Enable the projection of the bottom of the CubeMap on a plane with a given shape ('Disc', 'Rectangle', 'Ellipse', 'Infinite')"));

                EditorGUILayout.Space();

                if (m_EnableBackplate.value.boolValue)
                {
                    EditorGUI.indentLevel++;

                    PropertyField(m_BackplateType, new GUIContent("Type"));
                    bool constraintAsCircle = false;
                    if (m_BackplateType.value.enumValueIndex == (uint)BackplateType.Disc)
                    {
                        constraintAsCircle = true;
                    }

                    PropertyField(m_GroundLevel);

                    if (m_BackplateType.value.enumValueIndex != (uint)BackplateType.Infinite)
                    {
                        EditorGUI.BeginChangeCheck();
                        PropertyField(m_Scale);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (m_Scale.value.vector2Value.x < 0.0f || m_Scale.value.vector2Value.y < 0.0f)
                            {
                                m_Scale.value.vector2Value = new Vector2(Mathf.Abs(m_Scale.value.vector2Value.x), Mathf.Abs(m_Scale.value.vector2Value.y));
                            }
                        }

                        if (constraintAsCircle)
                        {
                            m_Scale.value.vector2Value = new Vector2(m_Scale.value.vector2Value.x, m_Scale.value.vector2Value.x);
                        }
                        else if (m_BackplateType.value.enumValueIndex == (uint)BackplateType.Ellipse &&
                                 Mathf.Abs(m_Scale.value.vector2Value.x - m_Scale.value.vector2Value.y) < 1e-4f)
                        {
                            m_Scale.value.vector2Value = new Vector2(m_Scale.value.vector2Value.x, m_Scale.value.vector2Value.x + 1e-4f);
                        }
                    }

                    PropertyField(m_ProjectionDistance, new GUIContent("Projection"));

                    PropertyField(m_PlateRotation, new GUIContent("Rotation"));
                    PropertyField(m_PlateTexRotation, new GUIContent("Texture Rotation"));
                    PropertyField(m_PlateTexOffset, new GUIContent("Texture Offset"));

                    if (m_BackplateType.value.enumValueIndex != (uint)BackplateType.Infinite)
                    {
                        PropertyField(m_BlendAmount);
                    }

                    // TODO Shadow parameters

                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
