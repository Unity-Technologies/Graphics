using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(IesImporter))]
    public class IesImporterEditor : ScriptedImporterEditor
    {
        GUIStyle m_WordWrapStyle = new GUIStyle();

        SerializedProperty m_ManufacturerProp;
        SerializedProperty m_LuminaireCatalogNumberProp;
        SerializedProperty m_LuminaireDescriptionProp;
        SerializedProperty m_LampCatalogNumberProp;
        SerializedProperty m_LampDescriptionProp;
        SerializedProperty m_ProfileRotationInYProp;

        bool m_ShowLuminaireProductInformation = true;
        bool m_PreviewCylindricalTexture       = false;

        PreviewRenderUtility m_PreviewRenderUtility = null;

        readonly bool k_UsingHdrp = RenderPipelineManager.currentPipeline?.ToString() == "UnityEngine.Rendering.HighDefinition.HDRenderPipeline";

        public override void OnEnable()
        {
            base.OnEnable();

            m_WordWrapStyle.wordWrap = true;

            m_ManufacturerProp           = serializedObject.FindProperty("Manufacturer");
            m_LuminaireCatalogNumberProp = serializedObject.FindProperty("LuminaireCatalogNumber");
            m_LuminaireDescriptionProp   = serializedObject.FindProperty("LuminaireDescription");
            m_LampCatalogNumberProp      = serializedObject.FindProperty("LampCatalogNumber");
            m_LampDescriptionProp        = serializedObject.FindProperty("LampDescription");
            m_ProfileRotationInYProp     = serializedObject.FindProperty("ProfileRotationInY");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (m_ShowLuminaireProductInformation = EditorGUILayout.Foldout(m_ShowLuminaireProductInformation, "Luminaire Product Information"))
            {
                EditorGUILayout.LabelField(m_ManufacturerProp.displayName, m_ManufacturerProp.stringValue, m_WordWrapStyle);
                EditorGUILayout.LabelField(m_LuminaireCatalogNumberProp.displayName, m_LuminaireCatalogNumberProp.stringValue, m_WordWrapStyle);
                EditorGUILayout.LabelField(m_LuminaireDescriptionProp.displayName, m_LuminaireDescriptionProp.stringValue, m_WordWrapStyle);
                EditorGUILayout.LabelField(m_LampCatalogNumberProp.displayName, m_LampCatalogNumberProp.stringValue, m_WordWrapStyle);
                EditorGUILayout.LabelField(m_LampDescriptionProp.displayName, m_LampDescriptionProp.stringValue, m_WordWrapStyle);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            ApplyRevertGUI();
        }

        public override bool HasPreviewGUI()
        {
            if (m_PreviewRenderUtility == null)
            {
                m_PreviewRenderUtility = new PreviewRenderUtility();

                m_PreviewRenderUtility.ambientColor = Color.black;

                m_PreviewRenderUtility.camera.fieldOfView   = 60f;
                m_PreviewRenderUtility.camera.nearClipPlane = 1f;
                m_PreviewRenderUtility.camera.farClipPlane  = 20f;
                m_PreviewRenderUtility.camera.transform.localPosition    = new Vector3(12f, 5f, 0f);
                m_PreviewRenderUtility.camera.transform.localEulerAngles = new Vector3(15f, -90f, 0f);

                m_PreviewRenderUtility.lights[0].type      = LightType.Point;
                m_PreviewRenderUtility.lights[0].color     = Color.white;
                m_PreviewRenderUtility.lights[0].intensity = 1.3f;
                m_PreviewRenderUtility.lights[0].range     = 60f;
                m_PreviewRenderUtility.lights[0].transform.localPosition    = new Vector3(0f, 7f, 0f);
                m_PreviewRenderUtility.lights[0].transform.localEulerAngles = new Vector3(0f, 0f, 0f);

                m_PreviewRenderUtility.lights[1].intensity = 0f;

                GameObject previewWall = GameObject.CreatePrimitive(PrimitiveType.Plane);
                previewWall.name = "IESPreviewWall";
                previewWall.hideFlags = HideFlags.HideAndDontSave;
                previewWall.transform.localPosition    = new Vector3(-1f, 4f, 0f);
                previewWall.transform.localEulerAngles = new Vector3(0f, 0f, -90f);
                previewWall.transform.localScale       = new Vector3(1f, 1f, 10f);
                MeshRenderer previewWallRenderer = previewWall.GetComponent<MeshRenderer>();
                previewWallRenderer.lightProbeUsage      = LightProbeUsage.Off;
                previewWallRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                previewWallRenderer.material             = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

                m_PreviewRenderUtility.AddSingleGO(previewWall);

                GameObject previewFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                previewFloor.name = "IESPreviewFloor";
                previewFloor.hideFlags = HideFlags.HideAndDontSave;
                previewFloor.transform.localPosition    = new Vector3(3f, 0f, 0f);
                previewFloor.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                previewFloor.transform.localScale       = new Vector3(1f, 1f, 10f);
                MeshRenderer previewFloorRenderer = previewFloor.GetComponent<MeshRenderer>();
                previewFloorRenderer.lightProbeUsage      = LightProbeUsage.Off;
                previewFloorRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                previewFloorRenderer.material             = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");

                m_PreviewRenderUtility.AddSingleGO(previewFloor);

                HDAdditionalCameraData hdCamera = m_PreviewRenderUtility.camera.gameObject.AddComponent<HDAdditionalCameraData>();
                HDAdditionalCameraData.InitDefaultHDAdditionalCameraData(hdCamera);
                hdCamera.isEditorCameraPreview = true;

                HDAdditionalLightData hdLight = m_PreviewRenderUtility.lights[0].gameObject.AddComponent<HDAdditionalLightData>();
                HDAdditionalLightData.InitDefaultHDAdditionalLightData(hdLight);
                hdLight.SetIntensity(1000000f, LightUnit.Candela);
                hdLight.shapeRadius    = 5f;
                hdLight.affectDiffuse  = true;
                hdLight.affectSpecular = false;

                previewWallRenderer.material  = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipelineResources/Material/DefaultHDMaterial.mat");
                previewFloorRenderer.material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipelineResources/Material/DefaultHDMaterial.mat");
            }

            return true;
        }

        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent(m_PreviewCylindricalTexture ? "IES Cylindrical Texture" : "IES Luminaire Profile");
        }

        public override void OnPreviewSettings()
        {
            serializedObject.Update();

            if (GUILayout.Button(m_PreviewCylindricalTexture ? "Display Profile View": "Display Texture View", EditorStyles.toolbarButton, GUILayout.Width(130)))
            {
                m_PreviewCylindricalTexture = !m_PreviewCylindricalTexture;
            }

            GUI.enabled = !m_PreviewCylindricalTexture;
            int profileRotationIn = (int)m_ProfileRotationInYProp.floatValue;
            if (GUILayout.Button($"Profile Rotation {profileRotationIn}", EditorStyles.toolbarButton, GUILayout.Width(130)))
            {
                m_ProfileRotationInYProp.floatValue = (profileRotationIn + 90)%360;
            }
            GUI.enabled = true;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Apply();
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Texture previewTexture = null;

                if (m_PreviewCylindricalTexture)
                {
                    previewTexture = (target as IesImporter).CylindricalTexture;
                }
                else
                {
                    bool doRender = true;

                    m_PreviewRenderUtility.lights[0].transform.localEulerAngles = new Vector3(0f, m_ProfileRotationInYProp.floatValue, 0f);

                    if (k_UsingHdrp)
                    {
                        m_PreviewRenderUtility.lights[0].GetComponent<HDAdditionalLightData>().SetCookie((target as IesImporter).CookieTexture);
                    }
                    else
                    {
                        m_PreviewRenderUtility.lights[0].cookie = (target as IesImporter).CookieTexture;
                    }

                    if (doRender)
                    {
                        m_PreviewRenderUtility.BeginPreview(r, background);

                        bool fog = RenderSettings.fog;
                        Unsupported.SetRenderSettingsUseFogNoDirty(false);

                        m_PreviewRenderUtility.camera.Render();

                        Unsupported.SetRenderSettingsUseFogNoDirty(fog);

                        previewTexture = m_PreviewRenderUtility.EndPreview();
                    }
                }

                GUI.DrawTexture(r, (previewTexture == null) ? Texture2D.blackTexture : previewTexture, ScaleMode.ScaleToFit, false);
            }
        }

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
