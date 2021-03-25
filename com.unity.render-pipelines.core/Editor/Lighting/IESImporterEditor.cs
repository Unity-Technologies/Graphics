using System.Reflection;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Common class for IES Importer Editor (currently implemented only on HDRP)
    /// </summary>
    public class IESImporterEditor
    {
        GUIStyle m_WordWrapStyle = new GUIStyle();

        SerializedProperty m_FileFormatVersionProp;
        SerializedProperty m_IESPhotometricTypeProp;
        SerializedProperty m_IESMaximumIntensityProp;
        SerializedProperty m_IESMaximumIntensityUnitProp;

        SerializedProperty m_ManufacturerProp;
        SerializedProperty m_LuminaireCatalogNumberProp;
        SerializedProperty m_LuminaireDescriptionProp;
        SerializedProperty m_LampCatalogNumberProp;
        SerializedProperty m_LampDescriptionProp;

        SerializedProperty m_PrefabLightTypeProp;
        SerializedProperty m_SpotAngleProp;
        SerializedProperty m_IESSizeProp;
        SerializedProperty m_ApplyLightAttenuationProp;
        SerializedProperty m_UseIESMaximumIntensityProp;
        SerializedProperty m_CookieCompressionProp;

        /// <summary>
        /// Property to the aim axis rotation for projection
        /// </summary>
        protected SerializedProperty m_LightAimAxisRotationProp;

        bool m_ShowLuminaireProductInformation = true;
        bool m_ShowLightProperties = true;

        /// <summary>
        /// Object used to setup Preview renderer
        /// </summary>
        protected PreviewRenderUtility m_PreviewRenderUtility = null;

        /// <summary>
        /// Delegate prototype sent by the specialization of the IESImporterEditor per Render Pipeline
        /// </summary>
        public delegate void LayoutRenderPipelineUseIesMaximumIntensity();
        /// <summary>
        /// Delegate prototype sent by the specialization of the IESImporterEditor per Render Pipeline
        /// </summary>
        /// <param name="camera">Current camera</param>
        public delegate void SetupRenderPipelinePreviewCamera(Camera camera);
        /// <summary>
        /// Delegate prototype sent by the specialization of the IESImporterEditor per Render Pipeline
        /// </summary>
        /// <param name="light">Light will be setuped, specialization for a given SRP</param>
        public delegate void SetupRenderPipelinePreviewLight(Light light);
        /// <summary>
        /// Delegate prototype sent by the specialization of the IESImporterEditor per Render Pipeline
        /// </summary>
        /// <param name="wallRenderer">Setup a wall for the preview</param>
        public delegate void SetupRenderPipelinePreviewWallRenderer(MeshRenderer wallRenderer);
        /// <summary>
        /// Delegate prototype sent by the specialization of the IESImporterEditor per Render Pipeline
        /// </summary>
        /// <param name="floorRenderer">Setup a floor for the preview</param>
        public delegate void SetupRenderPipelinePreviewFloorRenderer(MeshRenderer floorRenderer);
        /// <summary>
        /// Delegate prototype sent by the specialization of the IESImporterEditor per Render Pipeline
        /// </summary>
        /// <param name="light">Light used to setup the IES</param>
        /// <param name="useIESMaximumIntensityProp">Serialized property to the "useIESMaximumIntensity" property</param>
        /// <param name="iesMaximumIntensityUnitProp">Serialized property to the "iesMaximumIntensityUnit" property</param>
        /// <param name="iesMaximumIntensityProp">Serialized property to the "iesMaximumIntensity" property</param>
        public delegate void SetupRenderPipelinePreviewLightIntensity(Light light, SerializedProperty useIESMaximumIntensityProp, SerializedProperty iesMaximumIntensityUnitProp, SerializedProperty iesMaximumIntensityProp);

        /// <summary>
        /// Callback called on the Implemented IESImporterEditor (currently on HDRP Only)
        /// </summary>
        /// <param name="serializedObject">Serialized object which can be linked to IESMetadata</param>
        public void CommonOnEnable(SerializedProperty serializedObject)
        {
            m_WordWrapStyle.wordWrap = true;

            m_FileFormatVersionProp = serializedObject.FindPropertyRelative("FileFormatVersion");
            m_IESPhotometricTypeProp = serializedObject.FindPropertyRelative("IESPhotometricType");
            m_IESMaximumIntensityProp = serializedObject.FindPropertyRelative("IESMaximumIntensity");
            m_IESMaximumIntensityUnitProp = serializedObject.FindPropertyRelative("IESMaximumIntensityUnit");

            m_ManufacturerProp = serializedObject.FindPropertyRelative("Manufacturer");
            m_LuminaireCatalogNumberProp = serializedObject.FindPropertyRelative("LuminaireCatalogNumber");
            m_LuminaireDescriptionProp = serializedObject.FindPropertyRelative("LuminaireDescription");
            m_LampCatalogNumberProp = serializedObject.FindPropertyRelative("LampCatalogNumber");
            m_LampDescriptionProp = serializedObject.FindPropertyRelative("LampDescription");

            m_PrefabLightTypeProp = serializedObject.FindPropertyRelative("PrefabLightType");
            m_SpotAngleProp = serializedObject.FindPropertyRelative("SpotAngle");
            m_IESSizeProp = serializedObject.FindPropertyRelative("iesSize");
            m_ApplyLightAttenuationProp = serializedObject.FindPropertyRelative("ApplyLightAttenuation");
            m_UseIESMaximumIntensityProp = serializedObject.FindPropertyRelative("UseIESMaximumIntensity");
            m_CookieCompressionProp = serializedObject.FindPropertyRelative("CookieCompression");
            m_LightAimAxisRotationProp = serializedObject.FindPropertyRelative("LightAimAxisRotation");
        }

        /// <summary>
        /// Callback called on the Implemented IESImporterEditor (currently on HDRP Only)
        /// </summary>
        /// <param name="scriptedImporter">The current specialized scripted importer using the common code</param>
        public void CommonOnInspectorGUI(ScriptedImporterEditor scriptedImporter)
        {
            scriptedImporter.serializedObject.Update();

            EditorGUILayout.LabelField("File Format Version", m_FileFormatVersionProp.stringValue);
            EditorGUILayout.LabelField("Photometric Type", m_IESPhotometricTypeProp.stringValue);
            EditorGUILayout.LabelField("Maximum Intensity", $"{m_IESMaximumIntensityProp.floatValue} {m_IESMaximumIntensityUnitProp.stringValue}");

            if (m_ShowLuminaireProductInformation = EditorGUILayout.Foldout(m_ShowLuminaireProductInformation, "Luminaire Product Information"))
            {
                EditorGUILayout.LabelField(m_ManufacturerProp.displayName, m_ManufacturerProp.stringValue, m_WordWrapStyle);
                EditorGUILayout.LabelField(m_LuminaireCatalogNumberProp.displayName, m_LuminaireCatalogNumberProp.stringValue, m_WordWrapStyle);
                EditorGUILayout.LabelField(m_LuminaireDescriptionProp.displayName, m_LuminaireDescriptionProp.stringValue, m_WordWrapStyle);
                EditorGUILayout.LabelField(m_LampCatalogNumberProp.displayName, m_LampCatalogNumberProp.stringValue, m_WordWrapStyle);
                EditorGUILayout.LabelField(m_LampDescriptionProp.displayName, m_LampDescriptionProp.stringValue, m_WordWrapStyle);
            }

            if (m_ShowLightProperties = EditorGUILayout.Foldout(m_ShowLightProperties, "Light and Cookie Properties"))
            {
                EditorGUILayout.PropertyField(m_PrefabLightTypeProp, new GUIContent("Light Type"));
                EditorGUILayout.PropertyField(m_SpotAngleProp);
                EditorGUILayout.PropertyField(m_IESSizeProp, new GUIContent("IES Size"));
                EditorGUILayout.PropertyField(m_ApplyLightAttenuationProp);
                EditorGUILayout.PropertyField(m_CookieCompressionProp, new GUIContent("IES Compression"));

                // Before enabling this feature, more experimentation is needed with the addition of a Volume in the PreviewRenderUtility scene.
                EditorGUILayout.PropertyField(m_UseIESMaximumIntensityProp, new GUIContent("Use IES Maximum Intensity"));

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(m_LightAimAxisRotationProp, new GUIContent("Aim Axis Rotation"));

                    if (GUILayout.Button("Reset", GUILayout.Width(44)))
                    {
                        m_LightAimAxisRotationProp.floatValue = -90f;
                    }
                }
            }

            scriptedImporter.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Callback called on the Implemented IESImporterEditor (currently on HDRP Only)
        /// </summary>
        public void CommonApply()
        {
            if (m_PreviewRenderUtility != null)
            {
                m_PreviewRenderUtility.Cleanup();
                m_PreviewRenderUtility = null;
            }
        }

        /// <summary>
        /// Callback called on the Implemented IESImporterEditor (currently on HDRP Only)
        /// </summary>
        /// <param name="setupRenderPipelinePreviewCamera">Delegate provided by the Render pipeline to setup the Preview Camera</param>
        /// <param name="setupRenderPipelinePreviewFloorRenderer">Delegate provided by the Render pipeline to setup the Preview Light</param>
        /// <param name="setupRenderPipelinePreviewLight">Delegate provided by the Render pipeline to setup the Preview Wall</param>
        /// <param name="setupRenderPipelinePreviewWallRenderer">Delegate provided by the Render pipeline to setup the Preview Floor</param>
        /// <returns>true to specified IES has a Preview</returns>
        public bool CommonHasPreviewGUI(SetupRenderPipelinePreviewCamera setupRenderPipelinePreviewCamera,
            SetupRenderPipelinePreviewLight setupRenderPipelinePreviewLight,
            SetupRenderPipelinePreviewWallRenderer setupRenderPipelinePreviewWallRenderer,
            SetupRenderPipelinePreviewFloorRenderer setupRenderPipelinePreviewFloorRenderer)
        {
            if (m_PreviewRenderUtility == null)
            {
                m_PreviewRenderUtility = new PreviewRenderUtility();

                m_PreviewRenderUtility.ambientColor = Color.black;

                m_PreviewRenderUtility.camera.fieldOfView = 60f;
                m_PreviewRenderUtility.camera.nearClipPlane = 0.1f;
                m_PreviewRenderUtility.camera.farClipPlane = 10f;
                m_PreviewRenderUtility.camera.transform.localPosition = new Vector3(1.85f, 0.71f, 0f);
                m_PreviewRenderUtility.camera.transform.localEulerAngles = new Vector3(15f, -90f, 0f);

                setupRenderPipelinePreviewCamera(m_PreviewRenderUtility.camera);

                m_PreviewRenderUtility.lights[0].type = (m_PrefabLightTypeProp.enumValueIndex == (int)IESLightType.Point) ? LightType.Point : LightType.Spot;
                m_PreviewRenderUtility.lights[0].color = Color.white;
                m_PreviewRenderUtility.lights[0].intensity = 1f;
                m_PreviewRenderUtility.lights[0].range = 10f;
                m_PreviewRenderUtility.lights[0].spotAngle = m_SpotAngleProp.floatValue;
                m_PreviewRenderUtility.lights[0].transform.localPosition = new Vector3(0.14f, 1f, 0f);
                m_PreviewRenderUtility.lights[0].transform.localEulerAngles = new Vector3(90f, 0f, -90f);

                setupRenderPipelinePreviewLight(m_PreviewRenderUtility.lights[0]);

                m_PreviewRenderUtility.lights[1].intensity = 0f;

                GameObject previewWall = GameObject.CreatePrimitive(PrimitiveType.Plane);
                previewWall.name = "IESPreviewWall";
                previewWall.hideFlags = HideFlags.HideAndDontSave;
                previewWall.transform.localPosition = new Vector3(0f, 4f, 0f);
                previewWall.transform.localEulerAngles = new Vector3(0f, 0f, -90f);
                previewWall.transform.localScale = new Vector3(1f, 1f, 10f);
                MeshRenderer previewWallRenderer = previewWall.GetComponent<MeshRenderer>();
                previewWallRenderer.lightProbeUsage = LightProbeUsage.Off;
                previewWallRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                previewWallRenderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

                setupRenderPipelinePreviewWallRenderer(previewWallRenderer);

                m_PreviewRenderUtility.AddSingleGO(previewWall);

                GameObject previewFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                previewFloor.name = "IESPreviewFloor";
                previewFloor.hideFlags = HideFlags.HideAndDontSave;
                previewFloor.transform.localPosition = new Vector3(4f, 0f, 0f);
                previewFloor.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                previewFloor.transform.localScale = new Vector3(1f, 1f, 10f);
                MeshRenderer previewFloorRenderer = previewFloor.GetComponent<MeshRenderer>();
                previewFloorRenderer.lightProbeUsage = LightProbeUsage.Off;
                previewFloorRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                previewFloorRenderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");

                setupRenderPipelinePreviewFloorRenderer(previewFloorRenderer);

                m_PreviewRenderUtility.AddSingleGO(previewFloor);
            }

            return true;
        }

        /// <summary>
        /// Callback called on the Implemented IESImporterEditor (currently on HDRP Only)
        /// </summary>
        /// <returns>The title of the Preview</returns>
        public GUIContent CommonGetPreviewTitle()
        {
            return new GUIContent("IES Luminaire Profile");
        }

        /// <summary>
        /// Callback called on the Implemented IESImporterEditor (currently on HDRP Only)
        /// </summary>
        /// <param name="background">Background of the Preview</param>
        /// <param name="r">Rect of the Preview</param>
        /// <param name="target">ScriptedImporter targeted</param>
        /// <param name="setupRenderPipelinePreviewLightIntensity">Delegate provided by the Rendering Pipeline to setup the Light Intensity</param>
        public void CommonOnPreviewGUI(Rect r, GUIStyle background, ScriptedImporter target,
            SetupRenderPipelinePreviewLightIntensity setupRenderPipelinePreviewLightIntensity)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Texture cookieTexture = null;
                Texture previewTexture = null;

                if (m_PrefabLightTypeProp.enumValueIndex == (int)IESLightType.Point)
                {
                    foreach (var subAsset in AssetDatabase.LoadAllAssetRepresentationsAtPath(target.assetPath))
                    {
                        if (subAsset.name.EndsWith("-Cube-IES"))
                        {
                            cookieTexture = subAsset as Texture;
                            break;
                        }
                    }
                }
                else // LightType.Spot
                {
                    foreach (var subAsset in AssetDatabase.LoadAllAssetRepresentationsAtPath(target.assetPath))
                    {
                        if (subAsset.name.EndsWith("-2D-IES"))
                        {
                            cookieTexture = subAsset as Texture;
                            break;
                        }
                    }
                }

                if (cookieTexture != null)
                {
                    m_PreviewRenderUtility.lights[0].transform.localEulerAngles = new Vector3(90f, 0f, m_LightAimAxisRotationProp.floatValue);
                    setupRenderPipelinePreviewLightIntensity(m_PreviewRenderUtility.lights[0], m_UseIESMaximumIntensityProp, m_IESMaximumIntensityUnitProp, m_IESMaximumIntensityProp);
                    m_PreviewRenderUtility.lights[0].cookie = cookieTexture;
                    m_PreviewRenderUtility.lights[0].type = m_PrefabLightTypeProp.enumValueIndex == (int)IESLightType.Point ? LightType.Point : LightType.Spot;

                    m_PreviewRenderUtility.BeginPreview(r, background);

                    bool fog = RenderSettings.fog;
                    Unsupported.SetRenderSettingsUseFogNoDirty(false);

                    m_PreviewRenderUtility.camera.Render();

                    Unsupported.SetRenderSettingsUseFogNoDirty(fog);

                    previewTexture = m_PreviewRenderUtility.EndPreview();
                }

                if (previewTexture == null)
                {
                    GUI.DrawTexture(r, Texture2D.blackTexture, ScaleMode.StretchToFill, false);
                }
                else
                {
                    GUI.DrawTexture(r, previewTexture, ScaleMode.ScaleToFit, false);
                }
            }
        }

        /// <summary>
        /// Callback called on the Implemented IESImporterEditor (currently on HDRP Only)
        /// </summary>
        public void CommonOnDisable()
        {
            if (m_PreviewRenderUtility != null)
            {
                m_PreviewRenderUtility.Cleanup();
                m_PreviewRenderUtility = null;
            }
        }
    }
}
