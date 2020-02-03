using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(HDRISky))]
    class HDRISkyEditor
        : SkySettingsEditor
    {
        SerializedDataParameter m_hdriSky;
        SerializedDataParameter m_UpperHemisphereLuxValue;
        SerializedDataParameter m_UpperHemisphereLuxColor;
        SerializedDataParameter m_EnableBackplate;
        SerializedDataParameter m_BackplateType;
        SerializedDataParameter m_GroundLevel;
        SerializedDataParameter m_Scale;
        SerializedDataParameter m_ProjectionDistance;
        SerializedDataParameter m_PlateRotation;
        SerializedDataParameter m_PlateTexRotation;
        SerializedDataParameter m_PlateTexOffset;
        SerializedDataParameter m_BlendAmount;
        SerializedDataParameter m_PointLightShadow;
        SerializedDataParameter m_DirLightShadow;
        SerializedDataParameter m_RectLightShadow;
        SerializedDataParameter m_ShadowTint;

        RTHandle m_IntensityTexture;
        Material m_IntegrateHDRISkyMaterial; // Compute the HDRI sky intensity in lux for the skybox
        Texture2D m_ReadBackTexture;
        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            base.OnEnable();

            m_EnableLuxIntensityMode = true;

            // HDRI sky does not have control over sun display.
            m_CommonUIElementsMask = 0xFFFFFFFF & ~(uint)(SkySettingsUIElement.IncludeSunInBaking);

            var o = new PropertyFetcher<HDRISky>(serializedObject);
            m_hdriSky                   = Unpack(o.Find(x => x.hdriSky));
            m_UpperHemisphereLuxValue   = Unpack(o.Find(x => x.upperHemisphereLuxValue));
            m_UpperHemisphereLuxColor   = Unpack(o.Find(x => x.upperHemisphereLuxColor));

            m_EnableBackplate           = Unpack(o.Find(x => x.enableBackplate));
            m_BackplateType             = Unpack(o.Find(x => x.backplateType));
            m_GroundLevel               = Unpack(o.Find(x => x.groundLevel));
            m_Scale                     = Unpack(o.Find(x => x.scale));
            m_ProjectionDistance        = Unpack(o.Find(x => x.projectionDistance));
            m_PlateRotation             = Unpack(o.Find(x => x.plateRotation));
            m_PlateTexRotation          = Unpack(o.Find(x => x.plateTexRotation));
            m_PlateTexOffset            = Unpack(o.Find(x => x.plateTexOffset));
            m_BlendAmount               = Unpack(o.Find(x => x.blendAmount));
            m_PointLightShadow          = Unpack(o.Find(x => x.pointLightShadow));
            m_DirLightShadow            = Unpack(o.Find(x => x.dirLightShadow));
            m_RectLightShadow           = Unpack(o.Find(x => x.rectLightShadow));
            m_ShadowTint                = Unpack(o.Find(x => x.shadowTint));

            m_IntensityTexture = RTHandles.Alloc(1, 1, colorFormat: GraphicsFormat.R32G32B32A32_SFloat);
            var hdrp = HDRenderPipeline.defaultAsset;
            if (hdrp != null)
                m_IntegrateHDRISkyMaterial = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.integrateHdriSkyPS);
            m_ReadBackTexture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, false);
        }

        public override void OnDisable()
        {
            if (m_IntensityTexture != null)
                RTHandles.Release(m_IntensityTexture);

            m_ReadBackTexture = null;
        }

        // Compute the lux value in the upper hemisphere of the HDRI skybox
        public void GetUpperHemisphereLuxValue()
        {
            Cubemap hdri = m_hdriSky.value.objectReferenceValue as Cubemap;

            // null material can happen when no HDRP asset is present.
            if (hdri == null || m_IntegrateHDRISkyMaterial == null)
                return;

            m_IntegrateHDRISkyMaterial.SetTexture(HDShaderIDs._Cubemap, hdri);

            Graphics.Blit(Texture2D.whiteTexture, m_IntensityTexture.rt, m_IntegrateHDRISkyMaterial);

            // Copy the rendertexture containing the lux value inside a Texture2D
            RenderTexture.active = m_IntensityTexture.rt;
            m_ReadBackTexture.ReadPixels(new Rect(0.0f, 0.0f, 1, 1), 0, 0);
            RenderTexture.active = null;

            // And then the value inside this texture
            Color hdriIntensity = m_ReadBackTexture.GetPixel(0, 0);
            m_UpperHemisphereLuxValue.value.floatValue = hdriIntensity.a;
            float max = Mathf.Max(hdriIntensity.r, hdriIntensity.g, hdriIntensity.b);
            if (max == 0.0f)
                max = 1.0f;
            m_UpperHemisphereLuxColor.value.vector3Value = new Vector3(hdriIntensity.r/max, hdriIntensity.g/max, hdriIntensity.b/max);
            m_UpperHemisphereLuxColor.value.vector3Value *= 0.5f; // Arbitrary 25% to not have too dark or too bright shadow
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            {
                PropertyField(m_hdriSky);
                base.CommonSkySettingsGUI();
            }
            bool updateDefaultShadowTint = false;
            if (EditorGUI.EndChangeCheck())
            {
                GetUpperHemisphereLuxValue();
                updateDefaultShadowTint = true;
            }

            if (isInAdvancedMode)
            {
                PropertyField(m_EnableBackplate, new GUIContent("Backplate", "Enable the projection of the bottom of the CubeMap on a plane with a given shape ('Disc', 'Rectangle', 'Ellispe', 'Infinite')"));
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
                                m_Scale.value.vector2Value = new Vector2(Mathf.Abs(m_Scale.value.vector2Value.x), Mathf.Abs(m_Scale.value.vector2Value.x));
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
                        PropertyField(m_BlendAmount);
                    PropertyField(m_PointLightShadow, new GUIContent("Point/Spot Shadow"));
                    PropertyField(m_DirLightShadow, new GUIContent("Directional Shadow"));
                    PropertyField(m_RectLightShadow, new GUIContent("Area Shadow"));
                    PropertyField(m_ShadowTint);
                    if (updateDefaultShadowTint || GUILayout.Button("Reset Color"))
                    {
                        m_ShadowTint.value.colorValue = new Color(m_UpperHemisphereLuxColor.value.vector3Value.x, m_UpperHemisphereLuxColor.value.vector3Value.y, m_UpperHemisphereLuxColor.value.vector3Value.z);
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
