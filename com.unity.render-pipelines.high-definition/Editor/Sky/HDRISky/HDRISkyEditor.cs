using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(HDRISky))]
    class HDRISkyEditor : SkySettingsEditor
    {
        class Styles
        {
            public static GUIContent scrollOrientationLabel { get; } = new GUIContent("Orientation", "Controls the orientation of the distortion relative to the X world vector (in degrees).\nThis value can be relative to the Global Wind Orientation defined in the Visual Environment.");
            public static GUIContent scrollSpeedLabel { get; } = new GUIContent("Speed", "Sets the scrolling speed of the distortion. The higher the value, the faster the sky will move.\nThis value can be relative to the Global Wind Speed defined in the Visual Environment.");

            public static GUIContent backplate { get; } = EditorGUIUtility.TrTextContent("Backplate", "Enable the projection of the bottom of the CubeMap on a plane with a given shape ('Disc', 'Rectangle', 'Ellispe', 'Infinite')");
            public static GUIContent type { get; } = EditorGUIUtility.TrTextContent("Type");
            public static GUIContent projection { get; } = EditorGUIUtility.TrTextContent("Projection");
            public static GUIContent rotation { get; } = EditorGUIUtility.TrTextContent("Rotation");
            public static GUIContent textureRotation { get; } = EditorGUIUtility.TrTextContent("Texture Rotation");
            public static GUIContent textureOffset { get; } = EditorGUIUtility.TrTextContent("Texture Offset");
            public static GUIContent pointSpotShadow { get; } = EditorGUIUtility.TrTextContent("Point/Spot Shadow");
            public static GUIContent directionalShadow { get; } = EditorGUIUtility.TrTextContent("Directional Shadow");
            public static GUIContent areaShadow { get; } = EditorGUIUtility.TrTextContent("Area Shadow");
            public static GUIContent resetColors { get; } = EditorGUIUtility.TrTextContent("Reset Color");
            public static GUIContent procedural { get; } = EditorGUIUtility.TrTextContent("Procedural");
            public static GUIContent flowmap { get; } = EditorGUIUtility.TrTextContent("Flowmap");
            public static string flowmapInfoMessage { get; } = "The flowmap needs to be a 2D Texture in LatLong layout.";
        }

        SerializedDataParameter m_hdriSky;
        SerializedDataParameter m_UpperHemisphereLuxValue;
        SerializedDataParameter m_UpperHemisphereLuxColor;

        SerializedDataParameter m_DistortionMode;
        SerializedDataParameter m_Flowmap;
        SerializedDataParameter m_UpperHemisphereOnly;
        SerializedDataParameter m_ScrollOrientation;
        SerializedDataParameter m_ScrollSpeed;

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

        GUIContent[] m_DistortionModes = { Styles.procedural, Styles.flowmap };
        int[] m_DistortionModeValues = { 1, 0 };

        RTHandle m_IntensityTexture;
        Material m_IntegrateHDRISkyMaterial; // Compute the HDRI sky intensity in lux for the skybox
        Texture2D m_ReadBackTexture;

        public override void OnEnable()
        {
            base.OnEnable();

            m_EnableLuxIntensityMode = true;

            // HDRI sky does not have control over sun display.
            m_CommonUIElementsMask = 0xFFFFFFFF & ~(uint)(SkySettingsUIElement.IncludeSunInBaking);

            var o = new PropertyFetcher<HDRISky>(serializedObject);
            m_hdriSky = Unpack(o.Find(x => x.hdriSky));
            m_UpperHemisphereLuxValue = Unpack(o.Find(x => x.upperHemisphereLuxValue));
            m_UpperHemisphereLuxColor = Unpack(o.Find(x => x.upperHemisphereLuxColor));

            m_DistortionMode = Unpack(o.Find(x => x.distortionMode));
            m_Flowmap = Unpack(o.Find(x => x.flowmap));
            m_UpperHemisphereOnly = Unpack(o.Find(x => x.upperHemisphereOnly));
            m_ScrollOrientation = Unpack(o.Find(x => x.scrollOrientation));
            m_ScrollSpeed = Unpack(o.Find(x => x.scrollSpeed));

            m_EnableBackplate = Unpack(o.Find(x => x.enableBackplate));
            m_BackplateType = Unpack(o.Find(x => x.backplateType));
            m_GroundLevel = Unpack(o.Find(x => x.groundLevel));
            m_Scale = Unpack(o.Find(x => x.scale));
            m_ProjectionDistance = Unpack(o.Find(x => x.projectionDistance));
            m_PlateRotation = Unpack(o.Find(x => x.plateRotation));
            m_PlateTexRotation = Unpack(o.Find(x => x.plateTexRotation));
            m_PlateTexOffset = Unpack(o.Find(x => x.plateTexOffset));
            m_BlendAmount = Unpack(o.Find(x => x.blendAmount));
            m_PointLightShadow = Unpack(o.Find(x => x.pointLightShadow));
            m_DirLightShadow = Unpack(o.Find(x => x.dirLightShadow));
            m_RectLightShadow = Unpack(o.Find(x => x.rectLightShadow));
            m_ShadowTint = Unpack(o.Find(x => x.shadowTint));

            m_IntensityTexture = RTHandles.Alloc(1, 1, colorFormat: GraphicsFormat.R32G32B32A32_SFloat);
            if (HDRenderPipelineGlobalSettings.instance?.renderPipelineResources != null)
            {
                m_IntegrateHDRISkyMaterial = CoreUtils.CreateEngineMaterial(HDRenderPipelineGlobalSettings.instance.renderPipelineResources.shaders.integrateHdriSkyPS);
            }
            m_ReadBackTexture = new Texture2D(1, 1, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None);
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
            // null material can happen when no HDRP asset was present at startup
            if (m_IntegrateHDRISkyMaterial == null)
            {
                if (HDRenderPipeline.isReady)
                    m_IntegrateHDRISkyMaterial = CoreUtils.CreateEngineMaterial(HDRenderPipelineGlobalSettings.instance.renderPipelineResources.shaders.integrateHdriSkyPS);
                else
                    return;
            }

            Cubemap hdri = m_hdriSky.value.objectReferenceValue as Cubemap;
            if (hdri == null)
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
            m_UpperHemisphereLuxColor.value.vector3Value = new Vector3(hdriIntensity.r / max, hdriIntensity.g / max, hdriIntensity.b / max);
            m_UpperHemisphereLuxColor.value.vector3Value *= 0.5f; // Arbitrary 25% to not have too dark or too bright shadow
        }

        bool IsFlowmapFormatInvalid(SerializedDataParameter map)
        {
            if (!map.overrideState.boolValue || map.value.objectReferenceValue == null)
                return false;
            var tex = map.value.objectReferenceValue;
            if (!tex.GetType().IsSubclassOf(typeof(Texture)))
                return true;
            return (tex as Texture).dimension != TextureDimension.Tex2D;
        }

        public override void OnInspectorGUI()
        {
            bool updateDefaultShadowTint = false;

            EditorGUI.BeginChangeCheck();
            PropertyField(m_hdriSky);
            if (EditorGUI.EndChangeCheck())
            {
                GetUpperHemisphereLuxValue();
                updateDefaultShadowTint = true;
            }

            PropertyField(m_DistortionMode);
            if (m_DistortionMode.value.intValue != (int)HDRISky.DistortionMode.None)
            {
                using (new IndentLevelScope())
                {
                    PropertyField(m_ScrollOrientation, Styles.scrollOrientationLabel);
                    PropertyField(m_ScrollSpeed, Styles.scrollSpeedLabel);
                    if (m_DistortionMode.value.intValue == (int)HDRISky.DistortionMode.Flowmap)
                    {
                        PropertyField(m_Flowmap);
                        if (IsFlowmapFormatInvalid(m_Flowmap))
                            EditorGUILayout.HelpBox(Styles.flowmapInfoMessage, MessageType.Info);
                        PropertyField(m_UpperHemisphereOnly);
                    }
                }
            }

            base.CommonSkySettingsGUI();

            PropertyField(m_EnableBackplate, Styles.backplate);

            if (m_EnableBackplate.value.boolValue)
            {
                using (new IndentLevelScope())
                {
                    PropertyField(m_BackplateType, Styles.type);
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
                    PropertyField(m_ProjectionDistance, Styles.projection);
                    PropertyField(m_PlateRotation, Styles.rotation);
                    PropertyField(m_PlateTexRotation, Styles.textureRotation);
                    PropertyField(m_PlateTexOffset, Styles.textureOffset);
                    if (m_BackplateType.value.enumValueIndex != (uint)BackplateType.Infinite)
                        PropertyField(m_BlendAmount);
                    PropertyField(m_PointLightShadow, Styles.pointSpotShadow);
                    PropertyField(m_DirLightShadow, Styles.directionalShadow);
                    PropertyField(m_RectLightShadow, Styles.areaShadow);
                    PropertyField(m_ShadowTint);
                    if (updateDefaultShadowTint || GUILayout.Button(Styles.resetColors))
                    {
                        m_ShadowTint.value.colorValue = new Color(m_UpperHemisphereLuxColor.value.vector3Value.x, m_UpperHemisphereLuxColor.value.vector3Value.y, m_UpperHemisphereLuxColor.value.vector3Value.z);
                    }
                }
            }
        }
    }
}
