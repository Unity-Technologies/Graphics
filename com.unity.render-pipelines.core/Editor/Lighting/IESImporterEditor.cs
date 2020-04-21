using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    public abstract class IESImporterEditor : ScriptedImporterEditor
    {
        GUIStyle m_WordWrapStyle = new GUIStyle();

        SerializedProperty m_FileFormatVersionProp;
        SerializedProperty m_IesPhotometricTypeProp;
        SerializedProperty m_IesMaximumIntensityProp;
        SerializedProperty m_IesMaximumIntensityUnitProp;

        SerializedProperty m_ManufacturerProp;
        SerializedProperty m_LuminaireCatalogNumberProp;
        SerializedProperty m_LuminaireDescriptionProp;
        SerializedProperty m_LampCatalogNumberProp;
        SerializedProperty m_LampDescriptionProp;

        SerializedProperty m_PrefabLightTypeProp;
        SerializedProperty m_SpotAngleProp;
        SerializedProperty m_SpotCookieSizeProp;
        SerializedProperty m_ApplyLightAttenuationProp;
        SerializedProperty m_UseIesMaximumIntensityProp;
        SerializedProperty m_CookieCompressionProp;
        protected SerializedProperty m_LightAimAxisRotationProp;

        bool m_ShowLuminaireProductInformation = true;
        bool m_ShowLightProperties             = true;

        protected PreviewRenderUtility m_PreviewRenderUtility = null;

        public override void OnEnable()
        {
            base.OnEnable();

            m_WordWrapStyle.wordWrap = true;

            m_FileFormatVersionProp       = serializedObject.FindProperty("FileFormatVersion");
            m_IesPhotometricTypeProp      = serializedObject.FindProperty("IesPhotometricType");
            m_IesMaximumIntensityProp     = serializedObject.FindProperty("IesMaximumIntensity");
            m_IesMaximumIntensityUnitProp = serializedObject.FindProperty("IesMaximumIntensityUnit");

            m_ManufacturerProp            = serializedObject.FindProperty("Manufacturer");
            m_LuminaireCatalogNumberProp  = serializedObject.FindProperty("LuminaireCatalogNumber");
            m_LuminaireDescriptionProp    = serializedObject.FindProperty("LuminaireDescription");
            m_LampCatalogNumberProp       = serializedObject.FindProperty("LampCatalogNumber");
            m_LampDescriptionProp         = serializedObject.FindProperty("LampDescription");

            m_PrefabLightTypeProp         = serializedObject.FindProperty("PrefabLightType");
            m_SpotAngleProp               = serializedObject.FindProperty("SpotAngle");
            m_SpotCookieSizeProp          = serializedObject.FindProperty("SpotCookieSize");
            m_ApplyLightAttenuationProp   = serializedObject.FindProperty("ApplyLightAttenuation");
            m_UseIesMaximumIntensityProp  = serializedObject.FindProperty("UseIesMaximumIntensity");
            m_CookieCompressionProp       = serializedObject.FindProperty("CookieCompression");
            m_LightAimAxisRotationProp    = serializedObject.FindProperty("LightAimAxisRotation");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("File Format Version", m_FileFormatVersionProp.stringValue);
            EditorGUILayout.LabelField("Photometric Type",    m_IesPhotometricTypeProp.stringValue);
            EditorGUILayout.LabelField("Maximum Intensity",   $"{m_IesMaximumIntensityProp.floatValue} {m_IesMaximumIntensityUnitProp.stringValue}");

            if (m_ShowLuminaireProductInformation = EditorGUILayout.Foldout(m_ShowLuminaireProductInformation, "Luminaire Product Information"))
            {
                EditorGUILayout.LabelField(m_ManufacturerProp.displayName,           m_ManufacturerProp.stringValue,           m_WordWrapStyle);
                EditorGUILayout.LabelField(m_LuminaireCatalogNumberProp.displayName, m_LuminaireCatalogNumberProp.stringValue, m_WordWrapStyle);
                EditorGUILayout.LabelField(m_LuminaireDescriptionProp.displayName,   m_LuminaireDescriptionProp.stringValue,   m_WordWrapStyle);
                EditorGUILayout.LabelField(m_LampCatalogNumberProp.displayName,      m_LampCatalogNumberProp.stringValue,      m_WordWrapStyle);
                EditorGUILayout.LabelField(m_LampDescriptionProp.displayName,        m_LampDescriptionProp.stringValue,        m_WordWrapStyle);
            }

            if (m_ShowLightProperties = EditorGUILayout.Foldout(m_ShowLightProperties, "Light and Cookie Properties"))
            {
                EditorGUILayout.PropertyField(m_PrefabLightTypeProp, new GUIContent("Light Type"));

                EditorGUILayout.PropertyField(m_SpotAngleProp);
                EditorGUILayout.PropertyField(m_SpotCookieSizeProp, new GUIContent("Cookie Size"));
                EditorGUILayout.PropertyField(m_ApplyLightAttenuationProp);

                EditorGUILayout.PropertyField(m_CookieCompressionProp);

                LayoutRenderPipelineUseIesMaximumIntensity();

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(m_LightAimAxisRotationProp, new GUIContent("Aim Axis Rotation"));

                    if (GUILayout.Button("Reset", GUILayout.Width(44)))
                    {
                        m_LightAimAxisRotationProp.floatValue = -90f;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();

            ApplyRevertGUI();
        }

        protected override void Apply()
        {
            base.Apply();

            if (m_PreviewRenderUtility != null)
            {
                m_PreviewRenderUtility.Cleanup();
                m_PreviewRenderUtility = null;
            }
        }

        public override bool HasPreviewGUI()
        {
            if (m_PreviewRenderUtility == null)
            {
                m_PreviewRenderUtility = new PreviewRenderUtility();

                m_PreviewRenderUtility.ambientColor = Color.black;

                m_PreviewRenderUtility.camera.fieldOfView                = 60f;
                m_PreviewRenderUtility.camera.nearClipPlane              = 0.1f;
                m_PreviewRenderUtility.camera.farClipPlane               = 10f;
                m_PreviewRenderUtility.camera.transform.localPosition    = new Vector3(1.85f, 0.71f, 0f);
                m_PreviewRenderUtility.camera.transform.localEulerAngles = new Vector3(15f, -90f, 0f);

               SetupRenderPipelinePreviewCamera(m_PreviewRenderUtility.camera);

                m_PreviewRenderUtility.lights[0].type                       = (m_PrefabLightTypeProp.enumValueIndex == (int)IESLightType.Point) ? LightType.Point : LightType.Spot;
                m_PreviewRenderUtility.lights[0].color                      = Color.white;
                m_PreviewRenderUtility.lights[0].intensity                  = 1f;
                m_PreviewRenderUtility.lights[0].range                      = 10f;
                m_PreviewRenderUtility.lights[0].spotAngle                  = m_SpotAngleProp.floatValue;
                m_PreviewRenderUtility.lights[0].transform.localPosition    = new Vector3(0.14f, 1f, 0f);
                m_PreviewRenderUtility.lights[0].transform.localEulerAngles = new Vector3(90f, 0f, -90f);

                SetupRenderPipelinePreviewLight(m_PreviewRenderUtility.lights[0]);

                m_PreviewRenderUtility.lights[1].intensity = 0f;

                GameObject previewWall = GameObject.CreatePrimitive(PrimitiveType.Plane);
                previewWall.name                         = "IESPreviewWall";
                previewWall.hideFlags                    = HideFlags.HideAndDontSave;
                previewWall.transform.localPosition      = new Vector3(0f, 4f, 0f);
                previewWall.transform.localEulerAngles   = new Vector3(0f, 0f, -90f);
                previewWall.transform.localScale         = new Vector3(1f, 1f, 10f);
                MeshRenderer previewWallRenderer         = previewWall.GetComponent<MeshRenderer>();
                previewWallRenderer.lightProbeUsage      = LightProbeUsage.Off;
                previewWallRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                previewWallRenderer.material             = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

                SetupRenderPipelinePreviewWallRenderer(previewWallRenderer);

                m_PreviewRenderUtility.AddSingleGO(previewWall);

                GameObject previewFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                previewFloor.name                         = "IESPreviewFloor";
                previewFloor.hideFlags                    = HideFlags.HideAndDontSave;
                previewFloor.transform.localPosition      = new Vector3(4f, 0f, 0f);
                previewFloor.transform.localEulerAngles   = new Vector3(0f, 0f, 0f);
                previewFloor.transform.localScale         = new Vector3(1f, 1f, 10f);
                MeshRenderer previewFloorRenderer         = previewFloor.GetComponent<MeshRenderer>();
                previewFloorRenderer.lightProbeUsage      = LightProbeUsage.Off;
                previewFloorRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                previewFloorRenderer.material             = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");

                SetupRenderPipelinePreviewFloorRenderer(previewFloorRenderer);

                m_PreviewRenderUtility.AddSingleGO(previewFloor);
            }

            return true;
        }

        public abstract void LayoutRenderPipelineUseIesMaximumIntensity();
        public abstract void SetupRenderPipelinePreviewCamera(Camera camera);
        public abstract void SetupRenderPipelinePreviewLight(Light light);
        public abstract void SetupRenderPipelinePreviewWallRenderer(MeshRenderer wallRenderer);
        public abstract void SetupRenderPipelinePreviewFloorRenderer(MeshRenderer floorRenderer);
        public abstract void SetupRenderPipelinePreviewLightIntensity(Light light);

        void OnDestroy()
        {
            if (m_PreviewRenderUtility != null)
            {
                m_PreviewRenderUtility.Cleanup();
                m_PreviewRenderUtility = null;
            }
        }
    }
}
