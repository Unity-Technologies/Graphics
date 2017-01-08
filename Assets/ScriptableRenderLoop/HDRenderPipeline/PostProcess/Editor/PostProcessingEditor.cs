using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using GradingType = PostProcessing.ColorGradingSettings.GradingType;
    using EyeAdaptationType = PostProcessing.EyeAdaptationSettings.EyeAdaptationType;

    [CustomEditor(typeof(PostProcessing))]
    public class PostProcessingEditor : Editor
    {
        public class ColorGradingSettings
        {
            public SerializedProperty type;
            public SerializedProperty exposure;

            public SerializedProperty logLut;

            public SerializedProperty neutralBlackIn;
            public SerializedProperty neutralWhiteIn;
            public SerializedProperty neutralBlackOut;
            public SerializedProperty neutralWhiteOut;
            public SerializedProperty neutralWhiteLevel;
            public SerializedProperty neutralWhiteClip;
        }

        public class EyeAdaptationSettings
        {
            public SerializedProperty enabled;
            public SerializedProperty showDebugHistogramInGameView;

            public SerializedProperty lowPercent;
            public SerializedProperty highPercent;

            public SerializedProperty minLuminance;
            public SerializedProperty maxLuminance;
            public SerializedProperty exposureCompensation;

            public SerializedProperty adaptationType;

            public SerializedProperty speedUp;
            public SerializedProperty speedDown;

            public SerializedProperty logMin;
            public SerializedProperty logMax;
        }

        public ColorGradingSettings colorGrading;
        public EyeAdaptationSettings eyeAdaptation;

        void OnEnable()
        {
            colorGrading = new ColorGradingSettings()
            {
                type = serializedObject.FindProperty("colorGrading.type"),
                exposure = serializedObject.FindProperty("colorGrading.exposure"),

                logLut = serializedObject.FindProperty("colorGrading.logLut"),

                neutralBlackIn = serializedObject.FindProperty("colorGrading.neutralBlackIn"),
                neutralWhiteIn = serializedObject.FindProperty("colorGrading.neutralWhiteIn"),
                neutralBlackOut = serializedObject.FindProperty("colorGrading.neutralBlackOut"),
                neutralWhiteOut = serializedObject.FindProperty("colorGrading.neutralWhiteOut"),
                neutralWhiteLevel = serializedObject.FindProperty("colorGrading.neutralWhiteLevel"),
                neutralWhiteClip = serializedObject.FindProperty("colorGrading.neutralWhiteClip")
            };

            eyeAdaptation = new EyeAdaptationSettings()
            {
                enabled = serializedObject.FindProperty("eyeAdaptation.enabled"),
                showDebugHistogramInGameView = serializedObject.FindProperty("eyeAdaptation.showDebugHistogramInGameView"),

                lowPercent = serializedObject.FindProperty("eyeAdaptation.lowPercent"),
                highPercent = serializedObject.FindProperty("eyeAdaptation.highPercent"),

                minLuminance = serializedObject.FindProperty("eyeAdaptation.minLuminance"),
                maxLuminance = serializedObject.FindProperty("eyeAdaptation.maxLuminance"),
                exposureCompensation = serializedObject.FindProperty("eyeAdaptation.exposureCompensation"),

                adaptationType = serializedObject.FindProperty("eyeAdaptation.adaptationType"),

                speedUp = serializedObject.FindProperty("eyeAdaptation.speedUp"),
                speedDown = serializedObject.FindProperty("eyeAdaptation.speedDown"),

                logMin = serializedObject.FindProperty("eyeAdaptation.logMin"),
                logMax = serializedObject.FindProperty("eyeAdaptation.logMax")
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var camera = (target as PostProcessing).GetComponent<Camera>();
            if (camera == null)
                EditorGUILayout.HelpBox("Global post-processing settings will be overriden by local camera settings. Global settings are the only one visible in the scene view.", MessageType.Info);

            EditorGUILayout.LabelField("Color Grading", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            if (camera != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(EditorGUI.indentLevel * 15f);
                    if (GUILayout.Button("Export frame to EXR", EditorStyles.miniButton))
                    {
                        string path = EditorUtility.SaveFilePanelInProject("Export frame as EXR...", "Frame.exr", "exr", "");

                        if (!string.IsNullOrEmpty(path))
                            SaveFrameToEXR(camera, path);
                    }
                }
            }

            EditorGUILayout.PropertyField(colorGrading.exposure);
            EditorGUILayout.PropertyField(colorGrading.type);

            EditorGUI.indentLevel++;

            var gradingType = (GradingType)colorGrading.type.intValue;

            if (gradingType == GradingType.Custom)
            {
                EditorGUILayout.PropertyField(colorGrading.logLut);

                if (!ValidateLutImportSettings())
                {
                    EditorGUILayout.HelpBox("Invalid LUT import settings.", MessageType.Warning);

                    GUILayout.Space(-32);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Fix", GUILayout.Width(60)))
                        {
                            SetLUTImportSettings();
                            AssetDatabase.Refresh();
                        }
                        GUILayout.Space(8);
                    }
                    GUILayout.Space(11);
                }
            }
            else if (gradingType == GradingType.Neutral)
            {
                EditorGUILayout.PropertyField(colorGrading.neutralBlackIn);
                EditorGUILayout.PropertyField(colorGrading.neutralWhiteIn);
                EditorGUILayout.PropertyField(colorGrading.neutralBlackOut);
                EditorGUILayout.PropertyField(colorGrading.neutralWhiteOut);
                EditorGUILayout.PropertyField(colorGrading.neutralWhiteLevel);
                EditorGUILayout.PropertyField(colorGrading.neutralWhiteClip);
            }

            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Eye Adaptation", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(eyeAdaptation.enabled);

            if (eyeAdaptation.enabled.boolValue)
            {
                EditorGUILayout.PropertyField(eyeAdaptation.showDebugHistogramInGameView);

                EditorGUILayout.PropertyField(eyeAdaptation.logMin, new GUIContent("Histogram Log Min"));
                EditorGUILayout.PropertyField(eyeAdaptation.logMax, new GUIContent("Histogram Log Max"));
                EditorGUILayout.Space();

                float low = eyeAdaptation.lowPercent.floatValue;
                float high = eyeAdaptation.highPercent.floatValue;

                EditorGUILayout.MinMaxSlider(new GUIContent("Filter"), ref low, ref high, 1f, 99f);

                eyeAdaptation.lowPercent.floatValue = low;
                eyeAdaptation.highPercent.floatValue = high;

                EditorGUILayout.PropertyField(eyeAdaptation.minLuminance);
                EditorGUILayout.PropertyField(eyeAdaptation.maxLuminance);
                EditorGUILayout.PropertyField(eyeAdaptation.exposureCompensation);

                EditorGUILayout.PropertyField(eyeAdaptation.adaptationType);

                if (eyeAdaptation.adaptationType.intValue == (int)EyeAdaptationType.Progressive)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(eyeAdaptation.speedUp);
                    EditorGUILayout.PropertyField(eyeAdaptation.speedDown);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }

        void SetLUTImportSettings()
        {
            var lut = (target as PostProcessing).colorGrading.logLut;
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(lut));
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.anisoLevel = 0;
            importer.sRGBTexture = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
        }

        bool ValidateLutImportSettings()
        {
            var lut = (target as PostProcessing).colorGrading.logLut;

            if (lut == null)
                return true;

            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(lut));
            return importer.anisoLevel == 0
                && importer.mipmapEnabled == false
                && importer.sRGBTexture == false
                && (importer.textureCompression == TextureImporterCompression.Uncompressed)
                && importer.filterMode == FilterMode.Bilinear;
        }

        void SaveFrameToEXR(Camera camera, string path)
        {
            // We want a 1024x32 sized render at a minimum so that we have enough space to stamp the lut
            var aspect = (float)camera.pixelHeight / (float)camera.pixelWidth;
            var width = camera.pixelWidth >= 1024
                ? camera.pixelWidth
                : 1024;
            var height = Mathf.RoundToInt(width * aspect);

            var texture = new Texture2D(width, height, TextureFormat.RGBAHalf, false, true);
            var targetRt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

            // Render the current frame without post processing
            var oldPPState = (target as PostProcessing).enabled;
            (target as PostProcessing).enabled = false;
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            camera.targetTexture = targetRt;
            camera.Render();

            // Stamp the log lut in the top left corner
            const int k_InternalLogLutSize = 32;
            var stampMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/LutGen");
            stampMaterial.SetVector("_LutParams", new Vector4(
                    k_InternalLogLutSize,
                    0.5f / (k_InternalLogLutSize * k_InternalLogLutSize),
                    0.5f / k_InternalLogLutSize,
                    k_InternalLogLutSize / (k_InternalLogLutSize - 1f))
                );

            var stampRt = RenderTexture.GetTemporary(1024, 32, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            Graphics.Blit(null, stampRt, stampMaterial, 0);

            RenderTexture.active = targetRt;
            GL.PushMatrix();
            {
                GL.LoadPixelMatrix(0, width, height, 0);
                Graphics.DrawTexture(new Rect(0, 0, stampRt.width, stampRt.height), stampRt);
            }
            GL.PopMatrix();

            // Read back
            texture.ReadPixels(new Rect(0, 0, targetRt.width, targetRt.height), 0, 0);
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            (target as PostProcessing).enabled = oldPPState;

            // Cleanup
            RenderTexture.ReleaseTemporary(stampRt);
            RenderTexture.ReleaseTemporary(targetRt);
            Utilities.Destroy(stampMaterial);

            // Save
            File.WriteAllBytes(path, texture.EncodeToEXR(Texture2D.EXRFlags.CompressPIZ));
            AssetDatabase.Refresh();
        }
    }
}
